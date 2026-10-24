// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Nebula.Lobby.UI;

namespace Content.Client.Lobby.UI.Roles;

public sealed partial class RequirementsSelector
{
    public NebulaAltTitlePicker? AltTitlePicker { get; private set; }

    /// <summary>
    /// Shows <paramref name="picker"/> in place of the title label, with the same width, margin and tooltip.
    /// </summary>
    public void SetAltTitlePicker(NebulaAltTitlePicker picker)
    {
        picker.SetWidth = TitleLabel.SetWidth;
        picker.Margin = TitleLabel.Margin;
        picker.ToolTip = TitleLabel.ToolTip;

        AltTitlePicker = picker;
        TitleLabel.Visible = false;
        AddChild(picker);
        picker.SetPositionInParent(TitleLabel.GetPositionInParent() + 1);
    }
}
