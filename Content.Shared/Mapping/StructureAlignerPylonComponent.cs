// SPDX-License-Identifier: MIT
// Ported from https://github.com/space-wizards/space-station-14/blob/2561c7c9fee699d44cf6d314026f34258ec00bb6/Content.Shared/Mapping/StructureAlignerPylonComponent.cs

using Robust.Shared.GameStates;

namespace Content.Shared.Mapping;

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
