using DiscordBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DiscordBot.Enums;

namespace DiscordBot.Services
{
    public class DataBaseService
    {
        private FreeBeerdbContext freeBeerdbContext = new FreeBeerdbContext();

        public async Task AddPlayerInfo(Player player)
        {
            if (!await CheckPlayerIsExist(player.PlayerName))
            {
                await freeBeerdbContext.Player.AddAsync(player);
                await freeBeerdbContext.SaveChangesAsync();
            }
        }
        public async Task<Boolean> CheckPlayerIsExist(string playerName)
        {
            return await freeBeerdbContext.Player.AnyAsync(x => x.PlayerName == playerName);
        }
        public Player GetPlayerInfoByName(string playerName)
        {
            return freeBeerdbContext.Player.AsQueryable().Where(x => x.PlayerName == playerName).FirstOrDefault();
        }
        public MoneyType GetMoneyTypeByName(MoneyTypes moneyType)
        {
            return freeBeerdbContext.MoneyType.AsQueryable().Where(x => x.Type == moneyType).FirstOrDefault();
        }
        public async Task AddPlayerReGear(PlayerLoot playerLoot)
        {
            await freeBeerdbContext.PlayerLoot.AddAsync(playerLoot);
            await freeBeerdbContext.SaveChangesAsync();
        }
        public async Task AddSeedingData()
        {
            if (await freeBeerdbContext.MoneyType.AnyAsync(x => x.Type == MoneyTypes.FocusSale))
            {
                await freeBeerdbContext.MoneyType.AddAsync(new MoneyType
                {
                    Type = MoneyTypes.FocusSale
                });
            }
            if (await freeBeerdbContext.MoneyType.AnyAsync(x => x.Type == Enums.MoneyTypes.Hellgates))
            {
                await freeBeerdbContext.MoneyType.AddAsync(new MoneyType
                {
                    Type = MoneyTypes.Hellgates
                });
            }
            if (await freeBeerdbContext.MoneyType.AnyAsync(x => x.Type == Enums.MoneyTypes.LootSplit))
            {
                await freeBeerdbContext.MoneyType.AddAsync(new MoneyType
                {
                    Type = MoneyTypes.LootSplit
                });
            }
            if (await freeBeerdbContext.MoneyType.AnyAsync(x => x.Type == Enums.MoneyTypes.OCBreak))
            {
                await freeBeerdbContext.MoneyType.AddAsync(new MoneyType
                {
                    Type = MoneyTypes.OCBreak
                });
            }
            if (await freeBeerdbContext.MoneyType.AnyAsync(x => x.Type == Enums.MoneyTypes.Other))
            {
                await freeBeerdbContext.MoneyType.AddAsync(new MoneyType
                {
                    Type = MoneyTypes.Other
                });
            }
            if (await freeBeerdbContext.MoneyType.AnyAsync(x => x.Type == Enums.MoneyTypes.ReGear))
            {
                await freeBeerdbContext.MoneyType.AddAsync(new MoneyType
                {
                    Type = MoneyTypes.ReGear
                });
            }
            await freeBeerdbContext.SaveChangesAsync();
        }
    }
}
