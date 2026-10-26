// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Goob-Station/Goob-Station/blob/c5be8fd82d71fa28a4556d9bdc40b86a987ed361/Content.Shared/_DV/Prying/Systems/PlayerToolModifierSystem.cs

using Content.Shared._DV.Prying.Components;
using Content.Shared.Prying.Components;

namespace Content.Shared._DV.Prying.Systems;

public sealed partial class PlayerToolModifierSystem : EntitySystem
{
    [SubscribeLocalEvent]
    private void OnPry(Entity<PlayerToolModifierComponent> ent, ref GetPryTimeModifierEvent args)
    {
        args.PryTimeModifier *= ent.Comp.PryTimeMultiplier;
    }
}
