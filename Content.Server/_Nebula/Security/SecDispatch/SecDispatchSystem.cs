// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Administration.Logs;
using Content.Server.Pinpointer;
using Content.Server.Radio.EntitySystems;
using Content.Shared._Nebula.Security.SecDispatch;
using Content.Shared.ActionBlocker;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.Radio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._Nebula.Security.SecDispatch;

public sealed partial class SecDispatchSystem : SharedSecDispatchSystem
{
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private IAdminLogManager _adminLog = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private NavMapSystem _navMap = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private RadioSystem _radio = default!;

    /// <summary>
    /// Mask sending the combat report right now, so nested radio messages sent meanwhile aren't counted.
    /// </summary>
    private EntityUid? _combatSource;

    /// <summary>
    /// Wearers of headsets that received the combat report being sent right now, null outside of sending.
    /// </summary>
    private Filter? _combatListeners;

    #region BUI events

    [SubscribeLocalEvent]
    private void HandleDispatchMessage(Entity<SecDispatchComponent> ent, ref SecDispatchMessage args)
    {
        var user = args.Actor;
        if (!IsWornBy(ent.Owner, user)
            || !_actionBlocker.CanConsciouslyPerformAction(user)
            || !TryComp<ActionComponent>(ent.Comp.ActionEntity, out var action))
            return;

        if (!HasMindShield(user))
        {
            Popup.PopupEntity(Loc.GetString("sec-dispatch-no-mindshield-popup"), user, user, PopupType.SmallCaution);
            return;
        }

        if (_actions.IsCooldownActive(action))
        {
            Popup.PopupEntity(Loc.GetString("sec-dispatch-cooldown-popup"), user, user, PopupType.SmallCaution);
            return;
        }

        if (args.Index < 0 || args.Index >= ent.Comp.Entries.Count)
        {
            _adminLog.Add(LogType.Action, LogImpact.High,
                $"{ToPrettyString(user):user} sent invalid sec dispatch index {args.Index} (possible modified client)");
            return;
        }

        var entry = ent.Comp.Entries[args.Index];
        var location = _navMap.GetNearestBeaconString(user, onlyName: true);
        var message = Loc.GetString(entry.Message, ("location", location));

        if (entry.Combat && ent.Comp.CombatSound != null)
        {
            _combatSource = ent.Owner;
            _combatListeners = Filter.Empty();
        }

        _radio.SendRadioMessage(user, message, _proto.Index(ent.Comp.Channel), ent.Owner);

        if (_combatListeners is { } listeners && ent.Comp.CombatSound is { } sound)
            _audio.PlayGlobal(sound, listeners, true);

        _combatSource = null;
        _combatListeners = null;

        _actions.SetCooldown(ent.Comp.ActionEntity, ent.Comp.Cooldown);
    }

    #endregion

    #region Radio events

    [SubscribeLocalEvent]
    private void OnHeadsetReceive(Entity<ActorComponent> ent, ref HeadsetRadioReceiveRelayEvent args)
    {
        if (_combatListeners == null
            || args.RelayedEvent.RadioSource != _combatSource)
            return;

        _combatListeners.AddPlayer(ent.Comp.PlayerSession);
    }

    #endregion
}
