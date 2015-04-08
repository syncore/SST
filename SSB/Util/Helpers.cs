using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using SSB.Enums;
using SSB.Model;

namespace SSB.Util
{
    /// <summary>
    ///     Class containing general helper methods.
    /// </summary>
    public static class Helpers
    {
        // The time scales that are valid for our purposes (primarily used for adding various bans)
        public static string[] ValidTimeScales =
        {
            "secs", "mins", "hours", "days",
            "months", "years"
        };

        /// <summary>
        ///     Gets the argument value.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="argNum">The argument's index.</param>
        /// <returns>The string value at a given index.</returns>
        /// <remarks>
        ///     This method shifts the value to the right by one if the
        ///     command was initiated from IRC, to take into account the fact
        ///     that IRC commands will always have a first value, c[0], of the
        ///     IrcToQl command name (i.e. c[0] = "!ql")
        /// </remarks>
        public static string GetArgVal(CmdArgs c, int argNum)
        {
            return c.FromIrc ? c.Args[argNum + 1] : c.Args[argNum];
        }

        /// <summary>
        ///     Gets the name of player with the clan tag stripped away, if it exists.
        /// </summary>
        /// <param name="name">The input name.</param>
        /// <returns>The name as a string, with the clan tag stripped away, if it exists.</returns>
        /// <remarks>
        ///     This is necessary because certain events, namely player connections and when the player spectates,
        ///     use the full name with the clan tag included, but internally the bot always uses the short name.
        /// </remarks>
        public static string GetStrippedName(string name)
        {
            return name.LastIndexOf(" ", StringComparison.Ordinal) != -1
                ? name.Substring(name.LastIndexOf(" ", StringComparison.Ordinal) + 1).ToLowerInvariant()
                : name.ToLowerInvariant();
        }

        /// <summary>
        ///     Gets the array index of a given scale from the time scale array.
        /// </summary>
        /// <param name="scale">The scale.</param>
        /// <returns>
        ///     The array index of a given scale from the time scale array.
        /// </returns>
        public static int GetTimeScaleIndex(string scale)
        {
            var index = 0;
            for (var i = 0; i < ValidTimeScales.Length; i++)
            {
                if (scale.Equals(ValidTimeScales[i],
                    StringComparison.InvariantCultureIgnoreCase))
                {
                    index = i;
                }
            }
            return index;
        }

        /// <summary>
        ///     Gets the version of SSB currently running.
        /// </summary>
        /// <returns>The version number as a string.</returns>
        public static string GetVersion()
        {
            return typeof (EntryPoint).Assembly.GetName().Version.ToString();
        }

        /// <summary>
        ///     Invokes a method when making calls, if necessary, to a control because the caller is on a different
        ///     thread than the one on which the control was created.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="c">The control.</param>
        /// <param name="action">The action.</param>
        public static void InvokeIfRequired<T>(this T c, Action<T> action) where T : Control
        {
            if (c.InvokeRequired)
            {
                c.Invoke(new Action(() => action(c)));
            }
            else
            {
                action(c);
            }
        }

        /// <summary>
        ///     Determines whether the specified gametype is a team-based game.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if the specified gametype is a team-based game; otherwise
        ///     <c>false</c>
        /// </returns>
        public static bool IsQuakeLiveTeamGame(QlGameTypes gametype)
        {
            switch (gametype)
            {
                case QlGameTypes.Unspecified:
                case QlGameTypes.Ffa:
                case QlGameTypes.Duel:
                case QlGameTypes.Race:
                    return false;

                case QlGameTypes.Tdm:
                case QlGameTypes.Ca:
                case QlGameTypes.Ctf:
                case QlGameTypes.OneFlagCtf:
                case QlGameTypes.Harvester:
                case QlGameTypes.FreezeTag:
                case QlGameTypes.Domination:
                case QlGameTypes.AttackDefend:
                case QlGameTypes.RedRover:
                    return true;
            }
            return false;
        }

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
            var m = Regex.Match(input, "((" + value + ").*?){" + n + "}");

            if (m.Success)
            {
                return m.Groups[2].Captures[n - 1].Index;
            }
            return -1;
        }

        /// <summary>
        ///     Removes the QL color characters from the input string.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>A string with the QL color characters removed.</returns>
        public static string RemoveQlColorChars(string input)
        {
            return Regex.Replace(input, "\\^\\d+", string.Empty);
        }
    }
}