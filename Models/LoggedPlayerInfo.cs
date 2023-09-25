using System;
using System.ComponentModel.DataAnnotations;

// Code scaffolded by EF Core assumes nullable reference types (NRTs) are not used or disabled.
// If you have enabled NRTs for your project, then un-comment the following line:
// #nullable disable

namespace DiscordBot.Models
{
  public partial class LoggedPlayerInfo
  {
    //public GuildPlayerInfo()
    //{
    //  PlayerInfo = new HashSet<Player>();
    //}
    [Key]
    public int TransactionID { get; set; }
    public string PlayerId { get; set; }
    public string PlayerName { get; set; }
    public string GuildID { get; set; }
    public long DeathFame { get; set; }
    public long KillFame { get; set; }
    public float FameRatio { get; set; }
    public long PVEFame { get; set; }
    public long GatheringFame { get; set; }
    public long CraftingFame { get; set; }
    public DateTime RecordedDate { get; set; }

    //public virtual ICollection<Player> PlayerInfo { get; set; }
  }
}



