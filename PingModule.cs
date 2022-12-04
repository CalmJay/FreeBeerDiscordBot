using Discord;
using Discord.Interactions;
using DiscordbotLogging.Log;
using System;
using System.Collections.Generic;
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


        // Basic slash command. [SlashCommand("name", "description")]
        // Similar to text command creation, and their respective attributes
        [SlashCommand("ping", "Receive a pong!")]
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
            //var chnl = Context.Client.GetChannel(739949855195267174) as IMessageChannel;
            Random rnd = new Random();


            List<string> insultList = new List<string>
            { //$"Fuck you <@{Context.User.Id}> your mom keeps tryin' to slip a finger in my bum but I keep telling her that I only let Votels mom do that ya fuckin loser", /* rest of elements */ 
                $"Yeahhhhhhh bud. Votel is better than you....",
                $"Fuck you <@{Context.User.Id}> I made your Mom cum so hard that they made a Canadian heritage moment out of it and Don Mckellar played my dick",
                $"<@{Context.User.Id}>....I guess you prove that even god makes mistakes sometimes.",
                $"<@{Context.User.Id}> Last night I heard you went to a gathering CTA and MrAlbionOnline ganked you.",
                $"<@{Context.User.Id}>....Your family tree must be a cactus because everybody on it is a prick.",
                $"I hear Savage is recruiting <@{Context.User.Id}>. You meet their PVE expecations.",
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
                $"<@{Context.User.Id}> I checked your stats. I think your in the wrong guild? Here let me point you to the correct one. https://discord.com/invite/v7XCS9ZVaU",
                $"I'm not saying you're fat <@{Context.User.Id}>, but it looks like you were poured into your clothes and forgot to say when",
                $"You couldn't pour the water out of a boot if the instructions were written on the heel.",
                $"Everyone who's ever loved you was wrong.",
                $"Your mother should've swallowed you.",
                $"I would love to insult you but I'm afraid I won't do as well as nature did.",
                $"I envy the people that don't know you.",
                $"I find the fact that you've lived this long both surprising and disappointing."
            };

            int r = rnd.Next(insultList.Count);
            
            // New LogMessage created to pass desired info to the console using the existing Discord.Net LogMessage parameters
            await _logger.Log(new LogMessage(LogSeverity.Info, "Insult Time!!!", $"User: {Context.User.Username}, Command: insult", null));
            // Respond to the user

            switch((string)insultList[r])
            {
                case "TwoLiner":
                    await RespondAsync($"You’re my favorite person <@{Context.User.Id}>!");
                    System.Threading.Thread.Sleep(3000);
                    await FollowupAsync($"Besides every other person I’ve ever met.");
                    break;
                case "SuperBad":
                    await RespondAsync($"Hahahah Your gonna hate me. Lemme whisper you something <@{Context.User.Id}>!");
                    System.Threading.Thread.Sleep(2000);
                    await FollowupAsync("YOU FUCKING SUCK", null, false, true, null, null, null, null);
                    
                    break;


                default:
                    await RespondAsync((string)insultList[r]);
                    break;
            }
            

            
        }
    }
}
