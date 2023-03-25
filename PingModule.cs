using Discord;
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

namespace DNet_V3_Tutorial
{
    // Must use InteractionModuleBase<SocketInteractionContext> for the InteractionService to auto-register the commands
    public class PingModule : InteractionModuleBase<SocketInteractionContext>
    {
        public InteractionService Commands { get; set; }
        private static Logger _logger;

        public PingModule(ConsoleLogger logger)
        {
            _logger = logger;
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
                .AddField(@"\get-player-info {REQUIRED: Player name}", "RECRUITERS AND OFFICERS ONLY Search Albion API for player info");


            // New LogMessage created to pass desired info to the console using the existing Discord.Net LogMessage parameters
            await _logger.Log(new LogMessage(LogSeverity.Info, "PingModule : Help", $"User: {Context.User.Username}, Help: help", null));

            await RespondAsync($"Bot Commands.", null, false, true, null, null, null, embed.Build());

        }

        [SlashCommand("ping", "Receive a reply!")]
        public async Task Ping(string message)
        {
            var channels = Context.Guild.Channels;
            var chnl = Context.Client.GetChannel(1036552362380251157) as IMessageChannel;


            // New LogMessage created to pass desired info to the console using the existing Discord.Net LogMessage parameters
            await _logger.Log(new LogMessage(LogSeverity.Info, "PingModule : Ping", $"User: {Context.User.Username}, Command: ping", null));
            // Respond to the user
            //await Context.Channel.SendMessageAsync();
            //await RespondAsync(message);
            await ReplyAsync(message);
            //await chnl.SendMessageAsync(message);
        }

        [SlashCommand("insult", "Receive a insult!")]
        public async Task Getinsult()
        {
            var chnl = Context.Client.GetChannel(739949855195267174) as IMessageChannel;
            Random rnd = new Random();


            List<string> insultList = new List<string>
            {
                $"Yeahhhhhhh bud. Votel is better than you....",
                $"Fuck you <@{Context.User.Id}> I made your Mom cum so hard that they made a Canadian heritage moment out of it and Don Mckellar played my dick",
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
                //$"Logged",
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
                //$"Directions"
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
                case "SuperBad":
                    await RespondAsync($"Let me tell you a secret <@{Context.User.Id}>...");
                    System.Threading.Thread.Sleep(2000);
                    await FollowupAsync("YOU SUCK", null, false, true, null, null, null, null);
                    break;
                case "Logged":
                    await Context.Guild.CurrentUser.AddRoleAsync(1004428809409409024);
                    await RespondAsync($"<@{Context.User.Id}>! Have the honors of getting LOGGED bitch!!!  <:logs:1008762793404682340> ");
                    System.Threading.Thread.Sleep(3000);
                    await FollowupAsync($"@here <@{Context.User.Id}>! has been LOGGED. SHAME THEM!!!!");
                    break;
                case "Paychex":
                    await RespondAsync($"<@{Context.User.Id}> Can I get my paychex?");
                    System.Threading.Thread.Sleep(3000);
                    await Context.User.SendMessageAsync($"Bro for real where my paychex at?");
                    break;
                case "Gif":
                    await RespondAsync("https://tenor.com/view/aqua-teen-hunger-force-carl-mooning-peek-a-boo-gif-17477491");
                    break;
                case "Directions":
                    await DeferAsync();
                    string? sUserNickname = ((Context.User as SocketGuildUser).Nickname != null) ? new PlayerDataLookUps().CleanUpShotCallerName((Context.User as SocketGuildUser).Nickname) : Context.User.Username;
                    string miniMarketCreditsTotal = GoogleSheetsDataWriter.GetMiniMarketCredits(sUserNickname);

                    await RespondAsync("I hear you can't follow directions. Lets put it to the test...");
                    await FollowupAsync($"I see you have {miniMarketCreditsTotal} mini-mart credits.");

                    var directionsButton = new ButtonBuilder()
                    {
                        Label = "DANGER DON'T PUSH!!!",
                        CustomId = "directions",
                        Style = ButtonStyle.Danger
                    };


                    var component = new ComponentBuilder();
                    component.WithButton(directionsButton);

                    break;
                //$"attachment://image.jpg"
                default:
                    await RespondAsync((string)insultList[r]);
                    break;
            }

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

        [ComponentInteraction("directions")]
        public async Task directionsButton()
        {
            await DeferAsync();
            string? sUserNickname = ((Context.User as SocketGuildUser).Nickname != null) ? new PlayerDataLookUps().CleanUpShotCallerName((Context.User as SocketGuildUser).Nickname) : Context.User.Username;
            string miniMarketCreditsTotal = GoogleSheetsDataWriter.GetMiniMarketCredits(sUserNickname);
            await ReplyAsync($"Donating 10% of your paychex to the Learn how to read foundation. {miniMarketCreditsTotal}");



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
    }
}
