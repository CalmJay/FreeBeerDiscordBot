using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordBot.Enums;
using DiscordBot.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using Newtonsoft.Json.Linq;
using PlayerData;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleSheetsData
{
    public class GoogleSheetsDataWriter
    {
        public static string ReadRange { get; set; }
        public static string WriteRange { get; set; }

        private const string GoogleCredentialsFileName = "credentials.json";

        static string[] Scopes = { SheetsService.Scope.Spreadsheets };
        public static string GuildSpreadsheetId = System.Configuration.ConfigurationManager.AppSettings.Get("guildDataBaseSheetID");
        public static string RegearSheetID = System.Configuration.ConfigurationManager.AppSettings.Get("regearSpreadSheetID");

        static string ApplicationName = "Google Sheets API .NET Quickstart";
        public bool enableGoogleApi = bool.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("enableGoogleAPI"));


        public void ConnectToGoogleAPI()
        {
            try
            {
                UserCredential credential;

                using (var stream =
                       new FileStream("credentials.json", FileMode.Open, FileAccess.Read))
                {
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
                    service.Spreadsheets.Values.Get(GuildSpreadsheetId, range);

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
        public static SheetsService GetSheetsService()
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

        public async Task ReadAsync(SpreadsheetsResource.ValuesResource valuesResource, string sReadrange)
        {
            var response = await valuesResource.Get(GuildSpreadsheetId, sReadrange).ExecuteAsync();
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


        public static async Task WriteToFreeBeerRosterDatabase(string a_SocketGuildUser, string? a_sIngameName, string? a_sReason, string? a_sFine, string? a_sNotes)
        {
            //THIS ONLY WRITES TO THE FREE BEER BLACKLIST SPREADSHEET. ADJUST THIS METHOD SO THAT IT CAN WRITE TO ANYSPREADSHEET
            var serviceValues = GoogleSheetsDataWriter.GetSheetsService().Spreadsheets.Values;
            var col1 = 2;
            var col2 = 2;
            ReadRange = $"Free Beer BlackList!A{col1}";
            WriteRange = $"Free Beer BlackList!A{col1}:G{col2}";

            ValueRange GetResponse = null;
            IList<IList<object>> values = null;

            while (true)
            {
                GetResponse = await serviceValues.Get(GuildSpreadsheetId, ReadRange).ExecuteAsync();
                values = GetResponse.Values;

                if (values == null || !values.Any())
                {
                    break;
                }

                col1++;
                col2++;

                ReadRange = $"Free Beer BlackList!A{col1}";
                WriteRange = $"Free Beer BlackList!A{col1}:G{col2}";
            }

            if (values == null || !values.Any())
            {
                var rowValues = new ValueRange { Values = new List<IList<object>> { new List<object> { a_SocketGuildUser, a_sIngameName, "TRUE", a_sReason, a_sFine, DateTime.Now.ToString("M/d/yyyy"), a_sNotes } } };
                var update = serviceValues.Update(rowValues, GuildSpreadsheetId, WriteRange);
                update.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
                await update.ExecuteAsync();
            }

        }

        public static async Task WriteToRegearSheet(SocketInteractionContext a_command, PlayerDataHandler.Rootobject a_playerData, int a_iTotalSilverRefund, string a_sCallerName, string a_sEventType, MoneyTypes a_eMoneyType)
        {
            string sDiscordName = (a_command.User as SocketGuildUser).Nickname != null ? (a_command.User as SocketGuildUser).Nickname.ToString() : a_command.User.Username;

            var serviceValues = GetSheetsService().Spreadsheets.Values;

            var numberOfRow = serviceValues.Get(RegearSheetID, "Current Season Dumps!B2:B").Execute().Values.Count; // This finds the nearest last row int he spreadsheet. This saves on hitting the rate limit when hitting google API.

            var col1 = numberOfRow + 1;
            var col2 = numberOfRow + 1;

            var messages = await a_command.Channel.GetMessagesAsync(1).FlattenAsync();
            var msgRef = new MessageReference(messages.First().Id);

            ReadRange = $"Current Season Dumps!B{col1 + 1}";
            WriteRange = $"Current Season Dumps!B{col1 + 1}:J{col2 + 1}";

            ValueRange rowValues = null;

            switch (a_eMoneyType)
            {
                case MoneyTypes.ReGear:
                    rowValues = new ValueRange { Values = new List<IList<object>> { new List<object> { "@" + a_playerData.Victim.Name, a_iTotalSilverRefund, DateTime.UtcNow.Date.ToString("M/d/yyyy"), "Re-Gear", a_sEventType, a_sCallerName, msgRef.MessageId.ToString(), a_playerData.EventId, a_command.User.ToString() } } };          
                    break;
                case MoneyTypes.LootSplit:
                    rowValues = new ValueRange { Values = new List<IList<object>> { new List<object> { "@" + "PlayerName", DateTime.UtcNow.Date.ToString("M/d/yyyy"), "Loot Split", a_sEventType, a_sCallerName, msgRef.MessageId.ToString(), "N/A", a_command.User.ToString() } } };
                    break;
            }

            var update = serviceValues.Update(rowValues, RegearSheetID, WriteRange);

            update.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
            await update.ExecuteAsync();
        }

        public static async Task RegisterUserToDataRoster(string a_SocketGuildUser, string? a_sIngameName, string? a_sReason, string? a_sFine, string? a_sNotes)
        {
            var serviceValues = GoogleSheetsDataWriter.GetSheetsService().Spreadsheets.Values;

            var numberOfRow = serviceValues.Get(GuildSpreadsheetId, "Guild Roster!B2:B").Execute().Values.Count; // This finds the nearest last row int he spreadsheet. This saves on hitting the rate limit when hitting google API.
            var col1 = numberOfRow + 1;
            var col2 = numberOfRow + 1;

            ReadRange = $"Guild Roster!A{col1 + 1}";
            WriteRange = $"Guild Roster!A{col1 + 1}:E{col2 + 1}";

            
            var rowValues = new ValueRange { Values = new List<IList<object>> { new List<object> { DateTime.Now.ToString("M/d/yyyy"), a_SocketGuildUser, "N/A", DateTime.Now.ToString("M/d/yyyy"), "No notes" } } };
            var update = serviceValues.Update(rowValues, GuildSpreadsheetId, WriteRange);
            update.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
            await update.ExecuteAsync();

            
        }

        public static async Task RegisterUserToRegearSheets(SocketGuildUser a_SocketGuildUser, string? a_sIngameName, string? a_sReason, string? a_sFine, string? a_sNotes)
        {
            var serviceValues = GoogleSheetsDataWriter.GetSheetsService().Spreadsheets.Values;

            var numberOfRow = serviceValues.Get(RegearSheetID, "Data Validation!B2:B").Execute().Values.Count;
            var col1 = numberOfRow + 1;
            var col2 = numberOfRow + 1;

            ReadRange = $"Guild Roster!B{col1 + 1}";
            WriteRange = $"Guild Roster!B{col1 + 1}:E{col2 + 1}";


            var rowValues = new ValueRange { Values = new List<IList<object>> { new List<object> { a_SocketGuildUser.Nickname, $"@{a_SocketGuildUser.Nickname}",a_SocketGuildUser.Username ,a_SocketGuildUser.Id } } };
            var update = serviceValues.Update(rowValues, GuildSpreadsheetId, WriteRange);
            update.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
            await update.ExecuteAsync();
        }

        public static async Task RegisterUserToPayouts(SocketGuildUser a_SocketGuildUser)
        {
            var serviceValues = GoogleSheetsDataWriter.GetSheetsService().Spreadsheets.Values;

            var numberOfRow = serviceValues.Get(RegearSheetID, "Payouts!B2:B").Execute().Values.Count;
            var col1 = numberOfRow + 1;
            var col2 = numberOfRow + 1;

            ReadRange = $"Payouts!B{col1 + 1}";
            WriteRange = $"Payouts!B{col1 + 1}:E{col2 + 1}";


            var rowValues = new ValueRange { Values = new List<IList<object>> { new List<object> { a_SocketGuildUser.Nickname } } };
            var update = serviceValues.Update(rowValues, GuildSpreadsheetId, WriteRange);
            update.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
            await update.ExecuteAsync();
        }


        public static string GetCurrentPaychexAmount(string a_sUserName)
        {
            var serviceValues = GetSheetsService().Spreadsheets.Values;
            DateTime lastSunday = HelperMethods.StartOfWeek(DateTime.Today, DayOfWeek.Sunday);
            var dayTest = lastSunday.Day.ToString();
            var dayofweekTest = lastSunday.DayOfWeek.ToString();
            var monthTest = lastSunday.Month.ToString();
            var shortmonth = DateTime.Now.ToShortMonthName();
            var monthName = DateTime.Now.ToMonthName();

            string combinedDate = $"{shortmonth}-{lastSunday.Day}";

            var DaterowValues = serviceValues.Get(RegearSheetID, "Payouts!3:3").Execute().Values.FirstOrDefault().ToList();

            int dateIndex = 1;

            foreach (var dates in DaterowValues)
            {
                if(dates.ToString() == combinedDate)
                {

                    break;
                }
                dateIndex++;
            }

            ReadRange = $"Payouts!B4:B";

            var rowValues = serviceValues.Get(RegearSheetID, $"Payouts!R4C2:R305C{dateIndex}").Execute().Values;


            string currentPaychex = "0";
            int i = 0;
            foreach (var users in rowValues)
            {
                if (users[0].ToString().ToLower() == a_sUserName.ToLower())
                {
                    currentPaychex = users.Last().ToString(); 
                }
                i++;
            }

            //var finalAmount = rowValues.Where(x => x.ToString() == a_sUserName).FirstOrDefault();
            return currentPaychex;
        }


    }

    public static class HelperMethods
    {
        public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek)
        {
            int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
            return dt.AddDays(-1 * diff).Date;
        }

        public static string ToMonthName(this DateTime dateTime)
        {
            return CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(dateTime.Month);
        }

        public static string ToShortMonthName(this DateTime dateTime)
        {
            return CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(dateTime.Month);
        }
    }
}
