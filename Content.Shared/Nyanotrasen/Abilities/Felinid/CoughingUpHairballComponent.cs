// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Goob-Station/Goob-Station/blob/c5be8fd82d71fa28a4556d9bdc40b86a987ed361/Content.Server/Nyanotrasen/Abilities/Felinid/CoughingUpHairballComponent.cs

namespace Content.Shared.Nyanotrasen.Abilities.Felinid;

/// <summary>
/// Added while a felinid coughs, the hairball spawns at <see cref="SpawnTime"/>.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class CoughingUpHairballComponent : Component
{
    [DataField, AutoPausedField]
    public TimeSpan SpawnTime;

    /// <summary>
    /// Length of the cough sound.
    /// </summary>
    [DataField]
    public TimeSpan CoughUpTime = TimeSpan.FromSeconds(2.15);
}
