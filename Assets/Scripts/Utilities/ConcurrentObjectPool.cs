using System;
using System.Collections.Concurrent;

public class ConcurrentObjectPool<T>
{
    private readonly ConcurrentBag<T> items = new ConcurrentBag<T>();
    private int counter = 0;
    private int MAX = 20;

    public void Release(T item)
    {
        if (counter < MAX)
        {
            items.Add(item);
            counter++;
        }
    }

    public T Get()
    {
        T item;
        if (items.TryTake(out item))
        {
            counter--;
            return item;
        }
        else
        {
            T obj = default(T);
            items.Add(obj);
            counter++;
            return obj;
        }
    }
}