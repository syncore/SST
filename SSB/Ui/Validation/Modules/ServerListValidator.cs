namespace SSB.Ui.Validation.Modules
{
    /// <summary>
    ///     UI Validation class for the Modules: Server List tab.
    /// </summary>
    public class ServerListValidator
    {
        /// <summary>
        ///     Determines whether the specified user input is a valid maximum servers value.
        /// </summary>
        /// <param name="userInput">The user input.</param>
        /// <param name="errorMsg">The error message.</param>
        /// <returns>
        ///     <c>true</c> if the user input is valid, otherwise <c>false</c>
        /// </returns>
        public bool IsValidMaximumServersNum(string userInput, out string errorMsg)
        {
            if (userInput.Length == 0)
            {
                errorMsg =
                    "You must specify the maximum number of servers to display, as a number greater than zero!";
                return false;
            }

            int val;
            if (int.TryParse(userInput, out val))
            {
                if (val > 0)
                {
                    errorMsg = string.Empty;
                    return true;
                }
            }

            errorMsg = "The maximum number of servers to display must be a number greater than zero!";
            return false;
        }

        /// <summary>
        ///     Determines whether the specified user input is a valid value for the
        ///     time between server queries.
        /// </summary>
        /// <param name="userInput">The user input.</param>
        /// <param name="errorMsg">The error message.</param>
        /// <returns>
        ///     <c>true</c> if the user input is valid, otherwise <c>false</c>
        /// </returns>
        public bool IsValidTimeBetweenQueries(string userInput, out string errorMsg)
        {
            if (userInput.Length == 0)
            {
                errorMsg =
                    "You must specify the time between queries, in seconds, as a number zero or greater!";
                return false;
            }

            double val;
            if (double.TryParse(userInput, out val))
            {
                if (val >= 0)
                {
                    errorMsg = string.Empty;
                    return true;
                }
            }

            errorMsg = "The time between queries, in seconds, must be a number zero or greater!";
            return false;
        }
    }
}