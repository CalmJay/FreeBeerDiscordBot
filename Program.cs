using CoreHtmlToImage;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.Net;
using Discord.WebSocket;
using DiscordBot.Extension;
using DiscordBot.Models;
using DiscordBot.Services;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
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
using System.Threading;
using System.Threading.Tasks;
using static AlbionOnlineDataParser.AlbionOnlineDataParser;
using Color = Discord.Color;

namespace FreeBeerBot
{
    public class Program : InteractionModuleBase<SocketInteractionContext>
    {
        private DiscordSocketClient _client;
        private DataBaseService dataBaseService;
        static string[] Scopes = { SheetsService.Scope.Spreadsheets };
        string SpreadsheetId = "1s-W9waiJx97rgFsdOHg602qKf-CgrIvKww_d5dwthyU"; //REAL SHEET //ADD TO CONFIG
        private const string GoogleCredentialsFileName = "credentials.json"; //ADD TO CONFIG
        //string sFreeBeerGuildAPIID = "9ndyGFTPT0mYwPOPDXDmSQ";

        static string ApplicationName = "Google Sheets API .NET Quickstart";
        public bool enableGoogleApi = true; //ADD TO CONFIG

        ulong GuildID = 157626637913948160;//CHANGE THIS TO THE OFFICAL SERVER WHEN DONE. //ADD TO CONFIG

        

        public static void Main(string[] args) => new Program().MainAsync().GetAwaiter().GetResult();

        

        public async Task MainAsync()
        {
            _client = new DiscordSocketClient();
            //_client.MessageReceived += CommandHandler;
            _client.Log += Log;

            _client.Ready += Client_Ready;
            _client.SlashCommandExecuted += SlashCommandHandler;
            //  You can assign your bot token to a string, and pass that in to connect.
            //  This is, however, insecure, particularly if you plan to have your code hosted in a public repository.
            var token = File.ReadAllText("token.txt");

            // Some alternative options would be to keep your token in an Environment Variable or a standalone file.
            // var token = Environment.GetEnvironmentVariable("NameOfYourEnvironmentVariable");
            // var token = File.ReadAllText("token.txt");
            // var token = JsonConvert.DeserializeObject<AConfigurationClass>(File.ReadAllText("config.json")).Token;



            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            // Block this task until the program is closed.
            await Task.Delay(-1);
            var services = new ServiceCollection();
            //string usercount = ConfigurationSettings.AppSettings["ConnectionString"];
            DependencyInjectionExtension.DependencyInjection(services);


        }


        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        //private Task CommandHandler(SocketMessage message)
        //{
        //    //variables
        //    string command = "";
        //    int lengthOfCommand = -1;

        //    //filtering messages begin here
        //    if (!message.Content.StartsWith('!')) //This is your prefix
        //        return Task.CompletedTask;

        //    if (message.Author.IsBot) //This ignores all commands from bots
        //        return Task.CompletedTask;

        //    if (message.Content.Contains(' '))
        //        lengthOfCommand = message.Content.IndexOf(' ');
        //    else
        //        lengthOfCommand = message.Content.Length;

        //    command = message.Content.Substring(1, lengthOfCommand - 1).ToLower();

        //    switch (command)
        //    {
        //        case "hello":
        //            message.Channel.SendMessageAsync($@"Hello {message.Author.Mention}");
        //            break;
        //        case "regear":
        //            message.Channel.SendMessageAsync($@"This is the regear shit {message.Author.Mention}");
        //            break;

        //    }

        //    return Task.CompletedTask;
        //}

        public async Task Client_Ready()
        {
            //USE GUILD COMMANDS FOR PRIVATE USE
            //GLOBAL COMMANDS ARE MORE FOR LARGE USER BASE USE (AKA IF THE BOT IS GOING TO BE USED IN A LOT OF DISCORD SERVERS)
            var guild = _client.GetGuild(GuildID); 

            var guildCommand = new SlashCommandBuilder();

            guildCommand
                .WithName("regear")
                .WithDescription("Submit a regear")
                .AddOption("killnumber", ApplicationCommandOptionType.Integer, "Killboard ID", isRequired: true);
            await guild.CreateApplicationCommandAsync(guildCommand.Build());

            guildCommand = new SlashCommandBuilder();
            guildCommand
                .WithName("get-recent-deaths")
                .WithDescription("View recent deaths");
            await guild.CreateApplicationCommandAsync(guildCommand.Build());

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
                case "list-roles":
                    await HandleListRoleCommand(command);
                    break;
                case "blacklist":
                    BlacklistPlayer(command);
                    break;
                case "register":
                    AlbionOnlineDataParser.AlbionOnlineDataParser.InitializeClient();
                    await GetAlbionEventInfo(command);
                    Console.Write("Registering player");
                    break;
                case "regear":
                    AlbionOnlineDataParser.AlbionOnlineDataParser.InitializeClient();
                    await Task.Run(() => { RegearSubmission(command); });
                    Console.Write("Regear complete");
                    break;
                case "get-recent-deaths":
                    AlbionOnlineDataParser.AlbionOnlineDataParser.InitializeClient();
                    await Task.Run(() => { GetRecentDeaths(command); });

                    break;
            }
        }

        private async Task HandleListRoleCommand(SocketSlashCommand command)
        {
            // We need to extract the user parameter from the command. since we only have one option and it's required, we can just use the first option.
            var guildUser = (SocketGuildUser)command.Data.Options.First().Value;

            // We remove the everyone role and select the mention of each role.
            var roleList = string.Join(",\n", guildUser.Roles.Where(x => !x.IsEveryone).Select(x => x.Mention));

            var embedBuiler = new EmbedBuilder()
                .WithAuthor(guildUser.ToString(), guildUser.GetAvatarUrl() ?? guildUser.GetDefaultAvatarUrl())
                .WithTitle("Roles")
                .WithDescription(roleList)
                .WithColor(Discord.Color.Green)
                .WithCurrentTimestamp();

            // Now, Let's respond with the embed.
            await command.RespondAsync(embed: embedBuiler.Build());
        }

        private async void BlacklistPlayer(SocketSlashCommand command)
        {
            var serviceValues = GetSheetsService().Spreadsheets.Values;
            var guildUser = (SocketGuildUser)command.Data.Options.First().Value;

            Console.WriteLine("Dickhead " + guildUser + " has been blacklisted");

            await WriteAsync(serviceValues, guildUser.ToString(), "THIS IS A TEST STRING.");


            await command.Channel.SendMessageAsync(guildUser.ToString() + " has been blacklisted");

        }

        public void ConnectToGoogleAPI()
        {
            try
            {
                UserCredential credential;
                // Load client secrets.
                using (var stream =
                       new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
                {
                    /* The file token.json stores the user's access and refresh tokens, and is created
                     automatically when the authorization flow completes for the first time. */
                    string credPath = "token.json";
                    credential = GoogleWebAuthorizationBroker.AuthorizeAsync(GoogleClientSecrets.FromStream(stream).Secrets, Scopes, "user", CancellationToken.None, new FileDataStore(credPath, true)).Result;
                    Console.WriteLine("Credential file saved to: " + credPath);
                }

                // Create Google Sheets API service.
                var service = new SheetsService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName
                });

                // Define request parameters.

                String range = "Free Beer blackList!A2:G";
                SpreadsheetsResource.ValuesResource.GetRequest request =
                    service.Spreadsheets.Values.Get(SpreadsheetId, range);

                // Prints the names and majors of students in a sample spreadsheet:
                // https://docs.google.com/spreadsheets/d/1BxiMVs0XRA5nFMdKvBdBZjgmUUqptlbs74OgvE2upms/edit
                ValueRange response = request.Execute();
                IList<IList<Object>> values = response.Values;
                if (values == null || values.Count == 0)
                {
                    Console.WriteLine("No data found.");
                    return;
                }

                Console.WriteLine("DiscordName, InGameName, Blacklisted, Reason, Date Recruited, DateLeftKicked, Notes");

                foreach (var row in values)
                {
                    // Print columns A and D, which correspond to indices 0 and 5.
                    Console.WriteLine("{0}, {1}, {2}, {3}, {4}, {5}, {6}", row[0], row[1], row[2], row[3], row[4], row[5], row[6]);
                }

            }
            catch (FileNotFoundException e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public string ReadRange { get; set; }
        public string WriteRange { get; set; }

        private static SheetsService GetSheetsService()
        {
            using (var stream = new FileStream(GoogleCredentialsFileName, FileMode.Open, FileAccess.Read))
            {
                string credPath = "token.json";
                var serviceInitializer = new BaseClientService.Initializer
                {
                    HttpClientInitializer = GoogleWebAuthorizationBroker.AuthorizeAsync(GoogleClientSecrets.FromStream(stream).Secrets, Scopes, "user", CancellationToken.None, new FileDataStore(credPath, true)).Result  //GoogleCredential.FromStream(stream).CreateScoped(Scopes)
                };
                return new SheetsService(serviceInitializer);
            }
        }
        private async Task ReadAsync(SpreadsheetsResource.ValuesResource valuesResource, string sReadrange)
        {
            var response = await valuesResource.Get(SpreadsheetId, sReadrange).ExecuteAsync();
            var values = response.Values;
            if (values == null || !values.Any())
            {
                Console.WriteLine("No data found.");
                return;
            }
            var header = string.Join(" ", values.First().Select(r => r.ToString()));
            Console.WriteLine($"Header: {header}");

            foreach (var row in values.Skip(1))
            {

                var res = string.Join(" ", row.Select(r => r.ToString()));
                Console.WriteLine(res);
            }
        }


        private async Task WriteAsync(SpreadsheetsResource.ValuesResource valuesResource, string a_SocketGuildUser, string a_sReason)
        {
            ////writing to spreadsheet
            //var rowValues = new ValueRange { Values = new List<IList<object>> { new List<object> { a_SocketGuildUser, "NOT AVALIABLE", "TRUE", a_sReason, DateTime.Now.ToString("M/d/yyyy") } } };
            //var update = valuesResource.Update(rowValues, SpreadsheetId, WriteRange); 
            //update.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;

            var col1 = 2;
            var col2 = 2;
            ReadRange = $"Copy of Free Beer BlackList!A{col1}";
            WriteRange = $"Copy of Free Beer BlackList!A{col1}:G{col2}";

            ValueRange GetResponse = null;
            IList<IList<object>> values = null;
            int valuesCount = 0;
            //var GetResponse = await valuesResource.Get(SpreadsheetId, ReadRange).ExecuteAsync();
            //var values = GetResponse.Values;

            while (true)
            {
                GetResponse = await valuesResource.Get(SpreadsheetId, ReadRange).ExecuteAsync();
                values = GetResponse.Values;

                if (values == null || !values.Any())
                {
                    break;
                }

                //var testValue = GetResponse.Values.First();
                //valuesCount = values.Count;

                col1++;
                col2++;

                ReadRange = $"Copy of Free Beer BlackList!A{col1}";
                WriteRange = $"Copy of Free Beer BlackList!A{col1}:G{col2}";


            }

            if (values == null || !values.Any())
            {
                var rowValues = new ValueRange { Values = new List<IList<object>> { new List<object> { a_SocketGuildUser, "NOT AVALIABLE", "TRUE", a_sReason, DateTime.Now.ToString("M/d/yyyy") } } };
                var update = valuesResource.Update(rowValues, SpreadsheetId, WriteRange);
                update.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
                var updateresponse = await update.ExecuteAsync();

            }

            //var response = await update.ExecuteAsync();
            // Console.WriteLine($"Updated rows: { response.UpdatedRows}");
        }

        [Command("get-recent-deaths")]
        public async void GetRecentDeaths(SocketSlashCommand command)
        {
            string playerData = null;
            string playerAlbionId = "KYDr8-OIQKO_qEsilGyyHA";
            int deathDisplayCounter = 1;
            int visibleDeathsShown = 5; //can add up to 10 deaths

            using (HttpResponseMessage response = await AlbionOnlineDataParser.AlbionOnlineDataParser.ApiClient.GetAsync($"players/{playerAlbionId}/deaths"))
            {
                if (response.IsSuccessStatusCode)
                {
                    playerData = await response.Content.ReadAsStringAsync();
                    var parsedObjects = JArray.Parse(playerData);
                    //TODO: Add killer and date of death

                    var searchDeaths = parsedObjects.Children<JObject>()
                        .Select(jo => (int)jo["EventId"])
                        .ToList();

                    var embed = new EmbedBuilder()
                    .WithTitle("Recent Deaths")
                    .WithColor(new Color(238, 62, 75));

                    for (int i = 0; i < searchDeaths.Count; i++)
                    {
                        if (i <= visibleDeathsShown)
                        {
                            embed.AddField($"Death{deathDisplayCounter}", $"https://albiononline.com/en/killboard/kill/{searchDeaths[i]}", false);
                            deathDisplayCounter++;
                        }
                    }
                    await command.Channel.SendMessageAsync(null, false, embed.Build());
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

        public async Task<string> GetMarketDataAndGearImg(SocketSlashCommand command, PlayerDataHandler.Equipment1 victimEquipment)
        {
            AlbionOnlineDataParser.AlbionOnlineDataParser.InitializeAlbionDataProject();

            int returnValue = 0;
            int returnNotUnderRegearValue = 0;
            string sMarketLocation = AlbionCitiesEnum.Martlock.ToString(); //add this to config. If field is null, all cities market data will be pulled
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
                else if (victimEquipment.Head.Type.Contains("T6") && (victimEquipment.Head.Type.Contains("@2")  || victimEquipment.Head.Type.Contains("@3")))
                {
                    equipmentList.Add(head);
                    underRegearList.Add(headImg);
                }
                else if(victimEquipment.Head.Type.Contains("T7") && (victimEquipment.Head.Type.Contains("@1") || victimEquipment.Head.Type.Contains("@2")))
                {
                    equipmentList.Add(head);
                    underRegearList.Add(headImg);
                }
                else  if (victimEquipment.Head.Type.Contains("T8"))
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
                else if(victimEquipment.MainHand.Type.Contains("T6") && (victimEquipment.MainHand.Type.Contains("@2") || victimEquipment.MainHand.Type.Contains("@3")))
                {
                    equipmentList.Add(weapon);
                    underRegearList.Add(weaponImg);
                }
                else if (victimEquipment.MainHand.Type.Contains("T7") && (victimEquipment.MainHand.Type.Contains("@1") || victimEquipment.MainHand.Type.Contains("@2")))
                {
                    equipmentList.Add(weapon);
                    underRegearList.Add(weaponImg);
                }
                else if(victimEquipment.MainHand.Type.Contains("T8"))
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
                    else if(victimEquipment.OffHand.Type.Contains("T6") && (victimEquipment.OffHand.Type.Contains("@2") || victimEquipment.OffHand.Type.Contains("@3")))
                    {
                        equipmentList.Add(offhand);
                        underRegearList.Add(offhandImg);
                    }
                    else if(victimEquipment.OffHand.Type.Contains("T7") && (victimEquipment.OffHand.Type.Contains("@1") || victimEquipment.OffHand.Type.Contains("@2")))
                    {
                        equipmentList.Add(offhand);
                        underRegearList.Add(offhandImg);
                    }
                    else if(victimEquipment.OffHand.Type.Contains("T8"))
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
                else if(victimEquipment.Armor.Type.Contains("T6") && (victimEquipment.Armor.Type.Contains("@2") || victimEquipment.Armor.Type.Contains("@3")))
                {
                    equipmentList.Add(armor);
                    underRegearList.Add(armorImg);
                }
                else if(victimEquipment.Armor.Type.Contains("T7") && (victimEquipment.Armor.Type.Contains("@1") || victimEquipment.Armor.Type.Contains("@2")))
                {
                    equipmentList.Add(armor);
                    underRegearList.Add(armorImg);
                }
                else if(victimEquipment.Armor.Type.Contains("T8"))
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
                else if(victimEquipment.Shoes.Type.Contains("T6") && (victimEquipment.Shoes.Type.Contains("@2") || victimEquipment.Shoes.Type.Contains("@3")))
                {
                    equipmentList.Add(boots);
                    underRegearList.Add(bootsImg);
                }
                else if(victimEquipment.Shoes.Type.Contains("T7") && (victimEquipment.Shoes.Type.Contains("@1") || victimEquipment.Shoes.Type.Contains("@2")))
                {
                    equipmentList.Add(boots);
                    underRegearList.Add(bootsImg);
                }
                else if(victimEquipment.Shoes.Type.Contains("T8"))
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
                else if(victimEquipment.Cape.Type.Contains("T5") && (victimEquipment.Cape.Type.Contains("@2") || victimEquipment.Cape.Type.Contains("@3")))
                {
                    equipmentList.Add(cape);
                    underRegearList.Add(capeImg);
                }
                else if(victimEquipment.Cape.Type.Contains("T6") && (victimEquipment.Cape.Type.Contains("@1") || victimEquipment.Cape.Type.Contains("@2") || victimEquipment.Cape.Type.Contains("@3")))
                {
                    equipmentList.Add(cape);
                    underRegearList.Add(capeImg);
                }
                else if(victimEquipment.Cape.Type.Contains("T7"))
                {
                    equipmentList.Add(cape);
                    underRegearList.Add(capeImg);
                }
                else if(victimEquipment.Cape.Type.Contains("T8"))
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
                        notAvailableInMarketList.Add(marketData.FirstOrDefault().item_id.Replace('_',' ').Replace('@','.'));
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
            gearImage +=$"</center></div>";
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
            }else
            {
                return false;
            }
        }
        [SlashCommand("regeartest", "Submit death for regear")]
        public async void RegearSubmission(SocketSlashCommand command)
        {
            var eventData = await GetAlbionEventInfo(command);
            //await PostRegear(command, eventData);
            dataBaseService = new DataBaseService();
            await dataBaseService.AddPlayerInfo(new Player
            {
                PlayerId = eventData.Victim.Id,
                PlayerName = eventData.Victim.Name
            });
            if (CheckIfPlayerHaveReGearIcon(command))
            {
                await PostRegear(command, eventData);
            }

            Console.WriteLine("something");
        }

        public async Task PostRegear(SocketSlashCommand command, PlayerDataHandler.Rootobject eventData)
        {
            //ulong id = 1014912611004989491; // 3 "specific channel"
            ulong id = 603281980951494670; // 3 "private channel" //throw this in config
            var chnl = _client.GetChannel(id) as IMessageChannel; // 4

            //REWRITE THIS TO BE CLEANER. ASSIGN ALL GEAR DATA. ADD 

            
            var gearImg = await GetMarketDataAndGearImg(command, eventData.Victim.Equipment);
            try
            {


                var converter = new HtmlConverter();
                var html = gearImg;
                var bytes = converter.FromHtmlString(html);

                using (System.IO.MemoryStream imgStream = new System.IO.MemoryStream(bytes))
                {
                    var embed = new EmbedBuilder()
                                    .WithTitle($"Regear Submission")
                                    .AddField("User submitted ", command.User.Username, true)
                                    .AddField("Victim", eventData.Victim.Name)
                                    .AddField("Death Average IP", eventData.Victim.AverageItemPower)

                                    //.WithImageUrl(GearImageRenderSerivce(command))
                                    //.AddField(fb => fb.WithName("🌍 Location").WithValue("https://cdn.discordapp.com/attachments/944305637624533082/1026594623696678932/BAG_603948955.png").WithIsInline(true))
                                    .WithImageUrl($"attachment://image.jpg")
                                    .WithUrl($"https://albiononline.com/en/killboard/kill/{command.Data.Options.First().Value}");

                    await chnl.SendFileAsync(imgStream, "image.jpg", $"Regear Submission from {command.User}", false, embed.Build()); // 5
                    //await chnl.SendMessageAsync("Regear Submission from....", false, embed.Build()); // 5
                    //build.WithThumbnailUrl("attachment://anyImageName.png"); //or build.WithImageUrl("")
                    //await Context.Channel.SendFileAsync(imgStream, "anyImageName.png", "", false, build.Build());
                }

            }
            catch (Exception ex)
            {

                throw;
            }

        }


    }

}


public class InteractionModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("testregear", "regear buttons")]
    public async Task handleButtonCommand() 
    {
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

        //var menu = new SelectMenuBuilder()
        //{
        //    CustomId = "regearMenu",
        //    Placeholder = "Test Menu"
        //};

        var component = new ComponentBuilder();
        component.WithButton(approveButton);
        component.WithButton(denyButton);
        // componet.WithSelectMenu(menu);

        await RespondAsync("Regear Submission", components: component.Build());
    }

    [ComponentInteraction("approve")]
    public async Task ApproveButtonInput()
    {

        Console.WriteLine("Regear approved");

    }

    [ComponentInteraction("deny")]
    public async Task DenyButtonInputAsync()
    {
        Console.WriteLine("Regear denied");
    }




}