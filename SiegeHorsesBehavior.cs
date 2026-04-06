using TaleWorlds.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace SiegeHorses
{
    public class SiegeHorsesBehavior : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.OnMissionEndedEvent.AddNonSerializedListener(this, OnMissionEnded);
        }

        public override void SyncData(IDataStore dataStore) { }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            // Attacker menus — "menu_siege_strategies" is the confirmed literal constant in the DLL.
            AddAttackerOptions(starter, "menu_siege_strategies");
            AddAttackerOptions(starter, "siege_strategies");
            AddEncounterAttackerOptions(starter);

            // Defender menus — "defender_siege" is confirmed from DLL method names.
            AddDefenderOptions(starter, "defender_siege");
            AddEncounterDefenderOptions(starter);
        }

        private static void AddAttackerOptions(CampaignGameStarter starter, string menuId)
        {
            try
            {
                starter.AddGameMenuOption(
                    menuId,
                    "sh_cavalry_enable_" + menuId,
                    "Bring cavalry to the assault",
                    args =>
                    {
                        if (!SiegeHorsesFlag.IsAttackerCavalryEnabled) return false;
                        if (SiegeHorsesFlag.GoWithCavalry) return false;
                        args.optionLeaveType = GameMenuOption.LeaveType.Wait;
                        return true;
                    },
                    args =>
                    {
                        SiegeHorsesFlag.GoWithCavalry = true;
                        SiegeHorsesFlag.IsDefending = false;
                        InformationManager.DisplayMessage(new InformationMessage(
                            "Cavalry will join the assault.", Colors.Green));
                        GameMenu.SwitchToMenu(menuId);
                    },
                    false, 4, false);

                starter.AddGameMenuOption(
                    menuId,
                    "sh_cavalry_disable_" + menuId,
                    "Cancel cavalry assault [cavalry ENABLED]",
                    args =>
                    {
                        if (!SiegeHorsesFlag.IsAttackerCavalryEnabled) return false;
                        if (!SiegeHorsesFlag.GoWithCavalry) return false;
                        args.optionLeaveType = GameMenuOption.LeaveType.Wait;
                        return true;
                    },
                    args =>
                    {
                        SiegeHorsesFlag.GoWithCavalry = false;
                        SiegeHorsesFlag.IsDefending = false;
                        InformationManager.DisplayMessage(new InformationMessage(
                            "Cavalry assault cancelled.", Colors.Yellow));
                        GameMenu.SwitchToMenu(menuId);
                    },
                    false, 5, false);
            }
            catch { }
        }

        // "encounter" fallback for attacker — only shown when the player is the besieger.
        private static void AddEncounterAttackerOptions(CampaignGameStarter starter)
        {
            starter.AddGameMenuOption(
                "encounter",
                "sh_cavalry_enable_enc_atk",
                "Bring cavalry to the assault",
                args =>
                {
                    if (!SiegeHorsesFlag.IsAttackerCavalryEnabled) return false;
                    if (SiegeHorsesFlag.GoWithCavalry) return false;
                    if (!IsActivelySieging()) return false;
                    args.optionLeaveType = GameMenuOption.LeaveType.Wait;
                    return true;
                },
                args =>
                {
                    SiegeHorsesFlag.GoWithCavalry = true;
                    SiegeHorsesFlag.IsDefending = false;
                    InformationManager.DisplayMessage(new InformationMessage(
                        "Cavalry will join the assault.", Colors.Green));
                    GameMenu.SwitchToMenu("encounter");
                },
                false, 4, false);

            starter.AddGameMenuOption(
                "encounter",
                "sh_cavalry_disable_enc_atk",
                "Cancel cavalry assault [cavalry ENABLED]",
                args =>
                {
                    if (!SiegeHorsesFlag.IsAttackerCavalryEnabled) return false;
                    if (!SiegeHorsesFlag.GoWithCavalry) return false;
                    if (!IsActivelySieging()) return false;
                    args.optionLeaveType = GameMenuOption.LeaveType.Wait;
                    return true;
                },
                args =>
                {
                    SiegeHorsesFlag.GoWithCavalry = false;
                    SiegeHorsesFlag.IsDefending = false;
                    InformationManager.DisplayMessage(new InformationMessage(
                        "Cavalry assault cancelled.", Colors.Yellow));
                    GameMenu.SwitchToMenu("encounter");
                },
                false, 5, false);
        }

        // Defender menus — "defender_siege" confirmed from DLL method names.
        // Also fallback on "encounter" with an IsActivelyDefending() guard.
        private static void AddDefenderOptions(CampaignGameStarter starter, string menuId)
        {
            try
            {
                starter.AddGameMenuOption(
                    menuId,
                    "sh_cavalry_enable_def_" + menuId,
                    "Deploy cavalry for the defence",
                    args =>
                    {
                        if (!SiegeHorsesFlag.IsDefenderCavalryEnabled) return false;
                        if (SiegeHorsesFlag.GoWithCavalry) return false;
                        args.optionLeaveType = GameMenuOption.LeaveType.Wait;
                        return true;
                    },
                    args =>
                    {
                        SiegeHorsesFlag.GoWithCavalry = true;
                        SiegeHorsesFlag.IsDefending = true;
                        InformationManager.DisplayMessage(new InformationMessage(
                            $"Cavalry will defend the walls (cap: {SiegeHorsesFlag.CavalryCap}).", Colors.Green));
                        GameMenu.SwitchToMenu(menuId);
                    },
                    false, 4, false);

                starter.AddGameMenuOption(
                    menuId,
                    "sh_cavalry_disable_def_" + menuId,
                    "Cancel cavalry defence [cavalry ENABLED]",
                    args =>
                    {
                        if (!SiegeHorsesFlag.IsDefenderCavalryEnabled) return false;
                        if (!SiegeHorsesFlag.GoWithCavalry) return false;
                        args.optionLeaveType = GameMenuOption.LeaveType.Wait;
                        return true;
                    },
                    args =>
                    {
                        SiegeHorsesFlag.GoWithCavalry = false;
                        SiegeHorsesFlag.IsDefending = false;
                        InformationManager.DisplayMessage(new InformationMessage(
                            "Cavalry defence cancelled.", Colors.Yellow));
                        GameMenu.SwitchToMenu(menuId);
                    },
                    false, 5, false);
            }
            catch { }
        }

        // "encounter" fallback for defender — only shown when the player's settlement is besieged.
        private static void AddEncounterDefenderOptions(CampaignGameStarter starter)
        {
            starter.AddGameMenuOption(
                "encounter",
                "sh_cavalry_enable_enc_def",
                "Deploy cavalry for the defence",
                args =>
                {
                    if (!SiegeHorsesFlag.IsDefenderCavalryEnabled) return false;
                    if (SiegeHorsesFlag.GoWithCavalry) return false;
                    if (!IsActivelyDefending()) return false;
                    args.optionLeaveType = GameMenuOption.LeaveType.Wait;
                    return true;
                },
                args =>
                {
                    SiegeHorsesFlag.GoWithCavalry = true;
                    SiegeHorsesFlag.IsDefending = true;
                    InformationManager.DisplayMessage(new InformationMessage(
                        $"Cavalry will defend the walls (cap: {SiegeHorsesFlag.CavalryCap}).", Colors.Green));
                    GameMenu.SwitchToMenu("encounter");
                },
                false, 4, false);

            starter.AddGameMenuOption(
                "encounter",
                "sh_cavalry_disable_enc_def",
                "Cancel cavalry defence [cavalry ENABLED]",
                args =>
                {
                    if (!SiegeHorsesFlag.IsDefenderCavalryEnabled) return false;
                    if (!SiegeHorsesFlag.GoWithCavalry) return false;
                    if (!IsActivelyDefending()) return false;
                    args.optionLeaveType = GameMenuOption.LeaveType.Wait;
                    return true;
                },
                args =>
                {
                    SiegeHorsesFlag.GoWithCavalry = false;
                    SiegeHorsesFlag.IsDefending = false;
                    InformationManager.DisplayMessage(new InformationMessage(
                        "Cavalry defence cancelled.", Colors.Yellow));
                    GameMenu.SwitchToMenu("encounter");
                },
                false, 5, false);
        }

        private void OnMissionEnded(IMission mission)
        {
            SiegeHorsesFlag.GoWithCavalry = false;
            SiegeHorsesFlag.IsDefending = false;
        }

        private static bool IsActivelySieging()
        {
            try
            {
                MobileParty main = MobileParty.MainParty;
                if (main == null) return false;
                if (main.BesiegerCamp != null) return true;
                if (main.Army?.LeaderParty?.BesiegerCamp != null) return true;
                return false;
            }
            catch { return false; }
        }

        private static bool IsActivelyDefending()
        {
            try
            {
                // The player is defending if their current settlement is under siege
                // and they are NOT the besieger.
                var settlement = MobileParty.MainParty?.CurrentSettlement;
                if (settlement == null) return false;
                return settlement.IsUnderSiege && !IsActivelySieging();
            }
            catch { return false; }
        }
    }
}
