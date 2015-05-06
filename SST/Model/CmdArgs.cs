namespace SST.Model
{
    /// <summary>
    ///     Struct for bot commands.
    /// </summary>
    public struct CmdArgs
    {
        public string[] Args;
        public string CmdName;
        public bool FromIrc;
        public string FromUser;
        public string Text;

        /// <summary>
        /// Initializes a new instance of the <see cref="CmdArgs" /> struct.
        /// </summary>
        /// <param name="args">The arguments.</param>
        /// <param name="cmdName">Name of the command.</param>
        /// <param name="fromUser">The user who sent the command.</param>
        /// <param name="text">The full text.</param>
        /// <param name="fromIrc">if set to <c>true</c> then the message was sent from IRC.</param>
        public CmdArgs(string[] args, string cmdName, string fromUser, string text, bool fromIrc)
        {
            Args = args;
            CmdName = cmdName;
            FromUser = fromUser;
            Text = text;
            FromIrc = fromIrc;
        }
    }
}