namespace Lampyris.Crypto.Server.Lite;
using System;

/// <summary>
/// 每条公告的数据(Raw=未解析过的)
/// </summary>
public class RawAnnouncementData
{
    /// <summary>
    /// 公告标题
    /// </summary>
    public string Title;

    /// <summary>
    /// 公告内容
    /// </summary>
    public string Content;

    /// <summary>
    /// 公告发布的日期
    /// </summary>
    public DateTime ReleaseDate;

    /// <summary>
    /// 公告代码，拼接在URL最后：https://www.binance.com/zh-CN/support/announcement/detail/ 即可用浏览器访问内容
    /// </summary>
    public string Code;
}
