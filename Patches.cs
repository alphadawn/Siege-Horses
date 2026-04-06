using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SiegeHorses
{
    // ─────────────────────────────────────────────────────────────────────────────
    //  STATIC FLAG
    //  Set from the game menu before an assault; cleared in OnMissionEnded.
    // ─────────────────────────────────────────────────────────────────────────────

    public static class SiegeHorsesFlag
    {
        /// <summary>True when the player has chosen to bring cavalry to the assault.</summary>
        public static bool GoWithCavalry { get; set; } = false;

        /// <summary>True when the flag was set from the defender menu (not attacker).</summary>
        public static bool IsDefending { get; set; } = false;

        /// <summary>Gets the cavalry cap from MCM settings (defaults to 50).</summary>
        public static int CavalryCap => SiegeHorsesSettings.Instance.CavalryCap;

        /// <summary>Checks if defender cavalry is enabled in MCM settings.</summary>
        public static bool IsDefenderCavalryEnabled => SiegeHorsesSettings.Instance.EnableDefenderCavalry;

        /// <summary>Checks if attacker cavalry is enabled in MCM settings.</summary>
        public static bool IsAttackerCavalryEnabled => SiegeHorsesSettings.Instance.EnableAttackerCavalry;

        /// <summary>Gets the enemy cavalry mode from MCM settings.</summary>
        public static EnemyCavalryMode EnemyCavalryMode => SiegeHorsesSettings.Instance.EnemyCavalryModeSetting;
    }

    // ─────────────────────────────────────────────────────────────────────────────
    //  PATCH 1 — AgentBuildData.NoHorses
    //  Intercepts the builder call that prevents cavalry from spawning.
    //  When our flag is active, we override the "no horses" instruction to false
    //  so mounted troops receive their horses during siege battles.
    //
    //  The cavalry cap is enforced post-spawn by SiegeCavalryMissionBehavior
    //  (removes excess mounts after all agents are fully initialized).
    //
    //  Enemy mode is respected: PlayerOnly blocks enemy horses, Full allows all,
    //  LordsAndElite only allows heroes and tier 5+.
    // ─────────────────────────────────────────────────────────────────────────────

    [HarmonyPatch(typeof(AgentBuildData), nameof(AgentBuildData.NoHorses))]
    internal static class NoHorsesPatch
    {
        static void Prefix(AgentBuildData __instance, ref bool noHorses)
        {
            if (!noHorses) return;
            if (!SiegeHorsesFlag.GoWithCavalry) return;

            // Allow all horses during siege. The SiegeCavalryMissionBehavior will
            // enforce the cap and enemy mode after agents are fully spawned.
            noHorses = false;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────────
    //  PATCH 2 — SandBoxHelpers.MissionHelper.SpawnPlayer
    //  The player character also goes through SpawnPlayer which receives a
    //  `noHorses` parameter.  We intercept it for the battle-equipment path
    //  (civilianEquipment == false) so the player can ride during the siege assault.
    //
    //  Applied lazily via reflection (same technique as paradeMod's
    //  SpawnPlayerHorsePatch) because SandBox.dll is not loaded at OnSubModuleLoad.
    // ─────────────────────────────────────────────────────────────────────────────

    internal static class SpawnPlayerSiegePatch
    {
        private static bool _applied = false;

        internal static void TryApply(Harmony harmony)
        {
            if (_applied) return;
            _applied = true;

            Type type;
            try
            {
                type = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
                    .FirstOrDefault(t =>
                    {
                        try { return t.FullName == "SandBox.SandBoxHelpers+MissionHelper"; }
                        catch { return false; }
                    });
            }
            catch { return; }

            if (type == null) return;

            MethodInfo[] allMethods;
            try { allMethods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic); }
            catch { return; }

            var prefix = new HarmonyMethod(
                typeof(SpawnPlayerSiegePatch).GetMethod(
                    nameof(Prefix), BindingFlags.Static | BindingFlags.NonPublic));

            foreach (MethodInfo m in allMethods.Where(m => m.Name == "SpawnPlayer"))
            {
                try { harmony.Patch(m, prefix: prefix); }
                catch { }
            }
        }

        // Called before SpawnPlayer for every call-site.
        // We only care about battle-equipment spawns (not civilian town/castle scenes)
        // to avoid interfering with paradeMod's own town-horse logic.
        static void Prefix(ref bool noHorses, bool civilianEquipment)
        {
            if (civilianEquipment) return;                    // leave civilian spawns alone
            if (!SiegeHorsesFlag.GoWithCavalry) return;      // our flag must be active

            // Confirm the player actually has a horse in their battle equipment
            Hero hero = Hero.MainHero;
            if (hero == null) return;
            if (hero.BattleEquipment[EquipmentIndex.Horse].Item == null) return;

            noHorses = false;
        }
    }
}
