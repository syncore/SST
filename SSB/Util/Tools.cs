using System.Collections.Generic;
using System.Text.RegularExpressions;

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

        /// <summary>
        ///     Find the n-th occurrence of a substring s.
        /// </summary>
        /// <param name="input">The input string</param>
        /// <param name="value">The value to find</param>
        /// <param name="n">The n-th occurrence.</param>
        /// <returns>The position of the n-th occurrence of the value.</returns>
        /// <remarks>
        ///     Taken from Alexander PRokofyev's answer at:
        ///     http://stackoverflow.com/a/187394
        /// </remarks>
        public static int NthIndexOf(string input, string value, int n)
        {
            Match m = Regex.Match(input, "((" + value + ").*?){" + n + "}");

            if (m.Success)
            {
                return m.Groups[2].Captures[n - 1].Index;
            }
            return -1;
        }
    }
}