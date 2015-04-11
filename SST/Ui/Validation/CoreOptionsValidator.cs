using SST.Util;

namespace SST.Ui.Validation
{
    /// <summary>
    ///     UI Validation class for the Core Options tab.
    /// </summary>
    public class CoreOptionsValidator
    {
        /// <summary>
        ///     Determines whether the specified user input is a valid elo cache expiration value.
        /// </summary>
        /// <param name="userInput">The user input.</param>
        /// <param name="errorMsg">The error message.</param>
        /// <returns>
        ///     <c>true</c> if the user input is valid, otherwise <c>false</c>
        /// </returns>
        public bool IsValidEloCacheExpiration(string userInput, out string errorMsg)
        {
            errorMsg = "Elo cache expiration value must be a positive number.";
            if (userInput.Length == 0) return false;

            uint val;
            if (!uint.TryParse(userInput, out val)) return false;

            errorMsg = string.Empty;
            return true;
        }

        /// <summary>
        ///     Determines whether the specified user input is a valid Quake Live account name.
        /// </summary>
        /// <param name="userInput">The user input.</param>
        /// <param name="errorMsg">The error message.</param>
        /// <returns>
        ///     <c>true</c> if the user input is valid, otherwise <c>false</c>
        /// </returns>
        public bool IsValidQuakeLiveName(string userInput, out string errorMsg)
        {
            if (userInput.Length == 0)
            {
                errorMsg = "You must specify the name of the QL account. Do not include the clan tag!";
                return false;
            }

            if (Helpers.IsValidQlUsernameFormat(userInput, false))
            {
                errorMsg = string.Empty;
                return true;
            }

            errorMsg = "Invalid characters detected in QL account name.";
            return false;
        }

        /// <summary>
        ///     Determines whether the specified user input is a valid time between
        ///     user commands value.
        /// </summary>
        /// <param name="userInput">The user input.</param>
        /// <param name="errorMsg">The error message.</param>
        /// <returns>
        ///     <c>true</c> if the user input is valid, otherwise <c>false</c>
        /// </returns>
        public bool IsValidTimeBetweenCommands(string userInput, out string errorMsg)
        {
            errorMsg = "Time limit between user commands value must be a positive number.";
            if (userInput.Length == 0) return false;

            double val;
            if (!double.TryParse(userInput, out val) || val < 0.0) return false;

            errorMsg = string.Empty;
            return true;
        }
    }
}