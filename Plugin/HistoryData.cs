using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using AmiBroker.Plugin.Models;

/*
{
  "e": "kline",     // Event type
  "E": 123456789,   // Event time
  "s": "BNBBTC",    // Symbol
  "k": {
    "t": 123400000, // Kline start time
    "T": 123460000, // Kline close time
    "s": "BNBBTC",  // Symbol
    "i": "1m",      // Interval
    "f": 100,       // First trade ID
    "L": 200,       // Last trade ID
    "o": "0.0010",  // Open price
    "c": "0.0020",  // Close price
    "h": "0.0025",  // High price
    "l": "0.0015",  // Low price
    "v": "1000",    // Base asset volume
    "n": 100,       // Number of trades
    "x": false,     // Is this kline closed?
    "q": "1.0000",  // Quote asset volume
    "V": "500",     // Taker buy base asset volume
    "Q": "0.500",   // Taker buy quote asset volume
    "B": "123456"   // Ignore
  }
}
  
*/
namespace AmiBroker.Plugin
{

    public class k 
    {
        
        public ulong t { get; set; }
        public ulong T { get; set; }
        public string s { get; set; }
        public string i { get; set; }
        public int f { get; set; }
        public int F { get; set; }
        public string o { get; set; }
        public string c { get; set; }
        public string h { get; set; }
        public string l { get; set; }
        public string v { get; set; }
        public int n { get; set; }
        public bool x { get; set; }
        public string q { get; set; }
        public string V { get; set; }
        public string Q { get; set; }
        public string B { get; set; }
    }

    public class BinanceData
    {
        public string e { get; set; }
        public ulong E { get; set; }
        public string s { get; set; }
        public k k { get; set; }
    }

    /*
     
     {
          "e": "24hrTicker",  // Event type
          "E": 123456789,     // Event time
          "s": "BNBBTC",      // Symbol
          "p": "0.0015",      // Price change
          "P": "250.00",      // Price change percent
          "w": "0.0018",      // Weighted average price
          "x": "0.0009",      // Previous day's close price
          "c": "0.0025",      // Current day's close price
          "Q": "10",          // Close trade's quantity
          "b": "0.0024",      // Best bid price
          "B": "10",          // Best bid quantity
          "a": "0.0026",      // Best ask price
          "A": "100",         // Best ask quantity
          "o": "0.0010",      // Open price
          "h": "0.0025",      // High price
          "l": "0.0010",      // Low price
          "v": "10000",       // Total traded base asset volume
          "q": "18",          // Total traded quote asset volume
          "O": 0,             // Statistics open time
          "C": 86400000,      // Statistics close time
          "F": 0,             // First trade ID
          "L": 18150,         // Last trade Id
          "n": 18151          // Total number of trades
    } 
      
     */

    public class Individual
    {
        public string e { get; set; }
        public ulong E { get; set; }
        public string s { get; set; }
        public string p { get; set; }
        public string P { get; set; }
        public string w { get; set; }
        public string x { get; set; }
        public string c { get; set; }
        public string Q { get; set; }
        public string b { get; set; }
        public string B { get; set; }
        public string a { get; set; }
        public string A { get; set; }
        public string o { get; set; }
        public string h { get; set; }
        public string l { get; set; }
        public string v { get; set; }
        public string q { get; set; }
        public ulong O { get; set; }
        public ulong C { get; set; }
        public int F { get; set; }
        public int L { get; set; }
        public int n { get; set; }
    }


    #region Types
    class HistoryData
    {
        public string Response { get; set; }
        public string Message { get; set; }
        public short Type { get; set; }
        public bool Aggregated { get; set; }
        public IList<Ticker> Data { get; set; }
        public bool FirstValueInArray { get; set; }
        public ulong TimeTo { get; set; }
        public ulong TimeFrom { get; set; }

        

        public Quotation[] GetQuotes()
        {
            // TODO: Return the list of quotes for the specified ticker.

            List<Quotation> QList = new List<Quotation>();

            foreach (Ticker item in this.Data)
            {
                Quotation qt = new Quotation();

                qt.High = item.high;
                qt.Low = item.low;
                qt.Open = item.open;
                qt.Price = item.close;
                qt.Volume = item.volume; ///item.volumeto - item.volumefrom;

                // Date
                AmiDate tickerDate = new AmiDate(Utils.UnixTimeStampToDateTime(item.time));              
                qt.DateTime = tickerDate.ToUInt64();

                QList.Add(qt);
            }

            return QList.ToArray();
        }

    }
    #endregion
}
