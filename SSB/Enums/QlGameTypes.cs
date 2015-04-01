namespace SSB.Enums
{
    /// <summary>
    ///     Enum representing the various types of server game modes.
    /// </summary>
    public enum QlGameTypes
    {
        // error or default
        Unspecified = -1,
        Ffa = 0,
        Duel = 1,
        Race = 2,
        Tdm = 3,
        Ca = 4,
        Ctf = 5,
        OneFlagCtf = 6,
        // note there is no 7 type
        Harvester = 8,
        FreezeTag = 9,
        Domination = 10,
        AttackDefend = 11,
        RedRover = 12,
        
    }
}