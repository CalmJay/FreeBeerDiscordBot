using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;

namespace AlbionOnlineDataParser
{
    public static class AlbionOnlineDataParser
    {
        public static HttpClient ApiClient { get; set; } = new HttpClient();

        public enum AlbionAPIDataTypesEnum
        {
            search,
            playerSearch,
            playerDeaths,
            playerKills,
            playerStatistics,
            events

        }


        public static void InitializeClient()
        {
            ApiClient = new HttpClient();
            ApiClient.BaseAddress = new Uri("https://gameinfo.albiononline.com/api/gameinfo/");
            ApiClient.DefaultRequestHeaders.Accept.Clear();
            ApiClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }

    }
}
