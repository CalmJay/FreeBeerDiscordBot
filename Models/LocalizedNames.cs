using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Models
{
    public class LocalizedNames
    {
        [JsonProperty("EN-US")]
        public string ENUS { get; set; }

        [JsonProperty("DE-DE")]
        public string DEDE { get; set; }

        [JsonProperty("FR-FR")]
        public string FRFR { get; set; }

        [JsonProperty("RU-RU")]
        public string RURU { get; set; }

        [JsonProperty("PL-PL")]
        public string PLPL { get; set; }

        [JsonProperty("ES-ES")]
        public string ESES { get; set; }

        [JsonProperty("PT-BR")]
        public string PTBR { get; set; }

        [JsonProperty("IT-IT")]
        public string ITIT { get; set; }

        [JsonProperty("ZH-CN")]
        public string ZHCN { get; set; }

        [JsonProperty("KO-KR")]
        public string KOKR { get; set; }

        [JsonProperty("JA-JP")]
        public string JAJP { get; set; }

        [JsonProperty("ZH-TW")]
        public string ZHTW { get; set; }

        [JsonProperty("ID-ID")]
        public string IDID { get; set; }
    }
}
