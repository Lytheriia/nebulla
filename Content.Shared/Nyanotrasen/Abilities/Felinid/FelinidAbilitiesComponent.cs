// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Goob-Station/Goob-Station/blob/c5be8fd82d71fa28a4556d9bdc40b86a987ed361/Content.Server/Nyanotrasen/Abilities/Felinid/FelinidComponent.cs

using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Nyanotrasen.Abilities.Felinid;

/// <summary>
/// Hairball and mouse eating actions of felinids.
/// </summary>
[RegisterComponent]
public sealed partial class FelinidAbilitiesComponent : Component
{
    [DataField]
    public EntProtoId HairballPrototype = "Hairball";

    [DataField]
    public EntProtoId HairballActionId = "ActionHairball";

    [DataField]
    public EntityUid? HairballAction;

    [DataField]
    public EntProtoId EatActionId = "ActionEatMouse";

    [DataField]
    public EntityUid? EatAction;

    [DataField]
    public EntityUid? EatActionTarget;

    /// <summary>
    /// Blood moved from the felinid into its hairball.
    /// </summary>
    [DataField]
    public float HairballBlood = 20f;

    [DataField]
    public float MouseHunger = 50f;

    [DataField]
    public SoundSpecifier HairballSound = new SoundPathSpecifier("/Audio/Nyanotrasen/Effects/Species/hairball.ogg", AudioParams.Default.WithVariation(0.15f));

    [DataField]
    public SoundSpecifier EatSound = new SoundPathSpecifier("/Audio/_Goobstation/Heretic/eatfood.ogg", AudioParams.Default.WithVariation(0.15f));
}
