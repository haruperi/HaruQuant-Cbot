using System;

namespace cAlgo.Robots.Utils
{
    public static class BotConfig
    {
        public const string BotName = "HaruQuant CoreBot";
        public const string BotVersion = "1.0.0";
        
        public static readonly string[] ForexSymbols = new string[]
        {
            "AUDCAD", "AUDCHF", "AUDJPY", "AUDNZD", "AUDUSD",
            "CADCHF", "CADJPY", "CHFJPY",
            "EURAUD", "EURCAD", "EURCHF", "EURGBP", "EURJPY", "EURNZD", "EURUSD",
            "GBPAUD", "GBPCAD", "GBPCHF", "GBPJPY", "GBPNZD", "GBPUSD",
            "NZDCAD", "NZDCHF", "NZDJPY", "NZDUSD",
            "USDCHF", "USDCAD", "USDJPY"
        };

        public static readonly string[] CommoditySymbols = new string[]
        {
            "XAUUSD", "XAUEUR", "XAUGBP", "XAUJPY", "XAUAUD", "XAUCHF", "XAGUSD"
        };

        public static readonly string[] IndexSymbols = new string[]
        {
            "US500", "US30", "UK100", "GER40", "NAS100", "USDX", "EURX"
        };
    }

    public enum SymbolsToTrade
    {
        Forex = 0,
        Commodities = 1,
        Indices = 2,
        Custom = 3,
        All = 4
    }

    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3
    }

    public enum HourOfDay
    {
        H00 = 00,     // 00:00
        H01 = 01,     // 01:00
        H02 = 02,     // 02:00
        H03 = 03,     // 03:00
        H04 = 04,     // 04:00
        H05 = 05,     // 05:00
        H06 = 06,     // 06:00
        H07 = 07,     // 07:00
        H08 = 08,     // 08:00
        H09 = 09,     // 09:00
        H10 = 10,     // 10:00
        H11 = 11,     // 11:00
        H12 = 12,     // 12:00
        H13 = 13,     // 13:00
        H14 = 14,     // 14:00
        H15 = 15,     // 15:00
        H16 = 16,     // 16:00
        H17 = 17,     // 17:00
        H18 = 18,     // 18:00
        H19 = 19,     // 19:00
        H20 = 20,     // 20:00
        H21 = 21,     // 21:00
        H22 = 22,     // 22:00
        H23 = 23      // 23:00
    }

    public enum TradingDirection
    {
        Both = 0,       // Allow Both Buy and Sell
        Buy = 1,        // Allow Buy Only
        Sell = -1       // Allow Sell Only
    }

    public enum StopLossMode
    {
        Fixed = 0,          // Fixed Stop Loss
        None = 2,            // No Stop Loss
        UseATR = 3,          // Use ATR for Stop Loss
        UseADR = 4          // Use ADR for Stop Loss
    }

    public enum TakeProfitMode
    {
        Fixed = 0,          // Fixed Take Profit
        None = 1,            // No Take Profit
        UseATR = 2,          // Use ATR for Take Profit
        UseADR = 3          // Use ADR for Take Profit
    }

    public enum RiskBase
    {
        Equity = 1,         // Equity
        Balance = 2,        // Balance
        FreeMargin = 3,     // Free Margin
        FixedBalance = 4    // Fixed Balance
    }

    public enum RiskDefaultSize
    {
        FixedLots = 1,       // Fixed Size
        Auto = 2,            // Automatic Size Based on Risk
        FixedAmount = 3,      // Fixed Money
        FixedLotsStep = 4,    // Fixed Lots Step
    }

    public enum TradingMode
    {
        Auto,
        Manual,
        Both
    }

    public enum SignalEntry
    {
        Neutral = 0,    // Signal Entry Neutral
        Buy = 1,        // Signal Entry Buy
        Sell = -1       // Signal Entry Sell
    }

    public enum SignalExit
    {
        Neutral = 0,    // Signal Exit Neutral
        Buy = 1,        // Signal Exit Buy
        Sell = -1,      // Signal Exit Sell
        All = 2         // Signal Exit All
    }

    public enum Strategy
    {
        None = 0,
        TrendFollowing = 1,
        MeanReversion = 2,
        Breakout = 3,
        Swingline = 4,
        Scalping = 5,
        SwingTrendlineMTF = 6
    }

    public enum ManageTrade
    {
        None = 0,   
        Decomposition = 1,
        CostAverage = 2,
        Martingale = 3,
        Grid = 4
    }
}
