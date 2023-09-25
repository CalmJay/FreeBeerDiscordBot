using Discord.Interactions;
using Discord.WebSocket;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using PlayerData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using static DiscordBot.GuildDataHandler;
using Discord.Rest;

namespace DiscordBot
{
  public class GuildDataHandler
  {
    public class GuildInfo
    {
      public string Id { get; set; }
      public string Name { get; set; }
      public string FounderId { get; set; }
      public string FounderName { get; set; }
      public DateTime Founded { get; set; }
      public string AllianceTag { get; set; }
      public string AllianceId { get; set; }
      public object AllianceName { get; set; }
      public object Logo { get; set; }
      public long killFame { get; set; }
      public long DeathFame { get; set; }
      public object AttacksWon { get; set; }
      public object DefensesWon { get; set; }
      public int MemberCount { get; set; }
    }

    public class SearchInfo
    {
      public List<GuildDetails> guilds { get; set; }
      public List<Player> players { get; set; }
    }

    public class GuildDetails
    {
      public string Id { get; set; }
      public string Name { get; set; }
      public string AllianceId { get; set; }
      public string AllianceName { get; set; }
      public object KillFame { get; set; }
      public long DeathFame { get; set; }
    }

    public class Player
    {
      public string Id { get; set; }
      public string Name { get; set; }
      public string GuildId { get; set; }
      public object GuildName { get; set; }
      public string AllianceId { get; set; }
      public string AllianceName { get; set; }
      public string Avatar { get; set; }
      public string AvatarRing { get; set; }
      public long KillFame { get; set; }
      public long DeathFame { get; set; }
      public float FameRatio { get; set; }
      public object totalKills { get; set; }
      public object gvgKills { get; set; }
      public object gvgWon { get; set; }
    }



    public class Rootobject
    {
      public Class1[] Property1 { get; set; }
    }

    public class Class1
    {
      public int AverageItemPower { get; set; }
      public Equipment Equipment { get; set; }
      public object[] Inventory { get; set; }
      public string Name { get; set; }
      public string Id { get; set; }
      public string GuildName { get; set; }
      public string GuildId { get; set; }
      public string AllianceName { get; set; }
      public string AllianceId { get; set; }
      public string AllianceTag { get; set; }
      public string Avatar { get; set; }
      public string AvatarRing { get; set; }
      public int DeathFame { get; set; }
      public int KillFame { get; set; }
      public float FameRatio { get; set; }
      public Lifetimestatistics LifetimeStatistics { get; set; }
    }

    public class Equipment
    {
      public object MainHand { get; set; }
      public object OffHand { get; set; }
      public object Head { get; set; }
      public object Armor { get; set; }
      public object Shoes { get; set; }
      public object Bag { get; set; }
      public object Cape { get; set; }
      public object Mount { get; set; }
      public object Potion { get; set; }
      public object Food { get; set; }
    }

    public class Lifetimestatistics
    {
      public Pve PvE { get; set; }
      public Gathering Gathering { get; set; }
      public Crafting Crafting { get; set; }
      public int CrystalLeague { get; set; }
      public int FishingFame { get; set; }
      public int FarmingFame { get; set; }
      public DateTime Timestamp { get; set; }
    }

    public class Pve
    {
      public long Total { get; set; }
      public int Royal { get; set; }
      public int Outlands { get; set; }
      public int Avalon { get; set; }
      public int Hellgate { get; set; }
      public int CorruptedDungeon { get; set; }
      public int Mists { get; set; }
    }

    public class Gathering
    {
      public Fiber Fiber { get; set; }
      public Hide Hide { get; set; }
      public Ore Ore { get; set; }
      public Rock Rock { get; set; }
      public Wood Wood { get; set; }
      public All All { get; set; }
    }

    public class Fiber
    {
      public int Total { get; set; }
      public int Royal { get; set; }
      public int Outlands { get; set; }
      public int Avalon { get; set; }
    }

    public class Hide
    {
      public int Total { get; set; }
      public int Royal { get; set; }
      public int Outlands { get; set; }
      public int Avalon { get; set; }
    }

    public class Ore
    {
      public int Total { get; set; }
      public int Royal { get; set; }
      public int Outlands { get; set; }
      public int Avalon { get; set; }
    }

    public class Rock
    {
      public int Total { get; set; }
      public int Royal { get; set; }
      public int Outlands { get; set; }
      public int Avalon { get; set; }
    }

    public class Wood
    {
      public int Total { get; set; }
      public int Royal { get; set; }
      public int Outlands { get; set; }
      public int Avalon { get; set; }
    }

    public class All
    {
      public int Total { get; set; }
      public int Royal { get; set; }
      public int Outlands { get; set; }
      public int Avalon { get; set; }
    }

    public class Crafting
    {
      public int Total { get; set; }
      public int Royal { get; set; }
      public int Outlands { get; set; }
      public int Avalon { get; set; }
    }

    public async Task<GuildInfo> GetGuildSearchInfo(SocketInteractionContext a_socketInteraction, string a_searchName)
    {
      SearchInfo sSearchInfoDetails = null;
      string? sSearchInfo = null;
      GuildInfo returnValue = null;
      string? sGuildSearchID = null;

      using (HttpResponseMessage response = await AlbionOnlineDataParser.AlbionOnlineDataParser.ApiClient.GetAsync($"search?q={a_searchName}"))
      {
        if (response.IsSuccessStatusCode)
        {
          sSearchInfo = await response.Content.ReadAsStringAsync();
          var parsedObjects = JObject.Parse(sSearchInfo);
          SearchInfo searchInfo = JsonConvert.DeserializeObject<SearchInfo>(sSearchInfo);

          if (searchInfo.guilds.Count() > 0)
          {
            new SearchInfo
            {
              guilds = searchInfo.guilds,
              players = searchInfo.players
            };

            foreach (var guild in searchInfo.guilds)
            {
              if (guild.Name.ToLower() == a_searchName.ToLower())
              {
                sGuildSearchID = guild.Id;
                returnValue = await GetAlbionGuildInfo(sGuildSearchID);
                //using (HttpResponseMessage guildResponse = await AlbionOnlineDataParser.AlbionOnlineDataParser.ApiClient.GetAsync($"guilds/{sGuildSearchID}"))
                //{
                //  if (response.IsSuccessStatusCode)
                //  {
                //    sSearchInfo = await guildResponse.Content.ReadAsStringAsync();
                //    parsedObjects = JObject.Parse(sSearchInfo);

                //    GuildInfo guildInfo = JsonConvert.DeserializeObject<GuildInfo>(sSearchInfo);

                //    new GuildInfo
                //    {
                //      Id = guildInfo.Id,
                //      Name = guildInfo.Name,
                //      FounderId = guildInfo.FounderId,
                //      FounderName = guildInfo.FounderName,
                //      Founded = guildInfo.Founded,
                //      AllianceTag = guildInfo.AllianceTag,
                //      AllianceId = guildInfo.AllianceId,
                //      AllianceName = guildInfo.AllianceName,
                //      Logo = guildInfo.Logo,
                //      killFame = guildInfo.killFame,
                //      DeathFame = guildInfo.DeathFame,
                //      AttacksWon = guildInfo.AttacksWon,
                //      DefensesWon = guildInfo.DefensesWon,
                //      MemberCount = guildInfo.MemberCount
                //    };
                //    returnValue = guildInfo;
                //  }
                //}
              }
            };
          }
          else
          {

            Console.WriteLine($"Guild not found in Albion API. Discord user: {a_socketInteraction.User.Username}");
            await a_socketInteraction.Interaction.FollowupAsync($"Guild not found in Albion API. Please verify guild name.", ephemeral: true);
            returnValue = null;
          }
        }
        else
        {
          throw new Exception(response.ReasonPhrase);
        }
      }
      return returnValue;
    }

    public async Task<GuildInfo> GetAlbionGuildInfo(string a_guildID)
    {
      string? sSearchInfo = null;
      GuildInfo returnValue = null;

      using (HttpResponseMessage response = await AlbionOnlineDataParser.AlbionOnlineDataParser.ApiClient.GetAsync($"guilds/{a_guildID}"))
      {
        var parsedObjects = JObject.Parse(sSearchInfo);

        if (response.IsSuccessStatusCode)
        {
          sSearchInfo = await response.Content.ReadAsStringAsync();
          parsedObjects = JObject.Parse(sSearchInfo);

          GuildInfo guildInfo = JsonConvert.DeserializeObject<GuildInfo>(sSearchInfo);

          new GuildInfo
          {
            Id = guildInfo.Id,
            Name = guildInfo.Name,
            FounderId = guildInfo.FounderId,
            FounderName = guildInfo.FounderName,
            Founded = guildInfo.Founded,
            AllianceTag = guildInfo.AllianceTag,
            AllianceId = guildInfo.AllianceId,
            AllianceName = guildInfo.AllianceName,
            Logo = guildInfo.Logo,
            killFame = guildInfo.killFame,
            DeathFame = guildInfo.DeathFame,
            AttacksWon = guildInfo.AttacksWon,
            DefensesWon = guildInfo.DefensesWon,
            MemberCount = guildInfo.MemberCount
          };
          returnValue = guildInfo;
        }
      }

      return returnValue;
    }

    public async Task<PlayerDataHandler.Rootobject> GetAlbionSearchGuildInfo(int a_guildName)
    {
      string playerData = null;
      PlayerDataHandler.Rootobject eventData = null;
      using (HttpResponseMessage response = await AlbionOnlineDataParser.AlbionOnlineDataParser.ApiClient.GetAsync($"search?q=/{a_guildName}"))
      {
        if (response.IsSuccessStatusCode)
        {
          playerData = await response.Content.ReadAsStringAsync();
          eventData = JsonConvert.DeserializeObject<PlayerDataHandler.Rootobject>(playerData);
        }
      }

      return eventData;
    }

    public async Task<PlayerDataHandler.Rootobject> GetAlbionLookUpGuildInfo(string a_guildID)
    {
      string playerData = null;
      PlayerDataHandler.Rootobject eventData = null;
      using (HttpResponseMessage response = await AlbionOnlineDataParser.AlbionOnlineDataParser.ApiClient.GetAsync($"guilds/{a_guildID}/members"))
      {
        if (response.IsSuccessStatusCode)
        {
          playerData = await response.Content.ReadAsStringAsync();
          eventData = JsonConvert.DeserializeObject<PlayerDataHandler.Rootobject>(playerData);
        }
      }

      return eventData;
    }
  }
}
