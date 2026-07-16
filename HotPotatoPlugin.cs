using System.Drawing;
using System.Text.Json.Serialization;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;

namespace HotPotato;

public class HotPotatoConfig : BasePluginConfig
{
    [JsonPropertyName("SpeedMultiplier")]
    public float SpeedMultiplier { get; set; } = 1.15f;

    [JsonPropertyName("HealthDrainPerSecond")]
    public int HealthDrainPerSecond { get; set; } = 2;

    [JsonPropertyName("FuseSeconds")]
    public float FuseSeconds { get; set; } = 40f;

    [JsonPropertyName("PassRange")]
    public float PassRange { get; set; } = 60f;

    [JsonPropertyName("PassCooldownSeconds")]
    public float PassCooldownSeconds { get; set; } = 1.0f;

    [JsonPropertyName("GraceSeconds")]
    public float GraceSeconds { get; set; } = 5f;

    [JsonPropertyName("CommandPermission")]
    public string CommandPermission { get; set; } = "@css/generic";

    [JsonPropertyName("HolderColorR")]
    public byte HolderColorR { get; set; } = 255;

    [JsonPropertyName("HolderColorG")]
    public byte HolderColorG { get; set; } = 200;

    [JsonPropertyName("HolderColorB")]
    public byte HolderColorB { get; set; } = 0;

    [JsonPropertyName("ExplosionSoundPath")]
    public string ExplosionSoundPath { get; set; } = "sounds/weapons/c4/c4_explode1";

    [JsonPropertyName("KnivesOnly")]
    public bool KnivesOnly { get; set; } = true;

    [JsonPropertyName("DisablePlayerDamage")]
    public bool DisablePlayerDamage { get; set; } = true;

    // ---- New polish features ----

    // Number of potato rounds per match; scoreboard shown between rounds and at the end
    [JsonPropertyName("RoundsPerMatch")]
    public int RoundsPerMatch { get; set; } = 5;

    // Seconds between rounds (scoreboard display time)
    [JsonPropertyName("IntermissionSeconds")]
    public float IntermissionSeconds { get; set; } = 6f;

    [JsonPropertyName("FreezeSeconds")]
    public float FreezeSeconds { get; set; } = 8f;

    // Force the holder to be visible on everyone's radar
    [JsonPropertyName("HolderRadarBlip")]
    public bool HolderRadarBlip { get; set; } = true;

    // Give the holder a glowing outline visible through walls
    [JsonPropertyName("HolderGlow")]
    public bool HolderGlow { get; set; } = true;

    // Spread players across ALL map spawn points (T + CT + DM) each round
    [JsonPropertyName("DeathmatchSpawns")]
    public bool DeathmatchSpawns { get; set; } = true;

    // ---- Shrinking safe zone ----

    [JsonPropertyName("SafeZoneEnabled")]
    public bool SafeZoneEnabled { get; set; } = true;

    // 0 = auto (computed from the map's spawn point spread)
    [JsonPropertyName("SafeZoneStartRadius")]
    public float SafeZoneStartRadius { get; set; } = 0f;

    [JsonPropertyName("SafeZoneMinRadius")]
    public float SafeZoneMinRadius { get; set; } = 350f;

    // How long the zone takes to shrink from start radius to min radius
    [JsonPropertyName("SafeZoneShrinkSeconds")]
    public float SafeZoneShrinkSeconds { get; set; } = 90f;

    [JsonPropertyName("SafeZoneDamagePerSecond")]
    public int SafeZoneDamagePerSecond { get; set; } = 5;

    // Number of beam segments in the visual ring. 0 disables zone visuals
    // (useful if beams misbehave on a map).
    [JsonPropertyName("SafeZoneBeamSegments")]
    public int SafeZoneBeamSegments { get; set; } = 32;

    // Height of the zone "cage" wall (bottom ring, top ring, vertical posts)
    [JsonPropertyName("SafeZoneWallHeight")]
    public float SafeZoneWallHeight { get; set; } = 400f;

    // Server commands executed when a match starts. Defaults end warmup,
    // disable respawns, and prevent rounds ending mid-game. Adjust these
    // if your server doesn't run an infinite-warmup setup.
    [JsonPropertyName("OnStartCommands")]
    public List<string> OnStartCommands { get; set; } = new()
    {
        "mp_warmup_end",
        "mp_respawn_on_death_ct 0",
        "mp_respawn_on_death_t 0",
        "mp_ignore_round_win_conditions 1",
        "mp_roundtime 60",
        "mp_freezetime 0",
        "mp_forcecamera 0"
    };

    // Server commands executed when a match stops or finishes. Defaults
    // restore normal respawns and round conditions. If you run an
    // infinite-warmup practice server, add mp_warmuptime / mp_warmup_pausetimer /
    // mp_warmup_start here to drop back into it; otherwise leave those out.
    [JsonPropertyName("OnStopCommands")]
    public List<string> OnStopCommands { get; set; } = new()
    {
        "mp_respawn_on_death_ct 1",
        "mp_respawn_on_death_t 1",
        "mp_ignore_round_win_conditions 0",
        "mp_forcecamera 1"
    };
}

[MinimumApiVersion(80)]
public class HotPotatoPlugin : BasePlugin, IPluginConfig<HotPotatoConfig>
{
    public override string ModuleName => "Hot Potato";
    public override string ModuleVersion => "2.0.0";
    public override string ModuleAuthor => "Arsy";
    public override string ModuleDescription => "Hot Potato keep-away gamemode: multi-round matches, glow, radar, shrinking zone";

    public HotPotatoConfig Config { get; set; } = new();

    public void OnConfigParsed(HotPotatoConfig config)
    {
        config.SpeedMultiplier = Math.Clamp(config.SpeedMultiplier, 0.1f, 5f);
        config.HealthDrainPerSecond = Math.Clamp(config.HealthDrainPerSecond, 0, 100);
        config.FuseSeconds = Math.Clamp(config.FuseSeconds, 3f, 600f);
        config.PassRange = Math.Clamp(config.PassRange, 10f, 1000f);
        config.PassCooldownSeconds = Math.Clamp(config.PassCooldownSeconds, 0f, 30f);
        config.GraceSeconds = Math.Clamp(config.GraceSeconds, 0f, 60f);
        config.RoundsPerMatch = Math.Clamp(config.RoundsPerMatch, 1, 50);
        config.IntermissionSeconds = Math.Clamp(config.IntermissionSeconds, 2f, 60f);
        config.SafeZoneMinRadius = Math.Clamp(config.SafeZoneMinRadius, 100f, 5000f);
        config.SafeZoneShrinkSeconds = Math.Clamp(config.SafeZoneShrinkSeconds, 10f, 600f);
        config.SafeZoneDamagePerSecond = Math.Clamp(config.SafeZoneDamagePerSecond, 1, 100);
        config.SafeZoneBeamSegments = Math.Clamp(config.SafeZoneBeamSegments, 0, 64);

        Config = config;
    }

    // ---------------- Match state ----------------
    private bool _matchActive = false;
    private bool _roundActive = false;
    private int _currentRound = 0;
    private readonly HashSet<int> _participants = new();
    private readonly Dictionary<int, int> _wins = new();          // slot -> round wins
    private readonly Dictionary<int, string> _names = new();      // slot -> last known name

    // ---------------- Round state ----------------
    private CCSPlayerController? _holder = null;
    private float _fuseRemaining = 0f;
    private float _graceRemaining = 0f;
    private float _lastPassTime = 0f;
    private float _freezeRemaining = 0f;
    private bool _lastUsePressed = false;
    private bool _bypassDamageBlock = false;

    // Holder glow entities (prop_dynamic pair, same trick as the wallhack plugin)
    private CDynamicProp? _glowRelay = null;
    private CDynamicProp? _glowProp = null;

    // ---------------- Safe zone state ----------------
    private Vector _zoneCenter = new(0, 0, 0);
    private float _zoneStartRadius = 0f;
    private float _zoneRadius = 0f;
    private float _zoneElapsed = 0f;
    private float _actualShrinkSeconds = 90f;
    private readonly List<CBeam> _zoneBeams = new();
    private CBaseEntity? _zoneCenterEntity;


    private CounterStrikeSharp.API.Modules.Timers.Timer? _tickTimer;

    public override void Load(bool hotReload)
    {
        Logger.LogInformation("HotPotato loaded");

        RegisterEventHandler<EventPlayerDeath>(OnPlayerDeath);
        RegisterEventHandler<EventPlayerDisconnect>(OnPlayerDisconnect);
        RegisterEventHandler<EventPlayerSpawn>(OnPlayerSpawn);
        RegisterEventHandler<EventPlayerHurt>(OnPlayerHurt);

        RegisterEventHandler<EventRoundStart>((@event, info) =>
        {
            if (_matchActive && _roundActive && _holder != null)
            {
                var holder = _holder;
                AddTimer(1.0f, () =>
                {
                    if (_roundActive && _holder == holder && IsValidAlive(holder))
                        ApplyHolderEffects(holder);
                });
            }
            return HookResult.Continue;
        });

        RegisterListener<Listeners.OnTick>(OnTick);

        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Pre);
    }

    public override void Unload(bool hotReload)
    {
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(OnTakeDamage, HookMode.Pre);
        StopMatch(announce: false);
    }

    // ---------------- Commands ----------------

    [ConsoleCommand("css_potato", "Start or stop a hot potato match")]
    [CommandHelper(minArgs: 1, usage: "<start|stop>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
    public void OnPotatoCommand(CCSPlayerController? caller, CommandInfo command)
    {
        if (caller != null && !AdminManager.PlayerHasPermissions(caller, Config.CommandPermission))
        {
            command.ReplyToCommand(" [HotPotato] You don't have permission to use this command.");
            return;
        }

        string arg = command.GetArg(1).Trim().ToLowerInvariant();

        switch (arg)
        {
            case "start":
                if (_matchActive)
                {
                    command.ReplyToCommand(" [HotPotato] A match is already running. Use !potato stop first.");
                    return;
                }
                StartMatch(command);
                break;

            case "stop":
                if (!_matchActive)
                {
                    command.ReplyToCommand(" [HotPotato] No match running.");
                    return;
                }
                StopMatch(announce: true);
                break;

            default:
                command.ReplyToCommand(" [HotPotato] Usage: !potato <start|stop>");
                break;
        }
    }

    // ---------------- Match lifecycle ----------------

    private void StartMatch(CommandInfo command)
    {
        var alive = GetAlivePlayers();
        if (alive.Count < 2)
        {
            command.ReplyToCommand(" [HotPotato] Need at least 2 alive players.");
            return;
        }

        _participants.Clear();
        _wins.Clear();
        _names.Clear();
        foreach (var p in alive)
        {
            _participants.Add(p.Slot);
            _wins[p.Slot] = 0;
            _names[p.Slot] = p.PlayerName;
        }

        _matchActive = true;
        _currentRound = 0;

        foreach (var cmd in Config.OnStartCommands)
            Server.ExecuteCommand(cmd);

        Server.ExecuteCommand("mp_roundtime 60");
        Server.ExecuteCommand("mp_freezetime 0");
        Server.ExecuteCommand("mp_playerid 2");
        Server.ExecuteCommand("mp_randomspawn 1");
        Server.ExecuteCommand("mp_teammates_are_enemies 1");

        Server.PrintToChatAll($" {ChatColors.Gold}[HotPotato]{ChatColors.White} Match starting: {ChatColors.Green}{Config.RoundsPerMatch}{ChatColors.White} rounds, last survivor wins each round!");

        AddTimer(2.0f, StartRound);
    }

    private void StopMatch(bool announce)
    {
        EndRoundCleanup();

        _matchActive = false;
        _currentRound = 0;
        _participants.Clear();

        foreach (var cmd in Config.OnStopCommands)
            Server.ExecuteCommand(cmd);

        Server.ExecuteCommand("mp_playerid 0");
        Server.ExecuteCommand("mp_randomspawn 0");
        Server.ExecuteCommand("mp_teammates_are_enemies 0");

        if (announce)
            Server.PrintToChatAll($" {ChatColors.Gold}[HotPotato]{ChatColors.White} Match stopped, server restored.");
    }

    // ---------------- Round lifecycle ----------------

    private void StartRound()
    {
        if (!_matchActive) return;

        _currentRound++;

        // Reset CS2 HUD round timer to 5 minutes (300 seconds)
        var proxy = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules").FirstOrDefault();
        if (proxy != null)
        {
            var rules = proxy.GameRules;
            if (rules != null)
            {
                rules.RoundStartTime = (float)Server.CurrentTime;
                rules.RoundTime = 300;
                Utilities.SetStateChanged(proxy, "CCSGameRulesProxy", "m_pGameRules");
            }
        }

        _roundActive = false;
        _holder = null;
        _freezeRemaining = Config.FreezeSeconds;

        // Bring everyone back
        RespawnParticipants();

        // Start freeze countdown timer immediately
        if (Config.FreezeSeconds > 0)
        {
            _tickTimer?.Kill();
            _tickTimer = AddTimer(1.0f, GameSecondTick, TimerFlags.REPEAT);
        }

        // Wait for players to spawn, then teleport, heal, and strip them
        AddTimer(1.5f, () =>
        {
            if (!_matchActive) return;

            var candidates = GetAliveParticipants();
            if (candidates.Count < 2)
            {
                Server.PrintToChatAll($" {ChatColors.Gold}[HotPotato]{ChatColors.White} Not enough players, ending match.");
                FinishMatch();
                return;
            }

            if (Config.DeathmatchSpawns)
                SpreadPlayersAcrossSpawns(candidates);

            // Fresh HP for everyone, including last round's survivor
            foreach (var p in candidates)
            {
                var candidatePawn = p.PlayerPawn?.Value;
                if (candidatePawn != null && candidatePawn.IsValid)
                {
                    candidatePawn.Health = 100;
                    Utilities.SetStateChanged(candidatePawn, "CBaseEntity", "m_iHealth");

                    if (_freezeRemaining > 0)
                        candidatePawn.MoveType = MoveType_t.MOVETYPE_NONE;
                }
            }

            if (Config.KnivesOnly)
            {
                foreach (var p in candidates)
                    StripToKnife(p);
            }

            if (Config.SafeZoneEnabled)
                InitSafeZone();

            // If freeze time is disabled, start immediately
            if (Config.FreezeSeconds <= 0)
            {
                StartRoundPostFreeze();
            }
        });
    }

    private void StartRoundPostFreeze()
    {
        if (!_matchActive) return;

        var candidates = GetAliveParticipants();
        if (candidates.Count < 2)
        {
            Server.PrintToChatAll($" {ChatColors.Gold}[HotPotato]{ChatColors.White} Not enough players, ending match.");
            FinishMatch();
            return;
        }

        foreach (var p in candidates)
        {
            var pawn = p.PlayerPawn?.Value;
            if (pawn != null && pawn.IsValid)
            {
                pawn.MoveType = MoveType_t.MOVETYPE_WALK;
            }
        }

        _roundActive = true;
        _fuseRemaining = Config.FuseSeconds;
        _graceRemaining = Config.GraceSeconds;
        _lastPassTime = 0f;

        var random = new Random();
        var firstHolder = candidates[random.Next(candidates.Count)];
        SetHolder(firstHolder);

        Server.PrintToChatAll($" {ChatColors.Gold}[HotPotato]{ChatColors.White} ── ROUND {ChatColors.Green}{_currentRound}/{Config.RoundsPerMatch}{ChatColors.White} ── {ChatColors.Red}{firstHolder.PlayerName}{ChatColors.White} has the potato!");
        if (Config.GraceSeconds > 0)
            Server.PrintToChatAll($" {ChatColors.Gold}[HotPotato]{ChatColors.White} Drain starts in {ChatColors.Red}{Config.GraceSeconds:0}{ChatColors.White} seconds!");
        
        if (Config.FreezeSeconds <= 0)
        {
            _tickTimer?.Kill();
            _tickTimer = AddTimer(1.0f, GameSecondTick, TimerFlags.REPEAT);
        }
    }

    private void EndRound(CCSPlayerController? winner)
    {
        _roundActive = false;
        EndRoundCleanup();

        if (winner != null)
        {
            if (_wins.ContainsKey(winner.Slot))
                _wins[winner.Slot]++;
            _names[winner.Slot] = winner.PlayerName;

            Server.PrintToChatAll($" {ChatColors.Gold}[HotPotato] 🏆 {ChatColors.Green}{winner.PlayerName}{ChatColors.White} survives round {_currentRound}!");
        }
        else
        {
            Server.PrintToChatAll($" {ChatColors.Gold}[HotPotato]{ChatColors.White} Everyone died. Nobody scores. Incredible.");
        }

        PrintScoreboard(final: false);

        if (_currentRound >= Config.RoundsPerMatch)
        {
            FinishMatch();
            return;
        }

        Server.PrintToChatAll($" {ChatColors.Gold}[HotPotato]{ChatColors.White} Next round in {Config.IntermissionSeconds:0} seconds...");
        AddTimer(Config.IntermissionSeconds, StartRound);
    }

    private void FinishMatch()
    {
        PrintScoreboard(final: true);

        var best = _wins.OrderByDescending(kv => kv.Value).ToList();
        if (best.Count > 0 && best[0].Value > 0)
        {
            bool tie = best.Count > 1 && best[1].Value == best[0].Value;
            if (tie)
                Server.PrintToChatAll($" {ChatColors.Gold}[HotPotato]{ChatColors.White} Match over — it's a TIE at the top!");
            else
                Server.PrintToChatAll($" {ChatColors.Gold}[HotPotato] 👑 {ChatColors.Green}{_names.GetValueOrDefault(best[0].Key, "?")}{ChatColors.White} WINS THE MATCH with {best[0].Value} round wins!");
        }

        StopMatch(announce: false);
        Server.PrintToChatAll($" {ChatColors.Gold}[HotPotato]{ChatColors.White} GG! Server restored.");
    }

    private void PrintScoreboard(bool final)
    {
        string header = final
            ? $" {ChatColors.Gold}[HotPotato]{ChatColors.White} ═══ FINAL SCOREBOARD ═══"
            : $" {ChatColors.Gold}[HotPotato]{ChatColors.White} ─── Scoreboard ({_currentRound}/{Config.RoundsPerMatch}) ───";

        Server.PrintToChatAll(header);

        int rank = 1;
        foreach (var kv in _wins.OrderByDescending(kv => kv.Value))
        {
            string name = _names.GetValueOrDefault(kv.Key, "?");
            string medal = rank switch { 1 => "🥇", 2 => "🥈", 3 => "🥉", _ => "  " };
            Server.PrintToChatAll($" {ChatColors.Gold}[HotPotato]{ChatColors.White} {medal} {rank}. {name} — {ChatColors.Green}{kv.Value}{ChatColors.White} wins");
            rank++;
        }
    }

    private void EndRoundCleanup()
    {
        _roundActive = false;

        if (_holder != null)
            ClearHolderEffects(_holder);
        _holder = null;

        _tickTimer?.Kill();
        _tickTimer = null;

        RemoveGlow();
        RemoveZoneBeams();
    }

    // ---------------- Respawning & spawn spreading ----------------

    private void RespawnParticipants()
    {
        foreach (var p in Utilities.GetPlayers())
        {
            if (!p.IsValid || !_participants.Contains(p.Slot))
                continue;

            _names[p.Slot] = p.PlayerName;

            if (!p.PawnIsAlive &&
                (p.Team == CsTeam.Terrorist || p.Team == CsTeam.CounterTerrorist))
            {
                p.Respawn();
            }
            else
            {
                // Freeze already alive participants immediately
                var pawn = p.PlayerPawn?.Value;
                if (pawn != null && pawn.IsValid)
                {
                    pawn.MoveType = MoveType_t.MOVETYPE_NONE;
                }
            }
        }
    }

    private void SpreadPlayersAcrossSpawns(List<CCSPlayerController> players)
    {
        var spawns = new List<(Vector origin, QAngle angles)>();

        foreach (var designerName in new[] { "info_player_terrorist", "info_player_counterterrorist", "info_deathmatch_spawn" })
        {
            foreach (var spawn in Utilities.FindAllEntitiesByDesignerName<SpawnPoint>(designerName))
            {
                if (spawn.AbsOrigin != null)
                {
                    var o = spawn.AbsOrigin;
                    var a = spawn.AbsRotation ?? new QAngle(0, 0, 0);
                    spawns.Add((new Vector(o.X, o.Y, o.Z), new QAngle(a.X, a.Y, a.Z)));
                }
            }
        }

        if (spawns.Count == 0)
            return;

        var random = new Random();
        var selectedSpawns = new List<(Vector origin, QAngle angles)>();
        var remainingSpawns = new List<(Vector origin, QAngle angles)>(spawns);

        // First player gets a random spawn
        int firstIdx = random.Next(remainingSpawns.Count);
        selectedSpawns.Add(remainingSpawns[firstIdx]);
        remainingSpawns.RemoveAt(firstIdx);

        // Farthest-point sampling for remaining players
        for (int i = 1; i < players.Count; i++)
        {
            if (remainingSpawns.Count == 0) break; // Out of spawns (should be rare)

            int bestIdx = 0;
            float maxMinDist = -1f;

            for (int j = 0; j < remainingSpawns.Count; j++)
            {
                float minDistToSelected = float.MaxValue;
                foreach (var s in selectedSpawns)
                {
                    float dist = Distance2D(remainingSpawns[j].origin, s.origin);
                    if (dist < minDistToSelected)
                        minDistToSelected = dist;
                }

                if (minDistToSelected > maxMinDist)
                {
                    maxMinDist = minDistToSelected;
                    bestIdx = j;
                }
            }

            selectedSpawns.Add(remainingSpawns[bestIdx]);
            remainingSpawns.RemoveAt(bestIdx);
        }

        int pIdx = 0;
        foreach (var p in players)
        {
            var pawn = p.PlayerPawn?.Value;
            if (pawn == null || !pawn.IsValid) continue;

            var (origin, angles) = selectedSpawns[pIdx % selectedSpawns.Count];
            pawn.Teleport(origin, angles, new Vector(0, 0, 0));
            pIdx++;
        }
    }

    // ---------------- Safe zone ----------------

    private void InitSafeZone()
    {
        // Calculate the shrink time based on the player count (baseline is 4 players, minimum 30 seconds)
        var candidates = GetAliveParticipants();
        int playerCount = Math.Max(candidates.Count, 2);
        _actualShrinkSeconds = Math.Max(Config.SafeZoneShrinkSeconds * (playerCount / 4.0f), 30f);

        // Center = average of all spawn origins, radius = spread * 2
        var origins = new List<Vector>();
        foreach (var designerName in new[] { "info_player_terrorist", "info_player_counterterrorist" })
        {
            foreach (var spawn in Utilities.FindAllEntitiesByDesignerName<SpawnPoint>(designerName))
            {
                if (spawn.AbsOrigin != null)
                    origins.Add(new Vector(spawn.AbsOrigin.X, spawn.AbsOrigin.Y, spawn.AbsOrigin.Z));
            }
        }

        if (origins.Count == 0)
        {
            _zoneCenter = new Vector(0, 0, 0);
            _zoneStartRadius = Config.SafeZoneStartRadius > 0 ? Config.SafeZoneStartRadius : 6000f;
        }
        else
        {
            float cx = origins.Average(o => o.X);
            float cy = origins.Average(o => o.Y);
            float cz = origins.Average(o => o.Z);
            _zoneCenter = new Vector(cx, cy, cz);

            if (Config.SafeZoneStartRadius > 0)
            {
                _zoneStartRadius = Config.SafeZoneStartRadius;
            }
            else
            {
                float maxDist = origins.Max(o => Distance2D(o, _zoneCenter));
                _zoneStartRadius = maxDist * 2f;
            }
        }

        _zoneRadius = _zoneStartRadius;
        _zoneElapsed = 0f;

        if (Config.SafeZoneBeamSegments > 0)
        {
            CreateZoneBeams();
            UpdateZoneBeams();
        }
    }

    private void TickSafeZone()
    {
        if (!Config.SafeZoneEnabled || !_roundActive)
            return;

        _zoneElapsed += 1.0f;

        float t = Math.Clamp(_zoneElapsed / _actualShrinkSeconds, 0f, 1f);
        _zoneRadius = _zoneStartRadius + (Config.SafeZoneMinRadius - _zoneStartRadius) * t;

        // Damage players outside the zone (direct health modification, same
        // path as the potato drain, so the damage-block hook doesn't matter)
        foreach (var p in GetAliveParticipants())
        {
            var pawn = p.PlayerPawn?.Value;
            var origin = pawn?.AbsOrigin;
            if (pawn == null || origin == null) continue;

            if (Distance2D(origin, _zoneCenter) > _zoneRadius)
            {
                pawn.Health -= Config.SafeZoneDamagePerSecond;
                Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");
                p.PrintToCenter($"⚠ OUTSIDE THE ZONE! -{Config.SafeZoneDamagePerSecond} HP/s ⚠");

                if (pawn.Health <= 0)
                {
                    _bypassDamageBlock = true;
                    try { pawn.CommitSuicide(false, true); }
                    finally { _bypassDamageBlock = false; }

                    Server.PrintToChatAll($" {ChatColors.Gold}[HotPotato]{ChatColors.White} {p.PlayerName} died outside the zone!");
                }
            }
        }

        if (Config.SafeZoneBeamSegments > 0)
        {
            UpdateZoneBeams();
        }
    }

    private void CreateZoneBeams()
    {
        // FIX: Reuse beams across rounds to prevent CS2 from hitting the edict limit in later rounds!
        if (_zoneBeams.Count > 0) return; 

        if (_zoneCenterEntity == null || !_zoneCenterEntity.IsValid)
        {
            _zoneCenterEntity = Utilities.CreateEntityByName<CBaseEntity>("info_target");
            if (_zoneCenterEntity != null)
            {
                _zoneCenterEntity.Teleport(_zoneCenter, new QAngle(0, 0, 0), new Vector(0, 0, 0));
                _zoneCenterEntity.DispatchSpawn();
            }
        }

        // Override to 32 for dense circle without exceeding engine limits
        int segments = 32; 
        
        int numRings = 4;
        // 4 horizontal rings + 3 short vertical segments per pillar (frustum culling fixed by keeping segments short)
        int totalBeams = segments * (numRings + (numRings - 1)); 
        for (int i = 0; i < totalBeams; i++)
        {
            var beam = Utilities.CreateEntityByName<CBeam>("beam");
            if (beam != null)
            {
                beam.Render = Color.FromArgb(255, 255, 60, 60);
                beam.Width = 3.0f;

                // Force EF_NOCULL (128) so the client never frustum culls the beam even if both endpoints are off-screen
                beam.Effects |= 128;
                Utilities.SetStateChanged(beam, "CBaseEntity", "m_fEffects");

                // Fix CS2 frustum culling completely by forcing a massive bounding box
                beam.Collision.Mins.X = -10000;
                beam.Collision.Mins.Y = -10000;
                beam.Collision.Mins.Z = -10000;
                beam.Collision.Maxs.X = 10000;
                beam.Collision.Maxs.Y = 10000;
                beam.Collision.Maxs.Z = 10000;
                beam.Collision.SolidType = SolidType_t.SOLID_BBOX;

                beam.DispatchSpawn();

                if (_zoneCenterEntity != null && _zoneCenterEntity.IsValid)
                {
                    beam.AcceptInput("SetParent", _zoneCenterEntity, null, "");
                }

                Utilities.SetStateChanged(beam, "CCollisionProperty", "m_vecMins");
                Utilities.SetStateChanged(beam, "CCollisionProperty", "m_vecMaxs");
                Utilities.SetStateChanged(beam, "CCollisionProperty", "m_nSolidType");

                _zoneBeams.Add(beam);
            }
        }
    }

    private void UpdateZoneBeams()
    {
        if (_zoneCenterEntity != null && _zoneCenterEntity.IsValid)
        {
            _zoneCenterEntity.Teleport(_zoneCenter, new QAngle(0, 0, 0), new Vector(0, 0, 0));
        }

        int maxSegments = 32;
        int numRings = 4;
        if (_zoneBeams.Count < maxSegments * (numRings + (numRings - 1))) return;

        // Keep segments constant so the beams are always short enough to not be completely off-screen!
        int activeSegments = maxSegments;

        // Extend the cage height lower and higher
        float zLow = _zoneCenter.Z - 300f;
        float zHigh = _zoneCenter.Z + Math.Max(Config.SafeZoneWallHeight, 600f);

        // Calculate ring heights
        float[] ringHeights = new float[numRings];
        for (int r = 0; r < numRings; r++)
        {
            ringHeights[r] = zLow + ((zHigh - zLow) * ((float)r / (numRings - 1)));
        }

        var ringPoints = new Vector[numRings][];
        for (int r = 0; r < numRings; r++)
        {
            ringPoints[r] = new Vector[activeSegments + 1];
        }
        
        for (int i = 0; i <= activeSegments; i++)
        {
            float a = (float)(2 * Math.PI * i / activeSegments);
            float x = _zoneCenter.X + _zoneRadius * MathF.Cos(a);
            float y = _zoneCenter.Y + _zoneRadius * MathF.Sin(a);
            for (int r = 0; r < numRings; r++)
            {
                ringPoints[r][i] = new Vector(x, y, ringHeights[r]);
            }
        }

        int beamIndex = 0;
        for (int i = 0; i < activeSegments; i++)
        {
            // Draw horizontal rings
            for (int r = 0; r < numRings; r++)
            {
                UpdateBeam(_zoneBeams[beamIndex++], ringPoints[r][i], ringPoints[r][i + 1]);
            }
            // Draw vertical pillars split into multiple shorter segments to prevent frustum culling
            for (int r = 0; r < numRings - 1; r++)
            {
                UpdateBeam(_zoneBeams[beamIndex++], ringPoints[r][i], ringPoints[r + 1][i]);
            }
        }

        // Hide unused beams to prevent them from rendering at the origin
        // FIX: Start and end points MUST be different, otherwise CS2 renderer aborts rendering
        // due to zero-length math errors causing the whole circle to vanish.
        Vector underground1 = new Vector(0, 0, -10000);
        Vector underground2 = new Vector(0, 0, -9900);
        while (beamIndex < _zoneBeams.Count)
        {
            UpdateBeam(_zoneBeams[beamIndex++], underground1, underground2);
        }
    }

    private void UpdateBeam(CBeam beam, Vector start, Vector end)
    {
        if (!beam.IsValid) return;
        beam.Teleport(start, new QAngle(0, 0, 0), new Vector(0, 0, 0));
        beam.EndPos.X = end.X;
        beam.EndPos.Y = end.Y;
        beam.EndPos.Z = end.Z;
        Utilities.SetStateChanged(beam, "CBeam", "m_vecEndPos");

        // Force massive bounds AFTER teleport so CS2 doesn't frustum cull it when looking down/away
        beam.Collision.Mins.X = -10000;
        beam.Collision.Mins.Y = -10000;
        beam.Collision.Mins.Z = -10000;
        beam.Collision.Maxs.X = 10000;
        beam.Collision.Maxs.Y = 10000;
        beam.Collision.Maxs.Z = 10000;
        Utilities.SetStateChanged(beam, "CCollisionProperty", "m_vecMins");
        Utilities.SetStateChanged(beam, "CCollisionProperty", "m_vecMaxs");
    }

    private void RemoveZoneBeams()
    {
        foreach (var beam in _zoneBeams)
        {
            if (beam.IsValid)
                beam.Remove();
        }
        _zoneBeams.Clear();

        if (_zoneCenterEntity != null && _zoneCenterEntity.IsValid)
        {
            _zoneCenterEntity.Remove();
            _zoneCenterEntity = null;
        }
    }

    // ---------------- Main game tick ----------------

    private void GameSecondTick()
    {
        if (!_matchActive)
            return;

        if (_freezeRemaining > 0f)
        {
            var candidates = GetAliveParticipants();
            foreach (var p in candidates)
            {
                p.PrintToCenter($"YOU WILL BE ABLE TO MOVE IN: {_freezeRemaining:0} sec");
            }
            
            _freezeRemaining -= 1.0f;
            if (_freezeRemaining <= 0f)
            {
                StartRoundPostFreeze();
            }
            return;
        }

        if (!_roundActive)
            return;

        if (_graceRemaining > 0f)
        {
            _graceRemaining -= 1.0f;
            if (_graceRemaining is 3f or 2f or 1f)
                Server.PrintToChatAll($" {ChatColors.Gold}[HotPotato]{ChatColors.White} Drain starts in {ChatColors.Red}{_graceRemaining:0}{ChatColors.White}...");
            
            if (_holder != null && IsValidAlive(_holder))
            {
                _holder.PrintToCenter($"GRACE PERIOD: {_graceRemaining:0}s\nGET READY!");
            }
            return;
        }

        // Safe zone ticks even if the holder is momentarily invalid
        TickSafeZone();

        if (_holder == null || !IsValidAlive(_holder))
            return;

        _fuseRemaining -= 1.0f;

        var pawn = _holder.PlayerPawn?.Value;
        if (pawn != null && pawn.IsValid)
        {
            pawn.Health -= Config.HealthDrainPerSecond;
            Utilities.SetStateChanged(pawn, "CBaseEntity", "m_iHealth");

            if (pawn.Health <= 0)
            {
                Detonate(_holder);
                return;
            }
        }

        if (_fuseRemaining is 10f or 5f or 3f or 2f or 1f)
            Server.PrintToChatAll($" {ChatColors.Gold}[HotPotato]{ChatColors.Red} {_fuseRemaining:0} seconds!");

        _holder.PrintToCenter($"POTATO EXPLODES IN: {_fuseRemaining:0}s\nPASS IT (E / Knife)!");

        if (_fuseRemaining <= 0f)
        {
            Detonate(_holder);
        }
    }

    // ---------------- Holder management ----------------

    private void SetHolder(CCSPlayerController player)
    {
        if (_holder != null)
            ClearHolderEffects(_holder);

        _holder = player;
        _lastPassTime = Server.CurrentTime;

        ApplyHolderEffects(player);

        player.PrintToCenter("YOU HAVE THE POTATO! PASS IT (E / knife)!");
    }

    private void ApplyHolderEffects(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn?.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        pawn.Render = Color.FromArgb(255, Config.HolderColorR, Config.HolderColorG, Config.HolderColorB);
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");

        pawn.VelocityModifier = Config.SpeedMultiplier;

        if (Config.HolderGlow)
            CreateGlow(pawn);
    }

    private void ClearHolderEffects(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn?.Value;
        if (pawn != null && pawn.IsValid)
        {
            pawn.Render = Color.FromArgb(255, 255, 255, 255);
            Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
            pawn.VelocityModifier = 1.0f;
        }

        RemoveGlow();
    }

    // ---------------- Glow (prop_dynamic overlay trick) ----------------

    private void CreateGlow(CCSPlayerPawn pawn)
    {
        RemoveGlow();

        string modelName = pawn.CBodyComponent?.SceneNode?.GetSkeletonInstance()?.ModelState.ModelName ?? "";
        if (string.IsNullOrEmpty(modelName))
            return;

        _glowRelay = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
        _glowProp = Utilities.CreateEntityByName<CDynamicProp>("prop_dynamic");
        if (_glowRelay == null || _glowProp == null)
        {
            RemoveGlow();
            return;
        }

        _glowRelay.SetModel(modelName);
        _glowRelay.Spawnflags = 256u;
        _glowRelay.RenderMode = RenderMode_t.kRenderNone;
        _glowRelay.DispatchSpawn();

        _glowProp.SetModel(modelName);
        _glowProp.Spawnflags = 256u;
        _glowProp.DispatchSpawn();

        _glowProp.Glow.GlowColorOverride = Color.FromArgb(255, Config.HolderColorR, Config.HolderColorG, Config.HolderColorB);
        _glowProp.Glow.GlowRange = 5000;
        _glowProp.Glow.GlowTeam = -1;
        _glowProp.Glow.GlowType = 3;
        _glowProp.Glow.GlowRangeMin = 0;
        Utilities.SetStateChanged(_glowProp, "CBaseModelEntity", "m_Glow");

        _glowRelay.AcceptInput("FollowEntity", pawn, _glowRelay, "!activator");
        _glowProp.AcceptInput("FollowEntity", _glowRelay, _glowProp, "!activator");
    }

    private void RemoveGlow()
    {
        if (_glowProp != null && _glowProp.IsValid)
            _glowProp.Remove();
        if (_glowRelay != null && _glowRelay.IsValid)
            _glowRelay.Remove();
        _glowProp = null;
        _glowRelay = null;
    }

    // ---------------- Radar blip ----------------

    private void ApplyRadarBlip(CCSPlayerPawn pawn)
    {
        var spotted = pawn.EntitySpottedState;
        spotted.Spotted = true;
        spotted.SpottedByMask[0] = 0xFFFFFFFF;
        spotted.SpottedByMask[1] = 0xFFFFFFFF;
        Utilities.SetStateChanged(pawn, "CCSPlayerPawn", "m_entitySpottedState");
    }

    // ---------------- Pass detection ----------------

    private void OnTick()
    {
        if (!_matchActive)
            return;

        if (_freezeRemaining > 0f)
        {
            foreach (var p in GetAliveParticipants())
            {
                var playerPawn = p.PlayerPawn?.Value;
                if (playerPawn != null && playerPawn.IsValid)
                {
                    playerPawn.MoveType = MoveType_t.MOVETYPE_NONE;
                    playerPawn.AbsVelocity.X = 0;
                    playerPawn.AbsVelocity.Y = 0;
                    playerPawn.AbsVelocity.Z = 0;
                    Utilities.SetStateChanged(playerPawn, "CBaseEntity", "m_vecAbsVelocity");
                }
            }
            return;
        }

        if (!_roundActive || _holder == null || !IsValidAlive(_holder))
            return;

        var pawn = _holder.PlayerPawn?.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        pawn.VelocityModifier = Config.SpeedMultiplier;

        if (Config.HolderRadarBlip)
            ApplyRadarBlip(pawn);

        bool usePressed = (_holder.Buttons & PlayerButtons.Use) != 0;
        if (usePressed && !_lastUsePressed)
        {
            var target = FindNearestPassTarget(_holder);
            if (target != null)
                PassPotato(_holder, target);
        }
        _lastUsePressed = usePressed;
    }

    private CCSPlayerController? FindNearestPassTarget(CCSPlayerController holder)
    {
        var holderPawn = holder.PlayerPawn?.Value;
        if (holderPawn == null) return null;
        var origin = holderPawn.AbsOrigin;
        if (origin == null) return null;

        CCSPlayerController? nearest = null;
        float nearestDist = Config.PassRange;

        foreach (var p in GetAlivePlayers())
        {
            if (p.Slot == holder.Slot || !_participants.Contains(p.Slot))
                continue;

            var targetOrigin = p.PlayerPawn?.Value?.AbsOrigin;
            if (targetOrigin == null) continue;

            float zDiff = Math.Abs(origin.Z - targetOrigin.Z);
            if (zDiff > 80f) continue;

            float dist = Distance2D(origin, targetOrigin);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = p;
            }
        }

        return nearest;
    }

    private void PassPotato(CCSPlayerController from, CCSPlayerController to)
    {
        if (!_roundActive) return;
        if (Server.CurrentTime - _lastPassTime < Config.PassCooldownSeconds) return;
        if (!IsValidAlive(to) || !_participants.Contains(to.Slot)) return;

        SetHolder(to);

        Server.PrintToChatAll($" {ChatColors.Gold}[HotPotato]{ChatColors.White} {from.PlayerName} passed the potato to {ChatColors.Red}{to.PlayerName}{ChatColors.White}!");
        
        to.ExecuteClientCommand("play sounds/ui/armsrace_level_up.vsnd");
        from.ExecuteClientCommand("play sounds/ui/armsrace_kill_01.vsnd");
    }

    // ---------------- Damage handling ----------------

    private HookResult OnPlayerHurt(EventPlayerHurt @event, GameEventInfo info)
    {
        if (!_roundActive || _holder == null || Config.DisablePlayerDamage)
            return HookResult.Continue;

        var attacker = @event.Attacker;
        var victim = @event.Userid;

        if (attacker != null && victim != null &&
            attacker.Slot == _holder.Slot &&
            @event.Weapon.Contains("knife", StringComparison.OrdinalIgnoreCase))
        {
            PassPotato(attacker, victim);
        }

        return HookResult.Continue;
    }

    private HookResult OnTakeDamage(DynamicHook hook)
    {
        if (!_matchActive)
            return HookResult.Continue;

        var damageInfo = hook.GetParam<CTakeDamageInfo>(1);
        var victimEntity = hook.GetParam<CEntityInstance>(0);

        bool blockDamage = !_roundActive || Config.DisablePlayerDamage;
        if (_bypassDamageBlock)
            blockDamage = false;

        if (_roundActive && victimEntity.DesignerName == "player")
        {
            var attackerEntity = damageInfo.Attacker.Value;
            if (attackerEntity != null && attackerEntity.DesignerName == "player" && attackerEntity.Index != victimEntity.Index)
            {
                var victimPawn = victimEntity.As<CCSPlayerPawn>();
                var attackerPawn = attackerEntity.As<CCSPlayerPawn>();

                var victimController = victimPawn.OriginalController?.Value;
                var attackerController = attackerPawn.OriginalController?.Value;

                if (victimController != null && attackerController != null &&
                    _holder != null && attackerController.Slot == _holder.Slot)
                {
                    var weaponName = damageInfo.Ability.Value?.DesignerName ?? "";
                    if (weaponName.Contains("knife", StringComparison.OrdinalIgnoreCase) ||
                        weaponName.Contains("bayonet", StringComparison.OrdinalIgnoreCase))
                    {
                        PassPotato(attackerController, victimController);
                    }
                }
            }
        }

        if (blockDamage)
        {
            damageInfo.Damage = 0;
            return HookResult.Changed;
        }

        return HookResult.Continue;
    }

    // ---------------- Spawn / death / detonation / win ----------------

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

        // Freeze immediately if spawned during freeze period
        if (_freezeRemaining > 0 && _participants.Contains(player.Slot))
        {
            AddTimer(0.05f, () =>
            {
                if (player.IsValid && player.PawnIsAlive)
                {
                    var pawn = player.PlayerPawn?.Value;
                    if (pawn != null && pawn.IsValid)
                    {
                        pawn.MoveType = MoveType_t.MOVETYPE_NONE;
                    }
                }
            });
        }

        AddTimer(0.3f, () =>
        {
            if (!player.IsValid || !player.PawnIsAlive)
                return;

            if (_roundActive && Config.KnivesOnly && _participants.Contains(player.Slot))
                StripToKnife(player);

            if (_roundActive && _holder != null && player.Slot == _holder.Slot)
                ApplyHolderEffects(player);
            else
                ClearHolderEffectsRenderOnly(player);
        });

        return HookResult.Continue;
    }

    private void ClearHolderEffectsRenderOnly(CCSPlayerController player)
    {
        var pawn = player.PlayerPawn?.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        pawn.Render = Color.FromArgb(255, 255, 255, 255);
        Utilities.SetStateChanged(pawn, "CBaseModelEntity", "m_clrRender");
        pawn.VelocityModifier = 1.0f;
    }

    private HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
    {
        if (!_roundActive)
            return HookResult.Continue;

        var player = @event.Userid;
        if (player == null)
            return HookResult.Continue;

        if (_holder != null && player.Slot == _holder.Slot)
        {
            ClearHolderEffects(player);
            Detonate(_holder);
            return HookResult.Continue;
        }

        AddTimer(0.1f, () =>
        {
            if (_roundActive)
                CheckRoundWinner();
        });
        
        return HookResult.Continue;
    }

    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
    {
        if (!_matchActive)
            return HookResult.Continue;

        var player = @event.Userid;
        if (player == null)
            return HookResult.Continue;

        _participants.Remove(player.Slot);

        if (_roundActive && _holder != null && player.Slot == _holder.Slot)
        {
            var alive = GetAliveParticipants();
            if (alive.Count > 0)
            {
                var random = new Random();
                var newHolder = alive[random.Next(alive.Count)];
                SetHolder(newHolder);
                Server.PrintToChatAll($" {ChatColors.Gold}[HotPotato]{ChatColors.White} Holder disconnected! Potato jumps to {ChatColors.Red}{newHolder.PlayerName}{ChatColors.White}!");
            }
            else
            {
                StopMatch(announce: true);
                return HookResult.Continue;
            }
        }

        if (_roundActive)
            CheckRoundWinner();

        return HookResult.Continue;
    }

    private void Detonate(CCSPlayerController holder)
    {
        var pawn = holder.PlayerPawn?.Value;

        Server.PrintToChatAll($" {ChatColors.Gold}[HotPotato]{ChatColors.Red} 💥 {holder.PlayerName} EXPLODED!");

        if (pawn != null && pawn.IsValid && holder.PawnIsAlive)
        {
            ClearHolderEffects(holder);
            _bypassDamageBlock = true;
            try
            {
                pawn.CommitSuicide(true, true);
            }
            finally
            {
                _bypassDamageBlock = false;
            }
        }

        if (!string.IsNullOrWhiteSpace(Config.ExplosionSoundPath))
        {
            foreach (var p in Utilities.GetPlayers())
            {
                if (p.IsValid && !p.IsBot)
                    p.ExecuteClientCommand("play sounds/ui/armsrace_demoted.vsnd");
            }
        }

        RemoveGlow();
        _holder = null;

        AddTimer(0.5f, () =>
        {
            if (!_roundActive) return;

            if (CheckRoundWinner())
                return;

            var alive = GetAliveParticipants();
            if (alive.Count >= 2)
            {
                _fuseRemaining = Config.FuseSeconds;
                _graceRemaining = Math.Min(Config.GraceSeconds, 3f);
                var random = new Random();
                var newHolder = alive[random.Next(alive.Count)];
                SetHolder(newHolder);
                Server.PrintToChatAll($" {ChatColors.Gold}[HotPotato]{ChatColors.White} New potato! {ChatColors.Red}{newHolder.PlayerName}{ChatColors.White} has it!");
            }
        });
    }

    private bool CheckRoundWinner()
    {
        if (!_roundActive)
            return false;

        var alive = GetAliveParticipants();

        if (alive.Count == 1)
        {
            EndRound(alive[0]);
            return true;
        }

        if (alive.Count == 0)
        {
            EndRound(null);
            return true;
        }

        return false;
    }

    // ---------------- Helpers ----------------

    private void StripToKnife(CCSPlayerController player)
    {
        if (!IsValidAlive(player))
            return;

        player.RemoveWeapons();
        player.GiveNamedItem("weapon_knife");
    }

    private static bool IsFreezePeriod()
    {
        var gameRules = Utilities
            .FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules")
            .FirstOrDefault()?.GameRules;

        return gameRules?.FreezePeriod ?? false;
    }

    private static bool IsValidAlive(CCSPlayerController p) =>
        p.IsValid && p.PawnIsAlive && p.Connected == PlayerConnectedState.PlayerConnected;

    private List<CCSPlayerController> GetAlivePlayers() =>
        Utilities.GetPlayers().Where(p => p.IsValid && !p.IsHLTV && p.PawnIsAlive).ToList();

    private List<CCSPlayerController> GetAliveParticipants() =>
        GetAlivePlayers().Where(p => _participants.Contains(p.Slot)).ToList();

    private static float Distance(Vector a, Vector b)
    {
        float dx = a.X - b.X;
        float dy = a.Y - b.Y;
        float dz = a.Z - b.Z;
        return MathF.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    private static float Distance2D(Vector a, Vector b)
    {
        float dx = a.X - b.X;
        float dy = a.Y - b.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }
}
