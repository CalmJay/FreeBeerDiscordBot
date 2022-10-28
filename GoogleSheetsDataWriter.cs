using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GoogleSheetsData
{
    public class GoogleSheetsDataWriter
    {
        public static string ReadRange { get; set; }
        public static string WriteRange { get; set; }

        private const string GoogleCredentialsFileName = "credentials.json"; //ADD TO CONFIG

        static string[] Scopes = { SheetsService.Scope.Spreadsheets };
        public static string SpreadsheetId = "1s-W9waiJx97rgFsdOHg602qKf-CgrIvKww_d5dwthyU"; //REAL SHEET //ADD TO CONFIG

        static string ApplicationName = "Google Sheets API .NET Quickstart";
        public bool enableGoogleApi = true; //ADD TO CONFIG

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


        public static async Task WriteAsync(SpreadsheetsResource.ValuesResource valuesResource, string a_SocketGuildUser,string? a_sIngameName, string? a_sReason, string? a_sFine, string? a_sNotes)
        {
            //THIS ONLY WRITES TO THE FREE BEER BLACKLIST SPREADSHEET. ADJUST THIS METHOD SO THAT IT CAN WRITE TO ANYSPREADSHEET
            var col1 = 2;
            var col2 = 2;
            ReadRange = $"Free Beer BlackList!A{col1}";
            WriteRange = $"Free Beer BlackList!A{col1}:G{col2}";

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

                col1++;
                col2++;

                ReadRange = $"Free Beer BlackList!A{col1}";
                WriteRange = $"Free Beer BlackList!A{col1}:G{col2}";
            }

            if (values == null || !values.Any())
            {
                var rowValues = new ValueRange { Values = new List<IList<object>> { new List<object> { a_SocketGuildUser, a_sIngameName, "TRUE", a_sReason, a_sFine, DateTime.Now.ToString("M/d/yyyy"), a_sNotes } } };
                var update = valuesResource.Update(rowValues, SpreadsheetId, WriteRange);
                update.ValueInputOption = SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW;
                var updateresponse = await update.ExecuteAsync();

            }

            //var response = await update.ExecuteAsync();
            // Console.WriteLine($"Updated rows: { response.UpdatedRows}");
        }
    }
}
