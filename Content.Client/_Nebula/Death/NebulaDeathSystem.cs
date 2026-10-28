// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Nebula.Hud;
using Content.Client.GameTicking;
using Content.Client.Hands.Systems;
using Content.Client.Inventory;
using Content.Shared._Nebula.CCVar;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Client.Audio;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client._Nebula.Death;

/// <summary>
/// How the local player sees their own death. Everyone: crit greys the picture a little on top of the vanilla
/// vignette, death greys it out, blurs it and closes it to near dark over a few seconds while the heart stops.
/// With the neural interface HUD on, the HUD also breaks up, the screen glitches, the monitor flatlines
/// and "signal lost" shows up. The game interface fades out with the picture, the chat and the top menu come back
/// a moment later. Coming back to life or leaving the body drops the effect at once.
/// </summary>
public sealed partial class NebulaDeathSystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IResourceCache _cache = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IEyeManager _eye = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private AudioSystem _audio = default!;
    [Dependency] private SkewedHudSystem _hud = default!;
    [Dependency] private ClientGameTicker _ticker = default!;

    [Dependency] private EntityQuery<MobStateComponent> _mobStateQuery = default!;

    private const float CritGrey = 0.35f;
    private const float FadeTime = 4.5f;
    private const float MaxBlur = 2.5f;
    private const float FinalDarkness = 0.85f;
    private const float GlitchTime = 5f;
    private const float GlitchFadeTime = 1.5f;
    private const float CaptionStart = 2.4f;
    private const float CaptionFadeTime = 0.6f;

    private static readonly SoundSpecifier FlatlineSound = new SoundPathSpecifier("/Audio/_Nebula/Effects/flatline.ogg");
    private static readonly SoundSpecifier HeartbeatSound = new SoundPathSpecifier("/Audio/_Lavaland/heartbeat.ogg");

    /// <summary>
    /// Heartbeats without the HUD: later and quieter each time, then none.
    /// </summary>
    private static readonly (float At, float Volume)[] Heartbeats = [(0f, -2f), (1.5f, -6f), (3.5f, -11f)];

    private NebulaDeathOverlay _deathOverlay = default!;
    private EntityUid? _tracked;
    private TimeSpan? _diedAt;
    private bool _visor;
    private int _nextHeartbeat;

    public override void Initialize()
    {
        base.Initialize();

        _deathOverlay = new NebulaDeathOverlay(_proto, _cache, _random, _eye);
        _overlay.AddOverlay(_deathOverlay);
        _cfg.OnValueChanged(CCVars.ReducedMotion, OnReducedMotionChanged, true);
        _cfg.OnValueChanged(NCCVars.HideInterfaceOnDeath, OnHideInterfaceChanged, true);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlay.RemoveOverlay<NebulaDeathOverlay>();
        _cfg.UnsubValueChanged(CCVars.ReducedMotion, OnReducedMotionChanged);
        _cfg.UnsubValueChanged(NCCVars.HideInterfaceOnDeath, OnHideInterfaceChanged);
        RestoreInterface();
    }

    [SubscribeNetworkEvent]
    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        Reset();
    }

    /// <summary>
    /// The interface comes back before the hands and inventory hide their widgets for the ghost,
    /// otherwise it would show them again over their choice.
    /// </summary>
    [SubscribeLocalEvent(before: [typeof(HandsSystem), typeof(ClientInventorySystem)])]
    private void OnPlayerDetached(Entity<MobStateComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        Reset();
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_player.LocalEntity is not { } uid
            || !_mobStateQuery.TryComp(uid, out var mobState))
        {
            Reset();
            return;
        }

        var attached = uid != _tracked;
        if (attached)
        {
            Reset();
            _tracked = uid;
        }

        if (mobState.CurrentState == MobState.Dead)
        {
            if (_diedAt == null)
                StartDeath(!attached);

            UpdateDeath();
            return;
        }

        if (_diedAt != null)
            EndDeath();

        _deathOverlay.Grey = mobState.CurrentState is MobState.Critical or MobState.SoftCrit ? CritGrey : 0f;
    }

    #region Helpers

    private void OnReducedMotionChanged(bool reduced)
    {
        _deathOverlay.Motion = !reduced;
    }

    /// <summary>
    /// Without <paramref name="animate"/>, as when a ghost returns to its dead body, it starts at the end state silently.
    /// </summary>
    private void StartDeath(bool animate)
    {
        _diedAt = animate ? _timing.RealTime : _timing.RealTime - TimeSpan.FromSeconds(Math.Max(GlitchTime + GlitchFadeTime, InterfaceReturnStart + InterfaceReturnTime));
        _visor = _hud.Active;
        _nextHeartbeat = animate ? 0 : Heartbeats.Length;
        HideInterface();
        if (!_visor)
            return;

        var shiftTime = _timing.CurTime - _ticker.RoundStartTimeSpan;
        var profile = _hud.Profile;
        _deathOverlay.Accent = profile?.Accent ?? NebulaDeathOverlay.DefaultAccent;
        _deathOverlay.Alert = profile?.Alert ?? NebulaDeathOverlay.DefaultAlert;
        _deathOverlay.CaptionText = Loc.GetString("nebula-death-flatline");
        _deathOverlay.CaptionSub = Loc.GetString("nebula-death-flatline-sub",
            ("tag", profile?.Tag ?? string.Empty),
            ("time", shiftTime.ToString(@"hh\:mm\:ss")));
        _hud.SetFlatline(true);
        if (animate)
            _audio.PlayGlobal(FlatlineSound, Filter.Local(), false);
    }

    private void UpdateDeath()
    {
        if (_diedAt is not { } diedAt)
            return;

        var elapsed = (float) (_timing.RealTime - diedAt).TotalSeconds;
        var progress = Math.Clamp(elapsed / FadeTime, 0f, 1f);
        _deathOverlay.Grey = CritGrey + (1f - CritGrey) * progress;
        _deathOverlay.Blur = MaxBlur * progress;
        _deathOverlay.Vignette = progress;
        _deathOverlay.Darkness = FinalDarkness * progress;
        UpdateInterface(elapsed);

        if (_visor)
        {
            _deathOverlay.Glitch = elapsed < GlitchTime ? 1f : Math.Max(0f, 1f - (elapsed - GlitchTime) / GlitchFadeTime);
            _deathOverlay.Caption = Math.Clamp((elapsed - CaptionStart) / CaptionFadeTime, 0f, 1f);
            return;
        }

        while (_nextHeartbeat < Heartbeats.Length && elapsed >= Heartbeats[_nextHeartbeat].At)
        {
            var volume = Heartbeats[_nextHeartbeat].Volume;
            _audio.PlayGlobal(HeartbeatSound, Filter.Local(), false, AudioParams.Default.WithVolume(volume));
            _nextHeartbeat++;
        }
    }

    private void EndDeath()
    {
        if (_visor)
            _hud.SetFlatline(false);

        _diedAt = null;
        RestoreInterface();
        ClearOverlay();
    }

    private void Reset()
    {
        if (_diedAt != null)
            EndDeath();

        _tracked = null;
        ClearOverlay();
    }

    private void ClearOverlay()
    {
        _deathOverlay.Grey = 0f;
        _deathOverlay.Blur = 0f;
        _deathOverlay.Vignette = 0f;
        _deathOverlay.Darkness = 0f;
        _deathOverlay.Glitch = 0f;
        _deathOverlay.Caption = 0f;
    }

    #endregion
}
