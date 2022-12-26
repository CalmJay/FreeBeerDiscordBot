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

namespace CommandModule
{
    // Must use InteractionModuleBase<SocketInteractionContext> for the InteractionService to auto-register the commands
    public class CommandModule : InteractionModuleBase<SocketInteractionContext>
    {
        public InteractionService Commands { get; set; }
        private PlayerDataHandler.Rootobject PlayerEventData { get; set; }

        private static Logger _logger;
        private DataBaseService dataBaseService;
        private List<string> lootSplitMembers;
        private decimal lootSplitPerMember;

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
                            //await DeferAsync();

                            if (PlayerEventData.groupMemberCount >= 20 && PlayerEventData.BattleId != PlayerEventData.EventId)
                            {
                                await regearModule.PostRegear(Context, PlayerEventData, sCallerNickname, "ZVZ content", moneyType);
                                await RespondAsync($"<@{Context.User.Id}> Your regear ID:{regearModule.RegearQueueID} has been submitted successfully.", null, false, true);
                            }
                            else if (PlayerEventData.groupMemberCount <= 20 && PlayerEventData.BattleId != PlayerEventData.EventId)
                            {
                                await regearModule.PostRegear(Context, PlayerEventData, sCallerNickname, "Small group content", moneyType);
                                await RespondAsync($"<@{Context.User.Id}> Your regear ID:{regearModule.RegearQueueID} has been submitted successfully.", null, false, true);
                            }
                            else if (PlayerEventData.BattleId == 0 || PlayerEventData.BattleId == PlayerEventData.EventId)
                            {
                                await regearModule.PostRegear(Context, PlayerEventData, sCallerNickname, "Solo or small group content", moneyType);
                                await RespondAsync($"<@{Context.User.Id}> Your regear ID:{regearModule.RegearQueueID} has been submitted successfully.", null, false, true);
                            }
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
                await RespondAsync($"<@{Context.Guild.GetUser(regearPoster).Id}> Regear {killId} was denied. https://albiononline.com/en/killboard/kill/{killId}", null, false, false);
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
                await ReplyAsync(($"<@{Context.Guild.GetUser(regearPoster).Id}> your regear https://albiononline.com/en/killboard/kill/{killId} has been approved! ${refundAmount.ToString("N0")} has been added to your paychex"));
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
        [SlashCommand("split-loot", "Paste/upload party ss(s). Other fields required (tbu).")]
        public async Task SplitLoot(IAttachment partyImage, int lootAmount, int? silverBagAmount, int? chest, string? membersNotInImage)
        {
            var guildUser = (SocketGuildUser)Context.User;
            //var interaction = Context.Interaction as IComponentInteraction;
            var lootSplitModule = new LootSplitModule();

            //check if file types are valid
            if (partyImage.Filename.StartsWith("https://cdn.discordapp.com/attachments/") ||
                partyImage.Filename.EndsWith(".png") ||
                partyImage.Filename.EndsWith(".jpg"))
            {

                //find curr dir and change to the freebeerdiscordbot directory
                string currdir = Directory.GetCurrentDirectory();
                string parent = Directory.GetParent(currdir).FullName;
                string parentTwo = Directory.GetParent(parent).FullName;
                string freeBeerDir = Directory.GetParent(parentTwo).FullName;

                //create the temp dir if not existing
                string tempDir = @freeBeerDir + "\\Temp";
                if (!Directory.Exists(tempDir))
                {
                    Directory.CreateDirectory(tempDir);
                }

                //download attachment
                var fileUrl = partyImage.Url;

                await RespondAsync("Hold tight. Processing...");

                //download the image url that was uploaded to the channel
                using var httpClient = new HttpClient();
                using var s = httpClient.GetStreamAsync(fileUrl);
                using var fs = new FileStream(freeBeerDir + "\\Temp\\image.png", FileMode.OpenOrCreate);
                s.Result.CopyTo(fs);

                //scrape members and write to Json
                List<string> memberList = new List<string>();

                //grab iterable and make list
                var iterable = Context.Guild.GetUsersAsync().ToListAsync().Result.ToList();
                foreach (var member in iterable.FirstOrDefault())
                {
                    //if no nickname, add the username
                    if (member.Nickname is null)
                    { memberList.Add(member.Username); }
                    //if squad leader, remove the dumbass prefix
                    else if (member.Nickname.StartsWith("!sl"))
                    { memberList.Add(member.Nickname.Remove(0, 4)); }
                    //if neither, just add the Nickname - NEED EVERYONE IN CHANNEL TO HAVE IGNs
                    else
                    { memberList.Add(member.Nickname); }
                }

                //serialize and write to json
                string jsonstring = JsonConvert.SerializeObject(memberList);
                using (StreamWriter writer = System.IO.File.CreateText(freeBeerDir + "\\Temp\\members.json"))
                {
                    await writer.WriteAsync(jsonstring);
                }

                //strings for python.exe path and the tessaract python script (with the downloaded image as argument)
                //will eventually add the ability to upload multiple party images - these will become more command 
                //line arguments to a max of 3 or 4 maybe?
                string cmd = freeBeerDir + "\\PythonScript\\AO-Py-Script\\venv\\Scripts\\Python.exe";
                string pyth = freeBeerDir + "\\PythonScript\\AO-Py-Script\\main.py " +
                    freeBeerDir + "\\Temp\\image.png";

                ProcessStartInfo start = new ProcessStartInfo();
                start.FileName = cmd;//cmd is full path to python.exe
                start.Arguments = pyth;//pyth is path to .py file and any cmd line args
                start.UseShellExecute = false;
                start.RedirectStandardOutput = true;
                using (Process process = Process.Start(start))
                {
                    //read the python output and write it to json
                    using (StreamReader reader = process.StandardOutput)
                    {
                        string result = reader.ReadToEnd();
                        string jsonstringTwo = JsonConvert.SerializeObject(result); //serialize string to write to json
                        using (StreamWriter writerTwo = System.IO.File.CreateText(freeBeerDir + "\\Temp\\members.json"))
                        {
                            await writerTwo.WriteAsync(jsonstringTwo);
                        }

                        //split single string into Icollection? List
                        List<string> results = result.Split(',').ToList();

                        //clean list to strings with double quotes only - DONT USE FOREACH ON ICOLLECTION IT WILL BREAK THE LOOP
                        for (int i = 0; i < results.Count; i++)
                        {
                            //cleanup strings after list separation, there are extra quotations and brackets that are added during
                            //conversion to json-py-console-consoleread; this handles all of it
                            results[i] = results[i].Remove(0, 2);
                            results[i] = results[i].Remove(results[i].Length - 1);
                        }
                        string lastItem = results[results.Count - 1].Remove(results[results.Count - 1].Length - 3);
                        results[results.Count - 1] = lastItem;

                        //string from command parameters to list
                        List<string> notInImage = membersNotInImage.Split(",").ToList();

                        //add user that opened socket - likely the large frame at top of image not captured
                        if (guildUser.Nickname != null)
                        {
                            if (!results.Contains(guildUser.Nickname))
                            {
                                results.Add(guildUser.Nickname);
                            }
                        }
                        else
                        {
                            if (!results.Contains(guildUser.Username))
                            {
                                results.Add(guildUser.Username);
                            }
                        }
                        //check strings in List notInImage (list from command parameter string) for !in results, if not, add them
                        foreach (string x in notInImage)
                        {
                            if (x != null)
                            {
                                if (results.Contains(x)) { continue; }
                                else { results.Add(x); }
                            }
                            else
                            {
                                continue;
                            }
                        }

                        lootSplitMembers = results.ToList();

                        //Resulting List of members complete, begin embed builder
                        var embed = new EmbedBuilder()
                        .WithColor(Discord.Color.Orange)
                        .WithTitle($"Loot split submitted by {Context.User.Username}")
                        .AddField("Member Count", results.Count)
                        .AddField(x =>
                        {
                            //loop results and add members
                            x.Name = "Members recorded (Including Not In Image)";
                            for (int i = 0; i < (results.Count - 1); i++)
                            {
                                x.Value += results[i] + ", ";
                            }
                            x.Value += results[results.Count - 1];
                            x.IsInline = false;
                        });
                        //embed split amount
                        if (silverBagAmount != null)
                        {
                            decimal split = ((decimal)(lootAmount * .9 + silverBagAmount)) / results.Count;
                            embed.AddField("Split Amount (Per)", decimal.Round(split));
                            lootSplitPerMember = split;
                        }
                        else
                        {
                            decimal split = (decimal)(lootAmount * .9) / results.Count;
                            embed.AddField("Split Amount (Per)", decimal.Round(split));
                            lootSplitPerMember = split;
                        };
                        embed.WithImageUrl(fileUrl);
                        if (chest != null)
                        {
                            embed.AddField("Chest Number(s)", chest);
                        };

                        //send the embedded report
                        await Context.Channel.SendMessageAsync("--Loot Split Report--", false, embed.Build());

                        await lootSplitModule.PostLootSplit(Context);

                        //NEED TO ADD A WAY TO RESEND THE BUTTON COMPONENTS IF SOME IDIOT WITHOUT PERMS USES THEM

                    }

                }

            }
        }
        [ComponentInteraction("approve split")]
        async Task ApproveSplit()
        {
            var guildUser = (SocketGuildUser)Context.User;
            //check perms to push buttons
            if (guildUser.Roles.Any(r => r.Name == "AO - REGEARS" || r.Name == "AO - Officers" || r.Name == "admin"))
            {
                await RespondAsync("Approved. Now handling some spreadsheet bs, baby hold me just a little bit longer...");

                DataBaseService dataBaseService = new DataBaseService();

                string reasonLootSplit = "Loot split";
                string message = "Loot split silver addition for member.";
                //CHANGE ID TO BE THE ACTUAL ID FROM THE SCRAPE IF WE WANT TO ADD ACTUAL DISCORD IDS TO DB
                int tempInt = 0000; //this is just temp PlayerID for .Player table needs to be actual discord id
                string tempStr = "null";
                var moneyType = (MoneyTypes)Enum.Parse(typeof(MoneyTypes), "LootSplit");
                string constr = "Server = DESKTOP-CV02ERA; Database = FreeBeerdbTest; Trusted_Connection = True";

                foreach (string playerName in lootSplitMembers)
                {
                    //STATEMENT NEEDS TO BE UPDATED, ASSUMED PLAYERS WILL BE IN db.PLAYER, SO NO NEED TO CHECK / CREATE, JUST EXCEPT IF THEY DON'T EXIST
                    if (dataBaseService.GetPlayerInfoByName(playerName) == null)
                    {
                        using SqlConnection connection = new SqlConnection(constr);
                        {
                            using SqlCommand command = connection.CreateCommand();
                            {
                                //need to add player to Player table to work with foreign keys
                                command.Parameters.AddWithValue("@playerName", playerName);
                                command.Parameters.AddWithValue("@pid", tempInt.ToString());
                                command.CommandText = "INSERT INTO [FreeBeerdbTest].[dbo].[Player] (PlayerName, PlayerId) VALUES (@playerName, @pid)";
                                connection.Open();
                                command.ExecuteNonQuery();

                                //now we can actually use GetPlayerInfoByName to grab the foreign key
                                int playerID = dataBaseService.GetPlayerInfoByName(playerName).Id;
                                command.Parameters.AddWithValue("@type", moneyType);
                                command.Parameters.AddWithValue("@loot", lootSplitPerMember);
                                command.Parameters.AddWithValue("@date", DateTime.Now);
                                command.Parameters.AddWithValue("@reason", reasonLootSplit);
                                command.Parameters.AddWithValue("@leader", tempStr);
                                command.Parameters.AddWithValue("@killid", tempStr);
                                command.Parameters.AddWithValue("@queueid", tempStr);
                                command.Parameters.AddWithValue("@message", message);
                                command.Parameters.AddWithValue("@playerID", playerID);
                                command.CommandText = "INSERT INTO [FreeBeerdbTest].[dbo].[PlayerLoot] (TypeID, PlayerID, Loot, CreateDate, " +
                                    "Reason, PartyLeader, KillId, QueueId, Message) " +
                                    "VALUES (@type, @playerID, @loot, @date, @reason, @leader, @killid, @queueid, @message)";
                                command.ExecuteNonQuery();
                                connection.Close();
                                command.Parameters.Clear();
                            }
                        }
                    }
                    //if player exists in table .Player do this instead
                    else
                    {
                        using SqlConnection connection = new SqlConnection(constr);
                        {
                            using SqlCommand command = connection.CreateCommand();
                            {
                                int playerID = dataBaseService.GetPlayerInfoByName(playerName).Id;
                                command.Parameters.AddWithValue("@type", moneyType);
                                command.Parameters.AddWithValue("@loot", lootSplitPerMember);
                                command.Parameters.AddWithValue("@date", DateTime.Now);
                                command.Parameters.AddWithValue("@reason", reasonLootSplit);
                                command.Parameters.AddWithValue("@leader", tempStr);
                                command.Parameters.AddWithValue("@killid", tempStr);
                                command.Parameters.AddWithValue("@queueid", tempStr);
                                command.Parameters.AddWithValue("@playerID", playerID);
                                command.Parameters.AddWithValue("@message", message);
                                command.CommandText = "INSERT INTO [FreeBeerdbTest].[dbo].[PlayerLoot] (TypeID, PlayerID, Loot, CreateDate, " +
                                    "Reason, PartyLeader, KillId, QueueId, Message) " +
                                    "VALUES (@type, @playerID, @loot, @date, @reason, @leader, @killid, @queueid, @message)";
                                connection.Open();
                                command.ExecuteNonQuery();
                                connection.Close();
                                command.Parameters.Clear();
                            }
                        }
                    }
                }
                await Context.Channel.SendMessageAsync("Database updated. Split amount added to each member listed in report.");
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
            }
            else
            {
                await RespondAsync("Don't push buttons without perms you mongo.");
            }
        }
    }
}
