using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

namespace SiegeHorses
{
    public class SiegeHorsesSubModule : MBSubModuleBase
    {
        private readonly Harmony _harmony = new Harmony("siegeHorses.patches");

        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            _harmony.PatchAll(typeof(SiegeHorsesSubModule).Assembly);
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarter)
        {
            base.OnGameStart(game, gameStarter);

            if (game.GameType is Campaign)
            {
                CampaignGameStarter campaignStarter = (CampaignGameStarter)gameStarter;
                campaignStarter.AddBehavior(new SiegeHorsesBehavior());
            }
        }

        public override void OnBeforeMissionBehaviorInitialize(Mission mission)
        {
            base.OnBeforeMissionBehaviorInitialize(mission);

            // Apply the SpawnPlayer horse patch now — SandBox.dll is guaranteed loaded.
            SpawnPlayerSiegePatch.TryApply(_harmony);

            // Inject the cavalry formation fixer whenever the flag is active.
            // GoWithCavalry is only set from the siege menu, so no scene name guard needed.
            if (!SiegeHorsesFlag.GoWithCavalry) return;

            mission.AddMissionBehavior(new SiegeCavalryMissionBehavior());
        }

    }
}
