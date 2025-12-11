using System.IO.Compression;
using System.Net;
using System.Net.WebSockets;
using System.Reflection;
using System.Collections.Concurrent;

using IMessage = Google.Protobuf.IMessage;
using Google.Protobuf;

using Lampyris.CSharp.Common;
using Lampyris.Crypto.Protocol.Common;
using Lampyris.Crypto.Server.Lite;
using Lampyris.Crypto.Protocol.App;

[Component]
public class WebSocketService : ILifecycle
{
    /// <summary>
    /// 客户端连接信息类，用于存储每个客户端的连接状态和相关信息。
    /// </summary>
    private class ClientConnectionInfo
    {
        public int ID; // 客户端唯一标识
        public WebSocket WebSocket; // 客户端的 WebSocket 对象
        public Thread HandlerThread; // 处理客户端消息的线程
        public DateTime ConnectionTime; // 客户端连接时间
        public DateTime LastHeartbeat; // 客户端最后一次心跳时间
    }

    // 存储客户端连接信息的字典，键为客户端 ID，值为连接信息
    private Dictionary<int, ClientConnectionInfo> m_UserId2ConnectionInfoMap = new Dictionary<int, ClientConnectionInfo>();

    // 消息处理器注册表，用于注册和查找消息处理器
    private MessageHandlerRegistry m_MessageHandlerRegistry = new MessageHandlerRegistry();

    // 存储 Response 类型中 Protobuf 消息的属性信息，用于动态设置消息
    private Dictionary<Type, PropertyInfo> m_MessagePropertyInfo = new Dictionary<Type, PropertyInfo>();

    // 客户端 ID 计数器，用于生成唯一的客户端 ID
    private int m_ID = 1;

    // 连接事件的回调函数
    public Action<int> OnNewConnection;
    public Action<int> OnDisconnection;
    public Action<int> OnReconnection;

    // 发送消息时的锁，确保线程安全
    private readonly object m_SendLock = new object();

    // 消息队列，用于存储待发送的消息
    private readonly ConcurrentQueue<(WebSocket WebSocket, IMessage Message)> m_MessageQueue = new ConcurrentQueue<(WebSocket, IMessage)>();

    // 消息发送线程
    private Thread? m_SendThread;

    // 标志位，用于控制发送线程的运行状态
    private volatile bool m_IsRunning = true;

    public override void OnStart()
    {
        // 获取 Response 类型的所有公共实例属性，这些属性是 Protobuf 消息类型
        var responseProperties = typeof(Response).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(t => typeof(IMessage).IsAssignableFrom(t.PropertyType));

        Response response = new Response();
        foreach (var propertyInfo in responseProperties)
        {
            // 将属性类型和属性信息存储到字典中
            m_MessagePropertyInfo[propertyInfo.PropertyType] = propertyInfo;
            propertyInfo.SetValue(response, null); // 预热，确保属性初始化
        }

        // 注册消息处理器
        m_MessageHandlerRegistry.RegisterHandlers();

        // 启动 WebSocket 服务
        Task.Run(async () => { await StartAsync("http://127.0.0.1:38888/"); });

        // 启动消息发送线程
        m_SendThread = new Thread(SendMessageThread);
        m_SendThread.Start();
    }

    public override void OnDestroy()
    {
        // 停止发送线程
        m_IsRunning = false;
        m_SendThread?.Join();
    }

    /// <summary>
    /// 启动 WebSocket 服务，监听客户端连接。
    /// </summary>
    public async Task StartAsync(string url)
    {
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add(url);
        listener.Start();
        Logger.LogInfo("WebSocket监听已启动");

        while (true)
        {
            HttpListenerContext context = await listener.GetContextAsync();
            if (context.Request.IsWebSocketRequest)
            {
                // 获取客户端 IP 和端口信息
                string? clientIp = context.Request.RemoteEndPoint?.Address.ToString();
                int clientPort = context.Request.RemoteEndPoint?.Port ?? 0;

                if (clientIp != null && clientPort > 0)
                {
                    WebSocket webSocket = (await context.AcceptWebSocketAsync(null)).WebSocket;
                    Logger.LogInfo($"新的WebSocket连接:{clientIp}:{clientPort}");

                    // 创建客户端连接信息对象
                    ClientConnectionInfo clientConnectionInfo = new ClientConnectionInfo();
                    clientConnectionInfo.ConnectionTime = DateTime.UtcNow;
                    clientConnectionInfo.WebSocket = webSocket;
                    clientConnectionInfo.ID = m_ID++;

                    // 将连接信息存储到字典中
                    m_UserId2ConnectionInfoMap[clientConnectionInfo.ID] = clientConnectionInfo;

                    // 启动线程处理客户端消息
                    clientConnectionInfo.HandlerThread = new Thread(() =>
                    {
                        HandleWebSocketAsync(clientConnectionInfo, webSocket);
                    });
                    clientConnectionInfo.HandlerThread.Start();
                }
            }
        }
    }

    /// <summary>
    /// 关闭客户端连接。
    /// </summary>
    private void ShutDownConnection(ClientConnectionInfo? clientConnectionInfo)
    {
        if (clientConnectionInfo != null)
        {
            clientConnectionInfo.WebSocket?.Dispose();
            Logger.LogInfo($"ID为{clientConnectionInfo.ID}的WebSocket连接关闭");
            m_UserId2ConnectionInfoMap.Remove(clientConnectionInfo.ID);
        }
    }

    /// <summary>
    /// 处理客户端 WebSocket 消息。
    /// </summary>
    private void HandleWebSocketAsync(ClientConnectionInfo clientConnectionInfo, WebSocket webSocket)
    {
        if (clientConnectionInfo == null || webSocket == null)
            return;

        byte[] buffer = new byte[1024 * 4];

        while (webSocket.State == WebSocketState.Open)
        {
            WebSocketReceiveResult? result = null;
            try
            {
                // 接收客户端消息
                result = webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None).Result;
            }
            catch
            {
                ShutDownConnection(clientConnectionInfo);
                return;
            }

            if (result == null)
                continue;

            if (result.MessageType == WebSocketMessageType.Close)
            {
                // 关闭连接
                webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                ShutDownConnection(clientConnectionInfo);
                return;
            }

            if (result.MessageType != WebSocketMessageType.Binary)
            {
                continue;
            }
            try
            {
                // 解压缩并解析 Protobuf 消息
                Request request = Request.Parser.ParseFrom(buffer, 0, result.Count);
                if (request.RequestTypeCase == Request.RequestTypeOneofCase.ReqHeartBeat)
                {
                    // 处理心跳消息
                    if (clientConnectionInfo != null)
                    {
                        ReqHeartBeat reqHeartBeat = request.ReqHeartBeat;
                        clientConnectionInfo.LastHeartbeat = DateTimeUtil.FromUnixTimestamp(reqHeartBeat.ClientTime);
                    }
                }
                else
                {
                    // 处理业务协议消息
                    if (clientConnectionInfo != null)
                    {
                        if (m_MessageHandlerRegistry.TryGetHandler(request.RequestTypeCase, out var handler))
                        {
                            handler(clientConnectionInfo.ID, request);
                        }
                        else
                        {
                            Logger.LogWarning($"未注册的协议类型:{request.RequestTypeCase.ToString()}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"协议客户端数据错误，原因:{ex.Message}");
            }
        }

        // 关闭连接
        ShutDownConnection(clientConnectionInfo);
    }

    /// <summary>
    /// 消息发送线程的逻辑
    /// </summary>
    private void SendMessageThread()
    {
        while (m_IsRunning)
        {
            // 检查队列中是否有消息
            if (m_MessageQueue.TryDequeue(out var messageInfo))
            {
                // 从队列中取出消息并发送
                WebSocket webSocket = messageInfo.WebSocket;
                IMessage message = messageInfo.Message;

                try
                {
                    if (webSocket.State == WebSocketState.Open)
                    {
                        Response response = new Response();
                        if (m_MessagePropertyInfo.ContainsKey(message.GetType()))
                        {
                            m_MessagePropertyInfo[message.GetType()].SetValue(response, message);
                        }
                        // 序列化响应消息
                        byte[] responseData = response.ToByteArray();

                        lock (m_SendLock)
                        {
                            webSocket.SendAsync(new ArraySegment<byte>(responseData), WebSocketMessageType.Binary, true, CancellationToken.None).Wait();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"发送消息失败，原因:{ex.Message}");
                }
            }
            else
            {
                // 如果队列为空，线程休眠一段时间，避免 CPU 空转
                Thread.Sleep(1);
            }
        }
    }

    /// <summary>
    /// 向指定客户端发送消息。
    /// </summary>
    public void PushMessage(int clientUserId, IMessage message)
    {
        if (m_UserId2ConnectionInfoMap.TryGetValue(clientUserId, out var clientConnectionInfo))
        {
            // 将消息添加到队列中
            m_MessageQueue.Enqueue((clientConnectionInfo.WebSocket, message));
        }
    }

    /// <summary>
    /// 向多个客户端发送消息。
    /// </summary>
    public void PushMessage(ICollection<int> clientUserIds, IMessage message)
    {
        foreach (int clientUserId in clientUserIds)
        {
            if (m_UserId2ConnectionInfoMap.TryGetValue(clientUserId, out var clientConnectionInfo))
            {
                // 将消息添加到队列中
                m_MessageQueue.Enqueue((clientConnectionInfo.WebSocket, message));
            }
        }
    }

    /// <summary>
    /// 广播消息给所有客户端。
    /// </summary>
    public void BroadcastMessage(IMessage message)
    {
        foreach (var pair in m_UserId2ConnectionInfoMap)
        {
            // 将消息添加到队列中
            m_MessageQueue.Enqueue((pair.Value.WebSocket, message));
        }
    }

    /// <summary>
    /// 压缩数据。
    /// </summary>
    private byte[] Compress(byte[] data)
    {
        using (var output = new MemoryStream())
        {
            using (var gzip = new GZipStream(output, CompressionMode.Compress))
            {
                gzip.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }
    }

    /// <summary>
    /// 解压缩数据。
    /// </summary>
    private byte[] Decompress(byte[] data, int length)
    {
        using (var input = new MemoryStream(data, 0, length))
        using (var gzip = new GZipStream(input, CompressionMode.Decompress))
        using (var output = new MemoryStream())
        {
            gzip.CopyTo(output);
            return output.ToArray();
        }
    }

    /// <summary>
    /// 向客户端发送通知消息。
    /// </summary>
    public void SendClientMessage(string content, ClientMessageType clientMessageType = ClientMessageType.NONE, string? param = "")
    {
        ResClientMessage resClientMessage = new ResClientMessage();
        resClientMessage.Content = content;
        resClientMessage.MessageType = (int)clientMessageType;
        if (!string.IsNullOrEmpty(param))
        {
            resClientMessage.Param = param;
        }
        BroadcastMessage(resClientMessage);
    }
}