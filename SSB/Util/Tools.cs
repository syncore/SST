using System.Collections.Generic;

namespace SSB.Util
{
    /// <summary>
    ///     Class containing general helper functions.
    /// </summary>
    public static class Tools
    {
        /// <summary>
        ///     Checks whether a key is present in a given dictionary.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TVal">The type of the value.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="dictionary">The dictionary.</param>
        /// <returns>
        ///     <c>true</c> if the key is present, otherwise <c>false</c>.
        /// </returns>
        public static bool KeyExists<TKey, TVal>(TKey key, Dictionary<TKey, TVal> dictionary)
        {
            TVal val;
            return dictionary.TryGetValue(key, out val);
        }
    }
}