namespace Lampyris.Crypto.Server.Lite;

using Lampyris.CSharp.Common;

[IniFile("ai_chat_config.ini")]
public class AIChatServiceConfig
{
    [IniField("General","api_key","Deepseek的APIKey密钥")]
    public string APIKey;

    [IniField("General", "api_url", "Deepseek的API请求地址")]
    public string APIUrl;

    [IniField("General", "model", "Deepseek的模型名称")]
    public string Model;

    [IniField("General", "role", "Deepseek的请求的角色")]
    public string Role;
}
