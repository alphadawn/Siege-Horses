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

## How It Works (For Modders)

This mod uses three key techniques to make cavalry work in sieges:

### 1. Intercepting `NoHorses` at Spawn Time
Bannerlord calls `AgentBuildData.NoHorses(true)` for **every troop** during siege spawning. We patch it and set `noHorses = false`, allowing horses up to the configured cap per side. Cavalry that exceed the cap keep `noHorses = true` and spawn as infantry from the start — no wasteful spawn-and-dismount cycle.

### 2. Wave Safety via Patch Disable
After 60 ticks (when initial troops have spawned), we set `SiegeHorsesFlag.GoWithCavalry = false`. This disables the `NoHorses` patch, so all future spawns (wave reinforcements armies) get `noHorses = true` naturally. No periodic scanning needed.

### 3. Fixing the "Floating Rider" Bug
When a mount is killed mid-battle, the rider stays stuck at horse height with a frozen riding animation. We fix this with a three-step dismount:
1. **Kill the mount** (`MakeDead`) so it disappears
2. **Reset the action channel** (`SetActionChannel(0, act_none)`) to break the riding pose
3. **Teleport to ground level** — we find the Z of any nearby dismounted ally as a ground reference, then `TeleportToPosition` only if the rider is >0.5m above it. Riders who snap naturally are left alone.

### Formation Split
Player cavalry is assigned to custom formations (FormationClass.LightCavalry = 6, HeavyCavalry = 7). We check each agent's weapons — any ranged weapon (bow, crossbow, thrown) → Horse Archers, otherwise → Melee Cavalry.

---

## Notes

- The player's **civilian horse slot** is not touched; only the battle horse is used.
- If the player has no horse in their battle equipment, only troops (not the player) get horses.
- Compatible with mods that use `SpawnPlayer` for other purposes (e.g. paradeMod town entries).
- Enemy cavalry AI may get stuck on ladders — see the known issue above.
