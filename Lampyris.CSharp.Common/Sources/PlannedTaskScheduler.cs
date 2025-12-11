namespace Lampyris.CSharp.Common;

using System;
using System.Collections.Generic;

public static class PlannedTaskScheduler
{
    /* 自增的key */
    private static int m_IncreaseKey = 0;

    /* key对应的DelayHandler */
    private static readonly Dictionary<int, DelayHandler> m_Id2DelayHandlerDict = new Dictionary<int, DelayHandler>();

    /* 要移除ID的临时列表 */
    private static readonly List<int> m_ShouldRemoveIDList = new List<int>();

    private static long m_LastTimestamp = 0L;

    private enum DelayHandlerType
    {
        Interval = 0,
        Tick = 1,
        DailySpecificTime = 2,  // 修改：每天指定时刻执行
    }

    private class DelayHandler
    {
        public DelayHandlerType Type;
        public Action? Action;
        public float DelayMs;
        public int DelayFrame;
        public int RepeatTime;
        public float TotalTime;
        public int TotalFrame;
        public TimeSpan TargetTime;     // 修改：目标执行时间（时分秒）
        public DateTime NextExecution; // 新增：下次执行的完整时间（包含年月日）
    }

    public static int AddTimeTask(Action action, float delayMs, int repeatTime = -1)
    {
        lock (m_Id2DelayHandlerDict)
        {
            int id = m_IncreaseKey++;
            m_Id2DelayHandlerDict[id] = new DelayHandler()
            {
                Type = DelayHandlerType.Interval,
                Action = action,
                DelayMs = delayMs,
                RepeatTime = repeatTime,
            };

            return id;
        }
    }

    public static int AddTickTask(Action action, int delayFrame, int repeatTime = -1)
    {
        lock (m_Id2DelayHandlerDict)
        {
            int id = m_IncreaseKey++;
            m_Id2DelayHandlerDict[id] = new DelayHandler()
            {
                Type = DelayHandlerType.Tick,
                Action = action,
                DelayFrame = delayFrame,
                RepeatTime = repeatTime,
            };

            return id;
        }
    }

    /// <summary>
    /// 添加每天指定时刻执行的任务
    /// </summary>
    /// <param name="dateTime">时分秒</param>
    /// <param name="action">要执行的回调</param>
    /// <returns>任务ID</returns>
    public static int AddDailySpecificTimeTask(DateTime dateTime, Action action)
    {
        return AddDailySpecificTimeTask(dateTime.Hour, dateTime.Minute, dateTime.Second, action);
    }

    /// <summary>
    /// 添加每天指定时刻执行的任务
    /// </summary>
    /// <param name="hour">小时 (0-23)</param>
    /// <param name="minute">分钟 (0-59)</param>
    /// <param name="second">秒 (0-59)</param>
    /// <param name="action">要执行的回调</param>
    /// <returns>任务ID</returns>
    public static int AddDailySpecificTimeTask(int hour, int minute, int second, Action action)
    {
        lock (m_Id2DelayHandlerDict)
        {
            // 检查参数有效性
            if (hour < 0 || hour > 23 || minute < 0 || minute > 59 || second < 0 || second > 59)
            {
                throw new ArgumentException("时间参数无效");
            }

            TimeSpan targetTime = new TimeSpan(hour, minute, second);
            DateTime now = DateTime.Now;
            DateTime nextExecution = now.Date + targetTime;

            // 如果当前时间已经超过目标时间，设置为第二天的目标时间
            if (now.TimeOfDay > targetTime)
            {
                nextExecution = nextExecution.AddDays(1);
            }

            int id = m_IncreaseKey++;
            m_Id2DelayHandlerDict[id] = new DelayHandler()
            {
                Type = DelayHandlerType.DailySpecificTime,
                Action = action,
                TargetTime = targetTime,
                NextExecution = nextExecution,
            };

            return id;
        }
    }

    public static void Clear(int id)
    {
        lock (m_Id2DelayHandlerDict)
        {
            if (m_Id2DelayHandlerDict.ContainsKey(id))
            {
                m_Id2DelayHandlerDict.Remove(id);
            }
        }
    }

    public static void Update()
    {
        lock (m_Id2DelayHandlerDict)
        {
            long timestamp = DateTimeUtil.GetCurrentTimestamp();
            long deltaTime = timestamp - m_LastTimestamp;
            DateTime now = DateTime.Now;

            foreach (var pair in m_Id2DelayHandlerDict)
            {
                bool shouldDoAction = false;
                DelayHandler delayHandler = pair.Value;

                if (delayHandler.Type == DelayHandlerType.Interval)
                {
                    delayHandler.TotalTime += deltaTime;
                    if (delayHandler.TotalTime >= delayHandler.DelayMs)
                    {
                        shouldDoAction = true;
                        delayHandler.TotalTime = 0.0f;
                    }
                }
                else if (delayHandler.Type == DelayHandlerType.Tick)
                {
                    delayHandler.TotalFrame += 1;
                    if (delayHandler.TotalFrame >= delayHandler.DelayFrame)
                    {
                        shouldDoAction = true;
                        delayHandler.TotalFrame = 0;
                    }
                }
                else if (delayHandler.Type == DelayHandlerType.DailySpecificTime)
                {
                    // 检查是否到达指定时刻
                    if (now >= delayHandler.NextExecution)
                    {
                        shouldDoAction = true;
                        // 更新下次执行时间为第二天的目标时间
                        delayHandler.NextExecution = delayHandler.NextExecution.AddDays(1);
                    }
                }

                if (shouldDoAction)
                {
                    delayHandler.Action?.Invoke();
                    if (delayHandler.Type != DelayHandlerType.DailySpecificTime && delayHandler.RepeatTime != -1)
                    {
                        if (--delayHandler.RepeatTime <= 0)
                        {
                            m_ShouldRemoveIDList.Add(pair.Key);
                        }
                    }
                }
            }

            foreach (int id in m_ShouldRemoveIDList)
            {
                Clear(id);
            }
            m_ShouldRemoveIDList.Clear();
            m_LastTimestamp = timestamp;
        }
    }
}