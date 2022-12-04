using System;
using System.Collections.Generic;
using System.Text;

namespace MarketData
{
    public class MarketDataHandler
    {
        //public EquipmentPrices[] ItemMarketData { get; set; }
        public List<EquipmentMarketData> ItemMarketData { get; set; }
    }

    public class EquipmentMarketData
    {
        public string item_id { get; set; }
        public string city { get; set; }
        public int quality { get; set; }
        public int sell_price_min { get; set; }
        public DateTime sell_price_min_date { get; set; }
        public int sell_price_max { get; set; }
        public DateTime sell_price_max_date { get; set; }
        public int buy_price_min { get; set; }
        public DateTime buy_price_min_date { get; set; }
        public int buy_price_max { get; set; }
        public DateTime buy_price_max_date { get; set; }
    }

    public class EquipmentMarketDataSingleItem
    {
        public string item_id { get; set; }
        public string city { get; set; }
        public int quality { get; set; }
        public int sell_price_min { get; set; }
        public DateTime sell_price_min_date { get; set; }
        public int sell_price_max { get; set; }
        public DateTime sell_price_max_date { get; set; }
        public int buy_price_min { get; set; }
        public DateTime buy_price_min_date { get; set; }
        public int buy_price_max { get; set; }
        public DateTime buy_price_max_date { get; set; }
    }

    //24 hour DAY AVERAGE
    public class EquipmentMarketDataAveragePrice
    {
        public int item_count { get; set; }
        public int avg_price { get; set; }
        public DateTime timestamp { get; set; }
    }

    public class AverageItemPrice
    {
        public string location { get; set; }
        public string item_id { get; set; }
        public int quality { get; set; }
        public List<EquipmentMarketDataAveragePrice> data { get; set; }
    }

    //DateTimeAverage
    // Root myDeserializedClass = JsonConvert.DeserializeObject<List<Root>>(myJsonResponse);
    public class DateTimeAverage
    {
        public List<DateTime> timestamps { get; set; }
        public List<double> prices_avg { get; set; }
        public List<int> item_count { get; set; }
    }

    public class DateTimeItems
    {
        public DateTimeAverage data { get; set; }
        public string location { get; set; }
        public string item_id { get; set; }
        public int quality { get; set; }
    }

    public class MarketDataFetching
    {
        public int GetMarketPriceCurrent()
        {
            return 0;
        }

        public int GetMarketPriceDailyAverage()
        {
            return 0;
        }

        public int GetMarketPrice24dayAverage()
        {
            return 0;
        }
    }

}
