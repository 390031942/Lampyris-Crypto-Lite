using Lampyris.Crypto.Protocol.Quote;
using System.Security.Cryptography.X509Certificates;

namespace Lampyris.Crypto.Server.Lite;

public class SymbolTradeRule: IMessageConvertible<SymbolTradeRuleBean>
{
    /// <summary>
    /// 交易对，例如 BTCUSDT
    /// </summary>
    public string Symbol { get; set; }

    /// <summary>
    /// 最小价格
    /// </summary>
    public decimal MinPrice { get; set; }

    /// <summary>
    /// 最大价格
    /// </summary>
    public decimal MaxPrice { get; set; }

    /// <summary>
    /// 价格步进
    /// </summary>
    public decimal PriceStep { get; set; }

    /// <summary>
    /// 最小数量
    /// </summary>
    public decimal MinQuantity { get; set; }

    /// <summary>
    /// 最大数量
    /// </summary>
    public decimal MaxQuantity { get; set; }

    /// <summary>
    /// 数量步进
    /// </summary>
    public decimal QuantityStep { get; set; }

    /// <summary>
    /// 最小名义价值
    /// </summary>
    public decimal MinNotional { get; set; }

    /// <summary>
    /// 上架时间
    /// </summary>
    public long OnBoardTimestamp { get; set; }

    /// <summary>
    /// 缓存的SymbolTradeRuleBean，避免重复new
    /// </summary>
    private SymbolTradeRuleBean? m_CacheBean;

    public SymbolTradeRuleBean ToBean()
    {
        if(m_CacheBean == null)
        {
            m_CacheBean = new SymbolTradeRuleBean();
        }

        m_CacheBean.Symbol = Symbol;
        m_CacheBean.MaxPrice = Convert.ToDouble(MaxPrice);
        m_CacheBean.MinPrice = Convert.ToDouble(MinPrice);
        m_CacheBean.PriceTickSize = Convert.ToDouble(PriceStep);
        m_CacheBean.MaxQuantity = Convert.ToDouble(MaxQuantity);
        m_CacheBean.MinQuantity = Convert.ToDouble(MinQuantity);
        m_CacheBean.QuantityTickSize = Convert.ToDouble(QuantityStep);
        m_CacheBean.MinNotional = Convert.ToDouble(MinNotional);
        m_CacheBean.OnBoardTimestamp = OnBoardTimestamp;

        return m_CacheBean;;
    }
}