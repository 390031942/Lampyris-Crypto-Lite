namespace Lampyris.CSharp.Common;
public static class DateTimeUtil
{
    public static DateTime FromUnixTimestamp(long unixTimestampMilliseconds)
    {
        // Unix 纪元时间
        DateTimeOffset dateTimeOffset = DateTimeOffset.FromUnixTimeMilliseconds(unixTimestampMilliseconds);
        // 转换为 DateTime 对象
        return dateTimeOffset.DateTime;
    }

    public static long ToUnixTimestampMilliseconds(DateTime dateTime)
    {
        // 将 DateTime 转换为 DateTimeOffset
        DateTimeOffset dateTimeOffset = new DateTimeOffset(dateTime);
        // 获取 Unix 时间戳（以毫秒为单位）
        return dateTimeOffset.ToUnixTimeMilliseconds();
    }

    public static long GetCurrentTimestamp()
    {
        return ToUnixTimestampMilliseconds(DateTime.Now);
    }

    public static string GetCurrentDateString()
    {
        return DateTime.Now.ToString("yyyyMMdd");
    }

    public static DateTime GetTodayDate()
    {
        DateTime dateTime = DateTime.Now;
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0, DateTimeKind.Utc);
    }

    public static DateTime GetCurrentMinute()
    {
        DateTime dateTime = DateTime.Now;
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, dateTime.Minute, 0, DateTimeKind.Utc);
    }

    public static int GetMinuteDiff(DateTime dt1, DateTime dt2)
    {
        // 计算时间差
        TimeSpan timeSpan = dt1.Subtract(dt2);

        // 计算时间差的分钟数，并向上取整
        return (int)Math.Ceiling(Math.Abs(timeSpan.TotalMinutes));
    }

    public static int GetMinuteDiff(long timestamp1, long timestamp2)
    {
        return GetMinuteDiff(FromUnixTimestamp(timestamp1), FromUnixTimestamp(timestamp2));
    }
}