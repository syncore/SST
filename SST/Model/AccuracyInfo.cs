namespace SST.Model
{
    /// <summary>
    ///     Model class for players' weapon accuracies.
    /// </summary>
    public class AccuracyInfo
    {
        /// <summary>
        ///     Gets or sets the BFG accuracy.
        /// </summary>
        /// <value>
        ///     The BFG accuracy.
        /// </value>
        public int Bfg { get; set; }

        /// <summary>
        ///     Gets or sets the chain gun accuracy.
        /// </summary>
        /// <value>
        ///     The chain gun accuracy.
        /// </value>
        public int ChainGun { get; set; }

        /// <summary>
        ///     Gets or sets the grappling hook accuracy.
        /// </summary>
        /// <value>
        ///     The grappling hook accuracy.
        /// </value>
        public int GrapplingHook { get; set; }

        /// <summary>
        ///     Gets or sets the grenade launcher accuracy.
        /// </summary>
        /// <value>
        ///     The grenade launcher accuracy.
        /// </value>
        public int GrenadeLauncher { get; set; }

        /// <summary>
        ///     Gets or sets the heavy machine gun accuracy.
        /// </summary>
        /// <value>
        ///     The heavy machine gun accuracy.
        /// </value>
        public int HeavyMachineGun { get; set; }

        /// <summary>
        ///     Gets or sets the lightning gun accuracy.
        /// </summary>
        /// <value>
        ///     The lightning gun accuracy.
        /// </value>
        public int LightningGun { get; set; }

        /// <summary>
        ///     Gets or sets the machine gun accuracy.
        /// </summary>
        /// <value>
        ///     The machine gun accuracy.
        /// </value>
        public int MachineGun { get; set; }

        /// <summary>
        ///     Gets or sets the nail gun accuracy.
        /// </summary>
        /// <value>
        ///     The nail gun accuracy.
        /// </value>
        public int NailGun { get; set; }

        /// <summary>
        ///     Gets or sets the plasma gun accuracy.
        /// </summary>
        /// <value>
        ///     The plasma gun accuracy.
        /// </value>
        public int PlasmaGun { get; set; }

        /// <summary>
        ///     Gets or sets the proximity mine launcher accuracy.
        /// </summary>
        /// <value>
        ///     The proximity mine launcher accuracy.
        /// </value>
        public int ProximityMineLauncher { get; set; }

        /// <summary>
        ///     Gets or sets the rail gun accuracy.
        /// </summary>
        /// <value>
        ///     The rail gun accuracy.
        /// </value>
        public int RailGun { get; set; }

        /// <summary>
        ///     Gets or sets the rocket launcher accuracy.
        /// </summary>
        /// <value>
        ///     The rocket launcher accuracy.
        /// </value>
        public int RocketLauncher { get; set; }

        /// <summary>
        ///     Gets or sets the shotgun accuracy.
        /// </summary>
        /// <value>
        ///     The shotgun accuracy.
        /// </value>
        public int ShotGun { get; set; }

        /// <summary>
        ///     Determines whether this instance has accuracy data.
        /// </summary>
        /// <returns><c>true</c> if any value is not equal to the default, otherwise <c>false</c>.</returns>
        public bool HasAcc()
        {
            return Bfg != 0 || ChainGun != 0 || GrapplingHook != 0 || GrenadeLauncher != 0
                   || HeavyMachineGun != 0 || LightningGun != 0 || MachineGun != 0 || NailGun != 0 ||
                   PlasmaGun != 0 || ProximityMineLauncher != 0 || RailGun != 0 || RocketLauncher != 0
                   || ShotGun != 0;
        }
    }
}