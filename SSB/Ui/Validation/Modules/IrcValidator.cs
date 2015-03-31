using System.Text.RegularExpressions;
using SSB.Core.Modules.Irc;

namespace SSB.Ui.Validation.Modules
{
    /// <summary>
    ///     UI Validation class for the Modules: IRC tab.
    /// </summary>
    public class IrcValidator
    {
        private readonly Regex _validNickRegex;

        public IrcValidator(Regex validNickRegex)
        {
            _validNickRegex = validNickRegex;
        }

        /// <summary>
        ///     Determines whether the specified user input is a valid IRC channel.
        /// </summary>
        /// <param name="userInput">The user input.</param>
        /// <param name="errorMsg">The error message.</param>
        /// <returns>
        ///     <c>true</c> if the user input is valid, otherwise <c>false</c>
        /// </returns>
        public bool IsValidIrcChannel(string userInput, out string errorMsg)
        {
            errorMsg = "You must specify a valid IRC channel!";
            if (userInput.Length == 0) return false;
            if (!userInput.StartsWith("#")) return false;
            if (userInput.Contains(" ")) return false;

            errorMsg = string.Empty;
            return true;
        }

        /// <summary>
        ///     Determines whether the specified user input is a valid IRC channel key.
        /// </summary>
        /// <param name="userInput">The user input.</param>
        /// <param name="errorMsg">The error message.</param>
        /// <returns>
        ///     <c>true</c> if the user input is valid, otherwise <c>false</c>
        /// </returns>
        public bool IsValidIrcChannelKey(string userInput, out string errorMsg)
        {
            errorMsg = "IRC channel key cannot contain spaces!";
            // Channel key is not required, but if typed then no spaces are allowed.
            if (userInput.Contains(" ")) return false;

            errorMsg = string.Empty;
            return true;
        }

        /// <summary>
        ///     Determines whether the specified user input is a valid IRC nickname.
        /// </summary>
        /// <param name="userInput">The user input.</param>
        /// <param name="errorMsg">The error message.</param>
        /// <returns>
        ///     <c>true</c> if the user input is valid, otherwise <c>false</c>
        /// </returns>
        /// <remarks>
        ///     This is used for both nickname and username validation.
        /// </remarks>
        public bool IsValidIrcNickname(string userInput, out string errorMsg)
        {
            errorMsg = "You must specify a valid name!";
            if (userInput.Length == 0 || userInput.Length > IrcManager.MaxNickLength) return false;
            if (!_validNickRegex.IsMatch(userInput)) return false;
            if (userInput.Contains(" ")) return false;

            errorMsg = string.Empty;
            return true;
        }

        /// <summary>
        ///     Determines whether the specified user input is a valid IRC server address.
        /// </summary>
        /// <param name="userInput">The user input.</param>
        /// <param name="errorMsg">The error message.</param>
        /// <returns>
        ///     <c>true</c> if the user input is valid, otherwise <c>false</c>
        /// </returns>
        public bool IsValidIrcServerAddress(string userInput, out string errorMsg)
        {
            errorMsg = "You must specify a valid IRC server address!";
            if (userInput.Length == 0) return false;
            if (userInput.Contains(" ")) return false;

            errorMsg = string.Empty;
            return true;
        }

        /// <summary>
        ///     Determines whether the specified user input is a valid IRC server password.
        /// </summary>
        /// <param name="userInput">The user input.</param>
        /// <param name="errorMsg">The error message.</param>
        /// <returns>
        ///     <c>true</c> if the user input is valid, otherwise <c>false</c>
        /// </returns>
        public bool IsValidIrcServerPassword(string userInput, out string errorMsg)
        {
            // IRC server password is not required
            //if (userInput.Length == 0) return false;

            errorMsg = string.Empty;
            return true;
        }

        /// <summary>
        ///     Determines whether the specified user input is a valid IRC server port.
        /// </summary>
        /// <param name="userInput">The user input.</param>
        /// <param name="errorMsg">The error message.</param>
        /// <returns>
        ///     <c>true</c> if the user input is valid, otherwise <c>false</c>
        /// </returns>
        public bool IsValidIrcServerPort(string userInput, out string errorMsg)
        {
            errorMsg = "You must specify a valid IRC server port!";
            if (userInput.Length == 0) return false;
            uint val;
            if (!uint.TryParse(userInput, out val) || val > 65535) return false;

            errorMsg = string.Empty;
            return true;
        }

        /// <summary>
        ///     Determines whether the specified user input is a valid QuakeNet Q password.
        /// </summary>
        /// <param name="userInput">The user input.</param>
        /// <param name="errorMsg">The error message.</param>
        /// <returns>
        ///     <c>true</c> if the user input is valid, otherwise <c>false</c>
        /// </returns>
        public bool IsValidQuakeNetPassword(string userInput, out string errorMsg)
        {
            // QuakeNet Q password is not required, but UI will prevent user from
            // enabling auto-auth without specifying Q password.
            errorMsg = string.Empty;
            return true;
        }

        /// <summary>
        ///     Determines whether the specified user input is a valid QuakeNet Q username.
        /// </summary>
        /// <param name="userInput">The user input.</param>
        /// <param name="errorMsg">The error message.</param>
        /// <returns>
        ///     <c>true</c> if the user input is valid, otherwise <c>false</c>
        /// </returns>
        public bool IsValidQuakeNetUsername(string userInput, out string errorMsg)
        {
            errorMsg = "QuakeNet (Q) username cannot contain spaces!";
            // QuakeNet Q username is not required, but UI will prevent user from
            // enabling auto-auth without specifying Q username.
            if (userInput.Contains(" ")) return false;
            errorMsg = string.Empty;
            return true;
        }
    }
}