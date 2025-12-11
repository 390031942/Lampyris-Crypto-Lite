[Serializable]
public class SerializableKeyValuePair<TKey, TValue>
{
    private TKey m_key;
    private TValue m_value;

    public TKey Key => m_key;
    public TValue Value => m_value;

    public SerializableKeyValuePair(TKey key, TValue value)
    {
        m_key = key;
        m_value = value;
    }
}
