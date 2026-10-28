// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Nebula.Hud;

/// <summary>
/// The owner has a neural interface of the profile. Glasses with <see cref="SkewedHudComponent"/> show its HUD,
/// the data on it comes from the module of the profile, like <see cref="SecurityNeuroComponent"/> for security.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class NeuroLinkComponent : Component
{
    public override bool SendOnlyToOwner => true;

    [DataField(required: true), AutoNetworkedField]
    public ProtoId<NeuroProfilePrototype> Profile;
}

/// <summary>
/// Raised on the owner once the link to the servers is back, so the modules send their frozen data again.
/// </summary>
[ByRefEvent]
public readonly record struct NeuroSignalRestoredEvent;
