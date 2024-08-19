using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DiscordBot
{
  public class HelperMethods
  {
    private static ulong OfficerRoleID = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("OfficerRoleID"));
    private static ulong VeteranRoleID = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("veteran"));
    private static ulong MemberRoleID = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("member"));
    private static ulong NewRecruitRoleID = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("newRecruit"));

    public class Settings
    {
      public LootSplitSettings lootsplitsettings { get; set; }
    }

    public class LootSplitSettings
    {
      public string name { get; set; }
      public int guildfee { get; set; }
      public int damagedfee { get; set; }
      public int nondamagedfee { get; set; }
      public bool includesilverbags { get; set; }

    }

    public static bool IsUserFreeBeerMember(SocketGuildUser a_SocketUser)
    {
      bool returnValue = false;

      if (a_SocketUser.Roles.Any(r => r.Id == OfficerRoleID || r.Id == VeteranRoleID || r.Id == MemberRoleID || r.Id == NewRecruitRoleID))
      {
        returnValue = true;
      }

      return returnValue;
    }

    public static string ReadFromJsonString()
    {
      var sAppPath = Environment.CurrentDirectory;
      string jsonString;
      using (StreamReader r = new StreamReader($"{sAppPath}\\customsettings.json"))
      {
        jsonString = r.ReadToEnd();
      }
      return jsonString;
    }

    public static void WriteToJson(string key, string value)
    {
      var sAppPath = Environment.CurrentDirectory;

      var json = File.ReadAllText($"{sAppPath}\\customsettings.json");
      JsonNode BotConfiguationSettings = JsonNode.Parse(json);


      BotConfiguationSettings["lootsplitsettings"]![key] = value;
      var options = new JsonSerializerOptions { WriteIndented = true };

      File.WriteAllText($"{sAppPath}\\customsettings.json", BotConfiguationSettings.ToString());
    }

    public static Settings ReadCustomSettingsFromJson()
    {
      var sAppPath = Environment.CurrentDirectory;

      Settings items = new Settings();
      using (StreamReader r = new StreamReader($"{sAppPath}\\customsettings.json"))
      {
        string jsonString = r.ReadToEnd();
        items = JsonConvert.DeserializeObject<Settings>(jsonString);
      }

      return items;
    }
  }
}
