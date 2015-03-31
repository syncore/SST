using SSB.Core.Commands.Modules;

namespace SSB.Ui.Validation.Modules
{
    /// <summary>
    ///     UI Validation class for the Modules: Pickup tab.
    /// </summary>
    public class PickupValidator
    {
        /// <summary>
        ///     Determines whether the specified user input is a valid maximum no-show value.
        /// </summary>
        /// <param name="userInput">The user input.</param>
        /// <param name="errorMsg">The error message.</param>
        /// <returns>
        ///     <c>true</c> if the user input is valid, otherwise <c>false</c>
        /// </returns>
        public bool IsValidMaximumNoShowNum(string userInput, out string errorMsg)
        {
            errorMsg = "The maximum number of no-shows before ban must be a number greater than zero!";
            if (userInput.Length == 0) return false;

            int val;
            if (!int.TryParse(userInput, out val) || val <= 0) return false;
            errorMsg = string.Empty;
            return true;
        }

        /// <summary>
        ///     Determines whether the specified user input is a valid maximum subs value.
        /// </summary>
        /// <param name="userInput">The user input.</param>
        /// <param name="errorMsg">The error message.</param>
        /// <returns>
        ///     <c>true</c> if the user input is valid, otherwise <c>false</c>
        /// </returns>
        public bool IsValidMaximumSubsNum(string userInput, out string errorMsg)
        {
            errorMsg = "The maximum number of subs before ban must be a number greater than zero!";
            if (userInput.Length == 0) return false;

            int val;
            if (!int.TryParse(userInput, out val) || val <= 0) return false;
            errorMsg = string.Empty;
            return true;
        }

        /// <summary>
        ///     Determines whether the specified user input is a valid pickup team size value.
        /// </summary>
        /// <param name="userInput">The user input.</param>
        /// <param name="errorMsg">The error message.</param>
        /// <returns>
        ///     <c>true</c> if the user input is valid, otherwise <c>false</c>
        /// </returns>
        public bool IsValidPickupTeamSize(string userInput, out string errorMsg)
        {
            errorMsg = string.Format("The team size must be a number >= {0} but <= {1}",
                Pickup.TeamMinSize, Pickup.TeamMaxSize);
            if (userInput.Length == 0) return false;

            int val;
            if (!int.TryParse(userInput, out val)) return false;
            if (val < Pickup.TeamMinSize || val > Pickup.TeamMaxSize) return false;
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