using System;
using System.Threading.Tasks;
using SSB.Database;
using SSB.Enum;
using SSB.Interfaces;
using SSB.Model;

namespace SSB.Core.Commands.Admin
{
    /// <summary>
    ///     Command: Add a user to the bot's internal user database.
    /// </summary>
    public class AddUserCmd : IBotCommand
    {
        private readonly bool _isIrcAccessAllowed = true;
        private readonly int _minArgs = 2;
        private readonly SynServerBot _ssb;
        private readonly UserLevel _userLevel = UserLevel.Admin;
        private readonly DbUsers _users;

        /// <summary>
        ///     Initializes a new instance of the <see cref="AddUserCmd" /> class.
        /// </summary>
        /// <param name="ssb">The main class.</param>
        public AddUserCmd(SynServerBot ssb)
        {
            _ssb = ssb;
            _users = new DbUsers();
        }

        /// <summary>
        ///     Gets a value indicating whether this command can be accessed from IRC.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this command can be accessed from IRC; otherwise, <c>false</c>.
        /// </value>
        public bool IsIrcAccessAllowed
        {
            get { return _isIrcAccessAllowed; }
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
        /// <param name="c">The command args</param>
        public async Task DisplayArgLengthError(CmdArgs c)
        {
            await _ssb.QlCommands.QlCmdTell(string.Format(
                "^1[ERROR]^3 Usage: {0}{1} name access#^7 - name is without clantag. access #s are: 1(user), 2(superuser), 3(admin)",
                CommandProcessor.BotCommandPrefix, c.CmdName), c.FromUser);
        }

        /// <summary>
        ///     Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The c.</param>
        /// <remarks>
        ///     c.Args[1]: userToAdd, c.Args[2]: accessLevel
        /// </remarks>
        public async Task ExecAsync(CmdArgs c)
        {
            if (!c.Args[2].Equals("1") && !c.Args[2].Equals("2") && !c.Args[2].Equals("3"))
            {
                await DisplayArgLengthError(c);
                return;
            }
            if ((c.Args[2].Equals("3")) && ((_users.GetUserLevel(c.FromUser) != UserLevel.Owner)))
            {
                await _ssb.QlCommands.QlCmdSay(string.Format("^1[ERROR]^7 Only owners can add admins."));
                return;
            }
            var date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var result = _users.AddUserToDb(c.Args[1], (UserLevel) Convert.ToInt32(c.Args[2]), c.FromUser,
                date);
            if (result == UserDbResult.Success)
            {
                await _ssb.QlCommands.QlCmdSay(
                    string.Format("^2[SUCCESS]^7 Added user^2 {0} ^7to the^2 [{1}] ^7group.", c.Args[1],
                        (UserLevel) Convert.ToInt32(c.Args[2])));
            }
            else
            {
                await _ssb.QlCommands.QlCmdSay(
                    string.Format(
                        "^1[ERROR]^7 Unable to add user^1 {0}^7 to the^1 [{1}] ^7group. Code:^1 {2}",
                        c.Args[1], (UserLevel) Convert.ToInt32(c.Args[2]), result));
            }
        }
    }
}