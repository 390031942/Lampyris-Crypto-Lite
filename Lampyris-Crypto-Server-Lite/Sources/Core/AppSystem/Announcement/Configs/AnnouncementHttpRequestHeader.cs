using Lampyris.CSharp.Common;

[IniFile("announcement_http_request_header.ini")]
public class AnnouncementHttpRequestHeader
{
    [IniField("General", "accept", "指定客户端可以接收的响应内容类型")]
    public string Accept { get; set; }

    [IniField("General", "accept-language", "指定客户端的语言偏好")]
    public string AcceptLanguage { get; set; }

    [IniField("Binance", "bnc-level", "Binance 内部标识，用于区分用户级别")]
    public string BncLevel { get; set; }

    [IniField("Binance", "bnc-location", "Binance 内部标识，表示用户所在地区")]
    public string BncLocation { get; set; }

    [IniField("Binance", "bnc-time-zone", "Binance 内部标识，表示用户的时区")]
    public string BncTimeZone { get; set; }

    [IniField("Binance", "bnc-uuid", "客户端唯一标识符，用于追踪用户请求")]
    public string BncUuid { get; set; }

    [IniField("General", "clienttype", "指定客户端类型")]
    public string ClientType { get; set; }

    [IniField("General", "content-type", "指定请求的内容类型")]
    public string ContentType { get; set; }

    [IniField("Security", "csrftoken", "CSRF 防护令牌，用于防止跨站请求伪造攻击")]
    public string CsrfToken { get; set; }

    [IniField("Device", "device-info", "包含设备信息的 JSON 字符串，用于标识客户端设备")]
    public string DeviceInfo { get; set; }

    [IniField("Binance", "fvideo-id", "Binance 内部标识，可能用于视频追踪")]
    public string FvideoId { get; set; }

    [IniField("Binance", "fvideo-token", "Binance 内部标识，可能用于视频验证")]
    public string FvideoToken { get; set; }

    [IniField("General", "if-none-match", "HTTP 缓存控制字段，用于验证资源是否被修改")]
    public string IfNoneMatch { get; set; }

    [IniField("General", "lang", "指定客户端的语言")]
    public string Lang { get; set; }

    [IniField("Security", "sec-ch-ua", "客户端的 User-Agent 信息，用于标识浏览器和版本")]
    public string SecChUa { get; set; }

    [IniField("Security", "sec-ch-ua-mobile", "指定客户端是否为移动设备")]
    public string SecChUaMobile { get; set; }

    [IniField("Security", "sec-ch-ua-platform", "指定客户端的操作系统平台")]
    public string SecChUaPlatform { get; set; }

    [IniField("Security", "sec-fetch-dest", "指定请求的目标类型")]
    public string SecFetchDest { get; set; }

    [IniField("Security", "sec-fetch-mode", "指定请求的模式")]
    public string SecFetchMode { get; set; }

    [IniField("Security", "sec-fetch-site", "指定请求的来源")]
    public string SecFetchSite { get; set; }

    [IniField("Binance", "x-host", "Binance 内部标识，指定请求的主机")]
    public string XHost { get; set; }

    [IniField("Binance", "x-passthrough-token", "Binance 内部标识，可能用于透传令牌")]
    public string XPassthroughToken { get; set; }

    [IniField("Binance", "x-trace-id", "请求追踪 ID，用于标识和追踪请求链路")]
    public string XTraceId { get; set; }

    [IniField("Binance", "x-ui-request-trace", "请求追踪 ID，与 x-trace-id 类似")]
    public string XUiRequestTrace { get; set; }
}