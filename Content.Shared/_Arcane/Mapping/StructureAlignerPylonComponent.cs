// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/dcb9510767a539afab95d2f0c854c9932e63a0d3/Content.Shared/Mapping/StructureAlignerPylonComponent.cs

using Robust.Shared.GameStates;

namespace Content.Shared._Arcane.Mapping;

/// <summary>
/// This entity is used by <see cref="SharedStructureAlignerSystem"/> as a reference point when determining the alignment of adjacent entities.
/// </summary>
[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class StructureAlignerPylonComponent : Component
{
    /// <summary>
    /// The types that will align to this entity.
    /// </summary>
    [DataField("pylons"), AutoNetworkedField]
    public StructureAlignerType AlignerPylonTypes = StructureAlignerType.Door;
}
