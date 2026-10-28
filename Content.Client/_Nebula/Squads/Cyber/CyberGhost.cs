// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Client.UserInterface;

namespace Content.Client._Nebula.Squads.Cyber;

/// <summary>
/// Plate that follows the cursor while something is dragged in the squad interfaces.
/// </summary>
public static class CyberGhost
{
    public static Control Create(string label)
    {
        var ghost = new CyberPanel
        {
            Fill = CyberPalette.Background.WithAlpha(0.92f),
            Border = CyberPalette.Cyan,
            Stripe = CyberPalette.Cyan,
            Cut = 6f,
        };
        var text = CyberDraw.Label(label, CyberPalette.Cyan, 12, true);
        text.Margin = new Thickness(12, 4);
        ghost.AddChild(text);
        return ghost;
    }
}
