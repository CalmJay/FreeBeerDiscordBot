using System;
using System.ComponentModel.DataAnnotations;

namespace DiscordBot.Models
{
	public partial class RegisteredAllianceGuilds
	{
		[Key]
		public string GuildID { get; set; }
		public string GuildName { get; set; }
    public DateTime? DateRegistered { get; set; }
    public int KillFame { get; set; }
  }
}
