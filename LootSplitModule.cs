using System;
using System.Threading.Tasks;
using Discord;
using DiscordBot.Services;
using Discord.Interactions;

namespace DiscordBot.LootSplitModule
{
    public class LootSplitModule
    {
        public async Task PostLootSplit(SocketInteractionContext command)
        {
            //FOR RELEASE ADD LOOT SPLIT CHANNEL ID TO APPSETTINGS AND 
            //ulong id = ulong.Parse(System.Configuration.ConfigurationManager.AppSettings.Get("lootSplitChannelId"));

            //FOR RELEASE CHANGE THIS CHANNEL TO THE LOOT SPLIT CHANNEL
            var channel = command.Client.GetChannel(602243828266696720) as IMessageChannel;
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
                await channel.SendMessageAsync("--Approve or Deny--", isTTS: false, embed: null, options: null, allowedMentions: null, messageReference: null,
                    components: comp.Build(), stickers: null, embeds: null, flags: MessageFlags.None);
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}