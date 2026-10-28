// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client._Nebula.Squads.Cyber;

/// <summary>
/// Row of tabs with an underline that slides to the selected one.
/// </summary>
public sealed class CyberTabBar : BoxContainer
{
    private const float UnderlineHeight = 3f;
    private const float GlowHeight = 7f;
    private const float SlideSpeed = 28f;

    public event Action<int>? TabSelected;

    private readonly List<CyberButton> _tabs = [];
    private int _selected = -1;
    private float _x;
    private float _width;

    public CyberTabBar()
    {
        Orientation = LayoutOrientation.Horizontal;
        SeparationOverride = 2;
    }

    public int AddTab(string text)
    {
        var index = _tabs.Count;
        var tab = new CyberButton(text, CyberButtonStyle.Tab);
        tab.OnPressed += _ => Select(index);
        AddChild(tab);
        _tabs.Add(tab);
        return index;
    }

    public void Select(int index)
    {
        _selected = index;
        for (var i = 0; i < _tabs.Count; i++)
        {
            _tabs[i].Active = i == index;
        }

        _tabs[index].Ping = false;
        TabSelected?.Invoke(index);
    }

    public void SetPing(int index, bool ping)
    {
        _tabs[index].Ping = ping && index != _selected;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_selected < 0)
            return;

        var tab = _tabs[_selected];
        var step = Math.Min(1f, args.DeltaSeconds * SlideSpeed);
        _x += (tab.PixelPosition.X - _x) * step;
        _width += (tab.PixelWidth - _width) * step;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var bottom = PixelHeight;
        handle.DrawRect(new UIBox2(0, bottom - UIScale, PixelWidth, bottom), CyberPalette.Line);
        if (_selected < 0)
            return;

        handle.DrawRect(new UIBox2(_x, bottom - GlowHeight * UIScale, _x + _width, bottom), CyberPalette.Red.WithAlpha(0.18f));
        handle.DrawRect(new UIBox2(_x, bottom - UnderlineHeight * UIScale, _x + _width, bottom), CyberPalette.Red);
    }
}
