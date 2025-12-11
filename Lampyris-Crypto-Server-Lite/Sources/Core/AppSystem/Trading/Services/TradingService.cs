namespace Lampyris.Crypto.Server.Lite;

using Binance.Net.Clients;
using Binance.Net.Objects.Models.Futures;
using Binance.Net.Objects.Models.Futures.Socket;
using CryptoExchange.Net.Authentication;
using Lampyris.CSharp.Common;
using System.Collections;
using System.Text;

[Component]
public class TradingService:ILifecycle
{
    [Autowired]
    private QuoteService m_QuoteService;

    // 当前持仓信息(key = Symbol)
    public Dictionary<string, PositionInfo> PositionInfoMap = new();

    // 当前订单信息(key = Symbol, value = Dictionary<OrderId,OrderStatusInfo>)
    public Dictionary<string, Dictionary<long, OrderStatusInfo>> OpenOrderInfoMap = new();

    // 历史订单信息(key = Symbol, value = 订单状态列表, 按照时间由近到久排序)
    public Dictionary<string, List<OrderStatusInfo>> HistoricalOrderInfoList = new();

    // 杠杆设置信息(key = Symbol, value = 杠杆倍数)
    public Dictionary<string, int> LeverageSettingsDataMap = new Dictionary<string, int>();

    // 杠杆分层信息
    public Dictionary<string, List<LeverageBracketInfo>> LeverageBracketDataMap = new Dictionary<string, List<LeverageBracketInfo>>();

    // 每个symbol在当前杠杆倍数设定下对应的最大可开仓名义价值
    protected Dictionary<string, decimal> SymbolMaxNotionalMap = new Dictionary<string, decimal>();

    // 每个symbol的历史仓位记录，仅仅在清仓后被记录
    protected Dictionary<string, List<HistoricalPositionInfo>> SymbolHistoricalPositionDataMap = new Dictionary<string, List<HistoricalPositionInfo>>();

    // Binance客户端对象，为什么不和QuoteService的公用是因为这里会需要填充ApiKey等信息
    private BinanceRestClient m_RestClient;
    private BinanceSocketClient m_SocketClient;

    // USDT资产信息
    private BinanceFuturesStreamBalance? m_USDTAssetInfo;

    public override void OnStart()
    {
        string apiKey = "mXxjZ1Ud0xz1LIpGim7Vs22Y20Rwfyjb2JuARCpwUlezAi9u5n0BoT85m4LWRdAL";
        string apiSecret = "iuRmT6nvn7UdALNquOyWQT35dMtFBe5QqPdf40ZgmXNstQqE1zR0Ff7oo1Kham59";

        // 初始化REST客户端
        m_RestClient = new BinanceRestClient(options =>
        { 
            options.AutoTimestamp = true;
            options.RateLimiterEnabled = true;
            options.RateLimitingBehaviour = CryptoExchange.Net.Objects.RateLimitingBehaviour.Wait;
            options.ApiCredentials = new ApiCredentials(apiKey, apiSecret);
        });

        // 初始化Socket客户端
        m_SocketClient = new BinanceSocketClient(options =>
        {
            options.RateLimiterEnabled = true;
            options.RateLimitingBehaviour = CryptoExchange.Net.Objects.RateLimitingBehaviour.Wait;
            options.ApiCredentials = new ApiCredentials(apiKey, apiSecret);
            options.ReconnectInterval = TimeSpan.FromSeconds(1);  // 重连间隔
            options.SocketNoDataTimeout = TimeSpan.FromSeconds(5); // 无数据超时
        });

        APISubscribeToUserDataUpdates();
        APIUpdateAllLeverageBracket();
        APIUpdateAllOpenOrders();
        APIUpdateAllLeverageSettings();
    }

    public void APISubscribeToUserDataUpdates()
    {
        // 获取 Listen Key
        var listenKeyResult = m_RestClient.UsdFuturesApi.Account.StartUserStreamAsync().Result;
        if (!listenKeyResult.Success)
        {
            Logger.LogInfo($"Failed to obtain Listen Key：{listenKeyResult.Error?.Message}");
            return;
        }

        // 订阅用户数据更新
        var subscriptionResult = m_SocketClient.UsdFuturesApi.Account.SubscribeToUserDataUpdatesAsync(
            listenKeyResult.Data,
            // 杠杆更新事件回调
            onLeverageUpdate: leverageUpdate =>
            {
                if (leverageUpdate.Data != null && leverageUpdate.Data.LeverageUpdateData != null)
                {
                    var data = leverageUpdate.Data.LeverageUpdateData;
                    if (data.Symbol != null)
                    {
                        LeverageSettingsDataMap[data.Symbol] = data.Leverage;
                        Logger.LogInfo($"Lererage of symbol \"{data.Symbol}\" changed to {data.Leverage}");
                    }
                }
            },
            // 保证金追加事件回调
            onMarginUpdate: marginUpdate =>
            {
                if (marginUpdate.Data != null)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("!!!!!!! MARGIN CALL !!!!!!!");
                    foreach (var position in marginUpdate.Data.Positions)
                    {
                        sb.Append($"Symbol {position.Symbol}, Side {position.PositionSide.ToString()}, Unrealized Pnl = {position.UnrealizedPnl};");
                    }
                    sb.Append("\n");
                    Logger.LogWarning(sb.ToString());
                }
            },
            // 账户更新事件回调
            // 仅当账户信息有变动时(包括资金、仓位、保证金模式等发生变化)，才会推送此事件；
            // 订单状态变化没有引起账户和持仓变化的，不会推送此事件；
            // position 信息：仅当 symbol 仓位有变动时推送。
            onAccountUpdate: accountUpdate =>
            {
                if (accountUpdate.Data != null)
                {
                    // 资产信息仅仅更新USDT
                    foreach (var balance in accountUpdate.Data.UpdateData.Balances)
                    {
                        if (balance != null && balance.Asset == "USDT")
                        {
                            Logger.LogInfo($"总资产 = {balance.Asset} USDT");
                            Logger.LogInfo($"可用资产 = {balance.WalletBalance} USDT");

                            m_USDTAssetInfo = balance;
                            break;
                        }
                    }

                    // 更新持仓信息
                    APIUpdateAllPositionInfo();
                }
            },
            // 订单更新事件回调
            onOrderUpdate: orderUpdate =>
            {
                if (orderUpdate.Data != null && orderUpdate.Data.UpdateData != null)
                {
                    var rawOrderStatusData = orderUpdate.Data.UpdateData;
                    OrderStatusInfo orderStatusInfo = Converter.ToOrderStatusInfo(rawOrderStatusData);
                    if(!OpenOrderInfoMap.ContainsKey(rawOrderStatusData.Symbol))
                    {
                        OpenOrderInfoMap[rawOrderStatusData.Symbol] = new Dictionary<long, OrderStatusInfo>();
                    }
                    OpenOrderInfoMap[rawOrderStatusData.Symbol][orderStatusInfo.OrderId] = orderStatusInfo;
                }
            },
            // 交易更新事件回调
            onTradeUpdate: null,
            // ListenKey过期事件回调
            onListenKeyExpired: listenKeyExpired =>
            {
                Logger.LogInfo($"ListenKey expired, try to re-start user stream.");
                // 在这里处理ListenKey过期逻辑
                var listenKeyResult = m_RestClient.UsdFuturesApi.Account.StartUserStreamAsync().Result;
                if (!listenKeyResult.Success)
                {
                    Logger.LogError($"Failed to re-start user stream, reason：{listenKeyResult.Error?.Message}");
                }
                else
                {
                    Logger.LogInfo($"User stream re-start successfully");
                }
            },
            // 策略更新事件回调
            onStrategyUpdate: null,
            // 网格更新事件回调
            onGridUpdate: null,
            // 条件订单触发拒绝事件回调
            onConditionalOrderTriggerRejectUpdate: conditionalOrderTriggerRejectUpdate =>
            {
                if (conditionalOrderTriggerRejectUpdate.Data != null && conditionalOrderTriggerRejectUpdate.Data.RejectInfo != null)
                {
                    var data = conditionalOrderTriggerRejectUpdate.Data.RejectInfo;
                    Logger.LogWarning($"Conditional order rejected, orderId = : {data.OrderId}, reason = {data.Reason}");
                }
            },
            ct: CancellationToken.None
        ).Result;

        // 检查订阅是否成功
        if (!subscriptionResult.Success)
        {
            Logger.LogInfo($"Failed to re-start user stream, reason: {subscriptionResult.Error}");
        }
        else
        {
            Logger.LogInfo("User stream start successfully!");
        }
    }

    /// <summary>
    /// 更新杠杆设置
    /// </summary>
    private void APIUpdateAllLeverageSettings()
    {
        LeverageSettingsDataMap.Clear();
        var result = m_RestClient.UsdFuturesApi.Account.GetSymbolConfigurationAsync().Result;

        if (result.Success)
        {
            foreach (var data in result.Data)
            {
                LeverageSettingsDataMap[data.Symbol] = (int)data.Leverage;
            }
        }
    }

    /// <summary>
    /// 取消订单
    /// </summary>
    /// <param name="symbol"></param>
    /// <param name="orderId"></param>
    public async Task APICancelOrderAsync(string symbol, long orderId)
    {
        var result = await m_SocketClient.UsdFuturesApi.Trading.CancelOrderAsync(
            symbol: symbol,
            orderId: orderId,
            origClientOrderId: null,
            receiveWindow: 5000,
            ct: CancellationToken.None
        );

        if (!result.Success)
        {
            Logger.LogError($"合约{symbol} ID={orderId}的订单撤单失败: {result.Error?.Message}");
        }
        else
        {
            Logger.LogInfo($"合约{symbol} ID={orderId}的订单已撤单");
        }
    }

    /// <summary>
    /// 更新所有持仓信息
    /// </summary>
    public void APIUpdateAllPositionInfo()
    {
        PositionInfoMap.Clear();

        // 请求所有持仓信息
        var apiPositionInfoReqResult = m_RestClient.UsdFuturesApi.Trading.GetPositionsAsync().Result;

        if (apiPositionInfoReqResult != null && apiPositionInfoReqResult.Success)
        {
            var apiPositionInfoList = apiPositionInfoReqResult.Data;
            foreach (var apiPositionInfo in apiPositionInfoList)
            {
                string symbol = apiPositionInfo.Symbol;
                if (!PositionInfoMap.ContainsKey(symbol))
                {
                    PositionInfoMap[symbol] = new PositionInfo();
                }
                PositionInfoMap[symbol] = Converter.ToPositionInfo(apiPositionInfo);
            }
        }
    }

    /// <summary>
    /// 更新账号的所有订单信息(高权重，一般只在程序初始化时候调用)
    /// </summary>
    public void APIUpdateAllOpenOrders()
    {
        // 重设数据
        var result = m_RestClient.UsdFuturesApi.Trading.GetOrdersAsync(null, receiveWindow: 5000, ct: CancellationToken.None).Result;

        // 获取该symbol的所有订单
        // PS:如果订单满足如下条件，不会被查询到:
        // 订单的最终状态为 CANCELED 或者 EXPIRED 并且 订单没有任何的成交记录 并且 订单生成时间 +3天<当前时间
        // 订单创建时间 +90天<当前时间
        foreach (BinanceUsdFuturesOrder binanceOrderStatusInfo in result.Data)
        {
            OrderStatusInfo orderStatusInfo = Converter.ToOrderStatusInfo(binanceOrderStatusInfo);
            if (!OpenOrderInfoMap.ContainsKey(binanceOrderStatusInfo.Symbol))
            {
                OpenOrderInfoMap[binanceOrderStatusInfo.Symbol] = new Dictionary<long, OrderStatusInfo>();
            }

            OpenOrderInfoMap[binanceOrderStatusInfo.Symbol][binanceOrderStatusInfo.Id] = orderStatusInfo;
        }
    }

    /// <summary>
    /// 修改订单
    /// </summary>
    /// <param name="orderId"></param>
    /// <param name="updatedOrderInfo"></param>
    public void APIModifyOrder(int clientUserId, long orderId, OrderInfo updatedOrderInfo)
    {
        if (updatedOrderInfo == null)
            return;

        var result = m_SocketClient.UsdFuturesApi.Trading.EditOrderAsync(
            symbol: updatedOrderInfo.Symbol,               // 交易对，例如 ETHUSDT
            side: Converter.ConvertOrderSide(updatedOrderInfo.Side),     // 订单方向
            quantity: updatedOrderInfo.Quantity,          // 修改后的订单数量
            price: updatedOrderInfo.Price,                // 修改后的订单价格（限价单需要）
            priceMatch: null,                              // PriceMatch 参数（根据需求设置）
            orderId: orderId,                              // 订单ID
            origClientOrderId: null,                       // 原始客户端订单ID（如果需要）
            receiveWindow: 5000,                           // 接收窗口时间（可根据需求调整）
            ct: CancellationToken.None                    // 取消令牌
        ).Result;

        if (!result.Success)
        {
            Logger.LogError($"Failed to modify order for symbol \"{updatedOrderInfo.Symbol} \"ID: {orderId}, reason: {result.Error?.Message}");
        }
    }


    public void PlaceOrderAsync(OrderInfo orderInfo, Action<long> callback, double autoCancelSeconds = -1)
    {
        CoroutineManager.StartCoroutine(PlaceOrderAsyncPerc(orderInfo, callback, autoCancelSeconds));
    }

    private IEnumerator PlaceOrderAsyncPerc(OrderInfo orderInfo, Action<long> callback, double autoCancelSeconds)
    {
        Task<long> taskPlaceOrder = APIPlaceOrderAsync(orderInfo);
        yield return new WaitForTask(taskPlaceOrder);
        if (callback != null)
        {
            callback(taskPlaceOrder.Result);
        }
        yield return new WaitForSeconds(autoCancelSeconds);

        if(autoCancelSeconds > 0)
        {
            Task<long> taskCancel = APIPlaceOrderAsync(orderInfo);
            yield return new WaitForTask(taskCancel);
        }
    }
    public async Task<long> APIPlaceOrderAsync(OrderInfo order)
    {
        // 调用 PlaceOrderAsync 方法
        var placeOrderTask = m_SocketClient.UsdFuturesApi.Trading.PlaceOrderAsync(
            symbol: order.Symbol,                     // 交易对，例如 BTCUSDT
            side: Converter.ConvertOrderSide(order.Side),           // 订单方向，映射到 API 的枚举值
            type: Converter.ConvertOrderType(order.OrderType),      // 订单类型，映射到 API 的枚举值
            quantity: order.Quantity,                 // 订单数量（以标的为单位）
            price: order.Price,                       // 订单价格（限价单需要）
            timeInForce: Converter.ConvertTimeInForce(order.TifType), // 订单有效方式
            reduceOnly: order.ReduceOnly,             // 是否只减仓
            stopPrice: null,                          // 停止价格
            activationPrice: null,                    // 激活价格
            callbackRate: null                        // 回调比例
        );

        var result = await placeOrderTask;
        if (result == null || !result.Success)
        {
            Logger.LogError($"订单\"{order}\"下单失败，原因:" + (result == null ? "结果为null" : result.Error));
            return -1L;
        }

        long id = result.Data.Result.Id;
        Logger.LogError($"订单\"{order}\"下单成功，订单号:" + id);
        return id;
    }

    public long APIPlaceOrder(OrderInfo order)
    {
        return APIPlaceOrderAsync(order).Result;
    }

    /// <summary>
    /// 获取并汇总指定 symbol 和订单 ID 的交易记录
    /// </summary>
    protected OrderTradeSummaryData? GetAndSummarizeTrades(string symbol, long orderId)
    {
        const int limit = 500; // 每次请求的最大记录数
        var allTrades = new List<BinanceFuturesUsdtTrade>();
        long lastId = 0; // 初始值为 0，表示从最早的记录开始

        while (true)
        {
            // 分批请求交易记录
            var tradesResult = m_RestClient.UsdFuturesApi.Trading.GetUserTradesAsync(symbol, orderId: orderId, fromId: lastId, limit: limit).Result;

            if (!tradesResult.Success)
            {
                Logger.LogError($"获取{symbol}的成交记录失败: {tradesResult.Error}");
                return null;
            }

            var trades = tradesResult.Data;
            int count = trades.Count();

            // 没有更多数据则退出循环
            if (trades == null || count <= 0)
            {
                break;
            }

            // 筛选目标订单的交易记录
            var orderTrades = trades.Where(trade => trade.OrderId == orderId).ToList();

            // 添加到总交易记录列表
            allTrades.AddRange(orderTrades);

            // 更新 lastId 为最后一条记录的 ID
            lastId = trades.Last().Id + 1;

            // 如果返回的记录数少于 limit，说明已经是最后一页
            if (count < limit)
            {
                break;
            }
        }

        // 汇总交易记录
        return SummarizeTrades(allTrades, symbol, orderId);
    }

    private OrderTradeSummaryData SummarizeTrades(List<BinanceFuturesUsdtTrade> trades, string symbol, long orderId)
    {
        return new OrderTradeSummaryData
        {
            Symbol = symbol,
            OrderId = orderId,
            TotalRealizedPnL = trades.Sum(t => t.RealizedPnl),
            TotalFee = trades.Sum(t => t.Fee),
            TotalQuantity = trades.Sum(t => t.Quantity),
            TradeCount = trades.Count
        };
    }

    /// <summary>
    /// 一键清仓某个合约的持仓
    /// </summary>
    /// <param name="symbol"></param>
    protected void APIKillPosition(string symbol)
    {
        if (PositionInfoMap.ContainsKey(symbol))
        {
            var positionInfo = PositionInfoMap[symbol];
            if(positionInfo != null)
            {
                var placeOrderTask = m_SocketClient.UsdFuturesApi.Trading.PlaceOrderAsync(
                    symbol: symbol, 
                    side: Binance.Net.Enums.OrderSide.Sell,
                    type: Binance.Net.Enums.FuturesOrderType.Market,
                    positionSide: positionInfo.PositionSide == Protocol.Trading.PositionSide.Long ?
                                  Binance.Net.Enums.PositionSide.Long : Binance.Net.Enums.PositionSide.Short,
                    quantity: positionInfo.Quantity,
                    price: null,
                    timeInForce: Binance.Net.Enums.TimeInForce.GoodTillCanceled,
                    reduceOnly: true,
                    stopPrice: null,        
                    activationPrice: null,  
                    callbackRate: null      
                );

                var result = placeOrderTask.Result;
                if (!result.Success)
                {
                    Logger.LogError($"一键清仓Symbol={symbol}失败: {result.Error}");
                }
            }
        }
    }

    /// <summary>
    /// 设置某个USDT永续合约的杠杆倍数
    /// </summary>
    /// <param name="symbol"></param>
    /// <param name="leverage"></param>
    protected void APISetLeverage(string symbol, int leverage)
    {
        var result = m_RestClient.UsdFuturesApi.Account.ChangeInitialLeverageAsync(symbol, leverage).Result;
        if (result.Success)
        {
            LeverageSettingsDataMap[symbol] = leverage;
        }
    }

    /// <summary>
    /// 更新所有USDT永续合约的的杠杆分层信息
    /// </summary>
    protected void APIUpdateAllLeverageBracket()
    {
        LeverageSettingsDataMap.Clear();
        var result = m_RestClient.UsdFuturesApi.Account.GetBracketsAsync().Result;

        if (result.Success)
        {
            foreach (var data in result.Data)
            {
                if (!LeverageBracketDataMap.ContainsKey(data.Symbol))
                {
                    LeverageBracketDataMap[data.Symbol] = new List<LeverageBracketInfo>();
                }

                var bracketDataList = LeverageBracketDataMap[data.Symbol];
                foreach (var bracket in data.Brackets)
                {
                    LeverageBracketInfo leverageBracketInfo = new LeverageBracketInfo();
                    leverageBracketInfo.Symbol = data.Symbol;
                    leverageBracketInfo.Leverage = bracket.InitialLeverage;
                    leverageBracketInfo.NotionalCap = bracket.Cap;
                    leverageBracketInfo.NotionalFloor = bracket.Floor;

                    bracketDataList.Add(leverageBracketInfo);
                }

                // 对每个杠杆分层数据，根据杠杆倍数上界的大小 从小到大进行排序
                bracketDataList.Sort((lhs, rhs) => lhs.Leverage - rhs.Leverage);
            }
        }
    }

    /// <summary>
    /// 查询指定杠杆下的杠杆分层信息
    /// </summary>
    public LeverageBracketInfo? GetLeverageBracketInfo(string symbol, int leverage)
    {
        if(LeverageBracketDataMap.TryGetValue(symbol, out var bracketInfoList))
        {
            for(int i = 0; i < bracketInfoList.Count;i++)
            {
                var bracketInfo = bracketInfoList[i];
                if(leverage <= bracketInfo.Leverage)
                {
                    return bracketInfo;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// 计算最大可买数量
    /// </summary>
    public decimal CalculateBuyableQuantity(string symbol, decimal price)
    {
        // 需要从 交易规则 中获取 数量最小变化单位值
        SymbolTradeRule? rule = m_QuoteService.QueryTradeRule(symbol);

        if (rule == null)
        {
            Logger.LogError($"计算Symbol={symbol},Price={price}的最大可买数量失败: 无法找到对应的交易规则");
            return 0.0m;
        }

        if (m_USDTAssetInfo == null)
        {
            Logger.LogError($"计算Symbol={symbol},Price={price}的最大可买数量失败: USDT资产信息为空");
            return 0.0m;
        }

        if (!LeverageSettingsDataMap.TryGetValue(symbol, out var leverage))
        {
            Logger.LogError($"计算Symbol={symbol},Price={price}的最大可买数量失败: 杠杆倍数设置获取失败");
            return 0.0m;
        }

        LeverageBracketInfo? bracketInfo = GetLeverageBracketInfo(symbol, leverage);
        if (bracketInfo == null)
        {
            Logger.LogError($"计算Symbol={symbol},Price={price}的最大可买数量失败: 杠杆分层信息获取失败");
            return 0.0m;
        }

        // 杠杆分层下的 最大可开仓名义价值(USDT)
        decimal maxNotional = bracketInfo.NotionalCap;
        // 根据钱包下的可用USDT 计算 最大可开仓名义价值
        decimal walletMaxNotional = m_USDTAssetInfo.WalletBalance * leverage;

        // 根据数量步进调整
        decimal maxQuantity = Math.Min(maxNotional,walletMaxNotional) / price;
        maxQuantity = MathUtil.LowerByStep(maxQuantity, rule.QuantityStep);

        return maxQuantity;
    }
}
