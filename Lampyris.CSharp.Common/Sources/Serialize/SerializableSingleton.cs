using Lampyris.CSharp.Common;
using System.Reflection;

public interface IPostSerializationHandler
{
    public void PostSerialization();
}

[Serializable]
public class SerializableSingleton<T> where T : class, new()
{
    private static T ms_instance;

    public static T Instance
    {
        get
        {
            if (ms_instance == null)
            {
                ms_instance = Components.GetComponent<SerializationManager>().TryDeserializeObjectFromFile<T>();
                if(ms_instance == null)
                {
                    ms_instance = new T();
                }
                else
                {
                    if (typeof(IPostSerializationHandler).IsAssignableFrom(typeof(T)))
                    {
                        MethodInfo method = typeof(T).GetMethod("PostSerialization");
                        if (method != null)
                        {
                            method.Invoke(ms_instance, null); // 通过反射调用func方法
                        }
                    }
                }
            }
            return ms_instance;
        }
    }

    public void Save()
    {
        // 保存到缓存
        Components.GetComponent<SerializationManager>().Save(Instance);
    }

    public virtual void PostSerialization() { }
}