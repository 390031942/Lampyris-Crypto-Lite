namespace Lampyris.Crypto.Server.Lite;

using Lampyris.CSharp.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO.Compression;
using System.Net.Http.Headers;

[Component]
public class AnnouncementService:ILifecycle
{
    [Autowired]
    private SerializationManager m_SerializationManager;

    /// <summary>
    /// 请求参数
    /// </summary>
    private static readonly string URL = "https://www.binance.com/bapi/apex/v1/public/apex/cms/article/list/query?type=1&pageNo=1&pageSize={0}";

    /// <summary>
    /// 请求Header数据
    /// </summary>
    private AnnouncementHttpRequestHeader? m_Header;

    /// <summary>
    /// 用于请求数据的HttpClient，需要手动设置Header
    /// </summary>
    private HttpClient m_HttpClient = new HttpClient();

    /// <summary>
    /// 公告数据对象
    /// </summary>
    private AnnouncementServiceData m_Data = new AnnouncementServiceData();

    public override void OnStart()
    {
        // 加载公告数据
        m_Data = m_SerializationManager.TryDeserializeObjectFromFile<AnnouncementServiceData>() ?? new AnnouncementServiceData();

        // 初始化Http请求头部数据
        ReloadHeader();

        // 初始化公告请求 定时器
        Timer timer = new Timer(RequestAnnouncementData);
        // 先算到下一秒 0 毫秒，保证对齐整秒
        var now = DateTime.Now;
        var due = 1000 - now.Millisecond;          // 到下一秒
        timer = new Timer(RequestAnnouncementData, null, due, Timeout.Infinite);
    }

    /// <summary>
    /// 设置请求头
    /// </summary>
    public void ReloadHeader()
    {
        m_Header = IniConfigManager.Load<AnnouncementHttpRequestHeader>();
        m_HttpClient.DefaultRequestHeaders.Clear();
        m_HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        foreach (var property in typeof(AnnouncementHttpRequestHeader).GetProperties())
        {
            var value = property.GetValue(m_Header)?.ToString();
            if (!string.IsNullOrEmpty(value))
            {
                m_HttpClient.DefaultRequestHeaders.Add(property.Name.Replace("_", "-"), value);
            }
        }
    }

    /// <summary>
    /// 请求公告数据
    /// </summary>
    /// <param name="pageSize">每个栏目的请求条目</param>
    private async Task RequestAnnouncementData(int pageSize)
    {
        try
        {
            // 发起请求
            HttpResponseMessage response = await m_HttpClient.GetAsync(string.Format(URL, pageSize));
            response.EnsureSuccessStatusCode(); // 确保请求成功

            // 手动解压 Gzip 数据
            using (Stream responseStream = await response.Content.ReadAsStreamAsync())
            using (GZipStream decompressionStream = new GZipStream(responseStream, CompressionMode.Decompress))
            using (StreamReader reader = new StreamReader(decompressionStream))
            {
                string decompressedContent = await reader.ReadToEndAsync();
                JObject json = JObject.Parse(decompressedContent);
                ReadJson(json);
            }
        }
        catch (JsonReaderException ex)
        {
            Logger.LogError($"解析公告Json数据发生异常: {ex.Message}");
        }
        catch (Exception ex)
        {
            Logger.LogError($"请求公告数据发生异常: {ex.Message}");
        }
    }
    
    /// <summary>
    /// 读取公告Json
    /// </summary>
    private void ReadJson(JObject jObject)
    {
        JArray? catalogs = jObject?["data"]?["catalogs"]?.ToObject<JArray>();
        if(catalogs == null)
        {
            Logger.LogError($"解析公告Json数据发生错误: 未能找到节点\"data\".\"catalogs\"");
            return;
        }

        for(int i = 0; i < catalogs.Count; i++)
        {
            JObject? catalogNode = catalogs[i]?.ToObject<JObject>();
            if (catalogNode == null)
            {
                Logger.LogError($"解析公告Json数据发生错误: 节点\"data\".\"catalogs\"[{i}]为空");
                return;
            }

            int catalogId = Convert.ToInt32(catalogs["catalogId"]?.ToObject<string>() ?? "0");
            if (!m_Data.PerCatalogAnnouncementDataMap.ContainsKey(catalogId))
            {
                Logger.LogWarning($"解析公告Json数据发现未知的catalogId={catalogId}，" + 
                                   "请前往官网公告中心检查:https://www.binance.com/en/support/announcement/list/48");
                continue;
            }

            var catalogData = m_Data.PerCatalogAnnouncementDataMap[catalogId];
            int previousCount = catalogData.UpdatedIndex;
            int total = Convert.ToInt32(catalogs["total"]?.ToObject<string>() ?? "0"); 

            if(total > 0)
            {
                if (previousCount < total)
                {
                    // 做增量更新，取diff个就可以了
                    int diff = total - previousCount;
                    JArray? articles = catalogs["articles"]?.ToObject<JArray>();
                    if (articles == null)
                    {
                        Logger.LogError($"解析公告Json数据发生错误: 节点\"data\".\"catalogs\"[{i}].articles为空");
                        continue;
                    }
                    else
                    {
                        for (int j = 0; j < diff; j++)
                        {
                            JObject? articleNode = articles[i]?.ToObject<JObject>();
                            if (articleNode == null)
                            {
                                Logger.LogError($"解析公告Json数据发生错误: 节点\"data\".\"catalogs\"[{i}].articles[{j}]为空");
                                break;
                            }
                            RawAnnouncementData announcementData = new RawAnnouncementData()
                            {
                                Title = articleNode["title"]?.ToObject<string>() ?? "",
                                Content = "",
                                ReleaseDate = DateTimeUtil.FromUnixTimestamp(Convert.ToInt64(articleNode["releaseDate"]?.ToObject<string>() ?? "")),
                            };
                        }
                    }
                }
            }
            else
            {
                Logger.LogError($"解析公告Json数据发生错误: CatalogId={catalogId}类型的公告，服务器返回的公告数量total为0");
                continue;
            }
        }
    }

    /// <summary>
    /// 定时请求公告数据
    /// </summary>
    /// <param name="state"></param>
    private void RequestAnnouncementData(object? state)
    {
       
    }
}
