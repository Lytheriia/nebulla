// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server._Nebula.Sponsor;
using Content.Server.Chat.Managers;
using Content.Shared._Nebula.Wardrobe;
using Content.Shared.GameTicking;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Humanoid;
using Content.Shared.Roles;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Server._Nebula.Wardrobe;

/// <summary>
/// Gives players the clothing they picked in the wardrobe, packed in a duffel bag in their hand.
/// </summary>
public sealed partial class NebulaWardrobeSystem : EntitySystem
{
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private IComponentFactory _factory = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private SponsorManagerSystem _sponsors = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedStorageSystem _storage = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private static readonly EntProtoId Bag = "ClothingBackpackDuffel";

    [SubscribeLocalEvent]
    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        if (args.Profile.Wardrobe.Count == 0
            || !HasComp<HandsComponent>(args.Mob)
            || !HasComp<HumanoidProfileComponent>(args.Mob))
            return;

        int? rank = _sponsors.GetCachedUserInfo(args.Player.UserId, out var status) ? status.Rank : null;
        ProtoId<JobPrototype>? job = args.JobId is { } jobId ? jobId : null;
        var coords = Transform(args.Mob).Coordinates;
        List<EntityUid> items = [];
        var skipped = false;

        foreach (var id in args.Profile.Wardrobe)
        {
            if (!WardrobeHelpers.TryFindItem(_proto, id, out _, out var set))
                continue;

            if (!set.IsAllowed(rank) || !set.IsAllowedFor(job, _proto))
            {
                skipped = true;
                continue;
            }

            var item = Spawn(id, coords);
            RemoveComponents(item, set);
            items.Add(item);
        }

        if (skipped)
            _chat.DispatchServerMessage(args.Player, Loc.GetString("nebula-wardrobe-items-skipped"));

        if (items.Count == 0)
            return;

        var bag = Spawn(Bag, coords);
        foreach (var item in items)
        {
            if (!_storage.Insert(bag, item, out _, playSound: false))
                _transform.DropNextTo(item, args.Mob);
        }

        if (!_hands.TryPickupAnyHand(args.Mob, bag, checkActionBlocker: false, animate: false))
            _transform.DropNextTo(bag, args.Mob);
    }

    private void RemoveComponents(EntityUid item, WardrobeItemSet set)
    {
        foreach (var name in set.RemoveComponents)
        {
            var type = _factory.GetRegistration(name).Type;
            if (HasComp(item, type))
                RemComp(item, type);
        }
    }
}
