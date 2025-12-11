namespace Lampyris.Crypto.Server.Lite;

/// <summary>
/// 每天执行的信息，包括每一个symbol对应的执行动作，满足执行条件的symbol集合（不一定执行，因为可能没有槽位）
/// </summary>
public class PerDayExecutionInfo
{
    /// <summary>
    /// Symbol->执行信息
    /// </summary>
    public Dictionary<string, PerSymbolExecutionInfo> PerSymbolExecutionMap = new();

    /// <summary>
    /// StrategyTriggerReason->触发过的Symbol->触发的时间
    /// </summary>
    public Dictionary<StrategyTriggerReason, Dictionary<string, DateTime>> TriggeredSymbolDateTimeMap = new();
}
