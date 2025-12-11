using Lampyris.Crypto.Protocol.Trading;

namespace Lampyris.Crypto.Server.Lite;

public class LeverageBracketInfo:IMessageConvertible<LeverageBracketBean>
{
    public string Symbol { get; set; }

    // 当前分层下的最大杠杆倍数
    public int Leverage { get; set; }

    // 当前分层下的名义价值上限
    public decimal NotionalCap { get; set; }

    // 当前分层下的名义价值下限
    public decimal NotionalFloor { get; set; }

    private LeverageBracketBean? m_LeverageBracketBean;

    public LeverageBracketBean ToBean()
    {
        if(m_LeverageBracketBean == null)
        {
            m_LeverageBracketBean = new LeverageBracketBean();
        }

        m_LeverageBracketBean.Leverage = Leverage;
        m_LeverageBracketBean.NotionalCap = Convert.ToDouble(NotionalCap);
        m_LeverageBracketBean.NotionalFloor = Convert.ToDouble(NotionalFloor);

        return m_LeverageBracketBean;
    }
}
