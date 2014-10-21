namespace SSB.Model
{
    /// <summary>
    ///     Struct for bot commands.
    /// </summary>
    public struct CmdArgs
    {
        public string[] Args;
        public string CmdName;
        public string FromUser;

        /// <summary>
        ///     Initializes a new instance of the <see cref="CmdArgs" /> struct.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <param name="cmdName">Name of the command.</param>
        /// <param name="fromUser">The user who sent the command.</param>
        public CmdArgs(string[] args, string cmdName, string fromUser)
        {
            Args = args;
            CmdName = cmdName;
            FromUser = fromUser;
        }
    }
}