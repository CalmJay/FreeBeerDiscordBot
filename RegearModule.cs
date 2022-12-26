using CoreHtmlToImage;
using Discord;
using Discord.WebSocket;
using DiscordBot.Enums;
using DiscordBot.Services;
using PlayerData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord.Interactions;
using MarketData;
using DiscordBot.Models;

namespace DiscordBot.RegearModule
{
    public class RegearModule
    {
        private DataBaseService dataBaseService;

        private int goldTierRegearCap = 1700000;
        private int silverTierRegearCap = 1300000;
        private int bronzeTierRegearCap = 1000000;
        private int shitTierRegearCap = 600000;
        private int iTankMinimumIP = 1450;
        private int iDPSMinimumIP = 1450;
        private int iHealerMinmumIP = 1350;
        private int iSupportMinimumIP = 1350;

        public double TotalRegearSilverAmount { get; set; }
        public ulong RegearQueueID { get; set; }
        private ClassType eRegearClassType { get; set; }

        private string regearRoleIcon { get; set; }


        public async Task PostRegear(SocketInteractionContext command, PlayerDataHandler.Rootobject a_EventData, string partyLeader, string reason, MoneyTypes moneyTypes)
        {
            ulong id = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("regearTeamChannelId"));

            var chnl = command.Client.GetChannel(id) as IMessageChannel;

            var marketDataAndGearImg = await GetMarketDataAndGearImg(command, a_EventData);

            var converter = new HtmlConverter();
            var html = marketDataAndGearImg[0];
            var bytes = converter.FromHtmlString(html);

            RegearQueueID = command.Interaction.Id;

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
                dataBaseService = new DataBaseService();
                var player = dataBaseService.GetPlayerInfoByName(a_EventData.Victim.Name);
                var moneyType = dataBaseService.GetMoneyTypeByName(moneyTypes);
                await dataBaseService.AddPlayerReGear(new PlayerLoot
                {
                    TypeId = moneyType.Id,
                    CreateDate = DateTime.Now,
                    Loot = Convert.ToDecimal(marketDataAndGearImg[1]),
                    PlayerId = player.Id,
                    Message = " Regear(s) have been processed.  Has been added to your account. Please emote :beers: to confirm",
                    PartyLeader = partyLeader,
                    KillId = a_EventData.EventId.ToString(),
                    Reason = reason,
                    QueueId = "0"
                });

                var commandUser = command.Guild.GetUser(command.User.Id);


                using (MemoryStream imgStream = new MemoryStream(bytes))
                {
                    var embed = new EmbedBuilder()
                                    .WithTitle($" {regearRoleIcon} Regear Submission from {a_EventData.Victim.Name} {regearRoleIcon}")
                                    .AddField("KillID: ", a_EventData.EventId, true)
                                    .AddField("Victim", a_EventData.Victim.Name, true)
                                    .AddField("Killer", "[" + a_EventData.Killer.AllianceName + "] " + "[" + a_EventData.Killer.GuildName + "] " + a_EventData.Killer.Name)
                                    .AddField("Caller Name: ", partyLeader, true)
                                    .AddField("Refund Amount: ", TotalRegearSilverAmount, true)
                                    .AddField("Death Average IP ", a_EventData.Victim.AverageItemPower, true)
                                    .AddField("Discord User ID: ", command.User.Id, true)
                                    .AddField("Discord Username", command.User.Username, true)
                                    //.AddField("Date of death", a_EventData.TimeStamp)
                                    .WithUrl($"https://albiononline.com/en/killboard/kill/{a_EventData.EventId}")
                                    .WithCurrentTimestamp()
                                    //<:emoji_name:emoji_id> or <a:animated_emoji_name:emoji_id>
                                    //.WithImageUrl(GearImageRenderSerivce(command))
                                    //.AddField(fb => fb.WithName("🌍 Location").WithValue("https://cdn.discordapp.com/attachments/944305637624533082/1026594623696678932/BAG_603948955.png").WithIsInline(true))
                                    .WithImageUrl($"attachment://image.jpg");

                    CheckRegearRequirments(a_EventData, out bool requirementsMet, out string? errorMessage);

                    if (!requirementsMet)
                    {
                        embed.WithDescription($"<a:red_siren:1050052736206508132> WARNING: {errorMessage} <a:red_siren:1050052736206508132> ");
                        embed.Color = Color.Red;
                    }
                    else if(!UserHaveRegearRole(command))
                    {
                        embed.WithDescription($"<a:red_siren:1050052736206508132>  THIS MEMBER DOESN'T HAVE REGEAR ROLES  <a:red_siren:1050052736206508132> ");
                        embed.Color = Color.Red;
                    }
                    else
                    {
                        embed.WithDescription($":thumbsup:  Requreiments Met: This regear meets Free Beer Standards  :thumbsup: ");
                        embed.Color = Color.Green;
                    }

                    await chnl.SendFileAsync(imgStream, "image.jpg", $"Regear Submission from <@{command.Guild.GetUser(commandUser.Id).Id}>", false, embed.Build(), null, false, null, null, components: component.Build());


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
            ulong BronzeTierID = Convert.ToUInt64(System.Configuration.ConfigurationManager.AppSettings.Get("BronzeTierRegear"));

            if (socketInteractionUser.User is SocketGuildUser guildUser)
            {
                if ((guildUser.Roles.Any(r => r.Name == "Silver Tier Regear - Elligible" || r.Name == "Gold Tier Regear - Elligible" || r.Name == "Bronze Tier Regear - Elligible")) || (guildUser.Roles.Any(r => r.Id == GoldTierID || r.Id == SilverTierID)))
                {
                    return true;
                }
                //ADD BRONZE REGEAR LOGIC
            }
            return true;

        }

        public async Task<List<string>> GetMarketDataAndGearImg(SocketInteractionContext command, PlayerDataHandler.Rootobject a_Playerdata)
        {
            double returnValue = 0;
            string sMarketLocation = "Bridgewatch,BridgewatchPortal,Caerleon,FortSterling,FortSterling,Lymhurst,LymhurstPortal,Martlock,martlockportal,Thetford,Thetfordportal";//System.Configuration.ConfigurationManager.AppSettings.Get("chosenCityMarket"); //If field is null, all cities market data will be pulled
            var guildUser = (SocketGuildUser)command.User;
            var regearIconType = "";
            PlayerDataHandler.Equipment1 victimEquipment = a_Playerdata.Victim.Equipment;

            List<string> equipmentList = new List<string>();
            List<Equipment> underRegearList = new List<Equipment>();
            List<string> notAvailableInMarketList = new List<string>();

            #region OldTierCode
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
                    Type = "CAPE"
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

                Equipment underRegearItem = underRegearList.Where(x => itemType.Contains(x.Type)).FirstOrDefault();
                MarketDataFetching marketDataFetching = new MarketDataFetching();

                //Check for Current Price
                List<EquipmentMarketData> marketDataCurrent = await marketDataFetching.GetMarketPriceCurrentAsync(item);

                if (marketDataCurrent == null || marketDataCurrent.Where(x => x.sell_price_min != 0).Count() == 0)
                {
                    //Check for Daily Average
                    List<AverageItemPrice> marketDataDaily = await marketDataFetching.GetMarketPriceDailyAverage(item);

                    if (marketDataDaily == null || marketDataDaily.Where(x => x.data != null).Count() == 0)
                    {
                        

                        //Check for 24 Day Average
                        List<EquipmentMarketDataMonthylyAverage> marketDataMonthly = await marketDataFetching.GetMarketPriceMonthlyAverage(item);
                        if (marketDataMonthly == null || marketDataMonthly.Where(x => x.prices_avg != null).Count() == 0)
                        {
                            notAvailableInMarketList.Add(marketDataCurrent.FirstOrDefault().item_id.Replace('_', ' ').Replace('@', '.'));
                            underRegearItem.ItemPrice = "$0 (Not Found)";
                        }
                        else
                        {
                            //get monthly prices
                            var equipmentFetchPrice = FetchItemPrice(marketDataMonthly, out string? errorMessage);
                            returnValue += equipmentFetchPrice;
                            underRegearItem.ItemPrice = (errorMessage == null) ? "$" + equipmentFetchPrice.ToString("N0") : errorMessage;
                            underRegearItem.ItemPrice = (errorMessage == null) ? "$" + equipmentFetchPrice.ToString() : errorMessage;
                        }
                    }
                    else
                    {
                        //get daily prices
                        var equipmentFetchPrice = FetchItemPrice(marketDataDaily, out string? errorMessage);
                        returnValue += equipmentFetchPrice;
                        underRegearItem.ItemPrice = (errorMessage == null) ? "$" + equipmentFetchPrice.ToString("N0") : errorMessage;
                        underRegearItem.ItemPrice = (errorMessage == null) ? "$" + equipmentFetchPrice.ToString() : errorMessage;
                    }
                }
                else
                {
                    //get current prices
                    var equipmentFetchPrice = FetchItemPrice(marketDataCurrent, out string? errorMessage);

                    returnValue += equipmentFetchPrice;
                    underRegearItem.ItemPrice = (errorMessage == null) ? "$" + equipmentFetchPrice.ToString("N0") : errorMessage;
                    underRegearItem.ItemPrice = (errorMessage == null) ? "$" + equipmentFetchPrice.ToString() : errorMessage;
                }
            }


            if (guildUser.Roles.Any(r => r.Name == "Gold Tier Regear - Eligible")) // Role ID 1049889855619989515
            {
                returnValue = returnValue = Math.Min(goldTierRegearCap, returnValue);
                regearIconType = "Gold Tier Regear - Eligible";
                regearRoleIcon = "<:Gold:1009104748542185512>";
            }
            else if (guildUser.Roles.Any(r => r.Name == "Silver Tier Regear - Eligible")) //ROLE ID 970083338591289364
            {
                returnValue = Math.Min(silverTierRegearCap, returnValue);
                regearIconType = "Silver Tier Regear - Eligible";
                regearRoleIcon = "<:Silver:1009104762484047982>";
            }
            else if (guildUser.Roles.Any(r => r.Name == "Bronze Tier Regear - Eligible")) //Role ID 970083088241672245
            {
                returnValue = returnValue = Math.Min(bronzeTierRegearCap, returnValue);
                regearIconType = "Bronze Tier Regear - Eligible";
                regearRoleIcon = "<:Bronze_Bar:1019676753666527342>";
            }
            else if (guildUser.Roles.Any(r => r.Name == "Free Regear - Eligible")) // Role ID 1052241667329118349
            {
                returnValue = returnValue = Math.Min(bronzeTierRegearCap, returnValue);
                regearIconType = "Free Regear - Eligible";
                regearRoleIcon = "<:FreeRegearToken:1052241548856791040> ";
            }
            else
            {
                returnValue = returnValue = Math.Min(shitTierRegearCap, returnValue);
                regearIconType = "NOT ELIGIBLE FOR REGEAR";
                regearRoleIcon = ":poop:";
            }

            TotalRegearSilverAmount = returnValue;

            var gearImage = $"<div style='background-color: #c7a98f;'> <div> <center><h3>Regear Submitted By {a_Playerdata.Victim.Name} ({regearIconType})</h3>";
            foreach (var item in underRegearList)
            {
                gearImage += $"<div style='display: inline-block;line-height: .1;'>" +
                    $"<img style='width:150px;height:150px' src='{item.Image}'/>" +
                    $"<p >{item.ItemPrice}</p></div>";
            }

            gearImage += $"<div style='font-weight : bold;'>Refund amount. : {returnValue.ToString("N0")}</div></center></div>";

            gearImage += $"<center><br/>";
            if (notAvailableInMarketList.Count() != 0)
            {
                gearImage += $"<center><br/><h3> Items not found or price is too high </h3>";
            }

            foreach (var item in notAvailableInMarketList)
            {
                gearImage += $"{item}<br/>";
            }

            gearImage += $"</center></div>";

            return new List<string> { gearImage, returnValue.ToString("N0") };
        }

        private ClassType GetRegearClassType(string a_sGearItem)
        {
            ClassType returnvalue = ClassType.Unknown;

            if (IsRegearTankClass(a_sGearItem))
            {
                returnvalue = ClassType.Tank;
            }
            else if (IsRegearDPSClass(a_sGearItem))
            {
                returnvalue = ClassType.DPS;
            }
            else if (IsRegearHealerClass(a_sGearItem))
            {
                returnvalue = ClassType.Healer;
            }
            else if (IsRegearSupportClass(a_sGearItem))
            {
                returnvalue = ClassType.Support;
            }

            return returnvalue;
        }

        public void CheckRegearRequirments(PlayerDataHandler.Rootobject a_EventData, out bool requirementsMet, out string? ErrorMessage)
        {

            requirementsMet = true;
            ErrorMessage = null;
            ClassType eClassTypeEnum = ClassType.Unknown;


            if (a_EventData.Victim.Equipment.MainHand != null)
            {
                eClassTypeEnum = GetRegearClassType(a_EventData.Victim.Equipment.MainHand.Type.ToString());
            }


            switch (eClassTypeEnum)
            {
                case ClassType.Tank:

                    if (a_EventData.Victim.AverageItemPower < iTankMinimumIP)
                    {
                        ErrorMessage = $"Tank Regear doesn't meet the minimum IP requirments of at least {iTankMinimumIP}";
                        requirementsMet = false;
                    }

                    break;

                case ClassType.DPS:
                    if (a_EventData.Victim.AverageItemPower < iDPSMinimumIP)
                    {
                        ErrorMessage = $"DPS Regear doesn't meet the minimum IP requirments of at least {iDPSMinimumIP}";
                        requirementsMet = false;
                    }
                    break;

                case ClassType.Healer:
                    if (a_EventData.Victim.AverageItemPower < iHealerMinmumIP)
                    {
                        ErrorMessage = $"Healing Regear doesn't meet the minimum IP requirments of at least {iHealerMinmumIP}";
                        requirementsMet = false;
                    }
                    break;

                case ClassType.Support:
                    if (a_EventData.Victim.AverageItemPower < iSupportMinimumIP)
                    {
                        ErrorMessage = $"Support Regear doesn't meet the minimum IP requirments of at least {iSupportMinimumIP}";
                        requirementsMet = false;
                    }
                    break;
                default:
                    ErrorMessage = "REGEAR CLASS NOT FOUND.";
                    requirementsMet = false;
                    break;
            }

            if (a_EventData.Victim.Equipment.Cape != null && a_EventData.Victim.Equipment.Cape.ToString().Contains("@3"))
            {
                ErrorMessage = $"Cape doesn't meet the minimum requriments of being at least 4.3";
                requirementsMet = false;
            }

            string mountTier = a_EventData.Victim.Equipment.Mount.Type.ToString().Split('_')[0];
            if (a_EventData.Victim.Equipment.Mount.Type.ToString().Contains("UNIQUE_MOUNT") || mountTier == "T5" || mountTier == "T6" || mountTier == "T7" || mountTier == "T8")
            {

            }
            else
            {
                ErrorMessage = $"Mount is not T5 equivalent or higher";
                requirementsMet = false;
            }
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

            if (a_sGearItem.Contains("ARCANE") || a_sGearItem.Contains("ENIGMATIC") || a_sGearItem.Contains("KEEPER") || a_sGearItem.Contains("FLAIL") || a_sGearItem.Contains("ENIGMATICORB") || a_sGearItem.Contains("CURSEDSTAFF"))
            {
                return true;
            }
            return false;
        }

        public double FetchItemPrice<T>(T Test, out string? ErrorMessage)
        {
            double returnValue = 0;
            ErrorMessage = null;

            if (Test.GetType() == typeof(List<EquipmentMarketData>))
            {
                var marketData = (List<EquipmentMarketData>)(object)Test;
                var itemsData = marketData.Where(x => x.sell_price_min != 0);

                if (itemsData != null)
                {
                    if (itemsData.Count() != 0 && itemsData.Where(x => x.sell_price_min != 0).FirstOrDefault().sell_price_min != 0)
                    {
                        var value = itemsData.Min(x => x.sell_price_min);

                        if (value < 5000000)// Very simple check to verify if a single item is too high. (a single item shouldn't cost over 5 mil. more checks need to be in place)
                        {
                            returnValue = value;
                        }
                        else
                        {
                            ErrorMessage = $"$0 (Price to high - {value}";
                        }
                    }
                }
                else
                {
                    ErrorMessage = "$0 (Price not found)";
                }
                return returnValue;
            }
            if (Test.GetType() == typeof(List<AverageItemPrice>))
            {
                var marketData = (List<AverageItemPrice>)(object)Test;
                var itemsData = marketData.Where(x => x.data.Any(x => x.avg_price != 0));

                if (itemsData != null)
                {
                    if (marketData.Count() != 0 && marketData.Where(x => x.data.Any(x => x.avg_price != 0)).FirstOrDefault().data.FirstOrDefault().avg_price != 0)
                    {
                        var value = marketData.Min(x => x.data.Min(x => x.avg_price));

                        if (value < 5000000)// Very simple check to verify if a single item is too high. (a single item shouldn't cost over 5 mil. more checks need to be in place)
                        {
                            returnValue = value;
                        }
                        else
                        {
                            ErrorMessage = $"$0 (Price to high - {value}";
                        }
                    }
                }
                else
                {
                    ErrorMessage = "$0 (Price not found)";
                }
                return returnValue;

            }
            if (Test.GetType() == typeof(List<EquipmentMarketDataMonthylyAverage>))
            {
                var marketData = (List<EquipmentMarketDataMonthylyAverage>)(object)Test;
                var itemsData = marketData.Where(x => x.prices_avg.Any(x => x != 0));

                if (itemsData != null)
                {
                    if (marketData.Count() != 0 && marketData.Where(x => x.prices_avg.Any(x => x != 0)).FirstOrDefault().prices_avg.FirstOrDefault() != 0)
                    {
                        var value = marketData.Min(x => x.prices_avg.Min());

                        if (value < 5000000)// Very simple check to verify if a single item is too high. (a single item shouldn't cost over 5 mil. more checks need to be in place)
                        {
                            returnValue = value;
                        }
                        else
                        {
                            ErrorMessage = $"$0 (Price to high - {value}";
                        }
                    }
                }
                else
                {
                    ErrorMessage = "$0 (Price not found)";
                }
            }
            return returnValue;

        }

        public bool UserHaveRegearRole(SocketInteractionContext a_SocketContext)
        {

            var guildUser = (SocketGuildUser)a_SocketContext.User;

            if (guildUser.Roles.Any(r => r.Name == "Gold Tier Regear - Eligible")) // Role ID 1049889855619989515
            {
                return true;
            }
            else if (guildUser.Roles.Any(r => r.Name == "Silver Tier Regear - Eligible")) //ROLE ID 970083338591289364
            {
                return true;
            }
            else if (guildUser.Roles.Any(r => r.Name == "Bronze Tier Regear - Eligible")) //Role ID 970083088241672245
            {
                return true;
            }
            else if (guildUser.Roles.Any(r => r.Name == "Free Regear - Eligible")) // Role ID 1052241667329118349
            {
                return true;
            }
            else
            {
                return false;
            }
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
