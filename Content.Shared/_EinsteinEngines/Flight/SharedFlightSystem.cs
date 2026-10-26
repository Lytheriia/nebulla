// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Goob-Station/Goob-Station/blob/c5be8fd82d71fa28a4556d9bdc40b86a987ed361/Content.Shared/_EinsteinEngines/Flight/SharedFlightSystem.cs

using Content.Shared._EinsteinEngines.Flight.Events;
using Content.Shared.Actions;
using Content.Shared.Bed.Sleep;
using Content.Shared.Climbing.Events;
using Content.Shared.Cuffs.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Events;
using Content.Shared.Damage.Systems;
using Content.Shared.Gravity;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Components;
using Content.Shared.Inventory.VirtualItem;
using Content.Shared.Mobs;
using Content.Shared.Movement.Systems;
using Content.Shared.Physics;
using Content.Shared.Popups;
using Content.Shared.Standing;
using Content.Shared.Stunnable;
using Content.Shared.Zombies;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._EinsteinEngines.Flight;

public abstract partial class SharedFlightSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private SharedGravitySystem _gravity = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private MovementSpeedModifierSystem _movementSpeed = default!;
    [Dependency] private SharedPhysicsSystem _physics = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedStaminaSystem _stamina = default!;
    [Dependency] private StandingStateSystem _standing = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedVirtualItemSystem _virtualItem = default!;

    private const int FlightHands = 2;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<FlightComponent>();
        while (query.MoveNext(out var uid, out var flight))
        {
            if (!flight.On || _timing.CurTime < flight.NextFlap)
                continue;

            flight.NextFlap = _timing.CurTime + flight.FlapInterval;
            _audio.PlayPredicted(flight.FlapSound, uid, uid);

            if (TryComp<StaminaModifierStatusEffectComponent>(uid, out var modifier))
                _stamina.ModifyStaminaDrain(uid, flight.StaminaDrainKey, flight.StaminaDrainRate * modifier.Modifier * flight.StaminaDrainMultiplier);
        }
    }

    public void ToggleActive(EntityUid uid, bool active, FlightComponent component, bool gracefulStop = true)
    {
        component.On = active;
        component.NextFlap = _timing.CurTime;
        _actions.SetToggled(component.ToggleActionEntity, component.On);
        RaiseLocalEvent(uid, new FlightEvent(uid, component.On, component.IsAnimated));
        _stamina.ToggleStaminaDrain(uid, component.StaminaDrainRate, active, false, component.StaminaDrainKey, uid);

        _gravity.RefreshWeightless(uid);
        _movementSpeed.RefreshWeightlessModifiers(uid);
        ToggleCollisionMasks(uid, component);
        UpdateHands(uid, active);

        if (component.CanFail && !gracefulStop)
            _damageable.TryChangeDamage(uid, component.FailDamageSpecifier);

        Dirty(uid, component);
    }

    [SubscribeLocalEvent]
    private void OnStartup(Entity<FlightComponent> ent, ref ComponentStartup args)
    {
        _actions.AddAction(ent, ref ent.Comp.ToggleActionEntity, ent.Comp.ToggleAction);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<FlightComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.ToggleActionEntity);

        if (ent.Comp.On && !TerminatingOrDeleted(ent))
            ToggleActive(ent, false, ent.Comp);
    }

    [SubscribeLocalEvent]
    private void OnToggleFlight(Entity<FlightComponent> ent, ref ToggleFlightEvent args)
    {
        if (!ent.Comp.On && !CanFly(ent))
            return;

        ToggleActive(ent, !ent.Comp.On, ent.Comp);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnIsWeightless(Entity<FlightComponent> ent, ref IsWeightlessEvent args)
    {
        if (!ent.Comp.On || args.Handled)
            return;

        args.IsWeightless = true;
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnRefreshWeightlessMoveSpeed(Entity<FlightComponent> ent, ref RefreshWeightlessModifiersEvent args)
    {
        if (!ent.Comp.On)
            return;

        args.ModifyAcceleration(ent.Comp.SpeedModifier);
        args.ModifyFriction(ent.Comp.FrictionModifier, ent.Comp.FrictionNoInputModifier);
    }

    [SubscribeLocalEvent]
    private void OnBeforeStaminaDamage(Entity<FlightComponent> ent, ref BeforeStaminaDamageEvent args)
    {
        if (!ent.Comp.On || args.Value > 0)
            return;

        args.Value *= ent.Comp.StaminaRegenMultiplier;
    }

    [SubscribeLocalEvent]
    private void OnCuffableFlightAttempt(Entity<CuffableComponent> ent, ref FlightAttemptEvent args)
    {
        if (ent.Comp.CanStillInteract)
            return;

        _popup.PopupEntity(Loc.GetString("no-flight-while-restrained"), ent, ent, PopupType.Medium);
        args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnZombieFlightAttempt(Entity<ZombieComponent> ent, ref FlightAttemptEvent args)
    {
        _popup.PopupEntity(Loc.GetString("no-flight-while-zombified"), ent, ent, PopupType.Medium);
        args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnStandingStateFlightAttempt(Entity<StandingStateComponent> ent, ref FlightAttemptEvent args)
    {
        if (!_standing.IsDown(ent.AsNullable()))
            return;

        _popup.PopupEntity(Loc.GetString("no-flight-while-lying"), ent, ent, PopupType.Medium);
        args.Cancel();
    }

    [SubscribeLocalEvent]
    private void OnMobStateChanged(Entity<FlightComponent> ent, ref MobStateChangedEvent args)
    {
        if (ent.Comp.On && args.NewMobState is MobState.Critical or MobState.Dead)
            ToggleActive(ent, false, ent.Comp, gracefulStop: false);
    }

    [SubscribeLocalEvent]
    private void OnSleep(Entity<FlightComponent> ent, ref SleepStateChangedEvent args)
    {
        if (ent.Comp.On && args.FellAsleep)
            ToggleActive(ent, false, ent.Comp, gracefulStop: false);
    }

    [SubscribeLocalEvent]
    private void OnDowned(Entity<FlightComponent> ent, ref DownedEvent args)
    {
        if (!ent.Comp.On)
            return;

        ToggleActive(ent, false, ent.Comp, gracefulStop: false);
        RaiseNetworkEvent(new ToggleFlightVisualsEvent(GetNetEntity(ent), false, ent.Comp.IsAnimated));
    }

    [SubscribeLocalEvent]
    private void OnZombified(Entity<FlightComponent> ent, ref EntityZombifiedEvent args)
    {
        StopFlight(ent);
    }

    [SubscribeLocalEvent]
    private void OnKnockedDown(Entity<FlightComponent> ent, ref KnockedDownEvent args)
    {
        StopFlight(ent);
    }

    [SubscribeLocalEvent]
    private void OnStunned(Entity<FlightComponent> ent, ref StunnedEvent args)
    {
        StopFlight(ent);
    }

    [SubscribeLocalEvent]
    private void OnAttemptClimb(Entity<FlightComponent> ent, ref AttemptClimbEvent args)
    {
        if (ent.Comp.On)
            args.Cancelled = true;
    }

    private void StopFlight(Entity<FlightComponent> ent)
    {
        if (ent.Comp.On)
            ToggleActive(ent, false, ent.Comp, gracefulStop: false);
    }

    private bool CanFly(EntityUid uid)
    {
        var ev = new FlightAttemptEvent();
        RaiseLocalEvent(uid, ref ev);
        return !ev.Cancelled;
    }

    private void ToggleCollisionMasks(EntityUid uid, FlightComponent component)
    {
        if (!TryComp<FixturesComponent>(uid, out var fixtures))
            return;

        if (component.On)
        {
            foreach (var (key, fixture) in fixtures.Fixtures)
            {
                var newMask = (fixture.CollisionMask
                    & (int) ~CollisionGroup.HighImpassable
                    & (int) ~CollisionGroup.MidImpassable)
                    | (int) CollisionGroup.InteractImpassable;

                if (fixture.CollisionMask == newMask)
                    continue;

                component.ChangedFixtures.Add((key, fixture.CollisionMask));
                _physics.SetCollisionMask(uid, key, fixture, newMask, manager: fixtures);
            }

            return;
        }

        foreach (var (key, originalMask) in component.ChangedFixtures)
        {
            if (fixtures.Fixtures.TryGetValue(key, out var fixture))
                _physics.SetCollisionMask(uid, key, fixture, UndoFlightMask(fixture.CollisionMask, originalMask), fixtures);
        }

        component.ChangedFixtures.Clear();
    }

    private static int UndoFlightMask(int currentMask, int originalMask)
    {
        var restored = currentMask
            | (originalMask & (int) (CollisionGroup.HighImpassable | CollisionGroup.MidImpassable));

        if ((originalMask & (int) CollisionGroup.InteractImpassable) == 0)
            restored &= (int) ~CollisionGroup.InteractImpassable;

        return restored;
    }

    private void UpdateHands(EntityUid uid, bool flying)
    {
        if (!TryComp<HandsComponent>(uid, out var hands))
            return;

        if (!flying)
        {
            _virtualItem.DeleteInHandsMatching(uid, uid);
            return;
        }

        var freeHands = 0;
        foreach (var hand in _hands.EnumerateHands((uid, hands)))
        {
            if (_hands.TryGetHeldItem((uid, hands), hand, out var held)
                && (!HasComp<UnremoveableComponent>(held) || held == uid))
            {
                _hands.DoDrop((uid, hands), hand);
            }
            else if (held != null)
            {
                continue;
            }

            if (++freeHands == FlightHands)
                break;
        }

        for (var i = 0; i < FlightHands; i++)
        {
            if (_virtualItem.TrySpawnVirtualItemInHand(uid, uid, out var virtualItem))
                EnsureComp<UnremoveableComponent>(virtualItem.Value);
        }
    }
}
