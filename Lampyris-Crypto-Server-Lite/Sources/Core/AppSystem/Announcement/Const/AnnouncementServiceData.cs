namespace Lampyris.Crypto.Server.Lite;

/// <summary>
/// 公告数据对象（需要序列化保存）
/// </summary>
public class AnnouncementServiceData
{
    /// <summary>
    /// 保存了每个目录ID(由Binance定义) 对应的 原始公告信息
    /// </summary>
    public Dictionary<int, PerCatalogRawAnnouncementData> PerCatalogAnnouncementDataMap = new ()
    {
        { AnnouncementCatalogId.NEW_LISTING, new()},
        { AnnouncementCatalogId.LASEST_BINANCE_NEWS, new()},
        { AnnouncementCatalogId.LASEST_ACTIVITIES, new()},
        { AnnouncementCatalogId.NEW_FLAT_LISTINGS, new()},
        { AnnouncementCatalogId.DELISTING, new()},
        { AnnouncementCatalogId.MAINTENANCE_UPDATES, new()},
        { AnnouncementCatalogId.API_UPDATES, new()},
        { AnnouncementCatalogId.AIRDROP, new()},
    };

    /// <summary>
    /// 保存了每个公告类型(自己定义所关心的)所对应的，经过解析之后的公告信息
    /// </summary>
    public Dictionary<AnnouncementDetailType, List<AnnouncementDetailInfo>> PerDetailTypeAnnouncementInfoListMap = new ()
    {
        { AnnouncementDetailType.DELIST, new()},
        { AnnouncementDetailType.LAUNCH, new()},
        { AnnouncementDetailType.UPDATE_EXCHANGE_INFO, new()},
        { AnnouncementDetailType.UPDATE_LEVERAGE_BRACKET, new()},
    };
}
