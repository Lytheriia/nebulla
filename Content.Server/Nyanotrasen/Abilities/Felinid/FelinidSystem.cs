// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Goob-Station/Goob-Station/blob/c5be8fd82d71fa28a4556d9bdc40b86a987ed361/Content.Server/Nyanotrasen/Abilities/Felinid/FelinidSystem.cs

using Content.Shared.Actions;
using Content.Shared.Body.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Hands;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Medical;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Nutrition.Prototypes;
using Content.Shared.Nyanotrasen.Abilities.Felinid;
using Content.Shared.Nyanotrasen.Actions.Events;
using Content.Shared.Popups;
using Content.Shared.StatusEffect;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server.Nyanotrasen.Abilities.Felinid;

public sealed partial class FelinidSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private SharedChargesSystem _charges = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SatiationSystem _satiation = default!;
    [Dependency] private SharedSolutionContainerSystem _solutions = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private VomitSystem _vomit = default!;

    private static readonly ProtoId<SatiationTypePrototype> Hunger = "Hunger";
    private static readonly SatiationValue Overfed = "Okay";
    private const string MaskSlot = "mask";
    private const float VomitChance = 0.2f;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CoughingUpHairballComponent, FelinidAbilitiesComponent>();
        while (query.MoveNext(out var uid, out var coughing, out var felinid))
        {
            if (_timing.CurTime < coughing.SpawnTime)
                continue;

            SpawnHairball((uid, felinid));
            RemCompDeferred<CoughingUpHairballComponent>(uid);
        }
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<FelinidAbilitiesComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.HairballAction, ent.Comp.HairballActionId);
    }

    [SubscribeLocalEvent]
    private void OnEquipped(Entity<FelinidAbilitiesComponent> ent, ref DidEquipHandEvent args)
    {
        if (!HasComp<FelinidFoodComponent>(args.Equipped))
            return;

        ent.Comp.EatActionTarget = args.Equipped;
        _actions.AddAction(ent, ref ent.Comp.EatAction, ent.Comp.EatActionId);
    }

    [SubscribeLocalEvent]
    private void OnUnequipped(Entity<FelinidAbilitiesComponent> ent, ref DidUnequipHandEvent args)
    {
        if (args.Unequipped != ent.Comp.EatActionTarget)
            return;

        ent.Comp.EatActionTarget = null;
        _actions.RemoveAction(ent.Owner, ent.Comp.EatAction);
    }

    [SubscribeLocalEvent]
    private void OnHairball(Entity<FelinidAbilitiesComponent> ent, ref HairballActionEvent args)
    {
        if (IsMouthBlocked(ent))
            return;

        _popup.PopupEntity(Loc.GetString("hairball-cough", ("name", Identity.Entity(ent, EntityManager))), ent);
        _audio.PlayPvs(ent.Comp.HairballSound, ent);

        var coughing = EnsureComp<CoughingUpHairballComponent>(ent);
        coughing.SpawnTime = _timing.CurTime + coughing.CoughUpTime;
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnEatMouse(Entity<FelinidAbilitiesComponent> ent, ref EatMouseActionEvent args)
    {
        if (ent.Comp.EatActionTarget is not { } food || !TryComp<SatiationComponent>(ent, out var satiation))
            return;

        if (_satiation.IsValueInRange((ent, satiation), Hunger, above: Overfed))
        {
            _popup.PopupEntity(Loc.GetString("ingestion-other-cannot-ingest-any-more"), ent, ent, PopupType.SmallCaution);
            return;
        }

        if (IsMouthBlocked(ent))
            return;

        if (ent.Comp.HairballAction is { } hairball)
        {
            _charges.SetCharges(hairball, 1);
            _actions.SetEnabled(hairball, true);
        }

        QueueDel(food);
        ent.Comp.EatActionTarget = null;

        _audio.PlayPvs(ent.Comp.EatSound, ent);
        _satiation.ModifyValue((ent, satiation), Hunger, ent.Comp.MouseHunger);
        _actions.RemoveAction(ent.Owner, ent.Comp.EatAction);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnHairballHit(Entity<HairballComponent> ent, ref ThrowDoHitEvent args)
    {
        if (HasComp<FelinidAbilitiesComponent>(args.Target) || !HasComp<StatusEffectsComponent>(args.Target))
            return;

        if (_random.Prob(VomitChance))
            _vomit.Vomit(args.Target);
    }

    [SubscribeLocalEvent]
    private void OnHairballPickupAttempt(Entity<HairballComponent> ent, ref GettingPickedUpAttemptEvent args)
    {
        if (HasComp<FelinidAbilitiesComponent>(args.User) || !HasComp<StatusEffectsComponent>(args.User))
            return;

        if (!_random.Prob(VomitChance))
            return;

        _vomit.Vomit(args.User);
        args.Cancel();
    }

    private bool IsMouthBlocked(EntityUid uid)
    {
        if (!_inventory.TryGetSlotEntity(uid, MaskSlot, out var mask)
            || !TryComp<IngestionBlockerComponent>(mask, out var blocker)
            || !blocker.Enabled)
        {
            return false;
        }

        _popup.PopupEntity(Loc.GetString("hairball-mask", ("mask", mask)), uid, uid, PopupType.SmallCaution);
        return true;
    }

    private void SpawnHairball(Entity<FelinidAbilitiesComponent> ent)
    {
        var hairball = Spawn(ent.Comp.HairballPrototype, Transform(ent).Coordinates);

        if (!TryComp<BloodstreamComponent>(ent, out var bloodstream)
            || bloodstream.BloodSolution is not { } blood
            || !TryComp<HairballComponent>(hairball, out var hairballComp)
            || !_solutions.TryGetSolution(hairball, hairballComp.SolutionName, out var hairballSolution))
        {
            return;
        }

        _solutions.TryAddSolution(hairballSolution.Value, _solutions.SplitSolution(blood, ent.Comp.HairballBlood));
    }
}
