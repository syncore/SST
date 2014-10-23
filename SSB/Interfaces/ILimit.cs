using System.Threading.Tasks;
using SSB.Model;

namespace SSB.Interfaces
{
    /// <summary>
    ///     Interface for limiting bot commands.
    /// </summary>
    internal interface ILimit
    {
        /// <summary>
        ///     Gets the minimum arguments.
        /// </summary>
        /// <value>
        ///     The minimum arguments.
        /// </value>
        int MinLimitArgs { get; }

        /// <summary>
        ///     Displays the argument length error.
        /// </summary>
        Task DisplayArgLengthError(CmdArgs c);

        /// <summary>
        ///     Executes the specified limiting command asynchronously.
        /// </summary>
        /// <param name="c">The c.</param>
        Task EvalLimitCmdAsync(CmdArgs c);
    }
}