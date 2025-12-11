using Lampyris.CSharp.Common;
using Newtonsoft.Json;
using System.Net.Http.Json;
using System.Net.Sockets;

namespace Lampyris.Crypto.Server.Lite;

[Component]
public class ProxyProviderService:ILifecycle
{
    /// <summary>
    /// 代理服务数据，需要手动加载
    /// </summary>
    private ProxyProviderServiceData m_Data;

    /// <summary>
    /// 代理配置数据所在路径
    /// </summary>
    private const string ProxyInfoObjectSaveFilePath = "cfgs/proxy.json";

    public override int Priority => 1;

    /// <summary>
    /// 创建默认代理配置
    /// </summary>
    private void CreateDefaultConfig()
    {
        Logger.LogInfo($"文件\"{ProxyInfoObjectSaveFilePath}\"不存在，将创建默认的代理信息");
        m_Data = new ProxyProviderServiceData()
        {
            MinimumRequirement = 1,
            ProxyInfos = new List<ProxyInfo>()
            {
                new ProxyInfo()
                {
                    Address = "127.0.0.1",
                    Port = 7890,
                    Valid = false,
                },
                new ProxyInfo()
                {
                    Address = "127.0.0.1",
                    Port = 10808,
                    Valid = false,
                },
            },
            UseProxy = true,
        };

        File.WriteAllText(ProxyInfoObjectSaveFilePath, JsonConvert.SerializeObject(m_Data));
    }

    public override void OnStart()
    {
        Logger.LogInfo($"从路径\"{ProxyInfoObjectSaveFilePath}\"加载代理信息, 然后进行代理连通性测试...");
        if (File.Exists(ProxyInfoObjectSaveFilePath))
        {
            string jsonContent = File.ReadAllText(ProxyInfoObjectSaveFilePath);
            var data = JsonConvert.DeserializeObject<ProxyProviderServiceData>(jsonContent);
            if(data == null)
            {
                CreateDefaultConfig();
            }
            else
            {
                m_Data = data;
            }
        }
        else
        {
            CreateDefaultConfig();
        }

        // 如果要使用代理
        if (m_Data.UseProxy)
        {
            if (m_Data.ProxyInfos.Count < m_Data.MinimumRequirement)
            {
                throw new InvalidDataException($"提供的代理数量(现为{m_Data.ProxyInfos.Count}个)，不满足程序启动的最低要求({m_Data.MinimumRequirement}个)");
            }

            // 连通性测试
            int reachableCount = 0;
            foreach (ProxyInfo proxyInfo in m_Data.ProxyInfos)
            {
                if (TestReachable(proxyInfo))
                {
                    reachableCount++;
                }
            }

            if (reachableCount < m_Data.MinimumRequirement)
            {
                throw new InvalidDataException($"提供的代理信息中，连通测试通过的数量(现为{reachableCount}个)，不满足程序启动的最低要求({m_Data.MinimumRequirement}个)");
            }
        }
    }

    private static bool TestReachable(ProxyInfo proxyInfo)
    {
        bool result = false;
        // 代理服务器地址和端口
        // 使用 TcpClient 测试端口连通性
        using (var tcpClient = new TcpClient())
        {
            try
            {
                tcpClient.Connect(proxyInfo.Address, proxyInfo.Port);
                result = true;
            }
            catch { }
        }

        Logger.LogInfo($"连通性测试结果 \"{proxyInfo.Address}:{proxyInfo.Port}\": {(result ? "Reachable": "UnReachable")}");
        return result;
    }

    public int ProxyCount
    {
        get
        {
            return m_Data?.ProxyInfos.Count ?? 0;
        }
    }

    public ProxyInfo? Get(int index)
    {
        return (m_Data != null && index >= 0 && index < ProxyCount) ? m_Data.ProxyInfos[index] : null;
    }
}
