// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Plugin.cs" company="KriaSoft LLC">
//   Copyright © 2013 Konstantin Tarkus, KriaSoft LLC. See LICENSE.txt
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace AmiBroker.Plugin
{
    using System;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Drawing;
    using System.Linq;
    //using System.Net.Http;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Windows.Controls;
    using System.Windows.Forms;
    using Controls;
    using Models;
    using System.Collections.Generic;

    using System.Net;
    using System.IO;

    // Timer
    using System.Threading;


    // JSON
    using Newtonsoft.Json;
    
    using RGiesecke.DllExport;

    using WebSocket4Net;

    /// <summary>
    /// Standard implementation of a typical AmiBroker plug-ins.
    /// </summary>
    public class Plugin
    {

        public static string pluginVersion;

        public static WebSocket wss;
        public static bool isSocketConnected = false;
        public static bool isFirstRun = true;

        public static string lastTicker = "";

        /// <summary>
        /// Plugin status code
        /// </summary>
        static StatusCode Status = StatusCode.OK;

        // Дескриптор окна = null по умолчанию
        static IntPtr mainWnd = IntPtr.Zero;

        static string jsonAnswer = "";
        /// <summary>
        /// Default encoding
        /// </summary>
        static Encoding encoding = Encoding.GetEncoding("windows-1251"); // TODO: Update it based on your preferences

        /// <summary>
        /// WPF user control which is used to display right-click context menu.
        /// </summary>
        static RightClickMenu RightClickMenu;

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        public static void GetPluginInfo(ref PluginInfo pluginInfo)
        {
            //MessageBox.Show("GetPluginInfo...");

            pluginInfo.Name = "CryptoCurrencies Data Plug-in (Demo)";
            pluginInfo.Vendor = "Arakcheev V.A.";
            pluginInfo.Type = PluginType.Data;
            
            pluginInfo.Version = 0500; // v0.5.0
            pluginVersion = "0.5.0";

            pluginInfo.IDCode = new PluginID("DEMO");
            pluginInfo.Certificate = 0;
            pluginInfo.MinAmiVersion = 5600000; // v5.60
            pluginInfo.StructSize = Marshal.SizeOf((PluginInfo)pluginInfo);
        }

 
        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        public static void Init()
        {
            /*
            wss = new WebSocket("wss://stream.binance.com:9443/ws/ethbtc@kline_1m");
            wss.Opened += new EventHandler(WebSockedOpened);
            wss.MessageReceived += Wss_MessageReceived;
            wss.Closed += Wss_Closed;
            wss.Error += Wss_Error;
            wss.Open();      
   */
            BinanceHelper.onWSSMessage += Wss_MessageReceived;
            isSocketConnected = true;            
        }

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        public static void Release()
        {
            // Закрыть сокет
            wss.Close();
        }


        private static void Wss_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            Log.Write("ERR>> " + e.Exception);
        }

        private static void Wss_Closed(object sender, EventArgs e)
        {
            Log.Write("Try to close...");

            isSocketConnected = false;
        }

        private static void Wss_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            Log.Write("MSG>> " + e.Message,"WSS.log");

            jsonAnswer = e.Message;

            NativeMethods.SendMessage(mainWnd, 0x0400 + 13000, IntPtr.Zero, IntPtr.Zero); 
        }

        private static void WebSockedOpened(object sender, EventArgs e)
        {
            Log.Write("Try to open...");
            isSocketConnected = true;
        }
        

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        public static unsafe void Notify(PluginNotification* notification)
        {
           
            switch (notification->Reason)
            {
                case PluginNotificationReason.DatabaseLoaded:
 
                    mainWnd = notification->MainWnd;
                    RightClickMenu = new RightClickMenu(notification->MainWnd);
                    break;

                case PluginNotificationReason.DatabaseUnloaded:
                    break;

                case PluginNotificationReason.StatusRightClick:

                    RightClickMenu.ContextMenu.IsOpen = true;
                    break;

                case PluginNotificationReason.SettingsChange:
                    break;
            }
        }

        /// <summary>
        /// GetQuotesEx function is functional equivalent fo GetQuotes but
        /// handles new Quotation format with 64 bit date/time stamp and floating point volume/open int
        /// and new Aux fields
        /// it also takes pointer to context that is reserved for future use (can be null)
        /// Called by AmiBroker 5.27 and above 
        /// </summary>
        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        public static unsafe int GetQuotesEx(string ticker, Periodicity periodicity, int lastValid, int size, Quotation* quotes, GQEContext* context)
        {
            // Статус - в ожидании данных
            Status = StatusCode.Wait;

            #region Данных не было - БД пуста
            if (lastValid < 0)
            {
                List<Ticker> last24hBars = BinanceHelper.getLast24hBars(ticker);

                // Проверка что не пусто
                if (last24hBars == null || last24hBars.Count == 0)
                    return lastValid + 1;

                lastValid = 0;

                // Если влазим в окно - то i = 0; иначе last24hBars.Count - size

                for (var i = (last24hBars.Count < size) ? 0 : last24hBars.Count - size; i < last24hBars.Count; i++)
                {
                    quotes[lastValid].DateTime = last24hBars[i].time;
                    quotes[lastValid].Open = last24hBars[i].open;
                    quotes[lastValid].High = last24hBars[i].high;
                    quotes[lastValid].Low = last24hBars[i].low;
                    quotes[lastValid].Price = last24hBars[i].close;
                    quotes[lastValid].Volume = last24hBars[i].volume;

                    lastValid++;
                }

                // Сохраняем значение символьной пары
                lastTicker = ticker;

                return lastValid;
            }
            #endregion

            Log.Write("isConn: " + isSocketConnected + " lastT: " + lastTicker + " TC: " + ticker);

            // Проверка на то что не переключили символы + первый запуск
            if (String.IsNullOrEmpty(lastTicker) || !lastTicker.Equals(ticker))
            {
                isFirstRun = true;

                /*
                if (wss != null)
                    wss.Close();

                // Запуск сокета
                wss = new WebSocket(String.Format("wss://stream.binance.com:9443/ws/{0}@kline_{1}",ticker.ToLower(),"1m"));
                wss.Opened += new EventHandler(WebSockedOpened);
                wss.MessageReceived += Wss_MessageReceived;
               // wss.Closed += Wss_Closed;
                wss.Error += Wss_Error;
                wss.Open();      
   */
                if (!BinanceHelper.CreateWSS(ticker, periodicity))
                {
                    Log.Write("Create WSS failed!");
                    isSocketConnected = false;
                    return lastValid + 1;
                }
           // BinanceHelper.onWSSMessage = Wss_MessageReceived;
           // isSocketConnected = true;  
                
            }
            else
                isFirstRun = false;     

            // Обозначаем тикер
            lastTicker = ticker;

            #region Данные есть и это первый запуск
            if (isFirstRun && lastValid > 0)
            {                
                // Получаем данные
                List<Ticker> last24hBars = BinanceHelper.getLast24hBars(ticker);        

                // Проверка что не пусто
                if (last24hBars == null || last24hBars.Count == 0)
                    return lastValid + 1;

                // Кастрируем массив
                for (var i = 0; i < last24hBars.Count; i++ )
                {
                    AmiDate lastDate = new AmiDate(quotes[lastValid].DateTime);
                    AmiDate requestedDate = new AmiDate(last24hBars[i].time);

                    if (requestedDate.CompareTo(lastDate) <= 0)
                    {
                        last24hBars.RemoveAt(0);
                        i--;
                    }
                    else
                        // Вываливаемся из цикла так как последний элемент явно старше
                        break;
                  }

                // Вариант 1 - Count > size - переносим данные
                if (last24hBars.Count > size)
                {
                    lastValid = 0;
                   
                    // Перенос последних = size данных
                    for (var i = last24hBars.Count - size; i < last24hBars.Count; i++)
                    {
                        quotes[lastValid].DateTime = last24hBars[i].time;
                        quotes[lastValid].Open = last24hBars[i].open;
                        quotes[lastValid].High = last24hBars[i].high;
                        quotes[lastValid].Low = last24hBars[i].low;
                        quotes[lastValid].Price = last24hBars[i].close;
                        quotes[lastValid].Volume = last24hBars[i].volume;

                        lastValid++;
                    }

                    return lastValid;                                
                }

                // Вариант 2 - Count < size и входит в окно - добавить в список
                if ((last24hBars.Count < size) && (last24hBars.Count < (size - lastValid)))
                {
                    // Перенос всех из массива
                    for (var i = 0; i < last24hBars.Count; i++)
                    {
                        quotes[lastValid].DateTime = last24hBars[i].time;
                        quotes[lastValid].Open = last24hBars[i].open;
                        quotes[lastValid].High = last24hBars[i].high;
                        quotes[lastValid].Low = last24hBars[i].low;
                        quotes[lastValid].Price = last24hBars[i].close;
                        quotes[lastValid].Volume = last24hBars[i].volume;

                        lastValid++;
                    }

                    return lastValid;
                }
                else       
                    if ((last24hBars.Count < size) && (last24hBars.Count > (size - lastValid)))
                    { 
                        // Вариант 3 - данные в окно не входят - нужно сдвигать массив

                        lastValid = 0;

                        // Сколько элементов останется 
                        var j = size - last24hBars.Count;
                        // Индекс начала копирования
                        var index = lastValid - j;

                        // Смещение первой части
                        while (lastValid < j)
                        {

                            Log.Write("I: " + lastValid + " HIGH: " + quotes[index].High + " VOL: " + quotes[index].Volume, "3.txt");

                            quotes[lastValid].DateTime = quotes[index].DateTime;
                            quotes[lastValid].Open = quotes[index].Open;
                            quotes[lastValid].High = quotes[index].High;
                            quotes[lastValid].Low = quotes[index].Low;
                            quotes[lastValid].Price = quotes[index].Price;
                            quotes[lastValid].Volume = quotes[index].Volume;

                            lastValid++;
                            index++;
                        }

                        // КОпируем остатки
                        foreach (Ticker item in last24hBars)
                        {
                            Log.Write("I: " + lastValid + " HIGH: " + item.high + " VOL: " + item.volume, "4.txt");

                            quotes[lastValid].DateTime = item.time;
                            quotes[lastValid].Open = item.open;
                            quotes[lastValid].High = item.high;
                            quotes[lastValid].Low = item.low;
                            quotes[lastValid].Price = item.close;
                            quotes[lastValid].Volume = item.volume;

                            lastValid++;
                        }

                        return lastValid;
                    }
            }

            #endregion

            // не первый запуск - просто обновляем

            // Если не подключились - нечего разбирать
            if (!isSocketConnected)
                return lastValid + 1;            

            // Показывает что данные тикера устарели
            //bool isTooOld = false;
            BinanceData data = null;

            if (!String.IsNullOrEmpty(jsonAnswer))
            {         
                // Парсинг
                try
                {
                    data = JsonConvert.DeserializeObject<BinanceData>(jsonAnswer);
                }
                catch (Exception e)
                {
                    Log.Write("Parse Error: " + e.Message);
                    return lastValid + 1;
                }
            }

            // В БД есть какие-то данные
            if (lastValid >= 0)
            {
                ulong lastDate = quotes[lastValid].DateTime;
                ulong tickerDate = (new AmiDate(Utils.UnixTimeStampToDateTime(data.k.t / 1000))).ToUInt64();

                /*
                if (tickerDate < lastDate)
                    isTooOld = true;
                */

                if (tickerDate > lastDate)
                    lastValid++;
            }
            else
            {
                // Если пусто в БД - начинаем писать с 0го  индекса
                lastValid = 0;
            }


            // Поправка на лимит
            if (size > 0 && lastValid == size)
            { 
                // Сдвигание массива влево
                for (int i = 0; i < size - 1; i++)
                {
                    quotes[i].DateTime = quotes[i + 1].DateTime;
                    quotes[i].Open = quotes[i + 1].Open;
                    quotes[i].High = quotes[i + 1].High;
                    quotes[i].Low = quotes[i + 1].Low;
                    quotes[i].Price = quotes[i + 1].Price;
                    quotes[i].Volume = quotes[i + 1].Volume;
                }

                lastValid--;
            }

            // Правим
            //if (!isTooOld)
            {
                AmiDate tickerDate = new AmiDate(Utils.UnixTimeStampToDateTime(data.k.t / 1000));

                quotes[lastValid].DateTime = tickerDate.ToUInt64();         
                quotes[lastValid].Open = float.Parse(data.k.o.Replace(".", ","));
                quotes[lastValid].High = float.Parse(data.k.h.Replace(".", ","));
                quotes[lastValid].Low = float.Parse(data.k.l.Replace(".", ","));
                quotes[lastValid].Price = float.Parse(data.k.c.Replace(".", ","));
                quotes[lastValid].Volume = float.Parse(data.k.v.Replace(".", ","));
                quotes[lastValid].AuxData1 = 0;
                quotes[lastValid].AuxData2 = 0;
            } 

            Status = StatusCode.OK;
            return lastValid + 1;
        }

        


        public unsafe delegate void* Alloc(uint size);

        ///// <summary>
        ///// GetExtra data is optional function for retrieving non-quotation data
        ///// </summary>
        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        public static AmiVar GetExtraData(string ticker, string name, int arraySize, Periodicity periodicity, Alloc alloc)
        {
            return new AmiVar();
        }

        /// <summary>
        /// GetSymbolLimit function is optional, used only by real-time plugins
        /// </summary>
        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        public static int GetSymbolLimit()
        {
            return 10000;
        }

        /// <summary>
        /// GetStatus function is optional, used mostly by few real-time plugins
        /// </summary>
        /// <param name="statusPtr">A pointer to <see cref="AmiBrokerPlugin.PluginStatus"/></param>
        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        public static void GetStatus(IntPtr statusPtr)
        {
            switch (Status)
            {
                case StatusCode.OK:
                    SetStatus(statusPtr, StatusCode.OK, Color.LightGreen, "OK", "CryptoCurrencies Data Plug-in is running...");
                    break;
                case StatusCode.Wait:
                    SetStatus(statusPtr, StatusCode.Wait, Color.LightBlue, "WAIT", "Retrieving data...");
                    break;
                case StatusCode.Error:
                    SetStatus(statusPtr, StatusCode.Error, Color.Red, "ERR", "An error occured");
                    break;
                case StatusCode.Update:
                    SetStatus(statusPtr, StatusCode.Update, Color.LightSeaGreen, "Update", "Plugin update available at http://amicoins.ru");
                    break;
                default:
                    SetStatus(statusPtr, StatusCode.Unknown, Color.LightGray, "Ukno", "Unknown status");
                    break;
            }
        }

        #region Helper Functions

        // Устанавливает полное имя криптовалютной пары
        static void UpdateFullName(IntPtr ptr, string fullName)
        {
            var si = (StockInfo)Marshal.PtrToStructure(ptr, typeof(StockInfo));

            if (fullName != null)
            {
                var enc = Encoding.GetEncoding("windows-1251");
                var bytes = enc.GetBytes(fullName);

                for (var i = 0; i < (bytes.Length > 127 ? 127 : bytes.Length); i++)
                {
                    #if (_WIN64)
                        Marshal.WriteByte(new IntPtr(ptr.ToInt64() + 144 + i), bytes[i]);
                    #else
                        Marshal.WriteByte(new IntPtr(ptr.ToInt32() + 144 + i), bytes[i]);
                    #endif
                }

                #if (_WIN64)
                    Marshal.WriteByte(new IntPtr(ptr.ToInt64() + 144 + bytes.Length), 0x0);
                #else
                    Marshal.WriteByte(new IntPtr(ptr.ToInt32() + 144 + bytes.Length), 0x0);
                #endif
            }
        }


        // Добавляет в маркет сток
        private static void addStock(string stockName, int marketIndex = 0, string fullName = "")
        {
            IntPtr stock;

            stock = AddStockNew(stockName);

            // index of market

            //Marshal.WriteInt32(new IntPtr(stock.ToInt32() + 476), marketIndex);
            Marshal.WriteInt64(new IntPtr(stock.ToInt64() + 476), marketIndex);          

            // Update fullName
            UpdateFullName(stock, fullName);
        }

        /// <summary>
        /// Configure function is called when user presses "Configure" button in File->Database Settings
        /// </summary>
        /// <param name="path">Path to AmiBroker database</param>
        /// <param name="site">A pointer to <see cref="AmiBrokerPlugin.InfoSite"/></param>
        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        public static int Configure(string path, IntPtr infoSitePtr)
        {
            Status = StatusCode.Wait;

            // 32 bit
            // GetStockQty = (GetStockQtyDelegate)Marshal.GetDelegateForFunctionPointer(new IntPtr(Marshal.ReadInt32(new IntPtr(infoSitePtr.ToInt32() + 4))), typeof(GetStockQtyDelegate));
            
            // 64 bit
            //GetStockQty = (GetStockQtyDelegate)Marshal.GetDelegateForFunctionPointer(new IntPtr(Marshal.ReadInt32(new IntPtr(infoSitePtr.ToInt32() + 8))), typeof(GetStockQtyDelegate));

            #if (_WIN64)
                // 64 bit
                SetCategoryName = (SetCategoryNameDelegate)Marshal.GetDelegateForFunctionPointer(new IntPtr(Marshal.ReadInt64(new IntPtr(infoSitePtr.ToInt64() + 24))), typeof(SetCategoryNameDelegate));
            #else
                // 32 bit
                SetCategoryName = (SetCategoryNameDelegate)Marshal.GetDelegateForFunctionPointer(new IntPtr(Marshal.ReadInt32(new IntPtr(infoSitePtr.ToInt32() + 12))), typeof(SetCategoryNameDelegate));
            #endif

            //GetCategoryName = (GetCategoryNameDelegate)Marshal.GetDelegateForFunctionPointer(new IntPtr(Marshal.ReadInt32(new IntPtr(infoSitePtr.ToInt32() + 30))), typeof(GetCategoryNameDelegate));
            //SetIndustrySector = (SetIndustrySectorDelegate)Marshal.GetDelegateForFunctionPointer(new IntPtr(Marshal.ReadInt32(new IntPtr(infoSitePtr.ToInt32() + 16))), typeof(SetIndustrySectorDelegate));
            //GetIndustrySector = (GetIndustrySectorDelegate)Marshal.GetDelegateForFunctionPointer(new IntPtr(Marshal.ReadInt32(new IntPtr(infoSitePtr.ToInt32() + 20))), typeof(GetIndustrySectorDelegate));

            #if (_WIN64)
                // 64 bit
                AddStockNew = (AddStockNewDelegate)Marshal.GetDelegateForFunctionPointer(new IntPtr(Marshal.ReadInt64(new IntPtr(infoSitePtr.ToInt64() + 56))), typeof(AddStockNewDelegate));
            #else
                // 32 bit
                AddStockNew = (AddStockNewDelegate)Marshal.GetDelegateForFunctionPointer(new IntPtr(Marshal.ReadInt32(new IntPtr(infoSitePtr.ToInt32() + 28))), typeof(AddStockNewDelegate));
            #endif

            SetCategoryName(0, 0, "Binance");


            List<SymbolInfo> Symbols = BinanceHelper.getAllPairs();
            
            // Данных нет
            if (Symbols.Count < 0)
            {
                MessageBox.Show("No configuration data!", "Error!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Status = StatusCode.OK;
                return 0;
            }

            foreach (SymbolInfo symbol in Symbols)
                addStock(symbol.pairName, 0, symbol.description);
 
            Status = StatusCode.OK;

            MessageBox.Show("Configure is completed!", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);

            return 1;
        }

        /// <summary>
        /// SetTimeBase function is called when user is changing base time interval in File->Database Settings
        /// </summary>
        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        public static int SetTimeBase(int periodicity)
        {
           // MessageBox.Show("SetTimeBase...");

            switch (periodicity)
            { 
                case (int)Periodicity.OneMinute:
                case (int)Periodicity.FiveMinutes:
                case (int)Periodicity.FifteenMinutes:
                case (int)Periodicity.OneHour:
                case (int)Periodicity.EndOfDay:
                case (int)Periodicity.OneSecond:        
                    return 1;            
            }

            return 0;
            
            //return periodicity >= (int)Periodicity.OneHour && periodicity <= (int)Periodicity.EndOfDay ? 1 : 0;
        }

        /// <summary>
        /// Notify AmiBroker that new streaming data arrived
        /// </summary>
        static void NotifyStreamingUpdate()
        {
                        
        }

        /// <summary>
        /// Update status of the plugin
        /// </summary>
        /// <param name="statusPtr">A pointer to <see cref="AmiBrokerPlugin.PluginStatus"/></param>
        static void SetStatus(IntPtr statusPtr, StatusCode code, Color color, string shortMessage, string fullMessage)
        {

            #if (_WIN64) 
                Marshal.WriteInt64(new IntPtr(statusPtr.ToInt64() + 4), (int)code);
                Marshal.WriteInt64(new IntPtr(statusPtr.ToInt64() + 8), color.R);
                Marshal.WriteInt64(new IntPtr(statusPtr.ToInt64() + 9), color.G);
                Marshal.WriteInt64(new IntPtr(statusPtr.ToInt64() + 10), color.B);
            #else
                Marshal.WriteInt32(new IntPtr(statusPtr.ToInt32() + 4), (int)code);
                Marshal.WriteInt32(new IntPtr(statusPtr.ToInt32() + 8), color.R);
                Marshal.WriteInt32(new IntPtr(statusPtr.ToInt32() + 9), color.G);
                Marshal.WriteInt32(new IntPtr(statusPtr.ToInt32() + 10), color.B);
            #endif


            var msg = encoding.GetBytes(fullMessage);

            for (int i = 0; i < (msg.Length > 255 ? 255 : msg.Length); i++)
            {
                #if (_WIN64)
                    Marshal.WriteInt64(new IntPtr(statusPtr.ToInt64() + 12 + i), msg[i]);
                #else
                    Marshal.WriteInt32(new IntPtr(statusPtr.ToInt32() + 12 + i), msg[i]);
                #endif 
            }

            #if (_WIN64)
                Marshal.WriteInt64(new IntPtr(statusPtr.ToInt64() + 12 + msg.Length), 0x0);
            #else
                 Marshal.WriteInt32(new IntPtr(statusPtr.ToInt32() + 12 + msg.Length), 0x0);
            #endif

            msg = encoding.GetBytes(shortMessage);

            for (int i = 0; i < (msg.Length > 31 ? 31 : msg.Length); i++)
            {
                #if (_WIN64)
                    Marshal.WriteInt64(new IntPtr(statusPtr.ToInt64() + 268 + i), msg[i]);
                #else
                    Marshal.WriteInt32(new IntPtr(statusPtr.ToInt32() + 268 + i), msg[i]);
                #endif

            }
      
            #if (_WIN64)
                Marshal.WriteInt64(new IntPtr(statusPtr.ToInt64() + 268 + msg.Length), 0x0);
            #else
                Marshal.WriteInt32(new IntPtr(statusPtr.ToInt32() + 268 + msg.Length), 0x0);
            #endif

        }

        #endregion

        #region AmiBroker Method Delegates
                        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, SetLastError = true)]
        delegate int GetStockQtyDelegate();

        private static GetStockQtyDelegate GetStockQty;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, SetLastError = true)]
        delegate int SetCategoryNameDelegate(int category, int item, string name);

        private static SetCategoryNameDelegate SetCategoryName;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, SetLastError = true)]
        delegate string GetCategoryNameDelegate(int category, int item);

        private static GetCategoryNameDelegate GetCategoryName;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, SetLastError = true)]
        delegate int SetIndustrySectorDelegate(int industry, int sector);

        private static SetIndustrySectorDelegate SetIndustrySector;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, SetLastError = true)]
        delegate int GetIndustrySectorDelegate(int industry);

        private static GetIndustrySectorDelegate GetIndustrySector;

        // Only available if called from AmiBroker 5.27 or higher
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, SetLastError = true)]
        public delegate IntPtr AddStockNewDelegate([MarshalAs(UnmanagedType.LPStr)] string ticker);

        private static AddStockNewDelegate AddStockNew;


        #endregion
    } 
}
