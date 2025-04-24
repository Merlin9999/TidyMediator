using System;
using System.Collections.Generic;
using System.Linq;

namespace TidyMediator.Extensions
{
    public static class DictionaryExtensions
    {
        public static TValue TryGetValue<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> kvpCollection, TKey key)
        {
            if (kvpCollection == null)
                throw new ArgumentNullException(nameof(kvpCollection));

            if (kvpCollection is IDictionary<TKey, TValue> dict1)
                return dict1.TryGetValue(key, out TValue value)
                    ? value
                    : default;

            if (kvpCollection is IReadOnlyDictionary<TKey, TValue> dict2)
                return dict2.TryGetValue(key, out TValue value)
                    ? value
                    : default;

            // Fallback implementation. Shouldn't be called and unlikely that it will be called.
            return kvpCollection.Where(x => x.Key.Equals(key))
                .Select(x => x.Value)
                .SingleOrDefault();
        }
    }
}