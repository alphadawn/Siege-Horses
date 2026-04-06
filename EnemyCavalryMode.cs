namespace SiegeHorses
{
    /// <summary>
    /// Controls how enemy cavalry is handled during siege battles.
    /// </summary>
    public enum EnemyCavalryMode
    {
        /// <summary>Only the player's side gets horses; enemies are forced to infantry.</summary>
        PlayerOnly = 0,
        /// <summary>Both sides get full cavalry (default vanilla-like behavior when enabled).</summary>
        Full = 1,
        /// <summary>Enemy side only spawns horses for lords (heroes) and tier 5+ units.</summary>
        LordsAndElite = 2,
    }
}
