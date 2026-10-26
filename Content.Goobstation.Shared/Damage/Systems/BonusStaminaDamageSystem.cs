// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Goob-Station/Goob-Station/blob/c5be8fd82d71fa28a4556d9bdc40b86a987ed361/Content.Goobstation.Shared/Damage/Systems/BonusStaminaDamageSystem.cs

using Content.Goobstation.Shared.Damage.Components;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Events;
using Robust.Shared.Containers;

namespace Content.Goobstation.Shared.Damage.Systems;

public sealed partial class BonusStaminaDamageSystem : EntitySystem
{
    [Dependency] private SharedContainerSystem _container = default!;

    [SubscribeLocalEvent]
    private void OnStaminaMeleeHit(Entity<StaminaDamageOnHitComponent> ent, ref StaminaMeleeHitEvent args)
    {
        var attacker = _container.TryGetContainingContainer((ent.Owner, null, null), out var container)
            ? container.Owner
            : ent.Owner;

        if (TryComp<BonusStaminaDamageComponent>(attacker, out var bonus))
            args.Multiplier *= bonus.Multiplier;
    }
}
