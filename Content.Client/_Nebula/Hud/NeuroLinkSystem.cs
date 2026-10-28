// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Shared._Nebula.Hud;
using Content.Shared.AlertLevel;
using Content.Shared.GameTicking;
using Content.Shared.Mobs.Components;
using Content.Shared.Pinpointer;
using Content.Shared.Station.Systems;
using Robust.Client.Audio;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Nebula.Hud;

/// <summary>
/// Fills the part of the HUD every neural interface has, whatever its profile: the owner's state, the nearest beacon
/// of the current grid, the station alert level and the link to the servers. Off a station the last known alert level
/// stays, without a link the location and the code freeze. Owns the marker overlay the modules fill.
/// </summary>
public sealed partial class NeuroLinkSystem : EntitySystem
{
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private IEyeManager _eye = default!;
    [Dependency] private IResourceCache _cache = default!;
    [Dependency] private SkewedHudSystem _hud = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private AudioSystem _audio = default!;

    [Dependency] private EntityQuery<NeuroLinkComponent> _linkQuery = default!;
    [Dependency] private EntityQuery<MobStateComponent> _mobStateQuery = default!;
    [Dependency] private EntityQuery<NavMapComponent> _navMapQuery = default!;
    [Dependency] private EntityQuery<AlertLevelComponent> _alertQuery = default!;

    private static readonly TimeSpan InfoInterval = TimeSpan.FromSeconds(1);
    private static readonly SoundSpecifier SignalLostSound = new SoundPathSpecifier("/Audio/_Nebula/Effects/signal_lost.ogg");

    private TimeSpan _nextInfo;

    public override void Initialize()
    {
        base.Initialize();

        _overlay.AddOverlay(new SquadMarkerOverlay(_hud, _transform, _eye, _player, _timing, _cache));
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlay.RemoveOverlay<SquadMarkerOverlay>();
    }

    [SubscribeNetworkEvent]
    private void OnRoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _nextInfo = TimeSpan.Zero;
    }

    #region Component events

    [SubscribeLocalEvent]
    private void OnSignalLost(Entity<NeuroSignalLostComponent> ent, ref ComponentStartup args)
    {
        if (ent.Owner != _player.LocalEntity)
            return;

        _hud.SetSignalLost(true);
        if (_hud.Active)
            _audio.PlayGlobal(SignalLostSound, Filter.Local(), false);
    }

    [SubscribeLocalEvent]
    private void OnSignalRestored(Entity<NeuroSignalLostComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Owner == _player.LocalEntity)
            _hud.SetSignalLost(false);
    }

    /// <summary>
    /// Runs before the modules, which fill the cleared HUD with the new body's data.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnPlayerAttached(LocalPlayerAttachedEvent args)
    {
        _hud.Clear();
        _nextInfo = TimeSpan.Zero;
        _hud.SetSignalLost(HasComp<NeuroSignalLostComponent>(args.Entity));
    }

    [SubscribeLocalEvent]
    private void OnPlayerDetached(LocalPlayerDetachedEvent args)
    {
        _hud.Clear();
    }

    #endregion

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (!_hud.Active
            || _player.LocalEntity is not { } uid
            || !_linkQuery.HasComp(uid))
            return;

        if (_mobStateQuery.TryComp(uid, out var mobState))
            _hud.SetSelfState(mobState.CurrentState);

        if (_timing.RealTime < _nextInfo
            || _hud.SignalLostAt != null)
            return;

        _nextInfo = _timing.RealTime + InfoInterval;
        if (_hud.Profile?.ShowLocation ?? true)
            UpdateLocation(uid);

        if (_hud.Profile?.ShowCode ?? true)
            UpdateAlert(uid);
    }

    /// <summary>
    /// Pushes the location and the code to the HUD again, after something else drew on it.
    /// </summary>
    public void Resync()
    {
        _nextInfo = TimeSpan.Zero;
    }

    #region Helpers

    /// <summary>
    /// Beacon positions are local to the grid, so the player's position is moved into grid space first.
    /// </summary>
    private void UpdateLocation(EntityUid uid)
    {
        string? nearest = null;
        if (Transform(uid).GridUid is { } grid
            && _navMapQuery.TryComp(grid, out var navMap))
        {
            var position = Vector2.Transform(_transform.GetWorldPosition(uid), _transform.GetInvWorldMatrix(grid));
            var minDistance = float.PositiveInfinity;
            foreach (var beacon in navMap.Beacons.Values)
            {
                var distance = (beacon.Position - position).LengthSquared();
                if (distance >= minDistance)
                    continue;

                minDistance = distance;
                nearest = beacon.Text;
            }
        }

        _hud.SetLocation(nearest ?? Loc.GetString("skewed-hud-location-none"));
    }

    private void UpdateAlert(EntityUid uid)
    {
        if (_station.GetOwningStation(uid) is not { } station
            || !_alertQuery.TryComp(station, out var alert)
            || !_proto.Resolve(alert.CurrentAlertLevel, out var level))
            return;

        _hud.SetAlert(level.LocalizedName, level.Color);
    }

    #endregion
}
