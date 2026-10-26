// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Goob-Station/Goob-Station/blob/c5be8fd82d71fa28a4556d9bdc40b86a987ed361/Content.Server/Nyanotrasen/Abilities/Felinid/HairballComponent.cs

namespace Content.Shared.Nyanotrasen.Abilities.Felinid;

[RegisterComponent]
public sealed partial class HairballComponent : Component
{
    [DataField]
    public string SolutionName = "hairball";
}
