namespace CapriKit.Collections;

/// <summary>
/// A small mutable collection optimized for storing a single value.
/// Use <see cref="List{T}"/> when the collection usually holds many values, when you need the elements
/// to be contiguous, or when you need to pass the values around as an <see cref="IEnumerable{T}"/>.
/// </summary>
/// <remarks>
/// This is a mutable struct, to use it in a different or other collection take a reference
/// or the changes are discarded.
/// <code>
/// ref var values = ref CollectionsMarshal.GetValueRefOrAddDefault(dictionary, key, out _);
/// values.Add(value);
/// </code>
/// </remarks>
public struct OneOrMany<T>
{
    private T? head;
    private T[]? tail;

    /// <summary>Creates a collection holding a single value</summary>
    public OneOrMany(T value)
    {
        head = value;
        tail = null;
        Count = 1;
    }

    /// <summary>The number of values in the collection</summary>
    public int Count { get; private set; }

    /// <summary>Gets or replaces the value at <paramref name="index"/>, in insertion order.</summary>
    /// <exception cref="ArgumentOutOfRangeException">If the index is negative or not below <see cref="Count"/>.</exception>
    public T this[int index]
    {
        readonly get
        {
            ThrowIfOutOfRange(index);
            return index == 0 ? head! : tail![index - 1];
        }
        set
        {
            ThrowIfOutOfRange(index);
            if (index == 0) { head = value; }
            else { tail![index - 1] = value; }
        }
    }

    /// <summary>Appends a value.</summary>
    public void Add(T value)
    {
        if (Count == 0)
        {
            head = value;
        }
        else
        {
            var slot = Count - 1;
            if (tail is null) { tail = new T[2]; }
            else if (slot == tail.Length) { Array.Resize(ref tail, tail.Length * 2); }
            tail[slot] = value;
        }

        Count++;
    }

    /// <summary>Returns the index of the first value equal to <paramref name="value"/>, or -1.</summary>
    public readonly int IndexOf(T value)
    {
        var comparer = EqualityComparer<T>.Default;
        for (var i = 0; i < Count; i++)
        {
            if (comparer.Equals(this[i], value))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>Returns whether any value equals <paramref name="value"/>.</summary>
    public readonly bool Contains(T value) => IndexOf(value) >= 0;

    /// <summary>Removes the first value equal to <paramref name="value"/> and reports whether it was found.</summary>
    public bool Remove(T value)
    {
        var index = IndexOf(value);
        if (index < 0)
        {
            return false;
        }

        RemoveAt(index);
        return true;
    }

    /// <summary>Removes the value at <paramref name="index"/>, shifting the values after it down.</summary>
    /// <exception cref="ArgumentOutOfRangeException">If the index is negative or not below <see cref="Count"/>.</exception>
    public void RemoveAt(int index)
    {
        ThrowIfOutOfRange(index);

        for (var i = index; i < Count - 1; i++)
        {
            this[i] = this[i + 1];
        }

        Count--;

        // Overwrite the slot that just fell outside the collection, otherwise the removed value stays
        // reachable from this struct and the garbage collector cannot free it
        if (Count == 0) { head = default; }
        else { tail![Count - 1] = default!; }
    }

    /// <summary>Removes all values and releases the overflow array, so a later second value allocates again.</summary>
    public void Clear()
    {
        head = default;
        tail = null;
        Count = 0;
    }

    /// <summary>
    /// Returns a struct enumerator, so <c>foreach</c> over this collection does not allocate.
    /// The enumerator works on a copy: values added or removed while it runs are not observed.
    /// </summary>
    /// <remarks>
    /// This type deliberately does not implement <see cref="IEnumerable{T}"/>. Enumerating through
    /// that interface (or through LINQ) boxes the struct, which is the allocation this type exists
    /// to avoid. Copy the values into a list yourself if you really need an <see cref="IEnumerable{T}"/>.
    /// </remarks>
    public readonly Enumerator GetEnumerator() => new(this);

    private readonly void ThrowIfOutOfRange(int index)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);
    }

    /// <summary>Enumerates the values of a <see cref="OneOrMany{T}"/> in insertion order.</summary>
    public struct Enumerator
    {
        private readonly OneOrMany<T> values;
        private int index;

        internal Enumerator(OneOrMany<T> source)
        {
            values = source;
            index = -1;
        }

        public readonly T Current => values[index];

        public bool MoveNext() => ++index < values.Count;
    }
}
