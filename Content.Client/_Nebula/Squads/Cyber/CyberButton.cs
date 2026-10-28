// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client._Nebula.Squads.Cyber;

public enum CyberButtonStyle : byte
{
    /// <summary>
    /// Outlined, the accent fills it from the left on hover.
    /// </summary>
    Outline,

    /// <summary>
    /// List row: raised plate, an accent stripe on the left on hover or when active.
    /// </summary>
    Row,

    /// <summary>
    /// Tab: only text, red when active.
    /// </summary>
    Tab,

    /// <summary>
    /// Color swatch, see <see cref="CyberButton.Swatch"/>.
    /// </summary>
    Swatch,
}

/// <summary>
/// Button of the squad interfaces. Reacts fast: the hover fill takes 80 ms, a press flashes.
/// </summary>
public sealed class CyberButton : BaseButton
{
    private const float HoverTime = 0.08f;
    private const float FlashTime = 0.15f;
    private const float Cut = 8f;
    private const float StripeWidth = 2f;
    private const float PingSize = 6f;
    private const float PingPeriod = 0.4f;

    public readonly BoxContainer Content;
    public readonly Label Label;

    public CyberButtonStyle Style;
    public Color Accent = CyberPalette.Cyan;
    public Color Swatch = CyberPalette.Text;

    /// <summary>
    /// Selected tab, chosen squad, picked swatch.
    /// </summary>
    public bool Active;

    /// <summary>
    /// Blinking warning dot, used on tabs that have news.
    /// </summary>
    public bool Ping;

    private float _hover;
    private float _flash;
    private double _time;

    public CyberButton(string text = "", CyberButtonStyle style = CyberButtonStyle.Outline)
    {
        Style = style;
        MouseFilter = MouseFilterMode.Stop;
        Content = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 8,
            Margin = style == CyberButtonStyle.Row ? new Thickness(10, 5) : new Thickness(14, 6),
            HorizontalAlignment = style == CyberButtonStyle.Row ? HAlignment.Stretch : HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
        };
        Label = CyberDraw.Label(style is CyberButtonStyle.Row ? text : text.ToUpperInvariant(),
            CyberPalette.Text,
            12,
            style != CyberButtonStyle.Row);
        Label.VerticalAlignment = VAlignment.Center;
        Content.AddChild(Label);
        AddChild(Content);

        OnPressed += _ => _flash = 1f;
    }

    public string? Text
    {
        get => Label.Text;
        set => Label.Text = Style == CyberButtonStyle.Row ? value : value?.ToUpperInvariant();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        _time += args.DeltaSeconds;
        var target = DrawMode == DrawModeEnum.Hover && !Disabled ? 1f : 0f;
        _hover = MoveTowards(_hover, target, args.DeltaSeconds / HoverTime);
        _flash = Math.Max(0f, _flash - args.DeltaSeconds / FlashTime);
        Label.FontColorOverride = TextColor();
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var box = (UIBox2) PixelSizeBox;
        var scale = UIScale;
        switch (Style)
        {
            case CyberButtonStyle.Outline:
                DrawOutline(handle, box, scale);
                break;
            case CyberButtonStyle.Row:
                DrawRow(handle, box, scale);
                break;
            case CyberButtonStyle.Tab:
                DrawTab(handle, box, scale);
                break;
            case CyberButtonStyle.Swatch:
                DrawSwatch(handle, box, scale);
                break;
        }

        if (_flash > 0f)
            handle.DrawRect(box, Color.White.WithAlpha(0.25f * _flash));
    }

    private void DrawOutline(DrawingHandleScreen handle, UIBox2 box, float scale)
    {
        var accent = Disabled ? Accent.WithAlpha(0.3f) : Accent;
        var fill = Active ? 1f : _hover;
        if (fill > 0f)
        {
            CyberDraw.FillCutTo(handle, box, Cut * scale, box.Left + box.Width * fill, accent);
        }

        CyberDraw.OutlineCut(handle, box, Cut * scale, accent, scale);
    }

    private void DrawRow(DrawingHandleScreen handle, UIBox2 box, float scale)
    {
        var background = Active
            ? Color.InterpolateBetween(CyberPalette.PanelRaised, Accent, 0.22f)
            : Color.InterpolateBetween(CyberPalette.PanelRaised, CyberPalette.PanelHover, _hover);
        handle.DrawRect(box, background);

        var stripe = Active ? 1f : _hover;
        if (stripe > 0f)
            handle.DrawRect(new UIBox2(box.Left, box.Top, box.Left + StripeWidth * scale, box.Bottom), Accent.WithAlpha(stripe));
    }

    private void DrawTab(DrawingHandleScreen handle, UIBox2 box, float scale)
    {
        if (_hover > 0f)
            handle.DrawRect(box, CyberPalette.Red.WithAlpha(0.1f * _hover));

        if (!Ping || _time % PingPeriod > PingPeriod / 2f)
            return;

        var size = PingSize * scale;
        var center = new Vector2(box.Right - size * 1.6f, box.Top + size * 1.6f);
        handle.DrawRect(new UIBox2(center.X - size / 2f, center.Y - size / 2f, center.X + size / 2f, center.Y + size / 2f),
            CyberPalette.Warning);
    }

    private void DrawSwatch(DrawingHandleScreen handle, UIBox2 box, float scale)
    {
        var color = Disabled ? Swatch.WithAlpha(0.18f) : Swatch;
        CyberDraw.FillCut(handle, box, Cut * scale * 0.6f, color);
        if (Active || _hover > 0f)
            CyberDraw.OutlineCut(handle, box, Cut * scale * 0.6f, CyberPalette.Text.WithAlpha(Active ? 1f : _hover), 2f * scale);
    }

    private Color TextColor()
    {
        if (Disabled)
            return CyberPalette.Muted;

        return Style switch
        {
            CyberButtonStyle.Outline => (Active ? 1f : _hover) > 0.5f ? CyberPalette.Background : Accent,
            CyberButtonStyle.Tab => Active ? CyberPalette.Red : Color.InterpolateBetween(CyberPalette.Muted, CyberPalette.Text, _hover),
            _ => CyberPalette.Text,
        };
    }

    private static float MoveTowards(float value, float target, float step)
    {
        return value < target ? Math.Min(target, value + step) : Math.Max(target, value - step);
    }
}
