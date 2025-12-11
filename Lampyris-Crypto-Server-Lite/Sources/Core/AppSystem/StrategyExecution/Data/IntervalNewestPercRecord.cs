namespace Lampyris.Crypto.Server.Lite;

/// <summary>
/// 间隔时间新高/新低
/// </summary>
public class IntervalNewestPercRecord
{
    /// <summary>
    /// USDT永续合约
    /// </summary>
    public string Symbol;

    /// <summary>
    /// 最高价触发时间(精确到分钟)
    /// </summary>
    public long HighPercTimestamp;

    /// <summary>
    /// 最高涨幅(%)
    /// </summary>
    public long HighPercp;

    /// <summary>
    /// 最低价触发时间(精确到分钟)
    /// </summary>
    public long LowPercTimestamp;

    /// <summary>
    /// 最低涨幅(%)
    /// </summary>
    public long LowPerc;
}
