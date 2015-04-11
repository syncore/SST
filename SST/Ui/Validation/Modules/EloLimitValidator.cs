namespace SST.Ui.Validation.Modules
{
    /// <summary>
    ///     UI Validation class for the Modules: Elo Limiter Banner tab.
    /// </summary>
    public class EloLimitValidator
    {
        /// <summary>
        ///     Determines whether the specified user input is a valid minimum Elo value.
        /// </summary>
        /// <param name="userInput">The user input.</param>
        /// <param name="errorMsg">The error message.</param>
        /// <returns>
        ///     <c>true</c> if the user input is valid, otherwise <c>false</c>
        /// </returns>
        public bool IsValidMinimumElo(string userInput, out string errorMsg)
        {
            errorMsg = "The minimum Elo must be a positive number!";
            if (userInput.Length == 0) return false;

            uint val;
            if (!uint.TryParse(userInput, out val)) return false;

            errorMsg = string.Empty;
            return true;
        }

        /// <summary>
        ///     Determines whether the specified user input is a valid maximum Elo value.
        /// </summary>
        /// <param name="userInput">The user input.</param>
        /// <param name="errorMsg">The error message.</param>
        /// <returns>
        ///     <c>true</c> if the user input is valid, otherwise <c>false</c>
        /// </returns>
        public bool IsValidMaximumElo(string userInput, out string errorMsg)
        {
            errorMsg = "The maximum Elo must be a positive number!";
            // Maximum elo can be empty, meaning that it is unset (Elo range isn't to be used).
            if (userInput.Length == 0)
            {
                errorMsg = string.Empty;
                return true;
            }

            int val;
            if (!int.TryParse(userInput, out val) || val <= 0) return false;

            errorMsg = string.Empty;
            return true;
        }
    }
}