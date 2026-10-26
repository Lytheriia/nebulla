// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Administration.Logs;
using Content.Server.Chat.Managers;
using Content.Server.GameTicking;
using Content.Server.Maps;
using Content.Server.Voting;
using Content.Server.Voting.Managers;
using Content.Shared._Nebula.CCVar;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Prototypes;
using Content.Shared.Voting;
using Content.Trauma.Common.CCVar;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._Nebula.GameTicking;

/// <summary>
/// Starts the game mode and map votes by itself in the lobby, following <see cref="ModeRotationPrototype"/>.
/// Keeps the presets of the rounds that actually started, across server restarts.
/// </summary>
public sealed partial class ModeRotationSystem : EntitySystem
{
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IGameMapManager _gameMap = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ILogManager _logManager = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IResourceManager _res = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IVoteManager _vote = default!;
    [Dependency] private ServerGameTicker _ticker = default!;

    private static readonly ResPath HistoryPath = new("/nebula/mode_rotation_history.txt");
    private const int HistoryLength = 5;

    private bool _enabled;
    private ProtoId<ModeRotationPrototype> _rotationId;
    private TimeSpan _voteDelay;
    private TimeSpan _stageDuration;
    private bool _holdLobby;
    private bool _traumaMapVote;

    /// <summary>
    /// Presets of the latest started rounds, most recent first.
    /// </summary>
    private readonly List<string> _history = [];
    private bool _lobbyHandled;
    private TimeSpan? _voteAt;
    private IVoteHandle? _modeVote;
    private VoteFinishedEventArgs? _modeVoteResult;
    private GamePresetPrototype? _expectedPreset;

    /// <summary>
    /// An admin set the preset for several rounds or for good, the rotation leaves it alone.
    /// </summary>
    private bool _presetLocked;
    private bool _lobbyHeld;

    private ISawmill _sawmill = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.CVar(_cfg, NCCVars.RotationEnabled, v => _enabled = v, true);
        Subs.CVar(_cfg, NCCVars.RotationPrototype, v => _rotationId = v, true);
        Subs.CVar(_cfg, NCCVars.RotationVoteDelay, v => _voteDelay = TimeSpan.FromSeconds(v), true);
        Subs.CVar(_cfg, NCCVars.RotationStageDuration, v => _stageDuration = TimeSpan.FromSeconds(v), true);
        Subs.CVar(_cfg, NCCVars.RotationHoldLobby, v => _holdLobby = v, true);
        Subs.CVar(_cfg, TraumaCVars.AutomaticMapVote, v => _traumaMapVote = v, true);
        _sawmill = _logManager.GetSawmill("mode-rotation");

        LoadHistory();
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_ticker.RunLevel != GameRunLevel.PreRoundLobby
            || !_ticker.LobbyEnabled
            || _ticker.DummyTicker)
            return;

        // an admin resumed the countdown, it is not ours to resume anymore
        if (_lobbyHeld && !_ticker.Paused)
            _lobbyHeld = false;

        if (_modeVoteResult is { } result)
        {
            _modeVoteResult = null;
            FinishModeVote(result);
            return;
        }

        if (_modeVote != null)
        {
            if (_ticker.Preset != _expectedPreset)
                CancelModeVote();

            return;
        }

        if (!_enabled)
            return;

        if (!_lobbyHandled)
        {
            _lobbyHandled = true;
            _voteAt = _timing.CurTime + _voteDelay;
            _expectedPreset = _ticker.Preset;
            _presetLocked = _ticker.ResetCountdown != null
                || _ticker.Preset != _ticker.FindGamePreset(_cfg.GetCVar(CCVars.GameLobbyDefaultPreset));
        }

        if (_voteAt is not { } voteAt
            || _timing.CurTime < voteAt
            || _player.PlayerCount == 0)
            return;

        _voteAt = null;
        StartRotation();
    }

    #region Handle events

    [SubscribeLocalEvent]
    private void OnRunLevelChanged(GameRunLevelChangedEvent ev)
    {
        if (ev.New == GameRunLevel.InRound)
        {
            CancelModeVote();

            if (ev.Old == GameRunLevel.PreRoundLobby && _ticker.CurrentPreset is { } preset)
                AddHistory(preset.ID);
        }
    }

    [SubscribeLocalEvent]
    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        CancelModeVote();
        _lobbyHandled = false;
        _voteAt = null;
        _expectedPreset = null;
        _presetLocked = false;
    }

    #endregion

    #region Votes

    /// <summary>
    /// Starts the game mode and map votes of this lobby again right away.
    /// Works with rotation disabled and overrides a preset picked by an admin.
    /// </summary>
    /// <returns>False if the server is not in the lobby.</returns>
    public bool RestartVotes()
    {
        if (_ticker.RunLevel != GameRunLevel.PreRoundLobby)
            return false;

        CancelModeVote();
        CancelMapVotes();

        _lobbyHandled = true;
        _voteAt = null;
        _expectedPreset = _ticker.Preset;
        _presetLocked = false;
        StartRotation(mapVote: true);
        return true;
    }

    /// <param name="mapVote">Start a map vote even when MapVoteOnRoundRestartSystem handles it.</param>
    private void StartRotation(bool mapVote = false)
    {
        PickPreset();

        // MapVoteOnRoundRestartSystem already starts one on lobby entry
        if (mapVote || !_traumaMapVote)
            StartMapVote();
    }

    private void PickPreset()
    {
        // an admin already picked the preset
        if (_presetLocked || _ticker.Preset != _expectedPreset)
            return;

        if (!ProtoMan.TryIndex(_rotationId, out var rotation))
        {
            _sawmill.Error($"Mode rotation prototype {_rotationId} does not exist");
            return;
        }

        var players = _player.PlayerCount;
        if (rotation.Evaluate(players, _history) is not { } rule)
            return;

        _adminLog.Add(LogType.Vote, LogImpact.Medium,
            $"Mode rotation for {players} players, history [{string.Join(", ", _history)}]");

        if (rule.Force is { } forced)
        {
            SetPreset(forced);
            _chat.DispatchServerAnnouncement(Loc.GetString("mode-rotation-forced", ("preset", PresetName(forced))));
            return;
        }

        StartModeVote(rule.Options);
    }

    private void StartModeVote(List<ModeRotationOption> options)
    {
        var voteOptions = new VoteOptions
        {
            Title = Loc.GetString("ui-vote-gamemode-title"),
            Duration = _stageDuration,
        };
        voteOptions.SetInitiatorOrServer(null);

        foreach (var option in options)
        {
            var name = option.Name is { } groupName
                ? Loc.GetString(groupName)
                : PresetName(option.Preset!.Value);
            voteOptions.Options.Add((name, option));
        }

        _modeVote = _vote.CreateVote(voteOptions);
        _modeVote.OnFinished += OnModeVoteFinished;
        _modeVote.OnCancelled += OnModeVoteCancelled;

        if (_holdLobby && !_lobbyHeld)
            _lobbyHeld = _ticker.PauseStart(true, announce: false);
    }

    private void OnModeVoteFinished(IVoteHandle vote, VoteFinishedEventArgs args)
    {
        if (vote != _modeVote)
            return;

        // votes finish inside the VoteManager loop over active votes, a new vote can only be created after it
        _modeVote = null;
        _modeVoteResult = args;
    }

    private void FinishModeVote(VoteFinishedEventArgs args)
    {
        if (_ticker.RunLevel != GameRunLevel.PreRoundLobby
            || _ticker.Preset != _expectedPreset)
        {
            ReleaseLobby();
            return;
        }

        var picked = (ModeRotationOption) (args.Winner ?? _random.Pick(args.Winners));
        var name = picked.Name is { } groupName
            ? Loc.GetString(groupName)
            : PresetName(picked.Preset!.Value);
        _chat.DispatchServerAnnouncement(args.Winner == null
            ? Loc.GetString("ui-vote-gamemode-tie", ("picked", name))
            : Loc.GetString("ui-vote-gamemode-win", ("winner", name)));

        if (picked.Then.Count > 0)
        {
            StartModeVote(picked.Then.Select(preset => new ModeRotationOption { Preset = preset }).ToList());
            return;
        }

        SetPreset(picked.Preset!.Value);
        ReleaseLobby();
    }

    private void OnModeVoteCancelled(IVoteHandle vote)
    {
        // an admin cancelled it, the lobby goes on without a vote
        if (vote != _modeVote)
            return;

        _modeVote = null;
        ReleaseLobby();
    }

    private void CancelModeVote()
    {
        var vote = _modeVote;
        _modeVote = null;
        _modeVoteResult = null;
        vote?.Cancel();
        ReleaseLobby();
    }

    private void StartMapVote()
    {
        if (_gameMap.CurrentlyEligibleMaps().Count() > 1)
            _vote.CreateStandardVote(null, StandardVoteType.Map);
    }

    /// <returns>True if a map vote was running.</returns>
    private bool CancelMapVotes()
    {
        // standard map votes have no handle to keep, the title is the only way to find them
        var mapTitle = Loc.GetString("ui-vote-map-title");
        var cancelled = false;
        foreach (var vote in _vote.ActiveVotes.Where(v => !v.Finished && v.Title == mapTitle).ToList())
        {
            vote.Cancel();
            cancelled = true;
        }

        return cancelled;
    }

    private void ReleaseLobby()
    {
        if (!_lobbyHeld)
            return;

        _lobbyHeld = false;
        if (_ticker.Paused)
            _ticker.PauseStart(false, announce: false);
    }

    #endregion

    #region Helpers

    private void SetPreset(ProtoId<GamePresetPrototype> preset)
    {
        var oldPool = _ticker.Preset?.MapPool;
        _ticker.SetGamePreset(preset, resetDelay: 1);
        _expectedPreset = _ticker.Preset;
        _adminLog.Add(LogType.Vote, LogImpact.Medium, $"Mode rotation set preset {preset}");

        // a running map vote offers the maps of the previous preset
        if (_ticker.Preset?.MapPool != oldPool && CancelMapVotes())
            StartMapVote();
    }

    private string PresetName(ProtoId<GamePresetPrototype> preset)
    {
        return ProtoMan.TryIndex(preset, out var proto) ? Loc.GetString(proto.ModeTitle) : preset.Id;
    }

    private void AddHistory(string preset)
    {
        _history.Insert(0, preset);
        if (_history.Count > HistoryLength)
            _history.RemoveRange(HistoryLength, _history.Count - HistoryLength);

        try
        {
            _res.UserData.CreateDir(HistoryPath.Directory);
            _res.UserData.WriteAllText(HistoryPath, string.Join('\n', _history));
        }
        catch (Exception e)
        {
            _sawmill.Error($"Failed to save mode rotation history: {e}");
        }
    }

    private void LoadHistory()
    {
        _history.Clear();
        // TryReadAllText throws when the folder is missing
        if (!_res.UserData.Exists(HistoryPath)
            || !_res.UserData.TryReadAllText(HistoryPath, out var text))
            return;

        foreach (var line in text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (_history.Count >= HistoryLength)
                break;

            _history.Add(line);
        }
    }

    #endregion
}
