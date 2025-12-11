namespace Lampyris.Crypto.Server.Lite;

public enum StrategyExecutionSlotState
{
    NONE = 0, // 无状态，说明没有持有合约
    HOLDING = 1, // 持有
    SLOW = 2, // 持有慢合约，指的是持有了一段时间但是没有获利
    NORMAL = 3, // 中规中矩，持有并获利但是不多
    POTENTITL = 4, // 有暴涨/暴跌潜力
    EXCELLENT = 5, // 暴涨/暴跌的 龙一/熊一
    BLAST = 6, // 大爆发的,一秒拉升超过14.8%
}
