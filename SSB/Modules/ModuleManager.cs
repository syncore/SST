using System;
using System.Collections.Generic;
using System.Text;
using SSB.Core;

namespace SSB.Modules
{
    /// <summary>
    ///     Class for managing SSB modules.
    /// </summary>
    public class ModuleManager
    {
        private const string MNameAccountDate = "accountdate";
        private const string MNameEloLimiter = "elolimiter";
        private readonly SynServerBot _ssb;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ModuleManager" /> class.
        /// </summary>
        public ModuleManager(SynServerBot ssb)
        {
            _ssb = ssb;
            ValidModules = new List<string> {MNameEloLimiter, MNameAccountDate};
        }

        /// <summary>
        ///     Gets the account date module.
        /// </summary>
        /// <value>
        ///     The account date module.
        /// </value>
        public AccountDate ModAccountDate { get; private set; }

        /// <summary>
        ///     Gets the Elo limiter module.
        /// </summary>
        /// <value>
        ///     The elo limiter module.
        /// </value>
        public EloLimiter ModEloLimiter { get; private set; }

        /// <summary>
        ///     Gets the name of the account date limit module.
        /// </summary>
        /// <value>
        ///     The name of the account date limit module.
        /// </value>
        public string ModNameAccountDate
        {
            get { return MNameAccountDate; }
        }

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

                case MNameAccountDate:
                    mod = MNameAccountDate;
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

                case MNameAccountDate:
                    t = typeof (AccountDate);
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

                case MNameAccountDate:
                    if (ModAccountDate != null && ModAccountDate.IsEnabled)
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
                if (IsModuleActive(MNameEloLimiter)) return;
                ModEloLimiter = new EloLimiter(_ssb) {IsEnabled = true};
            }
            if (modtype == typeof (AccountDate))
            {
                if (IsModuleActive(MNameAccountDate)) return;
                ModAccountDate = new AccountDate(_ssb) {IsEnabled = true};
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
                if (!IsModuleActive(MNameEloLimiter)) return;
                ModEloLimiter.IsEnabled = false;
                ModEloLimiter = null;
            }
            if (modtype == typeof (AccountDate))
            {
                if (!IsModuleActive(MNameAccountDate)) return;
                ModAccountDate.IsEnabled = false;
                ModAccountDate = null;
            }
        }
    }
}