// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Nebula.Hud;

/// <summary>
/// Equipment that shows the security neural interface HUD to its wearer.
/// </summary>
/// <summary>
/// Eyewear that shows the HUD of the wearer's neural interface.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SkewedHudComponent : Component
{
    /// <summary>
    /// Interfaces the eyewear works with, any when empty.
    /// </summary>
    [DataField(readOnly: true)]
    public HashSet<ProtoId<NeuroProfilePrototype>> Profiles = [];
}
