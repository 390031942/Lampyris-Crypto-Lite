namespace Lampyris.Crypto.Server.Lite;

/// <summary>
/// 公告 经过解析所得到的 Symbol信息
/// </summary>
public class AnnouncementSymbolInfo
{
    /// <summary>
    /// 与公告相关的Symbol
    /// </summary>
    public string Symbol;

    /// <summary>
    /// 与每个Symbol有关的时间戳（可以表示上架时间/下架时间等）
    /// </summary>
    public long RelevantTimestamp = -1;
}
