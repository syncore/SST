using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SSB.Util
{
    /// <summary>
    ///     Class containing general helper methods.
    /// </summary>
    public static class Tools
    {
        // The time scales that are valid for our purposes (primarily used for adding various bans)
        public static string[] ValidTimeScales =
        {
            "sec", "secs", "min", "mins", "hour", "hours", "day", "days",
            "month", "months", "year", "years"
        };

        /// <summary>
        ///     Determines whether the specified user's name is valid per QL requirements.
        /// </summary>
        /// <param name="user">The user to check.</param>
        /// <param name="allowMultipleUsers">
        ///     if set to <c>true</c> then also allow
        ///     commas to be included in the regular expression (for things requiring multiple users
        ///     separated by commas).
        /// </param>
        /// <returns>
        ///     <c>true</c> if the user name is valid, otherwise <c>false.</c>
        /// </returns>
        /// <remarks>
        ///     Note: this does not check whether the user actually exists in QL, only whether the
        ///     username does not invalid characters.
        /// </remarks>
        public static bool IsValidQlUsernameFormat(string user, bool allowMultipleUsers)
        {
            if (allowMultipleUsers)
            {
                // Only A-Z, 0-9, and underscore (with comma as separator for multiple names) allowed by QL
                return !Regex.IsMatch(user, "[^a-zA-Z0-9_,]");
            }

            // Single user: Only A-Z, 0-9, and underscore allowed by QL
            return !Regex.IsMatch(user, "[^a-zA-Z0-9_]");
        }

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
        ///     Taken from Alexander Prokofyev's answer at:
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