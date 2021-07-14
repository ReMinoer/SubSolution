using System.Collections.Generic;

namespace SubSolution.Utils
{
    public class CovariantReadOnlyDictionary<TKey, TValue> : ICovariantReadOnlyDictionary<TKey, TValue>
    {
        private readonly IDictionary<TKey, TValue> _dictionary;

        public CovariantReadOnlyDictionary(IDictionary<TKey, TValue> dictionary)
        {
            _dictionary = dictionary;
            Keys = new ReadOnlyCollection<TKey>(_dictionary.Keys);
            Values = new ReadOnlyCollection<TValue>(_dictionary.Values);
        }

        public IReadOnlyCollection<TKey> Keys { get; }
        public IReadOnlyCollection<TValue> Values { get; }
        public int Count => _dictionary.Count;
        public TValue this[TKey key] => _dictionary[key];
        public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);
    }
}