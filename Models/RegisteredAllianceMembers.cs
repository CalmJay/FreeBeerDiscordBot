using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Models
{
	public partial class RegisteredAllianceMembers
	{
		[Key]
		public string PlayerID { get; set; }
		public string PlayerName { get; set; }
		public string GuildID { get; set; }
		public string GuildName { get; set; }
		public string AllianceID { get; set; }
		public string AllianceName { get; set; }
		public DateTime? DateRegistered { get; set; }
		public int KillFame { get; set; }
		
			
	}
}
