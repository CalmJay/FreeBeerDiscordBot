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


        public async Task PostRegear(SocketInteractionContext command, PlayerDataHandler.Rootobject eventData, string partyLeader, string reason, MoneyTypes moneyTypes)
        {
            ulong id = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("regearTeamChannelId"));

            var chnl = command.Client.GetChannel(id) as IMessageChannel;


            var marketDataAndGearImg = await GetMarketDataAndGearImg(command, eventData.Victim.Equipment);

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
                //var exceptionButton = new ButtonBuilder()
                //{
                //    Label = "Special Exception",
                //    CustomId = "exception",
                //    Style = ButtonStyle.Secondary,
                //};

                var component = new ComponentBuilder();
                component.WithButton(approveButton);
                component.WithButton(denyButton);
                //component.WithButton(exceptionButton);

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

            if (socketInteractionUser.User is SocketGuildUser guildUser)
            {

                if (guildUser.Roles.Any(r => r.Id == GoldTierID || r.Id == SilverTierID))
                {
                    return true;
                }
                if ((guildUser.Roles.Any(r => r.Name == "Silver Tier Regear - Elligible" || r.Name == "Gold Tier Regear - Elligible")) || (guildUser.Roles.Any(r => r.Id == GoldTierID || r.Id == SilverTierID)))
                {
                    return true;
                }
            }
            return false;

        }

        public async Task<List<string>> GetMarketDataAndGearImg(SocketInteractionContext command, PlayerDataHandler.Equipment1 victimEquipment)
        {
            AlbionOnlineDataParser.AlbionOnlineDataParser.InitializeAlbionDataProject();

            int returnValue = 0;
            int returnNotUnderRegearValue = 0;
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
            if (victimEquipment.Head != null)
            {
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
            }
            if (victimEquipment.MainHand != null)
            {
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
            }
            if (victimEquipment.OffHand != null)
            {
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
            }
            if (victimEquipment.Armor != null)
            {
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
            }
            if (victimEquipment.Shoes != null)
            {
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
            }
            if (victimEquipment.Cape != null)
            {
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
            }
            if (victimEquipment.Mount != null)
            {
                equipmentList.Add(mount);
                underRegearList.Add(mountImg);
            }


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
                    using (HttpResponseMessage response = await AlbionOnlineDataParser.AlbionOnlineDataParser.ApiAlbionDataProject.GetAsync(item))
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
            return new List<string> { gearImage, returnValue.ToString() };
        }
    }
}
