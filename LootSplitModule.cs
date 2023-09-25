using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DiscordBot.Enums;


namespace DiscordBot.LootSplitModule
{
  public class LootSplitModule
  {
    private ulong ManagementRoleID = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("ManagementRoleID"));
    private ulong OfficerRoleID = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("OfficerRoleID"));
    public ulong roleIdNewRecruit = 847350505977675796;
    public ulong roleIdMember = 739948841847095387;
    public ulong roleIdOfficer = 335894631810334720;
    public ulong roleIdVeteran = 739950349405782046;
    public Dictionary<string, ulong> scrapedDict { get; set; }
    public List<string> scrapedList { get; set; }
    public int imageCount { get; set; }
    public string freeBeerDirectory { get; set; }
    public List<string> imageMembers { get; set; }
    public string submitter { get; set; }
    public string addedMembers { get; set; }
    public decimal lootAmountPer { get; set; }
    public int GuildSplitFee { get; set; }

    private int NonDamagedLootTotal { get; set; }
    private int DamagedLootTotal { get; set; }
    private int LootAmountPerMembernonDamged { get; set; }
    private int LootAmountPerMemberDamaged { get; set; }
    private int TotalLootSplitPerMember { get; set; }
    private int GrandTotalLootSplit { get; set; }
    private int SilverBagsTotal { get; set; }
    private bool MemberAddedOrRemoved { get; set; }
    private EmbedBuilder Embed { get; set; }
    private ComponentBuilder Componets { get; set; }

    public Dictionary<string, ulong> CreateMemberDict()
    {
      return scrapedDict;
    }
    public async Task CreateMemberList(SocketInteractionContext context)
    {
      //scrape members and write to Json
      List<string> memberList = new List<string>();

      //grab iterable and make list

      var iterable = context.Guild.GetUsersAsync().ToListAsync().Result.ToList();
      foreach (var member in iterable.FirstOrDefault())
      {
        if (member.RoleIds.Contains(roleIdNewRecruit) || member.RoleIds.Contains(roleIdMember)
            || member.RoleIds.Contains(roleIdOfficer) || member.RoleIds.Contains(roleIdVeteran))
        {
          //if no nickname, add the username
          if (member.DisplayName is null)
          { memberList.Add(member.Username); }
          //if squad leader, remove the dumbass prefix
          else if (member.DisplayName.StartsWith("!slnew"))
          { memberList.Add(member.DisplayName.Remove(0, 7)); }
          //if neither, just add the Nickname - NEED EVERYONE IN CHANNEL TO HAVE IGNs
          else if (member.DisplayName.StartsWith("!!sl"))
          { memberList.Add(member.DisplayName.Remove(0, 5)); }
          else if (member.DisplayName.StartsWith("!sl"))
          { memberList.Add(member.DisplayName.Remove(0, 4)); }
          else
          { memberList.Add(member.DisplayName); }
        }
      }

      scrapedList = memberList;

      //serialize and write
      string jsonstring = JsonConvert.SerializeObject(memberList);
      using (StreamWriter writer = System.IO.File.CreateText(".\\Files\\members.json"))
      {
        await writer.WriteAsync(jsonstring);
      }
    }
    public Dictionary<string, ulong> CreateMemberDict(SocketInteractionContext context)
    {
      Dictionary<string, ulong> dict = new Dictionary<string, ulong>();

      //grab iterable and make dict

      var iterable = context.Guild.GetUsersAsync().ToListAsync().Result.ToList();
      foreach (var member in iterable.FirstOrDefault())
      {
        if (member.RoleIds.Contains(roleIdNewRecruit) || member.RoleIds.Contains(roleIdMember)
            || member.RoleIds.Contains(roleIdOfficer) || member.RoleIds.Contains(roleIdVeteran))
        {
          if (member.DisplayName != null)
          {
            if (member.DisplayName.StartsWith("!!"))
            {
              string temp = member.DisplayName.Remove(0, 5);
              dict.Add(temp, member.Id);
            }
            else
            {
              dict.Add(member.DisplayName, member.Id);
            }
          }
          else if (member.DisplayName == null)
          {
            dict.Add(member.Username, member.Id);
          }
          else
          {
            continue;
          }
        }
        scrapedDict = dict;


      }
      return dict;
    }



    public async Task ConfirmationEmbed(SocketInteractionContext Context, List<string> a_MemberNames, LootSplitType a_LootSplitType, string a_callerName, EventTypeEnum a_eventType)
    {
      var channel = Context.Client.GetChannel(Context.Channel.Id) as IMessageChannel;
      //int playerPayout = CalculateSplit(a_SilverTotal, a_MemberNames.Count, a_LootSplitType);

      //begin embed builder
      var embed = new EmbedBuilder();
      embed.WithTitle($"Loot Split submission");
      embed.WithColor(Discord.Color.Orange);
      embed.AddField("Raw Silver Grand Total", (NonDamagedLootTotal + DamagedLootTotal + SilverBagsTotal).ToString("N0"), true);
      embed.AddField("Raw Non-Damaged Loot Total", NonDamagedLootTotal.ToString("N0"), true);
      embed.AddField("Raw Damaged Loot Total", DamagedLootTotal.ToString("N0"), true);
      embed.AddField("Silver Bags Total", SilverBagsTotal.ToString("N0"), true);
      embed.AddField("Payout per player (Calculated)", TotalLootSplitPerMember.ToString("N0"), true);

      if (a_LootSplitType == LootSplitType.Guild)
      {
        embed.AddField("Guild Split Fee:", GuildSplitFee.ToString("N0"));
      }
      else
      {
        embed.AddField("Guild Split Fee:", 0);
      }

      if (a_LootSplitType == LootSplitType.OffSeason)
      {
        embed.AddField("Member Count in split", $"{a_MemberNames.Count} + 1 @Guild Split");
      }
      else
      {
        embed.AddField("Member Count in split", a_MemberNames.Count);
      }
      embed.AddField("Event Type:", a_eventType.ToString(), true);
      embed.AddField("Split Type:", a_LootSplitType.ToString(), true);

      embed.AddField(x =>
            {
              //loop results and add members
              x.Name = "Members Included In Split";
              for (int i = 0; i < (a_MemberNames.Count - 1); i++)
              {
                x.Value += a_MemberNames[i] + ", ";
              }
              x.Value += a_MemberNames[a_MemberNames.Count - 1];
              x.IsInline = false;
            });
      embed.AddField("Party Leader:", a_callerName);

      var approveSplit = new ButtonBuilder()
      {
        Label = "Approve",
        CustomId = "approve-split",
        Style = ButtonStyle.Success
      };
      var denySplit = new ButtonBuilder()
      {
        Label = "Deny",
        CustomId = "deny-split",
        Style = ButtonStyle.Danger
      };
      var addMember = new ButtonBuilder()
      {
        Label = "Add member",
        CustomId = "add-member",
        Style = ButtonStyle.Secondary
      };
      var removeMember = new ButtonBuilder()
      {
        Label = "Remove member",
        CustomId = "remove-member",
        Style = ButtonStyle.Secondary
      };
      var comp = new ComponentBuilder();
      comp.WithButton(approveSplit);
      comp.WithButton(denySplit);
      comp.WithButton(addMember);
      comp.WithButton(removeMember);

      Embed = embed;
      Componets = comp;

      try
      {
        if (!MemberAddedOrRemoved)
        {
          await channel.SendMessageAsync(null, isTTS: false, embed.Build(), options: null, allowedMentions: null, messageReference: null, components: comp.Build(), stickers: null, embeds: null, flags: MessageFlags.None);

          //Reseting bool
          MemberAddedOrRemoved = false;
        }
        else
        {
          await Context.Interaction.ModifyOriginalResponseAsync((x) =>
          {
            x.Embed = Embed.Build();
            x.Components = Componets.Build();
          });
          //await Context.Interaction.FollowupAsync("Members updated");
        }

      }
      catch (Exception ex)
      {
        throw;
      }
    }

    public int CalculateSplit(int a_iLootSplitTotal, int a_iSplitMemberCount, LootSplitType a_LootSplitType)
    {
      switch (a_LootSplitType)
      {
        case LootSplitType.Personal:
        case LootSplitType.Other:
          return a_iLootSplitTotal / a_iSplitMemberCount;

        case LootSplitType.Guild:
          GuildSplitFee = (int)Math.Round(a_iLootSplitTotal * .20);

          var lootAmountPerMember = (int)Math.Round((a_iLootSplitTotal * .8) / a_iSplitMemberCount);

          return lootAmountPerMember;

        case LootSplitType.OffSeason:
          GuildSplitFee = a_iLootSplitTotal / (a_iSplitMemberCount + 1);

          return a_iLootSplitTotal / (a_iSplitMemberCount + 1);
      }

      return 0;
    }
    public async Task LootSplitInitialPrompt(SocketInteractionContext a_Context, List<string> a_MembersList, string a_sCallerName, LootSplitType a_eLootSplitType, EventTypeEnum a_eEventType, int? a_iNonDamagedLootTotal, int? a_iDamagedLootTotal, int? SilverBagsTotalstring)
    {

      try
      {
        NonDamagedLootTotal = (int)((a_iNonDamagedLootTotal != null) ? a_iNonDamagedLootTotal : 0);
        LootAmountPerMembernonDamged = (int)Math.Round(NonDamagedLootTotal * .8);

        DamagedLootTotal = (int)(a_iDamagedLootTotal != null ? a_iDamagedLootTotal : 0);
        LootAmountPerMemberDamaged = (int)Math.Round(DamagedLootTotal * .75);

        SilverBagsTotal = (int)((SilverBagsTotalstring != null) ? SilverBagsTotalstring : 0); ;
        //RawGrandTotal = a_iNonDamagedLootTotal +a_iDamagedLootTotal +SilverBagsTotalstring)
        switch (a_eLootSplitType)
        {
          case LootSplitType.Personal:
          case LootSplitType.Other:
            int LootTotals = 0;
            if (a_iNonDamagedLootTotal != null)
            {
              LootTotals += (int)a_iNonDamagedLootTotal;

            }
            if (a_iDamagedLootTotal != null)
            {
              LootTotals += (int)a_iDamagedLootTotal;

            }
            GuildSplitFee = 0;
            TotalLootSplitPerMember = (LootTotals + SilverBagsTotal) / a_MembersList.Count;
            break;

          case LootSplitType.Guild:
            GuildSplitFee = (int)Math.Round(NonDamagedLootTotal * .20) + (int)Math.Round(DamagedLootTotal * .25);
            TotalLootSplitPerMember = (LootAmountPerMembernonDamged + LootAmountPerMemberDamaged) / a_MembersList.Count;
            break;
          case LootSplitType.OffSeason:
            TotalLootSplitPerMember = (LootAmountPerMembernonDamged + LootAmountPerMemberDamaged + SilverBagsTotal) / (a_MembersList.Count + 1);
            GuildSplitFee = 0;

            break;
        }
        await ConfirmationEmbed(a_Context, a_MembersList, a_eLootSplitType, a_sCallerName, a_eEventType);


      }
      catch (Exception ex)
      {
        throw;
      }

    }


    public async Task BuildModalHandler(SocketInteractionContext context, Boolean bobbyBoole, List<string> membersList, List<string> imagesMembers)
    {

      //add silver bags text input
      var mb = new ModalBuilder()
      .WithTitle("Add member to split")
      .WithCustomId("split_info");
      mb.AddTextInput("Please enter additional members not captured", "add_members", placeholder: "e.g. Nezcoupe, Ragejay, etc. (case sensitive)", required: true, value: null);

      try
      {
        //send modal

        await context.Interaction.RespondWithModalAsync(mb.Build());

        context.Client.ModalSubmitted += async modal =>
        {
          List<SocketMessageComponentData> components = modal.Data.Components.ToList();

          string lootTotal = components.FirstOrDefault(x => x.CustomId == "loot_total").Value;

          await modal.DeferAsync();

          string membersStr = components.First(x => x.CustomId == "add_members").Value;
          List<string> membersSplit = membersStr.Split(',').ToList();

          //clean list of strings with space at the end
          for (int i = 1; i < membersSplit.Count; i++)
          {
            //cleanup strings after list separation
            if (i > 0)
            {
              membersSplit[i] = membersSplit[i].Remove(0, 1);
            }
            else
            {
              continue;
            }
          }

          foreach (string member in membersSplit)
          {
            if (membersList.Contains(member))
            {
              if (!(imagesMembers.Contains(member)))
              {
                imagesMembers.Add(member);
              }
              else
              {
                continue;
              }
            }
            else
            {
              await context.Channel.SendMessageAsync("***User " + member + " not found.***");
            }
          }

          //New resulting List of members complete, begin embed builder two
          //convert lootTotal to ulong, if an alpha char is present, send error
          try
          {
            ulong lootTotalInt = (ulong)Int64.Parse(lootTotal);

            //selection for which kind of split?
            //is this the correct split amount?
            decimal lootAmountPerMember = ((decimal)((lootTotalInt * .9) / imagesMembers.Count));

            lootAmountPer = lootAmountPerMember;

            var embed = new EmbedBuilder()
                    .WithTitle($"Loot split report generated by {context.User.Username}")
                    .WithColor(Discord.Color.Orange)
                    .AddField("Member Count", imagesMembers.Count)
                    .AddField(x =>
                    {
                      //loop results and add members
                  x.Name = "Members recorded";
                  for (int i = 0; i < (imagesMembers.Count - 1); i++)
                  {
                    x.Value += imagesMembers[i] + ", ";
                  }
                  x.Value += imagesMembers[imagesMembers.Count - 1];
                  x.IsInline = false;
                })
                    .AddField("Loot Split Total", lootTotalInt)
                    .AddField("Loot Split Per", lootAmountPerMember);
            //.AddField("Chest Location(s)", chestLoc);

            //send the embedded report
            await context.Channel.SendMessageAsync("--Loot Split Report--", false, embed.Build());

            await PostLootSplit(context);

            await context.Channel.SendMessageAsync("***please post relevant chest loot images below " +
                        "for evaluation. Once regear team verifies/denies I’ll send you a message with the outcome.*** ");
          }
          catch (Exception ex)
          {
            throw;
          }
          ////call LootSplitModule to verify/deny
          //await PostLootSplit(context);
        };
      }
      catch (Exception ex)
      {
        throw;
      }
    }
    public async Task PostLootSplit(SocketInteractionContext context)
    {
      var channel = context.Client.GetChannel(context.Channel.Id) as IMessageChannel;
      var approveSplit = new ButtonBuilder()
      {
        Label = "Approve",
        CustomId = "approve split",
        Style = ButtonStyle.Success
      };
      var denySplit = new ButtonBuilder()
      {
        Label = "Deny",
        CustomId = "deny split",
        Style = ButtonStyle.Danger
      };
      var comp = new ComponentBuilder();
      comp.WithButton(approveSplit);
      comp.WithButton(denySplit);

      try
      {
        await channel.SendMessageAsync(" ", isTTS: false, embed: null, options: null, allowedMentions: null, messageReference: null,
        components: comp.Build(), stickers: null, embeds: null, flags: MessageFlags.None);
      }
      catch (Exception ex)
      {
        throw;
      }
    }

    public async Task AddRemoveNamesFromList(SocketInteractionContext Context, Options OptionsEnum)
    {
      var interaction = Context.Interaction as IComponentInteraction;
      var socketThreadChannel = (SocketThreadChannel)Context.Channel;
      var usersActiveInThread = await socketThreadChannel.GetUsersAsync();
      List<string> membersList = interaction.Message.Embeds.FirstOrDefault().Fields[9].Value.Replace(" ", "").Split(',').ToList();

      List<string> missingMembersList = null;
      var threadOwner = socketThreadChannel.Owner.DisplayName;

      //Embed Values
      int RawNonDamgedLootTotal = Convert.ToInt32(interaction.Message.Embeds.FirstOrDefault().Fields[1].Value.Replace(",", ""));
      int RawDamagedLootTotal = Convert.ToInt32(interaction.Message.Embeds.FirstOrDefault().Fields[2].Value.Replace(",", ""));
      int iSilverbagsTotal = Convert.ToInt32(interaction.Message.Embeds.FirstOrDefault().Fields[3].Value.Replace(",", ""));
      string sPartyLeader = interaction.Message.Embeds.FirstOrDefault().Fields[10].Value;
      Enum.TryParse(interaction.Message.Embeds.FirstOrDefault().Fields[7].Value, out EventTypeEnum EventTypeEnum);
      Enum.TryParse(interaction.Message.Embeds.FirstOrDefault().Fields[8].Value, out LootSplitType LootSplitTypeEnum);

      MemberAddedOrRemoved = true;

      var guildUser = (SocketGuildUser)Context.User;

      if (guildUser.Roles.Any(r => r.Id == ManagementRoleID || r.Id == OfficerRoleID || r.Name == "admin") || socketThreadChannel.Owner.DisplayName.ToLower() == Context.User.Username.ToLower() || Context.Interaction.User.Username.ToLower() == sPartyLeader.ToLower())//Add check to allow the ownder of the model submit
      {

        var mb = new ModalBuilder()
        .WithTitle("Add/remove member")
        .WithCustomId("missing_members");
        mb.AddTextInput("Add or remove members for the split", "add_members", placeholder: "e.g. Nezcoupe, Ragejay, etc. (case sensitive)", required: false, value: null);

        try
        {
          //send modal
          await Context.Interaction.RespondWithModalAsync(mb.Build());

          Context.Client.ModalSubmitted += async modal =>
          {
            List<SocketMessageComponentData> components = modal.Data.Components.ToList();
            string sMissingMembers = components.FirstOrDefault().Value;
            await modal.DeferAsync();// BUTTONS ONLY WORK IN ONE GO. ERROR IS HERE

            if (sMissingMembers != "")
            {
              missingMembersList = components.FirstOrDefault().Value.Split(',').ToList();

              for (int i = 1; i < missingMembersList.Count; i++)
              {
                //cleanup strings after list separation
                if (i > 0)
                {
                  missingMembersList[i] = missingMembersList[i].Remove(0, 1);
                }
              }

              switch (OptionsEnum)
              {
                case Options.Add:
                  foreach (string member in missingMembersList)
                  {
                    if (!membersList.Contains(member))
                    {
                      membersList.Add(member);
                      await modal.FollowupAsync("Member added");
                    }
                    //else
                    //{
                    //	await Context.Channel.SendMessageAsync("***User " + member + " not found.***");
                    //}
                  }
                  break;

                case Options.Remove:
                  foreach (string member in missingMembersList)
                  {
                    if (membersList.Contains(member))
                    {

                      membersList.Remove(member);
                      await modal.FollowupAsync("Member removed");
                    }
                    //else
                    //{
                    //	await Context.Channel.SendMessageAsync("***User " + member + " not found.***");
                    //}
                  }
                  break;

                  
              }
              await LootSplitInitialPrompt(Context, membersList, sPartyLeader, LootSplitTypeEnum, EventTypeEnum, RawNonDamgedLootTotal, RawDamagedLootTotal, iSilverbagsTotal);

            }
          };
        }
        catch (Exception ex)
        {
          throw;
        }
      }
      else
      {
        await Context.Interaction.RespondAsync(":middle_finger: Only works for thread owner or party leader", null, false, true);
      }
    }
  }
}