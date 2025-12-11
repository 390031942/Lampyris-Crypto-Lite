namespace Lampyris.CSharp.Common;

using System.Collections;

public class WaitForSeconds : IEnumerator
{
    private double m_waitTime;
    private double m_startTime;

    public WaitForSeconds(double seconds)
    {
        m_waitTime = seconds;
        m_startTime = DateTimeUtil.GetCurrentTimestamp();
    }

    public object Current => throw new NotImplementedException();

    public bool MoveNext()
    {
        return DateTimeUtil.GetCurrentTimestamp() - m_startTime >= m_waitTime;
    }

    public void Reset()
    {
        m_startTime = DateTimeUtil.GetCurrentTimestamp();
    }
}
