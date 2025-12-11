namespace Lampyris.Crypto.Server.Lite;

/// <summary>
/// VPN代理服务信息
/// </summary>
public class ProxyProviderServiceData
{
    /// <summary>
    /// 最小的VPN代理 要求数量，小于这个程序将无法启动
    /// </summary>
    public int MinimumRequirement = 1;

    /// <summary>
    /// 代理信息
    /// </summary>
    public List<ProxyInfo> ProxyInfos = new List<ProxyInfo>();

    /// <summary>
    /// 是否使用代理，在程序启动时决定
    /// </summary>
    public bool UseProxy = true;
}
