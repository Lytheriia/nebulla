// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.DeviceLinking.Components;
using Content.Shared._Nebula.Hud;
using Content.Shared._Nebula.Squads;

namespace Content.Server._Nebula.Squads;

public sealed partial class SecuritySquadSystem
{
    /// <summary>
    /// The timer system drops the active component both when a timer runs out and when it is stopped by hand,
    /// only a passed trigger time means it ran out.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnTimerRemoved(Entity<ActiveSignalTimerComponent> ent, ref ComponentRemove args)
    {
        if (ent.Comp.TriggerTime > _timing.CurTime
            || MetaData(ent).EntityPrototype?.ID != BrigTimer.Id
            || _station.GetOwningStation(ent.Owner) is not { } station
            || !TryComp<SignalTimerComponent>(ent, out var timer))
            return;

        var name = timer.Label.Length > 0
            ? timer.Label
            : _navMap.GetNearestBeaconString(ent.Owner, onlyName: true);

        var ev = new SecurityNeuroTimerEvent(name);
        foreach (var mob in GetMobs(station).Values)
        {
            if (mob.Comp.Commander
                && !HasComp<NeuroSignalLostComponent>(mob)
                && _player.TryGetSessionByEntity(mob, out var session))
                RaiseNetworkEvent(ev, session);
        }

        var message = new SquadTabletTimerFinishedMessage(name);
        var query = EntityQueryEnumerator<SquadTabletComponent>();
        while (query.MoveNext(out var uid, out var tablet))
        {
            if (_ui.IsUiOpen(uid, SquadTabletUiKey.Key)
                && (tablet.Station == station || _station.GetOwningStation(uid) == station))
                _ui.ServerSendUiMessage(uid, SquadTabletUiKey.Key, message);
        }
    }
}
