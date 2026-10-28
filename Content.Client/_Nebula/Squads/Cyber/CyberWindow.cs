// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Timing;

namespace Content.Client._Nebula.Squads.Cyber;

/// <summary>
/// Window of the security squad interfaces, drawn by itself rather than by the theme: cut corners, an accent frame
/// and scanlines. Opening flickers in and decodes the title. As a <see cref="Hologram"/> it is see-through,
/// with an unsteady glow that tears into glitch slices when it flickers, like a projection.
/// </summary>
[Virtual]
public class CyberWindow : BaseWindow
{
    private const float HeaderHeight = 40f;
    private const float Cut = 20f;
    private const float FrameThickness = 2f;
    private const float DragMargin = 6f;
    private const float BootTime = 0.3f;
    private const float DecodeTime = 0.35f;
    private const float ScanlineStep = 3f;
    private const float ToastTime = 4f;
    private const int GlitchSlices = 3;
    private const float GlitchMinHeight = 2f;
    private const float GlitchMaxHeight = 6f;
    private const float GlitchShift = 3f;
    private const float GlitchVisible = 0.25f;
    private const float FlickerChance = 0.02f;

    public readonly BoxContainer Contents;

    public Color Accent = CyberPalette.Red;

    /// <summary>
    /// See-through projection instead of a solid window.
    /// </summary>
    public bool Hologram;

    private readonly Label _tag;
    private readonly Label _title;
    private readonly BoxContainer _toasts;
    private readonly List<(Control Toast, double Until)> _toastTimes = [];
    private readonly Random _random = new();
    private string _titleText = string.Empty;
    private double _time;
    private double _openedAt;
    private float _flicker;
    private readonly (float Y, float Height, float Left, float Right)[] _glitch = new (float, float, float, float)[GlitchSlices];

    public CyberWindow()
    {
        MouseFilter = MouseFilterMode.Stop;

        _tag = CyberDraw.Label(string.Empty, CyberPalette.Red, 10, true);
        _tag.VerticalAlignment = VAlignment.Center;
        _title = CyberDraw.Label(string.Empty, CyberPalette.Text, 14, true);
        _title.VerticalAlignment = VAlignment.Center;
        _title.HorizontalExpand = true;
        _title.ClipText = true;

        var close = new CyberButton("X") { Accent = CyberPalette.Red, VerticalAlignment = VAlignment.Center };
        close.Content.Margin = new Thickness(9, 3);
        close.OnPressed += _ => Close();

        var header = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            MinHeight = HeaderHeight,
            SeparationOverride = 12,
            Margin = new Thickness(16, 0, Cut + 12, 0),
            Children = { _tag, _title, close },
        };

        Contents = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            VerticalExpand = true,
            SeparationOverride = 10,
            Margin = new Thickness(14, 8, 14, 14),
        };

        var frame = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Children = { header, Contents },
        };

        _toasts = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            SeparationOverride = 6,
            MaxWidth = 360,
        };

        var root = new LayoutContainer();
        root.AddChild(frame);
        root.AddChild(_toasts);
        LayoutContainer.SetAnchorPreset(frame, LayoutContainer.LayoutPreset.Wide);
        LayoutContainer.SetAnchorAndMarginPreset(_toasts, LayoutContainer.LayoutPreset.TopRight, margin: 18);
        LayoutContainer.SetMarginTop(_toasts, HeaderHeight + 52);
        AddChild(root);
    }

    public string Title
    {
        get => _titleText;
        set => _titleText = value.ToUpperInvariant();
    }

    public string Tag
    {
        set => _tag.Text = value;
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();
        _tag.FontColorOverride = Accent;
    }

    /// <summary>
    /// Short alarm in the top right corner, gone after a few seconds.
    /// </summary>
    public void ShowToast(string head, string text)
    {
        var toast = new CyberPanel
        {
            Fill = Color.FromHex("#120006"),
            Border = CyberPalette.Red,
            Stripe = CyberPalette.Red,
            SeparationOverride = 2,
            MouseFilter = MouseFilterMode.Ignore,
        };
        var content = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, Margin = new Thickness(14, 8) };
        content.AddChild(CyberDraw.Label(head, CyberPalette.Red, 11, true));
        content.AddChild(CyberDraw.Label(text, CyberPalette.Text, 12));
        toast.AddChild(content);

        _toasts.AddChild(toast);
        _toastTimes.Add((toast, _time + ToastTime));
    }

    protected override void Opened()
    {
        base.Opened();
        _openedAt = _time;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        _time += args.DeltaSeconds;
        var boot = (float) (_time - _openedAt);
        var title = CyberDraw.Decode(_titleText, boot / DecodeTime, _random);
        if (_title.Text != title)
            _title.Text = title;

        var alpha = boot switch
        {
            < BootTime * 0.4f => 0.35f,
            < BootTime * 0.55f => 1f,
            < BootTime * 0.7f => 0.25f,
            _ => 1f,
        };

        if (Hologram)
        {
            if (_random.NextSingle() < FlickerChance)
            {
                _flicker = 0.4f;
                for (var i = 0; i < _glitch.Length; i++)
                {
                    _glitch[i] = (
                        _random.NextSingle(),
                        GlitchMinHeight + _random.NextSingle() * (GlitchMaxHeight - GlitchMinHeight),
                        _random.NextSingle() * 0.3f,
                        0.6f + _random.NextSingle() * 0.4f);
                }
            }

            _flicker = Math.Max(0f, _flicker - args.DeltaSeconds * 4f);
            alpha *= 0.94f - _flicker;
        }

        Modulate = Color.White.WithAlpha(alpha);

        for (var i = _toastTimes.Count - 1; i >= 0; i--)
        {
            if (_toastTimes[i].Until > _time)
                continue;

            _toastTimes[i].Toast.Orphan();
            _toastTimes.RemoveAt(i);
        }
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var scale = UIScale;
        var box = (UIBox2) PixelSizeBox;
        var cut = Cut * scale;
        var background = Hologram ? Color.FromHex("#02080b").WithAlpha(0.72f) : CyberPalette.Background;
        CyberDraw.FillCut(handle, box, cut, background);

        var scan = Hologram ? Accent.WithAlpha(0.06f) : Color.White.WithAlpha(0.025f);
        CyberDraw.Scanlines(handle, box, scan, ScanlineStep * scale);

        if (Hologram && _flicker > GlitchVisible)
        {
            foreach (var (y, height, left, right) in _glitch)
            {
                var top = box.Top + y * box.Height;
                var bottom = Math.Min(box.Bottom, top + height * scale);
                var start = box.Left + left * box.Width;
                var end = box.Left + right * box.Width;
                var shift = GlitchShift * scale;
                handle.DrawRect(new UIBox2(start, top, end, bottom), Accent.WithAlpha(0.18f));
                handle.DrawRect(new UIBox2(start + shift, top - scale, end + shift, top), Accent.WithAlpha(0.08f));
            }
        }

        var headerBottom = HeaderHeight * scale;
        handle.DrawRect(new UIBox2(box.Left, headerBottom, box.Right, headerBottom + scale), Accent.WithAlpha(0.3f));
        CyberDraw.OutlineCut(handle, box, cut, Hologram ? Accent.WithAlpha(0.55f) : Accent, FrameThickness * scale);
    }

    protected override DragMode GetDragModeFor(Vector2 relativeMousePos)
    {
        var mode = DragMode.None;
        if (Resizable)
        {
            if (relativeMousePos.Y < DragMargin)
                mode = DragMode.Top;
            else if (relativeMousePos.Y > Size.Y - DragMargin)
                mode = DragMode.Bottom;

            if (relativeMousePos.X < DragMargin)
                mode |= DragMode.Left;
            else if (relativeMousePos.X > Size.X - DragMargin)
                mode |= DragMode.Right;
        }

        if (mode == DragMode.None && relativeMousePos.Y < HeaderHeight)
            mode = DragMode.Move;

        return mode;
    }
}
