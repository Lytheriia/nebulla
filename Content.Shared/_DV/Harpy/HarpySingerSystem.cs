// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Goob-Station/Goob-Station/blob/c5be8fd82d71fa28a4556d9bdc40b86a987ed361/Content.Shared/_DV/Harpy/HarpySingerSystem.cs

using Content.Shared.Actions;

namespace Content.Shared._DV.Harpy;

public sealed partial class HarpySingerSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<HarpySingerComponent> ent, ref MapInitEvent args)
    {
        _actions.AddAction(ent, ref ent.Comp.MidiAction, ent.Comp.MidiActionId);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<HarpySingerComponent> ent, ref ComponentShutdown args)
    {
        _actions.RemoveAction(ent.Owner, ent.Comp.MidiAction);
    }
}
