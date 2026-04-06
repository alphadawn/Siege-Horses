using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;

namespace SiegeHorses
{
    internal sealed class SiegeHorsesSettings : AttributeGlobalSettings<SiegeHorsesSettings>
    {
        public override string Id => "SiegeHorses_v1";
        public override string DisplayName => "Siege Horses";
        public override string FolderName => "SiegeHorses";
        public override string FormatType => "json2";

        [SettingPropertyInteger("Cavalry Cap (Per Side)", 1, 200, RequireRestart = false,
            HintText = "Maximum number of cavalry units per side in siege battles. Only the highest tier troops get horses. Applies to both player and enemy armies (default: 50).")]
        [SettingPropertyGroup("General", GroupOrder = 0)]
        public int CavalryCap { get; set; } = 50;

        // ── Defender Settings ─────────────────────────────────────────────────────

        [SettingPropertyBool("Enable Defender Cavalry", RequireRestart = false,
            HintText = "Allow cavalry to be deployed when defending a siege.")]
        [SettingPropertyGroup("Defender", GroupOrder = 1)]
        public bool EnableDefenderCavalry { get; set; } = true;

        // ── Attacker Settings ─────────────────────────────────────────────────────

        [SettingPropertyBool("Enable Attacker Cavalry", RequireRestart = false,
            HintText = "Allow cavalry to be deployed when attacking a siege.")]
        [SettingPropertyGroup("Attacker", GroupOrder = 2)]
        public bool EnableAttackerCavalry { get; set; } = true;

        // ── Enemy Settings ────────────────────────────────────────────────────────

        [SettingPropertyBool("Enemy: Player Only", RequireRestart = false,
            HintText = "Only your troops get horses; enemies are forced to infantry.")]
        [SettingPropertyGroup("Enemy", GroupOrder = 3)]
        public bool EnemyCavalryPlayerOnly { get; set; } = false;

        [SettingPropertyBool("Enemy: Full Cavalry", RequireRestart = false,
            HintText = "Both sides get full cavalry (more horses = more wasted mounts on enemy AI).")]
        [SettingPropertyGroup("Enemy", GroupOrder = 3)]
        public bool EnemyCavalryFull { get; set; } = false;

        [SettingPropertyBool("Enemy: Lords & Elite Only", RequireRestart = false,
            HintText = "Enemy side only gets horses for heroes (lords) and tier 5+ units.")]
        [SettingPropertyGroup("Enemy", GroupOrder = 3)]
        public bool EnemyCavalryLordsAndElite { get; set; } = true;

        /// <summary>Determines the enemy cavalry mode based on which bool is active.</summary>
        public EnemyCavalryMode EnemyCavalryModeSetting
        {
            get
            {
                if (EnemyCavalryPlayerOnly) return EnemyCavalryMode.PlayerOnly;
                if (EnemyCavalryFull) return EnemyCavalryMode.Full;
                return EnemyCavalryMode.LordsAndElite;
            }
        }
    }
}
