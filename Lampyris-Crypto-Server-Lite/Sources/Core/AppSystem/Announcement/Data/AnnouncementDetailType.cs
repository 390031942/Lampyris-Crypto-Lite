namespace Lampyris.Crypto.Server.Lite;

/// <summary>
/// 关心的公告具体类型(经过 AI聊天 解析过的)
/// </summary>
public enum AnnouncementDetailType
{
    UNKNOWN = 0,
    DELIST = 1,// 下架
    LAUNCH = 2,// 上架
    UPDATE_EXCHANGE_INFO = 3, // 价格精度调整
    UPDATE_LEVERAGE_BRACKET = 4, // 保证金梯度调整
}
