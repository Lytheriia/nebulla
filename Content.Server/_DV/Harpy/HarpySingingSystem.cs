// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Goob-Station/Goob-Station/blob/c5be8fd82d71fa28a4556d9bdc40b86a987ed361/Content.Server/_DV/Harpy/HarpySingerSystem.cs

using Content.Server.Instruments;
using Content.Shared._DV.Harpy;
using Content.Shared.ActionBlocker;
using Content.Shared.Bed.Sleep;
using Content.Shared.Damage.Systems;
using Content.Shared.Instruments;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Tag;
using Content.Shared.UserInterface;
using Content.Shared.Zombies;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._DV.Harpy;

/// <summary>
/// Stops harpy singing when the harpy can no longer speak, and shows the singing notes.
/// </summary>
public sealed partial class HarpySingingSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private ActionBlockerSystem _blocker = default!;
    [Dependency] private InstrumentSystem _instrument = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private TagSystem _tag = default!;

    private static readonly ProtoId<TagPrototype> MuzzleTag = "Muzzle";
    private const string MaskSlot = "mask";

    [SubscribeLocalEvent]
    private void OnEquipped(Entity<HarpySingerComponent> ent, ref DidEquipEvent args)
    {
        if (args.Slot == MaskSlot && _tag.HasTag(args.Equipment, MuzzleTag))
            CloseMidiUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnMobStateChanged(Entity<HarpySingerComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState is MobState.Critical or MobState.Dead)
            CloseMidiUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnZombified(Entity<HarpySingerComponent> ent, ref EntityZombifiedEvent args)
    {
        CloseMidiUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnStunned(Entity<HarpySingerComponent> ent, ref StunnedEvent args)
    {
        CloseMidiUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnSleep(Entity<HarpySingerComponent> ent, ref SleepStateChangedEvent args)
    {
        if (args.FellAsleep)
            CloseMidiUi(ent);
    }

    [SubscribeLocalEvent]
    private void OnDamageDealt(Entity<HarpySingerComponent> ent, ref DamageDealtEvent args)
    {
        if (ent.Comp.ShutUpDamageThreshold is { } threshold && args.Damage.GetTotal() >= threshold)
            CloseMidiUi(ent);
    }

    [SubscribeLocalEvent(before: [typeof(ActivatableUISystem)])]
    private void OnInstrumentOpen(Entity<HarpySingerComponent> ent, ref OpenUiActionEvent args)
    {
        var muzzled = _inventory.TryGetSlotEntity(ent, MaskSlot, out var mask) && _tag.HasTag(mask.Value, MuzzleTag);

        if (_blocker.CanSpeak(ent) && !muzzled && !HasComp<ZombieComponent>(ent))
            return;

        args.Handled = true;
        _popup.PopupEntity(Loc.GetString("no-sing-while-no-speak"), ent, ent, PopupType.Medium);
    }

    [SubscribeLocalEvent]
    private void OnBoundUIOpened(Entity<HarpySingerComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (args.UiKey is InstrumentUiKey)
            _appearance.SetData(ent, HarpyVisualLayers.Singing, true);
    }

    [SubscribeLocalEvent]
    private void OnBoundUIClosed(Entity<HarpySingerComponent> ent, ref BoundUIClosedEvent args)
    {
        if (args.UiKey is InstrumentUiKey)
            _appearance.SetData(ent, HarpyVisualLayers.Singing, false);
    }

    private void CloseMidiUi(EntityUid uid)
    {
        if (HasComp<ActiveInstrumentComponent>(uid) && TryComp<ActorComponent>(uid, out var actor))
            _instrument.ToggleInstrumentUi(uid, actor.PlayerSession.AttachedEntity ?? uid);
    }
}
