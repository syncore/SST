﻿using System.Threading.Tasks;
using SST.Enums;
using SST.Model;

namespace SST.Interfaces
{
    /// <summary>
    /// Interface for bot commands.
    /// </summary>
    public interface IBotCommand
    {
        /// <summary>
        /// Gets the minimum arguments for the IRC command.
        /// </summary>
        /// <value>The minimum arguments for the IRC command.</value>
        int IrcMinArgs { get; }

        /// <summary>
        /// Gets a value indicating whether this command can be accessed from IRC.
        /// </summary>
        /// <value><c>true</c> if this command can be accessed from IRC; otherwise, <c>false</c>.</value>
        bool IsIrcAccessAllowed { get; }

        /// <summary>
        /// Gets the minimum arguments for the QL command.
        /// </summary>
        /// <value>The minimum arguments for the QL command.</value>
        int QlMinArgs { get; }

        /// <summary>
        /// Gets the command's status message.
        /// </summary>
        /// <value>The command's status message.</value>
        string StatusMessage { get; }

        /// <summary>
        /// Gets the user level.
        /// </summary>
        /// <value>The user level.</value>
        UserLevel UserLevel { get; }

        /// <summary>
        /// Displays the argument length error.
        /// </summary>
        Task DisplayArgLengthError(Cmd c);

        /// <summary>
        /// Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns><c>true</c> if the command was successfully executed, otherwise <c>false</c>.</returns>
        Task<bool> ExecAsync(Cmd c);

        /// <summary>
        /// Gets the argument length error message.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <returns>
        /// The argument length error message, correctly color-formatted depending on its destination.
        /// </returns>
        string GetArgLengthErrorMessage(Cmd c);

        /// <summary>
        /// Sends a QL say message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        Task SendServerSay(Cmd c, string message);

        /// <summary>
        /// Sends a QL tell message if the command was not sent from IRC.
        /// </summary>
        /// <param name="c">The command argument information.</param>
        /// <param name="message">The message.</param>
        Task SendServerTell(Cmd c, string message);
    }
}
