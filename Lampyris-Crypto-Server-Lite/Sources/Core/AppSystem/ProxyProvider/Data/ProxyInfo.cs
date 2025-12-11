namespace Lampyris.Crypto.Server.Lite;

using Newtonsoft.Json;

/// <summary>
/// VPN代理信息
/// </summary>
public class ProxyInfo
{
    public string Address;
    public int    Port;

    /// <summary>
    /// 是否可用，通过连通性测试得到
    /// </summary>
    [JsonIgnore]
    public bool Valid = false;
}
