namespace Lampyris.Crypto.Server.Lite;

public interface IMessageConvertible<T> where T : new()
{
    /// <summary>
    /// 转为Bean
    /// </summary>
    /// <returns></returns>
    public T ToBean();
}
