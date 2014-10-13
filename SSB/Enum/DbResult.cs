namespace SSB.Enum
{
    /// <summary>
    ///     An enumeration that specifies the different types of database results.
    /// </summary>
    public enum DbResult
    {
        Unspecified,
        Success,
        UserAlreadyExists,
        UserDoesntExist,
        UserNotAddedBySender,
        InternalError
    }
}