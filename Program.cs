﻿using CoreHtmlToImage;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.Net;
using Discord.WebSocket;
using DiscordBot.Extension;
using DiscordBot.Models;
using DiscordBot.Services;
using MarketData;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PlayerData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using static AlbionOnlineDataParser.AlbionOnlineDataParser;
using Color = Discord.Color;
using AlbionData.Models;
using DiscordBot.Enums;
using GoogleSheetsData;
using System.Configuration;

namespace FreeBeerBot
{
    public class Program : InteractionModuleBase<SocketInteractionContext>
    {
        private DiscordSocketClient _client;
        private SocketGuildUser _user;
        private DataBaseService dataBaseService;
        private int TotalRegearSilverAmount { get; set; }
        private PlayerDataHandler.Rootobject PlayerEventData { get; set;}

        ulong GuildID = ulong.Parse(ConfigurationManager.AppSettings.Get("guildID"));

        public static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            //TODO: Redo then entire command handler system. This one can accidently make the but run twice in the loop
            //dataBaseService.AddSeedingData();
            _client = new DiscordSocketClient();

            _client.Ready += Client_Ready;
            _client.SlashCommandExecuted += SlashCommandHandler;
            _client.MessageReceived += CommandHandler;
            _client.ButtonExecuted += ButtonHandler;

            _client.ModalSubmitted += async modal =>
            {
                // Get the values of components.
                List<SocketMessageComponentData> components =
                    modal.Data.Components.ToList();
                string denyreason = components
                    .First(x => x.CustomId == "deny_reason").Value;

                // Build the message to send.
                string message = "hey @everyone; I just learned " +
                    $"{denyreason}";

                // Specify the AllowedMentions so we don't actually ping everyone.
                AllowedMentions mentions = new AllowedMentions();
                mentions.AllowedTypes = AllowedMentionTypes.Users;

                // Respond to the modal.
                await modal.RespondAsync(message, allowedMentions: mentions);
            };

            _client.Log += Log;// TODO: Switch to use the logger module.

            await _client.LoginAsync(TokenType.Bot, ConfigurationManager.AppSettings.Get("discordBotToken"));
            await _client.StartAsync();

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private Task CommandHandler(SocketMessage message)
        {
            string command = "";
            int lengthOfCommand = -1;
            string[] instultsList = { "You suck at Albion", "Votel is better than you" };
            //filtering messages begin here
            if (!message.Content.StartsWith('!')) //This is your prefix
                return Task.CompletedTask;

            if (message.Author.IsBot) //This ignores all commands from bots
                return Task.CompletedTask;

            if (message.Content.Contains(' '))
                lengthOfCommand = message.Content.IndexOf(' ');
            else
                lengthOfCommand = message.Content.Length;

            command = message.Content.Substring(1, lengthOfCommand - 1).ToLower();

           
            switch (command)
            {
                case "botsays":
                    string restofmessage = message.Content.Remove(0, message.Content.IndexOf(' ') + 1);
                    message.Channel.DeleteMessageAsync(message.Id);
                    message.Channel.SendMessageAsync($@"{restofmessage}");

                    break;
                case "insult":
                    message.Channel.SendMessageAsync($@"Command Online {message.Author.Mention}");
                    break;
            }

            return Task.CompletedTask;
        }

        public async Task Client_Ready()
        {
            var guild = _client.GetGuild(GuildID);

            var global = _client.GetGlobalApplicationCommandsAsync();
            //_client.Rest.DeleteAllGlobalCommandsAsync(); //USE TO DELETE ALL GLOBAL COMMANDS
            //guild.DeleteApplicationCommandsAsync(); //USE TO DELETE ALL GUILD COMMANDS

            var guildCommand = new SlashCommandBuilder();

            guildCommand
                .WithName("regear")
                .WithDescription("Submit a regear")
                .AddOption("killnumber", ApplicationCommandOptionType.Integer, "Killboard ID", isRequired: true);
            await guild.CreateApplicationCommandAsync(guildCommand.Build());

            guildCommand = new SlashCommandBuilder();
            guildCommand
                .WithName("recent-deaths")
                .WithDescription("View recent deaths");
            await guild.CreateApplicationCommandAsync(guildCommand.Build());


            guildCommand = new SlashCommandBuilder();
            guildCommand
                .WithName("componets")
                .WithDescription("test button and menu");
            await guild.CreateApplicationCommandAsync(guildCommand.Build());

            //guildCommand = new SlashCommandBuilder();
            //guildCommand
            //    .WithName("register")
            //    .WithDescription("Register new member to guild");
            //await guild.CreateApplicationCommandAsync(guildCommand.Build());

            guildCommand = new SlashCommandBuilder();
            guildCommand
                .WithName("blacklist")
                .WithDescription("Put someone on the shit list")
                .AddOption("discordusername", ApplicationCommandOptionType.Mentionable, "Discord username", false)
                .AddOption("ingame-name", ApplicationCommandOptionType.String, "In-game name or discord nickname", true)
                .AddOption("reason", ApplicationCommandOptionType.String, "Reason for being blacklisted.", false)
                .AddOption("fine", ApplicationCommandOptionType.String, "What's the fine?", false)
                .AddOption("notes", ApplicationCommandOptionType.String, "Additional notes on blacklist?", false);


            await guild.CreateApplicationCommandAsync(guildCommand.Build());

            //guildCommand = new SlashCommandBuilder();
            //guildCommand
            //    .WithName("view-paychex")
            //    .WithDescription("Put someone on the shit list");
            //await guild.CreateApplicationCommandAsync(guildCommand.Build());

            try
            {
                await _client.Rest.CreateGuildCommand(guildCommand.Build(), GuildID);
                await guild.CreateApplicationCommandAsync(guildCommand.Build());
            }
            catch (ApplicationCommandException exception)
            {
                var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);
                Console.WriteLine(json);
            }
        }

        private async Task SlashCommandHandler(SocketSlashCommand command)
        {
            //LIST OF COMMANDS

            switch (command.Data.Name)
            {
                case "blacklist":
                    //only recruiters and officers can use this command
                    await Task.Run(() => {BlacklistPlayer(command); });
                    break;
                //case "register":
                //    //only recruiters and officers can use this command
                //    InitializeClient();
                //    await Task.Run(() => { GetAlbionEventInfo(command); });
                //    //background check method
                //    //if playerblackgroundcheck == good {write them into database} else{send message back on the red flags that show up.}
                //    Console.Write("Registering player");
                    break;
                case "regear":
                    InitializeClient();
                    await Task.Run(() => { RegearSubmission(command); });
                    Console.Write("Regear complete");
                    break;
                case "recent-deaths":
                    InitializeClient();
                    await Task.Run(() => { GetRecentDeaths(command); });
                    break;
                case "view-paychex":
                    //await Task.Run(() => { GetSumOfCurrentWeekPaychex(command); });
                    break;

                case "componets":
                   //await HandleComponetCommand(command);
                   //this is a debug command
                    break;
            }
        }

        public async Task ButtonHandler(SocketMessageComponent component)
        {
            var guildUser = (SocketGuildUser)component.User;
            switch (component.Data.CustomId)
            {
                case "approve":
                   
                    if (guildUser.Roles.Any(r => r.Name == "AO - REGEARS" || r.Name == "AO - Officers"))
                    {
                        await GoogleSheetsDataWriter.WriteToRegearSheet(component, PlayerEventData, TotalRegearSilverAmount);
                        await component.Channel.DeleteMessageAsync(component.Message.Id);
                        await component.Channel.SendMessageAsync($"<@{component.User.Id}> your regear has been approved!" + Environment.NewLine + $"{TotalRegearSilverAmount} has been added to your paychex");
                    }
                    break;
                case "deny":
                    await RegearDenied(component);
                    break;
                case "exception":
                    if (guildUser.Roles.Any(r => r.Name == "AO - Officers"))
                    {
                        await component.RespondAsync($"Regear is approved by officer!");
                        await GoogleSheetsDataWriter.WriteToRegearSheet(component, PlayerEventData, TotalRegearSilverAmount);
                    }
                    else
                    {
                        await component.RespondAsync($"Access Denied: Only a officer can use this function!");
                    } 
                    break;
            }
        }
        public async Task RegearDenied(SocketMessageComponent component)
        {
            //Check 

            //var mb = new ModalBuilder()
            //.WithTitle("Regear Denied")
            //.WithCustomId("deny_menu")
            //.AddTextInput("Reason", "deny_reason", TextInputStyle.Paragraph, "Why is regear denied?");

            //await component.RespondWithModalAsync(mb.Build());

            await component.RespondAsync($"@{component.Message.Embeds.FirstOrDefault().Fields.FirstOrDefault().Value} Regear Denied", null, false, false, null, null, null, null);
            await component.Channel.DeleteMessageAsync(component.Message.Id);


        }
        private async void BlacklistPlayer(SocketSlashCommand command)
        {
            var sDiscordUsername = (SocketGuildUser)command.Data.Options.First().Value;
            string? sDiscordNickname = command.Data.Options.FirstOrDefault(x => x.Name == "ingame-name").Value.ToString();
            string? sReason = command.Data.Options.FirstOrDefault(x => x.Name == "reason").Value.ToString();
            string? sFine = command.Data.Options.FirstOrDefault(x => x.Name == "fine").Value.ToString();
            string? sNotes = command.Data.Options.FirstOrDefault(x => x.Name == "notes").Value.ToString();

            Console.WriteLine("Dickhead " + sDiscordUsername + " has been blacklisted");

            await GoogleSheetsDataWriter.WriteToFreeBeerRosterDatabase( sDiscordUsername.ToString(), sDiscordNickname, sReason, sFine, sNotes);
            await command.Channel.SendMessageAsync(sDiscordUsername.ToString() + " has been blacklisted");
        }

        public bool IsUserInDatabase()
        {
            return false;
        }

        [Command("recent-deaths")]
        public async void GetRecentDeaths(SocketSlashCommand command)
        {
            string? sPlayerData = null;
            string? sPlayerAlbionId = GetPlayerInfo(command).Result.Id; //either get from google sheet or search in albion API
            string? sUserNickname = ((command.User as SocketGuildUser).Nickname != null) ? (command.User as SocketGuildUser).Nickname : command.User.Username;

            int iDeathDisplayCounter = 1;
            int iVisibleDeathsShown = Int32.Parse(ConfigurationManager.AppSettings.Get("showDeathsQuantity")) - 1;  //can add up to 10 deaths //Add to config

            if (IsUserInDatabase())
            {
                //add user to database
            }
            else
            {
                PlayerLookupInfo test = await GetPlayerInfo(command);
                var albionID = test.Id;
            }

            using (HttpResponseMessage response = await AlbionOnlineDataParser.AlbionOnlineDataParser.ApiClient.GetAsync($"players/{sPlayerAlbionId}/deaths"))
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
                    await command.Channel.SendMessageAsync(null, false, embed.Build(),null ,null, null, component.Build(), null, null);
                }
                else
                {
                    throw new Exception(response.ReasonPhrase);
                }
            }
        }



        public async Task<PlayerDataHandler.Rootobject> GetAlbionEventInfo(SocketSlashCommand command)
        {
            string playerData = null;

            using (HttpResponseMessage response = await AlbionOnlineDataParser.AlbionOnlineDataParser.ApiClient.GetAsync($"events/{command.Data.Options.First().Value}"))
            {
                if (response.IsSuccessStatusCode)
                {
                    playerData = await response.Content.ReadAsStringAsync();
                }
                else
                {
                    throw new Exception(response.ReasonPhrase);
                }
            }

            var eventData = JsonConvert.DeserializeObject<PlayerDataHandler.Rootobject>(playerData);
            return eventData;
        }

        public async Task<PlayerLookupInfo> GetPlayerInfo(SocketSlashCommand command)
        {
            string? sPlayerData = null;
            string? sPlayerAlbionId = null; //either get from google sheet or search in albion API
            string? sUserNickname = ((command.User as SocketGuildUser).Nickname != null) ? (command.User as SocketGuildUser).Nickname : command.User.Username;
            PlayerLookupInfo returnValue = null;

            using (HttpResponseMessage response = await ApiClient.GetAsync($"search?q={sUserNickname}"))
            {
                if (response.IsSuccessStatusCode)
                {
                    sPlayerData = await response.Content.ReadAsStringAsync();
                    var parsedObjects = JObject.Parse(sPlayerData);
                    PlayersSearch playerSearchData = JsonConvert.DeserializeObject<PlayersSearch>(sPlayerData);

                    //USE THIS LOGIC TO CREATE METHOD TO ADD USER TO DATABASE
                    if (playerSearchData.players.FirstOrDefault() != null)
                    {
                        returnValue = playerSearchData.players.FirstOrDefault();
                        Console.WriteLine("Guild Nickname Matches Albion Username");
                    }
                    else
                    {
                        await command.Channel.SendMessageAsync("Hey idiot. Does your discord nickname match your in-game name?");
                    }
                }
                else
                {
                    throw new Exception(response.ReasonPhrase);
                }
            }
            return returnValue;
        }

        public async Task<string> GetMarketDataAndGearImg(SocketSlashCommand command, PlayerDataHandler.Equipment1 victimEquipment)
        {
            AlbionOnlineDataParser.AlbionOnlineDataParser.InitializeAlbionDataProject();

            int returnValue = 0;
            int returnNotUnderRegearValue = 0;
            string? sMarketLocation = ConfigurationManager.AppSettings.Get("chosenCityMarket"); //If field is null, all cities market data will be pulled
            bool bAddAllQualities = false;
            int iDefaultItemQuality = 2;



            string? head = (victimEquipment.Head != null) ? $"{victimEquipment.Head.Type + $"?Locations={sMarketLocation}&qualities=" + victimEquipment.Head.Quality }" : null;
            string? weapon = (victimEquipment.MainHand != null) ? $"{victimEquipment.MainHand.Type + $"?Locations={sMarketLocation}&qualities=" + victimEquipment.MainHand.Quality}" : null;
            string? offhand = (victimEquipment.OffHand != null) ? $"{victimEquipment.OffHand.Type + $"?Locations={sMarketLocation}&qualities=" + victimEquipment.OffHand.Quality}" : null;
            string? cape = (victimEquipment.Cape != null) ? $"{victimEquipment.Cape.Type + $"?Locations={sMarketLocation}&qualities=" + victimEquipment.Cape.Quality}" : null;
            string? armor = (victimEquipment.Armor != null) ? $"{victimEquipment.Armor.Type + $"?Locations={sMarketLocation}&qualities=" + victimEquipment.Armor.Quality}" : null;
            string? boots = (victimEquipment.Shoes != null) ? $"{victimEquipment.Shoes.Type + $"?Locations={sMarketLocation}&qualities=" + victimEquipment.Shoes.Quality}" : null;
            string? mount = (victimEquipment.Mount != null) ? $"{victimEquipment.Mount.Type + $"?Locations={sMarketLocation}&qualities=" + victimEquipment.Mount.Quality}" : null;

            var placeholder = "https://render.albiononline.com/v1/item/T1_WOOD.png";
            var headImg = (victimEquipment.Head != null) ? $"https://render.albiononline.com/v1/item/{victimEquipment.Head.Type + "?quality=" + victimEquipment.Head.Quality }" : placeholder;
            var weaponImg = (victimEquipment.MainHand != null) ? $"https://render.albiononline.com/v1/item/{victimEquipment.MainHand.Type + "?quality=" + victimEquipment.MainHand.Quality}" : placeholder;
            var offhandImg = (victimEquipment.OffHand != null) ? $"https://render.albiononline.com/v1/item/{victimEquipment.OffHand.Type + "?quality=" + victimEquipment.OffHand.Quality}" : placeholder;
            var capeImg = (victimEquipment.Cape != null) ? $"https://render.albiononline.com/v1/item/{victimEquipment.Cape.Type + "?quality=" + victimEquipment.Cape.Quality}" : placeholder;
            var armorImg = (victimEquipment.Armor != null) ? $"https://render.albiononline.com/v1/item/{victimEquipment.Armor.Type + "?quality=" + victimEquipment.Armor.Quality}" : placeholder;
            var bootsImg = (victimEquipment.Shoes != null) ? $"https://render.albiononline.com/v1/item/{victimEquipment.Shoes.Type + "?quality=" + victimEquipment.Shoes.Quality}" : placeholder;
            var mountImg = (victimEquipment.Mount != null) ? $"https://render.albiononline.com/v1/item/{victimEquipment.Mount.Type + "?quality=" + victimEquipment.Mount.Quality}" : placeholder;

            List<string> equipmentList = new List<string>();
            List<string> notUnderRegearEquipmentList = new List<string>();
            List<string> underRegearList = new List<string>();
            List<string> notUnderRegearList = new List<string>();
            List<string> notAvailableInMarketList = new List<string>();

            if (victimEquipment.Head.Type.Contains("T5") || victimEquipment.Head.Type.Contains("T6") || victimEquipment.Head.Type.Contains("T7") || victimEquipment.Head.Type.Contains("T8"))
            {
                if (victimEquipment.Head.Type.Contains("T5") && victimEquipment.Head.Type.Contains("@3"))
                {
                    equipmentList.Add(head);
                    underRegearList.Add(headImg);
                }
                else if (victimEquipment.Head.Type.Contains("T6") && (victimEquipment.Head.Type.Contains("@2") || victimEquipment.Head.Type.Contains("@3")))
                {
                    equipmentList.Add(head);
                    underRegearList.Add(headImg);
                }
                else if (victimEquipment.Head.Type.Contains("T7") && (victimEquipment.Head.Type.Contains("@1") || victimEquipment.Head.Type.Contains("@2")))
                {
                    equipmentList.Add(head);
                    underRegearList.Add(headImg);
                }
                else if (victimEquipment.Head.Type.Contains("T8"))
                {
                    equipmentList.Add(head);
                    underRegearList.Add(headImg);
                }
                else
                {
                    notUnderRegearEquipmentList.Add(head);
                    notUnderRegearList.Add(headImg);
                }
            }
            else
            {
                notUnderRegearEquipmentList.Add(head);
                notUnderRegearList.Add(headImg);
            }

            if (victimEquipment.MainHand.Type.Contains("T5") || victimEquipment.MainHand.Type.Contains("T6") || victimEquipment.MainHand.Type.Contains("T7") || victimEquipment.MainHand.Type.Contains("T8"))
            {
                if (victimEquipment.MainHand.Type.Contains("T5") && victimEquipment.MainHand.Type.Contains("@3"))
                {
                    equipmentList.Add(weapon);
                    underRegearList.Add(weaponImg);
                }
                else if (victimEquipment.MainHand.Type.Contains("T6") && (victimEquipment.MainHand.Type.Contains("@2") || victimEquipment.MainHand.Type.Contains("@3")))
                {
                    equipmentList.Add(weapon);
                    underRegearList.Add(weaponImg);
                }
                else if (victimEquipment.MainHand.Type.Contains("T7") && (victimEquipment.MainHand.Type.Contains("@1") || victimEquipment.MainHand.Type.Contains("@2")))
                {
                    equipmentList.Add(weapon);
                    underRegearList.Add(weaponImg);
                }
                else if (victimEquipment.MainHand.Type.Contains("T8"))
                {
                    equipmentList.Add(weapon);
                    underRegearList.Add(weaponImg);
                }
                else
                {
                    notUnderRegearEquipmentList.Add(weapon);
                    notUnderRegearList.Add(weaponImg);
                }
            }
            else
            {
                notUnderRegearEquipmentList.Add(weapon);
                notUnderRegearList.Add(weaponImg);
            }

            if (victimEquipment.OffHand != null)
            {
                if (victimEquipment.OffHand.Type.Contains("T5") || victimEquipment.OffHand.Type.Contains("T6") || victimEquipment.OffHand.Type.Contains("T7") || victimEquipment.OffHand.Type.Contains("T8"))
                {
                    if (victimEquipment.OffHand.Type.Contains("T5") && victimEquipment.OffHand.Type.Contains("@3"))
                    {
                        equipmentList.Add(offhand);
                        underRegearList.Add(offhandImg);
                    }
                    else if (victimEquipment.OffHand.Type.Contains("T6") && (victimEquipment.OffHand.Type.Contains("@2") || victimEquipment.OffHand.Type.Contains("@3")))
                    {
                        equipmentList.Add(offhand);
                        underRegearList.Add(offhandImg);
                    }
                    else if (victimEquipment.OffHand.Type.Contains("T7") && (victimEquipment.OffHand.Type.Contains("@1") || victimEquipment.OffHand.Type.Contains("@2")))
                    {
                        equipmentList.Add(offhand);
                        underRegearList.Add(offhandImg);
                    }
                    else if (victimEquipment.OffHand.Type.Contains("T8"))
                    {
                        equipmentList.Add(offhand);
                        underRegearList.Add(offhandImg);
                    }
                    else
                    {
                        notUnderRegearEquipmentList.Add(offhand);
                        notUnderRegearList.Add(offhandImg);
                    }
                }
                else
                {
                    notUnderRegearEquipmentList.Add(offhand);
                    notUnderRegearList.Add(offhandImg);
                }
            }

            if (victimEquipment.Armor.Type.Contains("T5") || victimEquipment.Armor.Type.Contains("T6") || victimEquipment.Armor.Type.Contains("T7") || victimEquipment.Armor.Type.Contains("T8"))
            {
                if (victimEquipment.Armor.Type.Contains("T5") && victimEquipment.Armor.Type.Contains("@3"))
                {
                    equipmentList.Add(armor);
                    underRegearList.Add(armorImg);
                }
                else if (victimEquipment.Armor.Type.Contains("T6") && (victimEquipment.Armor.Type.Contains("@2") || victimEquipment.Armor.Type.Contains("@3")))
                {
                    equipmentList.Add(armor);
                    underRegearList.Add(armorImg);
                }
                else if (victimEquipment.Armor.Type.Contains("T7") && (victimEquipment.Armor.Type.Contains("@1") || victimEquipment.Armor.Type.Contains("@2")))
                {
                    equipmentList.Add(armor);
                    underRegearList.Add(armorImg);
                }
                else if (victimEquipment.Armor.Type.Contains("T8"))
                {
                    equipmentList.Add(armor);
                    underRegearList.Add(armorImg);
                }
                else
                {
                    notUnderRegearEquipmentList.Add(armor);
                    notUnderRegearList.Add(armorImg);
                }
            }
            else
            {
                notUnderRegearEquipmentList.Add(armor);
                notUnderRegearList.Add(armorImg);
            }
            if (victimEquipment.Shoes.Type.Contains("T5") || victimEquipment.Shoes.Type.Contains("T6") || victimEquipment.Shoes.Type.Contains("T7") || victimEquipment.Shoes.Type.Contains("T8"))
            {
                if (victimEquipment.Shoes.Type.Contains("T5") && victimEquipment.Shoes.Type.Contains("@3"))
                {
                    equipmentList.Add(boots);
                    underRegearList.Add(bootsImg);
                }
                else if (victimEquipment.Shoes.Type.Contains("T6") && (victimEquipment.Shoes.Type.Contains("@2") || victimEquipment.Shoes.Type.Contains("@3")))
                {
                    equipmentList.Add(boots);
                    underRegearList.Add(bootsImg);
                }
                else if (victimEquipment.Shoes.Type.Contains("T7") && (victimEquipment.Shoes.Type.Contains("@1") || victimEquipment.Shoes.Type.Contains("@2")))
                {
                    equipmentList.Add(boots);
                    underRegearList.Add(bootsImg);
                }
                else if (victimEquipment.Shoes.Type.Contains("T8"))
                {
                    equipmentList.Add(boots);
                    underRegearList.Add(bootsImg);
                }
                else
                {
                    notUnderRegearEquipmentList.Add(boots);
                    notUnderRegearList.Add(bootsImg);
                }
            }
            else
            {
                notUnderRegearEquipmentList.Add(boots);
                notUnderRegearList.Add(bootsImg);
            }
            if (victimEquipment.Cape.Type.Contains("T4") || victimEquipment.Cape.Type.Contains("T5") || victimEquipment.Cape.Type.Contains("T6") || victimEquipment.Cape.Type.Contains("T7") || victimEquipment.Cape.Type.Contains("T8"))
            {
                if (victimEquipment.Cape.Type.Contains("T4") && victimEquipment.Cape.Type.Contains("@3"))
                {
                    equipmentList.Add(cape);
                    underRegearList.Add(capeImg);
                }
                else if (victimEquipment.Cape.Type.Contains("T5") && (victimEquipment.Cape.Type.Contains("@2") || victimEquipment.Cape.Type.Contains("@3")))
                {
                    equipmentList.Add(cape);
                    underRegearList.Add(capeImg);
                }
                else if (victimEquipment.Cape.Type.Contains("T6") && (victimEquipment.Cape.Type.Contains("@1") || victimEquipment.Cape.Type.Contains("@2") || victimEquipment.Cape.Type.Contains("@3")))
                {
                    equipmentList.Add(cape);
                    underRegearList.Add(capeImg);
                }
                else if (victimEquipment.Cape.Type.Contains("T7"))
                {
                    equipmentList.Add(cape);
                    underRegearList.Add(capeImg);
                }
                else if (victimEquipment.Cape.Type.Contains("T8"))
                {
                    equipmentList.Add(cape);
                    underRegearList.Add(capeImg);
                }
                else
                {
                    notUnderRegearEquipmentList.Add(cape);
                    notUnderRegearList.Add(capeImg);
                }
            }
            else
            {
                notUnderRegearEquipmentList.Add(cape);
                notUnderRegearList.Add(capeImg);
            }
            equipmentList.Add(mount);
            underRegearList.Add(mountImg);

            //https://www.albion-online-data.com/api/v2/stats/prices/T4_MAIN_ROCKMACE_KEEPER@3.json?locations=Martlock&qualities=4 brought back only 1
            //https://www.albion-online-data.com/api/v2/stats/prices/T4_MAIN_ROCKMACE_KEEPER@3?Locations=Martlock brought back all qualities

            //MarketResponse testMarketdata = new MarketResponse() // THIS IS THE CONSTRUCTORS TO THE AlbionData.MODELS
            //AVERGE PRICE TESTING

            //T6_2H_MACE@1
            //// Root myDeserializedClass = JsonConvert.DeserializeObject<List<Root>>(myJsonResponse);

            //SUDO
            //IF Market entry is zero change quality. If still zero send message back to user to update the market with the https://www.albion-online-data.com/ project and update the market items

            string jsonMarketData = null;
            string jsonMarketData2 = null;

            foreach (var item in equipmentList)
            {
                if (item != null)
                {
                    using (HttpResponseMessage response = await ApiAlbionDataProject.GetAsync(item))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            jsonMarketData = await response.Content.ReadAsStringAsync();
                        }
                        else
                        {
                            throw new Exception(response.ReasonPhrase);
                        }
                    }
                    var marketData = JsonConvert.DeserializeObject<List<EquipmentMarketData>>(jsonMarketData);


                    if (marketData.FirstOrDefault().sell_price_min != 0)
                    {
                        returnValue += marketData.FirstOrDefault().sell_price_min; //CHANGE TO AVERAGE SELL PRICE
                    }
                    else
                    {
                        notAvailableInMarketList.Add(marketData.FirstOrDefault().item_id.Replace('_', ' ').Replace('@', '.'));
                    }
                }

            }
            foreach (var item in notUnderRegearEquipmentList)
            {
                if (item != null)
                {
                    using (HttpResponseMessage response = await ApiAlbionDataProject.GetAsync(item))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            jsonMarketData2 = await response.Content.ReadAsStringAsync();
                        }
                        else
                        {
                            throw new Exception(response.ReasonPhrase);
                        }
                    }

                    var marketData = JsonConvert.DeserializeObject<List<EquipmentMarketData>>(jsonMarketData2);


                    if (marketData.FirstOrDefault().sell_price_min != 0)
                    {
                        returnNotUnderRegearValue += marketData.FirstOrDefault().sell_price_min; //CHANGE TO AVERAGE SELL PRICE
                    }
                    else
                    {
                        notAvailableInMarketList.Add(marketData.FirstOrDefault().item_id.Replace('_', ' ').Replace('@', '.'));
                    }
                }
            }

            

#if DEBUG
            Console.WriteLine("Mode=Debug");
#endif
            var guildUser = (SocketGuildUser)command.User;

            if (guildUser.Roles.Any(r => r.Name == "Silver Tier Regear - Elligible")) //ROLE ID 1031731037149081630
            {
                returnValue = Math.Min(1300000, returnValue);
            }
            else if (guildUser.Roles.Any(r => r.Name == "Gold Tier Regear - Elligible")) // Role ID 1031731127431479428
            {
                returnValue = returnValue = Math.Min(1700000, returnValue);
            }
            else
            {
                returnValue = returnValue = Math.Min(800000, returnValue);
            }

            TotalRegearSilverAmount = returnValue;

            var gearImage = "<div style='background-color: #c7a98f;'> <div> <center><h3>Regearable</h3>";
            foreach (var item in underRegearList)
            {
                gearImage += $"<img style='display: inline;width:100px;height:100px' src='{item}'/>";
            }
            gearImage += $"<div style:'text-align : right;'>Refund amt. : {returnValue}</div></center></div>";
            gearImage += $"<div><center><h3>Not Regearable</h3>";
            foreach (var item in notUnderRegearList)
            {
                gearImage += $"<img style='display: inline;width:100px;height:100px' src='{item}'/>";
            }
            gearImage += $"<div style:'text-align : right;'>Items Price : {returnNotUnderRegearValue}</div></center></div><center><br/><h3> Items not found or price is too high </h3>";
            foreach (var item in notAvailableInMarketList)
            {
                gearImage += $"{item}<br/>";
            }
            gearImage += $"</center></div>";
            //var img1 = $"<div style='width: auto'><img style='display: inline;width:100px;height:100px' src='{head}'/>";
            //var img2 = $"<img style='display: inline;width:100px;height:100px' src='{weapon}'/>";
            //var img3 = $"<img style='display: inline;width:100px;height:100px' src='{offhand}'/>";
            //var img4 = $"<img style='display: inline;width:100px;height:100px' src='{cape}'/>";
            //var img5 = $"<img style='display: inline;width:100px;height:100px' src='{armor}'/>";
            //var img6 = $"<img style='display: inline;width:100px;height:100px' src='{mount}'/>";
            //var img7 = $"<img style='display: inline;width:100px;height:100px' src='{boots}'/><div style:'text-align : right;'>Items Price : {gearPrice}</div></div>";

            //TODO: Add a selection to pick the cheapest item on the market if the quality is better (example. If regear submits a normal T6 Heavy mace and it costs 105k but there's a excellent quality for 100k. Submit the better quaility price

            //returnValue = marketData.FirstOrDefault().sell_price_min; 

            return gearImage;
        }
        public bool CheckIfPlayerHaveReGearIcon(SocketSlashCommand command)
        {
            var guildUser = (SocketGuildUser)command.User;

            if (guildUser.Roles.Any(r => r.Name == "Silver Tier Regear - Elligible" || r.Name == "Gold Tier Regear - Elligible"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        [SlashCommand("regear", "Submit a regear")]
        public async void RegearSubmission(SocketSlashCommand command)
        {
            PlayerDataHandler.Rootobject eventData = await GetAlbionEventInfo(command);
            PlayerEventData = eventData;
            // dataBaseService = new DataBaseService();

            //await dataBaseService.AddPlayerInfo(new Player // USE THIS FOR THE REGISTERING PROCESS
            //{
            //    PlayerId = eventData.Victim.Id,
            //    PlayerName = eventData.Victim.Name
            //});
#if DEBUG
            Console.WriteLine("Mode=Debug: Testing regear icon logic ");
#endif
            if (CheckIfPlayerHaveReGearIcon(command))
            {
                await PostRegear(command, eventData);
                //await GoogleSheetsDataWriter.WriteToRegearSheet(command, eventData, TotalRegearSilverAmount);
            }




        }

        public async Task PostRegear(SocketSlashCommand command, PlayerDataHandler.Rootobject eventData)
        {
            ulong id = ulong.Parse(ConfigurationManager.AppSettings.Get("regearTeamChannelId"));
            var chnl = _client.GetChannel(id) as IMessageChannel;

            var gearImg = await GetMarketDataAndGearImg(command, eventData.Victim.Equipment);

            try
            {
                var converter = new HtmlConverter();
                var html = gearImg;
                var bytes = converter.FromHtmlString(html);

                var approveButton = new ButtonBuilder()
                {
                    Label = "Approve",
                    CustomId = "approve",
                    Style = ButtonStyle.Success
                };
                var denyButton = new ButtonBuilder()
                {
                    Label = "Deny",
                    CustomId = "deny",
                    Style = ButtonStyle.Danger
                };
                var exceptionButton = new ButtonBuilder()
                {
                    Label = "Special Exception",
                    CustomId = "exception",
                    Style = ButtonStyle.Secondary,  
                };

                var component = new ComponentBuilder();
                component.WithButton(approveButton);
                component.WithButton(denyButton);
                component.WithButton(exceptionButton);

                using (MemoryStream imgStream = new MemoryStream(bytes))
                {
                    var embed = new EmbedBuilder()
                                    .WithTitle($"Regear Submission")
                                    .AddField("User submitted ", command.User.Username, true)
                                    .AddField("Victim", eventData.Victim.Name)
                                    .AddField("Killer", "[" + eventData.Killer.AllianceName + "] " + "[" + eventData.Killer.GuildName + "] " + eventData.Killer.Name)
                                    .AddField("Death Average IP", eventData.Victim.AverageItemPower)

                                    //.WithImageUrl(GearImageRenderSerivce(command))
                                    //.AddField(fb => fb.WithName("🌍 Location").WithValue("https://cdn.discordapp.com/attachments/944305637624533082/1026594623696678932/BAG_603948955.png").WithIsInline(true))
                                    .WithImageUrl($"attachment://image.jpg")
                                    .WithUrl($"https://albiononline.com/en/killboard/kill/{command.Data.Options.First().Value}");
                    await chnl.SendFileAsync(imgStream, "image.jpg", $"Regear Submission from {command.User}", false, embed.Build(), null, false, null, null, components: component.Build());

                    
                    //await chnl.SendMessageAsync("Regear Submission from....", false, embed.Build()); // 5
                    //build.WithThumbnailUrl("attachment://anyImageName.png"); //or build.WithImageUrl("")
                    //await Context.Channel.SendFileAsync(imgStream, "anyImageName.png", "", false, build.Build());
                    //command.RespondAsync();
                }

                //HandleComponetCommand(command);


            }
            catch (Exception ex)
            {
                throw;
            }
        }

        [SlashCommand("componets", "Demo of buttons")]
        public async Task HandleComponetCommand(SocketMessageComponent componenttest)
        {
            var button = new ButtonBuilder()
            {
                Label = "Button",
                CustomId = "button",
                Style = ButtonStyle.Primary
            };

            var menu = new SelectMenuBuilder()
            {
                CustomId = "menu",
                Placeholder = "Sample menu"
            };

            menu.AddOption("First option", "first");
            menu.AddOption("Second option", "second");

            var component = new ComponentBuilder();
            component.WithButton(button);
            component.WithSelectMenu(menu);


            await componenttest.RespondAsync("testing", components: component.Build());
            
        }

        [ComponentInteraction("button")]
        public async Task HandleButtonInput()
        {
            await RespondWithModalAsync<DemoModal>("demo-modal");
        }

        [ComponentInteraction("menu")]
        public async Task HandleMenuSelection(string[] inputs)
        {
            await RespondAsync(inputs[0]);
        }

        [ModalInteraction("demo_modal")]
        public async Task HandleModalInput(DemoModal modal)
        {
            string input = modal.Greeting;
            await RespondWithModalAsync<DemoModal> ("demo_modal");
        }
    }

    public class DemoModal : IModal
    {
        public string Title => "Demo Modal";
        [InputLabel("Send a greeting!")]
        [ModalTextInput("greeting_input", TextInputStyle.Short, placeholder: "Be nice...", maxLength: 100)]
        public string Greeting { get; set; }
    }
}