using Aspose.Words.Fields;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using GoogleSheetsData;
using PlayerData;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using CommandModule;
using System.Linq;

namespace InteractionHandlerService
{
    public class InteractionHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _commands;
        private readonly IServiceProvider _services;
        private ulong HQMiniMarketChannelID = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("HQMiniMarketChannelID"));

        private ulong ChannelThreadId { get; set; }
        // Using constructor injection
        public InteractionHandler(DiscordSocketClient client, InteractionService commands, IServiceProvider services)
        {
            _client = client;
            _commands = commands;
            _services = services;
        }

        public async Task InitializeAsync()
        {
            // Add the public modules that inherit InteractionModuleBase<T> to the InteractionService
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);

            // Process the InteractionCreated payloads to execute Interactions commands
            _client.InteractionCreated += HandleInteraction;
            _client.ThreadCreated += ThreadCreationExecuted;
            _client.UserJoined += UserJoinedGuildExecuted;
            _client.UserLeft += UserLeftGuildExecuted;

            // Process the command execution results 
            _commands.SlashCommandExecuted += SlashCommandExecuted;
            _commands.ContextCommandExecuted += ContextCommandExecuted;
            _commands.ComponentCommandExecuted += ComponentCommandExecuted;
            _commands.ModalCommandExecuted += ModalCommandExecuted;

        }

        private Task ComponentCommandExecuted(ComponentCommandInfo arg1, Discord.IInteractionContext arg2, IResult arg3)
        {
            return Task.CompletedTask;
        }

        private Task ContextCommandExecuted(ContextCommandInfo arg1, Discord.IInteractionContext arg2, IResult arg3)
        {
            return Task.CompletedTask;
        }

        private Task SlashCommandExecuted(SlashCommandInfo arg1, Discord.IInteractionContext arg2, IResult arg3)
        {
            return Task.CompletedTask;
        }
        private Task ModalCommandExecuted(ModalCommandInfo arg1, Discord.IInteractionContext arg2, IResult arg3)
        {
            return Task.CompletedTask;
        }

        private async Task UserJoinedGuildExecuted(SocketGuildUser SocketGuildUser)
        {
            var lobbyChannel = _client.GetChannel(ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("LobbyChannelID"))) as IMessageChannel;

            Random rnd = new Random();

            List<string> insultList = new List<string>
            {
                $"<@{SocketGuildUser.Id}> Welcome to Free Beer ya shmuck.",
                $"Sorry <@{SocketGuildUser.Id}>, if your here for the free beer we're fresh out.",
                $"Hi <@{SocketGuildUser.Id}>! If your looking to spy on us, please submit an app in <#880611767393345548>",
                $"Dominoes pizza, you spank it, we bank it.",
                $"<@{SocketGuildUser.Id}> Welcome to Free Beer!"
            };

            int r = rnd.Next(insultList.Count);
            await lobbyChannel.SendMessageAsync((string)insultList[r]);
        }

        private async Task UserLeftGuildExecuted(SocketGuild SocketGuild, SocketUser SocketUser)
        {
            
            
            var lobbyChannel = _client.GetChannel(ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("LobbyChannelID"))) as IMessageChannel;

            SocketGuildUser user = (SocketGuildUser)SocketUser;

            if(!SocketUser.IsBot && user.Roles.Any(x => x.Id == 9823749544))
            {
                //await CommandModule.CommandModule.Unregister(user.Username.ToString(), "Left guild or someone kicked because they were annoyed", user);
            }
           
            //<@{Context.User.Id}>

            await lobbyChannel.SendMessageAsync($"<@{SocketUser.Id}> / {SocketUser.Username} has left the server");
        }



        private async Task ThreadCreationExecuted(SocketThreadChannel arg)
        {
            if (arg.ParentChannel.Id == HQMiniMarketChannelID && ChannelThreadId != arg.Owner.Thread.Id)
            {
                string? sUserNickname = (arg.Owner.Nickname != null) ? arg.Owner.Nickname : arg.Owner.Username;
                if (sUserNickname.Contains("!sl"))
                {
                    sUserNickname = new PlayerDataLookUps().CleanUpShotCallerName(sUserNickname);
                }

                ChannelThreadId = arg.Owner.Thread.Id;
                string miniMarketCreditsTotal = GoogleSheetsDataWriter.GetMiniMarketCredits(sUserNickname);
                await arg.SendMessageAsync($"{sUserNickname} Mini market credits balance: {miniMarketCreditsTotal}");
            }
        }

        private async Task HandleInteraction(SocketInteraction arg)
        {
            try
            {
                // Create an execution context that matches the generic type parameter of your InteractionModuleBase<T> modules
                var ctx = new SocketInteractionContext(_client, arg);
                await _commands.ExecuteCommandAsync(ctx, _services);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                // If a Slash Command execution fails it is most likely that the original interaction acknowledgement will persist. It is a good idea to delete the original
                // response, or at least let the user know that something went wrong during the command execution.
                if (arg.Type == InteractionType.ApplicationCommand)
                    await arg.GetOriginalResponseAsync().ContinueWith(async (msg) => await msg.Result.DeleteAsync());
            }
        }
    }
}
