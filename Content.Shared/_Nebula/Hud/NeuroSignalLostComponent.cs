// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Shared._Nebula.Hud;

/// <summary>
/// The neural interface lost its link to the station servers: a radio jammer or a solar flare.
/// Orders, squad data and alerts stop until the link is back, only the personal tasks keep working.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NeuroSignalLostComponent : Component
{
    public override bool SendOnlyToOwner => true;
}
