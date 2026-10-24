// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Goob-Station/Goob-Station/blob/c5be8fd82d71fa28a4556d9bdc40b86a987ed361/Content.Goobstation.Shared/Damage/Components/BonusStaminaDamageComponent.cs

using Robust.Shared.GameStates;

namespace Content.Goobstation.Shared.Damage.Components;

/// <summary>
/// Multiplies the stamina damage of melee weapons this entity attacks with, its own fists included.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class BonusStaminaDamageComponent : Component
{
    [DataField]
    public float Multiplier = 1f;
}
