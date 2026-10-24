// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Goob-Station/Goob-Station/blob/c5be8fd82d71fa28a4556d9bdc40b86a987ed361/Content.Shared/_EinsteinEngines/Flight/FlightComponent.cs

using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._EinsteinEngines.Flight;

/// <summary>
/// Adds an action that allows the user to become temporarily weightless at the cost of stamina and hand usage.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class FlightComponent : Component
{
    [DataField]
    public EntProtoId? ToggleAction = "ActionToggleFlight";

    [DataField, AutoNetworkedField]
    public EntityUid? ToggleActionEntity;

    /// <summary>
    /// Is the user flying right now?
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool On;

    /// <summary>
    /// Stamina drain per second when flying.
    /// </summary>
    [DataField]
    public float StaminaDrainRate = 13.0f;

    /// <summary>
    /// Stamina recovery multiplier while flying, it compensates the reduced drain below.
    /// </summary>
    [DataField]
    public float StaminaRegenMultiplier = 0.25f;

    /// <summary>
    /// Stamina drain multiplier while a stamina modifier status effect is active.
    /// </summary>
    [DataField]
    public float StaminaDrainMultiplier = 0.85f;

    /// <summary>
    /// Key of the flight drain in the stamina drain dictionary.
    /// </summary>
    [DataField]
    public string StaminaDrainKey = "flight";

    /// <summary>
    /// Weightless acceleration and speed multiplier.
    /// </summary>
    [DataField]
    public float SpeedModifier = 1.8f;

    [DataField]
    public float FrictionModifier = 5f;

    /// <summary>
    /// Weightless friction multiplier when no input is applied.
    /// </summary>
    [DataField]
    public float FrictionNoInputModifier = 25f;

    [DataField]
    public SoundSpecifier FlapSound = new SoundCollectionSpecifier("WingFlaps");

    [DataField]
    public bool IsAnimated = true;

    /// <summary>
    /// Whether the animation is applied to the sprite layer from <see cref="Layer"/> only.
    /// </summary>
    [DataField]
    public bool IsLayerAnimated;

    /// <summary>
    /// RSI path of the sprite layer to animate.
    /// </summary>
    [DataField]
    public string? Layer;

    [DataField]
    public float ShaderSpeed = 6.0f;

    [DataField]
    public float ShaderMultiplier = 0.01f;

    [DataField]
    public float ShaderOffset = 0.25f;

    /// <summary>
    /// Shader prototype of the flight animation, "default" disables it.
    /// </summary>
    [DataField]
    public string AnimationKey = "default";

    [DataField]
    public TimeSpan FlapInterval = TimeSpan.FromSeconds(1);

    [DataField, AutoPausedField]
    public TimeSpan NextFlap;

    /// <summary>
    /// Fixtures whose collision mask flight changed, with the original masks.
    /// </summary>
    [ViewVariables]
    public List<(string Key, int OriginalMask)> ChangedFixtures = [];

    /// <summary>
    /// Whether stopping the flight abruptly deals <see cref="FailDamageSpecifier"/>.
    /// </summary>
    [DataField]
    public bool CanFail = true;

    [DataField]
    public DamageSpecifier FailDamageSpecifier = new()
    {
        DamageDict = new Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2>
        {
            { "Blunt", 3.5 },
        },
    };
}
