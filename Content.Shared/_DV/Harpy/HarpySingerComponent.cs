// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Goob-Station/Goob-Station/blob/c5be8fd82d71fa28a4556d9bdc40b86a987ed361/Content.Shared/_DV/Harpy/HarpySingerComponent.cs

using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._DV.Harpy;

/// <summary>
/// Lets a harpy sing with its voice through the instrument UI.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HarpySingerComponent : Component
{
    [DataField]
    public EntProtoId MidiActionId = "ActionHarpyPlayMidi";

    [DataField]
    public EntityUid? MidiAction;

    /// <summary>
    /// Damage taken at once that stops the singing, null never stops it.
    /// </summary>
    [DataField]
    public FixedPoint2? ShutUpDamageThreshold;
}
