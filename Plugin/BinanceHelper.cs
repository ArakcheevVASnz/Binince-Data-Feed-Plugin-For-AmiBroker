using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
// JSON
using Newtonsoft.Json;
using WebSocket4Net;

using AmiBroker.Plugin.Models;

namespace AmiBroker.Plugin
{
    class BinanceHelper
    {
        // Адрес, по которому запрашиваются все торгуемые пары
        public static string PAIRS_URL = "https://www.binance.com/exchange/public/product";
        //public static string PAIRS_URL = "http://kb2web/data.php";

        // Шаблон адреса получения котировок за последние 24 часа
        public static string LAST24_BARS = "https://www.binance.com/api/v1/klines?symbol={0}&interval={1}&limit=1000";
        //public static string LAST24_BARS = "http://kb2web/1min.php?{0}&{1}";

        // WebSocketStream
        public static string WSS_URL = "wss://stream.binance.com:9443/ws/{0}@kline_{1}";

        private static WebSocket wsSocket = null;

        public static EventHandler<MessageReceivedEventArgs> onWSSMessage;

        public static bool CreateWSS(string pairName, Periodicity period)
        {
            bool result = false;
            string url;

            // Закрыть
            if (wsSocket != null)
                wsSocket.Close();

            // Период
            url = String.Format(WSS_URL, pairName.ToLower(), "1m");               

            // Открываем сокет
            try
            {
                wsSocket = new WebSocket(url);
                wsSocket.MessageReceived += onMessage;
                wsSocket.Opened += wsSocket_Opened;
                wsSocket.Open();

                result = true;
            }
            catch (Exception e)
            {
                Log.Write("Can't create web socket stream! Error: " + e.Message);
                    return false;
            }

            return result;
        }

        private static void onMessage(object sender, MessageReceivedEventArgs e)
        {
            onWSSMessage(sender, e);
        }

        static void wsSocket_Opened(object sender, EventArgs e)
        {
            Log.Write("Socket connecting...");
        }



        // Получение котировок за последние 24 часа
        public static List<Ticker> getLast24hBars(string pairName)
        {
            List<Ticker> result = new List<Ticker>();

            // Строка запроса - данные за последние 24 часа в 1 мин интервале
            string url = String.Format(LAST24_BARS, pairName, "1m");
            // Получаем ответ
            string answer = getJSONData(url);

            //Log.Write(answer, "answer_" + pairName + ".log");
            // Массив данных
            List<ArrayList> decodedList = null;

            try
            {
                decodedList = JsonConvert.DeserializeObject<List<ArrayList>>(answer);
            }
            catch (Exception e)
            {
                Log.Write("Can't parse JSON data for last 24H bars!");
                return null;
            }

            if (decodedList.Count == 0)
            {
                Log.Write("Decooded JSON data for last 24H bars is empty!");
                return null;
            }

            int index = 0;

            foreach (ArrayList arr in decodedList)
            {
                Ticker ticker = new Ticker();

                // Формирование даты
                AmiDate time = new AmiDate(Utils.UnixTimeStampToDateTime(Convert.ToUInt64(arr[0])/1000));

                // Bar
                ticker.time = time.ToUInt64();
                ticker.open = float.Parse(arr[1].ToString().Replace(".", ","));
                ticker.high = float.Parse(arr[2].ToString().Replace(".", ","));
                ticker.low = float.Parse(arr[3].ToString().Replace(".", ","));
                ticker.close = float.Parse(arr[4].ToString().Replace(".", ","));
                ticker.volume = float.Parse(arr[5].ToString().Replace(".", ","));

                // Запись в массив
                result.Add(ticker);
            }

            // Возвращаем массив
            return result;
        }


        // Получение торгуемых символьных пар на Binance
        public static List<SymbolInfo> getAllPairs()
        {
            PairInfo pairList = null;            

            string answer = getJSONData(PAIRS_URL);

            try
            {
                pairList = JsonConvert.DeserializeObject<PairInfo>(answer);
            }
            catch (Exception e)
            {
                Log.Write("Can't parse JSON data! Message: " + e.Message);
                return null;
            }

            List<SymbolInfo> infoList = new List<SymbolInfo>();

            foreach(Pair item in pairList.data)
            {
                SymbolInfo info = new SymbolInfo();

                info.pairName = item.symbol;
                info.baseSymbol = item.quoteAssetName;
                info.quoteSymbol = item.quoteAssetName;
                info.description = item.baseAssetName + "/" + item.quoteAssetName + " at Binance Exchange";

                infoList.Add(info);
            }

            return infoList;
        }


        public static string getJSONData(string baseURL)
        {
            //Trust all certificates
            System.Net.ServicePointManager.ServerCertificateValidationCallback =
                ((sender, certificate, chain, sslPolicyErrors) => true);

            string result = "";

            // Пробуем запрос данных
            HttpWebRequest httpRequest;
            HttpWebResponse httpResponse;


            // Создать запрос
            httpRequest = (HttpWebRequest)WebRequest.Create(baseURL);

            try
            {
                httpResponse = (HttpWebResponse)httpRequest.GetResponse();

                // Ссылка на поток
                Stream webStream = httpResponse.GetResponseStream();
                StreamReader reader = new StreamReader(webStream);

                // Наш ответ с сервера
                result = reader.ReadToEnd();

                // Закрываем все
                reader.Close();
                webStream.Close();
                httpResponse.Close();
            }
            catch (Exception e)
            {
                Log.Write("GetJSONData Error: " + e.Message);
                return null;
            }

            return result;
        }

    }
}
