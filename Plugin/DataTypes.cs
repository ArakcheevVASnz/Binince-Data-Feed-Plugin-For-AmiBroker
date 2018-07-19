using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AmiBroker.Plugin
{

    // Данные графика
    public class Ticker
    {
        public ulong time { get; set; }
        public float close { get; set; }
        public float high { get; set; }
        public float low { get; set; }
        public float open { get; set; }
        public float volume { get; set; }
    }

    class SymbolInfo
    {
        public string pairName { get; set; }
        public string baseSymbol { get; set; }
        public string quoteSymbol { get; set; }
        public string description { get; set; }
    }

    // Запрос пар идет в виде объекта в котором определен массив 
    // пар Pair
    public class PairInfo
    {
        public List<Pair> data { get; set; }
    }

    public class Pair
    {
        public string symbol { get; set; }
        public string quoteAssetName { get; set; }
        public float tradedMoney { get; set; }
        public string baseAssetUnit { get; set; }
        public string baseAssetName { get; set; }
        public string baseAsset { get; set; }
        public string tickSize { get; set; }
        public float prevClose { get; set; }
        public float activeBuy { get; set; }
        public string high { get; set; }
        public int lastAggTradeId { get; set; }
        public string low { get; set; }
        public string matchingUnitType { get; set; }
        public string close { get; set; }
        public string quoteAsset { get; set; }
        public string productType { get; set; }
        public bool active { get; set; }
        public float minTrade { get; set; }
        public float activeSell { get; set; }
        public string withdrawFee { get; set; }
        public string volume { get; set; }
        public int decimalPlaces { get; set; }
        public string quoteAssetUnit { get; set; }
        public string open { get; set; }
        public string status { get; set; }
        public string minQty { get; set; }

    }


}
