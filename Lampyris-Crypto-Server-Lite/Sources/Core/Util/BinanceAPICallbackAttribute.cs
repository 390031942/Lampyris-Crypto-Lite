namespace Lampyris.Crypto.Server.Lite;

/// <summary>
/// 标记为BinanceAPI回调的属性，无实际功能
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class BinanceAPICallbackAttribute : Attribute
{
    public BinanceAPICallbackAttribute()
    {

    }
}
