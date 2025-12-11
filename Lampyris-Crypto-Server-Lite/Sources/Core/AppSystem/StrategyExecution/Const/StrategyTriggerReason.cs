namespace Lampyris.Crypto.Server.Lite;

/// <summary>
/// 触发原因
/// </summary>
public enum StrategyTriggerReason
{
    FIXED_PERC_LONG = 1, // 涨幅 达到固定比例
    FIXED_PERC_SHORT = 2, // 跌幅 达到固定比例
    BLAST = 3, // 爆拉
    INTERVAL_LONG = 4, // 间隔时间新高
    INTERVAL_SHORT = 5, // 间隔时间新低
}
