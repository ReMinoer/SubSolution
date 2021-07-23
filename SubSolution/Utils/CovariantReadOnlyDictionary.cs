using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SubSolution.Utils
{
    public class CovariantReadOnlyDictionary<TKey, TValue> : ICovariantReadOnlyDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
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
        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;
        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

        public int Count => _dictionary.Count;
        public TValue this[TKey key] => _dictionary[key];
        public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);
        public bool TryGetValue(TKey key, out TValue value) => _dictionary.TryGetValue(key, out value);

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => Keys.Select(key => new KeyValuePair<TKey,TValue>(key, this[key])).GetEnumerator();
        public IEnumerator<ICovariantKeyValuePair<TKey, TValue>> GetEnumerator() => Keys.Select(key => new Pair(key, this[key])).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public class Pair : ICovariantKeyValuePair<TKey, TValue>
        {
            public TKey Key { get; }
            public TValue Value { get; }

            public Pair(TKey key, TValue value)
            {
                Key = key;
                Value = value;
            }
        }
    }
}