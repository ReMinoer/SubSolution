using System;
using System.Collections.Generic;
#if NETFRAMEWORK
using System.Threading.Tasks;
#endif

// ReSharper disable once CheckNamespace
namespace SubSolution
{
#if NETSTANDARD
    static public class CompatibilityHelpers
    {
        static public string Join(this IEnumerable<string> enumerable, char separator)
        {
            return string.Join(separator, enumerable);
        }

        static public IAsyncDisposable AsAsyncDisposable<T>(this T disposable, out T variable)
            where T : IAsyncDisposable
        {
            variable = disposable;
            return disposable;
        }
    }
#elif NETFRAMEWORK
    static public class CompatibilityHelpers
    {
        static public string Join(this IEnumerable<string> enumerable, char separator)
        {
            return string.Join(separator.ToString(), enumerable);
        }

        static public IAsyncDisposable AsAsyncDisposable<T>(this T disposable, out T variable)
            where T : IDisposable
        {
            variable = disposable;
            return new AsyncDisposable(disposable);
        }

        private class AsyncDisposable : IAsyncDisposable
        {
            private readonly IDisposable _disposable;

            public AsyncDisposable(IDisposable disposable)
            {
                _disposable = disposable;
            }

            public ValueTask DisposeAsync()
            {
                _disposable.Dispose();
                return new ValueTask();
            }
        }

        static public HashSet<T> ToHashSet<T>(this IEnumerable<T> enumerable, IEqualityComparer<T> equalityComparer)
        {
            return new HashSet<T>(enumerable, equalityComparer);
        }

        static public void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> pair, out TKey key, out TValue value)
        {
            key = pair.Key;
            value = pair.Value;
        }

        static public bool Remove<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, out TValue value)
        {
            if (dictionary.ContainsKey(key))
            {
                value = dictionary[key];
                dictionary.Remove(key);
                return true;
            }
            else
            {
                value = default(TValue)!;
                return false;
            }
        }

        static public string[] Split(this string value, char separator, StringSplitOptions options)
        {
            return value.Split(new[] { separator }, options);
        }

        static public bool Contains(this string value, string match, StringComparison stringComparison)
        {
            return value.IndexOf(match, stringComparison) != -1;
        }

        static public bool StartsWith(this string value, char character)
        {
            return value.Length > 0 && value[0] == character;
        }

        static public bool EndsWith(this string value, char character)
        {
            return value.Length > 0 && value[^1] == character;
        }
    }
#endif
}