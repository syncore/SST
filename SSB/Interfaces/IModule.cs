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
        /// Gets a value indicating whether this <see cref="IModule"/> is active.
        /// </summary>
        /// <value>
        ///   <c>true</c> if active; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// Used to query activity status for a list of modules. Be sure to set
        /// a public static bool property IsModuleActive for outside access in other parts of app.
        /// </remarks>
        bool Active { get; }

        /// <summary>
        ///     Gets the minimum arguments.
        /// </summary>
        /// <value>
        ///     The minimum arguments.
        /// </value>
        int MinModuleArgs { get; }

        /// <summary>
        /// Gets the name of the module.
        /// </summary>
        /// <value>
        /// The name of the module.
        /// </value>
        string ModuleName { get; }

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