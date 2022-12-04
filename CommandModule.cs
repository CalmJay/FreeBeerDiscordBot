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
                                embed.AddField($"Death {iDeathDisplayCounter} : {searchDeaths[i]}", $"https://albiononline.com/en/killboard/kill/{searchDeaths[i]}", false);

                                //regearbutton.Label = $"Regear Death{iDeathDisplayCounter}"; //QOL Update. Allows members to start the regear process straight from the recent deaths list
                                //regearbutton.CustomId = searchDeaths[i].ToString();
                                //component.WithButton(regearbutton);

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

        [SlashCommand("blacklist", "Put a player on the shit list")]
        public async Task BlacklistPlayer(string a_sDiscordUsername, string? a_sIngameName)
        {
            //.AddOption("discordusername", ApplicationCommandOptionType.Mentionable, "Discord username", false)
            //.AddOption("ingame-name", ApplicationCommandOptionType.String, "In-game name or discord nickname", true)
            //.AddOption("reason", ApplicationCommandOptionType.String, "Reason for being blacklisted.", false)
            //.AddOption("fine", ApplicationCommandOptionType.String, "What's the fine?", false)
            //.AddOption("notes", ApplicationCommandOptionType.String, "Additional notes on blacklist?", false);

            var sDiscordUsername = a_sDiscordUsername;
            string? sDiscordNickname = a_sIngameName;
            //string? sDiscordNickname = command.Data.Options.FirstOrDefault(x => x.Name == "ingame-name").Value.ToString();
            //string? sReason = command.Data.Options.FirstOrDefault(x => x.Name == "reason").Value.ToString();
            //string? sFine = command.Data.Options.FirstOrDefault(x => x.Name == "fine").Value.ToString();
            //string? sNotes = command.Data.Options.FirstOrDefault(x => x.Name == "notes").Value.ToString();

            Console.WriteLine("Dickhead " + sDiscordUsername + " has been blacklisted");

            //await GoogleSheetsDataWriter.WriteToFreeBeerRosterDatabase(sDiscordUsername.ToString(), sDiscordNickname, sReason, sFine, sNotes);
            //await command.Channel.SendMessageAsync(sDiscordUsername.ToString() + " has been blacklisted");
        }

        [SlashCommand("regear", "Submit a regear")]
        public async Task RegearSubmission(int EventID, string callerName)
        {

            AlbionAPIDataSearch eventData = new AlbionAPIDataSearch();
            RegearModule regearModule = new RegearModule();
            var guildUser = (SocketGuildUser)Context.User;
            var interaction = Context.Interaction as IComponentInteraction;

            PlayerEventData = await eventData.GetAlbionEventInfo(EventID);
            //dataBaseService = new DataBaseService();

            //await dataBaseService.AddPlayerInfo(new Player // USE THIS FOR THE REGISTERING PROCESS
            //{
            //    PlayerId = PlayerEventData.Victim.Id,
            //    PlayerName = PlayerEventData.Victim.Name
            //});

            if (regearModule.CheckIfPlayerHaveReGearIcon(Context))
            {
                var moneyType = (MoneyTypes)Enum.Parse(typeof(MoneyTypes), "ReGear");

                string? sUserNickname = ((Context.User as SocketGuildUser).Nickname != null) ? (Context.User as SocketGuildUser).Nickname : Context.User.Username;

                if (PlayerEventData.Victim.Name.ToLower() == sUserNickname.ToLower() || guildUser.Roles.Any(r => r.Name == "AO - Officers"))
                {
                    if (PlayerEventData.groupMemberCount >= 20 && PlayerEventData.BattleId != PlayerEventData.EventId)
                    {
                        await regearModule.PostRegear(Context, PlayerEventData, callerName, "ZVZ content", moneyType);

                    }
                    else if (PlayerEventData.groupMemberCount <= 20 && PlayerEventData.BattleId != PlayerEventData.EventId)
                    {
                        await regearModule.PostRegear(Context, PlayerEventData, callerName, "Small group content", moneyType);

                    }
                    else if (PlayerEventData.BattleId == 0)
                    {
                        await regearModule.PostRegear(Context, PlayerEventData, callerName, "Solo or small group content", moneyType);

                    }

                }
                else
                {
                    await ReplyAsync($"<@{Context.User.Id}>. You can't submit regears on the behalf of {PlayerEventData.Victim.Name}. Ask an Officer if there's an issue. ");
                }
            }
            else
            {
                await ReplyAsync("You do not have regear roles or permissions to post a regear");
            }
            //if (FromButton)
            //{
            //    var moneyType = (MoneyTypes)Enum.Parse(typeof(MoneyTypes), "");
            //    await PostRegearException(command, eventData, "", "", moneyType);
            //}
        }
        [ComponentInteraction("deny")]
        public async Task Denied()
        {
            var guildUser = (SocketGuildUser)Context.User;
            var interaction = Context.Interaction as IComponentInteraction;

            int killId = Convert.ToInt32(interaction.Message.Embeds.FirstOrDefault().Fields[5].Value);

            if (guildUser.Roles.Any(r => r.Name == "AO - REGEARS" || r.Name == "AO - Officers"))
            {


                await RespondAsync("Regear Denied", null, false, false, null, null, null, null);
                await _logger.Log(new LogMessage(LogSeverity.Info, "Regear Denied", $"User: {Context.User.Username}, Command: regear", null));

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
            int refundAmount = Convert.ToInt32(interaction.Message.Embeds.FirstOrDefault().Fields[4].Value);
            string victimName = interaction.Message.Embeds.FirstOrDefault().Fields[1].Value.ToString();
            int killId = Convert.ToInt32(interaction.Message.Embeds.FirstOrDefault().Fields[5].Value);

            AlbionAPIDataSearch eventData = new AlbionAPIDataSearch();
            PlayerEventData = await eventData.GetAlbionEventInfo(killId);

            if (guildUser.Roles.Any(r => r.Name == "AO - REGEARS" || r.Name == "AO - Officers"))
            {
                await GoogleSheetsDataWriter.WriteToRegearSheet(Context, PlayerEventData, refundAmount);
                await Context.Channel.DeleteMessageAsync(interaction.Message.Id);
                await Context.Channel.SendMessageAsync($"@{victimName} your regear has been approved! {refundAmount} has been added to your paychex");
            }
            else
            {
                await RespondAsync($"Just because the button is green <@{Context.User.Id}> doesn't mean you can press it. Bug off.");
            }
        }
    }
}
