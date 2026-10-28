// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._Nebula.Squads;

/// <summary>
/// Order the head of security puts on the squad tablet map. Shown on the map and in the squad's HUD.
/// </summary>
[Prototype]
public sealed partial class SquadOrderPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    /// <summary>
    /// Monochrome icon, tinted with the squad color.
    /// </summary>
    [DataField(required: true)]
    public SpriteSpecifier Icon = default!;

    /// <summary>
    /// Position in the radial menu.
    /// </summary>
    [DataField]
    public int Order;
}
