// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Client._Nebula.Squads.Cyber;

/// <summary>
/// Fixed colors of the security squad interfaces. Red frames and alarms, cyan data and interaction,
/// yellow warnings only. Kept apart from the lobby theme on purpose.
/// </summary>
public static class CyberPalette
{
    public static readonly Color Background = Color.FromHex("#0a0c10");
    public static readonly Color Panel = Color.FromHex("#0f1218");
    public static readonly Color PanelRaised = Color.FromHex("#141922");
    public static readonly Color PanelHover = Color.FromHex("#1b2230");
    public static readonly Color Line = Color.FromHex("#2a1218");
    public static readonly Color Input = Color.FromHex("#07090c");

    public static readonly Color Red = Color.FromHex("#ff003c");
    public static readonly Color RedDim = Color.FromHex("#8a0a26");
    public static readonly Color RedText = Color.FromHex("#ff4d73");
    public static readonly Color Cyan = Color.FromHex("#00f0ff");
    public static readonly Color CyanDim = Color.FromHex("#0b6f78");
    public static readonly Color Warning = Color.FromHex("#fcee0a");

    public static readonly Color Text = Color.FromHex("#e6edf3");
    public static readonly Color Muted = Color.FromHex("#6b7785");
}
