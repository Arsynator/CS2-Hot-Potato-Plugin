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
    public int SafeZoneBeamSegments { get; set; } = 24;

    // Extra wall height above the highest spawn point. On vertical maps the
    // wall automatically spans from below the lowest spawn to above the
    // highest one; this adds margin on top.
    [JsonPropertyName("SafeZoneWallHeight")]
    public float SafeZoneWallHeight { get; set; } = 400f;

    // How fast (units/second) the zone center drifts to force movement.
    // 0 disables drifting entirely.
    [JsonPropertyName("SafeZoneDriftPerSecond")]
    public float SafeZoneDriftPerSecond { get; set; } = 12f;

    // ---- Anti-camp ----

    // Reveal players on the radar if they stay in one spot too long
    [JsonPropertyName("AntiCampEnabled")]
    public bool AntiCampEnabled { get; set; } = true;

    // How long (seconds) a player can stay within AntiCampRadius before
    // being revealed
    [JsonPropertyName("AntiCampSeconds")]
    public float AntiCampSeconds { get; set; } = 8f;

    // How far (units) a player must move to reset the camp timer
    [JsonPropertyName("AntiCampRadius")]
    public float AntiCampRadius { get; set; } = 150f;

    // How long (seconds) the camper stays revealed on the radar
    [JsonPropertyName("AntiCampRevealSeconds")]
    public float AntiCampRevealSeconds { get; set; } = 3f;

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
    public override string ModuleVersion => "2.2.0";
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
        config.SafeZoneDriftPerSecond = Math.Clamp(config.SafeZoneDriftPerSecond, 0f, 200f);
        config.AntiCampSeconds = Math.Clamp(config.AntiCampSeconds, 2f, 120f);
        config.AntiCampRadius = Math.Clamp(config.AntiCampRadius, 30f, 1000f);
        config.AntiCampRevealSeconds = Math.Clamp(config.AntiCampRevealSeconds, 1f, 30f);

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
    private readonly List<CBeam> _zoneBeams = new();
    private float _lastBeamUpdate = 0f;
    private float _zoneWallLow = 0f;
    private float _zoneWallHigh = 0f;
    private Vector _zoneOriginalCenter = new(0, 0, 0);
    private Vector _zoneDriftTarget = new(0, 0, 0);
    private float _nextDriftRetarget = 0f;

    // ---------------- HUD & anti-camp state ----------------
    private readonly HashSet<int> _outsideZone = new();
    private readonly Dictionary<int, Vector> _campAnchor = new();
    private readonly Dictionary<int, float> _campAnchorTime = new();
    private readonly Dictionary<int, float> _campRevealUntil = new();
    private CounterStrikeSharp.API.Modules.Timers.Timer? _hudTimer;

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

        // Unified center HUD: one place composes potato status, zone warning,
        // and camp reveal so the messages never overwrite each other
        _hudTimer = AddTimer(0.5f, UpdateHud, TimerFlags.REPEAT);
    }

    public override void Unload(bool hotReload)
    {
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(OnTakeDamage, HookMode.Pre);
        _hudTimer?.Kill();
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

        if (announce)
            Server.PrintToChatAll($" {ChatColors.Gold}[HotPotato]{ChatColors.White} Match stopped, server restored.");
    }

    // ---------------- Round lifecycle ----------------

    private void StartRound()
    {
        if (!_matchActive) return;

        _currentRound++;

        // Bring everyone back and spread them out
        RespawnParticipants();

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
                }
            }

            if (Config.KnivesOnly)
            {
                foreach (var p in candidates)
                    StripToKnife(p);
            }

            if (Config.SafeZoneEnabled)
                InitSafeZone();

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

            _tickTimer?.Kill();
            _tickTimer = AddTimer(1.0f, GameSecondTick, TimerFlags.REPEAT);
        });
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

        _outsideZone.Clear();
        _campAnchor.Clear();
        _campAnchorTime.Clear();
        _campRevealUntil.Clear();
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

        // Shuffle spawns, hand out one per player (wrap if more players than spawns)
        var random = new Random();
        var shuffled = spawns.OrderBy(_ => random.Next()).ToList();

        int i = 0;
        foreach (var p in players)
        {
            var pawn = p.PlayerPawn?.Value;
            if (pawn == null || !pawn.IsValid) continue;

            var (origin, angles) = shuffled[i % shuffled.Count];
            pawn.Teleport(origin, angles, new Vector(0, 0, 0));
            i++;
        }
    }

    // ---------------- Safe zone ----------------

    private void InitSafeZone()
    {
        // Center = average of all spawn origins, radius = spread + buffer.
        // The wall's vertical span is derived from the spawn Z range so the
        // cage is visible on every level of vertical maps like Nuke/Vertigo.
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
            _zoneStartRadius = Config.SafeZoneStartRadius > 0 ? Config.SafeZoneStartRadius : 3000f;
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
                _zoneStartRadius = maxDist + 500f;
            }
        }

        // Vertical wall span: from just below the lowest spawn to
        // SafeZoneWallHeight above the highest spawn. On flat maps this
        // degenerates gracefully to roughly the old fixed-height cage.
        if (origins.Count > 0)
        {
            float zMin = origins.Min(o => o.Z);
            float zMax = origins.Max(o => o.Z);
            _zoneWallLow = zMin - 64f;
            _zoneWallHigh = zMax + Config.SafeZoneWallHeight;
        }
        else
        {
            _zoneWallLow = _zoneCenter.Z + 10f;
            _zoneWallHigh = _zoneWallLow + Config.SafeZoneWallHeight;
        }

        _zoneOriginalCenter = new Vector(_zoneCenter.X, _zoneCenter.Y, _zoneCenter.Z);
        _zoneDriftTarget = new Vector(_zoneCenter.X, _zoneCenter.Y, _zoneCenter.Z);
        _nextDriftRetarget = 0f;

        _zoneRadius = _zoneStartRadius;
        _zoneElapsed = 0f;
        _lastBeamUpdate = -999f;
        _outsideZone.Clear();
    }

    private void TickSafeZone()
    {
        if (!Config.SafeZoneEnabled || !_roundActive)
            return;

        _zoneElapsed += 1.0f;

        float t = Math.Clamp(_zoneElapsed / Config.SafeZoneShrinkSeconds, 0f, 1f);
        _zoneRadius = _zoneStartRadius + (Config.SafeZoneMinRadius - _zoneStartRadius) * t;

        TickZoneDrift();

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
                _outsideZone.Add(p.Slot); // HUD compositor shows the warning

                if (pawn.Health <= 0)
                {
                    _bypassDamageBlock = true;
                    try { pawn.CommitSuicide(false, true); }
                    finally { _bypassDamageBlock = false; }

                    Server.PrintToChatAll($" {ChatColors.Gold}[HotPotato]{ChatColors.White} {p.PlayerName} died outside the zone!");
                }
            }
            else
            {
                _outsideZone.Remove(p.Slot);
            }
        }

        // Refresh the visual ring every 3 seconds (recreating beams too often
        // causes entity churn)
        if (Config.SafeZoneBeamSegments > 0 && _zoneElapsed - _lastBeamUpdate >= 3f)
        {
            _lastBeamUpdate = _zoneElapsed;
            DrawZoneRing();
        }
    }

    // Slowly moves the zone center toward a wandering target so players are
    // forced to reposition even inside the safe area. The target is re-rolled
    // periodically within a fraction of the CURRENT radius around the
    // original center, so the zone never wanders off the playable area.
    private void TickZoneDrift()
    {
        if (Config.SafeZoneDriftPerSecond <= 0f)
            return;

        if (_zoneElapsed >= _nextDriftRetarget)
        {
            _nextDriftRetarget = _zoneElapsed + 15f;
            var random = new Random();
            float angle = (float)(random.NextDouble() * 2 * Math.PI);
            float dist = (float)(random.NextDouble() * _zoneRadius * 0.4f);
            _zoneDriftTarget = new Vector(
                _zoneOriginalCenter.X + dist * MathF.Cos(angle),
                _zoneOriginalCenter.Y + dist * MathF.Sin(angle),
                _zoneCenter.Z);
        }

        float dx = _zoneDriftTarget.X - _zoneCenter.X;
        float dy = _zoneDriftTarget.Y - _zoneCenter.Y;
        float d = MathF.Sqrt(dx * dx + dy * dy);
        if (d < 1f)
            return;

        float step = Math.Min(Config.SafeZoneDriftPerSecond, d);
        _zoneCenter = new Vector(
            _zoneCenter.X + dx / d * step,
            _zoneCenter.Y + dy / d * step,
            _zoneCenter.Z);
    }

    private void DrawZoneRing()
    {
        RemoveZoneBeams();

        int segments = Config.SafeZoneBeamSegments;
        float zLow = _zoneWallLow;
        float zHigh = Math.Max(_zoneWallHigh, zLow + 200f);

        // Ring count scales with the vertical span so tall maps (Nuke,
        // Vertigo) get intermediate rings on every level: one ring per
        // ~350 units of height, between 2 and 4 rings total. Posts connect
        // consecutive rings so the wall reads as a cage from anywhere.
        int numRings = Math.Clamp((int)MathF.Ceiling((zHigh - zLow) / 350f) + 1, 2, 4);

        var ringZ = new float[numRings];
        for (int r = 0; r < numRings; r++)
            ringZ[r] = zLow + (zHigh - zLow) * r / (numRings - 1);

        var points = new Vector[numRings][];
        for (int r = 0; r < numRings; r++)
            points[r] = new Vector[segments + 1];

        for (int i = 0; i <= segments; i++)
        {
            float a = (float)(2 * Math.PI * i / segments);
            float x = _zoneCenter.X + _zoneRadius * MathF.Cos(a);
            float y = _zoneCenter.Y + _zoneRadius * MathF.Sin(a);
            for (int r = 0; r < numRings; r++)
                points[r][i] = new Vector(x, y, ringZ[r]);
        }

        for (int i = 0; i < segments; i++)
        {
            for (int r = 0; r < numRings; r++)
                SpawnBeam(points[r][i], points[r][i + 1]);      // rings
            for (int r = 0; r < numRings - 1; r++)
                SpawnBeam(points[r][i], points[r + 1][i]);      // posts
        }
    }

    private void SpawnBeam(Vector start, Vector end)
    {
        var beam = Utilities.CreateEntityByName<CBeam>("beam");
        if (beam == null) return;

        beam.Render = Color.FromArgb(255, 255, 60, 60);
        beam.Width = 3.0f;
        beam.Teleport(start, new QAngle(0, 0, 0), new Vector(0, 0, 0));
        beam.EndPos.X = end.X;
        beam.EndPos.Y = end.Y;
        beam.EndPos.Z = end.Z;
        beam.DispatchSpawn();

        _zoneBeams.Add(beam);
    }

    private void RemoveZoneBeams()
    {
        foreach (var beam in _zoneBeams)
        {
            if (beam.IsValid)
                beam.Remove();
        }
        _zoneBeams.Clear();
    }

    // ---------------- Main game tick ----------------

    private void GameSecondTick()
    {
        if (!_matchActive || !_roundActive)
            return;

        if (IsFreezePeriod())
            return;

        if (_graceRemaining > 0f)
        {
            _graceRemaining -= 1.0f;
            if (_graceRemaining is 3f or 2f or 1f)
                Server.PrintToChatAll($" {ChatColors.Gold}[HotPotato]{ChatColors.White} Drain starts in {ChatColors.Red}{_graceRemaining:0}{ChatColors.White}...");
            return;
        }

        // Safe zone ticks even if the holder is momentarily invalid
        TickSafeZone();

        if (Config.AntiCampEnabled)
            TickAntiCamp();

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
        if (!_matchActive || !_roundActive || _holder == null || !IsValidAlive(_holder))
            return;

        var pawn = _holder.PlayerPawn?.Value;
        if (pawn == null || !pawn.IsValid)
            return;

        pawn.VelocityModifier = Config.SpeedMultiplier;

        if (Config.HolderRadarBlip)
            ApplyRadarBlip(pawn);

        // Revealed campers blip on the radar for the reveal duration
        if (Config.AntiCampEnabled && _campRevealUntil.Count > 0)
        {
            foreach (var kv in _campRevealUntil)
            {
                if (kv.Value <= Server.CurrentTime) continue;
                var camper = Utilities.GetPlayerFromSlot(kv.Key);
                var camperPawn = camper?.PlayerPawn?.Value;
                if (camper != null && camper.IsValid && camper.PawnIsAlive &&
                    camperPawn != null && camperPawn.IsValid)
                {
                    ApplyRadarBlip(camperPawn);
                }
            }
        }

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

            float dist = Distance(origin, targetOrigin);
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
        if (!_roundActive || !Config.DisablePlayerDamage)
            return HookResult.Continue;

        if (_bypassDamageBlock)
            return HookResult.Continue;

        var victimEntity = hook.GetParam<CEntityInstance>(0);
        if (victimEntity.DesignerName != "player")
            return HookResult.Continue;

        var damageInfo = hook.GetParam<CTakeDamageInfo>(1);

        var attackerEntity = damageInfo.Attacker.Value;
        if (attackerEntity == null || attackerEntity.DesignerName != "player")
            return HookResult.Continue;

        if (attackerEntity.Index == victimEntity.Index)
            return HookResult.Continue;

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

        return HookResult.Handled;
    }

    // ---------------- Spawn / death / detonation / win ----------------

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event, GameEventInfo info)
    {
        var player = @event.Userid;
        if (player == null || !player.IsValid)
            return HookResult.Continue;

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

        CheckRoundWinner();
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
        _outsideZone.Remove(player.Slot);
        _campAnchor.Remove(player.Slot);
        _campAnchorTime.Remove(player.Slot);
        _campRevealUntil.Remove(player.Slot);

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
                    p.ExecuteClientCommand($"play {Config.ExplosionSoundPath}");
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

    // ---------------- Anti-camp ----------------

    // If a non-holder stays within AntiCampRadius for AntiCampSeconds, they
    // get revealed on everyone's radar for AntiCampRevealSeconds. The holder
    // is exempt: camping as the holder is already suicide via the drain.
    private void TickAntiCamp()
    {
        foreach (var p in GetAliveParticipants())
        {
            if (_holder != null && p.Slot == _holder.Slot)
            {
                _campAnchor.Remove(p.Slot);
                _campAnchorTime.Remove(p.Slot);
                continue;
            }

            var origin = p.PlayerPawn?.Value?.AbsOrigin;
            if (origin == null) continue;

            var pos = new Vector(origin.X, origin.Y, origin.Z);

            if (!_campAnchor.TryGetValue(p.Slot, out var anchor) ||
                Distance2D(pos, anchor) > Config.AntiCampRadius)
            {
                // Moved enough (or first sample): reset the anchor
                _campAnchor[p.Slot] = pos;
                _campAnchorTime[p.Slot] = _zoneElapsed;
                continue;
            }

            if (_zoneElapsed - _campAnchorTime.GetValueOrDefault(p.Slot) >= Config.AntiCampSeconds)
            {
                _campRevealUntil[p.Slot] = Server.CurrentTime + Config.AntiCampRevealSeconds;
                _campAnchorTime[p.Slot] = _zoneElapsed; // re-arm for repeat offenders
                Server.PrintToChatAll($" {ChatColors.Gold}[HotPotato]{ChatColors.White} 👁 {ChatColors.Red}{p.PlayerName}{ChatColors.White} is camping and has been revealed!");
            }
        }
    }

    // ---------------- Unified center HUD ----------------

    // Single compositor for all center-screen text so the potato status,
    // zone warning, and camp reveal never overwrite each other.
    private void UpdateHud()
    {
        if (!_matchActive || !_roundActive)
            return;

        foreach (var p in GetAliveParticipants())
        {
            var lines = new List<string>();
            bool isHolder = _holder != null && p.Slot == _holder.Slot;

            if (isHolder)
            {
                if (_graceRemaining > 0f)
                {
                    lines.Add("<font color='#FFD700'>🥔 YOU HAVE THE POTATO</font>");
                    lines.Add($"<font color='#00FF00'>Grace period: {_graceRemaining:0}s — get ready!</font>");
                }
                else
                {
                    lines.Add("<font color='#FFD700'>🥔 YOU HAVE THE POTATO</font>");
                    lines.Add($"<font color='#FF4444'>EXPLODES IN {Math.Max(_fuseRemaining, 0):0}s</font>");
                    lines.Add("<font color='#CCCCCC'>Pass it: E or knife hit</font>");
                }
            }

            if (_outsideZone.Contains(p.Slot))
                lines.Add($"<font color='#FF3333'>⚠ OUTSIDE THE ZONE: -{Config.SafeZoneDamagePerSecond} HP/s ⚠</font>");

            if (_campRevealUntil.TryGetValue(p.Slot, out float until) && until > Server.CurrentTime)
                lines.Add("<font color='#FF8800'>👁 CAMPING — POSITION REVEALED</font>");

            if (lines.Count > 0)
                p.PrintToCenterHtml(string.Join("<br>", lines));
        }
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
