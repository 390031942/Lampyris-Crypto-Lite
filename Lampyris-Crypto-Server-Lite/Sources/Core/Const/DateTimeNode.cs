namespace Lampyris.Crypto.Server.Lite;

/// <summary>
/// 重要时间节点（UTC+8时间，精确到时分秒）
/// </summary>
public static class DateTimeNode
{
    /// <summary>
    /// 相对于 UTC+0的0点前的 准备工作
    /// </summary>
    public static DateTime PREPARE_WORKING = FromHourMinuteSecond(07, 50, 0);

    /// <summary>
    /// 相对于 UTC+0的0点时刻，正式开始工作！
    /// </summary>
    public static DateTime START_WORKING = FromHourMinuteSecond(08, 00, 0);

    /// <summary>
    /// 相对于 UTC+0的晚上12点，可以歇逼
    /// </summary>
    public static DateTime NIGHT_STOP_WORKING = FromHourMinuteSecond(23, 59, 55);


    private static DateTime FromHourMinuteSecond(int hour, int minute, int second) 
    { 
        return new DateTime(0,0,0,hour, minute, second);
    }
    
}
