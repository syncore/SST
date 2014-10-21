using System.Threading.Tasks;
using SSB.Enum;
using SSB.Model;

namespace SSB.Interfaces
{
    /// <summary>
    ///     Interface for bot commands.
    /// </summary>
    internal interface IBotCommand
    {
        /// <summary>
        ///     Gets a value indicating whether the command is to be executed asynchronously or not.
        /// </summary>
        /// <value>
        ///     <c>true</c> the command is to be executed asynchronously; otherwise, <c>false</c>.
        /// </value>
        bool HasAsyncExecution { get; }

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
        void DisplayArgLengthError(CmdArgs c);

        /// <summary>
        ///     Executes the specified command.
        /// </summary>
        /// <param name="c">The command args</param>
        void Exec(CmdArgs c);

        /// <summary>
        ///     Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The c.</param>
        Task ExecAsync(CmdArgs c);
    }
}