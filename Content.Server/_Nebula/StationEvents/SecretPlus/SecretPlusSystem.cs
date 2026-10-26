// SPDX-License-Identifier: AGPL-3.0-or-later

// Ported from Goob-Station: https://github.com/Goob-Station/Goob-Station

using System.Linq;
using Content.Goobstation.Shared.StationEvents;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.StationEvents;
using Content.Server.StationEvents.Components;
using Content.Shared._Nebula.CCVar;
using Content.Shared._Nebula.StationEvents.SecretPlus;
using Content.Shared.Antag;
using Content.Shared.Antag.Components;
using Content.Shared.Antag.Selectors;
using Content.Shared.Database;
using Content.Shared.EntityTable;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.GameTicking.Rules;
using Content.Shared.Ghost.Components;
using Content.Shared.Humanoid;
using Content.Shared.Random.Helpers;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Nebula.StationEvents.SecretPlus;

/// <summary>
///   Temporary class for caching data.
/// </summary>
public sealed class SelectedEvent
{
    /// <summary>
    ///   The station event prototype
    /// </summary>
    public readonly EntityPrototype Proto;
    public readonly StationEventComponent EvComp;

    public SelectedEvent(EntityPrototype proto, StationEventComponent evComp)
    {
        Proto = proto;
        EvComp = evComp;
    }
}

public sealed class PlayerCount
{
    public int Players;
    public int Ghosts;
}

/// <summary>
///   A scheduler which keeps track of a 'chaos score' which it tries to get closer to 0.
/// </summary>
[UsedImplicitly]
public sealed partial class SecretPlusSystem : GameRuleSystem<SecretPlusComponent>
{
    [Dependency] private AntagSelectionSystem _antagSelection = default!;
    [Dependency] private EntityTableSystem _entityTable = default!;
    [Dependency] private EventManagerSystem _event = default!;
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private ILogManager _log = default!;
    [Dependency] private IPlayerManager _playerManager = default!;

    private static readonly ProtoId<EventTypePrototype> DefaultEventType = "Neutral";

    // cvars
    private float _minimumTimeUntilFirstEvent;
    private float _roundstartChaosScoreMultiplier;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        _sawmill = _log.GetSawmill("secret_plus");

        Subs.CVar(_cfg, NCCVars.SecretPlusFirstEventDelay, value => _minimumTimeUntilFirstEvent = value, true);
        Subs.CVar(_cfg, NCCVars.SecretPlusRoundstartChaosMultiplier, value => _roundstartChaosScoreMultiplier = value, true);
    }

    protected override void Added(Entity<SecretPlusComponent, GameRuleComponent> rule, ref GameRuleAddedEvent args)
    {
        var scheduler = rule.Comp1;
        var totalPlayers = GetTotalPlayerCount(_playerManager.Sessions);
        // set up starting chaos score
        scheduler.ChaosScore =
            -RobustRandom.NextFloat(scheduler.MinStartingChaos * totalPlayers, scheduler.MaxStartingChaos * totalPlayers) *
            _roundstartChaosScoreMultiplier;

        // roll midroundchaos generation variation
        var roll = RobustRandom.NextFloat();
        roll = MathF.Pow(roll, scheduler.ChaosChangeVariationExponent);
        // 50% chance to bias towards either higher chaos or lower chaos
        scheduler.ChaosChangeVariation = MathHelper.Lerp(1f,
                                                         RobustRandom.Prob(0.5f) ? scheduler.ChaosChangeVariationMin : scheduler.ChaosChangeVariationMax,
                                                         roll);
        LogMessage($"Using chaos change multiplier of {scheduler.ChaosChangeVariation}");

        if (GameTicker.RunLevel == GameRunLevel.PreRoundLobby)
            TrySpawnRoundstartAntags((rule, scheduler));

        SetupEvents((rule, scheduler));
    }

    /// <summary>
    ///   Build a list of events to use for the entire story
    /// </summary>
    private void SetupEvents(Entity<SecretPlusComponent> scheduler)
    {
        scheduler.Comp.SelectedEvents.Clear();

        var ignoreTimings = scheduler.Comp.IgnoreTimings;
        var reoccurrenceMult = 1f / GetRamping(scheduler);
        var available = _event.AvailableEvents(ignoreTimings ? int.MaxValue : null,
            ignoreTimings ? TimeSpan.MaxValue : null,
            reoccurrenceMult);

        HashSet<EntProtoId>? allowed = null;
        if (scheduler.Comp.ScheduledGameRules is { } table)
            allowed = _entityTable.GetSpawns(table).ToHashSet();

        foreach (var (proto, stationEvent) in available)
        {
            if (allowed?.Contains(proto.ID) == false
                || scheduler.Comp.DisallowedEvents.Contains(GetEventType(proto)))
                continue;

            scheduler.Comp.SelectedEvents.Add(new SelectedEvent(proto, stationEvent));
        }
    }

    /// <summary>
    ///   Decide what event to run next
    /// </summary>
    protected override void ActiveTick(EntityUid uid, SecretPlusComponent scheduler, GameRuleComponent gameRule, float frameTime)
    {
        var count = CountActivePlayers();
        var ramp = GetRamping((uid, scheduler));
        var speedup = _event.EventSpeedup;
        var mult = scheduler.ChaosChangeVariation;

        scheduler.ChaosScore += count.Players * scheduler.LivingChaosChange * frameTime * ramp * speedup * mult;
        scheduler.ChaosScore += count.Ghosts * scheduler.DeadChaosChange * frameTime * speedup * mult;

        var currTime = Timing.CurTime;
        if (currTime < scheduler.TimeNextEvent)
            return;

        // This is the first event, add an automatic delay
        if (scheduler.TimeNextEvent == TimeSpan.Zero)
        {
            var time = _minimumTimeUntilFirstEvent / speedup;
            scheduler.TimeNextEvent = Timing.CurTime + TimeSpan.FromSeconds(time);
            LogMessage($"Started, first event in {time} seconds");
            return;
        }

        TimeSpan amt = TimeSpan.FromSeconds(RobustRandom.NextDouble(scheduler.EventIntervalMin.TotalSeconds, scheduler.EventIntervalMax.TotalSeconds) / ramp / speedup);
        scheduler.TimeNextEvent = currTime + amt;
        // generally more useful than curTime
        LogMessage($"Chaos score: {scheduler.ChaosScore}, Next event at: {GameTicker.RoundDuration() + amt} (ramping {ramp})");

        SetupEvents((uid, scheduler));

        var selectedEvent = ChooseEvent((uid, scheduler));
        if (selectedEvent != null)
            StartRule((uid, scheduler), selectedEvent.Proto.ID, false);
        else
            LogMessage($"No runnable events");
    }

    /// <summary>
    /// Tries to spawn roundstart antags at the beginning of the round.
    /// </summary>
    private void TrySpawnRoundstartAntags(Entity<SecretPlusComponent> scheduler)
    {
        if (scheduler.Comp.NoRoundstartAntags)
            return;

        var primaryWeightList = ProtoMan.Index(scheduler.Comp.PrimaryAntagsWeightTable);
        var weightList = ProtoMan.Index(scheduler.Comp.RoundStartAntagsWeightTable);

        var count = GetTotalPlayerCount(_playerManager.Sessions);

        LogMessage($"Trying to run roundstart rules, total player count: {count}", false);

        var weights = weightList.Weights.ToDictionary();
        var primaryWeights = primaryWeightList.Weights.ToDictionary();
        int maxIters = 50, i = 0; // in case something dumb is tried
        var origChaos = scheduler.Comp.ChaosScore;
        while (scheduler.Comp.ChaosScore < 0 && i < maxIters)
        {
            i++;

            // on first iter pick a primary antag
            var pick = RobustRandom.Pick(i == 1 ? primaryWeights : weights);

            // on lowpop this may still go no likey even for the primary antag pick and pick thief or something, intended
            GameRuleComponent? ruleComp = null;
            if (!ProtoMan.TryIndex(pick, out var entProto)
                || !entProto.TryComp<GameRuleComponent>(out ruleComp, Factory)
            )
                continue;

            var chaosScore = GetChaosScore(entProto);

            if (chaosScore == null)
            {
                Log.Error($"Tried running roundstart event {entProto.ID}, but chaos score was null");
                continue;
            }
            // negative
            var pickProb = (-scheduler.Comp.ChaosScore) / chaosScore.Value;
            if (i == 1)
                pickProb *= scheduler.Comp.PrimaryAntagChaosBias;
            pickProb = MathF.Min(1f, pickProb); // to shut up debug
            if (!RobustRandom.Prob(pickProb)) // have a chance to re-pick if we have low chaos budget left compared to this
                continue;

            // for admeme presets
            if (!scheduler.Comp.IgnoreIncompatible)
            {
                weights.Remove(pick);

                if (ProtoMan.TryIndex(pick, out IncompatibleGameModesPrototype? incompModes))
                    weights = weights.Where(w => !incompModes.Modes.Contains(w.Key)).ToDictionary();
            }

            IndexAndStartGameMode(pick, entProto, ruleComp);

            if (weights.Count == 0
                || !scheduler.Comp.IgnoreIncompatible
                && ProtoMan.TryIndex<SecretPlusRulePrototype>(pick, out var data)
                && data.LoneRun
            )
                return;
        }

        return;

        void IndexAndStartGameMode(string pick, EntityPrototype pickProto, GameRuleComponent ruleComp)
        {
            if (ruleComp.MinPlayers > count)
                return;

            // pick less antags if we have less chaos left
            var effPlayers = (int) MathF.Round(count * scheduler.Comp.ChaosScore / origChaos);
            LogMessage($"Roundstart rule chosen: {pick} with score {GetChaosScore(pickProto, effPlayers)}");
            StartRule(scheduler, pick, false, effPlayers);
        }
    }

    /// <summary>
    ///   Adds and, optionally, starts a gamerule while respecting rules with variable chaos.
    /// </summary>
    private void StartRule(Entity<SecretPlusComponent> scheduler, string rule, bool doStart = true, int? players = null)
    {
        if (GameTicker.AddGameRule(rule) is not { } ruleEnt)
            return;

        // if we hijack playercount, also hijack how many antags we pick
        if (players != null && TryComp<AntagSelectionComponent>(ruleEnt, out var selection))
            FixAntagCounts(selection, players.Value);

        scheduler.Comp.ChaosScore += GetChaosScore(Prototype(ruleEnt)!, players) ?? 0f;

        if (doStart)
            GameTicker.StartGameRule(ruleEnt.AsNullable());
    }

    /// <summary>
    ///   Replaces every antag count of the rule with a fixed one, calculated for the given player count.
    /// </summary>
    private void FixAntagCounts(AntagSelectionComponent selection, int players)
    {
        var running = 0;
        var antags = new List<AntagCountSelector>(selection.Antags.Length);
        foreach (var antag in selection.Antags)
        {
            var count = _antagSelection.GetTargetAntagCount(antag, players, ref running);
            if (count <= 0)
                continue;

            antags.Add(new FixedAntagCount { Proto = antag.Proto, PlayerRatio = antag.PlayerRatio, Count = count });
        }

        selection.Antags = antags.ToArray();
    }

    /// <summary>
    ///   Count the active players and ghosts on the server to determine how chaos changes.
    /// </summary>
    private PlayerCount CountActivePlayers()
    {
        var count = new PlayerCount();
        foreach (var player in _playerManager.Sessions)
        {
            if (player.AttachedEntity is not { } attached)
                continue;

            // TODO: Consider a custom component here instead of HumanoidProfileComponent to represent
            //        "significant enough to count as a whole player"
            if (HasComp<HumanoidProfileComponent>(attached))
                count.Players += 1;
            // don't count bodyless ghosts aka say roundstart spectators
            // not reliable but works
            else if (TryComp<GhostComponent>(attached, out var ghost) && ghost.CanReturnToBody)
                count.Ghosts += 1;
        }

        count.Players += _event.PlayerCountBias;

        return count;
    }

    /// <summary>
    ///   Chaos score of a rule: per-antag scores times the antag count if any are set, the rule score otherwise.
    /// </summary>
    public float? GetChaosScore(EntityPrototype ruleProto, int? players = null)
    {
        if (ruleProto.TryComp<AntagSelectionComponent>(out var selection, Factory))
        {
            var any = false;
            var score = 0f;
            var running = 0;
            var playerCount = players ?? GetTotalPlayerCount(_playerManager.Sessions);

            foreach (var antag in selection.Antags)
            {
                // still counted, earlier antags take players away from later ones
                var count = _antagSelection.GetTargetAntagCount(antag, playerCount, ref running);
                if (!ProtoMan.TryIndex<SecretPlusAntagPrototype>(antag.Proto.Id, out var antagData))
                    continue;

                any = true;
                score += antagData.Chaos * count;
            }

            if (any)
                return score;
        }

        return ProtoMan.TryIndex<SecretPlusRulePrototype>(ruleProto.ID, out var data) ? data.Chaos : null;
    }

    private ProtoId<EventTypePrototype> GetEventType(EntityPrototype ruleProto)
    {
        return ProtoMan.TryIndex<SecretPlusRulePrototype>(ruleProto.ID, out var data) ? data.EventType : DefaultEventType;
    }

    /// <summary>
    ///   Count all the players on the server.
    /// </summary>
    public int GetTotalPlayerCount(IEnumerable<ICommonSession> pool)
    {
        return pool.Count(session => session.Status is not (SessionStatus.Disconnected or SessionStatus.Zombie))
            + _event.PlayerCountBias;
    }

    public float GetRamping(Entity<SecretPlusComponent> scheduler)
    {
        var curTime = GameTicker.RoundDuration();
        return 1f + (float) curTime.TotalSeconds * scheduler.Comp.SpeedRamping * _event.EventSpeedup;
    }

    /// <summary>
    ///   Picks an event based on current chaos score, events' chaos scores and weights.
    /// </summary>
    private SelectedEvent? ChooseEvent(Entity<SecretPlusComponent> scheduler)
    {
        var possible = scheduler.Comp.SelectedEvents;
        Dictionary<SelectedEvent, float> weights = [];

        foreach (var ev in possible)
        {
            // events without SecretPlus data are not meant for it
            if (GetChaosScore(ev.Proto) is not { } chaosScore)
                continue;

            var weight = chaosScore;
            bool negative = weight < 0f;
            weight = MathF.Abs(weight);
            weight = MathF.Pow(weight, scheduler.Comp.ChaosExponent);
            if (negative) weight = -weight;
            weight += scheduler.Comp.ChaosOffset; // offset negative-chaos events upwards too else they never happen
            weight += weight < 0f ? -scheduler.Comp.ChaosThreshold : scheduler.Comp.ChaosThreshold; // make sure it's not in (-1, 1) to not get absurdly low event probabilities
            var delta = ChaosDelta(-scheduler.Comp.ChaosScore, weight, scheduler.Comp.ChaosMatching, scheduler.Comp.ChaosThreshold * scheduler.Comp.ChaosThreshold);
            weights[ev] = ev.EvComp.Weight / (delta + 1f);
        }

        return weights.Count == 0 ? null : RobustRandom.Pick(weights);
    }

    private float ChaosDelta(float chaos1, float chaos2, float logBase, float differentSignMultiplier)
    {
        float ratio = chaos2 / chaos1;
        if (ratio < 0f) ratio = MathF.Abs(chaos2 * chaos1 / differentSignMultiplier);
        return MathF.Abs(MathF.Log(ratio, logBase));
    }

    private void LogMessage(string message, bool showChat = true)
    {
        // TODO: LogMessage strings all require localization.
        _adminLogger.Add(LogType.EventStarted, showChat ? LogImpact.Medium : LogImpact.High, $"SecretPlus {message}");
        if (showChat)
            _chat.SendAdminAnnouncement("SecretPlus " + message);

    }
}
