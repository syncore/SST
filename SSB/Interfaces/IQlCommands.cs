namespace SSB
{
    /// <summary>
    ///     Interface for sending QL commands.
    /// </summary>
    internal interface IQlCommands
    {
        void SendToQl(string toSend, bool delay);
    }
}