namespace SSB
{
    public class PlayerInfo
    {
        // configstring ctor
        public PlayerInfo(string name, Team team, string id)
        {
            Name = name;
            Team = team;
            Id = id;
        }

        // players cmd ctor
        public PlayerInfo(string name, string id)
        {
            Name = name;
            Id = id;
        }

        public string Id { get; set; }

        public string Name { get; set; }

        public Team Team { get; set; }
    }
}