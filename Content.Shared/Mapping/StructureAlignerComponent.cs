// SPDX-License-Identifier: MIT
// Ported from https://github.com/space-wizards/space-station-14/blob/2561c7c9fee699d44cf6d314026f34258ec00bb6/Content.Shared/Mapping/StructureAlignerComponent.cs

using Robust.Shared.GameStates;

namespace Content.Shared.Mapping;

/// <summary>
/// This entity can be aligned by <see cref="SharedStructureAlignerSystem"/>.
/// When being Aligned, it will look for adjacent <see cref="StructureAlignerPylonComponent"/>s with the matching type.
/// </summary>
[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class StructureAlignerComponent : Component
{
    /// <summary>
    /// The type that this entity will align to.
    /// </summary>
    /// <remarks>
    /// When aligning, the types in this enum must all be present in <see cref="StructureAlignerPylonComponent.AlignerPylonTypes"/>.
    /// It is recommended that you only set a single type here.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public StructureAlignerType AlignerType = StructureAlignerType.Door;

    /// <summary>
    /// Align the entity when it gets anchored by a player.
    /// </summary>
    /// <remarks>
    /// Anchoring without the input of a player (such as when spawning an anchored object) does not count.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public bool AnchorAlign;
}
