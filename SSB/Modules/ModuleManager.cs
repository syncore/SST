using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using SSB.Core;

namespace SSB.Modules
{
    /// <summary>
    ///     Class for managing SSB modules.
    /// </summary>
    public class ModuleManager
    {
        private const string MNameEloLimiter = "elolimiter";

        private readonly SynServerBot _ssb;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ModuleManager" /> class.
        /// </summary>
        public ModuleManager(SynServerBot ssb)
        {
            _ssb = ssb;
            ValidModules = new List<string> {MNameEloLimiter};
        }

        /// <summary>
        ///     Gets the Elo limiter module.
        /// </summary>
        /// <value>
        ///     The elo limiter module.
        /// </value>
        public EloLimiter ModEloLimiter { get; private set; }

        /// <summary>
        ///     Gets the name of the elo limiter module.
        /// </summary>
        /// <value>
        ///     The name of the elo limiter module.
        /// </value>
        public string ModNameEloLimiter
        {
            get { return MNameEloLimiter; }
        }

        /// <summary>
        ///     Gets the valid modules.
        /// </summary>
        /// <value>
        ///     The valid modules.
        /// </value>
        public List<string> ValidModules { get; private set; }

        /// <summary>
        ///     Gets the valid modules with status.
        /// </summary>
        /// <value>
        ///     The valid modules with status for use with QL say command.
        /// </value>
        public string ValidModulesWithStatus
        {
            get
            {
                var sb = new StringBuilder();
                foreach (string mod in ValidModules)
                {
                    sb.Append(IsModuleActive(mod)
                        ? string.Format("^5{0}^2++, ", mod)
                        : string.Format("^5{0}, ", mod));
                }
                return (sb.ToString(0, sb.Length - 2));
            }
        }

        /// <summary>
        ///     Gets the name of the module.
        /// </summary>
        /// <param name="moduleName">Name of the module.</param>
        /// <returns>The name of the module.</returns>
        public string GetModuleName(string moduleName)
        {
            string mod = string.Empty;
            switch (moduleName)
            {
                case MNameEloLimiter:
                    mod = MNameEloLimiter;
                    break;
            }
            return mod;
        }

        /// <summary>
        ///     Gets the type of module.
        /// </summary>
        /// <param name="moduleName">Name of the module.</param>
        /// <returns>The type of module.</returns>
        public Type GetModuleType(string moduleName)
        {
            Type t = null;
            switch (moduleName)
            {
                case MNameEloLimiter:
                    t = typeof (EloLimiter);
                    break;
            }
            return t;
        }

        /// <summary>
        ///     Determines whether whether the specified module is active.
        /// </summary>
        /// <param name="moduleName">The modulename.</param>
        /// <returns>
        ///     <c>true</c> if the module has been loaded and is active, otherwise <c>false</c>.
        /// </returns>
        public bool IsModuleActive(string moduleName)
        {
            bool active = false;
            switch (moduleName)
            {
                case MNameEloLimiter:
                    if (ModEloLimiter != null && ModEloLimiter.IsEnabled)
                    {
                        active = true;
                    }
                    break;
            }
            return active;
        }

        /// <summary>
        ///     Loads the specified module
        /// </summary>
        /// <param name="modtype">The module type.</param>
        public void Load(Type modtype)
        {
            if (modtype == typeof (EloLimiter))
            {
                if (IsModuleActive(ModNameEloLimiter)) return;
                ModEloLimiter = new EloLimiter(_ssb) {IsEnabled = true};
                Debug.WriteLine(string.Format(" *** LOADED: {0} module ***", ModNameEloLimiter));
            }
        }

        /// <summary>
        ///     Unloads the specified module.
        /// </summary>
        /// <param name="modtype">The module type.</param>
        public void Unload(Type modtype)
        {
            if (modtype == typeof (EloLimiter))
            {
                if (!IsModuleActive(ModNameEloLimiter)) return;
                ModEloLimiter.IsEnabled = false;
                ModEloLimiter = null;
                Debug.WriteLine(string.Format(" *** UNLOADED: {0} module ***", ModNameEloLimiter));
            }
        }
    }
}