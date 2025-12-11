using Lampyris.CSharp.Common;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// 序列化管理，会将指定的对象 序列化到系统的文档文件夹 
/// </summary>
[Component]
public class SerializationManager : ILifecycle
{
    private List<object> m_serializableObjectList = new List<object>();

    public void SaveAll()
    {
        foreach (object serializableObject in m_serializableObjectList)
        {
            Save(serializableObject);
        }
    }
    public void Save<T>() where T : SerializableSingleton<T> ,new()
    {
        Save(SerializableSingleton<T>.Instance);
    }

    public void Save(object serializableObject)
    {
        string filePath = Path.Combine(PathUtil.SerializedDataSavePath, serializableObject.GetType().Name + ".json");
        string json = JsonSerializer.Serialize(serializableObject, new JsonSerializerOptions
        {
            IncludeFields = true, // 允许序列化字段
            WriteIndented = true, // 格式化输出
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull // 忽略 null 值
        });

        File.WriteAllText(filePath, json);
    }

    public override void OnDestroy()
    {
        SaveAll();
    }

    public override void OnStart()
    {

    }

    public void Register(object serializableObject)
    {
        if (serializableObject != null)
        {
            m_serializableObjectList.Add(serializableObject);
        }
    }

    public T? TryDeserializeObjectFromFile<T>()
    {
        string filePath = Path.Combine(PathUtil.SerializedDataSavePath, typeof(T).Name + ".json");

        if (File.Exists(filePath))
        {
            try
            {
                string json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<T>(json, options: new JsonSerializerOptions
                {
                    IncludeFields = true, // 允许序列化字段
                    PropertyNameCaseInsensitive = true // 属性名大小写不敏感
                });
            }
            catch (JsonException ex)
            {
                Logger.LogError($"反序列化失败: {ex.Message}");
                return default(T);
            }
        }

        return default(T);
    }
}
