using System.Collections.Generic;

namespace SubSolution.Utils
{
    public interface ICovariantReadOnlyDictionary<TKey, out TValue> : IEnumerable<ICovariantKeyValuePair<TKey, TValue>>
    {
        int Count { get; }
        IReadOnlyCollection<TKey> Keys { get; }
        IReadOnlyCollection<TValue> Values { get; }
        TValue this[TKey key] { get; }
        bool ContainsKey(TKey key);
    }
}