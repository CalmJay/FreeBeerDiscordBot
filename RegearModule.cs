using CoreHtmlToImage;
using Discord;
using Discord.WebSocket;
using DiscordBot.Enums;
using DiscordBot.Models;
using DiscordBot.Services;
using PlayerData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayerData;
using Discord.Interactions;
using System.Net.Http;
using Newtonsoft.Json;
using MarketData;
using AlbionOnlineDataParser;
using FreeBeerBot;

namespace DiscordBot.RegearModule
{
    public class RegearModule
    {
        private DataBaseService dataBaseService;

        public int TotalRegearSilverAmount { get; set; }
        private int silverTierRegearCap = 1300000;
        private int goldTierRegearCap = 1700000;
        private int iTankMinimumIP = 1400;
        private int iDPSMinimumIP = 1450;
        private int iHealerMinmumIP = 1350;
        private int iSupportMinimumIP = 1350;

        public async Task PostRegear(SocketInteractionContext command, PlayerDataHandler.Rootobject eventData, string partyLeader, string reason, MoneyTypes moneyTypes)
        {
            ulong id = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("regearTeamChannelId"));

            var chnl = command.Client.GetChannel(id) as IMessageChannel;


            //Check if regear already has been submitted
                        //Search through all KILLID's in spreadsheet and database


            var marketDataAndGearImg = await GetMarketDataAndGearImg(command, eventData.Victim.Equipment);

            //var gearImage = GetGearImg(command, marketData);

            var converter = new HtmlConverter();
            var html = marketDataAndGearImg[0];
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
            var auditButton = new ButtonBuilder()
            {
                Label = "Audit",
                CustomId = "audit",
                Style = ButtonStyle.Secondary
            };


            var component = new ComponentBuilder();
            component.WithButton(approveButton);
            component.WithButton(denyButton);
            component.WithButton(auditButton);

            try
            {
#if !DEBUG
                dataBaseService = new DataBaseService();
                var player = dataBaseService.GetPlayerInfoByName(eventData.Victim.Name);
                var moneyType = dataBaseService.GetMoneyTypeByName(moneyTypes);
                await dataBaseService.AddPlayerReGear(new PlayerLoot
                {
                    TypeId = moneyType.Id,
                    CreateDate = DateTime.Now,
                    Loot = Convert.ToDecimal(marketDataAndGearImg[1]),
                    PlayerId = player.Id,
                    Message = " Regear(s) have been processed.  Has been added to your account. Please emote :beers: to confirm",
                    PartyLeader = partyLeader,
                    //KillId = command.Data.Options.First().Value.ToString(),
                    KillId = command.Interaction.Data.ToString(),//THIS NEEDS FIXING
                    Reason = reason
                });
#endif
               


                using (MemoryStream imgStream = new MemoryStream(bytes))
                {
                    var embed = new EmbedBuilder()
                                    .WithTitle($"Regear Submission from {command.User.Username}")
                                    .AddField("Victim", eventData.Victim.Name, true)
                                    .AddField("Death Average IP ", eventData.Victim.AverageItemPower, true)

                                    .AddField("Killer", "[" + eventData.Killer.AllianceName + "] " + "[" + eventData.Killer.GuildName + "] " + eventData.Killer.Name)                             
                                    .AddField("Refund Amount: ", TotalRegearSilverAmount)
                                    .AddField("KillID: ", eventData.EventId)
                                    .AddField("Death Location: ", eventData.KillArea)

                                    //.WithImageUrl(GearImageRenderSerivce(command))
                                    //.AddField(fb => fb.WithName("🌍 Location").WithValue("https://cdn.discordapp.com/attachments/944305637624533082/1026594623696678932/BAG_603948955.png").WithIsInline(true))
                                    .WithImageUrl($"attachment://image.jpg");
                    //.WithUrl($"https://albiononline.com/en/killboard/kill/{command.Data.Options.First().Value}"); GET KILL ID FROM HANDLER

                    

                    await chnl.SendFileAsync(imgStream, "image.jpg", $"Regear Submission from {command.User}", false, embed.Build(), null, false, null, null, components: component.Build());
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public bool CheckIfPlayerHaveReGearIcon(SocketInteractionContext socketInteractionUser)
        {
            ulong GoldTierID = Convert.ToUInt64(System.Configuration.ConfigurationManager.AppSettings.Get("GoldTierRegear"));
            ulong SilverTierID = Convert.ToUInt64(System.Configuration.ConfigurationManager.AppSettings.Get("SilverTierRegear"));

            //if (socketInteractionUser.User is SocketGuildUser guildUser)
            //{
            //    if ((guildUser.Roles.Any(r => r.Name == "Silver Tier Regear - Elligible" || r.Name == "Gold Tier Regear - Elligible")) || (guildUser.Roles.Any(r => r.Id == GoldTierID || r.Id == SilverTierID)))
            //    {
            //        return true;
            //    }
            //    //ADD BRONZE REGEAR LOGIC
            //}
            return true;

        }

        public async Task<List<string>> GetMarketDataAndGearImg(SocketInteractionContext command, PlayerDataHandler.Equipment1 victimEquipment)
        {
            int returnValue = 0;
            int returnNotUnderRegearValue = 0;
            string? sMarketLocation = System.Configuration.ConfigurationManager.AppSettings.Get("chosenCityMarket"); //If field is null, all cities market data will be pulled
            bool bAddAllQualities = false;
            int iDefaultItemQuality = 2;

            List<Tuple<string, GearLocation>> equipmentList = new List<Tuple<string, GearLocation>>();

            List<string> notUnderRegearEquipmentList = new List<string>();
            List<string> regearableEquipment = new List<string>();
            List<string> notUnderRegearList = new List<string>();
            List<string> notAvailableInMarketList = new List<string>();

            string jsonAverageMarketData = null;
            string jsonCurrentMarketData = null;
            string? currentPricingGearPiece = null;

            int iAverageItemPrice = 0;
            int iAllItemsAveragePrice = 0;
            int grandTotalPrice = 0;

            AlbionOnlineDataParser.AlbionOnlineDataParser.InitializeAlbion24HourDataMarketPricesHistory();
            AlbionOnlineDataParser.AlbionOnlineDataParser.InitializeAlbionDataProjectCurrentPrices();

            string? head = (victimEquipment.Head != null) ? $"{victimEquipment.Head.Type + $"?time-scale=24&Locations={sMarketLocation}&qualities=" + victimEquipment.Head.Quality }" : null;
            string? mainhand = (victimEquipment.MainHand != null) ? $"{victimEquipment.MainHand.Type + $"time-scale=24&?Locations={sMarketLocation}&qualities=" + victimEquipment.MainHand.Quality}" : null;
            string? offhand = (victimEquipment.OffHand != null) ? $"{victimEquipment.OffHand.Type + $"time-scale=24&?Locations={sMarketLocation}&qualities=" + victimEquipment.OffHand.Quality}" : null;
            string? cape = (victimEquipment.Cape != null) ? $"{victimEquipment.Cape.Type + $"?time-scale=24&Locations={sMarketLocation}&qualities=" + victimEquipment.Cape.Quality}" : null;
            string? armor = (victimEquipment.Armor != null) ? $"{victimEquipment.Armor.Type + $"?time-scale=24&Locations={sMarketLocation}&qualities=" + victimEquipment.Armor.Quality}" : null;
            string? shoes = (victimEquipment.Shoes != null) ? $"{victimEquipment.Shoes.Type + $"?time-scale=24&Locations={sMarketLocation}&qualities=" + victimEquipment.Shoes.Quality}" : null;
            string? mount = (victimEquipment.Mount != null) ? $"{victimEquipment.Mount.Type + $"?time-scale=24&Locations={sMarketLocation}&qualities=" + victimEquipment.Mount.Quality}" : null;

            equipmentList.Add(new Tuple<string, GearLocation>(head, GearLocation.Head));
            equipmentList.Add(new Tuple<string, GearLocation>(armor, GearLocation.Armor)); ;
            equipmentList.Add(new Tuple<string, GearLocation>(shoes, GearLocation.Shoes));
            equipmentList.Add(new Tuple<string, GearLocation>(mainhand, GearLocation.MainHand));
            equipmentList.Add(new Tuple<string, GearLocation>(offhand, GearLocation.OffHand));          
            equipmentList.Add(new Tuple<string, GearLocation>(cape, GearLocation.Cape));
            equipmentList.Add(new Tuple<string, GearLocation>(mount, GearLocation.Mount));
            
            
            if(victimEquipment.Head != null)
            {
                regearableEquipment.Add($"https://render.albiononline.com/v1/item/{victimEquipment.Head.Type + "?quality=" + victimEquipment.Head.Quality }");
            }

            if (victimEquipment.MainHand != null)
            {
                regearableEquipment.Add($"https://render.albiononline.com/v1/item/{victimEquipment.MainHand.Type + "?quality=" + victimEquipment.MainHand.Quality}");
            }

            if (victimEquipment.OffHand != null)
            {
                regearableEquipment.Add($"https://render.albiononline.com/v1/item/{victimEquipment.OffHand.Type + "?quality=" + victimEquipment.OffHand.Quality}");
            }

            if (victimEquipment.Cape != null)
            {
                regearableEquipment.Add($"https://render.albiononline.com/v1/item/{victimEquipment.Cape.Type + "?quality=" + victimEquipment.Cape.Quality}");
            }

            if (victimEquipment.Armor != null)
            {
                regearableEquipment.Add($"https://render.albiononline.com/v1/item/{victimEquipment.Armor.Type + "?quality=" + victimEquipment.Armor.Quality}");
            }

            if (victimEquipment.Shoes != null)
            {
                regearableEquipment.Add($"https://render.albiononline.com/v1/item/{victimEquipment.Shoes.Type + "?quality=" + victimEquipment.Shoes.Quality}");
            }

            if (victimEquipment.Mount != null)
            {
                regearableEquipment.Add($"https://render.albiononline.com/v1/item/{victimEquipment.Mount.Type + "?quality=" + victimEquipment.Mount.Quality}");
            }
  
            foreach (var item in equipmentList)
            {
                if (item.Item1 != null)
                {
 
                    using (HttpResponseMessage response = await AlbionOnlineDataParser.AlbionOnlineDataParser.ApiAlbionDataProject.GetAsync($"{item.Item1}"))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            jsonAverageMarketData = await response.Content.ReadAsStringAsync();
                        }
                        else
                        {
                            throw new Exception(response.ReasonPhrase);
                        }
                    }

                    List <AverageItemPrice> marketData = JsonConvert.DeserializeObject<List<AverageItemPrice>>(jsonAverageMarketData);

                    iAverageItemPrice = (marketData.Count >= 1) ? marketData.FirstOrDefault().data.FirstOrDefault().avg_price : 0; 

                    iAllItemsAveragePrice = (iAverageItemPrice > 0) ? (int)Math.Round(marketData.FirstOrDefault().data.Average(x => x.avg_price),0) : 0;

                    if (iAverageItemPrice > 0 || iAllItemsAveragePrice > 0)
                    {
                        returnValue += (iAverageItemPrice <= iAllItemsAveragePrice) ? iAverageItemPrice : iAllItemsAveragePrice;
                    }
                    else if (iAverageItemPrice == 0 && item.Item1 != null)
                    {
                        //Try to find current pricing if average pricing doesnt exist
                        

                        switch (item.Item2)
                        {
                            case GearLocation.Head:
                                currentPricingGearPiece = $"{victimEquipment.Head.Type + $"?Locations={sMarketLocation}&qualities=" + victimEquipment.Head.Quality }";
                                break;
                            case GearLocation.Armor:
                                currentPricingGearPiece = $"{victimEquipment.Armor.Type + $"?Locations={sMarketLocation}&qualities=" + victimEquipment.Armor.Quality}";
                                break;
                            case GearLocation.Shoes:
                                currentPricingGearPiece = $"{victimEquipment.Shoes.Type + $"?Locations={sMarketLocation}&qualities=" + victimEquipment.Shoes.Quality}";
                                break;
                            case GearLocation.MainHand:
                                currentPricingGearPiece = $"{victimEquipment.MainHand.Type + $"?Locations={sMarketLocation}&qualities=" + victimEquipment.MainHand.Quality}";
                                break;
                            case GearLocation.OffHand:
                                currentPricingGearPiece = $"{victimEquipment.OffHand.Type + $"?Locations={sMarketLocation}&qualities=" + victimEquipment.OffHand.Quality}";
                                break;
                            case GearLocation.Mount:
                                currentPricingGearPiece = $"{victimEquipment.Mount.Type + $"?Locations={sMarketLocation}&qualities=" + victimEquipment.Mount.Quality}";
                                break;
                            case GearLocation.Cape:
                                currentPricingGearPiece = $"{victimEquipment.Cape.Type + $"?Locations={sMarketLocation}&qualities=" + victimEquipment.Cape.Quality}";
                                break;
                        }


                        if (currentPricingGearPiece != null)
                        {
                            using (HttpResponseMessage response = await AlbionOnlineDataParser.AlbionOnlineDataParser.ApiAlbionDataProjectCurrentPrices.GetAsync(currentPricingGearPiece))
                            {
                                if (response.IsSuccessStatusCode)
                                {
                                    jsonCurrentMarketData = await response.Content.ReadAsStringAsync();
                                }
                                else
                                {
                                    throw new Exception(response.ReasonPhrase);
                                }
                            }
                            var marketDataCurrentPricing = JsonConvert.DeserializeObject<List<EquipmentMarketData>>(jsonCurrentMarketData);

                            returnValue += marketDataCurrentPricing.FirstOrDefault().sell_price_min;
                        }
                    }
                    if (iAverageItemPrice == 0)
                    {
                        //notAvailableInMarketList.Add((marketData.Count == 0) ? "" : marketData.FirstOrDefault().item_id.Replace('_', ' ').Replace('@', '.'));
                        //notAvailableInMarketList.Add(currentPricingGearPiece.Replace('_', ' ').Replace('@', '.'));
                        //notAvailableInMarketList.Add(currentPricingGearPiece.Substring(0,currentPricingGearPiece.IndexOf("@")).Replace('_', ' '));
                    }
                }

            }


            var guildUser = (SocketGuildUser)command.User;

            grandTotalPrice = returnValue;

            if (guildUser.Roles.Any(r => r.Name == "Gold Tier Regear - Elligible")) // Role ID 1031731127431479428
            {
                returnValue = returnValue = Math.Min(goldTierRegearCap, returnValue);
            }
            else if (guildUser.Roles.Any(r => r.Name == "Silver Tier Regear - Elligible")) //ROLE ID 1031731037149081630
            {
                returnValue = Math.Min(silverTierRegearCap, returnValue);
            }
            else
            {
                returnValue = returnValue = Math.Min(800000, returnValue);
            }

            TotalRegearSilverAmount = returnValue;




            var gearImage = "<div style='background-color: #c7a98f;'> <div> <center><h3>Regearable</h3>";
            foreach (var item in regearableEquipment)
            {
                gearImage += $"<img style='display: inline;width:150px;height:150px' src='{item}'/>";
            }
            gearImage += $"<div style:'text-align : right;'>Refund amt. : {returnValue}</div></center></div>";
            gearImage += $"<div style:'text-align : right;'>Grand Total amt. : {grandTotalPrice}</div>";
            gearImage += $"<div><center>";
            gearImage += $"<div style:'text-align : right;'>Items Price : {returnNotUnderRegearValue}</div></center></div><center><br/><h3> Items not found or price is too high </h3>";

            if(notAvailableInMarketList.Count != 0)
            {

            }
            foreach (var item in notAvailableInMarketList)
            {
                gearImage += $"{item}<br/>";
            }

            gearImage += $"</center></div>";

            return new List<string> { gearImage, returnValue.ToString() };
        }

        public async Task<int> GetMarketData(SocketSlashCommand command, PlayerDataHandler.Equipment1 victimEquipment)
        {
            AlbionOnlineDataParser.AlbionOnlineDataParser.InitializeAlbionDataProjectCurrentPrices();

            int returnValue = 0;
            string? sMarketLocation = System.Configuration.ConfigurationManager.AppSettings.Get("chosenCityMarket"); //If field is null, all cities market data will be pulled
            bool bAddAllQualities = false;
            int iDefaultItemQuality = 2;


            string? head = (victimEquipment.Head != null) ? $"{victimEquipment.Head.Type + $"?Locations={sMarketLocation}&qualities=" + victimEquipment.Head.Quality }" : null;
            string? weapon = (victimEquipment.MainHand != null) ? $"{victimEquipment.MainHand.Type + $"?Locations={sMarketLocation}&qualities=" + victimEquipment.MainHand.Quality}" : null;
            string? offhand = (victimEquipment.OffHand != null) ? $"{victimEquipment.OffHand.Type + $"?Locations={sMarketLocation}&qualities=" + victimEquipment.OffHand.Quality}" : null;
            string? cape = (victimEquipment.Cape != null) ? $"{victimEquipment.Cape.Type + $"?Locations={sMarketLocation}&qualities=" + victimEquipment.Cape.Quality}" : null;
            string? armor = (victimEquipment.Armor != null) ? $"{victimEquipment.Armor.Type + $"?Locations={sMarketLocation}&qualities=" + victimEquipment.Armor.Quality}" : null;
            string? boots = (victimEquipment.Shoes != null) ? $"{victimEquipment.Shoes.Type + $"?Locations={sMarketLocation}&qualities=" + victimEquipment.Shoes.Quality}" : null;
            string? mount = (victimEquipment.Mount != null) ? $"{victimEquipment.Mount.Type + $"?Locations={sMarketLocation}&qualities=" + victimEquipment.Mount.Quality}" : null;

            List<string> equipmentList = new List<string>();

            equipmentList.Add(head);
            equipmentList.Add(weapon);
            equipmentList.Add(offhand);
            equipmentList.Add(cape);
            equipmentList.Add(armor);
            equipmentList.Add(boots);
            equipmentList.Add(mount);


            //https://www.albion-online-data.com/api/v2/stats/prices/T4_MAIN_ROCKMACE_KEEPER@3.json?locations=Martlock&qualities=4 brought back only 1
            //https://www.albion-online-data.com/api/v2/stats/prices/T4_MAIN_ROCKMACE_KEEPER@3?Locations=Martlock brought back all qualities

            //MarketResponse testMarketdata = new MarketResponse() // THIS IS THE CONSTRUCTORS TO THE AlbionData.MODELS

            //SUDO
            //IF Market entry is zero change quality. If still zero send message back to user to update the market with the https://www.albion-online-data.com/ project and update the market items

            string jsonMarketData = null;

            foreach (var item in equipmentList)
            {
                if (item != null)
                {
                    using (HttpResponseMessage response = await AlbionOnlineDataParser.AlbionOnlineDataParser.ApiAlbionDataProject.GetAsync(item))
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

                    returnValue += marketData.FirstOrDefault().sell_price_min; //CHANGE TO AVERAGE SELL PRICE
                }
            }
            var guildUser = (SocketGuildUser)command.User;

            if (guildUser.Roles.Any(r => r.Name == "Silver Tier Regear - Elligible")) //ROLE ID 1031731037149081630
            {
                returnValue = Math.Min(1300000, returnValue);
            }
            else if (guildUser.Roles.Any(r => r.Name == "Gold Tier Regear - Elligible")) // Role ID 1031731127431479428
            {
                returnValue = returnValue = Math.Min(1700000, returnValue);
            }

            //TODO: Add a selection to pick the cheapest item on the market if the quality is better (example. If regear submits a normal T6 Heavy mace and it costs 105k but there's a excellent quality for 100k. Submit the better quaility price

            return returnValue;
        }

        private bool IsRegearTankClass()
        {

            return false;
        }

        private bool IsRegearDPSClass()
        {

            return false;
        }

        private bool IsRegearHealerClass()
        {

            return false;
        }

        private bool IsRegearSupportClass()
        {

            return false;
        }
    }
}
