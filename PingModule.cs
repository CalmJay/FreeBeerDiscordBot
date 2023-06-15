using Discord;
using DiscordBot.Enums;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordbotLogging.Log;
using GoogleSheetsData;
using PlayerData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using static System.Net.Mime.MediaTypeNames;
using System.Configuration;

namespace DNet_V3_Tutorial
{
    // Must use InteractionModuleBase<SocketInteractionContext> for the InteractionService to auto-register the commands
    public class PingModule : InteractionModuleBase<SocketInteractionContext>
    {
        public InteractionService Commands { get; set; }
        private static Logger _logger;

        public int goldTierRegearCap = int.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("GoldTierRegearPriceCap"));
        public int silverTierRegearCap = int.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("SilverTierRegearPriceCap"));
        public int bronzeTierRegearCap = int.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("BronzeTierRegearPriceCap"));
        public int shitTierRegearCap = int.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("DefaultTierSubmissionCap"));
        public int mountCap = int.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("MountPriceCap"));
        public int iTankMinimumIP = int.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("TankMinimumIP"));
        public int iDPSMinimumIP = int.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("DPSMinimumIP"));
        public int iHealerMinmumIP = int.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("HealerMinmumIP"));
        public int iSupportMinimumIP = int.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("SupportMinimumIP"));
        public int iTemporaryPeakRegearAdjustment = int.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("TemporaryPeakRegearAdjustment"));
        public int iTestSetting = int.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("TestSetting"));
        public static ulong TankMentorID = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("TankMentorID"));
        public static ulong HealerMentorID = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("HealerMentorID"));
        public static ulong DPSMentorID = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("DPSMentorID"));
        public static ulong SupportMentorID = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("SupportMentorID"));

        public PingModule(ConsoleLogger logger)
        {
            _logger = logger;
        }


        //// Simple slash command to bring up a message with a button to press
        //[SlashCommand("button", "message with button")]
        //public async Task BotConfiguationMenu()
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

        // This is the handler for the button created above. It is triggered by nmatching the customID of the button.
        [ComponentInteraction("button1")]
        public async Task ButtonHandler()
        {
            // try setting a breakpoint here to see what kind of data is supplied in a ComponentInteraction.
            var c = Context;
            await RespondAsync($"You pressed a button!");
        }



        // Simple slash command to bring up a message with a select menu
        [SlashCommand("configation", "Adjust bot settings")]
        public async Task MenuInput()
        {

            var components = new ComponentBuilder();
            var button = new ButtonBuilder()
            {
                Label = "Button",
                CustomId = "button1",
                Style = ButtonStyle.Primary
            };

            components.WithButton(button);

            //var mb = new ModalBuilder()
            //    .WithTitle("Fav Food")
            //    .WithCustomId("food_menu");

            //.AddTextInput("What??", "food_name", placeholder: "Pizza")
            //.AddTextInput("Why??", "food_reason", TextInputStyle.Paragraph,"Kus it's so tasty");


            //var embedFieldBuilter = new EmbedFieldBuilder()
            //    .WithName($"Regear Settings")
            //    .WithIsInline(false)
            //    .WithValue("Tank /n" +
            //    "healer /n" +
            //    "support /n" +
            //    "dps");


            //var embed = new EmbedBuilder()
            //.WithTitle("Free Beer Bot Menu")
            //.WithDescription("These are all the settings that control some of the main functions of the bot and regears")
            //.AddField("Main Menu", "description")
            //.AddField(embedFieldBuilter)
            //.AddField(embedFieldBuilter)
            //.AddField($">>> `Healer Regear Minimum IP` {iHealerMinmumIP}", "test", false)
            //.AddField(">>> `DPS Regear Minimum IP`", iDPSMinimumIP)
            //.AddField(">>> `Support Regear Minimum IP`", iSupportMinimumIP)
            //.AddField(">>> `Gold Tier Regear Cap`", goldTierRegearCap)
            //.AddField(">>> `Silver Tier Regear Cap`", silverTierRegearCap)
            //.AddField(">>> `Bronze Tier Regear Cap`", bronzeTierRegearCap)
            //.AddField(">>> `Shit Tier Regear Cap`", shitTierRegearCap)
            //.AddField(">>> `Mount cap`", mountCap)
            //.AddField("bold test", "**boldtest**")
            //.WithFooter("The Footer");

            //var components = new ComponentBuilder();
            //// A SelectMenuBuilder is created
            //var select = new SelectMenuBuilder()
            //{
            //    CustomId = "menu1",
            //    Placeholder = "Select something"
            //};

            //// Options are added to the select menu. The option values can be generated on execution of the command. You can then use the value in the Handler for the select menu
            //// to determine what to do next. An example would be including the ID of the user who made the selection in the value.
            //select.AddOption("abc", "abc_value");
            //select.AddOption("def", "def_value");
            //select.AddOption("ghi", "ghi_value");

            //components.WithSelectMenu(select);

            //await RespondAsync("This message has a menu!", components: components.Build(), embed: embed);

            //await Context.Interaction.RespondWithModalAsync(mb.Build());

            await RespondAsync($"Setting currently is: {iTestSetting}");

            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings.Add("TestSetting", "15");
            config.Save(ConfigurationSaveMode.Minimal);

            await FollowupAsync ($"Setting changed to: {iTestSetting}");
        }

        // SelectMenu interaction handler. This receives an array of the selections made.
        [ComponentInteraction("menu1")]
        public async Task MenuHandler(string[] selections)
        {
            // For the sake of demonstration, we only want the first value selected.
            await RespondAsync($"You selected {selections.First()}");
        }


        [SlashCommand("view-commands", "See list of commands")]
        public async Task DisplayCommands()
        {

            var embed = new EmbedBuilder()
                .WithTitle($" Bot Commands")
                .AddField(@"\help", "Displays commands and info about Free Beer Bot")
                .AddField(@"\recent-deaths", "Review your last 5 deaths and display KILL IDs")
                .AddField(@"\regear {REQUIRED: KILLID} {REQUIRED: Shotcaller} {REQUIRED: Event Type}", "Submit a regear for gear refund")
                .AddField(@"\regear-oc {REQUIRED: Item Codes} {REQUIRED: Shotcaller} {REQUIRED: Event Type}", "Submit OC breaks for gear refund")
                .AddField(@"\register {REQUIRED DiscordName},{REQUIRED InGameName}", "RECRUITERS AND OFFICERS ONLY: Registers user to Free Beer guild")
                .AddField(@"\blacklist {REQUIRED DiscordName},{REQUIRED InGameName}", "RECRUITERS AND OFFICERS ONLY: Add someone to the shit list")
                .AddField(@"\view-paychex", "View your current weeks running paychex balance.")
                .AddField(@"\transfer-paychex", "Transfer paychex to mini-mart credits.")
                .AddField(@"\get-player-info {REQUIRED: Player name}", "RECRUITERS AND OFFICERS ONLY Search Albion API for player info")
                .AddField(@"\register", "Add player to Database and regear system")
                .AddField(@"\unregister-member {REQUIRED: Player name} {REASON} {OPTIONAL: DiscordName}", "RECRUITERS AND OFFICERS ONLY Remove player from Free Beer Database")
                .AddField(@"\configure", "Adjust the bot settings")
                .AddField(@"\clear-songs", "Clears the song queue")
                .AddField(@"\stop-song", "Stops the current song")
                .AddField(@"\set-volumne", "Adjust the bot volume for music")
			    .AddField(@"\split-loot", "Does a split fothe reported loot then udpates paychex")
                .AddField(@"\give-regear", "Adjusts multipleuers regear status")
                .AddField(@"\insult", "Get an insult from the bot");
			// New LogMessage created to pass desired info to the console using the existing Discord.Net LogMessage parameters
			await _logger.Log(new LogMessage(LogSeverity.Info, "PingModule : Help", $"User: {Context.User.Username}, Help: help", null));

            await RespondAsync($"Bot Commands.", null, false, true, null, null, null, embed.Build());

        }

        [SlashCommand("ping", "Receive a reply!")]
        public async Task Ping(string message, SocketTextChannel channelName)
        {
            //await DeferAsync();
			//var channels = Context.Guild.Channels;
            var chnl = Context.Client.GetChannel(channelName.Id) as IMessageChannel;


            // New LogMessage created to pass desired info to the console using the existing Discord.Net LogMessage parameters
            await _logger.Log(new LogMessage(LogSeverity.Info, "PingModule : Ping", $"User: {Context.User.Username}, Command: ping", null));
            // Respond to the user
            //await Context.Channel.SendMessageAsync();
            //await RespondAsync(message);

            await chnl.SendMessageAsync(message);
			await RespondAsync($"Message has been sent too {channelName.Name}",null,false,true);
		}

        [SlashCommand("insult", "Receive a insult!")]
        public async Task Getinsult()
        {
            var chnl = Context.Client.GetChannel(739949855195267174) as IMessageChannel;
            Random rnd = new Random();
			string? sUserNickname = ((Context.User as SocketGuildUser).Nickname != null) ? new PlayerDataLookUps().CleanUpShotCallerName((Context.User as SocketGuildUser).Nickname) : Context.User.Username;

			List<string> insultList = new List<string>
            {
                $"Yeahhhhhhh bud. Voltel is better than you....",
                $"<@{Context.User.Id}>....I guess you prove that even god makes mistakes sometimes.",
                $"<@{Context.User.Id}> Last night I heard you went to a gathering CTA and MrAlbionOnline ganked you.",
                $"<@{Context.User.Id}>....Your family tree must be a cactus because everybody on it is a prick.",
                $"I hear ARCH is recruiting <@{Context.User.Id}>. You meet their PVE expecations.",
                $"https://www.youtube.com/watch?v=xfr64zoBTAQ&t=3s",
                $"I'd slap you but that'd be animal abuse",
                $"Don't worry bud. We saw you die in that 8.3 set to ARCH. That's why were now raising the requirements on IQ",
                $":middle_finger:",
                $"Jesus might love you <@{Context.User.Id}>, but everyone else definitely thinks you’re an idiot.",
                $"If you’re going to act like a turd, go lay on the yard.",
                $"Calling you an idiot would be an insult to all stupid people.",
                $"TwoLiner",
                $"SuperBad",
                $"Bum",
                $"<@{Context.User.Id}> I checked your stats. I think your in the wrong guild? Here let me point you to a spot I know a few that fit your caliber. https://discord.gg/archgayy",
                $"I'm not saying you're fat <@{Context.User.Id}>, but it looks like you were poured into your clothes and forgot to say when",
                $"You couldn't pour the water out of a boot if the instructions were written on the heel.",
                $"Everyone who's ever loved you was wrong.",
                $"Your mother should've swallowed you.",
                $"I would love to insult you but I'm afraid I won't do as well as nature did.",
                $"I envy the people that don't know you.",
                $"I find the fact that you've lived this long both surprising and disappointing.",
                $"Logged",
                $"HEY <@&930220030820515850>! You have some explaining to do. I wasn't the one that invited this shitter in here.",
                $"You have beautiful hair.",
                $"If free beer had a dick size requirement, you would be removed for inactivity",
                $"Jisungi died less then you",
                $"<@{Context.User.Id}> Regear denied, Reason: Skill Issue",
                $"You know all the shotcallers have you muted right?",
                $"Congratulations!!! You have found the mystery insult.",
                $"You should of bought a pair of Nutmollers boots.",
                $"Whoever told you to be yourself gave you bad advice",
                $"Paychex",
                $"Thanks for your opinion, no1 cares",
                $"Gif",
                //$"SlotMachine",
                $"You do realize we're just tolerating you, right?",
                $"It's all about balance… you start talking, I stop listening.",
                $"You're the reason this country has to put directions on shampoo bottles.",
                $"Don't worry… the first 40 years of childhood are always the hardest.",
                $"I was thinking about you today. It reminded me to take out the trash.",
                $"You are the human equivalent of a participation award",
                "You're about as useful as Anne Frank's drum kit",
                "You know what... Your awesome. Have a good day.",
                "I bet your eco is stealing the guild hammers",
                "Vearyx tells me your only here because you look pretty.",
                "OPENMIC",
                "I don't know what's more trashy. You or JesusEkber's ganks",
                "Let me guess... You like to play Death givers",
                "I have no balls but yet mine are still bigger than yours",
                "Yo, I need a bit of a break from free beer. I've been super frustrated for 80% of the fights these past couple months, and its making me not enjoy the game." +
                " I understand we have lots of new players, and the guild wants to train them, but thats just not the environment I wanna be in rn. I need to get the tryhard out of me. " +
                " I intend on coming back later (if you let me back in). Thanks for the past 10 months I spent here",
				"You act like your colon. You're full of shit.",
				$"PHATED",
				$"Directions"
            };
            int r = rnd.Next(insultList.Count);

            await _logger.Log(new LogMessage(LogSeverity.Info, "Insult Time!!!", $"User: {Context.User.Username}, Command: insult", null));

            switch ((string)insultList[r])
            {
                case "TwoLiner":
                    await RespondAsync($"You’re my favorite person <@{Context.User.Id}>!");
                    System.Threading.Thread.Sleep(3000);
                    await FollowupAsync($"Besides every other person I’ve ever met.");
                    break;
				case "PHATED":
                    if(sUserNickname.ToLower() == "phatedfool")
                    {
						await RespondAsync($"<@{Context.User.Id}> Holy shit bro. You spent more time spamming me than you do in our ZvZs :rofl: ");
					}
					await RespondAsync($"Your like a Little Cesars pizza. Good enough.");

					break;
				case "SuperBad":
                    await RespondAsync($"Let me tell you a secret <@{Context.User.Id}>...");
                    System.Threading.Thread.Sleep(2000);
                    await FollowupAsync("YOU SUCK", null, false, true, null, null, null, null);
                    break;
                case "Logged":
                    await Context.Guild.CurrentUser.AddRoleAsync(1004428809409409024);
                    await RespondAsync($"<@{Context.User.Id}> has stolen our logs!!!  <:logs:1008762793404682340> ");
                    System.Threading.Thread.Sleep(3000);
                    await FollowupAsync($"@here <@{Context.User.Id}> has been LOGGED. SHAME THEM!!!!");
                    break;
                case "Paychex":
                    await RespondAsync($"<@{Context.User.Id}> Can I get my paychex?");
                    System.Threading.Thread.Sleep(5000);
                    await Context.User.SendMessageAsync($"Bro for real... Where my paychex at?");
                    break;
                case "Gif":
                    await RespondAsync("https://tenor.com/view/aqua-teen-hunger-force-carl-mooning-peek-a-boo-gif-17477491");
                    break;
                case "Directions":
                    await DeferAsync();        
                    string miniMarketCreditsTotal = GoogleSheetsDataWriter.GetMiniMarketCredits(sUserNickname);

                    var directionsButton = new ButtonBuilder()
                    {
                        Label = "DANGER DON'T PUSH!!! YOU MAY LOSE MONEY",
                        CustomId = "directions",
                        Style = ButtonStyle.Danger
                    };
				
					var component = new ComponentBuilder();
                    component.WithButton(directionsButton);

                    await FollowupAsync("I hear you can't follow directions. Lets put it to the test...", null, false, false, null, null, component.Build(), null);
                    
                    break;
				case "OPENMIC":
                    await ReplyAsync($"<@{Context.User.Id}> You know what. The floor is open... I'll send an insult on your behalf");
					var mb = new ModalBuilder()
					.WithTitle("INSULT TIME")
					.WithCustomId("insult_menu");
					mb.AddTextInput("What hot trash would you like to say?", "reason", TextInputStyle.Paragraph, placeholder: "Enter some sort of trash talk here", required: false, value: null, maxLength: 500);

					await Context.Interaction.RespondWithModalAsync(mb.Build());
					Context.Client.ModalSubmitted += async modal =>
					{
						await modal.DeferAsync();

						string Reason = (modal.Data.Components.FirstOrDefault().Value != null || modal.Data.Components.FirstOrDefault().Value != "") ? modal.Data.Components.FirstOrDefault().Value : $"Nevermind. <@{Context.User.Id}> was too lazy to say shit.";

						await FollowupAsync(Reason);

					};
					break;
				//$"attachment://image.jpg"
				default:
                    await RespondAsync((string)insultList[r]);
                    break;
            }

            
        }

        [ComponentInteraction("directions")]
        public async Task directionsButton()
        {
            await DeferAsync();
            string? sUserNickname = ((Context.User as SocketGuildUser).Nickname != null) ? new PlayerDataLookUps().CleanUpShotCallerName((Context.User as SocketGuildUser).Nickname) : Context.User.Username;
            var miniMarketCreditsTotal = GoogleSheetsDataWriter.GetMiniMarketCredits(sUserNickname);
            //var convertedCredits = int.Parse(miniMarketCreditsTotal);

            int convertedCredits = int.Parse(miniMarketCreditsTotal.Replace(",", "").Replace("$", ""));

            await ReplyAsync($"Donating 10% of your paychex to the Free Beer Learn how to read foundation.");

            int freebeercut = Convert.ToInt32(Math.Floor(convertedCredits * .10));


            await GoogleSheetsDataWriter.MiniMartTransaction(Context.User as SocketGuildUser, Context.User as SocketGuildUser, freebeercut, MiniMarketType.Withdrawal);

            await FollowupAsync($"Your mini-mart balance is now {GoogleSheetsDataWriter.GetMiniMarketCredits(sUserNickname)}");


        }

        public int GetKillFame()
        {

            return 0;
        }

        public void WriteToCSV(List<string> UsersList)
        {
            var csv = new StringBuilder();
            foreach (var item in UsersList)
            {
                //string line = "Users Reacted";
                //csv.AppendLine(line);
                //line = string.Format(item.ToString());
                csv.AppendLine(string.Format(item.ToString()));
            }



            string fileName = @"C:\Repos\WriteText.csv";
            if (File.Exists(fileName))
                System.IO.File.AppendAllText(fileName, csv.ToString());
            else
                System.IO.File.WriteAllText(fileName, csv.ToString());
        }

        public async Task ServerScript()
        {
            //var message = await Context.Channel.GetMessageAsync(1079801885025914910);

            //var users = message.Reactions.Values;
            //IEmote emoji = Emoji.Parse(":thumbsup:");
            //RequestOptions options = new RequestOptions();

            ////var userslist = message.GetReactionUsersAsync(emoji, 300);

            //var temp = await (message.GetReactionUsersAsync(emoji, 300)).FlattenAsync();


            //List<string> usersreacted= new List<string>();
            //string? usernameCleanup = "";
            //foreach (var user in temp) 
            //{
            //    var userinfo = Context.Guild.GetUser(user.Id);
            //    if (userinfo != null)
            //    {
            //        usernameCleanup = (userinfo.Nickname != null) ? new PlayerDataLookUps().CleanUpShotCallerName(userinfo.Nickname) : userinfo.Username;
            //        usersreacted.Add(usernameCleanup);
            //    }
            //    else
            //    {
            //        usersreacted.Add(user.Username);
            //    }


            //}

            //WriteToCSV(usersreacted);
            //Console.WriteLine("Reactions grabbed");
        }
    }
}
