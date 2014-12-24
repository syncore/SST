using System;

namespace SSB.Util
{
    /// <summary>
    ///     Helper class for generating an expiration date to be used with various player banning operations.
    /// </summary>
    public static class ExpirationDateGenerator
    {
        /// <summary>
        ///     Generates a future expiration date based on a time period and time scale.
        /// </summary>
        /// <param name="timePeriod">The time period.</param>
        /// <param name="timeScale">The time scale.</param>
        /// <returns>The future date as a DateTime object.</returns>
        public static DateTime GenerateExpirationDate(double timePeriod, string timeScale)
        {
            var expirationDate = new DateTime();
            switch (timeScale)
            {
                case "sec":
                case "secs":
                    expirationDate = DateTime.Now.AddSeconds(timePeriod);
                    break;

                case "min":
                case "mins":
                    expirationDate = DateTime.Now.AddMinutes(timePeriod);
                    break;

                case "hour":
                case "hours":
                    expirationDate = DateTime.Now.AddHours(timePeriod);
                    break;

                case "day":
                case "days":
                    expirationDate = DateTime.Now.AddDays(timePeriod);
                    break;

                case "month":
                case "months":
                    // AddMonths(int months) and AddYears(int years) only accept int type; also already checked for
                    // overflow in Eval method
                    int monthAsInt = Convert.ToInt32(timePeriod);
                    expirationDate = DateTime.Now.AddMonths(monthAsInt);
                    break;

                case "year":
                case "years":
                    int yearAsInt = Convert.ToInt32(timePeriod);
                    expirationDate = DateTime.Now.AddYears(yearAsInt);
                    break;
            }

            return expirationDate;
        }
    }
}