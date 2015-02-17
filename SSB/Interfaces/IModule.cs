using System.Threading.Tasks;
using SSB.Model;

namespace SSB.Interfaces
{
    /// <summary>
    ///     Interface for module commands.
    /// </summary>
    public interface IModule
    {
        /// <summary>
        ///     Gets a value indicating whether this <see cref="IModule" /> is active.
        /// </summary>
        /// <value>
        ///     <c>true</c> if active; otherwise, <c>false</c>.
        /// </value>
        bool Active { get; set; }

        /// <summary>
        ///     Gets the minimum arguments.
        /// </summary>
        /// <value>
        ///     The minimum arguments.
        /// </value>
        int MinModuleArgs { get; }

        /// <summary>
        ///     Gets the name of the module.
        /// </summary>
        /// <value>
        ///     The name of the module.
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

        /// <summary>
        /// Loads the configuration.
        /// </summary>
        void LoadConfig();

        /// <summary>
        ///     Updates the configuration.
        /// </summary>
        /// <param name="active">
        ///     if set to <c>true</c> then the module is to remain active; otherwise it is to be disabled when
        ///     updating the configuration.
        /// </param>
        void UpdateConfig(bool active);
    }
}