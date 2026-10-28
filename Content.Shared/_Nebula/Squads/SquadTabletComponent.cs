// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared._Nebula.Squads;

/// <summary>
/// Head of security tablet: squad management and orders on top of the suit sensor map.
/// </summary>
[RegisterComponent]
public sealed partial class SquadTabletComponent : Component
{
    /// <summary>
    /// Station the tablet was last used on, so it keeps working off the station grid.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? Station;
}
