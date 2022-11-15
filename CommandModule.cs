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
                                embed.AddField($"Death{iDeathDisplayCounter}", $"https://albiononline.com/en/killboard/kill/{searchDeaths[i]}", false);
                                //regearbutton.Label = $"Regear Death{iDeathDisplayCounter}"; //QOL Update. Allows members to start the regear process straight from the recent deaths list
                                //regearbutton.CustomId = searchDeaths[i].ToString();
                                //component.WithButton(regearbutton);

                                iDeathDisplayCounter++;
                            }
                        }
                        //await RespondAsync(null, null, false, true, null, null, component.Build(), embed.Build()); //Enable once buttons are working
                        await RespondAsync(null, null, false, false, null, null, null, embed.Build());

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
        public async Task RegearSubmission(int EventID)
        {
          
            AlbionAPIDataSearch eventData = new AlbionAPIDataSearch();
            RegearModule regearModule = new RegearModule();

            PlayerEventData = await eventData.GetAlbionEventInfo(EventID);
            //PlayerEventData = playerEventData;
            //dataBaseService = new DataBaseService();

            //await dataBaseService.AddPlayerInfo(new Player // USE THIS FOR THE REGISTERING PROCESS
            //{
            //    PlayerId = playerEventData.Victim.Id,
            //    PlayerName = playerEventData.Victim.Name
            //});

            if (regearModule.CheckIfPlayerHaveReGearIcon(Context))
            {
                var moneyType = (MoneyTypes)Enum.Parse(typeof(MoneyTypes), "");
                await regearModule.PostRegear(Context, PlayerEventData, "", "", moneyType);
                await GoogleSheetsDataWriter.WriteToRegearSheet(Context, PlayerEventData, regearModule.TotalRegearSilverAmount);
            }
            else
            {
                await ReplyAsync("You do not have regear roles to post a regear");
            }
            //if (FromButton)
            //{
            //    var moneyType = (MoneyTypes)Enum.Parse(typeof(MoneyTypes), "");
            //    await PostRegearException(command, eventData, "", "", moneyType);
            //}
        }

        
    }
}
