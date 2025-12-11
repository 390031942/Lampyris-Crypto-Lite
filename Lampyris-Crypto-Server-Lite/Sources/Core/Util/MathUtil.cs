namespace Lampyris.Crypto.Server.Lite;

public static class MathUtil
{
    /// <summary>
    /// 根据步进值向下调整
    /// </summary>
    /// <param name="value">原始值</param>
    /// <param name="step">步进值</param>
    /// <returns>向下调整后的值</returns>
    public static decimal LowerByStep(decimal value, decimal step)
    {
        if (step <= 0)
        {
            throw new ArgumentException("步进值必须大于0", nameof(step));
        }

        // 向下调整到最近的步进倍数
        return Math.Floor(value / step) * step;
    }

    /// <summary>
    /// 根据步进值向上调整
    /// </summary>
    /// <param name="value">原始值</param>
    /// <param name="step">步进值</param>
    /// <returns>向上调整后的值</returns>
    public static decimal UpperByStep(decimal value, decimal step)
    {
        if (step <= 0)
        {
            throw new ArgumentException("步进值必须大于0", nameof(step));
        }

        // 向上调整到最近的步进倍数
        return Math.Ceiling(value / step) * step;
    }
}
