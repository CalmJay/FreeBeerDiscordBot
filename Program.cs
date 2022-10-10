using Aspose.Imaging;
using Aspose.Imaging.FileFormats.Jpeg;
using Aspose.Imaging.ImageOptions;
using Aspose.Imaging.Sources;
using Aspose.Words;
using CoreHtmlToImage;
using Discord;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using GroupDocs.Merger;
using GroupDocs.Merger.Domain;
using GroupDocs.Merger.Domain.Options;
using Newtonsoft.Json;
using PlayerData;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Image = Aspose.Imaging.Image;
using Rectangle = Aspose.Imaging.Rectangle;
using Size = Aspose.Imaging.Size;

namespace FreeBeerBot
{
    class Program
    {
        static string[] Scopes = { SheetsService.Scope.Spreadsheets };
        //string SpreadsheetId = "1HFGJk3lAIMrMMBg3PlyPZrX0ooJ_O86brWYrSWdf9Gk"; //TEST SHEET
        string SpreadsheetId = "1s-W9waiJx97rgFsdOHg602qKf-CgrIvKww_d5dwthyU"; //REAL SHEET
        private const string GoogleCredentialsFileName = "credentials.json";
        //string sFreeBeerGuildAPIID = "9ndyGFTPT0mYwPOPDXDmSQ";

        static string ApplicationName = "Google Sheets API .NET Quickstart";
        public bool enableGoogleApi = true;

        ulong GuildID = 157626637913948160;//CHANGE THIS TO THE OFFICAL SERVER WHEN DONE

        public static void Main(string[] args)
        => new Program().MainAsync().GetAwaiter().GetResult();

        private DiscordSocketClient _client;

        public async Task MainAsync()
        {
            _client = new DiscordSocketClient();
            _client.MessageReceived += CommandHandler;
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


        }


        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }

        private Task CommandHandler(SocketMessage message)
        {
            //variables
            string command = "";
            int lengthOfCommand = -1;

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
                case "hello":
                    message.Channel.SendMessageAsync($@"Hello {message.Author.Mention}");
                    break;
                case "regear":
                    message.Channel.SendMessageAsync($@"This is the regear shit {message.Author.Mention}");
                    break;

            }

            //Commands begin here
            //if (command.Equals("hello"))
            //{
            //    message.Channel.SendMessageAsync($@"Hello {message.Author.Mention}");
            //}
            //else if (command.Equals("age"))
            //{
            //    message.Channel.SendMessageAsync($@"Your account was created at {message.Author.CreatedAt.DateTime.Date}");
            //}


            return Task.CompletedTask;
        }

        public async Task Client_Ready()
        {
            //USE GUILD COMMANDS FOR PRIVATE USE
            //GLOBAL COMMANDS ARE MORE FOR LARGE USER BASE USE (AKA IF THE BOT IS GOING TO BE USED IN A LOT OF DISCORD SERVERS)
            // Let's build a guild command! We're going to need a guild so lets just put that in a variable.
            var guild = _client.GetGuild(GuildID); //guildID = server ID

            // Next, lets create our slash command builder. This is like the embed builder but for slash commands.
            var guildCommand = new SlashCommandBuilder();

            // Note: Names have to be all lowercase and match the regular expression ^[\w-]{3,32}$
            //guildCommand
            //    .WithName("list-roles")
            //    .WithDescription("List all roles of a user")
            //    .AddOption("user", ApplicationCommandOptionType.User, "The users whos roles you want to be listed", isRequired: true);

            //guildCommand
            //    .WithName("blacklist")
            //    .WithDescription("Blacklist an asshole")
            //    .AddOption("player", ApplicationCommandOptionType.String, "Name of player blacklisting", isRequired: true);

            //guildCommand //SEE ABOUT ADDING slash bulk commands  https://discordnet.dev/guides/int_basics/application-commands/slash-commands/bulk-overwrite-of-global-slash-commands.html
            //    .WithName("register")
            //    .WithDescription("Verify if the player is not on the blacklist and meets recruitment requirements")
            //    .AddOption("player", ApplicationCommandOptionType.String, "Name of player", isRequired: false);

            guildCommand
                .WithName("regear")
                .WithDescription("Submit a regear")
                .AddOption("killnumber", ApplicationCommandOptionType.Integer, "Killboard ID", isRequired: true);

            // Descriptions can have a max length of 100.
            //guildCommand.WithDescription("When chest is filled the bot will send a message to the player their regear is done");

            // Let's do our global command
            //var globalCommand = new SlashCommandBuilder();
            //globalCommand.WithName("first-global-command");
            //globalCommand.WithDescription("This is my first global slash command");

            try
            {
                // Now that we have our builder, we can call the CreateApplicationCommandAsync method to make our slash command.
                // await guild.CreateApplicationCommandAsync(guildCommand.Build());
                await _client.Rest.CreateGuildCommand(guildCommand.Build(), GuildID);
                // With global commands we don't need the guild.
                // await _client.CreateGlobalApplicationCommandAsync(globalCommand.Build());
                // Using the ready event is a simple implementation for the sake of the example. Suitable for testing and development.
                // For a production bot, it is recommended to only run the CreateGlobalApplicationCommandAsync() once for each command.
            }
            catch (ApplicationCommandException exception)
            {
                // If our command was invalid, we should catch an ApplicationCommandException. This exception contains the path of the error as well as the error message. You can serialize the Error field in the exception to get a visual of where your error is.
                var json = JsonConvert.SerializeObject(exception.Errors, Formatting.Indented);

                // You can send this error somewhere or just print it to the console, for this example we're just going to print it.
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

        //private const string ReadRange = "Copy of Free Beer BlackList!A2:G2";
        //private const string WriteRange = "Copy of Free Beer BlackList!A2:G2";

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

        private async Task WriteToSheetAsync(SpreadsheetsResource.ValuesResource valuesResource, string sSpreadSheetName, string a_SocketGuildUser, string a_sReason)
        {

        }

        public async void RegearSubmission(SocketSlashCommand command)
        {
            var eventData = await GetAlbionEventInfo(command);



            PostRegear(command, eventData);
            Console.WriteLine("something");


        }

        public async Task<PlayerDataHandler.Rootobject> GetAlbionEventInfo(SocketSlashCommand command)
        {

            string playerData = null;
            //SEND IN ALBION EVENT ID
            //TEST ID 569198599
            //CASEY PLAYER ID A8YOx_EpS7SRvlSvC9nzTw
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
            //PlayerDataHandler.Rootobject eventData = new PlayerDataHandler.Rootobject();
            var eventData = JsonConvert.DeserializeObject<PlayerDataHandler.Rootobject>(playerData);

            return eventData;
        }

        [Command("regear")]
        public async Task PostRegear(SocketSlashCommand command, PlayerDataHandler.Rootobject eventData)
        {
            //ulong id = 1014912611004989491; // 3 "specific channel"
            ulong id = 603281980951494670; // 3 "private channel"
            var chnl = _client.GetChannel(id) as IMessageChannel; // 4

            var head = "https://render.albiononline.com/v1/item/T8_HEAD_PLATE_SET2.png?count=1&quality=3";
            var weapon = "https://render.albiononline.com/v1/item/T5_2H_RAM_KEEPER@3.png?count=1&quality=3";
            var cape = "https://render.albiononline.com/v1/item/T4_CAPEITEM_FW_MARTLOCK@3.png?count=1&quality=3";
            var armor = "https://render.albiononline.com/v1/item/T8_ARMOR_PLATE_SET3.png?count=1&quality=3";
            var boots = "https://render.albiononline.com/v1/item/T8_SHOES_LEATHER_SET2.png?count=1&quality=3";

            try
            {
                
                var img1 = $"<div style='width: auto'><img style='display: inline;width:100px;height:100px' src='{head}'/>";
                var img2 = $"<img style='display: inline;width:100px;height:100px' src='{weapon}'/>";
                var img3 = $"<img style='display: inline;width:100px;height:100px' src='{cape}'/>";
                var img4 = $"<img style='display: inline;width:100px;height:100px' src='{armor}'/>";
                var img5 = $"<img style='display: inline;width:100px;height:100px' src='{boots}'/><div style:'text-align : right;'>Items Price : 10000$</div></div>";
                var converter = new HtmlConverter();
                var html = img1+ img2 + img3 + img4 + img5;
                var bytes = converter.FromHtmlString(html);

                using (System.IO.MemoryStream imgStream = new System.IO.MemoryStream(bytes))
                {
                    var embed = new EmbedBuilder()
                                    .WithTitle($"{command.Data.Name} Regear")
                                    .AddField("Discord user ", command.User.Username, true)
                                    .AddField("Victim", eventData.Victim.Name)

                                    //.WithImageUrl(GearImageRenderSerivce(command))
                                    //.AddField(fb => fb.WithName("🌍 Location").WithValue("https://cdn.discordapp.com/attachments/944305637624533082/1026594623696678932/BAG_603948955.png").WithIsInline(true))
                                    .WithImageUrl($"attachment://image.jpg")
                                    .WithUrl($"https://albiononline.com/en/killboard/kill/{command.Data.Options.First().Value}");

                    await chnl.SendFileAsync(imgStream, "image.jpg", "Regear Submission from....", false, embed.Build()); // 5
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
        public static Bitmap Combine(string[] files)
        {
            //read all images into memory
            List<System.Drawing.Bitmap> images = new List<System.Drawing.Bitmap>();
            System.Drawing.Bitmap finalImage = null;

            int width = 0;
            int height = 0;

            foreach (string image in files)
            {
                //create a Bitmap from the file and add it to the list
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(image);

                //update the size of the final bitmap
                width += bitmap.Width;
                height = bitmap.Height > height ? bitmap.Height : height;

                images.Add(bitmap);
            }

            //create a bitmap to hold the combined image
            finalImage = new System.Drawing.Bitmap(width, height);
            return finalImage;
        }

    }


}


