/*
 * 这是一个简单的资源文件版本控制工具，其主要功能如下：
 * 
 * 首先，对于INI配置文件中 指定的资源文件夹路径，
 * 计算文件夹中 每一个文件的MD5（需要递归子文件夹），
 * 并和json里 原先的MA5值 的进行对比，得到 MD5发生改变的 文件列表。
 * 若存在任意一个文件发生改变，需要版本号递增1，版本号格式：日期.当日版本号(从1开始)， 
 * 最后把变更的文件 上传到服务器（不需要全量上传）。
 */

namespace Lampyris.ResourceVersionControlTool;

using System.Collections.Generic;

// 定义 version.json 的结构
public class VersionInfo
{
    public string Version { get; set; }
    public Dictionary<string, string> Md5 { get; set; }
}