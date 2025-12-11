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

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using Lampyris.CSharp.Common;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;

public static class Program
{
    private static async Task Main(string[] args)
    {
        try
        {
            // 加载配置
            Config config = IniConfigManager.Load<Config>();

            // 平台，每个平台对应不同的资源路径（取值应该为:Standalone/Android/IOS)
            string platform = args.Length > 1 ? args[1] : "Standalone";

            string resourceFolderPath = config.ResourceFolder;
            string uploadUrl = config.RemoteUrl;
            string jsonFilePath = Path.Combine(resourceFolderPath, "version.json");

            // 检查资源文件夹是否存在
            if (!Directory.Exists(resourceFolderPath))
            {
                Console.WriteLine($"资源文件夹不存在：{resourceFolderPath}");
                return;
            }

            // 加载或初始化 version.json 文件
            VersionInfo versionInfo = LoadOrInitializeVersionJson(jsonFilePath);

            // 计算文件夹中所有文件的 MD5 哈希值
            var newFileHashes = CalculateFolderHashes(resourceFolderPath);

            // 获取发生变化的文件列表
            var changedFiles = GetChangedFiles(versionInfo.Md5, newFileHashes);

            if (changedFiles.Count > 0)
            {
                // 更新版本号
                versionInfo.Version = UpdateVersion(versionInfo.Version);

                // 更新 MD5 信息
                foreach (var file in changedFiles)
                {
                    if (newFileHashes.ContainsKey(file))
                    {
                        versionInfo.Md5[file] = newFileHashes[file];
                    }
                    else
                    {
                        versionInfo.Md5.Remove(file); // 文件被删除
                    }
                }

                // 保存更新后的 version.json 文件
                SaveVersionJson(jsonFilePath, versionInfo);

                // 上传发生变化的文件到 HTTP 服务器(一定要添加上version.json)
                changedFiles.Add("version.json");
                await UploadChangedFilesToHttp(uploadUrl, resourceFolderPath, changedFiles, platform);
            }
            else
            {
                Console.WriteLine("没有检测到文件变化，无需上传。");
            }
        }
        catch (FileNotFoundException ex)
        {
            Console.WriteLine($"错误：{ex.Message}Stack Trace:\n{ex.StackTrace}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"发生未知错误：{ex.Message}\nStack Trace:\n{ex.StackTrace}");
        }
    }

    // 加载或初始化 version.json 文件
    static VersionInfo LoadOrInitializeVersionJson(string jsonFilePath)
    {
        try
        {
            if (File.Exists(jsonFilePath))
            {
                string jsonContent = File.ReadAllText(jsonFilePath);
                return JsonConvert.DeserializeObject<VersionInfo>(jsonContent) ?? new VersionInfo();
            }
            return new VersionInfo { Version = "19700101.0", Md5 = new Dictionary<string, string>() };
        }
        catch (Exception ex)
        {
            throw new Exception($"加载或初始化 version.json 文件失败：{ex.Message}");
        }
    }

    // 保存 version.json 文件
    static void SaveVersionJson(string jsonFilePath, VersionInfo versionInfo)
    {
        try
        {
            string jsonContent = JsonConvert.SerializeObject(versionInfo, Formatting.Indented);
            File.WriteAllText(jsonFilePath, jsonContent);
        }
        catch (Exception ex)
        {
            throw new Exception($"保存 version.json 文件失败：{ex.Message}");
        }
    }

    // 递归计算文件夹中所有文件的 MD5 哈希值
    static Dictionary<string, string> CalculateFolderHashes(string folderPath)
    {
        var fileHashes = new Dictionary<string, string>();
        try
        {
            foreach (string file in Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(folderPath, file);
                if (relativePath == "version.json")
                    continue;

                string hash = CalculateFileMd5(file);
                fileHashes[relativePath] = hash;
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"计算文件夹哈希失败：{ex.Message}");
        }
        return fileHashes;
    }

    // 计算单个文件的 MD5 哈希值
    static string CalculateFileMd5(string filePath)
    {
        try
        {
            using (var md5 = MD5.Create())
            using (var stream = File.OpenRead(filePath))
            {
                byte[] hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"计算文件 MD5 哈希失败：{ex.Message}");
        }
    }

    // 获取发生变化的文件列表
    static List<string> GetChangedFiles(Dictionary<string, string> oldHashes, Dictionary<string, string> newHashes)
    {
        var changedFiles = new List<string>();
        try
        {
            // 检查新增或修改的文件
            foreach (var kvp in newHashes)
            {
                if (!oldHashes.ContainsKey(kvp.Key) || oldHashes[kvp.Key] != kvp.Value)
                {
                    changedFiles.Add(kvp.Key);
                }
            }

            // 检查被删除的文件
            foreach (var kvp in oldHashes)
            {
                if (!newHashes.ContainsKey(kvp.Key))
                {
                    changedFiles.Add(kvp.Key);
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"获取发生变化的文件列表失败：{ex.Message}");
        }
        return changedFiles;
    }

    // 更新版本号
    static string UpdateVersion(string currentVersion)
    {
        try
        {
            string today = DateTime.Now.ToString("yyyyMMdd");
            int versionNumber = 1;

            if (!string.IsNullOrEmpty(currentVersion))
            {
                string[] parts = currentVersion.Split('.');
                if (parts[0] == today)
                {
                    versionNumber = int.Parse(parts[1]) + 1;
                }
            }

            string newVersion = $"{today}.{versionNumber}";
            Console.WriteLine($"版本号更新为：{newVersion}");
            return newVersion;
        }
        catch (Exception ex)
        {
            throw new Exception($"更新版本号失败：{ex.Message}");
        }
    }

    // 上传发生变化的文件到 HTTP 服务器
    static async Task UploadChangedFilesToHttp(string uploadUrl, string folderPath, List<string> changedFiles, string platform)
    {
        try
        {
            using (var client = new HttpClient())
            {
                foreach (string relativePath in changedFiles)
                {
                    string localFilePath = Path.Combine(folderPath, relativePath);

                    // 如果文件被删除，则跳过上传
                    if (!File.Exists(localFilePath))
                    {
                        Console.WriteLine($"文件已被删除，跳过上传：{relativePath}");
                        continue;
                    }

                    using (var fileStream = File.OpenRead(localFilePath))
                    {
                        var content = new MultipartFormDataContent();
                        content.Add(new StreamContent(fileStream), "lampyris_resource_file", relativePath);
                        content.Add(new StringContent(platform), "platform");

                        var response = await client.PostAsync(uploadUrl, content);
                        if (response.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"已上传文件：{relativePath}");
                        }
                        else
                        {
                            Console.WriteLine($"上传文件失败：{relativePath}, 状态码：{response.StatusCode}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"上传文件到 HTTP 服务器失败：{ex.Message}");
        }
    }
}