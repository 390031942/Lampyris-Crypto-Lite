using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Objects.Models.Futures.Socket;
using Binance.Net.Objects.Models.Spot.Socket;
using CryptoExchange.Net.Objects.Sockets;
using Lampyris.CSharp.Common;
using System.Collections.Concurrent;

namespace Lampyris.Crypto.Server.Lite;

[Component]
public class QuoteService : ILifecycle
{
    [Autowired]
    private ProxyProviderService m_ProxyProvideService;

    [Autowired]
    private WebSocketService m_WebSocketService;

    #region Binance连接客户端相关

    /// <summary>
    /// 存储了一个或多个BinanceRestClient的列表。
    /// 多个的情况，取决于使用Proxy的数量。如果没有使用Proxy，那只会有一个
    /// </summary>
    private List<BinanceRestClient> m_RestClientList = new();

    /// <summary>
    /// 轮流使用BinanceRestClient对象所记录的索引
    /// </summary>ss
    private int m_RestClientIndex = -1;

    /// <summary>
    /// REST客户端，为了方便轮流取值所以定义为属性
    /// m_RestClientList多个情况下，轮流取值，可以避免速率控制
    /// </summary>
    private BinanceRestClient m_RestClient
    {
        get
        {
            return m_RestClientList[(m_RestClientIndex + 1 % m_RestClientList.Count)];
        }
    }

    /// <summary>
    /// 用于订阅的公共数据的WebSocketClient，比如Ticker，标记价格数据,和Trade数据，
    /// 这些数据就算全部都订阅了，也不会超过stream的数目，所以使用一个BinanceSocketClient对象
    /// 这个BinanceSocketClient对象 同时还用于WebSocket API的调用
    /// </summary>
    private BinanceSocketClient m_CommonSocketClient;

    /// <summary>
    /// 不同KlineInterval对应的WebSocket对象，这样的好处是可以确保BinanceSocketClient使用的stream数量不会超过最大值1024。
    /// </summary>
    private Dictionary<KlineInterval, BinanceSocketClient> m_KLineSubscriptionSocketClient;
    #endregion

    #region 订阅句柄
    private ConcurrentDictionary<string, Dictionary<KlineInterval, UpdateSubscription>> m_KlineSubscriptions = new();
    private ConcurrentDictionary<string, UpdateSubscription> m_TradeSubscriptions = new();
    private UpdateSubscription m_MarkPriceSubscription;
    #endregion

    #region 行情数据+交易规则
    /// <summary>
    /// 存储全体USDT永续合约 symbol
    /// </summary>
    protected HashSet<string> m_Symbols = new HashSet<string>();

    /// <summary>
    /// 存储每个USDT永续合约的交易规则
    /// </summary>
    protected Dictionary<string, SymbolTradeRule> m_Symbol2TradeRuleMap = new Dictionary<string, SymbolTradeRule>();

    /// <summary>
    /// 市场概况
    /// </summary>
    private MarketSummaryData m_MarketSummaryData = new MarketSummaryData();

    /// <summary>
    /// Ticker行情字典(不需要存到数据库中，所以直接存储到这里)
    /// </summary>
    protected Dictionary<string, TickerData> m_TickerDataMap = new();

    /// <summary>
    /// Ticker行情列表(方便遍历)
    /// </summary>
    protected List<TickerData> m_TickerDataList = new();

    /// <summary>
    /// 主流币
    /// </summary>
    private HashSet<string> m_MainStreamSymbols = new HashSet<string>()
    {
        "BTCUSDT",
        "ETHUSDT"
    };

    /// <summary>
    /// 关心的K线尺度,只有在这个集合内的KlineInterval的K线数据才会被缓存 + 订阅推送 + 同步计算MA指标值
    /// 其中value保存了某个KlineInterval需要缓存的K线数目
    /// </summary>
    private Dictionary<KlineInterval,int> m_ConcernedIntervalMap = new Dictionary<KlineInterval, int>
    {
        {KlineInterval.OneMinute,1500 },
        {KlineInterval.FifteenMinutes,100 },
        {KlineInterval.OneDay,30 },
    };

    /// <summary>
    /// 单次API查询的最大K线数量
    /// </summary>
    private const int KLINE_MAX_COUNT_PER_QUERY = 1500;

    /// <summary>
    /// 服务器时间
    /// </summary>
    private DateTime m_ServerTime;

    #endregion

    #region K线数据缓存
    private Dictionary<string, Dictionary<KlineInterval, CircularBuffer<CandlestickData>>> m_KLineCacheDataMap;
    #endregion

    private BinanceRestClient CreateRestClient(bool useProxy, ProxyInfo? proxyInfo = null)
    {
        return new BinanceRestClient(options =>
        {
            options.AutoTimestamp = true;
            options.RateLimiterEnabled = true;
            options.RateLimitingBehaviour = CryptoExchange.Net.Objects.RateLimitingBehaviour.Wait;

            if (useProxy && proxyInfo != null)
            {
                options.Proxy = new CryptoExchange.Net.Objects.ApiProxy("http://" + proxyInfo.Address, proxyInfo.Port);
            }
        });
    }

    private BinanceSocketClient CreateSocketClient(bool useProxy, ProxyInfo? proxyInfo = null)
    {
        return new BinanceSocketClient(options =>
        {
            options.RateLimiterEnabled = true;
            options.RateLimitingBehaviour = CryptoExchange.Net.Objects.RateLimitingBehaviour.Wait;

            if (useProxy && proxyInfo != null)
            {
                options.Proxy = new CryptoExchange.Net.Objects.ApiProxy("http://" + proxyInfo.Address, proxyInfo.Port);
            }
        });
    }

    private void InitializeRestClients(bool useProxy)
    {
        m_RestClientList = new List<BinanceRestClient>(m_ProxyProvideService.ProxyCount);

        if (useProxy)
        {
            for (int i = 0; i < m_ProxyProvideService.ProxyCount; i++)
            {
                var proxyInfo = m_ProxyProvideService.Get(i);
                if (proxyInfo == null)
                {
                    throw new ApplicationException("初始化Socket客户端失败: 代理信息无效");
                }
                m_RestClientList.Add(CreateRestClient(useProxy, proxyInfo));
            }
        }
        else
        {
            m_RestClientList.Add(CreateRestClient(useProxy));
        }
    }

    private void InitializeSocketClients(bool useProxy, IEnumerable<KlineInterval> concernedIntervals)
    {
        if (useProxy)
        {
            var proxyInfo = m_ProxyProvideService.Get(0);
            if (proxyInfo == null)
            {
                throw new ApplicationException("初始化Socket客户端失败: 代理信息无效");
            }

            m_CommonSocketClient = CreateSocketClient(useProxy, proxyInfo);

            foreach (var interval in concernedIntervals)
            {
                m_KLineSubscriptionSocketClient[interval] = CreateSocketClient(useProxy, proxyInfo);
            }
        }
        else
        {
            m_CommonSocketClient = CreateSocketClient(useProxy);

            foreach (var interval in concernedIntervals)
            {
                m_KLineSubscriptionSocketClient[interval] = CreateSocketClient(useProxy);
            }
        }
    }

    public override void OnStart()
    {
        bool useProxy = true; // m_ProxyProvideService.UseProxy;

        // 初始化 REST 客户端
        InitializeRestClients(useProxy);

        // 初始化 WebSocket 客户端
        InitializeSocketClients(useProxy, m_ConcernedIntervalMap.Keys);

        // 初始化行情数据
        // 获取USDT永续合约名单 + 每个合约的交易规则
        APIUpdateExchangeInfo();

        // 订阅Ticker + Mark Price 更新
        APISubscriptionAll();

        // 缓存 + 订阅K线数据
        APICacheKLineDataAndSubscribe();

        // 初始化定时任务
        PlannedTaskScheduler.AddDailySpecificTimeTask(DateTimeNode.PREPARE_WORKING,() =>
        {
            Logger.LogInfo("======== 盘前任务-行情系统 开始 ========");
            Logger.LogInfo("开始更新全体USDT永续合约名单，并获取每个合约的交易规则...");
            APIUpdateExchangeInfo();
            Logger.LogInfo("======== 盘前任务-行情系统 结束 ========");
        });

        // TODO: 随公告更新ExchangeInfo
    }

    /// <summary>
    /// 刷新交易信息，包括所有symbol列表和每个symbol的交易规则
    /// 调用时机，程序启动和每日刷新
    /// </summary>
    /// <param name="updateOnly">是否只更新信息，不能添加任何新上架的交易对</param>
    private void APIUpdateExchangeInfo(bool updateOnly = false)
    {
        try
        {
            // 获取所有 USDT 永续合约的 symbol
            var exchangeInfo = m_RestClient.UsdFuturesApi.ExchangeData.GetExchangeInfoAsync().Result;
            if (!exchangeInfo.Success)
            {
                Logger.LogError($"USDT永续合约名单 更新失败: {exchangeInfo.Error?.Message}");
                return;
            }

            // 筛选出所有 USDT 永续合约的 symbol
            var usdtSymbolExchangeInfoList = exchangeInfo.Data.Symbols
                .Where(s => s.ContractType == ContractType.Perpetual && s.QuoteAsset == "USDT" && s.Status == SymbolStatus.Trading)
                .ToList();

            Logger.LogInfo($"USDT永续合约名单 更新完成，总数量={usdtSymbolExchangeInfoList.Count}");

            m_Symbol2TradeRuleMap.Clear();

            foreach (var symbolInfo in usdtSymbolExchangeInfoList)
            {
                m_Symbols.Add(symbolInfo.Name);

                // 处理updateOnly, 如果为true，需要检查之前在不在m_Symbols集合中
                if (updateOnly)
                {
                    if(!m_Symbols.Contains(symbolInfo.Name))
                    {
                        continue;
                    }
                }

                if (!m_Symbol2TradeRuleMap.ContainsKey(symbolInfo.Name))
                {
                    m_Symbol2TradeRuleMap[symbolInfo.Name] = new SymbolTradeRule();
                }
                m_Symbol2TradeRuleMap[symbolInfo.Name] = Converter.ToSymbolTradeRule(symbolInfo, m_Symbol2TradeRuleMap[symbolInfo.Name]);
            }
            Logger.LogInfo($"USDT永续合约交易规则更新完成");
            m_Symbols = usdtSymbolExchangeInfoList.Select(s => s.Name).ToHashSet();

        }
        catch (Exception ex)
        {
            Logger.LogError($"USDT永续合约名单更新 发生异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 缓存必要的K线数据 并订阅K线数据更新
    /// </summary>
    private void APICacheKLineDataAndSubscribe()
    {
        // 使用多线程访问 K线数据
        var tasks = new List<Task>();
        foreach (var symbol in m_Symbols)
        {
            if (!m_KLineCacheDataMap.ContainsKey(symbol))
            {
                m_KLineCacheDataMap[symbol] = new Dictionary<KlineInterval, CircularBuffer<CandlestickData>>();
            }

            var map = m_KLineCacheDataMap[symbol];
            if (!map.ContainsKey(KlineInterval.OneMinute))
            {
                map[KlineInterval.OneMinute] = new CircularBuffer<CandlestickData>(1500); // 最多缓存1500根1分钟k线
            }
            if (!map.ContainsKey(KlineInterval.OneDay))
            {
                map[KlineInterval.OneDay] = new CircularBuffer<CandlestickData>(3);
            }

            tasks.Add(Task.Run(async () =>
            {
                var response = await m_RestClient.UsdFuturesApi.ExchangeData.GetKlinesAsync(symbol, KlineInterval.OneMinute, limit: 10);
                if (!response.Success)
                {
                    Logger.LogError($"获取 {symbol} 的 K线数据失败: {response.Error}");
                }
                else
                {
                    var buffer = m_KLineCacheDataMap[symbol][KlineInterval.OneMinute];
                    foreach (var data in response.Data)
                    {
                        var candlestickData = Converter.ToCandlestickData(data);
                        buffer.Add(candlestickData);
                    }

                    // 订阅数据
                    APISubscribeToKlineUpdates(symbol, KlineInterval.OneMinute);
                }
            }));
        }

        // 等待所有任务完成
        Task.WaitAll(tasks.ToArray());
    }

    /// <summary>
    /// 订阅全体Symbol的通用更新
    /// </summary>
    protected void APISubscriptionAll()
    {
        // 获取全体 USDT 永续合约的 symbol 列表
        var symbols = GetSymbolList();
        if (symbols != null)
        {
            foreach (var symbol in symbols)
            {
                m_Symbols.Add(symbol);
            }

            // 初始化订阅
            var task1 = APISubscribeToTickerUpdatesForAllSymbols();
            // var task2 = APISubscribeToTradeUpdatesForAllSymbols();
            var task3 = APISubscribeToMarkPriceUpdatesForAllSymbols();

            Task.WaitAll(task1, task3);
        }
    }

    /// <summary>
    /// 获取全体USDT永续合约列表
    /// </summary>
    /// <returns></returns>
    public IEnumerable<string> GetSymbolList()
    {
        return m_Symbols;
    }

    #region Ticker数据
    /// <summary>
    /// 订阅全体 symbol 的 Ticker 数据
    /// </summary>
    private async Task APISubscribeToTickerUpdatesForAllSymbols()
    {
        if (m_RestClient != null && m_CommonSocketClient != null)
        {
            var result = await m_RestClient.UsdFuturesApi.ExchangeData.GetTickersAsync();
            await m_CommonSocketClient.UsdFuturesApi.ExchangeData.SubscribeToAllTickerUpdatesAsync(OnTickerUpdate);
        }
    }

    [BinanceAPICallback]
    private void OnTickerUpdate(DataEvent<IBinance24HPrice[]> dataEvent)
    {
        if (dataEvent != null && dataEvent.DataTime != null)
        {
            m_ServerTime = dataEvent.DataTime.Value;
            long timestamp = DateTimeUtil.ToUnixTimestampMilliseconds(m_ServerTime);
            foreach (var rawTickerData in dataEvent.Data)
            {
                string symbol = rawTickerData.Symbol;

                TickerData? tickerData = null;
                if (m_TickerDataMap.ContainsKey(rawTickerData.Symbol))
                {
                    tickerData = m_TickerDataMap[rawTickerData.Symbol];
                }
                else
                {
                    tickerData = new TickerData();
                    m_TickerDataList.Add(tickerData);
                }
                m_TickerDataMap[rawTickerData.Symbol] = Converter.ToTickerData(timestamp, rawTickerData, tickerData);

                // 3min涨速计算，需要取到缓存的K线数据
                var candlestickData = QueryNthRecentlyCachedCandlestickData(symbol, KlineInterval.OneMinute, 3);
                if (candlestickData != null && candlestickData.Close > 0)
                {
                    tickerData.RiseSpeed = Math.Round((tickerData.Price - candlestickData.Close) / candlestickData.Close * 100, 2);
                }
                else
                {
                    tickerData.RiseSpeed = 0;
                }
            }

            // 根据涨幅降序排序
            m_TickerDataList.Sort((lhs, rhs) =>
            {
                if (lhs.ChangePerc == rhs.ChangePerc) return 0;
                return rhs.ChangePerc > lhs.ChangePerc ? 1 : -1;
            });

            RecalculateMarketPreviewData();
            PushMarketPreviewData();
        }
    }

    /// <summary>
    /// 重新计算市场总览数据，包括涨跌平的合约数，主流币平均涨幅，前10的平均涨幅跌幅，以及涨跌幅分布
    /// </summary>
    protected void RecalculateMarketPreviewData()
    {
        m_MarketSummaryData.Reset();

        decimal percentageSum = 0.0m;
        decimal top10PercentageSum = 0.0m;
        decimal last10PercentageSum = 0.0m;
        decimal mainStreamPercentageSum = 0.0m;

        for (int i = 0; i < m_TickerDataList.Count; i++)
        {
            var quoteTickerData = m_TickerDataList[i];
            if (quoteTickerData.ChangePerc > 0)
            {
                m_MarketSummaryData.RiseCount++;
            }
            else if (quoteTickerData.ChangePerc < 0)
            {
                m_MarketSummaryData.FallCount++;
            }
            else
            {
                m_MarketSummaryData.UnchangedCount++;
            }

            if (m_MainStreamSymbols.Contains(quoteTickerData.Symbol))
            {
                mainStreamPercentageSum += quoteTickerData.ChangePerc;
            }

            if (i < 10)
            {
                top10PercentageSum += quoteTickerData.ChangePerc;
            }
            else if (i >= m_TickerDataList.Count - 10)
            {
                last10PercentageSum += quoteTickerData.ChangePerc;
            }
            percentageSum += quoteTickerData.ChangePerc;
            m_MarketSummaryData.RecordPerc(quoteTickerData.ChangePerc);
        }

        m_MarketSummaryData.AvgChangePerc = percentageSum / m_TickerDataList.Count;
        m_MarketSummaryData.MainStreamAvgChangePerc = mainStreamPercentageSum / m_MainStreamSymbols.Count;
        m_MarketSummaryData.Top10AvgChangePerc = top10PercentageSum / 10;
        m_MarketSummaryData.Last10AvgChangePerc = last10PercentageSum / 10;
    }

    public TickerData? GetTickerData(string symbol)
    {
        if (m_TickerDataMap.ContainsKey(symbol))
        {
            return m_TickerDataMap[symbol];
        }
        return null;
    }

    protected void PushMarketPreviewData()
    {
        QuotePushUtil.PushMarketPreviewData(m_WebSocketService, m_MarketSummaryData);
    }
    #endregion

    #region K线数据
    /// <summary>
    /// API查询K线数据(不考虑缓存，直接从API进行下载)
    /// </summary>
    public List<CandlestickData> APIQueryCandlestickData(string symbol, KlineInterval interval, DateTime? startTime, DateTime? endTime, int n = -1)
    {
        n = n == -1 ? KLINE_MAX_COUNT_PER_QUERY : n;
        return APIQueryCandlestickDataImpl(symbol, interval, startTime, endTime, n);
    }

    /// <summary>
    /// 主线程查询K线数据(当查询数量小于等于KLINE_MAX_COUNT_PER_QUERY)
    /// </summary>
    private List<CandlestickData> APIQueryCandlestickDataNonThreaded(string symbol, KlineInterval interval, DateTime? startTime, DateTime? endTime, int limit)
    {
        var response = m_RestClient.UsdFuturesApi.ExchangeData.GetKlinesAsync(
            symbol: symbol,
            interval: interval,
            startTime: startTime,
            endTime: endTime,
            limit: limit
        ).Result;

        if (!response.Success || response.Data == null)
        {
            Logger.LogError($"API查询Symbol={symbol},Interval={interval}的K线数据 失败: {response.Error?.Message}");
            return new List<CandlestickData>();
        }

        return response.Data.Select(kline => Converter.ToCandlestickData(kline)).ToList();
    }

    /// <summary>
    /// 查询K线数据的实现
    /// </summary>
    private List<CandlestickData> APIQueryCandlestickDataImpl(string symbol, KlineInterval interval, DateTime? startTime, DateTime? endTime, int? limit)
    {
        // 如果 limit 未设置，默认为 500
        limit ??= 500;

        // 规则 1: 如果没有 startTime
        if (!startTime.HasValue)
        {
            if (endTime.HasValue)
            {
                // 根据 endTime 和 limit 推出 startTime
                startTime = endTime.Value.AddSeconds(-(int)interval * limit.Value);
            }
            else
            {
                // 如果没有 endTime 和 limit，则根据服务器时间和 limit 推出 startTime
                endTime = m_ServerTime;
                startTime = endTime.Value.AddSeconds(-(int)interval * limit.Value);
            }
        }
        else
        {
            // 规则 2: 如果有 startTime
            if (endTime.HasValue)
            {
                // 根据 endTime 和 limit 推出 startTime，覆盖掉之前的 startTime
                startTime = endTime.Value.AddSeconds(-(int)interval * limit.Value);
            }
            // 如果没有 endTime，则 endTime 不传值（保持为 null）
        }

        // 退化为主线程查询
        if (limit.Value <= KLINE_MAX_COUNT_PER_QUERY)
        {
            return APIQueryCandlestickDataNonThreaded(symbol, interval, startTime, endTime, limit.Value);
        }

        // 分段时间范围
        List<(DateTime Start, DateTime End)> timeRanges = new List<(DateTime, DateTime)>();

        DateTime currentStartTime = startTime.Value;

        while (!endTime.HasValue || currentStartTime < endTime.Value)
        {
            DateTime currentEndTime = currentStartTime.AddSeconds((int)interval * KLINE_MAX_COUNT_PER_QUERY);
            if (endTime.HasValue && currentEndTime > endTime.Value)
                currentEndTime = endTime.Value;

            timeRanges.Add((currentStartTime, currentEndTime));
            currentStartTime = currentEndTime;

            // 如果没有 endTime，则根据 limit 和 interval 控制时间范围
            if (!endTime.HasValue && timeRanges.Count * KLINE_MAX_COUNT_PER_QUERY >= limit.Value)
            {
                break;
            }
        }

        // 多线程并发请求
        var tasks = timeRanges.Select(range =>
        {
            return Task.Run(() => APIQueryCandlestickData(symbol, interval, range.Start, range.End, KLINE_MAX_COUNT_PER_QUERY));
        });

        // 等待所有任务完成并合并结果
        Task.WaitAll(tasks.ToArray());
        List<CandlestickData> result = new List<CandlestickData>();
        foreach (var task in tasks)
        {
            result.AddRange(task.Result);
        }

        // 如果结果数量大于限制，则取最近的 n 条数据
        if (result.Count > limit.Value)
        {
            result = result.TakeLast(limit.Value).ToList();
        }

        return result;
    }

    /// <summary>
    /// 订阅指定 symbol的K 线数据
    /// </summary>
    private void APISubscribeToKlineUpdates(string symbol, KlineInterval interval)
    {
        BinanceSocketClient socketClient;
        socketClient = m_KLineSubscriptionSocketClient[interval];
        if (socketClient == null)
        {
            // 不支持订阅
            Logger.LogWarning($"订阅Symbol={symbol},Interval={interval}的K线数据失败: 不支持的interval");
            return;
        }

        var subscriptionResult = socketClient.UsdFuturesApi.ExchangeData.SubscribeToKlineUpdatesAsync(symbol, interval, OnKlineUpdate).Result;
        if (subscriptionResult.Success)
        {
            lock (m_KlineSubscriptions)
            {
                if (!m_KlineSubscriptions.ContainsKey(symbol))
                {
                    m_KlineSubscriptions[symbol] = new Dictionary<KlineInterval, UpdateSubscription>();
                }
                var subscription = m_KlineSubscriptions[symbol];
                if (subscription.ContainsKey(interval))
                {
                    Logger.LogWarning($"订阅Symbol={symbol},Interval={interval}的K线数据失败: 该interval已经订阅过了");
                    return;
                }
                m_KlineSubscriptions[symbol][interval] = subscriptionResult.Data;
            }
        }
        else
        {
            Logger.LogError($"订阅Symbol={symbol},Interval={interval}的K线数据失败:{subscriptionResult.Error}");
        }
    }

    [BinanceAPICallback]
    private void OnKlineUpdate(DataEvent<IBinanceStreamKlineData> dataEvent)
    {
        if (dataEvent == null || dataEvent.Data == null || dataEvent.Symbol == null)
            return;

        var klineData = dataEvent.Data.Data;
        var symbol = dataEvent.Symbol;
        var interval = klineData.Interval;

        if (klineData == null)
            return;

        try
        {
            // 获取或创建对应symbol和interval的缓存
            lock (m_KLineCacheDataMap)
            {
                if (!m_KLineCacheDataMap.ContainsKey(symbol))
                {
                    m_KLineCacheDataMap[symbol] = new Dictionary<KlineInterval, CircularBuffer<CandlestickData>>();
                }

                var intervalMap = m_KLineCacheDataMap[symbol];
                if (!intervalMap.ContainsKey(interval))
                {
                    // 初始化缓存大小为20（满足MA20计算需求）
                    intervalMap[interval] = new CircularBuffer<CandlestickData>(20);
                }

                var kLineList = intervalMap[interval];

                // 最近一根k线的价格，是否为空 将决定均线的计算方式
                decimal? previousPrice = null;

                // 判断是新的K线还是更新当前K线
                if (kLineList.Count > 0 && kLineList[-1].DateTime == klineData.OpenTime)
                {
                    // 更新最新的K线数据
                    var kline = kLineList[-1];
                    previousPrice = kline.Close;
                    kLineList[-1] = Converter.ToCandlestickData(klineData, kLineList[-1]);
                }
                else
                {
                    // 如果没有K线数据，先添加一个
                    kLineList.Add(Converter.ToCandlestickData(klineData));
                }

                // 计算并更新均线数据
                UpdateMAValues(symbol, interval, kLineList, previousPrice);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"处理K线更新时发生错误 Symbol={symbol}, Interval={interval}: {ex.Message}");
        }
    }

    /// <summary>
    /// 更新指定symbol和interval的均线数据
    /// </summary>
    /// <param name="previousPrice">最近一根k线的更新前价格</param>
    private void UpdateMAValues(string symbol, KlineInterval interval, CircularBuffer<CandlestickData> kLineList, decimal? previousPrice)
    {
        if (kLineList == null || kLineList.Count == 0)
        {
            Logger.LogError($"计算Symbol={symbol},Interval={interval}均线数据失败: K线数据列表为空!");
            return;
        }

        if (!m_Symbol2TradeRuleMap.ContainsKey(symbol))
        {
            Logger.LogError($"计算Symbol={symbol},Interval={interval}均线数据失败: 找不到交易规则!");
            return;
        }

        // 计算精确到小数点后几位
        decimal priceStep = m_Symbol2TradeRuleMap[symbol].PriceStep;
        int precision = -(int)(Math.Log10((double)priceStep));

        // 获取最新的K线数据
        var latestKline = kLineList[-1];

        // 优化均线计算
        // 如果 previousPrice 不为空，使用之前的均线值进行动态调整
        UpdateMA(kLineList, latestKline, MAIndicatorType.MA5, 5, previousPrice, precision);
        UpdateMA(kLineList, latestKline, MAIndicatorType.MA10, 10, previousPrice, precision);
        UpdateMA(kLineList, latestKline, MAIndicatorType.MA20, 20, previousPrice, precision);
    }

    /// <summary>
    /// 更新指定均线类型的值
    /// </summary>
    /// <param name="kLineList">K线数据列表</param>
    /// <param name="latestKline">最新的K线数据</param>
    /// <param name="maType">均线类型</param>
    /// <param name="period">均线周期</param>
    /// <param name="previousPrice">最近一根K线的更新前价格</param>
    /// <param name="precision">小数点精度</param>
    private void UpdateMA(CircularBuffer<CandlestickData> kLineList, CandlestickData latestKline, int maType, int period, decimal? previousPrice, int precision)
    {
        if (kLineList.Count < period) return; // 如果K线数量不足，直接返回

        if (previousPrice.HasValue)
        {
            // 动态调整均线值
            decimal previousSum = latestKline.MA[maType] != 0
                ? latestKline.MA[maType] * period
                : GetLastNthPriceSum(kLineList, period); // 如果均线尚未计算过，重新计算
            decimal newSum = previousSum - previousPrice.Value + latestKline.Close;
            latestKline.MA[maType] = Math.Round(newSum / period, precision);
        }
        else
        {
            // 完整计算均线值
            decimal sum = GetLastNthPriceSum(kLineList, period);
            latestKline.MA[maType] = Math.Round(sum / period, precision);
        }
    }

    /// <summary>
    /// 最后N个K线的价格求和
    /// </summary>
    private decimal GetLastNthPriceSum(CircularBuffer<CandlestickData> kLineList, int n)
    {
        decimal sum = 0m;
        if(kLineList.Count >= n)
        {
            for(int i = 1; i <= n;i++)
            {
                sum += kLineList[-i].Close;
            }
        }

        return sum;
    }

    /// <summary>
    /// 在缓存中查询最近的第N根K线数据
    /// </summary>
    public CandlestickData? QueryNthRecentlyCachedCandlestickData(string symbol, KlineInterval interval, int n = 1)
    {
        if (n <= 0)
        {
            Logger.LogError("参数n不能为负值");
            return null;
        }

        lock (m_KLineCacheDataMap)
        {
            if (!m_KLineCacheDataMap.ContainsKey(symbol) ||
                !m_KLineCacheDataMap[symbol].ContainsKey(interval))
            {
                return null;
            }

            var buffer = m_KLineCacheDataMap[symbol][interval];

            if (buffer.Count < n)
                return null;

            // 使用负索引：-1表示最新，-2表示倒数第二，以此类推
            return buffer[-n];
        }
    }

    #endregion

    #region 交易数据

    public delegate void OnTradeUpdateHandlerDelegate(string symbol, TradePushData quoteTradeData);

    public OnTradeUpdateHandlerDelegate OnTradeUpdateHandler;

    /// <summary>
    /// 订阅全体 symbol 的聚合交易数据
    /// </summary>
    private async Task APISubscribeToTradeUpdatesForAllSymbols()
    {
        Task[] tasks = new Task[m_Symbols.Count];
        int index = 0;

        foreach (var symbol in m_Symbols)
        {
            tasks[index++] = Task.Run(() => APISubscribeToTradeUpdate(symbol));
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// 订阅指定 symbol 的聚合交易数据
    /// </summary>
    private void APISubscribeToTradeUpdate(string symbol)
    {
        BinanceSocketClient socketClient = m_CommonSocketClient;
        if (socketClient == null)
        {
            throw new NullReferenceException($"Unexpected null pointer for exchangeInfo \"{symbol}\"");
        }

        var subscriptionResult = socketClient.UsdFuturesApi.ExchangeData.SubscribeToAggregatedTradeUpdatesAsync(symbol, OnTradeUpdate).Result;
        if (subscriptionResult.Success)
        {
            m_TradeSubscriptions[symbol] = subscriptionResult.Data;
        }
        else
        {
            Logger.LogError($"Failed to subscribe trade update for symbol \"{symbol}\"");
        }
    }

    [BinanceAPICallback]
    private void OnTradeUpdate(DataEvent<BinanceStreamAggregatedTrade> dataEvent)
    {
        if (dataEvent.Data != null)
        {
            TradePushData quoteTradeData = new TradePushData();
            quoteTradeData.Price = dataEvent.Data.Price;
            quoteTradeData.Quantity = dataEvent.Data.Quantity;
            quoteTradeData.TradeTime = dataEvent.Data.TradeTime;
            quoteTradeData.BuyerIsMaker = dataEvent.Data.BuyerIsMaker;
        }
    }
    #endregion

    #region 标记价格
    /// <summary>
    /// 订阅全体 symbol 的标记价格数据
    /// </summary>
    private async Task APISubscribeToMarkPriceUpdatesForAllSymbols()
    {
        if (m_MarkPriceSubscription != null)
            return;

        var subscriptionResult = await m_CommonSocketClient.UsdFuturesApi.ExchangeData.SubscribeToAllMarkPriceUpdatesAsync(1000, OnMarkPriceUpdate);
        if (subscriptionResult.Success)
        {
            m_MarkPriceSubscription = subscriptionResult.Data;
        }
        else
        {
            Logger.LogError($"订阅全体Symbbol的标记价格数据失败:{subscriptionResult.Error}");
        }
    }

    [BinanceAPICallback]
    private void OnMarkPriceUpdate(DataEvent<BinanceFuturesUsdtStreamMarkPrice[]> dataEvent)
    {
        if(dataEvent != null && dataEvent.Data != null)
        {
            foreach(var markPriceData in dataEvent.Data)
            {
                if (m_TickerDataMap.ContainsKey(markPriceData.Symbol))
                {
                    var tickerData = m_TickerDataMap[markPriceData.Symbol];
                    tickerData.MarkPrice = markPriceData.MarkPrice;
                    tickerData.IndexPrice = markPriceData.IndexPrice;
                    tickerData.NextFundingTime = DateTimeUtil.ToUnixTimestampMilliseconds(markPriceData.NextFundingTime);
                    tickerData.FundingRate = markPriceData.FundingRate ?? 0.0m;
                }
            }
        }
    }
    #endregion

    #region API订阅
    /// <summary>
    /// 订阅单个 symbol 的所有数据
    /// </summary>
    protected void APISubscribeToSymbol(string symbol, KlineInterval interval)
    {
        APISubscribeToKlineUpdates(symbol, interval);
        APISubscribeToTradeUpdate(symbol);
    }

    /// <summary>
    /// 取消订阅单个 symbol 的所有数据, 一般在一个合约下架以后调用
    /// </summary>
    protected void APIUnsubscribeForSymbol(string symbol)
    {
        if (m_KlineSubscriptions.ContainsKey(symbol))
        {
            foreach (var pair in m_KlineSubscriptions[symbol])
            {
                foreach (var pair2 in m_ConcernedIntervalMap)
                {
                    BinanceSocketClient socketClient;
                    socketClient = m_KLineSubscriptionSocketClient[pair2.Key];
                    if (socketClient == null)
                    {
                        // 不在concernedInterval里不支持
                        continue;
                    }
                    socketClient.UsdFuturesApi.UnsubscribeAsync(pair.Value);
                }
            }
            m_KlineSubscriptions.Remove(symbol,out var _);
        }

        if (m_TradeSubscriptions.ContainsKey(symbol))
        {
            m_CommonSocketClient.UsdFuturesApi.UnsubscribeAsync(m_TradeSubscriptions[symbol]);
            m_TradeSubscriptions.Remove(symbol, out var _);
        }
    }

    /// <summary>
    /// 查询服务器时间
    /// </summary>
    /// <returns></returns>
    public DateTime GetAPIServerDateTime()
    {
        return m_ServerTime;
    }

    /// <summary>
    /// 查询交易规则
    /// </summary>
    public SymbolTradeRule? QueryTradeRule(string symbol)
    {
        m_Symbol2TradeRuleMap.TryGetValue(symbol, out var rule);
        return rule;
    }
    #endregion
}
