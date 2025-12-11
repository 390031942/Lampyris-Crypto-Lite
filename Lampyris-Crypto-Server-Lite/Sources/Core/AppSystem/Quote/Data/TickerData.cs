namespace Lampyris.Crypto.Server.Lite;

public class TickerData
{
    // 合约ID
    public string Symbol = "";

    // 最新成交价
    public decimal Price;

    // 最新成交的数量，0 代表没有成交量
    public decimal LastSize;

    // 24小时最高价
    public decimal High24H;

    // 24小时最低价
    public decimal Low24H;

    // 24小时成交量，以币为单位
    public decimal Volume24H;

    // 24小时成交量，以张为单位
    public decimal QuoteVolume24H;

    // 24小时均价ss
    public decimal AvgPrice24H => Volume24H / QuoteVolume24H;

    // ticker数据产生时间，Unix时间戳的毫秒数格式，如 1597026383085
    public long Timestamp;

    // UTC+0 开盘价
    public decimal Open;

    // UTC+0 最高价
    public decimal High;

    // UTC+0 最低价
    public decimal Low;

    // UTC+0 最高涨幅
    public decimal HighPerc;

    // UTC+0 最低涨幅
    public decimal LowPerc;

    // UTC+0 涨幅
    public decimal ChangePerc;

    // UTC+0 涨跌额
    public decimal Change;

    // UTC+0 标记价格
    public decimal MarkPrice;

    // UTC+0 指数价格
    public decimal IndexPrice;

    // 资金费率
    public decimal FundingRate;

    // 下一次资金时间戳
    public long NextFundingTime;

    // 涨速(手动计算,3min)
    public decimal RiseSpeed;

    // 异动标签
    public List<string> Labels = new List<string>();
}
