namespace SST.Ui.Validation.Modules
{
    /// <summary>
    ///     UI Validation class for the Modules: Early Quit Banner tab.
    /// </summary>
    public class EarlyQuitValidator
    {
        /// <summary>
        ///     Determines whether the specified user input is a valid maximum number of quits value.
        /// </summary>
        /// <param name="userInput">The user input.</param>
        /// <param name="errorMsg">The error message.</param>
        /// <returns>
        ///     <c>true</c> if the user input is valid, otherwise <c>false</c>
        /// </returns>
        public bool IsValidMaximumNumQuits(string userInput, out string errorMsg)
        {
            errorMsg = "The maximum number of quits must be a positive number!";
            if (userInput.Length == 0) return false;

            uint val;
            if (!uint.TryParse(userInput, out val)) return false;

            errorMsg = string.Empty;
            return true;
        }

        /// <summary>
        ///     Determines whether the specified user input is a valid time to ban value.
        /// </summary>
        /// <param name="userInput">The user input.</param>
        /// <param name="errorMsg">The error message.</param>
        /// <returns>
        ///     <c>true</c> if the user input is valid, otherwise <c>false</c>
        /// </returns>
        public bool IsValidTimeBanNum(string userInput, out string errorMsg)
        {
            errorMsg = "The time to ban number must be greater than zero!";
            if (userInput.Length == 0) return false;

            double val;
            if (!double.TryParse(userInput, out val) || (val <= 0)) return false;

            errorMsg = string.Empty;
            return true;
        }
    }
}