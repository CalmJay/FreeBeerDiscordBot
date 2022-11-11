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

        public static void InitializeAlbionAPIClient()
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
