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
using Microsoft.EntityFrameworkCore.Internal;

namespace CommandModule
{
    // Must use InteractionModuleBase<SocketInteractionContext> for the InteractionService to auto-register the commands
    public class CommandModule : InteractionModuleBase<SocketInteractionContext>
    {
        public InteractionService Commands { get; set; }
        private PlayerDataHandler.Rootobject PlayerEventData { get; set; }

        private static Logger _logger;
        private DataBaseService dataBaseService;
        public CommandModule(ConsoleLogger logger)
        {
            _logger = logger;
        }

        // Simple slash command to bring up a message with a button to press
        //[SlashCommand("button", "Button demo command")]
        //public async Task ButtonInput()
        //{
        //    var components = new ComponentBuilder();
        //    var button = new ButtonBuilder()
        //    {
        //        Label = "Button",
        //        CustomId = "button1",
        //        Style = ButtonStyle.Primary
        //    };

        //    // Messages take component lists. Either buttons or select menus. The button can not be directly added to the message. It must be added to the ComponentBuilder.
        //    // The ComponentBuilder is then given to the message components property.
        //    components.WithButton(button);

        //    await RespondAsync("This message has a button!", components: components.Build());
        //}

        //// This is the handler for the button created above. It is triggered by nmatching the customID of the button.
        //[ComponentInteraction("button1")]
        //public async Task ButtonHandler()
        //{
        //    // try setting a breakpoint here to see what kind of data is supplied in a ComponentInteraction.
        //    var c = Context;

        //    await RespondAsync($"You pressed a button!");
        //}

        //// Simple slash command to bring up a message with a select menu
        //[SlashCommand("menu", "Select Menu demo command")]
        //public async Task MenuInput()
        //public async Task MenuInput()
        //{
        //    var components = new ComponentBuilder();
        //    // A SelectMenuBuilder is created
        //    var select = new SelectMenuBuilder()
        //    {
        //        CustomId = "menu1",
        //        Placeholder = "Select something"
        //    };
        //    // Options are added to the select menu. The option values can be generated on execution of the command. You can then use the value in the Handler for the select menu
        //    // to determine what to do next. An example would be including the ID of the user who made the selection in the value.
        //    select.AddOption("abc", "abc_value");
        //    select.AddOption("def", "def_value");
        //    select.AddOption("ghi", "ghi_value");

        //    components.WithSelectMenu(select);

        //    await RespondAsync("This message has a menu!", components: components.Build());
        //}

        //// SelectMenu interaction handler. This receives an array of the selections made.
        //[ComponentInteraction("menu1")]
        //public async Task MenuHandler(string[] selections)
        //{
        //    // For the sake of demonstration, we only want the first value selected.
        //    await RespondAsync($"You selected {selections.First()}");
        //}

        //[SlashCommand("split-loot", "Automated Split loot")]
        //public async Task SplitLoot(IAttachment a_iImageAttachment)
        //{


        //}
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
                    //.AddField("Player ID: ", (playerInfo.Id == null) ? "No info" : playerInfo.Id, true)

                    .AddField("Kill Fame", (playerInfo.KillFame == 0) ? 0 : playerInfo.KillFame)
                    .AddField("Death Fame: ", (playerInfo.DeathFame == 0) ? 0 : playerInfo.DeathFame, true)

                    .AddField("Guild Name ", (playerInfo.GuildName == null || playerInfo.GuildName == "") ? "No info" : playerInfo.GuildName, true)
                    //.AddField("Guild ID: ", (playerInfo.GuildId == null || playerInfo.GuildId == "") ? "No info" : playerInfo.GuildId, true)

                    .AddField("Alliance Name", (playerInfo.AllianceName == null || playerInfo.AllianceName == "") ? "No info" : playerInfo.AllianceName, true);
                //.AddField("Alliance ID", (playerInfo.AllianceId == null || playerInfo.AllianceId == "") ? "No info" : playerInfo.AllianceId, true);

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

            if (ingameName != null)
            {
                sUserNickname = ingameName;
                await guildUserName.ModifyAsync(x => x.Nickname = ingameName);
            }

            playerInfo = await albionData.GetPlayerInfo(Context, sUserNickname);

            if (sUserNickname == playerInfo.Name)
            {
                await dataBaseService.AddPlayerInfo(new Player
                {
                    PlayerId = playerInfo.Id,
                    PlayerName = playerInfo.Name
                });

                await user.AddRoleAsync(newMemberRole);
                await user.AddRoleAsync(freeRegearRole);//free regear role

                await _logger.Log(new LogMessage(LogSeverity.Info, "Register Member", $"User: {Context.User.Username} has registered {playerInfo.Name}, Command: register", null));

                await GoogleSheetsDataWriter.RegisterUserToDataRoster(playerInfo.Name.ToString(), null, null, null, null);

                var embed = new EmbedBuilder()
               .WithTitle($":beers: WELCOME TO FREE BEER :beers:")
               //.WithImageUrl($"attachment://logo.png")
               .WithDescription("We're glad to have you. Please checkout the following below.")
               .AddField($"Don't get kicked", "<#995798935631302667>")
               .AddField($"General info / location of the guild stuff", "<#880598854947454996>")
               .AddField($"Regear program", "<#970081185176891412>")
               .AddField($"ZVZ builds", "<#906375085449945131>")
               .AddField($"Before you do ANYTHING else", "Your existence in the guild relies you on reading these");
                //.AddField(new EmbedFieldBuilder() { Name = "This is the name field? ", Value = "This is the value in the name field" });

                await freeBeerMainChannel.SendMessageAsync($"<@{Context.Guild.GetUser(guildUserName.Id).Id}>", false, embed.Build());
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
            var sPlayerAlbionId = new PlayerDataLookUps().GetPlayerInfo(Context, null); //either get from google sheet or search in albion API;
            string? sUserNickname = ((Context.Interaction.User as SocketGuildUser).Nickname != null) ? (Context.Interaction.User as SocketGuildUser).Nickname : Context.Interaction.User.Username;

            int iDeathDisplayCounter = 1;
            int iVisibleDeathsShown = Int32.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("showDeathsQuantity")) - 1;  //can add up to 10 deaths //Add to config

            if (sPlayerAlbionId.Result != null)
            {
                using (HttpResponseMessage response = await AlbionOnlineDataParser.AlbionOnlineDataParser.ApiClient.GetAsync($"players/{sPlayerAlbionId.Result.Id}/deaths"))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        sPlayerData = await response.Content.ReadAsStringAsync();
                        var parsedObjects = JArray.Parse(sPlayerData);
                        //TODO: Add killer and date of death

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

                        var component = new ComponentBuilder();


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
        public async Task FetchMarketPrice(int PriceOption, string a_sItemType, int a_iQuality, string? a_sMarketLocation = "")
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
        public async Task BlacklistPlayer(SocketGuildUser a_DiscordUsername, string? IngameName = null, string Reason = null, string Fine = null, string AdditionalNotes = null)
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

        [SlashCommand("regear", "Submit a regear")]
        public async Task RegearSubmission(int EventID, SocketGuildUser callerName)
        {

            PlayerDataLookUps eventData = new PlayerDataLookUps();
            RegearModule regearModule = new RegearModule();

            var guildUser = (SocketGuildUser)Context.User;
            var interaction = Context.Interaction as IComponentInteraction;

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

            await _logger.Log(new LogMessage(LogSeverity.Info, "Regear Submit", $"User: {Context.User.Username}, Command: regear", null));

            PlayerEventData = await eventData.GetAlbionEventInfo(EventID);

            dataBaseService = new DataBaseService();

            await dataBaseService.AddPlayerInfo(new Player
            {
                PlayerId = PlayerEventData.Victim.Id,
                PlayerName = PlayerEventData.Victim.Name
            });

            //Check If The Player Got 5 Regear Or Not
            if (!await dataBaseService.CheckPlayerIsDid5RegearBefore(sUserNickname) || guildUser.Roles.Any(r => r.Name == "AO - Officers"))
            {
                //CheckToSeeIfRegearHasAlreadyBeenClaimed
                if (!await dataBaseService.CheckKillIdIsRegeared(EventID.ToString()))
                {
                    if (PlayerEventData != null)
                    {
                        var moneyType = (MoneyTypes)Enum.Parse(typeof(MoneyTypes), "ReGear");

                        if (PlayerEventData.Victim.Name.ToLower() == sUserNickname.ToLower() || guildUser.Roles.Any(r => r.Name == "AO - Officers"))
                        {
                            await DeferAsync();

                            if (PlayerEventData.groupMemberCount >= 20 && PlayerEventData.BattleId != PlayerEventData.EventId)
                            {
                                await regearModule.PostRegear(Context, PlayerEventData, sCallerNickname, "ZVZ content", moneyType);
                                await Context.User.SendMessageAsync($"<@{Context.User.Id}> Your regear ID:{regearModule.RegearQueueID} has been submitted successfully.");
                            }
                            else if (PlayerEventData.groupMemberCount <= 20 && PlayerEventData.BattleId != PlayerEventData.EventId)
                            {
                                await regearModule.PostRegear(Context, PlayerEventData, sCallerNickname, "Small group content", moneyType);
                                await Context.User.SendMessageAsync($"<@{Context.User.Id}> Your regear ID:{regearModule.RegearQueueID} has been submitted successfully.");
                            }
                            else if (PlayerEventData.BattleId == 0 || PlayerEventData.BattleId == PlayerEventData.EventId)
                            {
                                await regearModule.PostRegear(Context, PlayerEventData, sCallerNickname, "Solo or small group content", moneyType);
                                await Context.User.SendMessageAsync($"<@{Context.User.Id}> Your regear ID:{regearModule.RegearQueueID} has been submitted successfully.");
                            }

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

        [ComponentInteraction("deny")]
        public async Task Denied()
        {
            var guildUser = (SocketGuildUser)Context.User;

            var interaction = Context.Interaction as IComponentInteraction;
            string victimName = interaction.Message.Embeds.FirstOrDefault().Fields[1].Value.ToString();
            int killId = Convert.ToInt32(interaction.Message.Embeds.FirstOrDefault().Fields[0].Value);
            ulong regearPoster = Convert.ToUInt64(interaction.Message.Embeds.FirstOrDefault().Fields[6].Value);

            if (guildUser.Roles.Any(r => r.Name == "AO - REGEARS" || r.Name == "AO - Officers"))
            {

                dataBaseService = new DataBaseService();
                dataBaseService.DeletePlayerLootByKillId(killId.ToString());

                await Context.Guild.GetUser(regearPoster).SendMessageAsync($"<@{Context.Guild.GetUser(regearPoster).Id}> Regear {killId} was denied. https://albiononline.com/en/killboard/kill/{killId}");
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
            ulong regearPoster = Convert.ToUInt64(interaction.Message.Embeds.FirstOrDefault().Fields[6].Value);

            PlayerDataLookUps eventData = new PlayerDataLookUps();

            if (guildUser.Roles.Any(r => r.Name == "AO - REGEARS" || r.Name == "AO - Officers"))
            {
                PlayerEventData = await eventData.GetAlbionEventInfo(killId);
                await GoogleSheetsDataWriter.WriteToRegearSheet(Context, PlayerEventData, refundAmount, callername);
                await Context.Channel.DeleteMessageAsync(interaction.Message.Id);

                await Context.Guild.GetUser(regearPoster).SendMessageAsync($"<@{Context.Guild.GetUser(regearPoster).Id}> your regear https://albiononline.com/en/killboard/kill/{killId} has been approved! ${refundAmount.ToString("N0")} has been added to your paychex");

                if (Context.Guild.GetUser(regearPoster).Roles.Any(r => r.Name == "Free Regear - Eligible"))
                {
                    await Context.Guild.GetUser(regearPoster).RemoveRoleAsync(1052241667329118349);//using test role
                }

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
        //[SlashCommand("split", "Scrapes members, grabs image, compares members and stores list of those contained in image")]
        //public async Task SplitLoot()
        //{

        //    List<string> memberList = new List<string>();

        //    foreach (IGuildUser user in Context.Guild.Users)
        //    {
        //        memberList.Add(user.Username);
        //    }

        //    string tempDir = @"C:\Users\gmbro\Source\Repos\FreeBeerDiscordBot\Temp";
        //    if (!Directory.Exists(tempDir))//create Temp folder for the python program to utilize if it doesn't exist
        //    {
        //        Directory.CreateDirectory(tempDir);
        //    }

        //    string jsonstring = JsonConvert.SerializeObject(memberList);

        //    using (StreamWriter writer = File.CreateText("C:\\Users\\gmbro\\Source\\Repos\\FreeBeerDiscordBot\\Temp\\members.json"))
        //    {
        //        await writer.WriteAsync(jsonstring);
        //    }

        //    await ReplyAsync("members received.");


        //}


        [SlashCommand("view-paychex","Views your current paychex amount")]
        public async Task GetCurrentPaychexAmount()
        {
            string? sUserNickname = ((Context.User as SocketGuildUser).Nickname != null) ? (Context.User as SocketGuildUser).Nickname : Context.User.Username;
            string returnValue = GoogleSheetsDataWriter.GetCurrentPaychexAmount(sUserNickname);

            await RespondAsync($"Your current paychex total is ${returnValue}",null,false,true);
        }
    }
}
