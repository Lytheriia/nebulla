// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Goob-Station/Goob-Station/blob/c5be8fd82d71fa28a4556d9bdc40b86a987ed361/Content.Shared/_DV/Prying/Components/PlayerToolModifierComponent.cs

using Robust.Shared.GameStates;

namespace Content.Shared._DV.Prying.Components;

/// <summary>
/// Alters the interaction speed of attached entity's tools.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PlayerToolModifierComponent : Component
{
    /// <summary>
    /// Multiplies the time this entity takes to pry doors open.
    /// </summary>
    [DataField]
    public float PryTimeMultiplier = 1.0f;
}
