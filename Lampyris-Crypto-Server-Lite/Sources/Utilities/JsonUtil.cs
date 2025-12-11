using Newtonsoft.Json.Linq;

namespace Lampyris.Crypto.Server.Lite;

public static class JsonUtil
{
    public static double SafeToDouble(this JObject jObject, string key)
    {
        if (jObject == null)
            return 0.0;

        var token = jObject.ToObject<JToken>();
        if (token == null)
            return 0.0;

        string str = token.ToString();

        if(!double.TryParse(str,out double result))
        {
            result = 0.0;
        }

        return result;
    }
}
