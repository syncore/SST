using System;
using System.Collections.Generic;
using System.Diagnostics;
using SSB.Core;

namespace SSB.Modules
{
    /// <summary>
    /// Class for managing SSB modules.
    /// </summary>
    public class ModuleManager
    {
        private const string ModNameEloLimiter = "elolimiter";

        private readonly SynServerBot _ssb;
        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleManager"/> class.
        /// </summary>
        public ModuleManager(SynServerBot ssb)
        {
            _ssb = ssb;
            ValidModules = new List<string> { ModNameEloLimiter };
        }

        /// <summary>
        /// Gets the Elo limiter module.
        /// </summary>
        /// <value>
        /// The elo limiter module.
        /// </value>
        public EloLimiter ModEloLimiter { get; private set; }

        public List<string> ValidModules { get; private set; }

        /// <summary>
        /// Gets the name of the module.
        /// </summary>
        /// <param name="moduleName">Name of the module.</param>
        /// <returns>The name of the module.</returns>
        public string GetModuleName(string moduleName)
        {
            string mod = string.Empty;
            switch (moduleName)
            {
                case ModNameEloLimiter:
                    mod = ModNameEloLimiter;
                    break;
            }
            return mod;
        }

        /// <summary>
        /// Gets the type of module.
        /// </summary>
        /// <param name="moduleName">Name of the module.</param>
        /// <returns>The type of module.</returns>
        public Type GetModuleType(string moduleName)
        {
            Type t = null;
            switch (moduleName)
            {
                case ModNameEloLimiter:
                    t = typeof(EloLimiter);
                    break;
            }
            return t;
        }

        /// <summary>
        /// Determines whether whether the specified module is active.
        /// </summary>
        /// <param name="moduleName">The modulename.</param>
        /// <returns>
        ///   <c>true</c> if the module has been loaded and is active, otherwise <c>false</c>.
        /// </returns>
        public bool IsModuleActive(string moduleName)
        {
            bool active = false;
            switch (moduleName)
            {
                case ModNameEloLimiter:
                    if (ModEloLimiter != null && ModEloLimiter.IsEnabled)
                    {
                        active = true;
                    }
                    break;
            }
            return active;
        }

        /// <summary>
        /// Loads the specified module
        /// </summary>
        /// <param name="modtype">The module type.</param>
        public void Load(Type modtype)
        {
            if (modtype == typeof(EloLimiter))
            {
                if (IsModuleActive(ModNameEloLimiter)) return;
                ModEloLimiter = new EloLimiter(_ssb) { IsEnabled = true };
                Debug.WriteLine(string.Format(" *** LOADED: {0} module ***", ModNameEloLimiter));
            }
        }

        /// <summary>
        /// Unloads the specified module.
        /// </summary>
        /// <param name="modtype">The module type.</param>
        public void Unload(Type modtype)
        {
            if (modtype == typeof(EloLimiter))
            {
                if (!IsModuleActive(ModNameEloLimiter)) return;
                ModEloLimiter.IsEnabled = false;
                ModEloLimiter = null;
                Debug.WriteLine(string.Format(" *** UNLOADED: {0} module ***", ModNameEloLimiter));
            }
        }
    }
}