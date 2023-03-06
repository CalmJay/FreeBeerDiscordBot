using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Enums
{
    public enum EventTypeEnum
    {
        Castle,
        Chest,
        Ganking,
        Hellgates,
        Hideout,
        Mages,
        Orb,
        Other,
        SpecialEvent,
        Territory,
        Vortex,
        DrinkingBeer
    }

    public enum AlbionAPIDataTypesEnum
    {
        search,
        playerSearch,
        playerDeaths,
        playerKills,
        playerStatistics,
        events
    }

    public enum AlbionCitiesEnum
    {
        Thetford,
        FortSterling,
        Lymhurst,
        Bridgewatch,
        Martlock,
        Caerleon
    }

    public enum MarketEnum
    {
        buy,
        sell,
        prices,
        history
    }

    public enum MoneyTypes
    {
        FocusSale,
        Hellgates,
        LootSplit,
        OCBreak,
        Other,
        ReGear
    }

    public enum GearLocation
    {
        Head,
        Armor,
        Chest,
        Shoes,
        Cape,
        MainHand,
        OffHand,
        Bag,
        Mount,
        Potion,
        Food,
        Inventory
    }

    public enum ClassType
    {
        Tank,
        DPS,
        Support,
        Healer,
        Unknown
    }

    public enum MiniMarketType
    {
        Purchase,
        Deposit,
        Withdrawal,
        CreditsTransfer,
        AccountSetup,
        Other
    }
}
