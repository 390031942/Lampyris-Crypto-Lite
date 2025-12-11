namespace Lampyris.Crypto.Server.Lite;

public static class StopLossTypeMssk
{
    /// <summary>
    /// 固定到某个比例止损，对于多头仓位是小于，对于空头仓位是取相反数后的小于
    /// </summary>
    public static readonly int FIXED_PERC_STOP = 1;

    /// <summary>
    /// 移动止损
    /// </summary>
    public static readonly int MOVE_STOP = 2;

    /// <summary>
    /// 无人值守止损
    /// </summary>
    public static readonly int UNATTENDED_STOP = 4;

    /// <summary>
    /// 破均线止损
    /// </summary>
    public static readonly int MA_COMPARE_STOP = 8;
}
