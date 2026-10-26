// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Goob-Station/Goob-Station/blob/c5be8fd82d71fa28a4556d9bdc40b86a987ed361/Content.Client/_EinsteinEngines/Flight/Components/FlightVisualsComponent.cs

using Robust.Client.Graphics;

namespace Content.Client._EinsteinEngines.Flight.Components;

/// <summary>
/// Added on the client while an entity flies, applies the flight shader to its sprite.
/// </summary>
[RegisterComponent]
public sealed partial class FlightVisualsComponent : Component
{
    [DataField]
    public float Speed;

    /// <summary>
    /// How far the animation moves in any direction.
    /// </summary>
    [DataField]
    public float Multiplier;

    /// <summary>
    /// How much the limbs rotate.
    /// </summary>
    [DataField]
    public float Offset;

    [DataField]
    public string AnimationKey = "default";

    public int? TargetLayer;

    [ViewVariables]
    public ShaderInstance? Shader;
}
