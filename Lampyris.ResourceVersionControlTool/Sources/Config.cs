namespace Lampyris.ResourceVersionControlTool;
using Lampyris.CSharp.Common;

[IniFile("config.ini")]
public class Config
{
    [IniField("Paths", "ResourceFolder", "资源文件夹路径")]
    public string ResourceFolder { get; set; }

    [IniField("HTTP", "RemoteUrl", "资源服务器URL")]
    public string RemoteUrl { get; set; }
}