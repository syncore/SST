using System.Threading.Tasks;
using SSB.Model;

namespace SSB.Interfaces
{
    /// <summary>
    ///     Interface for module commands.
    /// </summary>
    internal interface IModule
    {
        /// <summary>
        ///     Gets the minimum arguments.
        /// </summary>
        /// <value>
        ///     The minimum arguments.
        /// </value>
        int MinModuleArgs { get; }

        /// <summary>
        ///     Displays the argument length error.
        /// </summary>
        Task DisplayArgLengthError(CmdArgs c);

        /// <summary>
        ///     Executes the specified module command asynchronously.
        /// </summary>
        /// <param name="c">The c.</param>
        Task EvalModuleCmdAsync(CmdArgs c);
    }
}