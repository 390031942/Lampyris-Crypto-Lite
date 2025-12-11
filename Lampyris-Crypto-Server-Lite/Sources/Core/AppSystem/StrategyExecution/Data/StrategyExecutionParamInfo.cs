namespace Lampyris.Crypto.Server.Lite;

/// <summary>
/// 执行参数(可以)
/// </summary>
public class StrategyExecutionParamInfo
{
    /// <summary>
    /// 触发止损(%)
    /// </summary>
    public double StopLossPerc = 7;

    /// <summary>
    /// 无人值守止盈(%)
    /// </summary>
    public double UnattendedMakeProfitPerc = 15;

    /// <summary>
    /// 最小容忍时间（分钟）
    /// </summary>
    public double MinHoldingMinutes = 3;
}
