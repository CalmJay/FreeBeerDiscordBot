using DiscordBot.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            }
        }
        public async Task<Boolean> CheckPlayerIsExist(string playerName)
        {
            try
            {
                return await freeBeerdbContext.Player.AnyAsync(x => x.PlayerName == playerName);

            }
            catch (Exception ex)
            {

                throw;
            }
        }
        public Player GetPlayerInfoByName(string playerName)
        {
            return freeBeerdbContext.Player.AsQueryable().Where(x=>x.PlayerName==playerName).FirstOrDefault();
        }
        public async Task AddPlayerReGear(PlayerLoot playerLoot)
        {
            await freeBeerdbContext.PlayerLoot.AddAsync(playerLoot);
        }
        public async Task AddSeedingData()
        {
            if (await freeBeerdbContext.MoneyType.AnyAsync(x => x.Type == Enums.MoneyTypes.FocusSale))
            {
                await freeBeerdbContext.MoneyType.AddAsync(new MoneyType { 
                Type= Enums.MoneyTypes.FocusSale
                });
            }
            if (await freeBeerdbContext.MoneyType.AnyAsync(x => x.Type == Enums.MoneyTypes.Hellgates))
            {
                await freeBeerdbContext.MoneyType.AddAsync(new MoneyType
                {
                    Type = Enums.MoneyTypes.Hellgates
                });
            }
            if (await freeBeerdbContext.MoneyType.AnyAsync(x => x.Type == Enums.MoneyTypes.LootSplit))
            {
                await freeBeerdbContext.MoneyType.AddAsync(new MoneyType
                {
                    Type = Enums.MoneyTypes.LootSplit
                });
            }
            if (await freeBeerdbContext.MoneyType.AnyAsync(x => x.Type == Enums.MoneyTypes.OCBreak))
            {
                await freeBeerdbContext.MoneyType.AddAsync(new MoneyType
                {
                    Type = Enums.MoneyTypes.OCBreak
                });
            }
            if (await freeBeerdbContext.MoneyType.AnyAsync(x => x.Type == Enums.MoneyTypes.Other))
            {
                await freeBeerdbContext.MoneyType.AddAsync(new MoneyType
                {
                    Type = Enums.MoneyTypes.Other
                });
            }
            if (await freeBeerdbContext.MoneyType.AnyAsync(x => x.Type == Enums.MoneyTypes.ReGear))
            {
                await freeBeerdbContext.MoneyType.AddAsync(new MoneyType
                {
                    Type = Enums.MoneyTypes.ReGear
                });
            }
        }
    }
}
