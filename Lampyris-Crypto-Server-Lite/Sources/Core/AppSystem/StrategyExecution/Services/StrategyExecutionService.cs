using Lampyris.CSharp.Common;

namespace Lampyris.Crypto.Server.Lite;

[Component]
public class StrategyExecutionService:ILifecycle
{
    [Autowired]
    private WebSocketService m_WebSocketService;

    [Autowired]
    private TradingService m_TradingService;

    [Autowired]
    private SerializationManager m_SerializationManager;

    [Autowired]
    private QuoteService m_QuoteService;

    /// <summary>
    /// 执行数据，需要序列化保存
    /// </summary>
    private StrategyExecutionServiceData m_Data = new StrategyExecutionServiceData();

    public override void OnStart()
    {
        m_Data = m_SerializationManager.TryDeserializeObjectFromFile<StrategyExecutionServiceData>() ?? new StrategyExecutionServiceData();
    }

    public void DailyReset()
    {
        // m_Symbol2ContextMap.Clear();
    }

    /// <summary>
    /// 外部调用，传入所有symbol列表(按照涨幅降序排序)
    /// </summary>
    /// <param name="tickerDataList"></param>
    public void TickQuoteList(IEnumerable<TickerData> tickerDataList)
    {
        long currentTimestamp = DateTimeUtil.GetCurrentTimestamp();
        DateTime currentMinute = DateTimeUtil.GetCurrentMinute();

        // TODO:监测比特币BTC的涨跌幅，涨速/跌速，以应对BTC穿/插针的时候
        TickerData? btcTickerData = m_QuoteService.GetTickerData("BTCUSDT");
        CandlestickData? btcKLine = m_QuoteService.QueryNthRecentlyCachedCandlestickData("BTCUSDT", Binance.Net.Enums.KlineInterval.OneMinute);

        if (currentTimestamp - m_Data.LastBTCSuddenlyActiveTimestamp >= 60 * 60 * 1000)
        {
            if (btcKLine != null && btcTickerData != null)
            {
                if (btcTickerData.RiseSpeed >= 1.5m || btcKLine.ChangePerc >= 1.0m)
                {
                    m_Data.LastBTCSuddenlyActiveTimestamp = currentTimestamp;
                    m_WebSocketService.SendClientMessage("[策略提示]BTCUSDT", ClientMessageType.STRATEGY_POSITION_PHASED_ACHIEVED, "BTCUSDT_12");
                }
                else if (btcTickerData.RiseSpeed <= -1.5m || btcKLine.ChangePerc <= -1.0m)
                {
                    m_Data.LastBTCSuddenlyActiveTimestamp = currentTimestamp;
                    m_WebSocketService.SendClientMessage("[策略提示]BTCUSDT", ClientMessageType.STRATEGY_POSITION_PHASED_ACHIEVED, "BTCUSDT_12");

                }
            }
        }
        
        if (!m_Data.IsBTCHighPercToday)
        {
            if (btcTickerData != null)
            {
                if (btcTickerData.ChangePerc >= 3m)
                {
                    m_Data.IsBTCHighPercToday = true;
                    m_WebSocketService.SendClientMessage("[策略提示]BTCUSDT", ClientMessageType.STRATEGY_POSITION_PHASED_ACHIEVED, "BTCUSDT_12");
                }
                else if (btcTickerData.ChangePerc <= 3m)
                {
                    m_Data.IsBTCHighPercToday = true;
                    m_WebSocketService.SendClientMessage("[策略提示]BTCUSDT", ClientMessageType.STRATEGY_POSITION_PHASED_ACHIEVED, "BTCUSDT_12");
                }
            }
        }

        int rank = 1;
        foreach(TickerData ticker in tickerDataList)
        {
            string symbol = ticker.Symbol;
            if(!string.IsNullOrEmpty(symbol) && !m_Data.IsHighVolatitle(symbol))
            {
                // 历史一个小时波动率监测，太大的波动率会使得阈值失效，建议是直接不操作
                if(!TestHighVolatile(symbol))
                {
                    m_Data.RecordHighVolatile(symbol);
                    continue;
                }

                // 穿针插针嫌疑检测?

                // 下架公告相关标的（只可空）

                // 做多
                if(ticker.ChangePerc >= m_Data.TriggerPerc)
                {
                    var dt = m_Data.GetTriggeredDateTime(StrategyTriggerReason.FIXED_PERC_LONG, symbol);
                    if(dt == null)
                    {
                        // 记录触发
                        m_Data.SetTriggeredDateTime(StrategyTriggerReason.FIXED_PERC_LONG, symbol, currentMinute);

                        // 槽位占用
                        var slotData = AllocateExecutionSlot(symbol, StrategyExecutionSlotType.LONG, currentTimestamp);
                        if (slotData == null)
                        {
                            return;
                        }

                        decimal targetPrice = ticker.Open * (1 + (m_Data.TriggerPerc + m_Data.LimitOrderBuyOffsetPerc) / 100);
                        decimal maxQuantity = m_TradingService.CalculateBuyableQuantity(symbol, targetPrice);
                        // 产生订单（方案有待考察，市价单可能造成较大的价格滑点，现价的限价单可能买不进去，需要加一个偏移）

                        var orderInfo = new OrderInfo()
                        {
                            Symbol = symbol,
                            Side = Protocol.Trading.OrderSide.Buy,
                            Price = ticker.Open * (1 + (m_Data.TriggerPerc + m_Data.LimitOrderBuyOffsetPerc) / 100),
                            OrderType = Protocol.Trading.OrderType.Limit,
                            PositionSide = Protocol.Trading.PositionSide.Long,
                            Quantity = maxQuantity,
                            TifType = Protocol.Trading.TimeInForceType.Fok, // 不能全部成交即撤
                        };

                        m_TradingService.PlaceOrderAsync(orderInfo, 
                        (long orderId) => 
                        {
                            Logger.LogInfo($"对合约{symbol}触发做多订单创建:\"{orderInfo}\"");
                        });
                    }
                }
                // 做空
                else if(ticker.ChangePerc <= -m_Data.TriggerPerc)
                {
                    var dt = m_Data.GetTriggeredDateTime(StrategyTriggerReason.FIXED_PERC_SHORT, symbol);
                    if (dt == null)
                    {
                        // 记录触发
                        m_Data.SetTriggeredDateTime(StrategyTriggerReason.FIXED_PERC_SHORT, symbol, DateTime.UtcNow);

                        // 槽位占用
                        var slotData = AllocateExecutionSlot(symbol, StrategyExecutionSlotType.SHORT, currentTimestamp);
                        if (slotData == null)
                        {
                            return;
                        }

                        // 产生订单
                        decimal targetPrice = ticker.Open * (1 + (m_Data.TriggerPerc - m_Data.LimitOrderBuyOffsetPerc) / 100);
                        decimal maxQuantity = m_TradingService.CalculateBuyableQuantity(symbol, ticker.Price);

                        // 产生订单（方案有待考察，市价单可能造成较大的价格滑点，现价的限价单可能买不进去，需要加一个偏移）
                        var orderInfo = new OrderInfo()
                        {
                            Symbol = symbol,
                            Side = Protocol.Trading.OrderSide.Buy,
                            Price = ticker.Open * (1 + (m_Data.TriggerPerc + m_Data.LimitOrderBuyOffsetPerc) / 100),
                            OrderType = Protocol.Trading.OrderType.Limit,
                            PositionSide = Protocol.Trading.PositionSide.Long,
                            Quantity = maxQuantity,
                            TifType = Protocol.Trading.TimeInForceType.Fok, // 不能全部成交即撤
                        };

                        m_TradingService.PlaceOrderAsync(orderInfo,
                        (long orderId) =>
                        {
                            Logger.LogInfo($"对合约{symbol}触发做空订单创建:\"{orderInfo}\"");
                        });
                    }
                }
            }

            // 记录最佳排名
            if(!m_Data.SymbolBestRankMap.ContainsKey(symbol))
            {
                m_Data.SymbolBestRankMap[symbol] = 99999;
            }

            m_Data.SymbolBestRankMap[symbol] = Math.Min(m_Data.SymbolBestRankMap[symbol], rank);
            rank++;
        }

        // 监测动态止盈止损条件
        // foreach(var symbol in m_Data.GetSlot)
        // 监测间隔时间新高/新低（只针对当日最高排名为第一名/最后一名的合约,涨幅>=20%,跌幅>=20%的）

        // 高涨速/跌速 提示(方便做反向操作)

        // 更新+排序Slot状态（如果一个槽位对应的持仓数量为0，则槽位得回收）

        // 阶段性涨幅/跌幅给出提示

        // 手动条件单监测
        m_WebSocketService.SendClientMessage("[策略提示]BTCUSDT", ClientMessageType.STRATEGY_POSITION_PHASED_ACHIEVED, "BTCUSDT_12");
    }

    /// <summary>
    /// 评估分数，最低的将被新触发的替换掉
    /// </summary>
    /// <returns></returns>
    private int EvaluateSlotScore(StrategyExecutionSlotData slotData, TickerData tickerData, StrategyExecutionParamInfo paramInfo, long currentTimestamp)
    {
        var symbol = slotData.Symbol;
        var exeuctionInfo = m_Data.GetOrCreateExecutionInfo(symbol);

        // 最小持仓时间判定
        var positionMinutes = DateTimeUtil.GetMinuteDiff(currentTimestamp, slotData.TriggeredTimestamp);
        if (positionMinutes < paramInfo.MinHoldingMinutes)
        {
            return int.MaxValue;
        }

        // 从持仓信息得到收益率
        if (!m_TradingService.PositionInfoMap.TryGetValue(symbol, out var positionInfo))
        {
            return int.MinValue;
        }

        decimal pnlPerc = positionInfo.GetPnLPerc(tickerData.Price);

        // 计算达成的阶段
        for (int i = 0; i < m_Data.PhasedPercList.Count; i++)
        {

        }
        return 0;
    }

    /// <summary>
    /// 分配执行槽位
    /// </summary>
    private StrategyExecutionSlotData? AllocateExecutionSlot(string symbol, StrategyExecutionSlotType slotType, long currentTimestamp)
    {
        // 记录了触发的前提下 拒绝开仓
        if (m_Data.ShouldOpenSlotPosition)
        {
            return null;
        }

        if (!m_Data.SlotDataMap.ContainsKey(slotType))
        {
            Logger.LogError($"不包含ExecutionSlotType{slotType},这不科学");
            return null;
        }

        var slotList = m_Data.SlotDataMap[slotType];

        // 槽位被占满, 就应该尝试借用
        if (m_Data.GetExecutingSlotCount(slotType) >= m_Data.GetSlotMaxCount(slotType))
        {
            // 得到相反的槽位类型
            var oppositeSlotType = slotType == StrategyExecutionSlotType.SHORT ?
                                   StrategyExecutionSlotType.LONG : StrategyExecutionSlotType.SHORT;

            // 可以借用
            if(m_Data.GetExecutingSlotCount(oppositeSlotType) < m_Data.GetSlotMaxCount(oppositeSlotType))
            {
            }
            // 找到被借用的
            // else if()
        }
        else
        {
            var slotData = slotList.Last();
            if (slotData != null)
            {
            }
        }

        return null;
    }

    /// <summary>
    /// 中断执行(清除槽位数据并一键清仓)
    /// </summary>
    /// <param name="symbol"></param>
    public void InterruptExecution(string symbol)
    {
        // 找到槽位数据
        m_Data.FindSlotDataBySymbol(symbol);

        // 找到持仓数据
    }

    /// <summary>
    /// 读取行情中的历史K线数据，测试是否是高波动性的合约
    /// </summary>
    /// <param name="symbol"></param>
    private bool TestHighVolatile(string symbol)
    {
        return false;
    }

    /// <summary>
    /// 读取行情中的历史K线数据，测试1分钟成交额级别（不满足阈值的不能买）
    /// </summary>
    /// <param name="symbol"></param>
    private bool TestVolume()
    {
        return false;
    }

    #region 策略查询
    public void QueryExectionHistory(string symbol)
    {
        var todayExectuionInfo = m_Data.CurrentDayExecutionInfo;
        var perSymbolExecutionInfo = todayExectuionInfo.PerSymbolExecutionMap;
        if(perSymbolExecutionInfo.ContainsKey(symbol))
        {
            var strategyExecutionInfo = perSymbolExecutionInfo[symbol].ExecutionActionInfos;
        }
    }

    public void QueryExectionLog(int strategyUId)
    {
        throw new NotImplementedException();
    }
    #endregion
}