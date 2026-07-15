# 🥔 HotPotato - CS2 Keep-Away Gamemode

A chaotic party gamemode plugin for Counter-Strike 2, built on [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp).

A "hot potato" is assigned to a random player. The holder gets a speed boost, a golden glow, and shows on everyone's radar, but their health drains every second. Pass it by getting close and pressing **E**, or by knifing someone. If the fuse runs out or the holder dies, they explode. Last survivor wins the round. Most round wins takes the match. A shrinking safe zone keeps everyone moving.

## Features

- 🏃 **Configurable holder speed boost**: the potato carrier moves faster
- 💛 **Golden glow visible through walls**, plus gold model tint and forced radar blip, so the holder can't hide
- 🔪 **Two pass mechanics**: press E near a player, or land a knife hit (knife passes deal no damage)
- 💥 **Fuse timer + health drain**: hold too long and you explode
- 🏆 **Multi-round matches with a scoreboard**: round wins tracked, medals in chat, champion announced
- 🌀 **Shrinking safe zone** (battle-royale style) with a visible beam ring and out-of-zone damage
- 🗺️ **Deathmatch-style spawn spreading**: players scattered across all map spawn points each round
- 🛡️ **Player-vs-player damage disabled during games** (configurable), only the potato kills
- ⚙️ Fully config-driven, every mechanic tunable without recompiling

## Requirements

- [Metamod:Source](https://www.sourcemm.net/) (CS2 build)
- [CounterStrikeSharp](https://github.com/roflmuffin/CounterStrikeSharp) v80+

## Installation

1. Download the latest release (or build from source, see below)
2. Upload the plugin folder to your server:
   ```
   game/csgo/addons/counterstrikesharp/plugins/HotPotato/
   ├── HotPotato.dll
   └── HotPotato.deps.json
   ```
3. Restart the server
4. Verify with `css_plugins list` in the server console

The config file auto-generates on first load at:
```
game/csgo/addons/counterstrikesharp/configs/plugins/HotPotato/HotPotato.json
```

## Usage

| Command | Description |
|---|---|
| `!potato start` | Start a match (needs 2+ alive players) |
| `!potato stop` | Stop the match and restore the server |

Both require the permission flag set in `CommandPermission` (default `@css/generic`).

Starting a match automatically ends warmup, disables respawns, and prevents rounds from ending mid-game. Stopping (or finishing) a match restores everything.

## Configuration

| Option | Default | Description |
|---|---|---|
| `SpeedMultiplier` | `1.15` | Holder movement speed multiplier |
| `HealthDrainPerSecond` | `2` | HP the holder loses per second |
| `FuseSeconds` | `40` | Seconds until the potato detonates |
| `PassRange` | `60` | Max distance (units) for an E-key pass |
| `PassCooldownSeconds` | `1` | Minimum time between passes |
| `GraceSeconds` | `5` | Countdown before drain/fuse start each round |
| `CommandPermission` | `@css/generic` | Admin flag required for `!potato` |
| `HolderColorR/G/B` | `255/200/0` | Holder tint & glow color (gold) |
| `ExplosionSoundPath` | `sounds/weapons/c4/c4_explode1` | Explosion sound (empty = silent) |
| `KnivesOnly` | `true` | Strip everyone to knives during games |
| `DisablePlayerDamage` | `true` | Block all PvP damage during games |
| `RoundsPerMatch` | `5` | Rounds per match |
| `IntermissionSeconds` | `6` | Scoreboard pause between rounds |
| `FreezeSeconds` | `8` | Pre-round freeze period where players are locked in spawn |
| `HolderRadarBlip` | `true` | Force the holder onto everyone's radar |
| `HolderGlow` | `true` | Through-wall glow outline on the holder |
| `DeathmatchSpawns` | `true` | Spread players across all spawn points |
| `SafeZoneEnabled` | `true` | Enable the shrinking zone |
| `SafeZoneStartRadius` | `0` | Starting radius (`0` = auto-calculated to 2x map size) |
| `SafeZoneMinRadius` | `350` | Final radius after shrinking |
| `SafeZoneShrinkSeconds` | `90` | Shrink time (automatically scales with player count) |
| `SafeZoneDamagePerSecond` | `5` | HP/s damage outside the zone |
| `SafeZoneBeamSegments` | `24` | Beam ring detail (`0` = no visuals) |

## Building from source

Requires the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).

```bash
git clone https://github.com/Arsynator/CS2-Hot-Potato-Plugin.git
cd CS2-Hot-Potato-Plugin
dotnet build -c Release
```

Output: `bin/Release/net8.0/HotPotato.dll`

## Known limitations

- The safe zone ring draws at a single height (based on spawn-point average), so on multi-level maps (Nuke, Vertigo) the visual may sit at an odd height. The zone *damage* logic is 2D and works correctly regardless.
- Uses the deprecated `CBaseEntity_TakeDamageOldFunc` hook (still functional; migration to `OnEntityTakeDamagePre` planned).

## License

MIT