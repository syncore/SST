namespace SSB.Enum
{
    /// <summary>
    ///     An enumeration that specifies the different types of User database results.
    /// </summary>
    public enum UserDbResult
    {
        Unspecified,
        Success,
        UserAlreadyExists,
        UserDoesntExist,
        UserNotAddedBySender,
        InternalError
    }
}