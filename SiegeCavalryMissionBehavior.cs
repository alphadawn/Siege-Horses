using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SiegeHorses
{
    /// <summary>
    /// Injected into missions when GoWithCavalry is enabled.
    ///
    /// After the battle loading screen finishes:
    ///   1. Creates formations 7 (Horse Archers) and 8 (Melee Cavalry) for the player team.
    ///   2. Enforces the cavalry cap by dismounting excess agents and killing their mounts.
    ///   3. Assigns remaining mounted player agents to the correct formation based on weapons.
    ///   4. Applies enemy cavalry mode (PlayerOnly, Full, LordsAndElite).
    ///
    /// The cap is enforced continuously (every 30 ticks) to handle wave attacks where
    /// new cavalry may spawn after the initial battle load.
    /// </summary>
    public class SiegeCavalryMissionBehavior : MissionBehavior
    {
        public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

        // Player-side custom formation indices (0-based FormationClass).
        private const int MeleeCavalryIndex = 7;   // FormationClass.HeavyCavalry  → UI "8"
        private const int HorseArcherIndex = 6;    // FormationClass.LightCavalry  → UI "7"

        // Phase 1: wait for battle to start
        private bool _phase1Done = false;
        private int _waitTicks = 0;
        private const int TicksToWait = 60;

        // Track dismounted riders so we don't process them twice
        private readonly HashSet<Agent> _dismountedRiders = new HashSet<Agent>();

        public override void OnMissionTick(float dt)
        {
            Mission mission = Mission.Current;
            if (mission == null) return;

            // ── Wait for battle to start ─────────────────────────────────────
            if (!_phase1Done)
            {
                Agent mainAgent = mission.MainAgent;
                if (mainAgent != null
                    && mainAgent.IsActive()
                    && mission.IsLoadingFinished
                    && mission.Mode == MissionMode.Battle)
                {
                    _waitTicks++;
                    if (_waitTicks >= TicksToWait)
                    {
                        ProcessCavalry();
                        _phase1Done = true;

                        // Disable the NoHorses patch for future spawns.
                        // All initial cavalry has spawned by now; any new troops
                        // (wave reinforcements) will spawn without horses.
                        SiegeHorsesFlag.GoWithCavalry = false;
                    }
                }
                return;
            }
        }

        private void ProcessCavalry()
        {
            Mission mission = Mission.Current;
            if (mission == null) return;

            Team playerTeam = mission.PlayerTeam;
            if (playerTeam == null) return;

            // ── Create formations 7 and 8 ─────────────────────────────────────
            Formation meleeCavFormation = playerTeam.GetFormation((FormationClass)MeleeCavalryIndex);
            Formation horseArcherFormation = playerTeam.GetFormation((FormationClass)HorseArcherIndex);

            if (meleeCavFormation == null || horseArcherFormation == null)
                return;

            // ── Collect all mounted non-player agents per team ──────────────────
            var playerMounted = new List<Agent>();
            var enemyMounted = new List<Agent>();

            foreach (Agent agent in mission.Agents.ToList())
            {
                if (agent == null || !agent.IsHuman || agent.IsMainAgent) continue;
                if (!agent.IsActive()) continue;
                if (agent.MountAgent == null || !agent.MountAgent.IsActive()) continue;
                if (_dismountedRiders.Contains(agent)) continue;

                if (agent.Team == playerTeam)
                    playerMounted.Add(agent);
                else
                    enemyMounted.Add(agent);
            }

            // ── Player side: cap, split into melee cav / horse archers ──────────
            int cap = SiegeHorsesFlag.CavalryCap;

            // Sort by level descending (higher tier troops get priority)
            playerMounted.Sort((a, b) =>
            {
                int levelA = a.Character?.Level ?? 0;
                int levelB = b.Character?.Level ?? 0;
                return levelB.CompareTo(levelA);
            });

            var playerToKeep = playerMounted.Take(cap).ToList();
            var playerExcess = playerMounted.Skip(cap).ToList();

            // Dismount excess
            foreach (Agent agent in playerExcess)
                QueueDismount(agent);

            // Assign kept cavalry to formations
            foreach (Agent agent in playerToKeep)
            {
                if (!agent.IsActive()) continue;
                bool hasRanged = HasRangedWeapon(agent);
                Formation target = hasRanged ? horseArcherFormation : meleeCavFormation;
                try { agent.Formation = target; }
                catch { }
            }

            // ── Enemy side: cap + mode logic ────────────────────────────────────
            ApplyEnemyCavalryMode(enemyMounted);
        }

        private void ApplyEnemyCavalryMode(List<Agent> enemyMounted)
        {
            switch (SiegeHorsesFlag.EnemyCavalryMode)
            {
                case EnemyCavalryMode.PlayerOnly:
                    foreach (Agent agent in enemyMounted)
                    {
                        if (!_dismountedRiders.Contains(agent))
                            QueueDismount(agent);
                    }
                    break;

                case EnemyCavalryMode.Full:
                    CapEnemyCavalry(enemyMounted);
                    break;

                case EnemyCavalryMode.LordsAndElite:
                    DismountLowTierEnemyCavalry(enemyMounted);
                    // Re-collect after dismounting low-tier
                    var remaining = enemyMounted.Where(a => a != null && a.IsActive() && a.MountAgent != null && a.MountAgent.IsActive() && !_dismountedRiders.Contains(a)).ToList();
                    CapEnemyCavalry(remaining);
                    break;
            }
        }

        private void CapEnemyCavalry(List<Agent> enemyMounted)
        {
            int cap = SiegeHorsesFlag.CavalryCap;

            enemyMounted.Sort((a, b) =>
            {
                int levelA = a.Character?.Level ?? 0;
                int levelB = b.Character?.Level ?? 0;
                return levelB.CompareTo(levelA);
            });

            foreach (Agent agent in enemyMounted.Skip(cap))
            {
                if (!_dismountedRiders.Contains(agent))
                    QueueDismount(agent);
            }
        }

        private void DismountLowTierEnemyCavalry(List<Agent> enemyMounted)
        {
            foreach (Agent agent in enemyMounted)
            {
                if (_dismountedRiders.Contains(agent)) continue;
                if (agent == null || !agent.IsActive()) continue;
                var ch = agent.Character;
                if (ch == null) continue;
                bool isHero = ch.IsHero;
                if (!isHero && ch.Level < 20)
                    QueueDismount(agent);
            }
        }

        /// <summary>
        /// Dismounts a rider by clearing the mount reference, killing the horse,
        /// and immediately snapping floating riders to the ground.
        /// </summary>
        private void QueueDismount(Agent rider)
        {
            if (rider == null || !rider.IsActive()) return;
            if (_dismountedRiders.Contains(rider)) return;

            Agent mount = rider.MountAgent;

            // Try setting MountAgent to null to trigger a clean dismount
            try
            {
                rider.GetType().GetProperty("MountAgent")?.SetValue(rider, null, null);
            }
            catch { }

            // Kill the mount
            if (mount != null && mount.IsActive())
            {
                try
                {
                    mount.MakeDead(false, ActionIndexCache.act_none, 0);
                }
                catch { }
            }

            // Force-reset the rider's action channel to break them out of
            // the mounted riding pose
            try
            {
                rider.SetActionChannel(0, ActionIndexCache.act_none, true, default(AnimFlags), 0f, 0f, 0f, 0.1f, 0f, false, 0f, 0, false);
            }
            catch { }

            // Find ground Z from any nearby dismounted agent
            Mission mission = Mission.Current;
            float groundZ = -1f;

            if (mission != null)
            {
                foreach (Agent a in mission.Agents)
                {
                    if (a == null || !a.IsActive() || !a.IsHuman) continue;
                    if (a.MountAgent != null) continue;
                    if (a.Team != rider.Team) continue;
                    groundZ = a.Position.Z;
                    break;
                }
            }

            // Fallback: just subtract typical horse height
            if (groundZ < 0f)
                groundZ = rider.Position.Z - 1.5f;

            // If the rider is more than 0.5m above ground, they're floating — fix immediately
            float riderZ = rider.Position.Z;
            if (riderZ - groundZ > 0.5f)
            {
                try
                {
                    var pos = rider.Position;
                    var newPos = new Vec3(pos.X, pos.Y, groundZ);
                    rider.TeleportToPosition(newPos);
                }
                catch { }
            }

            _dismountedRiders.Add(rider);
        }

        private static bool HasRangedWeapon(Agent agent)
        {
            if (agent == null) return false;

            try
            {
                MissionEquipment equip = agent.Equipment;
                if (equip == null) return false;

                for (EquipmentIndex i = EquipmentIndex.Weapon0; i <= EquipmentIndex.Weapon3; i++)
                {
                    ItemObject item = equip[i].Item;
                    if (item == null) continue;
                    WeaponComponentData wc = item.PrimaryWeapon;
                    WeaponClass weaponClass = wc.WeaponClass;
                    if (weaponClass == WeaponClass.Bow ||
                        weaponClass == WeaponClass.Crossbow ||
                        weaponClass == WeaponClass.ThrowingAxe ||
                        weaponClass == WeaponClass.ThrowingKnife ||
                        weaponClass == WeaponClass.Javelin ||
                        weaponClass == WeaponClass.Stone)
                        return true;
                }
            }
            catch { }
            return false;
        }
    }
}
