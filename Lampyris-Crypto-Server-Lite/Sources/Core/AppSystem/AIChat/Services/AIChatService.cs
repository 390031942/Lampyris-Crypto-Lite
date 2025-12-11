namespace Lampyris.Crypto.Server.Lite;

using Lampyris.CSharp.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Threading.Tasks;

[Component]
public class AIChatService:ILifecycle
{
    /// <summary>
    /// 配置数据
    /// </summary>
    private AIChatServiceConfig m_Config;

    /// <summary>
    /// 用于请求的HttpClient
    /// </summary>
    private HttpClient m_HttpClient = new HttpClient();

    public override void OnStart()
    {
        m_Config = IniConfigManager.Load<AIChatServiceConfig>();
        if(m_Config == null)
        {
            m_Config = new AIChatServiceConfig()
            {
                APIKey = "sk-27f2acc523eb40cab9728281dad6dc1d",
                APIUrl = "https://api.deepseek.com/v1/chat/completions",
                Model = "deepseek-chat",
                Role = "user",
            };

            IniConfigManager.Save(m_Config);
        }

        // 初始化Header信息
        m_HttpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {m_Config.APIKey}");
    } 

    public async Task<JObject?> GetChatResult(string content)
    {
        var requestBody = new
        {
            model = m_Config.Model,
            messages = new[]
            {
                new { role = "user", content = content }
            }
        };

        var json = Newtonsoft.Json.JsonConvert.SerializeObject(requestBody);
        var response = await m_HttpClient.PostAsync(m_Config.APIUrl, 
                                                    new StringContent(json, Encoding.UTF8, "application/json"));
        var responseString = await response.Content.ReadAsStringAsync();

        try
        {
            JObject jObject = JObject.Parse(responseString);
            return jObject;
        }
        catch (JsonReaderException ex)
        {
            Logger.LogError($"AI聊天返回结果 解析为Json失败，请检查输入内容(需要提示输出为json):{ex.Message}");
        }

        return null;
    }
}
