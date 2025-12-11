using Lampyris.CSharp.Common;

using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;

public static class Application
{
    // 应用程序名称
    public static string Name { get; private set; } = "Lampyris.CSharp.Common";

    // 应用程序版本
    public static string Version { get; private set; } = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

    // 应用程序启动时间
    public static DateTime StartTime { get; private set; } = Process.GetCurrentProcess().StartTime;

    // 应用程序运行时长
    public static TimeSpan Uptime => DateTime.Now - StartTime;

    // 当前内存使用情况（以 MB 为单位）
    public static double MemoryUsage => Process.GetCurrentProcess().WorkingSet64 / 1024.0 / 1024.0;

    // 当前 CPU 使用率（百分比）
    public static double CpuUsage => GetCpuUsage();

    // 当前网络流量（字节）
    public static long NetworkUsage => GetNetworkUsage();

    // 应用程序日志文件路径
    public static string LogFilePath { get; private set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "application.log");

    // 是否启用调试模式
    public static bool IsDebugMode { get; private set; } = false;

    // 设置应用程序名称
    public static void SetName(string name)
    {
        Name = name;
    }

    // 设置应用程序版本
    public static void SetVersion(string version)
    {
        Version = version;
    }

    // 启用或禁用调试模式
    public static void SetDebugMode(bool isDebug)
    {
        IsDebugMode = isDebug;
    }

    // 获取操作系统信息
    public static string GetOSInfo()
    {
        return RuntimeInformation.OSDescription;
    }

    // 获取当前用户信息
    public static string GetCurrentUser()
    {
        return Environment.UserName;
    }

    // 获取当前进程 ID
    public static int ProcessId => Process.GetCurrentProcess().Id;

    // 获取当前线程 ID
    public static int ThreadId => Thread.CurrentThread.ManagedThreadId;

    // 获取当前目录
    public static string CurrentDirectory => Directory.GetCurrentDirectory();

    // 获取当前应用程序路径
    public static string ApplicationPath => AppDomain.CurrentDomain.BaseDirectory;

    // 获取当前网络状态
    public static bool IsNetworkAvailable => NetworkInterface.GetIsNetworkAvailable();

    // 退出应用程序
    public static void Exit(int exitCode = 0)
    {
        Environment.Exit(exitCode);
    }

    // 私有方法：获取当前 CPU 使用率
    private static double GetCpuUsage()
    {
        using (var process = Process.GetCurrentProcess())
        {
            TimeSpan startCpuUsage = process.TotalProcessorTime;
            DateTime startTime = DateTime.UtcNow;

            Thread.Sleep(500); // 等待 500ms

            TimeSpan endCpuUsage = process.TotalProcessorTime;
            DateTime endTime = DateTime.UtcNow;

            double cpuUsedMs = (endCpuUsage - startCpuUsage).TotalMilliseconds;
            double totalMsPassed = (endTime - startTime).TotalMilliseconds;

            int processorCount = Environment.ProcessorCount;
            return (cpuUsedMs / totalMsPassed) * 100 / processorCount;
        }
    }

    // 私有方法：获取当前网络流量
    private static long GetNetworkUsage()
    {
        long totalBytes = 0;
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            var stats = nic.GetIPv4Statistics();
            totalBytes += stats.BytesReceived + stats.BytesSent;
        }
        return totalBytes;
    }
}
