using System.Collections;
using System.Collections.Generic;

namespace SubSolution.Utils
{
    public class ReadOnlyCollection<T> : IReadOnlyCollection<T>
    {
        private readonly ICollection<T> _collection;

        public ReadOnlyCollection(ICollection<T> collection)
        {
            _collection = collection;
        }

        public int Count => _collection.Count;
        public IEnumerator<T> GetEnumerator() => _collection.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}