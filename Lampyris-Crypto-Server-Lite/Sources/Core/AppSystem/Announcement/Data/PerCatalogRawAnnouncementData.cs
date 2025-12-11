namespace Lampyris.Crypto.Server.Lite;

/// <summary>
/// 每个目录下的公告数据
/// </summary>
public class PerCatalogRawAnnouncementData
{
    /// <summary>
    /// 目录ID
    /// </summary>
    public int Id;

    /// <summary>
    /// 目录的名称，如"数字货币及交易对上新"
    /// </summary>
    public string Name;

    /// <summary>
    /// 所有已经缓存的公告数据列表
    /// </summary>
    public List<RawAnnouncementData> AnnouncementDataList = new List<RawAnnouncementData>();

    /// <summary>
    /// 最近一次请求公告时，服务端返回的 这个目录包含的公告总数（并非本地缓存的公告总数）
    /// 通过这个字段 可以判断有没有新的公告
    /// </summary>
    public int UpdatedIndex;
}
