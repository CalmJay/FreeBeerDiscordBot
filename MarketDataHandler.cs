using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using DiscordbotLogging.Log;
using Newtonsoft.Json;

namespace MarketData
{
    public class MarketDataHandler
    {
        //public EquipmentPrices[] ItemMarketData { get; set; }
        public List<EquipmentMarketData> ItemMarketData { get; set; }
        public List<EquipmentMarketDataAveragePrice> ItemMarketData2 { get; set; }
        public List<EquipmentMarketDataMonthylyAverage> ItemMarketData3 { get; set; }
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

    //public class EquipmentMarketDataSingleItem
    //{
    //    public string item_id { get; set; }
    //    public string city { get; set; }
    //    public int quality { get; set; }
    //    public int sell_price_min { get; set; }
    //    public DateTime sell_price_min_date { get; set; }
    //    public int sell_price_max { get; set; }
    //    public DateTime sell_price_max_date { get; set; }
    //    public int buy_price_min { get; set; }
    //    public DateTime buy_price_min_date { get; set; }
    //    public int buy_price_max { get; set; }
    //    public DateTime buy_price_max_date { get; set; }
    //}

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
    public class EquipmentMarketDataMonthylyAverage
    {
        public List<DateTime> timestamps { get; set; }
        public List<double> prices_avg { get; set; }
        public List<int> item_count { get; set; }
    }

    public class DateTimeItems
    {
        public EquipmentMarketDataMonthylyAverage data { get; set; }
        public string location { get; set; }
        public string item_id { get; set; }
        public int quality { get; set; }
    }

    public class MarketDataFetching
    {
        public async Task<List<EquipmentMarketData>> GetMarketPriceCurrentAsync(string a_sEquipmentItem)
        {
            string jsonCurrentMarketData = null;
            List<EquipmentMarketData> marketDataCurrentPricing;
            //"T5_HEAD_PLATE_KEEPER@3?Locations=&qualities=2"
            try
            {

                using (HttpResponseMessage response = await AlbionOnlineDataParser.AlbionOnlineDataParser.ApiAlbionDataProjectCurrentPrices.GetAsync(a_sEquipmentItem))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        jsonCurrentMarketData = await response.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        throw new Exception(response.ReasonPhrase);
                    }
                }
                marketDataCurrentPricing = JsonConvert.DeserializeObject<List<EquipmentMarketData>>(jsonCurrentMarketData);
            }
            catch (Exception ex)
            {
                //await _logger.Log(new LogMessage(LogSeverity.Info, "Insult Time!!!", $"User: {context.User.Username}, Command: insult", null));
                throw;
            }

            return marketDataCurrentPricing;
        }

        public async Task<List<AverageItemPrice>> GetMarketPriceDailyAverage(string a_sEquipmentItem)
        {
            string jsonCurrentMarketData = null;
            List<AverageItemPrice> marketData = null;
            string debugAPICall = a_sEquipmentItem + "&time-scale=24";

            //example call for correct 24 hour avarage https://www.albion-online-data.com/api/v2/stats/history/T7_HEAD_PLATE_SET2?qualities=2&locations=&time-scale=24
            //T8_HEAD_CLOTH_SET2@1?Locations=&qualities=4&time-scale=24
            //T7_HEAD_PLATE_SET2?location=&qualities=2&time-scale=24
            try
            {

                using (HttpResponseMessage response = await AlbionOnlineDataParser.AlbionOnlineDataParser.ApiAlbionDataProjectDailyPrices.GetAsync(a_sEquipmentItem + "&time-scale=24"))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        jsonCurrentMarketData = await response.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        throw new Exception(response.ReasonPhrase);
                    }
                }
                marketData = JsonConvert.DeserializeObject<List<AverageItemPrice>>(jsonCurrentMarketData);

            }
            catch (Exception ex)
            {
                throw;
            }

            return marketData;
        }

        public async Task<List<EquipmentMarketDataMonthylyAverage>> GetMarketPriceMonthlyAverage(string a_sEquipmentItem)
        {
            string jsonCurrentMarketData = null;
            List<EquipmentMarketDataMonthylyAverage> marketData = null;
            //https://www.albion-online-data.com/api/v2/stats/charts/T8_HEAD_PLATE_SET2?date=11-1-2022&end_date=11-20-2022&locations=Martlock&qualities=4&time-scale=24
            //EXAMPLE TIME SCALE THE WORKS https://www.albion-online-data.com/api/v2/stats/charts/T4_MOUNT_HORSE?locations=&qualities=4&time-scale=24&date=11-1-2022&end_date=11-20-2022
            //broken down

            DateTime endDate = DateTime.Today.AddDays(-1);
            DateTime startDate = endDate.AddDays(-24);

            string startDateString = startDate.ToString("MM-dd-yyyy");
            var endDateString = endDate.ToString("MM-dd-yyyy");

            var combinedString = a_sEquipmentItem + $"&time-scale=24&date={startDateString}&end_date={endDateString}";

            using (HttpResponseMessage response = await AlbionOnlineDataParser.AlbionOnlineDataParser.ApiAlbionDataProjectMonthlyPrices.GetAsync(a_sEquipmentItem + $"&time-scale=24&date={startDateString}&end_date={endDateString}"))
            {
                if (response.IsSuccessStatusCode)
                {
                    jsonCurrentMarketData = await response.Content.ReadAsStringAsync();
                }
                else
                {
                    throw new Exception(response.ReasonPhrase);
                }
            }
            marketData = JsonConvert.DeserializeObject<List<EquipmentMarketDataMonthylyAverage>>(jsonCurrentMarketData);



            return marketData;
        }


        

        









    }

}
