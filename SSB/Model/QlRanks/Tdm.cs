namespace SSB.Model.QlRanks
{
    /// <summary>
    /// Model representing the team deathmatch rank and elo information returned from the QLRanks API
    /// </summary>
    public class Tdm
    {
        public int elo
        {
            get;
            set;
        }

        public int rank
        {
            get;
            set;
        }
    }
}