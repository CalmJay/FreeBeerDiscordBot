using System;
using System.Collections.Generic;
using System.Text;

namespace DiscordBot.Enums
{
    //public enum Enumerations
    //{
    //    ReGear,
    //    LootSplit,
    //    Other,
    //    OCBreak,
    //    FocusSale,
    //    Hellgates,
    //}

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
}
