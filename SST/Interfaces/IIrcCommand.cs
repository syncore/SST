using System.Threading.Tasks;
using SST.Enums;
using SST.Model;

namespace SST.Interfaces
{
    /// <summary>
    /// Interface for IRC commands.
    /// </summary>
    public interface IIrcCommand
    {
        /// <summary>
        /// Gets the minimum arguments for the IRC command.
        /// </summary>
        /// <value>The minimum arguments for the IRC command.</value>
        int IrcMinArgs { get; }

        /// <summary>
        /// Gets a value that determines whether this command is to be executed asynchronously.
        /// </summary>
        /// <returns>
        /// <c>true</c> if this command is to be executed asynchronously, otherwise <c>false</c>.
        /// </returns>
        bool IsAsync { get; }

        /// <summary>
        /// Gets a value indicating whether this command requires the bot to be monitoring a server
        /// before it can be used.
        /// </summary>
        /// <value>
        /// <c>true</c> if this command requires the bot to be monitoring a server; otherwise, <c>false</c>.
        /// </value>
        bool RequiresMonitoring { get; }

        /// <summary>
        /// Gets the user level.
        /// </summary>
        /// <value>The user level.</value>
        IrcUserLevel UserLevel { get; }

        /// <summary>
        /// Displays the argument length error.
        /// </summary>
        /// <param name="c">The cmd args.</param>
        void DisplayArgLengthError(Cmd c);

        /// <summary>
        /// Executes the specified command.
        /// </summary>
        /// <param name="c">The cmd args.</param>
        /// <returns>
        /// <c>true</c> if the command was successfully executed, otherwise returns <c>false</c>.
        /// </returns>
        bool Exec(Cmd c);

        /// <summary>
        /// Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The cmd args.</param>
        /// <returns>
        /// <c>true</c> if the command was successfully executed, otherwise returns <c>false</c>.
        /// </returns>
        Task<bool> ExecAsync(Cmd c);
    }
}
