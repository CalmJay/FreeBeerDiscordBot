using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordBot.Enums;
using DiscordBot.Models;
using DiscordBot.Services;
using DiscordbotLogging.Log;
using GoogleSheetsData;
using Newtonsoft.Json.Linq;
using PlayerData;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DiscordBot.RegearModule;
using MarketData;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using DiscordBot.LootSplitModule;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using Discord.Commands;
using Microsoft.VisualBasic;
using Aspose.Imaging.FileFormats.Emf.EmfPlus.Consts;
using Aspose.Words.Fields;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using System.Data;

namespace CommandModule
{
    public class CommandModule : InteractionModuleBase<SocketInteractionContext>
    {
        public InteractionService Commands { get; set; }
        private PlayerDataHandler.Rootobject PlayerEventData { get; set; }

        private static Logger _logger;
        private DataBaseService dataBaseService;
        private static LootSplitModule lootSplitModule;

        public CommandModule(ConsoleLogger logger)
        {
            _logger = logger;
        }

        [SlashCommand("get-player-info", "Search for Player Info")]
        public async Task GetBasicPlayerInfo(string a_sPlayerName)
        {
            PlayerLookupInfo playerInfo = new PlayerLookupInfo();
            PlayerDataLookUps albionData = new PlayerDataLookUps();

            playerInfo = await albionData.GetPlayerInfo(Context, a_sPlayerName);

            try
            {
                var embed = new EmbedBuilder()
                .WithTitle($"Player Search Results")
                .AddField("Player Name", (playerInfo.Name == null) ? "No info" : playerInfo.Name, true)
                .AddField("Kill Fame", (playerInfo.KillFame == 0) ? 0 : playerInfo.KillFame)
                .AddField("Death Fame: ", (playerInfo.DeathFame == 0) ? 0 : playerInfo.DeathFame, true)
                .AddField("Guild Name ", (playerInfo.GuildName == null || playerInfo.GuildName == "") ? "No info" : playerInfo.GuildName, true)
                .AddField("Alliance Name", (playerInfo.AllianceName == null || playerInfo.AllianceName == "") ? "No info" : playerInfo.AllianceName, true);

                await RespondAsync(null, null, false, true, null, null, null, embed.Build());
            }
            catch (Exception ex)
            {
                await RespondAsync("Player info not found");
            }
        }

        [SlashCommand("register", "Register player to Free Beer guild")]
        public async Task Register(SocketGuildUser guildUserName, string ingameName)
        {
            PlayerDataHandler playerDataHandler = new PlayerDataHandler();
            PlayerLookupInfo playerInfo = new PlayerLookupInfo();
            PlayerDataLookUps albionData = new PlayerDataLookUps();
            dataBaseService = new DataBaseService();

            string? sUserNickname = (guildUserName.Nickname == null) ? guildUserName.Username : guildUserName.Nickname;

            var freeBeerMainChannel = Context.Client.GetChannel(739949855195267174) as IMessageChannel;
            var newMemberRole = guildUserName.Guild.GetRole(847350505977675796);//new member role id
            var freeRegearRole = guildUserName.Guild.GetRole(1052241667329118349);//new member role id

            var user = guildUserName.Guild.GetUser(guildUserName.Id);

            if (ingameName != null || sUserNickname != ingameName)
            {

                if (sUserNickname != ingameName)
                {
                    sUserNickname = ingameName;
                    await guildUserName.ModifyAsync(x => x.Nickname = ingameName);
                }

            }

            playerInfo = await albionData.GetPlayerInfo(Context, sUserNickname);

            if (sUserNickname.ToLower() == playerInfo.Name.ToLower())
            {
                await dataBaseService.AddPlayerInfo(new Player
                {
                    PlayerId = playerInfo.Id,
                    PlayerName = playerInfo.Name
                });

                await user.AddRoleAsync(newMemberRole);
                await user.AddRoleAsync(freeRegearRole);

                await _logger.Log(new LogMessage(LogSeverity.Info, "Register Member", $"User: {Context.User.Username} has registered {playerInfo.Name}, Command: register", null));

                await GoogleSheetsDataWriter.RegisterUserToDataRoster(playerInfo.Name.ToString(), ingameName, null, null, null);
                await GoogleSheetsDataWriter.RegisterUserToRegearSheets(guildUserName, ingameName, null, null, null);

                var embed = new EmbedBuilder()
               .WithTitle($":beers: WELCOME TO FREE BEER :beers:")
               .WithDescription("We're glad to have you. Please checkout the following below.")
               .AddField($"Don't get kicked", "<#995798935631302667>")
               .AddField($"General info / location of the guild stuff", "<#880598854947454996>")
               .AddField($"Regear program", "<#970081185176891412>")
               .AddField($"ZVZ builds", "<#906375085449945131>")
               .AddField($"Before you do ANYTHING else", "Your existence in the guild relies you on reading these");

                await freeBeerMainChannel.SendMessageAsync($"<@{Context.Guild.GetUser(guildUserName.Id).Id}>", false, embed.Build());

                List<string> insultList = new List<string>
                {
                    $"food",
                    $"weapon in Albion",
                    $"drink",
                    $"hobby",
                    $"car",
                };
                Random rnd = new Random();
                int r = rnd.Next(insultList.Count);
                await freeBeerMainChannel.SendMessageAsync($"<@{Context.Guild.GetUser(guildUserName.Id).Id}> We want to get to know you! Tell us... What's your favorite {(string)insultList[r]}?");
            }
            else
            {
                await ReplyAsync($"The discord name doen't match the ingame name. {playerInfo.Name}");
            }
        }

        [SlashCommand("recent-deaths", "View recent deaths")]
        public async Task GetRecentDeaths()
        {
            var testuser = Context.User.Id;
            string? sPlayerData = null;
            var sPlayerAlbionId = new PlayerDataLookUps().GetPlayerInfo(Context, null);
            string? sUserNickname = ((Context.Interaction.User as SocketGuildUser).Nickname != null) ? (Context.Interaction.User as SocketGuildUser).Nickname : Context.Interaction.User.Username;

            int iDeathDisplayCounter = 1;
            int iVisibleDeathsShown = Int32.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("showDeathsQuantity")) - 1;  //can add up to 10 deaths. Adjust in config

            if (sPlayerAlbionId.Result != null)
            {
                using (HttpResponseMessage response = await AlbionOnlineDataParser.AlbionOnlineDataParser.ApiClient.GetAsync($"players/{sPlayerAlbionId.Result.Id}/deaths"))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        sPlayerData = await response.Content.ReadAsStringAsync();
                        var parsedObjects = JArray.Parse(sPlayerData);
                        var component = new ComponentBuilder();
                        var searchDeaths = parsedObjects.Children<JObject>()
                        .Select(jo => (int)jo["EventId"])
                        .ToList();

                        var embed = new EmbedBuilder()
                        .WithTitle("Recent Deaths")
                        .WithColor(new Color(238, 62, 75));


                        var regearbutton = new ButtonBuilder()
                        {
                            Style = ButtonStyle.Secondary
                        };

                        
                        for (int i = 0; i < searchDeaths.Count; i++)
                        {
                            if (i <= iVisibleDeathsShown)
                            {
                                embed.AddField($"Death {iDeathDisplayCounter} : KILL ID - {searchDeaths[i]}", $"https://albiononline.com/en/killboard/kill/{searchDeaths[i]}", false);

                                regearbutton.Label = $"Regear Death {iDeathDisplayCounter}"; //QOL Update. Allows members to start the regear process straight from the recent deaths list
                                regearbutton.CustomId = searchDeaths[i].ToString();
                                component.WithButton(regearbutton);

                                iDeathDisplayCounter++;
                            }
                        }
                        //await RespondAsync(null, null, false, true, null, null, component.Build(), embed.Build()); //Enable once buttons are working
                        await RespondAsync(null, null, false, true, null, null, null, embed.Build());
                    }
                    else
                    {
                        throw new Exception(response.ReasonPhrase);
                    }
                }
            }
            else
            {
                await RespondAsync("Hey idiot. Does your discord nickname match your in-game name?");
            }
        }

        [SlashCommand("fetchprice", "Testing market item finder")]
        public async Task FetchMarketPrice(int PriceOption, string a_sItemType, int a_iQuality, string a_sMarketLocation = "")
        {
            //Task<List<EquipmentMarketData>> marketData = new MarketDataFetching().GetMarketPrice24dayAverage(a_sItemType);
            Task<List<EquipmentMarketData>> marketData = null;
            string combinedInfo = "";
            //1 = 24 day average
            //2 = daily average
            //3 = current price
            //
            int itemCost = 0;
            await _logger.Log(new LogMessage(LogSeverity.Info, "Price Check", $"User: {Context.User.Username}, Command: Price check", null));

            switch (PriceOption)
            {
                case 1:
                    combinedInfo = $"{a_sItemType}?qualities={a_iQuality}&locations={a_sMarketLocation}";
                    marketData = new MarketDataFetching().GetMarketPriceCurrentAsync(combinedInfo);
                    break;

                case 2:

                    combinedInfo = $"{a_sItemType}?qualities={a_iQuality}&locations={a_sMarketLocation}";
                    marketData = new MarketDataFetching().GetMarketPriceCurrentAsync(combinedInfo);

                    break;

                case 3:
                    combinedInfo = $"{a_sItemType}?qualities={a_iQuality}&locations={a_sMarketLocation}";
                    marketData = new MarketDataFetching().GetMarketPriceCurrentAsync(combinedInfo);

                    break;

                default:
                    await ReplyAsync($"<@{Context.User.Id}> Option doesn't exist");
                    break;
            }

            if (marketData.Result != null)
            {
                if (marketData.Result.Count > 0 && marketData.Result.FirstOrDefault().sell_price_min > 0)
                {
                    await RespondAsync($"<@{Context.User.Id}> Price for {a_sItemType} is: " + marketData.Result.FirstOrDefault().sell_price_min, null, false, true);
                }
                else
                {
                    await RespondAsync($"<@{Context.User.Id}> Price not found", null, false, true);
                }
            }
            else
            {
                await RespondAsync($"<@{Context.User.Id}> Price not found", null, false, true);
            }
        }

        [SlashCommand("blacklist", "Put a player on the shit list")]
        public async Task BlacklistPlayer(SocketGuildUser a_DiscordUsername, string IngameName = null, string Reason = null, string Fine = null, string AdditionalNotes = null)
        {
            await DeferAsync();

            string? sDiscordNickname = IngameName;
            string? AlbionInGameName = IngameName;
            string? sReason = Reason;
            string? sFine = Fine;
            string? sNotes = AdditionalNotes;

            Console.WriteLine("Dickhead " + a_DiscordUsername + " has been blacklisted");

            await GoogleSheetsDataWriter.WriteToFreeBeerRosterDatabase(a_DiscordUsername.ToString(), sDiscordNickname, sReason, sFine, sNotes);
            await FollowupAsync(a_DiscordUsername.ToString() + " has been blacklisted <:kekw:816748015372861512> ", null, false, false);
        }

        [SlashCommand("view-paychex", "Views your current paychex amount")]
        public async Task GetCurrentPaychexAmount()
        {
            
            await DeferAsync(true);
            await _logger.Log(new LogMessage(LogSeverity.Info, "View-Paychex", $"User: {Context.User.Username}, Command: view-paychex", null));
            string? sUserNickname = ((Context.User as SocketGuildUser).Nickname != null) ? (Context.User as SocketGuildUser).Nickname : Context.User.Username;
            if (sUserNickname.Contains("!sl"))
            {
                sUserNickname = new PlayerDataLookUps().CleanUpShotCallerName(sUserNickname);
            }

            if (GoogleSheetsDataWriter.GetRegisteredUser(sUserNickname))
            {
                List<string> paychexRunningTotal = GoogleSheetsDataWriter.GetRunningPaychexTotal(sUserNickname);
                string miniMarketCreditsTotal = GoogleSheetsDataWriter.GetMiniMarketCredits(sUserNickname);

                if (paychexRunningTotal.Count > 0)
                {
                    var embed = new EmbedBuilder()
                    .WithTitle($":moneybag: Your Free Beer Paychex Info :moneybag: ")
                    .AddField("Last weeks Paychex total", $"${paychexRunningTotal[0]:n0}")
                    .AddField("Current week running total:", $"${paychexRunningTotal[1]:n0}")
                    .AddField("Mini-mart Credits balance:", $"{miniMarketCreditsTotal:n0}");

                    await FollowupAsync(null, null, false, true, null, null, null, embed.Build());
                }
                else
                {
                    await RespondAsync("Sorry you don't seem to be registed. Ask for a @AO - REGEARS officer to get you squared away.", null, false, true);
                }
            }
            else
            {
                await FollowupAsync("Looks like your not fully registered to the guild. Please contact a recruiter or officer to fix the issue.", null, false, true);
            }
        }

        [SlashCommand("render-paychex", "If you don't know what this means at this point don't use it")]
        public async Task RenderPaychex()
        {
            await DeferAsync();
            await _logger.Log(new LogMessage(LogSeverity.Info, "Render Paychex", $"User: {Context.User.Username}, Command: render-paychex", null));
            await GoogleSheetsDataWriter.RenderPaychex(Context);                  
        }

        [SlashCommand("transfer-paychex", "Convert your current paychex to Mini Mart credits")]
        public async Task TransferPaychexToMiniMart()
        {
            await _logger.Log(new LogMessage(LogSeverity.Info, "Mini-Mart Submit", $"User: {Context.User.Username}, Command: mm-transaction", null));

            await DeferAsync();
            await GoogleSheetsDataWriter.TransferPaychexToMiniMartCredits(Context.User as SocketGuildUser);
            await FollowupAsync("Transfer Complete");
            await DeleteOriginalResponseAsync();
        }

        [SlashCommand("mm-transaction", "Submit transaction to Mini-mart")]
        public async Task MiniMartTransactions(SocketGuildUser GuildUser, int Amount, MiniMarketType TransactionType)
        {          
            string? sManagerNickname = ((Context.User as SocketGuildUser).Nickname != null)  ? new PlayerDataLookUps().CleanUpShotCallerName((Context.User as SocketGuildUser).Nickname) : (Context.User as SocketGuildUser).Username;
            string? sUserNickname = (GuildUser.Nickname != null) ? new PlayerDataLookUps().CleanUpShotCallerName(GuildUser.Nickname) : GuildUser.Username;
            
            if (GuildUser.Roles.Any(r => r.Name == "AO - Officers")|| GuildUser.Roles.Any(r => r.Name == "AO - HO Manager") || GuildUser.Roles.Any(r => r.Name == "AO - Minimart"))
            {
                await DeferAsync();
                await GoogleSheetsDataWriter.MiniMartTransaction(Context.User as SocketGuildUser, GuildUser, Amount, TransactionType);

                var miniMarketCreditsTotal = GoogleSheetsDataWriter.GetMiniMarketCredits(sUserNickname);

                if (TransactionType == MiniMarketType.Purchase)
                {
                    int discount = Convert.ToInt32(Amount * .10);

                    await ReplyAsync($"{TransactionType} of {Amount.ToString("N0")} is complete. Discount of {discount.ToString("N0")} was subtracted. {sUserNickname} current balance is {miniMarketCreditsTotal} ");
                    await FollowupAsync("React :thumbsup: when you pickup your items.");
                }
                else
                {
                    await FollowupAsync($"{sUserNickname} {TransactionType} of {Amount.ToString("N0")} is complete");
                }
            }
            else
            {
                await RespondAsync($"You need perms to do this ya bum.",null,false,true,null,null,null,null);
            }
        }

        [SlashCommand("regear", "Submit a regear")]
        public async Task RegearSubmission(int EventID, SocketGuildUser callerName, EventTypeEnum EventType, SocketGuildUser mentor = null)
        {
            List<string> args = new List<string>();

            PlayerDataLookUps eventData = new PlayerDataLookUps();
            RegearModule regearModule = new RegearModule();

            var guildUser = (SocketGuildUser)Context.User;
            var interaction = Context.Interaction as IComponentInteraction;

            string? sUserNickname = ((Context.User as SocketGuildUser).Nickname != null) ? (Context.User as SocketGuildUser).Nickname : Context.User.Username;
            string? sCallerNickname = (callerName.Nickname != null) ? callerName.Nickname : callerName.Username;

            bool bRegearAllowed = true;

            if (sUserNickname.Contains("!sl"))
            {
                sUserNickname = new PlayerDataLookUps().CleanUpShotCallerName(sUserNickname);
            }

            if (sCallerNickname.Contains("!sl"))
            {
                sCallerNickname = new PlayerDataLookUps().CleanUpShotCallerName(sCallerNickname);
            }

            await _logger.Log(new LogMessage(LogSeverity.Info, "Regear Submit", $"User: {Context.User.Username}, Command: regear", null));

            PlayerEventData = await eventData.GetAlbionEventInfo(EventID);

            dataBaseService = new DataBaseService();

            await dataBaseService.AddPlayerInfo(new Player
            {
                PlayerId = PlayerEventData.Victim.Id,
                PlayerName = PlayerEventData.Victim.Name
            });
          
            if (EventType == EventTypeEnum.SpecialEvent)
            {
                bRegearAllowed = true;
            }
            else if ((guildUser.Roles.Any(x => x.Id == 970083088241672245) || guildUser.Roles.Any(r => r.Name == "Bronze Tier Regear - Elligible")) && mentor == null)
            {
                await RespondAsync($"Hey bud. You need to submit your regear with your mentor tagged. They are the optional choice at the end of the command. ", null, false, true);
                bRegearAllowed = false;
            }
            else if (mentor != null && !mentor.Roles.Any(x => x.Id == 1049889855619989515) && (guildUser.Roles.Any(x => x.Id == 970083088241672245) || guildUser.Roles.Any(r => r.Name == "Bronze Tier Regear - Elligible")))
            {
                await RespondAsync($"Cleary you need a mentor you idiot. Make sure you select an ACTUAL mentor", null, false, true);
                bRegearAllowed= false;
            }

            if (bRegearAllowed)
            { 
                if (!await dataBaseService.CheckPlayerIsDid5RegearBefore(sUserNickname) || guildUser.Roles.Any(r => r.Name == "AO - Officers"))
                {
                    if (!await dataBaseService.CheckKillIdIsRegeared(EventID.ToString()))
                    {
                        if (PlayerEventData != null)
                        {
                            var moneyType = (MoneyTypes)Enum.Parse(typeof(MoneyTypes), "ReGear");

                            if (PlayerEventData.Victim.Name.ToLower() == sUserNickname.ToLower() || guildUser.Roles.Any(r => r.Name == "AO - Officers"))
                            {
                                await DeferAsync();

                                await regearModule.PostRegear(Context, PlayerEventData, sCallerNickname, EventType, moneyType, mentor);
                                await Context.User.SendMessageAsync($"<@{Context.User.Id}> Your regear ID:{regearModule.RegearQueueID} has been submitted successfully.");

                                await FollowupAsync("Regear Submission Complete", null, false, true);
                                await DeleteOriginalResponseAsync();
                            }
                            else
                            {
                                await RespondAsync($"<@{Context.User.Id}>. You can't submit regears on the behalf of {PlayerEventData.Victim.Name}. Ask the Regear team if there's an issue.", null, false, true);
                                await _logger.Log(new LogMessage(LogSeverity.Info, "Regear Submit", $"User: {Context.User.Username}, Tried submitting regear for {PlayerEventData.Victim.Name}", null));
                            }
                        }
                        else
                        {
                            await RespondAsync("Event info not found. Please verify Kill ID or event has expired.", null, false, true);
                        }
                    }
                    else
                    {
                        await RespondAsync($"You dumbass <@{Context.User.Id}>. Don't try to scam the guild and steal money. You can't submit another regear for same death. :middle_finger: ", null, false, true);
                    }
                }
                else
                {
                    await RespondAsync($"Woah woah waoh there <@{Context.User.Id}>.....I'm cutting you off. You already submitted 5 regears today. Time to use the eco money you don't have. You can't claim more than 5 regears in a day", null, false, false);
                }
            }
        }

        [ComponentInteraction("deny")]
        public async Task Denied()
        {
            var guildUser = (SocketGuildUser)Context.User;

            var interaction = Context.Interaction as IComponentInteraction;
            string victimName = interaction.Message.Embeds.FirstOrDefault().Fields[1].Value.ToString();
            int killId = Convert.ToInt32(interaction.Message.Embeds.FirstOrDefault().Fields[0].Value);
            ulong regearPoster = Convert.ToUInt64(interaction.Message.Embeds.FirstOrDefault().Fields[6].Value);

            if (guildUser.Roles.Any(r => r.Name == "AO - REGEARS" || r.Name == "AO - Officers" || r.Name == "Gold Tier Regear - Eligible"))
            {
                dataBaseService = new DataBaseService();

                try
                {
                    dataBaseService.DeletePlayerLootByKillId(killId.ToString());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString() + " ERROR DELETING RECORD FROM DATABASE");
                }

                var guildUsertest = Context.Guild.GetUser(regearPoster); // user must be online for it to work.
                
                await Context.Guild.GetUser(regearPoster).SendMessageAsync($"Regear {killId} was denied. https://albiononline.com/en/killboard/kill/{killId}");

                await _logger.Log(new LogMessage(LogSeverity.Info, "Regear Denied", $"User: {Context.User.Username}, Denied regear {killId} for {victimName} ", null));

                await Context.Channel.DeleteMessageAsync(interaction.Message.Id);
            }
            else
            {
                await RespondAsync($"<@{Context.User.Id}>Stop pressing random buttons idiot. That aint your job.", null, false, true);
            }
        }
        
        [ComponentInteraction("approve")]
        public async Task RegearApprove()
        {
            var guildUser = (SocketGuildUser)Context.User;
            var interaction = Context.Interaction as IComponentInteraction;

            int killId = Convert.ToInt32(interaction.Message.Embeds.FirstOrDefault().Fields[0].Value);
            string victimName = interaction.Message.Embeds.FirstOrDefault().Fields[1].Value.ToString();
            string callername = Regex.Replace(interaction.Message.Embeds.FirstOrDefault().Fields[3].Value.ToString(), @"\p{C}+", string.Empty);
            int refundAmount = Convert.ToInt32(interaction.Message.Embeds.FirstOrDefault().Fields[4].Value);
            ulong regearPosterID = Convert.ToUInt64(interaction.Message.Embeds.FirstOrDefault().Fields[6].Value);
            string eventType = interaction.Message.Embeds.FirstOrDefault().Fields[7].Value.ToString();
            string? mentor = (interaction.Message.Embeds.Count >= 8) ? interaction.Message.Embeds.FirstOrDefault().Fields[8].Value.ToString() : null  ;

            PlayerDataLookUps eventData = new PlayerDataLookUps();

            if (guildUser.Roles.Any(r => r.Name == "AO - REGEARS" || r.Name == "AO - Officers" || r.Name == "Gold Tier Regear - Eligible"))
            {
                PlayerEventData = await eventData.GetAlbionEventInfo(killId);
                await GoogleSheetsDataWriter.WriteToRegearSheet(Context, PlayerEventData, refundAmount, callername, eventType, MoneyTypes.ReGear);
                await Context.Channel.DeleteMessageAsync(interaction.Message.Id);

                IReadOnlyCollection<Discord.Rest.RestGuildUser> guildUsers = await Context.Guild.SearchUsersAsync(victimName);

                if (guildUsers.Any(x => x.RoleIds.Any(x => x == 1052241667329118349)) || Context.Guild.GetUser(regearPosterID).Roles.Any(r => r.Name == "Free Regear - Eligible"))
                {
                    await Context.Guild.GetUser(regearPosterID).RemoveRoleAsync(1052241667329118349);
                }

                await Context.Guild.GetUser(regearPosterID).SendMessageAsync($"<@{Context.Guild.GetUser(regearPosterID).Id}> your regear https://albiononline.com/en/killboard/kill/{killId} has been approved! ${refundAmount.ToString("N0")} has been added to your paychex");

                await _logger.Log(new LogMessage(LogSeverity.Info, "Regear Approved", $"User: {Context.User.Username}, Approved the regear {killId} for {victimName} ", null));
            }
            else
            {
                await RespondAsync($"Just because the button is green <@{Context.User.Id}> doesn't mean you can press it. Bug off.", null, false, true);
            }
        }

        [ComponentInteraction("audit")]
        public async Task AuditRegear()
        {
            var guildUser = (SocketGuildUser)Context.User;
            var interaction = Context.Interaction as IComponentInteraction;
            int killId = Convert.ToInt32(interaction.Message.Embeds.FirstOrDefault().Fields[0].Value);
            
            PlayerDataLookUps eventData = new PlayerDataLookUps();
            PlayerEventData = await eventData.GetAlbionEventInfo(killId);

            string sBattleID = (PlayerEventData.EventId == PlayerEventData.BattleId) ? "No battle found" : PlayerEventData.BattleId.ToString();

            string sKillerGuildName = (PlayerEventData.Killer.GuildName == "" || PlayerEventData.Killer.GuildName == null) ? "No Guild" : PlayerEventData.Killer.GuildName;

            if (guildUser.Roles.Any(r => r.Name == "AO - REGEARS" || r.Name == "AO - Officers"))
            {
                var embed = new EmbedBuilder()
                .WithTitle($"Regear audit on {PlayerEventData.Victim.Name}")
                .AddField("Event ID", PlayerEventData.EventId, true)
                .AddField("Victim", PlayerEventData.Victim.Name, true)
                .AddField("Average IP", PlayerEventData.Victim.AverageItemPower, true)
                .AddField("Killer Name", PlayerEventData.Killer.Name, true)
                .AddField("Killer Guild Name", sKillerGuildName, true)
                .AddField("Killer Avg IP", PlayerEventData.Killer.AverageItemPower, true)
                .AddField("Kill Area", PlayerEventData.KillArea, true)
                .AddField("Number of participants", PlayerEventData.numberOfParticipants, false)
                .AddField("Time Stamp", PlayerEventData.TimeStamp, true)
                .WithUrl($"https://albiononline.com/en/killboard/kill/{PlayerEventData.EventId}");

                if (PlayerEventData.EventId != PlayerEventData.BattleId)
                {
                    embed.AddField("BattleID", sBattleID);
                    embed.AddField("BattleBoard Name", $"https://albionbattles.com/battles/{sBattleID}", false);
                    embed.AddField("Battle Killboard", $"https://albiononline.com/en/killboard/battles/{sBattleID}", false);
                }

                await RespondAsync($"Audit report for event {PlayerEventData.EventId}.", null, false, true, null, null, null, embed.Build());
            }
            else
            {
                await RespondAsync($"You cannot see this juicy info <@{Context.User.Id}> Not like you can read anyways.", null, false, true, null, null, null, null);
            }          
        }

        [SlashCommand("regear-oc", "Submit a OC-break regear")]
        public async Task RegearOCSubmission(string items, SocketGuildUser callerName, EventTypeEnum enumEventType, int? a_iEstimatedGearPrice = null)
        {

            PlayerDataLookUps eventData = new PlayerDataLookUps();
            RegearModule regearModule = new RegearModule();
            var guildUser = (SocketGuildUser)Context.User;

            string? sUserNickname = ((Context.User as SocketGuildUser).Nickname != null) ? (Context.User as SocketGuildUser).Nickname : Context.User.Username;
            string? sCallerNickname = (callerName.Nickname != null) ? callerName.Nickname : callerName.Username;
            
            if (sUserNickname.Contains("!sl"))
            {
                sUserNickname = new PlayerDataLookUps().CleanUpShotCallerName(sUserNickname);
            }

            if (sCallerNickname.Contains("!sl"))
            {
                sCallerNickname = new PlayerDataLookUps().CleanUpShotCallerName(sCallerNickname);
            }

            var playerInfo = await eventData.GetAlbionPlayerInfo(sUserNickname);
            var PlayerEventData = playerInfo.players.Where(x => x.Name == sUserNickname).FirstOrDefault();

            dataBaseService = new DataBaseService();

            await dataBaseService.AddPlayerInfo(new Player
            {
                PlayerId = PlayerEventData.Id,
                PlayerName = PlayerEventData.Name
            });


            await _logger.Log(new LogMessage(LogSeverity.Info, "Regear Submit", $"User: {Context.User.Username}, Command: regear", null));

            
            
            if (!await dataBaseService.CheckPlayerIsDid5RegearBefore(sUserNickname) || guildUser.Roles.Any(r => r.Name == "AO - Officers" || r.Name == "Gold Tier Regear - Eligible"))
            {
                await DeferAsync();
                var moneyType = (MoneyTypes)Enum.Parse(typeof(MoneyTypes), "OCBreak");

                if (PlayerEventData.Name.ToLower() == sUserNickname.ToLower() || guildUser.Roles.Any(r => r.Name == "AO - Officers"))
                {
                    await regearModule.PostOCRegear(Context, items.Split(",").ToList(), sCallerNickname, MoneyTypes.OCBreak, enumEventType);
                    await Context.User.SendMessageAsync($"Your OC Break ID:{regearModule.RegearQueueID} has been submitted successfully.");

                    await FollowupAsync("Regear Submission Complete", null, false, true);
                    await DeleteOriginalResponseAsync();
                }
                else
                {
                    await FollowupAsync($"<@{Context.User.Id}>. You can't submit regears on the behalf of {PlayerEventData.Name}. Ask the Regear team if there's an issue.", null, false, true);
                    await _logger.Log(new LogMessage(LogSeverity.Info, "Regear Submit", $"User: {Context.User.Username}, Tried submitting regear for {PlayerEventData.Name}", null));
                }
            }
            else
            {
                await RespondAsync($"Woah woah waoh there <@{Context.User.Id}>.....I'm cutting you off. You already submitted 5 regears today. Time to use the eco money you don't have. You can't claim more than 5 regears in a day", null, false, true);
            }

        }

        [ComponentInteraction("oc-approve")]
        public async Task OCApproved()
        {
            var guildUser = (SocketGuildUser)Context.User;
            string? sUserNickname = ((Context.User as SocketGuildUser).Nickname != null) ? new PlayerDataLookUps().CleanUpShotCallerName((Context.User as SocketGuildUser).Nickname) : Context.User.Username;
            var interaction = Context.Interaction as IComponentInteraction;

            string victimName = interaction.Message.Embeds.FirstOrDefault().Fields[0].Value.ToString();
            string callername = Regex.Replace(interaction.Message.Embeds.FirstOrDefault().Fields[1].Value.ToString(), @"\p{C}+", string.Empty);
            int refundAmount = Convert.ToInt32(interaction.Message.Embeds.FirstOrDefault().Fields[2].Value);
            string eventType = interaction.Message.Embeds.FirstOrDefault().Fields[3].Value.ToString();
            ulong queueID = Convert.ToUInt64(interaction.Message.Embeds.FirstOrDefault().Fields[4].Value);
            ulong regearPosterID = Convert.ToUInt64(interaction.Message.Embeds.FirstOrDefault().Fields[5].Value);
            

            PlayerDataLookUps eventData = new PlayerDataLookUps();

            if (guildUser.Roles.Any(r => r.Name == "AO - REGEARS" || r.Name == "AO - Officers"))
            {
                await GoogleSheetsDataWriter.WriteToRegearSheet(Context, PlayerEventData, refundAmount, callername, eventType, MoneyTypes.OCBreak);
                await Context.Channel.DeleteMessageAsync(interaction.Message.Id);

                await Context.Guild.GetUser(regearPosterID).SendMessageAsync($"Your OC Break {queueID} has been approved by {sUserNickname}! ${refundAmount.ToString("N0")} has been added to your paychex");
                await _logger.Log(new LogMessage(LogSeverity.Info, "OC Break Approved", $"User: {Context.User.Username}, Approved the regear {queueID} for {victimName} ", null));
            }
            else
            {
                await RespondAsync($"Just because the button is green <@{Context.User.Id}> doesn't mean you can press it. Bug off.", null, false, true);
            }
        }

        [ComponentInteraction("oc-deny")]
        public async Task OCDenied()
        {
            var guildUser = (SocketGuildUser)Context.User;

            var interaction = Context.Interaction as IComponentInteraction;
            string victimName = interaction.Message.Embeds.FirstOrDefault().Fields[0].Value.ToString();
            var iQueueID = interaction.Message.Embeds.FirstOrDefault().Fields[4].Value;
            ulong regearPoster = Convert.ToUInt64(interaction.Message.Embeds.FirstOrDefault().Fields[5].Value);

            if (guildUser.Roles.Any(r => r.Name == "AO - REGEARS" || r.Name == "AO - Officers"))
            {
                dataBaseService = new DataBaseService();

                try
                {
                    dataBaseService.DeletePlayerLootByQueueId(iQueueID.ToString());
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString() + " ERROR DELETING RECORD FROM DATABASE");
                }

                var guildUsertest = Context.Guild.GetUser(regearPoster);

                await Context.Guild.GetUser(regearPoster).SendMessageAsync($"OC Regear {iQueueID} was denied.");
                await _logger.Log(new LogMessage(LogSeverity.Info, "OC Regear Denied", $"User: {Context.User.Username}, Denied regear {iQueueID} for {victimName} ", null));

                await Context.Channel.DeleteMessageAsync(interaction.Message.Id);
            }
            else
            {
                await RespondAsync($"<@{Context.User.Id}>Stop pressing random buttons idiot. That aint your job.", null, false, true);
            }
        }

        [SlashCommand("split-loot", "Images should already be uploaded to channel.")]
        public async Task SplitLoot()
        {
            await DeferAsync();

            //var interaction = Context.Interaction as IComponentInteraction;
            LootSplitModule lootSplitMod = new LootSplitModule();

            //scrape images and save via lootSplitModule
            await lootSplitMod.ScrapeImages(Context);

            //scrape members and write to json
            await lootSplitMod.CreateMemberList(Context);

            //create member dict for ids to be used later
            await lootSplitMod.CreateMemberDict(Context);

            //strings for python.exe path and the tessaract python script (with the downloaded image as argument)
            string cmd = lootSplitMod.freeBeerDirectory + "\\PythonScript\\AO-Py-Script\\venv\\Scripts\\Python.exe";
            string pythArgs = lootSplitMod.freeBeerDirectory + "\\PythonScript\\AO-Py-Script\\main.py " + lootSplitMod.freeBeerDirectory + "\\Temp\\image1.png";

            for (int n = 2; n < lootSplitMod.imageCount; n++)
            {
                pythArgs += " " + lootSplitMod.freeBeerDirectory + "\\Temp\\image" + n.ToString() + ".png";
            }

            //call py tesseract and grab output
            await lootSplitMod.CallPyTesseract(Context, cmd, pythArgs);

            //create initial embed
            await lootSplitMod.CreateFirstEmbed(Context);

            //check if members look good, proceed to modal
            await lootSplitMod.SendAddMemButtons(Context);

            lootSplitModule = lootSplitMod;
        }
        [ComponentInteraction("add-members-modal")]
        async Task AddMembersModal()
        {
            LootSplitModule lootSplitMod = lootSplitModule;

            //build modal and send with add members option
            Boolean addIsTrue = true;

            await lootSplitMod.BuildModalHandler(Context, addIsTrue, lootSplitMod.scrapedList, lootSplitMod.imageMembers);

        }
        [ComponentInteraction("no-add-modal")]
        async Task NoAddMembersModal()
        {
            LootSplitModule lootSplitMod = lootSplitModule;

            //build modal and send with add members option
            Boolean addIsTrue = false;

            await lootSplitModule.BuildModalHandler(Context, addIsTrue, lootSplitMod.scrapedList, lootSplitMod.imageMembers);

            await lootSplitMod.PostLootSplit(Context);
        }
        [ComponentInteraction("approve split")]
        async Task ApproveSplit()
        {
            LootSplitModule lootSplitMod = lootSplitModule;
            RegearModule regearModule = new RegearModule();

            var guildUser = (SocketGuildUser)Context.User;
            var interaction = Context.Interaction as IComponentInteraction;

            //check perms to push buttons
            if (guildUser.Roles.Any(r => r.Name == "AO - REGEARS" || r.Name == "AO - Officers" || r.Name == "admin"))
            {
                await RespondAsync("Approved. Now handling some spreadsheet bs, baby hold me just a little bit longer...");

                DataBaseService dataBaseService = new DataBaseService();

                string reasonLootSplit = "Loot split";
                string tempStr = "null";
                var moneyTypes = (MoneyTypes)Enum.Parse(typeof(MoneyTypes), "LootSplit");
                var moneyType = dataBaseService.GetMoneyTypeByName(moneyTypes);
                string constr = "Server = .; Database = FreeBeerdbTest; Trusted_Connection = True";
                string partyLead = lootSplitMod.submitter;
                int refundAmount = Convert.ToInt32(lootSplitMod.lootAmountPer);

                foreach (string playerName in lootSplitMod.imageMembers)
                {
                    //conditional to add members to .Player table if not in there already
                    if (dataBaseService.GetPlayerInfoByName(playerName) == null)
                    {
                        using SqlConnection connection = new SqlConnection(constr);
                        {
                            using SqlCommand command = connection.CreateCommand();

                            {
                                //need to add player to Player table to work with foreign keys
                                command.Parameters.AddWithValue("@playerName", playerName);
                                command.Parameters.AddWithValue("@pid", lootSplitMod.scrapedDict[playerName].ToString());
                                command.CommandText = "INSERT INTO [FreeBeerdbTest].[dbo].[Player] (PlayerName, PlayerId) VALUES (@playerName, @pid)";
                                connection.Open();
                                command.ExecuteNonQuery();
                                connection.Close();
                                command.Parameters.Clear();
                            }
                        }
                    }
                    int playerID = dataBaseService.GetPlayerInfoByName(playerName).Id;
                    await dataBaseService.AddPlayerReGear(new PlayerLoot
                    {
                        TypeId = moneyType.Id,
                        CreateDate = DateTime.Now,
                        Loot = refundAmount,
                        PlayerId = playerID,
                        Message = tempStr,
                        PartyLeader = partyLead,
                        KillId = tempStr,
                        Reason = reasonLootSplit,
                        QueueId = tempStr
                    });
                    //Sheets write for each playerName - Needs Review
                    //PlayerLookupInfo playerInfo = new PlayerLookupInfo();
                    //PlayerDataLookUps albionData = new PlayerDataLookUps();
                    //playerInfo = await albionData.GetPlayerInfo(Context, playerName);
                    //await GoogleSheetsDataWriter.WriteToRegearSheet(Context, null, refundAmount, partyLead, moneyTypes);

                }
                await Context.User.SendMessageAsync(($"<@{Context.Guild.GetUser(lootSplitMod.scrapedDict[partyLead]).Id}> your loot split has been approved! " +
                    $"${refundAmount.ToString("N0")} has been added to your paychex"));
                await _logger.Log(new LogMessage(LogSeverity.Info, "Loot Split Approved", $"User: {Context.User.Username}, Approved the regear for {partyLead} ", null));

                SocketGuildChannel socketChannel = Context.Guild.GetChannel(Context.Channel.Id);
                await socketChannel.DeleteAsync();
            }
            else
            {
                await RespondAsync("Don't push buttons without perms you mongo.");
            }
        }
        [ComponentInteraction("deny split")]
        async Task DenySplit()
        {
            var guildUser = (SocketGuildUser)Context.User;
            //check perms for button pushing
            if (guildUser.Roles.Any(r => r.Name == "AO - REGEARS" || r.Name == "AO - Officers" || r.Name == "admin"))
            {
                await RespondAsync("Loot split denied. Humblest apologies - but don't blame me, blame the regear team.");
                //create and fill list with n urls from channel
                var msgsList = Context.Channel.GetMessagesAsync().ToListAsync().Result.ToList();

                List<string> msgsUrls = new List<string>();
                foreach (var msg in msgsList.FirstOrDefault())
                {
                    await Context.Channel.DeleteMessageAsync(msg);
                }
            }
            else
            {
                await RespondAsync("Don't push buttons without perms you mongo.");
            }

        }
    }
}
