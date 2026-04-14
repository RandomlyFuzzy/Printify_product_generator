using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

public sealed class RoundRobinSelector<T>
{
    private readonly IReadOnlyList<T> _items;
    private int _index = -1;

    public RoundRobinSelector(IEnumerable<T> items)
    {
        if (items is null)
            throw new ArgumentNullException(nameof(items));

        _items = items.ToList();
        if (_items.Count == 0)
            throw new ArgumentException("At least one item is required.", nameof(items));
    }

    public IReadOnlyList<T> Items => _items;

    public T Next()
    {
        var nextIndex = Interlocked.Increment(ref _index);
        return _items[nextIndex % _items.Count];
    }
}