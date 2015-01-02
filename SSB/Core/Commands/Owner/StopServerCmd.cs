using System.Threading.Tasks;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Core.Commands.Owner
{
    /// <summary>
    ///     Command: Stop the current server.
    /// </summary>
    public class StopServerCmd : IBotCommand
    {
        private readonly SynServerBot _ssb;
        private int _minArgs = 2;
        private UserLevel _userLevel = UserLevel.Owner;

        /// <summary>
        ///     Initializes a new instance of the <see cref="StopServerCmd" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public StopServerCmd(SynServerBot ssb)
        {
            _ssb = ssb;
        }

        /// <summary>
        ///     Gets the minimum arguments.
        /// </summary>
        /// <value>
        ///     The minimum arguments.
        /// </value>
        public int MinArgs
        {
            get { return _minArgs; }
        }

        /// <summary>
        ///     Gets the user level.
        /// </summary>
        /// <value>
        ///     The user level.
        /// </value>
        public UserLevel UserLevel
        {
            get { return _userLevel; }
        }

        /// <summary>
        ///     Displays the argument length error.
        /// </summary>
        /// <param name="c"></param>
        public async Task DisplayArgLengthError(CmdArgs c)
        {
            await _ssb.QlCommands.QlCmdSay(string.Format(
                "^1[ERROR]^3 Usage: {0}{1} delay - delay is in seconds.",
                CommandProcessor.BotCommandPrefix, c.CmdName));
        }

        /// <summary>
        ///     Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The c.</param>
        public async Task ExecAsync(CmdArgs c)
        {
            int delay;
            bool delayIsNum = (int.TryParse(c.Args[1], out delay));
            if (delayIsNum)
            {
                await
                    _ssb.QlCommands.QlCmdSay(
                        string.Format(
                            "^1[ATTENTION] ^7This server will be shutting down in^1 ***{0}***^7 seconds. Thanks for playing!",
                            delay));
                
                // ReSharper disable once UnusedVariable
                var s = Task.Run(async delegate
                {
                    await Task.Delay(delay*1000);
                    await _ssb.QlCommands.SendToQlAsync("stopserver", false);
                });
            }
            else
            {
                await DisplayArgLengthError(c);
            }
        }
    }
}