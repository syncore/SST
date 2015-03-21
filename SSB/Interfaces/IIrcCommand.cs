﻿using System.Threading.Tasks;
using SSB.Enum;
using SSB.Model;

namespace SSB.Interfaces
{
    /// <summary>
    ///     Interface for IRC commands.
    /// </summary>
    public interface IIrcCommand
    {
        /// <summary>
        ///     Gets the minimum arguments for the IRC command.
        /// </summary>
        /// <value>
        ///     The minimum arguments for the IRC command.
        /// </value>
        int IrcMinArgs { get; }

        /// <summary>
        ///     Gets a value that determines whether this command is to be executed asynchronously.
        /// </summary>
        /// <returns>
        ///     <c>true</c> if this command is to be executed asynchronously,
        ///     otherwise <c>false</c>.
        /// </returns>
        bool IsAsync { get; }

        /// <summary>
        ///     Gets the user level.
        /// </summary>
        /// <value>
        ///     The user level.
        /// </value>
        IrcUserLevel UserLevel { get; }

        /// <summary>
        ///     Displays the argument length error.
        /// </summary>
        /// <param name="c">The cmd args.</param>
        void DisplayArgLengthError(CmdArgs c);

        /// <summary>
        ///     Executes the specified command.
        /// </summary>
        /// <param name="c">The cmd args.</param>
        void Exec(CmdArgs c);

        /// <summary>
        ///     Executes the specified command asynchronously.
        /// </summary>
        /// <param name="c">The cmd args.</param>
        Task ExecAsync(CmdArgs c);
    }
}