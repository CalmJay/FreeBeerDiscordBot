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
using PlayerData;
using Newtonsoft.Json;
using DiscordBot.RegearModule;
using MarketData;
using System.Collections.Generic;
using System.Text.RegularExpressions;

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
        [SlashCommand("button", "Button demo command")]
        public async Task ButtonInput()
        {
            var components = new ComponentBuilder();
            var button = new ButtonBuilder()
            {
                Label = "Button",
                CustomId = "button1",
                Style = ButtonStyle.Primary
            };

            // Messages take component lists. Either buttons or select menus. The button can not be directly added to the message. It must be added to the ComponentBuilder.
            // The ComponentBuilder is then given to the message components property.
            components.WithButton(button);

            await RespondAsync("This message has a button!", components: components.Build());
        }

        // This is the handler for the button created above. It is triggered by nmatching the customID of the button.
        [ComponentInteraction("button1")]
        public async Task ButtonHandler()
        {
            // try setting a breakpoint here to see what kind of data is supplied in a ComponentInteraction.
            var c = Context;

            await RespondAsync($"You pressed a button!");
        }

        // Simple slash command to bring up a message with a select menu
        [SlashCommand("menu", "Select Menu demo command")]
        public async Task MenuInput()
        {
            var components = new ComponentBuilder();
            // A SelectMenuBuilder is created
            var select = new SelectMenuBuilder()
            {
                CustomId = "menu1",
                Placeholder = "Select something"
            };
            // Options are added to the select menu. The option values can be generated on execution of the command. You can then use the value in the Handler for the select menu
            // to determine what to do next. An example would be including the ID of the user who made the selection in the value.
            select.AddOption("abc", "abc_value");
            select.AddOption("def", "def_value");
            select.AddOption("ghi", "ghi_value");

            components.WithSelectMenu(select);

            await RespondAsync("This message has a menu!", components: components.Build());
        }

        // SelectMenu interaction handler. This receives an array of the selections made.
        [ComponentInteraction("menu1")]
        public async Task MenuHandler(string[] selections)
        {
            // For the sake of demonstration, we only want the first value selected.
            await RespondAsync($"You selected {selections.First()}");
        }

        [SlashCommand("recent-deaths", "View recent deaths")]
        public async Task GetRecentDeaths()
        {
            var testuser = Context.User.Id;
            //SocketSlashCommand command = test;
            string? sPlayerData = null;
            var sPlayerAlbionId = new AlbionAPIDataSearch().GetPlayerInfo(Context); //either get from google sheet or search in albion API;
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
                                embed.AddField($"Death {iDeathDisplayCounter} : {searchDeaths[i] }", $"https://albiononline.com/en/killboard/kill/{searchDeaths[i]}", false);

                                regearbutton.Label = $"Regear Death{iDeathDisplayCounter}"; //QOL Update. Allows members to start the regear process straight from the recent deaths list
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
                    await ReplyAsync($"<@{Context.User.Id}> Price for {a_sItemType} is: " + marketData.Result.FirstOrDefault().sell_price_min);
                }
                else
                {
                    await ReplyAsync($"<@{Context.User.Id}> Price not found");
                }
            }
            else
            {
                await ReplyAsync($"<@{Context.User.Id}> Price not found");
            }


        }

        [SlashCommand("blacklist", "Put a player on the shit list")]
        public async Task BlacklistPlayer(string DiscordUsername, string? IngameName = null, string Reason = null, string Fine = null, string AdditionalNotes = null)
        {
            var guildUser = (SocketGuildUser)Context.User;

            var sDiscordUsername = DiscordUsername;
            string? sDiscordNickname = IngameName;
            string? AlbionInGameName = IngameName;
            string? sReason = Reason;
            string? sFine = Fine;
            string? sNotes = AdditionalNotes;

            Console.WriteLine("Dickhead " + sDiscordUsername + " has been blacklisted");

            await GoogleSheetsDataWriter.WriteToFreeBeerRosterDatabase(sDiscordUsername.ToString(), sDiscordNickname, sReason, sFine, sNotes);
            ReplyAsync(sDiscordUsername.ToString() + " has been blacklisted");
        }

        [SlashCommand("regear", "Submit a regear")]
        public async Task RegearSubmission(int EventID, SocketGuildUser callerName)
        {

            AlbionAPIDataSearch eventData = new AlbionAPIDataSearch();
            RegearModule regearModule = new RegearModule();
            var guildUser = (SocketGuildUser)Context.User;
            var interaction = Context.Interaction as IComponentInteraction;
            string? sUserNickname = ((Context.User as SocketGuildUser).Nickname != null) ? (Context.User as SocketGuildUser).Nickname : Context.User.Username;

            await _logger.Log(new LogMessage(LogSeverity.Info, "Regear Submit", $"User: {Context.User.Username}, Command: regear", null));
            
            
            PlayerEventData = await eventData.GetAlbionEventInfo(EventID);

            dataBaseService = new DataBaseService();

            await dataBaseService.AddPlayerInfo(new Player // USE THIS FOR THE REGISTERING PROCESS
            {
                PlayerId = PlayerEventData.Victim.Id,
                PlayerName = PlayerEventData.Victim.Name
            });

            //Check If The Player Got 5 Regear Or Not
            if (!await dataBaseService.CheckPlayerIsDid5RegearBefore(sUserNickname))
            {
                //CheckToSeeIfRegearHasAlreadyBeenClaimed
                if (!await dataBaseService.CheckKillIdIsRegeared(EventID.ToString()))
                {
                    if (PlayerEventData != null)
                    {
                        var moneyType = (MoneyTypes)Enum.Parse(typeof(MoneyTypes), "ReGear");
                        string cleanedUpCallerName = callerName.ToString().Split('#')[0];
                        //await DeferAsync();

                        if (PlayerEventData.Victim.Name.ToLower() == sUserNickname.ToLower() || guildUser.Roles.Any(r => r.Name == "AO - Officers"))
                        {
                            if (PlayerEventData.groupMemberCount >= 20 && PlayerEventData.BattleId != PlayerEventData.EventId)
                            {
                                await regearModule.PostRegear(Context, PlayerEventData, cleanedUpCallerName, "ZVZ content", moneyType);
                                await FollowupAsync($"<@{Context.User.Id}> Your regear ID:{regearModule.RegearQueueID} has been submitted successfully.", null, false, true);


                            }
                            else if (PlayerEventData.groupMemberCount <= 20 && PlayerEventData.BattleId != PlayerEventData.EventId)
                            {
                                await regearModule.PostRegear(Context, PlayerEventData, cleanedUpCallerName, "Small group content", moneyType);
                                await FollowupAsync($"<@{Context.User.Id}> Your regear ID:{regearModule.RegearQueueID} has been submitted successfully.", null, false, true);


                            }
                            else if (PlayerEventData.BattleId == 0 || PlayerEventData.BattleId == PlayerEventData.EventId)
                            {
                                await regearModule.PostRegear(Context, PlayerEventData, cleanedUpCallerName, "Solo or small group content", moneyType);
                                await FollowupAsync($"<@{Context.User.Id}> Your regear ID:{regearModule.RegearQueueID} has been submitted successfully.", null, false, true);


                            }
                            //await FollowupAsync($"<@{Context.User.Id}> Your regear ID:{regearModule.RegearQueueID} has been submitted successfully.", null, false ,true, null, null, null, null);
                        }
                        else
                        {
                            await RespondAsync($"<@{Context.User.Id}>. You can't submit regears on the behalf of {PlayerEventData.Victim.Name}. Ask the Regear team if there's an issue.", null, false, true);
                        }
                    }
                    else
                    {
                        await RespondAsync("Event info not found. Please verify Kill ID or event has expired.", null, false, true);
                    }
                }
                else
                {
                    await RespondAsync($"You dumbass <@{Context.User.Id}>. Don't try to scam your guild and theft the money. You can't get another regear for same death", null, false, true);
                }
            }
            else
            {
                await RespondAsync($"<@{Context.User.Id}>, you are the stupidest person here, trying to  scam the guild and theft the money, You can't get more than 5 regear in one day", null, false, true);
            }
        }

        [ComponentInteraction("deny")]
        public async Task Denied()
        {
            var guildUser = (SocketGuildUser)Context.User;
            
            var interaction = Context.Interaction as IComponentInteraction;
            string victimName = interaction.Message.Embeds.FirstOrDefault().Fields[1].Value.ToString();
            int killId = Convert.ToInt32(interaction.Message.Embeds.FirstOrDefault().Fields[0].Value);
            ulong regearPoster =  Convert.ToUInt64(interaction.Message.Embeds.FirstOrDefault().Fields[6].Value);

            if (guildUser.Roles.Any(r => r.Name == "AO - REGEARS" || r.Name == "AO - Officers"))
            {

                dataBaseService = new DataBaseService();
                dataBaseService.DeletePlayerLootByKillId(killId.ToString());
                await RespondAsync($"<@{Context.Guild.GetUser(regearPoster).Id}> Regear {killId} was denied. https://albiononline.com/en/killboard/kill/{killId}", null, false, true, null, null, null, null);
                await _logger.Log(new LogMessage(LogSeverity.Info, "Regear Denied", $"User: {Context.User.Username}, Denied regear {killId} for {victimName} ", null));

                await Context.Channel.DeleteMessageAsync(interaction.Message.Id);
            }
            else
            {
                await RespondAsync($"HEY EVERYONE!!! <@{Context.User.Id}> was trying to deny a regear. Stop pressing random buttons idiot. That aint your job.");
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
   
            AlbionAPIDataSearch eventData = new AlbionAPIDataSearch();
            
            if (guildUser.Roles.Any(r => r.Name == "AO - REGEARS" || r.Name == "AO - Officers"))
            {
                PlayerEventData = await eventData.GetAlbionEventInfo(killId);
                await GoogleSheetsDataWriter.WriteToRegearSheet(Context, PlayerEventData, refundAmount, callername);
                await Context.Channel.DeleteMessageAsync(interaction.Message.Id);
                await ReplyAsync(($"<@{Context.Guild.GetUser(regearPoster).Id}> your regear has been approved! ${refundAmount.ToString("N0")} has been added to your paychex"));
                await _logger.Log(new LogMessage(LogSeverity.Info, "Regear Approved", $"User: {Context.User.Username}, Approved the regear {killId} for {victimName} ", null));
            }
            else
            {
                await RespondAsync($"Just because the button is green <@{Context.User.Id}> doesn't mean you can press it. Bug off.",null,false,true);
            }
        }

        [ComponentInteraction("audit")]
        public async Task AuditRegear()
        {
            var guildUser = (SocketGuildUser)Context.User;
            var interaction = Context.Interaction as IComponentInteraction;
            int killId = Convert.ToInt32(interaction.Message.Embeds.FirstOrDefault().Fields[0].Value);

            AlbionAPIDataSearch eventData = new AlbionAPIDataSearch();
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
                    embed.AddField("Battle Killboard", $"https://albiononline.com/en/killboard/battles/{sBattleID}",false);
                }

                await RespondAsync($"Audit report for event {PlayerEventData.EventId}.", null, false, true, null, null, null, embed.Build());
            }
            else
            {
                await RespondAsync($"You cannot see this juicy info <@{Context.User.Id}> Not like you can read anyways.");
            }
                
            
        }


        private bool isRegearDuplicate(int a_iEventID)
        {

            //await GoogleSheetsDataWriter.ReadAsync();


            return false;
        }
    }
}
