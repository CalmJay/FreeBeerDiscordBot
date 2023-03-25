using System;
using System.Net.Http;

namespace AlbionOnlineDataParser
{
    public static class AlbionOnlineDataParser
    {
        public static HttpClient ApiClient { get; set; } = new HttpClient();
        public static HttpClient ApiAlbionDataProjectMonthlyPrices { get; set; } = new HttpClient();
        public static HttpClient ApiAlbionDataProjectCurrentPrices { get; set; } = new HttpClient();
        public static HttpClient ApiAlbionDataProjectDailyPrices { get; set; } = new HttpClient();
        public static void InitializeAlbionAPIClient()
        {
            ApiClient = new HttpClient();
            ApiClient.BaseAddress = new Uri("https://gameinfo.albiononline.com/api/gameinfo/");
            ApiClient.DefaultRequestHeaders.Accept.Clear();
            ApiClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }
        public static void InitializeAlbionDataProjectCurrentPrices()
        {
            ApiAlbionDataProjectCurrentPrices = new HttpClient();
            ApiAlbionDataProjectCurrentPrices.BaseAddress = new Uri("https://www.albion-online-data.com/api/v2/stats/prices/");
            ApiAlbionDataProjectCurrentPrices.DefaultRequestHeaders.Accept.Clear();
            ApiAlbionDataProjectCurrentPrices.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }

        public static void InitializeAlbion24HourDataMarketPricesHistory()
        {
            //https://www.albion-online-data.com/api/v2/stats/history/T8_HEAD_PLATE_SET2?time-scale=24&locations=Martlock&qualities=2
            ApiAlbionDataProjectDailyPrices = new HttpClient();
            ApiAlbionDataProjectDailyPrices.BaseAddress = new Uri("https://www.albion-online-data.com/api/v2/stats/history/");
            ApiAlbionDataProjectDailyPrices.DefaultRequestHeaders.Accept.Clear();
            ApiAlbionDataProjectDailyPrices.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        }
        public static void InitializeAlbionData24DayAveragePrices()
        {
            ApiAlbionDataProjectMonthlyPrices = new HttpClient();
            ApiAlbionDataProjectMonthlyPrices.BaseAddress = new Uri("https://www.albion-online-data.com/api/v2/stats/prices/");
            ApiAlbionDataProjectMonthlyPrices.DefaultRequestHeaders.Accept.Clear();
            ApiAlbionDataProjectMonthlyPrices.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }
    }
}
