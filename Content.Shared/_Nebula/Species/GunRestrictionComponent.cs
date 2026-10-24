// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._Nebula.Species;

/// <summary>
/// Stops this entity from shooting guns that match <see cref="Whitelist"/>, like oni with two-handed guns.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class GunRestrictionComponent : Component
{
    [DataField(required: true)]
    public EntityWhitelist Whitelist = new();

    [DataField]
    public List<LocId> Messages = [];

    [DataField]
    public TimeSpan PopupCooldown = TimeSpan.FromSeconds(1);

    [ViewVariables]
    public TimeSpan NextPopup;
}
