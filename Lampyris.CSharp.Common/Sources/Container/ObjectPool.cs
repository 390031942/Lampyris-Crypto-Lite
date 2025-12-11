namespace Lampyris.CSharp.Common;

using System;
using System.Collections.Concurrent;

public class ObjectPool<T> where T : class
{
    private readonly ConcurrentBag<T> _objects; // 用于存储对象的容器
    private readonly Func<T> _objectGenerator;  // 用于创建新对象的委托
    private readonly int _maxCapacity;         // 最大容量

    /// <summary>
    /// 当前池中的对象数量
    /// </summary>
    public int Count => _objects.Count;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="objectGenerator">创建对象的委托</param>
    /// <param name="maxCapacity">对象池的最大容量</param>
    public ObjectPool(Func<T> objectGenerator, int maxCapacity = int.MaxValue)
    {
        _objectGenerator = objectGenerator ?? throw new ArgumentNullException(nameof(objectGenerator));
        _objects = new ConcurrentBag<T>();
        _maxCapacity = maxCapacity;
    }

    /// <summary>
    /// 从对象池中获取一个对象
    /// </summary>
    /// <returns>对象池中的对象</returns>
    public T Get()
    {
        if (_objects.TryTake(out T item))
        {
            return item; // 如果池中有对象，直接返回
        }

        // 如果池中没有对象，则创建一个新对象
        return _objectGenerator();
    }

    /// <summary>
    /// 将对象归还到对象池
    /// </summary>
    /// <param name="item">要归还的对象</param>
    public void Return(T item)
    {
        if (item == null)
        {
            throw new ArgumentNullException(nameof(item));
        }

        // 如果池中的对象数量未超过最大容量，则归还对象
        if (_objects.Count < _maxCapacity)
        {
            _objects.Add(item);
        }
        else
        {
            // 如果超过最大容量，可以选择丢弃对象或执行其他逻辑
            // 此处直接丢弃对象
        }
    }
}
