namespace Lampyris.Crypto.Server.Lite;

public class LeverageSettingResult
{
    /// <summary>
    /// 交易对
    /// </summary>
    public string Symbol;

    /// <summary>
    /// 杠杆设置错误信息
    /// </summary>
    public string ErrorMessage;

    /// <summary>
    /// 设置后的杠杆倍数
    /// </summary>
    public int Leverage;

    /// <summary>
    /// 设置后的最大可开仓名义价值
    /// </summary>
    public double MaxNotional;
}