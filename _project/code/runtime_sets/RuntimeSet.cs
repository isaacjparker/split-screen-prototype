using Godot;
using System.Collections.Generic;

public partial class RuntimeSet<T> : Resource
{
    private readonly HashSet<T> _items = new HashSet<T>();

    public void Add(T item)
    {
        _items.Add(item);
    }

    public void Remove(T item)
    {
        _items.Remove(item);
    }

    public bool Contains(T item)
    {
        return _items.Contains(item);
    }

    public HashSet<T> GetAll()
    {
        return _items;
    }

    public int Count => _items.Count;

    public void Clear()
    {
        _items.Clear();
    }
}
