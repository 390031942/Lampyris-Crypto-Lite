namespace Lampyris.CSharp.Common;

public class CircularBuffer<T>
{
    private readonly T[] m_Buffer;
    private readonly int m_Capacity;
    private int m_Head = 0;
    private int m_Tail = 0;
    private int m_Count = 0;

    public CircularBuffer(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentException("Capacity of CircularBuffer must be greater than zero.");
        m_Capacity = capacity;
        m_Buffer = new T[capacity];
    }

    public void Add(T item)
    {
        m_Buffer[m_Tail] = item;
        m_Tail = (m_Tail + 1) % m_Capacity;

        if (m_Count < m_Capacity)
        {
            m_Count++;
        }
        else
        {
            // 缓冲区已满，移动头指针（覆盖最老的元素）
            m_Head = (m_Head + 1) % m_Capacity;
        }
    }

    public T[] ToArray()
    {
        T[] result = new T[m_Count];
        for (int i = 0; i < m_Count; i++)
        {
            result[i] = m_Buffer[(m_Head + i) % m_Capacity];
        }
        return result;
    }

    /// <summary>
    /// 索引器，支持正索引和负索引
    /// 正索引：0 = 最老元素，Count-1 = 最新元素
    /// 负索引：-1 = 最新元素，-Count = 最老元素
    /// </summary>
    public T this[int index]
    {
        get
        {
            if (m_Count == 0)
                throw new IndexOutOfRangeException("Buffer is empty");

            // 处理负索引
            if (index < 0)
            {
                index = m_Count + index;
                if (index < 0)
                    throw new IndexOutOfRangeException($"Index {index} is out of range. Valid range is [-{m_Count}, {m_Count - 1}]");
            }

            if (index >= m_Count || index < 0)
                throw new IndexOutOfRangeException($"Index {index} is out of range. Valid range is [0, {m_Count - 1}] or [-{m_Count}, -1]");

            return m_Buffer[(m_Head + index) % m_Capacity];
        }
        set
        {
            if (m_Count == 0)
                throw new IndexOutOfRangeException("Buffer is empty");

            // 处理负索引
            if (index < 0)
            {
                index = m_Count + index;
                if (index < 0)
                    throw new IndexOutOfRangeException($"Index {index} is out of range. Valid range is [-{m_Count}, {m_Count - 1}]");
            }

            if (index >= m_Count || index < 0)
                throw new IndexOutOfRangeException($"Index {index} is out of range. Valid range is [0, {m_Count - 1}] or [-{m_Count}, -1]");

            m_Buffer[(m_Head + index) % m_Capacity] = value;
        }
    }

    /// <summary>
    /// 获取最后一个元素
    /// </summary>
    public T Last => this[-1];

    /// <summary>
    /// 获取首个元素
    /// </summary>
    public T First => this[0];

    public int Count => m_Count;

    public int Capacity => m_Capacity; 
}