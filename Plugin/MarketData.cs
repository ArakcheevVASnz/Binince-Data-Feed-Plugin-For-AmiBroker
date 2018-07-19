using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Threading;

namespace AmiBroker.Plugin
{
    public class RequestState
    {
        public HttpWebRequest request;
        public HttpWebResponse streamResponse;
        public Stream webStream;
        public StreamReader reader;
        public string result;

        public RequestState()
        {
            request = null;
            streamResponse = null;
            reader = null;
            webStream = null;
        }
    }


    class MarketData
    {
        private static string errorData = "";
        public static string data { get; set; }
        // 
        public static bool hasData = false;
        private static IntPtr mainWnd = IntPtr.Zero;

        static void GetBarsInfo(IAsyncResult result)
        {
            HttpWebResponse response = (result.AsyncState as HttpWebRequest).EndGetResponse(result) as HttpWebResponse;
            StreamReader reader = new StreamReader(response.GetResponseStream());
            data = reader.ReadToEnd();

            hasData = true;

            // Обновить данные
            NativeMethods.SendMessage(mainWnd, 0x400 + 13000, IntPtr.Zero, IntPtr.Zero);
            //Log.Write(">>>>>> AsyncData: " + reader.ReadToEnd());
        }

        public static void getMarketDataAsync(string baseURL, IntPtr mainWnd)
        {
            mainWnd = mainWnd;
            errorData = "";
            data = "";
            hasData = false;

            // Создать запрос
            HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(baseURL);

            try
            {
                IAsyncResult result = (IAsyncResult)httpRequest.BeginGetResponse(new AsyncCallback(GetBarsInfo), httpRequest);
            }
            catch (Exception e)
            {
                errorData = e.Message;
                return;
            }
        }

        public static string getMarketData(string baseURL)
        {
             // Пробуем запрос данных
            HttpWebRequest httpRequest;
            HttpWebResponse httpResponse;

            string result;

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
                Log.Write("GetMarketData Error: " + e.Message);

                errorData = e.Message;
                return null;
            }

            return result;
        }

        public static string getLastError()
        {
            return errorData;
        }
    }
}
