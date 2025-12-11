namespace Lampyris.Crypto.Server.Lite;

using Lampyris.Crypto.Protocol.App;
using Lampyris.CSharp.Common;

[Component]
public class UploadLoggerService: ILifecycle, ILogger
{
    [Autowired]
    private WebSocketService m_WebSocketService;

    /// <summary>
    /// 所有日志缓存的列表（每日清除）
    /// </summary>
    private List<string> m_HistoryLog = new List<string>(10000);

    /// <summary>
    /// 对于新的连接的推送数量
    /// </summary>
    private int m_NewConnectionPushCount = 50;

    /// <summary>
    /// 已经上传的Log数量
    /// </summary>
    private int m_UploadCount = 0;

    /// <summary>
    /// 缓存的ResServerLogPush
    /// </summary>
    private ResServerLogPush m_ResServerLogPush = new ResServerLogPush();

    public void Log(string message)
    {
        lock (m_HistoryLog)
        {
            m_ResServerLogPush.Index = m_HistoryLog.Count;
            m_HistoryLog.Add(message);
        }
        m_ResServerLogPush.Log = message;
        m_WebSocketService.BroadcastMessage(m_ResServerLogPush);
    }

    public override void OnStart()
    {
        Logger.AddLogger(this);
        m_WebSocketService.OnNewConnection += (int clientId) =>
        {
            // 新的连接需要推送最近若干条消息
            lock (m_HistoryLog)
            {
                if (m_HistoryLog.Count > 0)
                {
                    ResServerLogQuery resServerLogQuery = new ResServerLogQuery();
                    resServerLogQuery.StartIndex = Math.Max(0, m_HistoryLog.Count - m_NewConnectionPushCount - 1);
                    resServerLogQuery.EndIndex = m_HistoryLog.Count - 1;
                    m_WebSocketService.PushMessage(clientId, resServerLogQuery);
                }
            }
        };

        PlannedTaskScheduler.AddDailySpecificTimeTask(DateTimeNode.PREPARE_WORKING, ()=>
        {
            lock (m_HistoryLog)
            {
                m_HistoryLog.Clear();
            }
        });
    }

    public override int Priority => int.MinValue;
}
