namespace SST.Ui.Validation.Modules
{
    /// <summary>
    /// UI Validation class for the Modules: Account Date Limiter tab.
    /// </summary>
    public class AccountDateLimitValidator
    {
        /// <summary>
        /// Determines whether the specified user input is a valid minimum account age value.
        /// </summary>
        /// <param name="userInput">The user input.</param>
        /// <param name="errorMsg">The error message.</param>
        /// <returns><c>true</c> if the user input is valid, otherwise <c>false</c></returns>
        public bool IsValidMinimumAccountAge(string userInput, out string errorMsg)
        {
            if (userInput.Length == 0)
            {
                errorMsg = "You must specify the minimum account age in days, as a number greater than zero!";
                return false;
            }

            uint val;
            if (uint.TryParse(userInput, out val))
            {
                if (val != 0)
                {
                    errorMsg = string.Empty;
                    return true;
                }
            }

            errorMsg = "The minimum account age (in days) must be a number greater than zero!";
            return false;
        }
    }
}
