using Lampyris.Crypto.Protocol.Trading;

namespace Lampyris.Crypto.Server.Lite;

public class LeverageSetting
{
    // 交易对
    public string Symbol { get; set; }

    // 杠杆倍数
    public int Leverage { get; set; }

    // 当前杠杆倍数下最大开仓名义价值
    public decimal MaxNotional { get; set; }

    public LeverageBean ToBean()
    {
        LeverageBean bean = new LeverageBean();
        bean.Symbol = Symbol;
        bean.Leverage = Leverage;
        bean.MaxNotional = Convert.ToDouble(MaxNotional);
        return bean;
    }
}
