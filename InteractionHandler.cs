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
using static System.Net.WebRequestMethods;
using Aspose.Imaging.AsyncTask;
using Aspose.Imaging.ProgressManagement;

namespace InteractionHandlerService
{
    public class InteractionHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _commands;
        private readonly IServiceProvider _services;
        private ulong HQMiniMarketChannelID = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("HQMiniMarketChannelID")); 
        private ulong LootSplitChannelID = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("LootSplitChannelID"));

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
            //_client.ButtonExecuted += ButtonExecuted;

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

		//private async Task<Task> ButtonExecuted(SocketMessageComponent MessageComponet)
		//{
		//	return Task.CompletedTask;
		//}

		private async Task UserJoinedGuildExecuted(SocketGuildUser SocketGuildUser)
        {
            var lobbyChannel = _client.GetChannel(ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("LobbyChannelID"))) as IMessageChannel;
			PlayerLookupInfo? playerInfo = new PlayerLookupInfo();
			PlayerDataLookUps albionData = new PlayerDataLookUps();

            SocketInteractionContext test = null;
            try
            {
				playerInfo = await albionData.GetPlayerInfo(test, SocketGuildUser.DisplayName);
				if (playerInfo != null)
				{
					switch (playerInfo.KillFame)
					{
						case > 50000000:
							await lobbyChannel.SendMessageAsync("https://tenor.com/bpJX6.gif Free Beer is recruiting for you :kissing_heart:");
							break;

						case > 10000000:
							await lobbyChannel.SendMessageAsync($"Over 10 mil killfame <@{SocketGuildUser.Id}>. Your almost a banger!!! Welcome to Free Beer");
							break;

						case > 5000000:
							await lobbyChannel.SendMessageAsync("");
							break;
						case > 1000000:
							await lobbyChannel.SendMessageAsync("Hello and welcome to Free Beer. I'll break the ice with you first.... Application Denied!");
							break;
						case > 1:
							await lobbyChannel.SendMessageAsync($"<@{SocketGuildUser.Id}> You look to be fresh off the boat. What's up?");
							break;

						case 0:
							await lobbyChannel.SendMessageAsync("Hmmmmm. I think your a spy");
							break;
					}

				}
				else
				{
					Random rnd = new Random();

					List<string> insultList = new List<string>
				{
				$"<@{SocketGuildUser.Id}> Welcome to Free Beer ya shmuck.",
				$"Sorry <@{SocketGuildUser.Id}>, if your here for the free beer we're fresh out.",
				$"Hi <@{SocketGuildUser.Id}>! If your looking to spy on us, please submit an app in <#880611767393345548>",
				$"Dominoes pizza, you spank it, we bank it.",
				$"<@{SocketGuildUser.Id}> Welcome to Free Beer!",
				$"Hello <@{SocketGuildUser.Id}>. But just in case your here to talk shit. :middle_finger:",
				$"<@{ SocketGuildUser.Id}>. What's up homie? ",
				$"<@{SocketGuildUser.Id}>. Welcome. Do you ever feel like a plastic bag?",
				$"<@{ SocketGuildUser.Id}>. https://tenor.com/bxDCP.gif"
				};

					int r = rnd.Next(insultList.Count);
					await lobbyChannel.SendMessageAsync((string)insultList[r]);
				}

			}
            catch (Exception ex) { Console.WriteLine(ex); }
        }

        private async Task UserLeftGuildExecuted(SocketGuild SocketGuild, SocketUser SocketUser)
        {
            
            
            var lobbyChannel = _client.GetChannel(ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("LobbyChannelID"))) as IMessageChannel;

            SocketGuildUser user = (SocketGuildUser)SocketUser;

            if(!SocketUser.IsBot && user.Roles.Any(x => x.Id == 9823749544))
            {
                //await CommandModule.CommandModule.Unregister(user.Username.ToString(), "Left guild or someone kicked because they were annoyed", user);
            }
			Random probability = new Random();
			Random gifRandom = new Random();

			List<string> GoodByeList = new List<string>
			{
			    $"<@{SocketUser.Id}> / {SocketUser.Username} has left probably because they're sick of us",
			    $"<@{SocketUser.Id}> / {SocketUser.Username} has left. https://tenor.com/view/ok-bye-ok-bye-bye-ok-girl-bye-gif-18696870",
				$"<@{SocketUser.Id}> / {SocketUser.Username} has left. https://tenor.com/view/peace-out-later-bye-gif-14086405",
				$"<@{SocketUser.Id}> / {SocketUser.Username} has left. https://tenor.com/view/bye-slide-baby-later-peace-out-gif-19322436",
				$"<@{SocketUser.Id}> / {SocketUser.Username} has left. https://tenor.com/view/chris-tucker-bye-bish-gif-13500768",
				$"<@{SocketUser.Id}> / {SocketUser.Username} has left. https://tenor.com/view/i-wont-miss-them-at-all-i-wont-miss-them-matthew-rhys-i-dont-care-about-them-i-dont-care-gif-12663265",
				$"<@{SocketUser.Id}> / {SocketUser.Username} has left. https://tenor.com/view/see-ya-kick-woman-gif-11295867",
				$"<@{SocketUser.Id}> / {SocketUser.Username} has left. https://tenor.com/view/chris-tucker-bye-bish-gif-13500768",
				$"<@{SocketUser.Id}> / {SocketUser.Username} has left. https://tenor.com/view/bye-tata-ok-by-gif-gif-18973858",
				$"<@{SocketUser.Id}> / {SocketUser.Username} has left. https://tenor.com/view/rip-gif-19364920",
				$"<@{SocketUser.Id}> / {SocketUser.Username} has left. https://tenor.com/view/rip-rest-in-peace-rip-bozo-pour-one-out-homie-gif-22783396",
				$"<@{SocketUser.Id}> / {SocketUser.Username} has left. https://tenor.com/view/bye-tata-ok-by-gif-gif-18973858",
				$"<@{SocketUser.Id}> / {SocketUser.Username} rage quitted the server",
				$"<@{SocketUser.Id}> / {SocketUser.Username} left to avoid getting shit from us in that last fight.",
			};

			if(probability.NextDouble() < 0.3)
            {
				int r = gifRandom.Next(GoodByeList.Count);
				await lobbyChannel.SendMessageAsync((string)GoodByeList[r]);
			}
            else
            {
				await lobbyChannel.SendMessageAsync($"<@{SocketUser.Id}> / {SocketUser.Username} has left the server");
			}
			
		}

        

        private async Task ThreadCreationExecuted(SocketThreadChannel arg)
        {
			string? sUserNickname = (arg.Owner.DisplayName != null) ? arg.Owner.DisplayName : arg.Owner.Username;

			if (sUserNickname.Contains("!sl"))
			{
				sUserNickname = new PlayerDataLookUps().CleanUpShotCallerName(sUserNickname);
			}

			if (arg.ParentChannel.Id == HQMiniMarketChannelID && ChannelThreadId != arg.Owner.Thread.Id)
            {
                ChannelThreadId = arg.Owner.Thread.Id;
                string miniMarketCreditsTotal = GoogleSheetsDataWriter.GetMiniMarketCredits(sUserNickname);
                await arg.SendMessageAsync($"{sUserNickname} Mini market credits balance: {miniMarketCreditsTotal}");
            }
            else if (arg.ParentChannel.Id == LootSplitChannelID && ChannelThreadId != arg.Owner.Thread.Id)
			{
				ChannelThreadId = arg.Owner.Thread.Id;
				await arg.SendMessageAsync($"Don't forget to post info first before you run /Split-Loot");
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
