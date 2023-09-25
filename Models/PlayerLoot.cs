using System;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace DiscordBot.Models
{
    public partial class PlayerLoot
    {
        public int Id { get; set; }
        public int TypeId { get; set; }
        public int PlayerId { get; set; }
        public decimal Loot { get; set; }
        public DateTime? CreateDate { get; set; }
        public string Reason { get; set; }
        public string PartyLeader { get; set; }
        public string KillId { get; set; }
        public string QueueId { get; set; }
        public string Message { get; set; }

        public virtual Player Player { get; set; }
        public virtual MoneyType Type { get; set; }
    }
}
