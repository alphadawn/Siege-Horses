# Siege Horses — Bannerlord Mod

Allows the player's cavalry to participate in siege assaults. Normally Bannerlord forces all troops to fight on foot during sieges; this mod lets you opt in to a cavalry charge through the breach.
Built and tested against Bannerlord **1.3.15** (final release / War Sails).

---

## How It Works

Before clicking "Lead an assault" (or when defending a siege), toggle cavalry in the siege menu. When enabled:

- **Per-side cavalry cap** — Only the top N troops per side (configurable in MCM, default 50) spawn with horses. Higher-tier troops are prioritised.
- **Auto formation split** — Player cavalry is automatically assigned to **Formation 7 (Melee Cavalry)** and **Formation 6 (Horse Archers)** based on equipped weapons. No manual reassignment needed.
- **Wave-safe** — After the initial troops spawn, the horse-spawn patch is disabled so all wave reinforcements spawn as infantry. The cap stays enforced throughout the battle.
- Enemy cavalry is handled by your MCM choice: stripped entirely, fully enabled, or limited to lords & elite units only.
- The flag resets automatically after every battle.

---

## ⚠️ Known Issue: Cavalry on Ladders/Stairs

Bannerlord's siege AI was designed for infantry. Mounted agents that attempt to climb ladders or use stairs will get stuck on horseback. This is an engine-level limitation — the mod only controls horse spawning, not AI pathfinding.

**Workarounds:**
- Keep cavalry capped low enough that they focus on open-area combat (gate defence, courtyard charges) rather than wall assaults.
- Manually dismount cavalry before sending them up ladders (select → "dismount" hotkey).
- Use the MCM **Enemy CavalryMode → Player Only** setting so enemy cavalry can't abuse the same exploit.

---

## MCM Settings

Configure everything in **Mod Options → Siege Horses**:

| Setting | Default | Description |
|---------|---------|-------------|
| **Cavalry Cap (Per Side)** | 50 | Maximum mounted troops per side in siege battles. Higher-tier troops are prioritised. |
| **Enable Defender Cavalry** | On | Allow cavalry when defending a siege. |
| **Enable Attacker Cavalry** | On | Allow cavalry when attacking a siege. |
| **Enemy: Player Only** | Off | Enemies get zero horses — all forced to infantry. |
| **Enemy: Full Cavalry** | Off | Both sides spawn full cavalry (more horses = more wasted mounts on enemy AI). |
| **Enemy: Lords & Elite Only** | On | Enemy side only gets horses for heroes (lords) and tier 5+ units. |

---

## Usage

### As Attacker
1. Besiege a settlement until the siege menu appears
2. Click **"Bring cavalry to the assault"**
3. Click **"Lead an assault"** as normal
4. Mounted troops arrive on horseback in their own formations

### As Defender
1. When your settlement is under siege and you choose to defend
2. Click **"Deploy cavalry for the defence"**
3. Start the battle — your best cavalry spawns mounted

To cancel before the battle, click the **"Cancel cavalry …"** option that appears.

---

## Architecture

### Files

| File | Purpose |
|------|---------|
| `SiegeHorsesSubModule.cs` | Entry point; Harmony patches, campaign & mission behavior injection |
| `SiegeHorsesBehavior.cs` | Campaign behavior; adds menu options, resets flag on mission end |
| `SiegeCavalryMissionBehavior.cs` | Mission behavior; enforces cap at spawn time, splits cav into melee/horse-archer, disables patch after 60 ticks for wave safety |
| `Patches.cs` | Harmony patches for `AgentBuildData.NoHorses` and `SpawnPlayer` |
| `SiegeHorsesSettings.cs` | MCM settings class (Attribute API v2) |
| `EnemyCavalryMode.cs` | Enum for enemy cavalry behavior |

---

## Key APIs

### Cavalry Cap Enforcement at Spawn Time

```csharp
// NoHorsesPatch intercepts every AgentBuildData.NoHorses call.
// It counts cavalry per side and only allows horses up to the cap.
// Once the cap is reached, noHorses stays true — remaining troops
// spawn as infantry from the start. After 60 ticks the flag is
// disabled so wave reinforcements never get horses.

if (currentCount >= cap) return;  // cap reached — leave noHorses = true
noHorses = false;
counter++;
```

### Disabling the Patch for Wave Reinforcements

```csharp
// In SiegeCavalryMissionBehavior.OnMissionTick, after the initial
// cavalry has spawned (60 ticks):
SiegeHorsesFlag.GoWithCavalry = false;
// NoHorsesPatch stops running → all future spawns get noHorses = true.
```

### Dismounting Excess Cavalry

```csharp
// Cavalry that exceed the cap are dismounted by killing their mount,
// resetting the action channel (to fix the frozen riding animation),
// and teleporting the rider to ground level (to fix floating in air).

mount.MakeDead(false, ActionIndexCache.act_none, 0);
rider.SetActionChannel(0, ActionIndexCache.act_none, true, default(AnimFlags), ...);
rider.TeleportToPosition(new Vec3(pos.X, pos.Y, groundZ));
```

### Auto Formation Split

```csharp
// Player cavalry is split into melee cavalry (Formation 7) and horse archers
// (Formation 6) based on whether they have a bow, crossbow, or thrown weapon.

foreach (Agent agent in mountedAgents)
{
    bool hasRanged = HasRangedWeapon(agent);
    agent.Formation = hasRanged ? horseArcherFormation : meleeCavFormation;
}
```

### Allowing Horses in Siege Missions

```csharp
// AgentBuildData.NoHorses(bool) is called during every agent spawn.
// In siege missions, the game passes noHorses = true to strip mounts.
// A Harmony Prefix intercepts this and sets noHorses = false when
// the flag is active and the cap hasn't been reached yet.

[HarmonyPatch(typeof(AgentBuildData), nameof(AgentBuildData.NoHorses))]
public static class NoHorsesPatch
{
    static void Prefix(AgentBuildData __instance, ref bool noHorses)
    {
        if (!noHorses) return;
        if (!SiegeHorsesFlag.GoWithCavalry) return;
        // Count cavalry per side, check cap, enemy mode, etc.
        if (currentCount >= cap) return;
        noHorses = false;  // spawn the horse
        counter++;
    }
}
```

---

## Notes

- The player's **civilian horse slot** is not touched; only the battle horse is used.
- If the player has no horse in their battle equipment, only troops (not the player) get horses.
- Compatible with mods that use `SpawnPlayer` for other purposes (e.g. paradeMod town entries).
- Enemy cavalry AI may get stuck on ladders — see the known issue above.
