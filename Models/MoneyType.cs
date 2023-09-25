using System.Collections.Generic;

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
        public int Type { get; set; }

        public virtual ICollection<PlayerLoot> PlayerLoot { get; set; }
    }
}
