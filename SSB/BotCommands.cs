using System.Collections.Generic;

namespace SSB
{
    /// <summary>
    /// Class that represents the bot commands.
    /// </summary>
    public class BotCommands
    {

        private readonly SynServerBot _ssb;
        private const string HelpCmd = "!help";
        private const string LevelCmd = "!level";


        /// <summary>
        /// Initializes a new instance of the <see cref="BotCommands"/> class.
        /// </summary>
        public BotCommands(SynServerBot ssb)
        {
            _ssb = ssb;
            AllBotCommands = new List<string> { HelpCmd, LevelCmd };
        }

        /// <summary>
        /// Gets all of the bot commands.
        /// </summary>
        /// <value>
        /// All of the bot commands
        /// </value>
        public List<string> AllBotCommands { get; private set; }

        /// <summary>
        /// Processes the bot command.
        /// </summary>
        /// <param name="command">The command.</param>
        public void ProcessBotCommand(string command)
        {
            if (command.Equals(HelpCmd))
            {
                ExecHelpCmd();
            }
            if (command.Equals(LevelCmd))
            {
                ExecLevelCmd();
            }

        }

        /// <summary>
        /// Executes the !help command.
        /// </summary>
        private void ExecHelpCmd()
        {
            _ssb.QlCommands.SendToQl("^3The ^7!help ^3command will go here.", false);
        }

        /// <summary>
        /// Executes the !level command.
        /// </summary>
        private void ExecLevelCmd()
        {
            _ssb.QlCommands.SendToQl("^3The ^7!level ^3command will go here.", false);
        }
    }
}