namespace SST.Enums
{
    /// <summary>
    ///     An enum that represents the types of users in an IRC channel.
    /// </summary>
    public enum IrcUserLevel
    {
        None,
        Voice, // +
        Operator, // @
        // Admin and owner are not applicable to QuakeNet
        Admin, // &
        Owner // ~
    }
}