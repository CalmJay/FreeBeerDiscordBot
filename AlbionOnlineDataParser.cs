using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;

namespace AlbionOnlineDataParser
{
    public static class AlbionOnlineDataParser
    {
        public static HttpClient ApiClient { get; set; } = new HttpClient();
        public static HttpClient ApiAlbionDataProject { get; set; } = new HttpClient();

        public enum AlbionAPIDataTypesEnum
        {
            search,
            playerSearch,
            playerDeaths,
            playerKills,
            playerStatistics,
            events
        }

        public enum AlbionCitiesEnum
        {
            Thetford,
            FortSterling,
            Lymhurst,
            Bridgewatch,
            Martlock,
            Caerleon
        }

        public enum MarketEnum
        {
            buy,
            sell,
            prices,
            history
        }

        public static void InitializeClient()
        {
            ApiClient = new HttpClient();
            ApiClient.BaseAddress = new Uri("https://gameinfo.albiononline.com/api/gameinfo/");
            ApiClient.DefaultRequestHeaders.Accept.Clear();
            ApiClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }

        public static void InitializeAlbionDataProject()
        {

            ApiAlbionDataProject = new HttpClient();
            ApiAlbionDataProject.BaseAddress = new Uri("https://www.albion-online-data.com/api/v2/stats/prices/");
            ApiAlbionDataProject.DefaultRequestHeaders.Accept.Clear();
            ApiAlbionDataProject.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        }

    }
}
