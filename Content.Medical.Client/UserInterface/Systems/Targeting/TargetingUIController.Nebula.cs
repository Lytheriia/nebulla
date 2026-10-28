// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Nebula.Death;
using Robust.Client.UserInterface.Controllers;

namespace Content.Medical.Client.UserInterface.Systems.Targeting;

/// <summary>
/// Fades the targeting doll out with the rest of the interface while the player sees their own death.
/// </summary>
public sealed partial class TargetingUIController : IOnSystemChanged<NebulaDeathSystem>
{
    public void OnSystemLoaded(NebulaDeathSystem system)
    {
        system.InterfaceFaded += OnInterfaceFaded;
    }

    public void OnSystemUnloaded(NebulaDeathSystem system)
    {
        system.InterfaceFaded -= OnInterfaceFaded;
    }

    private void OnInterfaceFaded(float alpha)
    {
        if (TargetingControl is not { } control)
            return;

        // Only a body with targeting has the doll, a ghost must not get it back.
        control.Modulate = Color.White.WithAlpha(alpha);
        control.Visible = alpha > 0f && _targetingComponent != null;
    }
}
