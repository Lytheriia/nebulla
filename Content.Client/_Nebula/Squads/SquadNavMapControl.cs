// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Client.Pinpointer.UI;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.Map;

namespace Content.Client._Nebula.Squads;

/// <summary>
/// Suit sensor map of the squad tablet that also draws squad orders.
/// Right click on an order cancels it, right click anywhere else asks for a new order there.
/// </summary>
public sealed partial class SquadNavMapControl : NavMapControl
{
    private const float OrderIconSize = 18f;
    private const float OrderBackingAlpha = 0.7f;

    public readonly List<SquadMapOrder> Orders = [];

    public event Action<EntityCoordinates>? MapRightClicked;
    public event Action<int>? OrderRightClicked;

    public SquadNavMapControl()
    {
        WallColor = new Color(214, 92, 92);
        TileColor = new Color(72, 30, 30);
        BackgroundColor = Color.FromSrgb(TileColor.WithAlpha(BackgroundOpacity));

        // The base sets a fixed size, the tablet gives the map whatever room its panel has.
        SetWidth = float.NaN;
        SetHeight = float.NaN;
    }

    protected override void KeyBindDown(GUIBoundKeyEventArgs args)
    {
        base.KeyBindDown(args);

        if (args.Function != EngineKeyFunctions.UIRightClick
            || MapUid is not { } grid)
            return;

        args.Handle();
        var pixel = args.PointerLocation.Position - GlobalPixelPosition;
        var radius = OrderIconSize * UIScale / 2f;
        foreach (var order in Orders)
        {
            if ((GridToPixel(order.Position) - pixel).Length() > radius)
                continue;

            OrderRightClicked?.Invoke(order.Id);
            return;
        }

        MapRightClicked?.Invoke(new EntityCoordinates(grid, PixelToGrid(pixel)));
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var size = OrderIconSize * UIScale;
        var half = new Vector2(size / 2f);
        foreach (var order in Orders)
        {
            var center = GridToPixel(order.Position);
            handle.DrawCircle(center, size * 0.65f, Color.Black.WithAlpha(OrderBackingAlpha));
            handle.DrawTextureRect(order.Icon, new UIBox2(center - half, center + half), order.Color);
        }
    }

    private Vector2 PixelToGrid(Vector2 pixel)
    {
        var unscaled = (pixel - MidPointVector) / MinimapScale;
        return new Vector2(unscaled.X, -unscaled.Y) + GetOffset();
    }

    private Vector2 GridToPixel(Vector2 position)
    {
        var local = position - GetOffset();
        return ScalePosition(new Vector2(local.X, -local.Y));
    }
}

/// <summary>
/// <see cref="Position"/> is local to the shown grid.
/// </summary>
public readonly record struct SquadMapOrder(int Id, Vector2 Position, Texture Icon, Color Color);
