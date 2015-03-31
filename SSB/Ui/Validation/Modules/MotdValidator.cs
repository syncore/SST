using SSB.Core.Commands.Modules;

namespace SSB.Ui.Validation.Modules
{
    /// <summary>
    ///     UI Validation class for the Modules: MOTD tab.
    /// </summary>
    public class MotdValidator
    {
        /// <summary>
        ///     Determines whether the specified user input is a valid repeat message.
        /// </summary>
        /// <param name="userInput">The user input.</param>
        /// <param name="errorMsg">The error message.</param>
        /// <returns>
        ///     <c>true</c> if the user input is valid, otherwise <c>false</c>
        /// </returns>
        public bool IsValidRepeatMessage(string userInput, out string errorMsg)
        {
            errorMsg = "You must specify the message to repeat!";
            if (userInput.Length == 0) return false;

            errorMsg = string.Empty;
            return true;
        }

        /// <summary>
        ///     Determines whether the specified user input is a valid repeat time value.
        /// </summary>
        /// <param name="userInput">The user input.</param>
        /// <param name="errorMsg">The error message.</param>
        /// <returns>
        ///     <c>true</c> if the user input is valid, otherwise <c>false</c>
        /// </returns>
        public bool IsValidRepeatTime(string userInput, out string errorMsg)
        {
            errorMsg =
                string.Format("You must specify the repeat time in minutes, as a number greater than {0}",
                    Motd.MinRepeatThresholdStart);
            if (userInput.Length == 0) return false;

            int val;
            if (!int.TryParse(userInput, out val) || val <= Motd.MinRepeatThresholdStart) return false;

            errorMsg = string.Empty;
            return true;
        }
    }
}