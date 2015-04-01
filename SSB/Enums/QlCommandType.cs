namespace SSB.Enums
{
    /// <summary>
    /// An enum representing various QL command types.
    /// </summary>
    public enum QlCommandType
    {
        Ignored,
        ServerInfoServerId,
        ServerInfoServerGametype,
        ServerInfoServerGamestate,
        ConfigStrings,
        Players,
        ServerInfo,
        // Note: InitInfo isn't a bot-issued ommand but occurs automatically in QL
        // when certain things occur - i.e. map loading, vid_restart, etc.
        InitInfo,
        CvarRequest
    }
}