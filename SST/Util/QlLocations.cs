namespace SST.Util
{
    /// <summary>
    /// Class for Quake Live location names.
    /// </summary>
    public class QlLocations
    {
        /// <summary>
        /// Gets the location name from the location_id identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns>The coutnry name as a string.</returns>
        public string GetLocationNameFromId(long id)
        {
            var locName = string.Empty;
            switch (id)
            {
                case 14:
                case 33:
                case 35:
                case 51:
                case 60:
                    locName = "AU";
                    break;

                case 29:
                case 34:
                case 36:
                case 54:
                    locName = "SE";
                    break;

                case 18:
                case 52:
                    locName = "DE";
                    break;

                case 17:
                case 61:
                case 64:
                    locName = "NL";
                    break;

                case 50:
                case 59:
                    locName = "IT";
                    break;

                case 40:
                    locName = "AR";
                    break;

                case 48:
                    locName = "BG";
                    break;

                case 66:
                    locName = "BR";
                    break;

                case 26:
                    locName = "CAN";
                    break;

                case 38:
                    locName = "CL";
                    break;

                case 31:
                    locName = "CN";
                    break;

                case 28:
                    locName = "ES";
                    break;

                case 20:
                    locName = "FR";
                    break;

                case 19:
                    locName = "UK";
                    break;

                case 41:
                    locName = "IS";
                    break;

                case 27:
                case 42:
                    locName = "JP";
                    break;

                case 49:
                    locName = "KR";
                    break;

                case 65:
                    locName = "NO";
                    break;

                case 68:
                    locName = "NZ";
                    break;

                case 30:
                case 32:
                    locName = "PL";
                    break;

                case 37:
                case 39:
                    locName = "RO";
                    break;

                case 43:
                case 44:
                case 69:
                    locName = "RU";
                    break;

                case 45:
                    locName = "SG";
                    break;

                case 47:
                    locName = "RS";
                    break;

                case 67:
                    locName = "TR";
                    break;

                case 58:
                    locName = "UA";
                    break;

                case 11:
                    locName = "VA,US";
                    break;

                case 22:
                    locName = "GA,US";
                    break;

                case 21:
                    locName = "IL,US";
                    break;

                case 6:
                case 12:
                case 53:
                case 666:
                    locName = "TX,US";
                    break;

                case 63:
                    locName = "IN,US";
                    break;

                case 10:
                case 16:
                case 25:
                case 70:
                    locName = "CA,US";
                    break;

                case 24:
                    locName = "NY,US";
                    break;

                case 23:
                    locName = "WA,US";
                    break;

                case 62:
                    locName = "DC,US";
                    break;

                case 46:
                    locName = "ZA";
                    break;
            }
            return locName;
        }
    }
}
