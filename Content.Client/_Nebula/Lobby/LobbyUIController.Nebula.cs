// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Client.Lobby;

public sealed partial class LobbyUIController
{
    /// <summary>
    /// Switches the profile editor to the wardrobe tab.
    /// </summary>
    public void SelectWardrobeTab()
    {
        var (_, editor) = EnsureGui();
        editor.SelectWardrobeTab();
    }
}
