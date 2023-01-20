using System;
using System.Threading.Tasks;
using Discord;
using DiscordBot.Services;
using Discord.Interactions;
using System.Threading.Channels;
using System.ComponentModel;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json;
using System.Diagnostics;
using Discord.WebSocket;

namespace DiscordBot.LootSplitModule
{
    public class LootSplitModule
    {
        public ulong roleIdNewRecruit = 847350505977675796;
        public ulong roleIdMember = 739948841847095387;
        public ulong roleIdOfficer = 335894631810334720;
        public ulong roleIdVeteran = 739950349405782046;
        public Dictionary<string, ulong> scrapedDict { get; set; }
        public List<string> scrapedList { get; set; }
        public int imageCount { get; set; }
        public string freeBeerDirectory { get; set; }
        public List<string> imageMembers { get; set; }
        public string submitter { get; set; }
        public string addedMembers { get; set; }
        public decimal lootAmountPer { get; set; }
        public Dictionary<string, ulong> CreateMemberDict()
        {
            return scrapedDict;
        }
        public async Task ScrapeImages(SocketInteractionContext context)
        {
            //find curr dir and change to the freebeerdiscordbot directory
            string currdir = Directory.GetCurrentDirectory();
            string parent = Directory.GetParent(currdir).FullName;
            string parentTwo = Directory.GetParent(parent).FullName;
            string freeBeerDir = Directory.GetParent(parentTwo).FullName;

            //create the temp dir if not existing
            string tempDir = @freeBeerDir + "\\Temp";
            if (!Directory.Exists(tempDir))
            {
                Directory.CreateDirectory(tempDir);
            }

            freeBeerDirectory = freeBeerDir;

            //scrape thread for image uploads
            var msgsIterable = context.Channel.GetMessagesAsync().ToListAsync().Result.ToList();

            //create and fill list with n urls from channel
            List<string> msgsUrls = new List<string>();

            ////NEED FIX TO GET ALL ATTACHMENTS FROM EACH MESSAGE INSTEAD OF JUST 1
            foreach (var msg in msgsIterable.FirstOrDefault())
            {
                if (msg.Attachments.FirstOrDefault() != null)
                {
                    msgsUrls.Add(msg.Attachments.FirstOrDefault().Url);
                }
            }
            int m = 1;
            foreach (string url in msgsUrls)
            {
                //download the image(s) url that was uploaded to the channel
                using var httpClient = new HttpClient();
                using var s = httpClient.GetStreamAsync(url);
                using var fs = new FileStream(freeBeerDir + "\\Temp\\image" + m.ToString() + ".png", FileMode.OpenOrCreate);
                s.Result.CopyTo(fs);
                m++;
            }
            imageCount = m;
        }
        public async Task CreateMemberList(SocketInteractionContext context)
        {
            //scrape members and write to Json
            List<string> memberList = new List<string>();

            //grab iterable and make list

            var iterable = context.Guild.GetUsersAsync().ToListAsync().Result.ToList();
            foreach (var member in iterable.FirstOrDefault())
            {
                if (member.RoleIds.Contains(roleIdNewRecruit) || member.RoleIds.Contains(roleIdMember)
                    || member.RoleIds.Contains(roleIdOfficer) || member.RoleIds.Contains(roleIdVeteran))
                {
                    //if no nickname, add the username
                    if (member.Nickname is null)
                    { memberList.Add(member.Username); }
                    //if squad leader, remove the dumbass prefix
                    else if (member.Nickname.StartsWith("!slnew"))
                    { memberList.Add(member.Nickname.Remove(0, 7)); }
                    //if neither, just add the Nickname - NEED EVERYONE IN CHANNEL TO HAVE IGNs
                    else if (member.Nickname.StartsWith("!!sl"))
                    { memberList.Add(member.Nickname.Remove(0, 5)); }
                    else if (member.Nickname.StartsWith("!sl"))
                    { memberList.Add(member.Nickname.Remove(0, 4)); }
                    else
                    { memberList.Add(member.Nickname); }
                }
            }

            scrapedList = memberList;

            //serialize and write
            string jsonstring = JsonConvert.SerializeObject(memberList);
            using (StreamWriter writer = System.IO.File.CreateText(freeBeerDirectory + "\\Temp\\members.json"))
            {
                await writer.WriteAsync(jsonstring);
            }
        }
        public async Task CreateMemberDict(SocketInteractionContext context)
        {
            Dictionary<string, ulong> dict = new Dictionary<string, ulong>();

            //grab iterable and make dict

            var iterable = context.Guild.GetUsersAsync().ToListAsync().Result.ToList();
            foreach (var member in iterable.FirstOrDefault())
            {
                if (member.RoleIds.Contains(roleIdNewRecruit) || member.RoleIds.Contains(roleIdMember)
                    || member.RoleIds.Contains(roleIdOfficer) || member.RoleIds.Contains(roleIdVeteran))
                {
                    if (member.Nickname != null)
                    {
                        if (member.Nickname.StartsWith("!!"))
                        {
                            string temp = member.Nickname.Remove(0, 5);
                            dict.Add(temp, member.Id);
                        }
                        else
                        {
                            dict.Add(member.Nickname, member.Id);
                        }
                    }
                    else if (member.Nickname == null)
                    {
                        dict.Add(member.Username, member.Id);
                    }
                    else
                    {
                        continue;
                    }
                }
                scrapedDict = dict;
            }
        }
        public async Task CallPyTesseract(SocketInteractionContext context, string pyPath, string pyArgs)
        {
            ProcessStartInfo start = new ProcessStartInfo();
            start.FileName = pyPath;//cmd is full path to python.exe
            start.Arguments = pyArgs;//pyth is path to .py file and any cmd line args
            start.UseShellExecute = false;
            start.RedirectStandardOutput = true;
            using (Process process = Process.Start(start))
            {
                //read the python output and write it to json
                using (StreamReader reader = process.StandardOutput)
                {
                    string result = reader.ReadToEnd();
                    //serialize string to write to json
                    string jsonstringTwo = JsonConvert.SerializeObject(result);
                    using (StreamWriter writerTwo = System.IO.File.CreateText(freeBeerDirectory + "\\Temp\\members.json"))
                    {
                        await writerTwo.WriteAsync(jsonstringTwo);
                    }

                    //split single string into Icollection? List
                    List<string> results = result.Split(',').ToList();

                    //clean list to strings with double quotes only - DONT USE FOREACH ON ICOLLECTION IT WILL BREAK THE LOOP
                    for (int i = 0; i < results.Count; i++)
                    {
                        //cleanup strings after list separation, there are extra quotations and brackets that are added during
                        //conversion to json-py-console-consoleread; this handles all of it
                        if (i != results.Count - 1)
                        {
                            results[i] = results[i].Remove(0, 2);
                            results[i] = results[i].Remove(results[i].Length - 1);
                        }
                        else
                        {
                            results[i] = results[i].Remove(0, 2);
                            results[i] = results[i].Remove(results[i].Length - 2);
                        }
                    }

                    var guildUser = (SocketGuildUser)context.User;
                    //add user that opened socket - likely the large frame at top of image not captured
                    if (guildUser.Nickname != null)
                    {
                        if (!results.Contains(guildUser.Nickname))
                        {
                            results.Add(guildUser.Nickname);
                            submitter = guildUser.Nickname;
                        }
                        else
                        {
                            submitter = guildUser.Nickname;
                        }
                    }
                    else 
                    {
                        if (!results.Contains(guildUser.Username))
                        {
                            results.Add(guildUser.Username);
                            submitter = guildUser.Username;
                        }
                        else
                        {
                            submitter = guildUser.Username;
                        }
                    }
                    imageMembers = results;
                }

            }
        }
        public async Task CreateFirstEmbed(SocketInteractionContext context)
        {
            //begin embed builder
            var embed = new EmbedBuilder()
            .WithColor(Discord.Color.Orange)
            .AddField("Member Count", imageMembers.Count)
            .AddField(x =>
            {
                //loop results and add members
                x.Name = "Members recorded";
                for (int i = 0; i < (imageMembers.Count - 1); i++)
                {
                    x.Value += imageMembers[i] + ", ";
                }
                x.Value += imageMembers[imageMembers.Count - 1];
                x.IsInline = false;
            });
            //send the embedded report
            await context.Channel.SendMessageAsync("", false, embed.Build());
        }
        public async Task SendAddMemButtons(SocketInteractionContext context)
        {
            var channel = context.Client.GetChannel(context.Channel.Id) as IMessageChannel;
            var approveSplit = new ButtonBuilder()
            {
                Label = "Yes",
                CustomId = "add-members-modal",
                Style = ButtonStyle.Success
            };
            var denySplit = new ButtonBuilder()
            {
                Label = "No",
                CustomId = "no-add-modal",
                Style = ButtonStyle.Danger
            };
            var comp = new ComponentBuilder();
            comp.WithButton(approveSplit);
            comp.WithButton(denySplit);

            try
            {
                await channel.SendMessageAsync("Add members not captured above, or not present in party image?", isTTS: false,
                    embed: null, options: null, allowedMentions: null, messageReference: null,
                    components: comp.Build(), stickers: null, embeds: null, flags: MessageFlags.None);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public async Task BuildModalHandler(SocketInteractionContext context, Boolean bobbyBoole, List<string> membersList, List<string> imagesMembers)
        {

            //add silver bags text input
            var mb = new ModalBuilder()
            .WithTitle("Additional Split Information")
            .WithCustomId("split_info");
            if (bobbyBoole == true)
            {
                mb.AddTextInput("Please enter additional members not captured", "add_members", placeholder: "e.g. Nezcoupe, Ragejay, etc." +
            " (case sensitive)", required: true, value: null);
            };
            mb.AddTextInput("Please enter total loot amount",
            "loot_total", placeholder: "e.g. 42069 (no units or commas)", required: true)
            .AddTextInput("Please enter chest location", "chest_loc", placeholder: "e.g. 25, 26, 27, etc.", required: false);

            try
            {
                //send modal

                await context.Interaction.RespondWithModalAsync(mb.Build());

                context.Client.ModalSubmitted += async modal =>
                {
                    List<SocketMessageComponentData> components = modal.Data.Components.ToList();
                    //may need to put membersStr in some kind of conditional if they select "no" for add other members
                    string membersStr = components.First(x => x.CustomId == "add_members").Value;
                    string lootTotal = components.FirstOrDefault(x => x.CustomId == "loot_total").Value;
                    string chestLoc = components.FirstOrDefault(x => x.CustomId == "chest_loc").Value;

                    await modal.DeferAsync();

                    //split single string into member list
                    List<string> membersSplit = membersStr.Split(',').ToList();

                    //clean list of strings with space at the end
                    for (int i = 1; i < membersSplit.Count; i++)
                    {
                        //cleanup strings after list separation
                        if (i > 0)
                        {
                            membersSplit[i] = membersSplit[i].Remove(0, 1);
                        }
                        else
                        {
                            continue;
                        }
                    }

                    foreach (string member in membersSplit)
                    {
                        if (membersList.Contains(member))
                        {
                            if (!(imagesMembers.Contains(member)))
                            {
                                imagesMembers.Add(member);
                            }
                            else
                            {
                                continue;
                            }
                        }
                        else
                        {
                            await context.Channel.SendMessageAsync("***User " + member + " not found.***");
                        }
                    }

                    //New resulting List of members complete, begin embed builder two
                    //convert lootTotal to ulong, if an alpha char is present, send error
                    try
                    {
                        ulong lootTotalInt = (ulong)Int64.Parse(lootTotal);

                        //selection for which kind of split?
                        //is this the correct split amount?
                        decimal lootAmountPerMember = ((decimal)((lootTotalInt * .9) / imagesMembers.Count));

                        lootAmountPer = lootAmountPerMember;

                        var embed = new EmbedBuilder()
                        .WithTitle($"Loot split report generated by {context.User.Username}")
                        .WithColor(Discord.Color.Orange)
                        .AddField("Member Count", imagesMembers.Count)
                        .AddField(x =>
                        {
                            //loop results and add members
                            x.Name = "Members recorded";
                            for (int i = 0; i < (imagesMembers.Count - 1); i++)
                            {
                                x.Value += imagesMembers[i] + ", ";
                            }
                            x.Value += imagesMembers[imagesMembers.Count - 1];
                            x.IsInline = false;
                        })
                        .AddField("Loot Split Total", lootTotalInt)
                        .AddField("Loot Split Per", lootAmountPerMember)
                        .AddField("Chest Location(s)", chestLoc);

                        //send the embedded report
                        await context.Channel.SendMessageAsync("--Loot Split Report--", false, embed.Build());

                        await PostLootSplit(context);

                        await context.Channel.SendMessageAsync("***please post relevant chest loot images below " +
                            "for evaluation. Once regear team verifies/denies I’ll send you a message with the outcome. " +
                            "So long and thanks for all the fish.***");
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }
                    ////call LootSplitModule to verify/deny
                    //await PostLootSplit(context);
                };
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public async Task PostLootSplit(SocketInteractionContext context)
        {
            var channel = context.Client.GetChannel(context.Channel.Id) as IMessageChannel;
            var approveSplit = new ButtonBuilder()
            {
                Label = "Approve",
                CustomId = "approve split",
                Style = ButtonStyle.Success
            };
            var denySplit = new ButtonBuilder()
            {
                Label = "Deny",
                CustomId = "deny split",
                Style = ButtonStyle.Danger
            };
            var comp = new ComponentBuilder();
            comp.WithButton(approveSplit);
            comp.WithButton(denySplit);

            try
            {
                    await channel.SendMessageAsync(" ", isTTS: false, embed: null, options: null, allowedMentions: null, messageReference: null,
                    components: comp.Build(), stickers: null, embeds: null, flags: MessageFlags.None);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}