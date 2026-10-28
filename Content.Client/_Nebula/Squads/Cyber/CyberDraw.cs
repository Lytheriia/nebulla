// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client._Nebula.Squads.Cyber;

/// <summary>
/// Drawing and text helpers of the squad interfaces: boxes with cut corners, outlines, scanlines and the mono font.
/// </summary>
/// <remarks>
/// Shapes are drawn with colored vertices on <see cref="Texture.White"/>: the Vector2 overload of DrawPrimitives
/// converts the color from sRGB and makes everything darker.
/// </remarks>
public static class CyberDraw
{
    public const string ScrambleChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789#%@$&";
    private const string RegularFont = "/Fonts/RobotoMono/RobotoMono-Regular.ttf";
    private const string BoldFont = "/Fonts/RobotoMono/RobotoMono-Bold.ttf";

    private static readonly DrawVertexUV2DColor[] Shape = new DrawVertexUV2DColor[6];
    private static readonly DrawVertexUV2DColor[] Segment = new DrawVertexUV2DColor[4];
    private static readonly DrawVertexUV2DColor[] Clipped = new DrawVertexUV2DColor[12];
    private static readonly Vector2[] Points = new Vector2[6];

    public static Font Font(int size, bool bold = false)
    {
        return IoCManager.Resolve<IResourceCache>().GetFont(bold ? BoldFont : RegularFont, size);
    }

    public static Label Label(string text, Color color, int size = 12, bool bold = false)
    {
        return new Label { Text = text, FontColorOverride = color, FontOverride = Font(size, bold) };
    }

    /// <summary>
    /// Text that wraps to the available width, for hints that must not run past the window.
    /// </summary>
    public static RichTextLabel Wrapped(string text, Color color)
    {
        var message = new FormattedMessage();
        message.PushColor(color);
        message.AddText(text);
        message.Pop();

        var label = new RichTextLabel { HorizontalExpand = true };
        label.SetMessage(message);
        return label;
    }

    /// <summary>
    /// Dark field with a cyan edge on the left instead of the theme's input style.
    /// </summary>
    public static T Input<T>(T edit) where T : LineEdit
    {
        edit.StyleBoxOverride = new StyleBoxFlat
        {
            BackgroundColor = CyberPalette.Input,
            BorderColor = CyberPalette.CyanDim,
            BorderThickness = new Thickness(2, 1, 1, 1),
            ContentMarginLeftOverride = 10,
            ContentMarginRightOverride = 10,
        };
        edit.MinHeight = 32;
        return edit;
    }

    /// <summary>
    /// Box with the top right and bottom left corners cut off.
    /// </summary>
    public static void FillCut(DrawingHandleScreen handle, UIBox2 box, float cut, Color color)
    {
        SetPoints(box, cut);
        for (var i = 0; i < Points.Length; i++)
        {
            Shape[i] = new DrawVertexUV2DColor(Points[i], color);
        }

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, Texture.White, Shape);
    }

    /// <summary>
    /// The cut box filled from the left edge up to <paramref name="right"/>, for fills that grow across it.
    /// </summary>
    public static void FillCutTo(DrawingHandleScreen handle, UIBox2 box, float cut, float right, Color color)
    {
        if (right <= box.Left)
            return;

        SetPoints(box, cut);
        var count = 0;
        for (var i = 0; i < Points.Length; i++)
        {
            var from = Points[i];
            var to = Points[(i + 1) % Points.Length];
            if (from.X <= right)
                Clipped[count++] = new DrawVertexUV2DColor(from, color);

            if ((from.X <= right) != (to.X <= right))
            {
                var t = (right - from.X) / (to.X - from.X);
                Clipped[count++] = new DrawVertexUV2DColor(Vector2.Lerp(from, to, t), color);
            }
        }

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, Texture.White, Clipped.AsSpan(0, count));
    }

    /// <summary>
    /// The line runs inside the box, otherwise the control clips its outer half and the edges come out uneven.
    /// </summary>
    public static void OutlineCut(DrawingHandleScreen handle, UIBox2 box, float cut, Color color, float thickness)
    {
        var inset = thickness / 2f;
        SetPoints(new UIBox2(box.Left + inset, box.Top + inset, box.Right - inset, box.Bottom - inset), Math.Max(0f, cut - inset));
        for (var i = 0; i < Points.Length; i++)
        {
            // Square caps close the gaps a thick line leaves at the corners.
            var from = Points[i];
            var to = Points[(i + 1) % Points.Length];
            if ((to - from).LengthSquared() < 0.0001f)
                continue;

            var cap = Vector2.Normalize(to - from) * (thickness / 2f);
            Line(handle, from - cap, to + cap, color, thickness);
        }
    }

    public static void Line(DrawingHandleScreen handle, Vector2 from, Vector2 to, Color color, float thickness)
    {
        var direction = to - from;
        if (direction.LengthSquared() < 0.0001f)
            return;

        var normal = new Vector2(-direction.Y, direction.X) / direction.Length() * (thickness / 2f);
        Segment[0] = new DrawVertexUV2DColor(from + normal, color);
        Segment[1] = new DrawVertexUV2DColor(from - normal, color);
        Segment[2] = new DrawVertexUV2DColor(to + normal, color);
        Segment[3] = new DrawVertexUV2DColor(to - normal, color);
        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleStrip, Texture.White, Segment);
    }

    /// <summary>
    /// Faint horizontal lines over the box, like an old monitor.
    /// </summary>
    public static void Scanlines(DrawingHandleScreen handle, UIBox2 box, Color color, float step)
    {
        for (var y = box.Top; y < box.Bottom; y += step)
        {
            handle.DrawRect(new UIBox2(box.Left, y, box.Right, y + 1f), color);
        }
    }

    /// <summary>
    /// Replaces the characters not yet revealed with random ones, revealing left to right as progress goes to 1.
    /// </summary>
    public static string Decode(string text, float progress, Random random)
    {
        if (progress >= 1f)
            return text;

        var revealed = (int) (text.Length * Math.Max(progress, 0f));
        var chars = text.ToCharArray();
        for (var i = revealed; i < chars.Length; i++)
        {
            if (!char.IsWhiteSpace(chars[i]))
                chars[i] = ScrambleChars[random.Next(ScrambleChars.Length)];
        }

        return new string(chars);
    }

    private static void SetPoints(UIBox2 box, float cut)
    {
        Points[0] = new Vector2(box.Left, box.Top);
        Points[1] = new Vector2(box.Right - cut, box.Top);
        Points[2] = new Vector2(box.Right, box.Top + cut);
        Points[3] = new Vector2(box.Right, box.Bottom);
        Points[4] = new Vector2(box.Left + cut, box.Bottom);
        Points[5] = new Vector2(box.Left, box.Bottom - cut);
    }
}
