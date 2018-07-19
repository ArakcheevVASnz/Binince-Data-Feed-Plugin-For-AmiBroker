using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AmiBroker.Plugin
{
    class Market
    {
        public uint ID { get; set; }
        public string Name { get; set; }
        public IList<string> Pairs { get; set; }
        
        public string[] GetPairs() 
        {
            return Pairs.ToArray();
        }
    }

    class Markets
    {
        private static IList<Market> MarketList;

        //  Конструктор
        public Markets()
        { 
        
        }

        public static void addMarket(Market item)
        {
        
        }

        public static Market getMarket()
        {
            return new Market() { };
        }
    }
}
