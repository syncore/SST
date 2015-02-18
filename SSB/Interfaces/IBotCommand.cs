using System.Threading.Tasks;
using SSB.Enum;
using SSB.Model;

namespace SSB.Interfaces
{
    /// <summary>
    ///     Interface for bot commands.
    /// </summary>
    public interface IBotCommand
    {
        /// <summary>
        ///     Gets a value indicating whether this command can be accessed from IRC.
        /// </summary>
        /// <value>
        ///     <c>true</c> if this command can be accessed from IRC; otherwise, <c>false</c>.
        /// </value>
        bool IsIrcAccessAllowed { get; }

        /// <summary>
        ///     Gets the minimum arguments.
        /// </summary>
        /// <value>
        ///     The minimum arguments.
        /// </value>
        int MinArgs { get; }

        /// <summary>
        ///     Gets the user level.
        /// </summary>
        /// <value>
        ///     The user level.
        /// </value>
        UserLevel UserLevel { get; }

        /// <summary>
        ///     Displays the argument length error.
        /// </summary>
        Task DisplayArgLengthError(CmdArgs c);

        /// <summary>
        ///     Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The c.</param>
        Task ExecAsync(CmdArgs c);
    }
}