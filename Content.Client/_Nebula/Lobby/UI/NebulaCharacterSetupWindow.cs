// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client._Nebula.Lobby.UI;

/// <summary>
/// Frameless window for <see cref="NebulaCharacterSetup"/> opened from the escape menu.
/// The setup draws its own title row and close button, so the window only adds dragging and resizing.
/// </summary>
public sealed class NebulaCharacterSetupWindow : BaseWindow
{
    private const float HeaderHeight = 46f;
    private const float DragMargin = 7f;

    public NebulaCharacterSetupWindow(NebulaCharacterSetup setup)
    {
        MouseFilter = MouseFilterMode.Stop;
        MinSize = new Vector2(1100, 600);
        SetSize = new Vector2(1280, 760);

        AddChild(setup);
    }

    protected override DragMode GetDragModeFor(Vector2 relativeMousePos)
    {
        var mode = DragMode.None;

        if (relativeMousePos.Y < DragMargin)
            mode = DragMode.Top;
        else if (relativeMousePos.Y > Size.Y - DragMargin)
            mode = DragMode.Bottom;

        if (relativeMousePos.X < DragMargin)
            mode |= DragMode.Left;
        else if (relativeMousePos.X > Size.X - DragMargin)
            mode |= DragMode.Right;

        if (mode == DragMode.None && relativeMousePos.Y < HeaderHeight)
            mode = DragMode.Move;

        return mode;
    }
}
