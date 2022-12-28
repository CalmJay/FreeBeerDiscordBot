using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Models
{
    public class Item
    {
        public string LocalizationNameVariable { get; set; }
        public string LocalizationDescriptionVariable { get; set; }
        public LocalizedNames LocalizedNames { get; set; }
        public LocalizedDescriptions LocalizedDescriptions { get; set; }
        public string Index { get; set; }
        public string UniqueName { get; set; }

    }
}
