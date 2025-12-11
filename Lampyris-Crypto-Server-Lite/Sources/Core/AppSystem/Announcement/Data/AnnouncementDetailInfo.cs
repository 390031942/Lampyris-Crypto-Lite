namespace Lampyris.Crypto.Server.Lite;

/// <summary>
/// 公告 经过解析所得到的信息
/// </summary>
public class AnnouncementDetailInfo
{
    /// <summary>
    /// 与公告相关的Symbol信息
    /// </summary>
    public List<AnnouncementSymbolInfo> RelevantSymbols = new List<AnnouncementSymbolInfo>();

    /// <summary>
    /// 公告具体类型
    /// </summary>
    public AnnouncementDetailType Type;

    /// <summary>
    /// 生效时间戳（可以表示保证金梯度调整，价格精度调整等的通用生效时刻，非针对某一个Symbol的）
    /// </summary>
    public long EffectiveTimestamp = -1;
}
