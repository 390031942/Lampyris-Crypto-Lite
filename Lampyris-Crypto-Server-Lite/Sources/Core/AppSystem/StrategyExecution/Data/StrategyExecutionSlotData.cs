using Binance.Net.Enums;

namespace Lampyris.Crypto.Server.Lite;

/// <summary>
/// 涨幅/跌幅 异动持仓槽位
/// </summary>
public class StrategyExecutionSlotData
{
    /// <summary>
    /// USDT永续合约
    /// </summary>
    public string Symbol;

    /// <summary>
    /// 持仓方向
    /// </summary>
    public PositionSide Side;

    /// <summary>
    /// 仓位触发时间
    /// </summary>
    public long TriggeredTimestamp;

    /// <summary>
    /// 槽位优先级，从0开始递增，优先级0表示龙一/熊一
    /// </summary>
    public int Priority;

    /// <summary>
    /// 是否锁仓
    /// </summary>
    public bool IsLocked = true;

    /// <summary>
    /// 是否槽位被借用，如槽位类型是SHORT，但是被LONG借用了
    /// </summary>
    public bool IsBorrowed = false;

    public bool IsExecuting()
    {
        return !string.IsNullOrEmpty(Symbol);
    }

    public void Clear()
    {
        Symbol = "";
        TriggeredTimestamp = 0;
        Priority = 0;
        IsLocked = false;
    }
}
