// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Nebula.Squads.Cyber;

/// <summary>
/// Block with a cut corner, an optional outline and an accent stripe down the left edge.
/// </summary>
public sealed class CyberPanel : BoxContainer
{
    private const float StripeWidth = 2f;

    public Color Fill = CyberPalette.Panel;
    public Color? Border;
    public Color Stripe = CyberPalette.RedDim;
    public float Cut = 10f;
    public float BorderThickness = 1f;

    public CyberPanel()
    {
        Orientation = LayoutOrientation.Vertical;
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var box = (UIBox2) PixelSizeBox;
        var cut = Cut * UIScale;
        CyberDraw.FillCut(handle, box, cut, Fill);
        if (Stripe.A > 0f)
            handle.DrawRect(new UIBox2(box.Left, box.Top, box.Left + StripeWidth * UIScale, box.Bottom - cut), Stripe);

        if (Border is { } border)
            CyberDraw.OutlineCut(handle, box, cut, border, BorderThickness * UIScale);
    }
}
