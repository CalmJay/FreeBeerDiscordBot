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
using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
using PlayerData;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

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
            string? sUserNickname = ((a_command.User as SocketGuildUser).Nickname != null) ? new PlayerDataLookUps().CleanUpShotCallerName((a_command.User as SocketGuildUser).Nickname) : a_command.User.Username;
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
                case MoneyTypes.OCBreak:
                    rowValues = new ValueRange { Values = new List<IList<object>> { new List<object> { "@" + sUserNickname, a_iTotalSilverRefund, DateTime.UtcNow.Date.ToString("M/d/yyyy"), "OC Break", a_sEventType, a_sCallerName, msgRef.MessageId.ToString(), "N/A", a_command.User.ToString() } } };
                    break;

            }

            var update = serviceValues.Update(rowValues, RegearSheetID, WriteRange);

            update.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
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
            string? sUserNickname = (a_SocketGuildUser.Nickname != null) ? new PlayerDataLookUps().CleanUpShotCallerName(a_SocketGuildUser.Nickname) : a_SocketGuildUser.Username;

            var payOutsNumberOfRow = serviceValues.Get(RegearSheetID, "Payouts!B2:B").Execute().Values.Count;
            var dataValidationNumberOfRow = serviceValues.Get(RegearSheetID, "Data Validation!B2:B").Execute().Values.Count;

            var col1 = payOutsNumberOfRow + 2;
            var col2 = dataValidationNumberOfRow + 2;

            try
            {
                var payoutsRowValues = new ValueRange { Values = new List<IList<object>> { new List<object> { sUserNickname } } };
                var payoutsUpdate = serviceValues.Update(payoutsRowValues, RegearSheetID, $"B{col1}");
                payoutsUpdate.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                await payoutsUpdate.ExecuteAsync();

                var dataValidationRowValues = new ValueRange { Values = new List<IList<object>> { new List<object> { sUserNickname, "@" + sUserNickname } } };
                var dataValidationUpdate = serviceValues.Update(dataValidationRowValues, RegearSheetID, $"Data Validation!B{col2}:C2{col2}");
                dataValidationUpdate.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                await dataValidationUpdate.ExecuteAsync();
            }
            catch (Exception ex) 
            { 
                Console.WriteLine(ex.ToString()); 
            }
        }

        public static List<string> GetRunningPaychexTotal(string a_sUserName)
        {
            var serviceValues = GetSheetsService().Spreadsheets;
            DateTime lastSunday = HelperMethods.StartOfWeek(DateTime.Today, DayOfWeek.Sunday);
            var testmonth = DateTime.Now - lastSunday;
            var shortmonth = DateTime.Now.ToShortMonthName();

            IList<IList<object>> paychexClaimedColumn = null;
            string combinedDate = $"{shortmonth}-{lastSunday.Day}";//This gets the running total

            List<string> paychexData = new List<string>();
            var DaterowValues = serviceValues.Values.Get(RegearSheetID, "Payouts!3:3").Execute().Values.FirstOrDefault().ToList();

            int dateIndex = 0;

            foreach (var dates in DaterowValues)
            {
                if (dates.ToString() == combinedDate)
                {

                    break;
                }
                dateIndex++;
            }

            var rowValues = serviceValues.Values.Get(RegearSheetID, $"Payouts!R4C2:R350C{dateIndex}").Execute().Values;

            int i = 0;
            foreach (var users in rowValues)
            {
                if (users[0].ToString().ToLower() == a_sUserName.ToLower())
                {
                    paychexData.Add(users[users.Count - 2].ToString() + $" {DaterowValues[dateIndex - 2]}");
                    paychexData.Add(users.Last().ToString() + $" {DaterowValues[dateIndex - 1]}");
                }

                i++;
            }

            var ssRequest = serviceValues.Get(RegearSheetID);
            Spreadsheet ss = ssRequest.Execute();
            List<string> sheetList = new List<string>();

            foreach (Sheet sheet in ss.Sheets)
            {
                sheetList.Add(sheet.Properties.Title);
            }

            paychexClaimedColumn = serviceValues.Values.Get(RegearSheetID, $"{DaterowValues[dateIndex - 2]} Paychex!R2C1:R350C4").Execute().Values;

            if (sheetList.Contains($"{DaterowValues[dateIndex - 2]} Paychex"))
            {

                i = 0;
                foreach (var users in paychexClaimedColumn)
                {
                    if (users[0].ToString().ToLower() == a_sUserName.ToLower())
                    {
                        if (users[3].ToString() == "TRUE")
                        {
                            paychexData[0] += " (CLAIMED)";
                        }
                        else if (users[3].ToString() == "FALSE")
                        {
                            paychexData[0] += " (NOT CLAIMED)";
                        }
                        break;
                    }
                    i++;
                }
            }
            else if(paychexData.Count > 0)
            {
                paychexData[0] += " (NOT RENDERED)";
            }

            return paychexData;
        }

        public static string GetMiniMarketCredits(string a_sUserName)
        {
            var serviceValues = GetSheetsService().Spreadsheets.Values;
            string returnValue = "0";

            ReadRange = $"Mini-Market Credits!A2:A305";

            var rowValues = serviceValues.Get(RegearSheetID, $"Mini-Market Credits!R2C1:R305C2").Execute().Values;

            int i = 0;
            foreach (var users in rowValues)
            {
                if (users[0].ToString().ToLower() == a_sUserName.ToLower())
                {
                    returnValue = users.Last().ToString();
                    break;
                }

                i++;
            }

            return returnValue;
        }

        public static async Task RenderPaychex()
        {
            var serviceValues = GetSheetsService().Spreadsheets;

            //serviceValues.Sheets.CopyTo()


        }

        public static async Task TransferPaychexToMiniMartCredits(SocketGuildUser a_SocketGuildUser)
        {
            string? sUserNickname = (a_SocketGuildUser.Nickname != null) ? new PlayerDataLookUps().CleanUpShotCallerName(a_SocketGuildUser.Nickname) : a_SocketGuildUser.Username;

            var serviceValues = GetSheetsService().Spreadsheets;
            DateTime lastSunday = HelperMethods.StartOfWeek(DateTime.Today, DayOfWeek.Sunday);
            var shortmonth = DateTime.Now.ToShortMonthName();

            IList<IList<object>> paychexClaimedColumn = null;
            string combinedDate = $"{shortmonth}-{lastSunday.Day}";



            List<string> paychexRunningTotal = GoogleSheetsDataWriter.GetRunningPaychexTotal(sUserNickname);
            var cleanupedTotal = paychexRunningTotal[0].Substring(0, paychexRunningTotal[0].IndexOf(" "));
            int a = Int32.Parse(cleanupedTotal.Replace(",", ""));
            //if(a > 10000000)
            //{ 
            if (paychexRunningTotal[0].Contains("(NOT CLAIMED"))
            {
                var numberOfRow = serviceValues.Values.Get(RegearSheetID, "MiniMart Ledger!B2:B").Execute().Values.Count;
                var col1 = numberOfRow + 2;



                try
                {
                    var rowValues = new ValueRange { Values = new List<IList<object>> { new List<object> { "@" + sUserNickname, cleanupedTotal, DateTime.Now.ToString("M/d/yyyy"), "Free Beer Bot", "Credits Transfer" } } };
                    var rowUpdates = serviceValues.Values.Update(rowValues, RegearSheetID, $"MiniMart Ledger!B{col1}:F{col1}");
                    rowUpdates.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                    await rowUpdates.ExecuteAsync();


                    var splitPaychexString = paychexRunningTotal[0].Split(" ");
                    paychexClaimedColumn = serviceValues.Values.Get(RegearSheetID, $"{splitPaychexString[1].ToString()} Paychex!R2C1:R350C4").Execute().Values;

                    int i = 0;
                    foreach (var users in paychexClaimedColumn)
                    {
                        if (users[0].ToString().ToLower() == sUserNickname.ToLower())
                        {
                            var dataValidationRowValues = new ValueRange { Values = new List<IList<object>> { new List<object> { "Free Beer Bot", "TRUE" } } };
                            var dataValidationUpdate = serviceValues.Values.Update(dataValidationRowValues, RegearSheetID, $"{splitPaychexString[1]} Paychex!C{i + 2}:D{i + 2}");
                            dataValidationUpdate.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
                            await dataValidationUpdate.ExecuteAsync();
                        }

                        i++;
                    }


                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
                await a_SocketGuildUser.SendMessageAsync($"Your Paychex have successfully been transfered to mini-mart credits. Total: {cleanupedTotal}");
            }
            else if (paychexRunningTotal[0].Contains("(CLAIMED"))
            {
                await a_SocketGuildUser.SendMessageAsync("You already claimed or transferred your paychex");
            }
            else if (paychexRunningTotal[0].Contains("(NOT RENDERED)"))
            {
                await a_SocketGuildUser.SendMessageAsync("The new paychex have not been rendered yet. Please be patient.");
            }
            else
            {
                await a_SocketGuildUser.SendMessageAsync("The Paychex are not rendered yet or there was an issue transferring your paychex to mini mart credits. Seek out an Officer to investigate.");
            }
        //}
        }

        public static async Task MiniMartTransaction(SocketGuildUser a_Manager, SocketGuildUser a_User, int a_iAmount, MiniMarketType a_eTransactionType)
        {
            string? sManagerNickname = (a_Manager.Nickname != null) ? new PlayerDataLookUps().CleanUpShotCallerName(a_Manager.Nickname) : a_Manager.Username;
            string? sUserNickname = (a_User.Nickname != null) ? new PlayerDataLookUps().CleanUpShotCallerName(a_User.Nickname) : a_User.Username;

            double dMiniMartDiscount = .10;

            var serviceValues = GetSheetsService().Spreadsheets.Values;

            var numberOfRow = serviceValues.Get(RegearSheetID, "MiniMart Ledger!B2:B").Execute().Values.Count; // This finds the nearest last row int he spreadsheet. This saves on hitting the rate limit when hitting google API.

            var col1 = numberOfRow + 1;
            var col2 = numberOfRow + 1;

            //var messages = await a_command.Channel.GetMessagesAsync(1).FlattenAsync();
            //var msgRef = new MessageReference(messages.First().Id);

            ReadRange = $"MiniMart Ledger!B{col1 + 1}";
            WriteRange = $"MiniMart Ledger!B{col1 + 1}:f{col2 + 1}";

            ValueRange rowValues = null;

            int discountedAmount = Convert.ToInt32(Math.Floor(a_iAmount - (a_iAmount * dMiniMartDiscount)));

            switch (a_eTransactionType)
            {
                case MiniMarketType.AccountSetup:
                    rowValues = new ValueRange { Values = new List<IList<object>> { new List<object> { "@" + sUserNickname, a_iAmount, DateTime.UtcNow.Date.ToString("M/d/yyyy"), sManagerNickname, "Account Setup" } } };
                    break;
                case MiniMarketType.Deposit:
                    rowValues = new ValueRange { Values = new List<IList<object>> { new List<object> { "@" + sUserNickname, a_iAmount, DateTime.UtcNow.Date.ToString("M/d/yyyy"), sManagerNickname, "Deposit" } } };
                    break;
                case MiniMarketType.Purchase:
                    rowValues = new ValueRange { Values = new List<IList<object>> { new List<object> { "@" + sUserNickname, -discountedAmount, DateTime.UtcNow.Date.ToString("M/d/yyyy"), sManagerNickname, "Purchase" } } };
                    break;
                case MiniMarketType.CreditsTransfer:
                    rowValues = new ValueRange { Values = new List<IList<object>> { new List<object> { "@" + sUserNickname, a_iAmount, DateTime.UtcNow.Date.ToString("M/d/yyyy"), sManagerNickname, "Credits Transfer" } } };
                    break;
                case MiniMarketType.Withdrawal:
                    rowValues = new ValueRange { Values = new List<IList<object>> { new List<object> { "@" + sUserNickname, -a_iAmount, DateTime.UtcNow.Date.ToString("M/d/yyyy"), sManagerNickname, "Withdrawal" } } };
                    break;
                case MiniMarketType.Other:
                    rowValues = new ValueRange { Values = new List<IList<object>> { new List<object> { "@" + sUserNickname, a_iAmount, DateTime.UtcNow.Date.ToString("M/d/yyyy"), sManagerNickname, "Other" } } };
                    break;
            }

            var update = serviceValues.Update(rowValues, RegearSheetID, WriteRange);

            update.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED;
            await update.ExecuteAsync();

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
