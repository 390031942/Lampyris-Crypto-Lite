namespace Lampyris.CSharp.Common;

[Component]

public class MessageService
{
    // 字典存储消息类型和对应的订阅者列表
    private readonly Dictionary<string, List<Action<object[]>>> m_messageHandlers = new Dictionary<string, List<Action<object[]>>>();

    /// <summary>
    /// 订阅消息
    /// </summary>
    /// <param name="messageType">消息类型</param>
    /// <param name="handler">处理消息的回调函数</param>
    public void Subscribe(string messageType, Action<object[]> handler)
    {
        if (!m_messageHandlers.ContainsKey(messageType))
        {
            m_messageHandlers[messageType] = new List<Action<object[]>>();
        }
        m_messageHandlers[messageType].Add(handler);
    }

    /// <summary>
    /// 取消订阅消息
    /// </summary>
    /// <param name="messageType">消息类型</param>
    /// <param name="handler">处理消息的回调函数</param>
    public void Unsubscribe(string messageType, Action<object[]> handler)
    {
        if (m_messageHandlers.ContainsKey(messageType))
        {
            m_messageHandlers[messageType].Remove(handler);
            if (m_messageHandlers[messageType].Count == 0)
            {
                m_messageHandlers.Remove(messageType);
            }
        }
    }

    /// <summary>
    /// 分发消息
    /// </summary>
    /// <param name="messageType">消息类型</param>
    /// <param name="args">消息参数</param>
    public void Dispatch(string messageType, params object[] args)
    {
        if (m_messageHandlers.ContainsKey(messageType))
        {
            foreach (var handler in m_messageHandlers[messageType])
            {
                handler?.Invoke(args);
            }
        }
    }
}