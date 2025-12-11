namespace Lampyris.Crypto.Server.Lite;

using Lampyris.Crypto.Protocol.Trading;
using Newtonsoft.Json;
using System;

public class OrderInfo
{
    // 订单ID
    public long OrderId;

    // 交易对，例如 BTCUSDT
    public string Symbol = "";

    // 订单方向
    public OrderSide Side;

    // 持仓方向
    public PositionSide PositionSide;

    // 订单类型
    public OrderType OrderType;

    // 订单数量（以标的为单位）
    public decimal Quantity;

    // 订单数量（以USDT为单位）
    public decimal CashQuantity;

    // 订单价格（限价单需要）
    public decimal Price;

    // 订单有效方式
    public TimeInForceType TifType;

    // TIF为GTD时订单的自动取消时间
    public long GoodTillDate;

    // 是否只减仓
    public bool ReduceOnly;

    // 创建时间
    public long CreatedTime;

    public static OrderInfo ValueOf(OrderBean bean)
    {
        if (bean == null)
        {
            throw new ArgumentNullException(nameof(bean), "OrderBean cannot be null");
        }

        return new OrderInfo
        {
            OrderId = -1,
            Symbol = bean.Symbol,
            Side = bean.Side,
            PositionSide = bean.PositionSide,
            OrderType = bean.OrderType,
            Quantity = (decimal)bean.Quantity,
            CashQuantity = (decimal)bean.CashQuantity,
            Price = (decimal)bean.Price,
            TifType = bean.TifType,
            GoodTillDate = bean.GoodTillDate,
            ReduceOnly = bean.ReduceOnly,
            CreatedTime = bean.CreatedTime
        };
    }

    public OrderBean ToBean()
    {
        var bean = new OrderBean
        {
            Symbol = Symbol,
            Side = Side,
            PositionSide = PositionSide,
            OrderType = OrderType,
            Quantity = (double)Quantity,
            CashQuantity = (double)CashQuantity,
            Price = (double)Price,
            TifType = TifType,
            GoodTillDate = GoodTillDate,
            ReduceOnly = ReduceOnly,
            CreatedTime = CreatedTime
        };

        return bean;
    }

    // 重写 ToString 方法
    public override string ToString()
    {
        string orderSide = Side == OrderSide.Buy ? "买入" : "卖出";
        string positionSide = PositionSide == PositionSide.Long ? "做多" : "做空";
        string orderType = OrderType == OrderType.Limit ? "限价单" : "市价单";

        string quantityInfo = Quantity > 0
            ? $"数量:{Quantity:F2}"
            : $"数量:{CashQuantity:F2} USDT";

        string priceInfo = OrderType == OrderType.Limit
            ? $"价格:{Price:F2}"
            : ""; // 市价单不显示价格

        return $"{Symbol} {orderType} {positionSide} {priceInfo} {quantityInfo}";
    }
}
