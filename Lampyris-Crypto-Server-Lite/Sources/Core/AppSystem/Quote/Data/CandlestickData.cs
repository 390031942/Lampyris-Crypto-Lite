namespace Lampyris.Crypto.Server.Lite;

public class CandlestickData : IComparable<CandlestickData>
{
    public DateTime DateTime { get; set; }

    public decimal Open { get; set; }

    public decimal Close { get; set; }

    public decimal Low { get; set; }

    public decimal High { get; set; }

    public decimal Volume { get; set; }

    public decimal Currency { get; set; }

    // 均线数据（MA5,MA10,MA20）
    public decimal[] MA = new decimal[MAIndicatorType.COUNT];
    
    public decimal ChangePerc
    {
        get
        {
            if (Open != 0)
            {
                return Math.Round(100 * (Close - Open) / Open, 2);
            }
            return 0;
        }

    }
    public int CompareTo(CandlestickData? other)
    {
        if (other == null)
        {
            return 0;
        }
        return other.DateTime.CompareTo(this.DateTime);
    }

    public decimal ChangePercentage(CandlestickData? other)
    {
        if (other == null)
        {
            return 0;
        }
        return Math.Round((other.Close - Close) / Close * 100, 2);
    }
}
