namespace TimelessEchoes.Hero
{
    /// <summary>
    /// Defines behaviour presets for Echo helpers.
    /// </summary>
    public enum EchoType
    {
        /// <summary>
        ///     Echo focuses exclusively on combat and ignores task assignments.
        /// </summary>
        Combat,

        /// <summary>
        ///     Echo can both fight and complete any available non-combat task.
        /// </summary>
        All,

        /// <summary>
        ///     Echo only performs tasks and never engages in combat.
        /// </summary>
        TaskOnly,

        /// <summary>
        ///     Echo performs only the explicitly listed skills in
        ///     <see cref="TimelessEchoes.EchoSpawnConfig.capableSkills" />.
        /// </summary>
        Selective
    }
}
