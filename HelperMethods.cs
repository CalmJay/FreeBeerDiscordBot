using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot
{
  public class HelperMethods
  {
    private static ulong OfficerRoleID = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("OfficerRoleID"));
    private static ulong VeteranRoleID = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("veteran"));
    private static ulong MemberRoleID = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("member"));
    private static ulong NewRecruitRoleID = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("newRecruit"));

    public static bool IsUserFreeBeerMember(SocketGuildUser a_SocketUser)
    {
      bool returnValue = false;

      if (a_SocketUser.Roles.Any(r => r.Id == OfficerRoleID || r.Id == VeteranRoleID || r.Id == MemberRoleID || r.Id == NewRecruitRoleID))
      {
        returnValue = true;
      }

      return returnValue;
    }


  }
}
