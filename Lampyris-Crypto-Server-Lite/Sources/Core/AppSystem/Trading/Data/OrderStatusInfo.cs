namespace Lampyris.Crypto.Server.Lite;

using Lampyris.Crypto.Protocol.Trading;

public class OrderStatusInfo
{
    // 订单ID
    public long OrderId { get; set; }

     // 订单基本信息（存储为 JSON）
    public OrderInfo OrderInfo { get; set; }

    // 订单状态
    public OrderStatus Status { get; set; }

    // 已成交数量
    public decimal FilledQuantity { get; set; }

    // 成交均价
    public decimal AvgFilledPrice { get; set; }

    public void Reset()
    {
        Status = OrderStatus.New;
        FilledQuantity = 0.0m;
        AvgFilledPrice = 0.0m;
    }

    public OrderStatusBean ToBean()
    {
        return new OrderStatusBean()
        {
            OrderId = OrderId,
            OrderBean = OrderInfo.ToBean(),
            Status = Status,
            FilledQuantity = (double)FilledQuantity,
            AvgFilledPrice = (double)AvgFilledPrice,
        };
    }
}