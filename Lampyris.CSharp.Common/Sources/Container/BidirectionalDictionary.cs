namespace Lampyris.CSharp.Common;

using System.Collections;

public class BidirectionalDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>> where TKey : notnull where TValue : notnull
{
    private readonly Dictionary<TKey, TValue> m_Forward = new();

    private readonly Dictionary<TValue, TKey> m_Reverse = new();

    public IEnumerable<TKey> Keys => m_Forward.Keys;

    public IEnumerable<TValue> Values => m_Reverse.Keys;

    public int Count => m_Forward.Count;

    /// <summary>
    /// 添加键值对到字典中。
    /// </summary>
    public void Add(TKey key, TValue value)
    {
        if (m_Forward.ContainsKey(key))
            throw new ArgumentException($"Key '{key}' already exists in the dictionary.");
        if (m_Reverse.ContainsKey(value))
            throw new ArgumentException($"Value '{value}' already exists in the dictionary.");

        m_Forward[key] = value;
        m_Reverse[value] = key;
    }

    /// <summary>
    /// 尝试通过键获取值。
    /// </summary>
    public bool TryGetByKey(TKey key, out TValue value)
    {
        return m_Forward.TryGetValue(key, out value);
    }

    /// <summary>
    /// 尝试通过值获取键。
    /// </summary>
    public bool TryGetByValue(TValue value, out TKey key)
    {
        return m_Reverse.TryGetValue(value, out key);
    }

    /// <summary>
    /// 新增方法：尝试通过键获取值。
    /// </summary>
    public bool TryGetValue(TKey key, out TValue value)
    {
        return TryGetByKey(key, out value);
    }

    /// <summary>
    /// 新增方法：尝试通过值获取键。
    /// </summary>
    public bool TryGetValue(TValue value, out TKey key)
    {
        return TryGetByValue(value, out key);
    }

    /// <summary>
    /// 通过键获取值。
    /// </summary>
    public TValue GetByKey(TKey key)
    {
        if (!m_Forward.TryGetValue(key, out var value))
            throw new KeyNotFoundException($"Key '{key}' not found in the dictionary.");
        return value;
    }

    /// <summary>
    /// 通过值获取键。
    /// </summary>
    public TKey GetByValue(TValue value)
    {
        if (!m_Reverse.TryGetValue(value, out var key))
            throw new KeyNotFoundException($"Value '{value}' not found in the dictionary.");
        return key;
    }

    /// <summary>
    /// 通过键移除键值对。
    /// </summary>
    public void RemoveByKey(TKey key)
    {
        if (!m_Forward.TryGetValue(key, out var value))
            throw new KeyNotFoundException($"Key '{key}' not found in the dictionary.");

        m_Forward.Remove(key);
        m_Reverse.Remove(value);
    }

    /// <summary>
    /// 通过值移除键值对。
    /// </summary>
    public void RemoveByValue(TValue value)
    {
        if (!m_Reverse.TryGetValue(value, out var key))
            throw new KeyNotFoundException($"Value '{value}' not found in the dictionary.");

        m_Reverse.Remove(value);
        m_Forward.Remove(key);
    }

    /// <summary>
    /// 清空字典。
    /// </summary>
    public void Clear()
    {
        m_Forward.Clear();
        m_Reverse.Clear();
    }

    /// <summary>
    /// 索引器：通过键访问值。
    /// </summary>
    public TValue this[TKey key]
    {
        get => GetByKey(key);
        set
        {
            if (m_Forward.ContainsKey(key))
            {
                // 如果键已存在，更新值，同时更新反向映射
                var oldValue = m_Forward[key];
                m_Reverse.Remove(oldValue);
            }
            if (m_Reverse.ContainsKey(value))
            {
                // 如果值已存在，更新键，同时更新正向映射
                var oldKey = m_Reverse[value];
                m_Forward.Remove(oldKey);
            }

            m_Forward[key] = value;
            m_Reverse[value] = key;
        }
    }

    /// <summary>
    /// 实现 IEnumerable<KeyValuePair<TKey, TValue>>。
    /// </summary>
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return m_Forward.GetEnumerator();
    }

    /// <summary>
    /// 实现 IEnumerable。
    /// </summary>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}