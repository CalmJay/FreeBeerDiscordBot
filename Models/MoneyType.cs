using DiscordBot.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace DiscordBot.Models
{
    public partial class MoneyType
    {
        public MoneyType()
        {
            PlayerLoot = new HashSet<PlayerLoot>();
        }

        public int Id { get; set; }
        //public string Type
        //{
        //    get
        //    {
        //        return this.Types.ToString();
        //    }
        //    set
        //    {
        //        Type = value;
        //    }
        //}
        [EnumDataType(typeof(MoneyTypes))]
        public MoneyTypes Type { get; set; }
        public virtual ICollection<PlayerLoot> PlayerLoot { get; set; }
    }
}
