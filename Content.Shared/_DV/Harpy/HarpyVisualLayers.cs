// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Goob-Station/Goob-Station/blob/c5be8fd82d71fa28a4556d9bdc40b86a987ed361/Content.Shared/_DV/Harpy/Components/HarpyVisualSystem.cs

using Robust.Shared.Serialization;

namespace Content.Shared._DV.Harpy;

[Serializable, NetSerializable]
public enum HarpyVisualLayers : byte
{
    Singing,
}
