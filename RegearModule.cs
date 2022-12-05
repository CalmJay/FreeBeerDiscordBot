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
using AlbionData.Models;

namespace DiscordBot.RegearModule
{
    public class RegearModule
    {
        private DataBaseService dataBaseService;

        public int TotalRegearSilverAmount { get; set; }
        private int goldTierRegearCap = 1700000;
        private int silverTierRegearCap = 1300000;
        private int bronzeTierRegearCap = 8000000;
        private int iTankMinimumIP = 1400;
        private int iDPSMinimumIP = 1450;
        private int iHealerMinmumIP = 1350;
        private int iSupportMinimumIP = 1350;

        public async Task PostRegear(SocketInteractionContext command, PlayerDataHandler.Rootobject eventData, string partyLeader, string reason, MoneyTypes moneyTypes)
        {
            ulong id = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("regearTeamChannelId"));

            var chnl = command.Client.GetChannel(id) as IMessageChannel;


            var marketDataAndGearImg = await GetMarketDataAndGearImg(command, eventData.Victim.Equipment, eventData.Victim.Name);

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
                                    .AddField("KillID: ", eventData.EventId, true)
                                    .AddField("Victim", eventData.Victim.Name, true)
                                    .AddField("Caller Name: ", partyLeader)
                                    .AddField("Killer", "[" + eventData.Killer.AllianceName + "] " + "[" + eventData.Killer.GuildName + "] " + eventData.Killer.Name)
                                    .AddField("Death Average IP ", eventData.Victim.AverageItemPower)
                                    .AddField("Refund Amount: ", TotalRegearSilverAmount)
                                    
                                    //.AddField("Death Location: ", eventData.KillArea)

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
                if ((guildUser.Roles.Any(r => r.Name == "Silver Tier Regear - Elligible" || r.Name == "Gold Tier Regear - Elligible")) || (guildUser.Roles.Any(r => r.Id == GoldTierID || r.Id == SilverTierID)))
                {
                    return true;
                }
                //ADD BRONZE REGEAR LOGIC
            }
            return true;

        }

        public async Task<List<string>> GetMarketDataAndGearImg(SocketInteractionContext command, PlayerDataHandler.Equipment1 victimEquipment, string victimName)
        {
            AlbionOnlineDataParser.AlbionOnlineDataParser.InitializeAlbionDataProjectCurrentPrices();

            int returnValue = 0;
            int returnNotUnderRegearValue = 0;
            string? sMarketLocation = "";//System.Configuration.ConfigurationManager.AppSettings.Get("chosenCityMarket"); //If field is null, all cities market data will be pulled
            bool bAddAllQualities = false;
            int iDefaultItemQuality = 2;
            var guildUser = (SocketGuildUser)command.User;
            var regearIconType = "";

            

            //string? head = (victimEquipment.Head != null) ? $"{victimEquipment.Head.Type + $"?Locations={sMarketLocation}&qualities=" + victimEquipment.Head.Quality}" : null;
            //string? mainhand = (victimEquipment.MainHand != null) ? $"{victimEquipment.MainHand.Type + $"?Locations={sMarketLocation}&qualities=" + victimEquipment.MainHand.Quality}" : null;
            //string? offhand = (victimEquipment.OffHand != null) ? $"{victimEquipment.OffHand.Type + $"?Locations={sMarketLocation}&qualities=" + victimEquipment.OffHand.Quality}" : null;
            //string? cape = (victimEquipment.Cape != null) ? $"{victimEquipment.Cape.Type + $"?Locations={sMarketLocation}&qualities=" + victimEquipment.Cape.Quality}" : null;
            //string? armor = (victimEquipment.Armor != null) ? $"{victimEquipment.Armor.Type + $"?Locations={sMarketLocation}&qualities=" + victimEquipment.Armor.Quality}" : null;
            //string? boots = (victimEquipment.Shoes != null) ? $"{victimEquipment.Shoes.Type + $"?Locations={sMarketLocation}&qualities=" + victimEquipment.Shoes.Quality}" : null;
            //string? mount = (victimEquipment.Mount != null) ? $"{victimEquipment.Mount.Type + $"?Locations={sMarketLocation}&qualities=" + victimEquipment.Mount.Quality}" : null;

            //var placeholder = "https://render.albiononline.com/v1/item/T1_WOOD.png";
            //var headImg = (victimEquipment.Head != null) ? $"https://render.albiononline.com/v1/item/{victimEquipment.Head.Type + "?quality=" + victimEquipment.Head.Quality}" : placeholder;
            //var weaponImg = (victimEquipment.MainHand != null) ? $"https://render.albiononline.com/v1/item/{victimEquipment.MainHand.Type + "?quality=" + victimEquipment.MainHand.Quality}" : placeholder;
            //var offhandImg = (victimEquipment.OffHand != null) ? $"https://render.albiononline.com/v1/item/{victimEquipment.OffHand.Type + "?quality=" + victimEquipment.OffHand.Quality}" : placeholder;
            //var capeImg = (victimEquipment.Cape != null) ? $"https://render.albiononline.com/v1/item/{victimEquipment.Cape.Type + "?quality=" + victimEquipment.Cape.Quality}" : placeholder;
            //var armorImg = (victimEquipment.Armor != null) ? $"https://render.albiononline.com/v1/item/{victimEquipment.Armor.Type + "?quality=" + victimEquipment.Armor.Quality}" : placeholder;
            //var bootsImg = (victimEquipment.Shoes != null) ? $"https://render.albiononline.com/v1/item/{victimEquipment.Shoes.Type + "?quality=" + victimEquipment.Shoes.Quality}" : placeholder;
            //var mountImg = (victimEquipment.Mount != null) ? $"https://render.albiononline.com/v1/item/{victimEquipment.Mount.Type + "?quality=" + victimEquipment.Mount.Quality}" : placeholder;


            List<string> equipmentList = new List<string>();
            List<Equipment> underRegearList = new List<Equipment>();
            List<string> notAvailableInMarketList = new List<string>();
            #region test
            //if (victimEquipment.Head != null)
            //{
            //    if (victimEquipment.Head.Type.Contains("T5") || victimEquipment.Head.Type.Contains("T6") || victimEquipment.Head.Type.Contains("T7") || victimEquipment.Head.Type.Contains("T8"))
            //    {
            //        if (victimEquipment.Head.Type.Contains("T5") && victimEquipment.Head.Type.Contains("@3"))
            //        {
            //            equipmentList.Add(head);
            //            underRegearList.Add(headImg);
            //        }
            //        else if (victimEquipment.Head.Type.Contains("T6") && (victimEquipment.Head.Type.Contains("@2") || victimEquipment.Head.Type.Contains("@3")))
            //        {
            //            equipmentList.Add(head);
            //            underRegearList.Add(headImg);
            //        }
            //        else if (victimEquipment.Head.Type.Contains("T7") && (victimEquipment.Head.Type.Contains("@1") || victimEquipment.Head.Type.Contains("@2")))
            //        {
            //            equipmentList.Add(head);
            //            underRegearList.Add(headImg);
            //        }
            //        else if (victimEquipment.Head.Type.Contains("T8"))
            //        {
            //            equipmentList.Add(head);
            //            underRegearList.Add(headImg);
            //        }
            //        else
            //        {
            //            notUnderRegearEquipmentList.Add(head);
            //            notUnderRegearList.Add(headImg);
            //        }
            //    }
            //    else
            //    {
            //        notUnderRegearEquipmentList.Add(head);
            //        notUnderRegearList.Add(headImg);
            //    }
            //}
            //if (victimEquipment.MainHand != null)
            //{
            //    if (victimEquipment.MainHand.Type.Contains("T5") || victimEquipment.MainHand.Type.Contains("T6") || victimEquipment.MainHand.Type.Contains("T7") || victimEquipment.MainHand.Type.Contains("T8"))
            //    {
            //        if (victimEquipment.MainHand.Type.Contains("T5") && victimEquipment.MainHand.Type.Contains("@3"))
            //        {
            //            equipmentList.Add(weapon);
            //            underRegearList.Add(weaponImg);
            //        }
            //        else if (victimEquipment.MainHand.Type.Contains("T6") && (victimEquipment.MainHand.Type.Contains("@2") || victimEquipment.MainHand.Type.Contains("@3")))
            //        {
            //            equipmentList.Add(weapon);
            //            underRegearList.Add(weaponImg);
            //        }
            //        else if (victimEquipment.MainHand.Type.Contains("T7") && (victimEquipment.MainHand.Type.Contains("@1") || victimEquipment.MainHand.Type.Contains("@2")))
            //        {
            //            equipmentList.Add(weapon);
            //            underRegearList.Add(weaponImg);
            //        }
            //        else if (victimEquipment.MainHand.Type.Contains("T8"))
            //        {
            //            equipmentList.Add(weapon);
            //            underRegearList.Add(weaponImg);
            //        }
            //        else
            //        {
            //            notUnderRegearEquipmentList.Add(weapon);
            //            notUnderRegearList.Add(weaponImg);
            //        }
            //    }
            //    else
            //    {
            //        notUnderRegearEquipmentList.Add(weapon);
            //        notUnderRegearList.Add(weaponImg);
            //    }
            //}
            //if (victimEquipment.OffHand != null)
            //{
            //    if (victimEquipment.OffHand != null)
            //    {
            //        if (victimEquipment.OffHand.Type.Contains("T5") || victimEquipment.OffHand.Type.Contains("T6") || victimEquipment.OffHand.Type.Contains("T7") || victimEquipment.OffHand.Type.Contains("T8"))
            //        {
            //            if (victimEquipment.OffHand.Type.Contains("T5") && victimEquipment.OffHand.Type.Contains("@3"))
            //            {
            //                equipmentList.Add(offhand);
            //                underRegearList.Add(offhandImg);
            //            }
            //            else if (victimEquipment.OffHand.Type.Contains("T6") && (victimEquipment.OffHand.Type.Contains("@2") || victimEquipment.OffHand.Type.Contains("@3")))
            //            {
            //                equipmentList.Add(offhand);
            //                underRegearList.Add(offhandImg);
            //            }
            //            else if (victimEquipment.OffHand.Type.Contains("T7") && (victimEquipment.OffHand.Type.Contains("@1") || victimEquipment.OffHand.Type.Contains("@2")))
            //            {
            //                equipmentList.Add(offhand);
            //                underRegearList.Add(offhandImg);
            //            }
            //            else if (victimEquipment.OffHand.Type.Contains("T8"))
            //            {
            //                equipmentList.Add(offhand);
            //                underRegearList.Add(offhandImg);
            //            }
            //            else
            //            {
            //                notUnderRegearEquipmentList.Add(offhand);
            //                notUnderRegearList.Add(offhandImg);
            //            }
            //        }
            //        else
            //        {
            //            notUnderRegearEquipmentList.Add(offhand);
            //            notUnderRegearList.Add(offhandImg);
            //        }
            //    }
            //}
            //if (victimEquipment.Armor != null)
            //{
            //    if (victimEquipment.Armor.Type.Contains("T5") || victimEquipment.Armor.Type.Contains("T6") || victimEquipment.Armor.Type.Contains("T7") || victimEquipment.Armor.Type.Contains("T8"))
            //    {
            //        if (victimEquipment.Armor.Type.Contains("T5") && victimEquipment.Armor.Type.Contains("@3"))
            //        {
            //            equipmentList.Add(armor);
            //            underRegearList.Add(armorImg);
            //        }
            //        else if (victimEquipment.Armor.Type.Contains("T6") && (victimEquipment.Armor.Type.Contains("@2") || victimEquipment.Armor.Type.Contains("@3")))
            //        {
            //            equipmentList.Add(armor);
            //            underRegearList.Add(armorImg);
            //        }
            //        else if (victimEquipment.Armor.Type.Contains("T7") && (victimEquipment.Armor.Type.Contains("@1") || victimEquipment.Armor.Type.Contains("@2")))
            //        {
            //            equipmentList.Add(armor);
            //            underRegearList.Add(armorImg);
            //        }
            //        else if (victimEquipment.Armor.Type.Contains("T8"))
            //        {
            //            equipmentList.Add(armor);
            //            underRegearList.Add(armorImg);
            //        }
            //        else
            //        {
            //            notUnderRegearEquipmentList.Add(armor);
            //            notUnderRegearList.Add(armorImg);
            //        }
            //    }
            //    else
            //    {
            //        notUnderRegearEquipmentList.Add(armor);
            //        notUnderRegearList.Add(armorImg);
            //    }
            //}
            //if (victimEquipment.Shoes != null)
            //{
            //    if (victimEquipment.Shoes.Type.Contains("T5") || victimEquipment.Shoes.Type.Contains("T6") || victimEquipment.Shoes.Type.Contains("T7") || victimEquipment.Shoes.Type.Contains("T8"))
            //    {
            //        if (victimEquipment.Shoes.Type.Contains("T5") && victimEquipment.Shoes.Type.Contains("@3"))
            //        {
            //            equipmentList.Add(boots);
            //            underRegearList.Add(bootsImg);
            //        }
            //        else if (victimEquipment.Shoes.Type.Contains("T6") && (victimEquipment.Shoes.Type.Contains("@2") || victimEquipment.Shoes.Type.Contains("@3")))
            //        {
            //            equipmentList.Add(boots);
            //            underRegearList.Add(bootsImg);
            //        }
            //        else if (victimEquipment.Shoes.Type.Contains("T7") && (victimEquipment.Shoes.Type.Contains("@1") || victimEquipment.Shoes.Type.Contains("@2")))
            //        {
            //            equipmentList.Add(boots);
            //            underRegearList.Add(bootsImg);
            //        }
            //        else if (victimEquipment.Shoes.Type.Contains("T8"))
            //        {
            //            equipmentList.Add(boots);
            //            underRegearList.Add(bootsImg);
            //        }
            //        else
            //        {
            //            notUnderRegearEquipmentList.Add(boots);
            //            notUnderRegearList.Add(bootsImg);
            //        }
            //    }
            //    else
            //    {
            //        notUnderRegearEquipmentList.Add(boots);
            //        notUnderRegearList.Add(bootsImg);
            //    }
            //}
            //if (victimEquipment.Cape != null)
            //{
            //    if (victimEquipment.Cape.Type.Contains("T4") || victimEquipment.Cape.Type.Contains("T5") || victimEquipment.Cape.Type.Contains("T6") || victimEquipment.Cape.Type.Contains("T7") || victimEquipment.Cape.Type.Contains("T8"))
            //    {
            //        if (victimEquipment.Cape.Type.Contains("T4") && victimEquipment.Cape.Type.Contains("@3"))
            //        {
            //            equipmentList.Add(cape);
            //            underRegearList.Add(capeImg);
            //        }
            //        else if (victimEquipment.Cape.Type.Contains("T5") && (victimEquipment.Cape.Type.Contains("@2") || victimEquipment.Cape.Type.Contains("@3")))
            //        {
            //            equipmentList.Add(cape);
            //            underRegearList.Add(capeImg);
            //        }
            //        else if (victimEquipment.Cape.Type.Contains("T6") && (victimEquipment.Cape.Type.Contains("@1") || victimEquipment.Cape.Type.Contains("@2") || victimEquipment.Cape.Type.Contains("@3")))
            //        {
            //            equipmentList.Add(cape);
            //            underRegearList.Add(capeImg);
            //        }
            //        else if (victimEquipment.Cape.Type.Contains("T7"))
            //        {
            //            equipmentList.Add(cape);
            //            underRegearList.Add(capeImg);
            //        }
            //        else if (victimEquipment.Cape.Type.Contains("T8"))
            //        {
            //            equipmentList.Add(cape);
            //            underRegearList.Add(capeImg);
            //        }
            //        else
            //        {
            //            notUnderRegearEquipmentList.Add(cape);
            //            notUnderRegearList.Add(capeImg);
            //        }
            //    }
            //    else
            //    {
            //        notUnderRegearEquipmentList.Add(cape);
            //        notUnderRegearList.Add(capeImg);
            //    }
            //}
            //if (victimEquipment.Mount != null)
            //{
            //    equipmentList.Add(mount);
            //    underRegearList.Add(mountImg);
            //}
            #endregion

            if (victimEquipment.Head != null)
            {
                equipmentList.Add($"{victimEquipment.Head.Type + $"?Locations={sMarketLocation}&qualities=" + victimEquipment.Head.Quality}");
                underRegearList.Add(new Equipment
                {
                    Image = $"https://render.albiononline.com/v1/item/{victimEquipment.Head.Type + "?quality=" + victimEquipment.Head.Quality}",
                    Type = "HEAD"
                });
            }

            if (victimEquipment.MainHand != null)
            {
                equipmentList.Add($"{victimEquipment.MainHand.Type + $"?Locations={sMarketLocation}&qualities=" + victimEquipment.MainHand.Quality}");
                underRegearList.Add(new Equipment
                {
                    Image = $"https://render.albiononline.com/v1/item/{victimEquipment.MainHand.Type + "?quality=" + victimEquipment.MainHand.Quality}",
                    Type = "MAIN"
                });
            }
            if (victimEquipment.OffHand != null)
            {
                equipmentList.Add($"{victimEquipment.OffHand.Type + $"?Locations={sMarketLocation}&qualities=" + victimEquipment.OffHand.Quality}");
                underRegearList.Add(new Equipment
                {
                    Image = $"https://render.albiononline.com/v1/item/{victimEquipment.OffHand.Type + "?quality=" + victimEquipment.OffHand.Quality}",
                    Type = "OFF"
                });
            }

            if (victimEquipment.Armor != null)
            {
                equipmentList.Add($"{victimEquipment.Armor.Type + $"?Locations={sMarketLocation}&qualities=" + victimEquipment.Armor.Quality}");
                underRegearList.Add(new Equipment
                {
                    Image = $"https://render.albiononline.com/v1/item/{victimEquipment.Armor.Type + "?quality=" + victimEquipment.Armor.Quality}",
                    Type = "ARMOR"
                });
            }

            if (victimEquipment.Shoes != null)
            {
                equipmentList.Add($"{victimEquipment.Shoes.Type + $"?Locations={sMarketLocation}&qualities=" + victimEquipment.Shoes.Quality}");
                underRegearList.Add(new Equipment
                {
                    Image = $"https://render.albiononline.com/v1/item/{victimEquipment.Shoes.Type + "?quality=" + victimEquipment.Shoes.Quality}",
                    Type = "SHOES"
                });
            }

            if (victimEquipment.Cape != null)
            {
                equipmentList.Add($"{victimEquipment.Cape.Type + $"?Locations={sMarketLocation}&qualities=" + victimEquipment.Cape.Quality}");
                underRegearList.Add(new Equipment
                {
                    Image = $"https://render.albiononline.com/v1/item/{victimEquipment.Cape.Type + "?quality=" + victimEquipment.Cape.Quality}",
                    Type = "CAPEITEM"
                });
            }
            if (victimEquipment.Mount != null)
            {
                equipmentList.Add($"{victimEquipment.Mount.Type + $"?Locations={sMarketLocation}&qualities=" + victimEquipment.Mount.Quality}");
                underRegearList.Add(new Equipment
                {
                    Image = $"https://render.albiononline.com/v1/item/{victimEquipment.Mount.Type + "?quality=" + victimEquipment.Mount.Quality}",
                    Type = "MOUNT"
                });
            }

            foreach (var item in equipmentList)
            {
                string itemType = (item.Split('_')[1] == "2H") ? "MAIN" : item.Split('_')[1];
                Equipment underRegearItem = underRegearList.Where(x => x.Type.Contains(itemType)).FirstOrDefault();


                //Check for 24 Day Average
                Task<List<EquipmentMarketData>> marketData = new MarketDataFetching().GetMarketPrice24dayAverage(item);
                //await Task.Delay(1000);

                if ((marketData.Result == null || marketData.Result.Count == 0) || marketData.Result.FirstOrDefault().sell_price_min == 0)
                {
                    //Check for Daily Average
                    marketData = new MarketDataFetching().GetMarketPriceDailyAverage(item);
                    //await Task.Delay(1000);

                    
                    if ((marketData.Result == null || marketData.Result.Count == 0) || marketData.Result.FirstOrDefault().sell_price_min == 0)
                    {
                        //Check for Current Price
                         marketData = new MarketDataFetching().GetMarketPriceCurrentAsync(item);
                        //await Task.Delay(1000);

                        if ((marketData.Result == null || marketData.Result.Count == 0) || marketData.Result.FirstOrDefault().sell_price_min == 0)
                        {
                            notAvailableInMarketList.Add(marketData.Result.FirstOrDefault().item_id.Replace('_', ' ').Replace('@', '.'));
                            underRegearItem.ItemPrice = "$0 (Not Found)";
                        }
                    }
                }

                if (marketData.Result.FirstOrDefault().sell_price_min != 0)
                {
                    returnValue += marketData.Result.FirstOrDefault().sell_price_min; 
                    underRegearItem.ItemPrice = "$" + marketData.Result.FirstOrDefault().sell_price_min.ToString();
                }
                //else
                //{
                        
                //    notAvailableInMarketList.Add(marketData.Result.FirstOrDefault().item_id.Replace('_', ' ').Replace('@', '.'));
                //    underRegearItem.ItemPrice = "$0 (Not Found)";
                //}
            }
        
            if (guildUser.Roles.Any(r => r.Name == "Silver Tier Regear - Elligible")) //ROLE ID 1031731037149081630
            {
                returnValue = Math.Min(silverTierRegearCap, returnValue);
                regearIconType = "Silver Tier Regear - Elligible";
            }
            else if (guildUser.Roles.Any(r => r.Name == "Gold Tier Regear - Elligible")) // Role ID 1031731127431479428
            {
                returnValue = returnValue = Math.Min(goldTierRegearCap, returnValue);
                regearIconType = "Gold Tier Regear - Elligible";
            }
            else
            {
                returnValue = returnValue = Math.Min(bronzeTierRegearCap, returnValue);
                regearIconType = "Bronze Tier Regear - Elligible";
            }

            TotalRegearSilverAmount = returnValue;

            var gearImage = $"<div style='background-color: #c7a98f;'> <div> <center><h3>Regear Submitted By {victimName} ({regearIconType})</h3>";
            foreach (var item in underRegearList)
            {
                gearImage += $"<div style='display: inline-block;line-height: .1;'>" +
                    $"<img style='width:150px;height:150px' src='{item.Image}'/>" +
                    $"<p >{item.ItemPrice}</p></div>";
            }

            gearImage += $"<div style='font-weight : bold;'>Refund amt. : {returnValue}</div></center></div>";
            gearImage += $"<center><br/><h3> Items not found or price is too high </h3>";

            foreach (var item in notAvailableInMarketList)
            {
                gearImage += $"{item}<br/>";
            }

            gearImage += $"</center></div>";

            return new List<string> { gearImage, returnValue.ToString() };
        }

        private bool IsRegearTankClass(string a_sGearItem)
        {
            if (a_sGearItem.Contains("HAMMER") || a_sGearItem.Contains("MACE") || a_sGearItem.Contains("KEEPER") || a_sGearItem.Contains("FLAIL"))
            {
                return true;
            }
            return false;
        }

        private bool IsRegearDPSClass(string a_sGearItem)
        {
            if (!IsRegearTankClass(a_sGearItem) && !IsRegearHealerClass(a_sGearItem) && !IsRegearSupportClass(a_sGearItem))
            {
                return true;
            }
            return false;
        }

        private bool IsRegearHealerClass(string a_sGearItem)
        {
            if (a_sGearItem.Contains("HOLY") || a_sGearItem.Contains("NATURE") || a_sGearItem.Contains("WILD") || a_sGearItem.Contains("DIVINE"))
            {
                return true;
            }
            return false;
        }

        private bool IsRegearSupportClass(string a_sGearItem)
        {

            if (a_sGearItem.Contains("ARCANE") || a_sGearItem.Contains("ENIGMATIC") || a_sGearItem.Contains("KEEPER") || a_sGearItem.Contains("FLAIL"))
            {
                return true;
            }
            return false;
        }

        public class Equipment
        {
            private string image;
            private string itemPrice;
            private string type;
            public string Image { get => image; set => image = value; }
            public string ItemPrice { get => itemPrice; set => itemPrice = value; }
            public string Type { get => type; set => type = value; }
        }
    }
}
