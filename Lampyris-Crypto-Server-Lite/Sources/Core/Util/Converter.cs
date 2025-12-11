using Binance.Net.Enums;
using Binance.Net.Interfaces;
using Binance.Net.Objects.Models.Futures;
using Binance.Net.Objects.Models.Futures.Socket;
using Lampyris.Crypto.Protocol.Trading;
using Lampyris.CSharp.Common;
using OrderSide = Lampyris.Crypto.Protocol.Trading.OrderSide;
using OrderStatus = Lampyris.Crypto.Protocol.Trading.OrderStatus;
using PositionSide = Lampyris.Crypto.Protocol.Trading.PositionSide;

namespace Lampyris.Crypto.Server.Lite;

public static class Converter
{
    private static readonly BidirectionalDictionary<Binance.Net.Enums.OrderStatus, OrderStatus> ms_OrderStatusConvertMap = new()
    {
        { Binance.Net.Enums.OrderStatus.New, OrderStatus.New },
        { Binance.Net.Enums.OrderStatus.PartiallyFilled, OrderStatus.PartiallyFilled },
        { Binance.Net.Enums.OrderStatus.Filled, OrderStatus.Filled },
        { Binance.Net.Enums.OrderStatus.Canceled, OrderStatus.Canceled },
        { Binance.Net.Enums.OrderStatus.Rejected, OrderStatus.Rejected },
        { Binance.Net.Enums.OrderStatus.Expired, OrderStatus.Expired },
        { Binance.Net.Enums.OrderStatus.ExpiredInMatch, OrderStatus.ExpiredInMatch }
    };

    public static OrderStatus ConvertOrderStatus(Binance.Net.Enums.OrderStatus orderStatus)
    {
        return ms_OrderStatusConvertMap.TryGetValue(orderStatus, out var resultOrderStatus) ? resultOrderStatus :
            throw new ArgumentException($"Unsupported Binance OrderStatus: {orderStatus}");
    }


    private static readonly BidirectionalDictionary<Binance.Net.Enums.PositionSide, PositionSide> ms_PositionSideConvertMap = new()
    {
        { Binance.Net.Enums.PositionSide.Long, PositionSide.Long },
        { Binance.Net.Enums.PositionSide.Short, PositionSide.Short },
    };

    public static PositionSide ConvertPositionSide(Binance.Net.Enums.PositionSide positionSide)
    {
        return ms_PositionSideConvertMap.TryGetValue(positionSide, out var resultPositionSide) ? resultPositionSide :
            throw new ArgumentException($"Unsupported Binance PositionSide: {resultPositionSide}");
    }

    public static Binance.Net.Enums.PositionSide ConvertPositionSide(PositionSide positionSide)
    {
        return ms_PositionSideConvertMap.TryGetValue(positionSide, out var resultPositionSide) ? resultPositionSide :
            throw new ArgumentException($"Unsupported PositionSide: {resultPositionSide}");
    }

    private static readonly BidirectionalDictionary<Binance.Net.Enums.OrderSide, OrderSide> ms_OrderSideConvertMap = new()
    {
        { Binance.Net.Enums.OrderSide.Buy, OrderSide.Buy },
        { Binance.Net.Enums.OrderSide.Sell, OrderSide.Sell },
    };

    public static OrderSide ConvertOrderSide(Binance.Net.Enums.OrderSide orderSide)
    {
        return ms_OrderSideConvertMap.TryGetValue(orderSide, out var resultOrderSide) ? resultOrderSide :
            throw new ArgumentException($"Unsupported Binance OrderSide: {resultOrderSide}");
    }
    public static Binance.Net.Enums.OrderSide ConvertOrderSide(OrderSide orderSide)
    {
        return ms_OrderSideConvertMap.TryGetValue(orderSide, out var resultOrderSide) ? resultOrderSide :
            throw new ArgumentException($"Unsupported OrderSide: {resultOrderSide}");
    }

    private static readonly BidirectionalDictionary<TimeInForce, TimeInForceType> ms_TimeInForceConvertMap = new()
    {
        { TimeInForce.GoodTillCanceled, TimeInForceType.Gtc },
        { TimeInForce.GoodTillDate, TimeInForceType.Gtd },
        { TimeInForce.FillOrKill, TimeInForceType.Fok },
        { TimeInForce.ImmediateOrCancel , TimeInForceType.Ioc },
    };

    public static TimeInForceType ConvertTimeInForce(TimeInForce tif)
    {
        return ms_TimeInForceConvertMap.TryGetValue(tif, out var resultTif) ? resultTif :
            throw new ArgumentException($"Unsupported Binance TimeInForce: {resultTif}");
    }

    public static TimeInForce ConvertTimeInForce(TimeInForceType tif)
    {
        return ms_TimeInForceConvertMap.TryGetValue(tif, out var resultTif) ? resultTif :
            throw new ArgumentException($"Unsupported TimeInForce: {resultTif}");
    }

    private static readonly Dictionary<FuturesOrderType, OrderType> ms_CommonOrderTypeConvertMap = new()
    {
        { FuturesOrderType.Limit, OrderType.Limit },
        { FuturesOrderType.Market, OrderType.Market },
        { FuturesOrderType.Stop, OrderType.StopLoss },
        { FuturesOrderType.StopMarket, OrderType.StopLossMarket },
        { FuturesOrderType.TakeProfit, OrderType.TakeProfit },
        { FuturesOrderType.TakeProfitMarket, OrderType.TakeProfitMarket },
        { FuturesOrderType.TrailingStopMarket, OrderType.TrailingStopMarket },
    };

    public static OrderType ConvertOrderType(Binance.Net.Enums.FuturesOrderType orderType)
    {
        return ms_CommonOrderTypeConvertMap.TryGetValue(orderType, out var resultOrderType) ? resultOrderType :
            throw new ArgumentException($"Unsupported Binance OrderType: {resultOrderType}");
    }

    private static readonly Dictionary<OrderType, FuturesOrderType> ms_BinanceOrderTypeConvertMap = new()
    {
        { OrderType.Limit, FuturesOrderType.Limit },
        { OrderType.Market, FuturesOrderType.Market },
        { OrderType.StopLoss, FuturesOrderType.Stop },
        { OrderType.StopLossMarket, FuturesOrderType.StopMarket },
        { OrderType.TakeProfit, FuturesOrderType.TakeProfit },
        { OrderType.TakeProfitMarket,FuturesOrderType.TakeProfitMarket },
        { OrderType.TrailingStopMarket, FuturesOrderType.TrailingStopMarket },
    };

    public static FuturesOrderType ConvertOrderType(OrderType orderType)
    {
        return ms_BinanceOrderTypeConvertMap.TryGetValue(orderType, out var resultOrderType) ? resultOrderType :
            throw new ArgumentException($"Unsupported OrderType: {resultOrderType}");
    }

    public static OrderStatusInfo ToOrderStatusInfo(BinanceFuturesStreamOrderUpdateData orderUpdateData, OrderStatusInfo? allocated = null)
    {
        return new OrderStatusInfo
        {
            OrderId = (int)orderUpdateData.OrderId,
            OrderInfo = new OrderInfo
            {
                OrderId = orderUpdateData.OrderId,
                Symbol = orderUpdateData.Symbol,
                Side = ConvertOrderSide(orderUpdateData.Side),
                OrderType = ConvertOrderType(orderUpdateData.Type),
                Quantity = orderUpdateData.Quantity,
                CashQuantity = orderUpdateData.Quantity * orderUpdateData.Price,
                Price = orderUpdateData.Price,
                TifType = ConvertTimeInForce(orderUpdateData.TimeInForce),
                GoodTillDate = orderUpdateData.GoodTillDate.HasValue ? orderUpdateData.GoodTillDate.Value.Ticks : 0,
                ReduceOnly = orderUpdateData.IsReduce,
                CreatedTime = orderUpdateData.UpdateTime.Ticks
            },
            Status = ConvertOrderStatus(orderUpdateData.Status),
            FilledQuantity = orderUpdateData.AccumulatedQuantityOfFilledTrades,
            AvgFilledPrice = orderUpdateData.AveragePrice
        };
    }

    public static OrderStatusInfo ToOrderStatusInfo(BinanceUsdFuturesOrder binanceOrder, OrderStatusInfo? existingInfo = null)
    {
        // 转换 BinanceFuturesOrder 为 OrderInfo
        var orderInfo = existingInfo == null ? new OrderInfo() : existingInfo.OrderInfo;

        orderInfo.OrderId = binanceOrder.Id; // Binance订单ID
        orderInfo.Symbol = binanceOrder.Symbol; // 交易对，例如 BTCUSDT
        orderInfo.Side = ConvertOrderSide(binanceOrder.Side); // 订单方向
        orderInfo.OrderType = ConvertOrderType(binanceOrder.Type); // 订单类型
        orderInfo.Quantity = binanceOrder.Quantity; // 订单数量（以标的为单位）
        orderInfo.CashQuantity = binanceOrder.QuoteQuantityFilled ?? 0; // 订单数量（以USDT为单位）
        orderInfo.Price = binanceOrder.Price; // 订单价格
        orderInfo.TifType = ConvertTimeInForce(binanceOrder.TimeInForce); // 订单有效方式
        orderInfo.ReduceOnly = binanceOrder.ReduceOnly; // 是否只减仓
        orderInfo.CreatedTime = DateTimeUtil.ToUnixTimestampMilliseconds(binanceOrder.CreateTime);// 创建时间

        // 转换为 OrderStatusInfo
        OrderStatusInfo orderStatusInfo = existingInfo == null ? new OrderStatusInfo() : existingInfo;
        orderStatusInfo.OrderId = binanceOrder.Id; // 订单ID
        orderStatusInfo.OrderInfo = orderInfo; // 嵌套的订单基本信息
        orderStatusInfo.Status = ConvertOrderStatus(binanceOrder.Status); // 订单状态
        orderStatusInfo.FilledQuantity = binanceOrder.QuantityFilled; // 已成交数量
        orderStatusInfo.AvgFilledPrice = binanceOrder.AveragePrice; // 成交均价

        if (binanceOrder.GoodTillDate != null)
        {
            orderInfo.GoodTillDate = DateTimeUtil.ToUnixTimestampMilliseconds(binanceOrder.GoodTillDate.Value); // 订单的自动取消时间
        }

        return orderStatusInfo;
    }

    public static PositionInfo ToPositionInfo(BinancePositionV3 source)
    {
        return new PositionInfo
        {
            Symbol = source.Symbol,
            PositionSide = Converter.ConvertPositionSide(source.PositionSide),
            Quantity = source.PositionAmt,
            UnrealizedPnL = source.UnrealizedProfit,
            RealizedPnL = 0.0m, // 需要从AccountInfo里拿取
            InitialMargin = source.InitialMargin,
            MaintenanceMargin = source.MaintenanceMargin,
            CostPrice = source.EntryPrice,
            MarkPrice = source.MarkPrice,
            LiquidationPrice = source.LiquidationPrice,
            AutoDeleveragingLevel = Convert.ToInt32(source.Adl),
            UpdateTime = source.UpdateTime ?? DateTime.MinValue // 如果 UpdateTime 为 null，则设置为默认值
        };
    }

    public static PositionUpdateInfo ToPositionUpdateInfo(BinanceFuturesStreamPosition apiPosition)
    {
        return new PositionUpdateInfo
        {
            Symbol = apiPosition.Symbol,
            Quantity = apiPosition.Quantity,
            UnrealizedPnL = apiPosition.UnrealizedPnl,
            RealizedPnL = apiPosition.RealizedPnl,
            CostPrice = apiPosition.EntryPrice,
            UpdateTime = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 将 IBinance24HPrice 转换为 QuoteTickerData
    /// </summary>
    /// <param name="binance24HPrice">IBinance24HPrice 实例</param>
    /// <returns>转换后的 QuoteTickerData 实例</returns>
    public static TickerData ToTickerData(long timestamp, IBinance24HPrice binance24HPrice, TickerData? tickerData)
    {
        if (binance24HPrice == null)
            throw new ArgumentNullException(nameof(binance24HPrice));

        tickerData = (tickerData != null ? tickerData : new TickerData());

        tickerData.Symbol = binance24HPrice.Symbol;
        tickerData.Price = binance24HPrice.LastPrice;
        tickerData.LastSize = binance24HPrice.LastQuantity;
        tickerData.High24H = binance24HPrice.HighPrice;
        tickerData.Low24H = binance24HPrice.LowPrice;
        tickerData.Volume24H = binance24HPrice.Volume;
        tickerData.QuoteVolume24H = binance24HPrice.QuoteVolume;
        tickerData.Timestamp = timestamp; // 当前时间戳
        tickerData.ChangePerc = binance24HPrice.PriceChangePercent;
        tickerData.Change = binance24HPrice.PriceChange;

        return tickerData;
    }

    public static CandlestickData ToCandlestickData(IBinanceKline klineData, CandlestickData? allocated = null)
    {
        allocated = allocated ?? new CandlestickData();
        allocated.DateTime = klineData.OpenTime;
        allocated.Open = klineData.OpenPrice;
        allocated.High = klineData.HighPrice;
        allocated.Low = klineData.LowPrice;
        allocated.Close = klineData.ClosePrice;
        allocated.Volume = klineData.Volume;
        allocated.Currency = klineData.QuoteVolume;

        return allocated;
    }

    public static SymbolTradeRule ToSymbolTradeRule(BinanceFuturesSymbol symbol, SymbolTradeRule? allocated = null)
    {
        allocated = allocated ?? new SymbolTradeRule();
        var priceFilter = symbol.PriceFilter;
        var lotSizeFilter = symbol.LotSizeFilter;
        var minNotionalFilter = symbol.MinNotionalFilter;

        var tradeRule = allocated != null ? allocated : new SymbolTradeRule();
        tradeRule.Symbol = symbol.Name;
        tradeRule.MinPrice = priceFilter?.MinPrice ?? 0;
        tradeRule.MaxPrice = priceFilter?.MaxPrice ?? 0;
        tradeRule.PriceStep = priceFilter?.TickSize ?? 0;
        tradeRule.MinQuantity = lotSizeFilter?.MinQuantity ?? 0;
        tradeRule.MaxQuantity = lotSizeFilter?.MaxQuantity ?? 0;
        tradeRule.QuantityStep = lotSizeFilter?.StepSize ?? 0;
        tradeRule.MinNotional = minNotionalFilter?.MinNotional ?? 0;
        tradeRule.OnBoardTimestamp = DateTimeUtil.ToUnixTimestampMilliseconds(symbol.ListingDate);

        return tradeRule;
    }
}
