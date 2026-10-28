// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client._Nebula.Hud;

/// <summary>
/// Points at the squad's orders with the distance: a pulsing diamond over the spot, or held on an ellipse around
/// the player with a chevron toward it, so it never hides under the game interface at the screen edges.
/// </summary>
public sealed class SquadMarkerOverlay : Overlay
{
    private const float IconSize = 18f;
    private const float DiamondSize = 30f;
    private const float WaypointLift = 34f;
    private const float EllipseShare = 0.36f;
    private const float EllipseAspect = 1.35f;
    private const float PulsePeriod = 1.6f;
    private const float PulseGrowth = 0.7f;
    private const float BackingAlpha = 0.7f;

    private static readonly Color Shadow = Color.Black.WithAlpha(0.8f);

    private readonly SkewedHudSystem _hud;
    private readonly SharedTransformSystem _transform;
    private readonly IEyeManager _eye;
    private readonly ISharedPlayerManager _player;
    private readonly IGameTiming _timing;
    private readonly Texture _chevron;
    private readonly Font _font;

    /// <summary>
    /// Distance captions by whole meters, so they are not formatted every frame.
    /// </summary>
    private readonly Dictionary<int, string> _distances = [];
    private readonly List<Vector2> _vertices = [];

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public SquadMarkerOverlay(SkewedHudSystem hud,
        SharedTransformSystem transform,
        IEyeManager eye,
        ISharedPlayerManager player,
        IGameTiming timing,
        IResourceCache cache)
    {
        _hud = hud;
        _transform = transform;
        _eye = eye;
        _player = player;
        _timing = timing;
        _chevron = cache.GetTexture("/Textures/_Nebula/Interface/Lobby/expand_more.svg.192dpi.png");
        _font = cache.GetFont("/Fonts/RobotoMono/RobotoMono-Bold.ttf", 11);
    }

    /// <summary>
    /// Without a link to the servers the markers go away, whatever module set them.
    /// </summary>
    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (!_hud.Active
            || _hud.FlatlineAt != null
            || _hud.SignalLostAt != null
            || _player.LocalEntity == null
            || args.ViewportControl != _eye.MainViewport)
            return false;

        foreach (var markers in _hud.Markers.Values)
        {
            if (markers.Count > 0)
                return true;
        }

        return false;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_player.LocalEntity is not { } local)
            return;

        var handle = args.ScreenHandle;
        var scale = (args.ViewportControl as Control)?.UIScale ?? 1f;
        var bounds = (UIBox2) args.ViewportBounds;
        var self = _transform.GetMapCoordinates(local);
        var selfScreen = _eye.MapToScreen(self).Position;
        var pulse = (float) (_timing.RealTime.TotalSeconds % PulsePeriod) / PulsePeriod;

        foreach (var markers in _hud.Markers.Values)
        {
            foreach (var marker in markers)
            {
                var map = _transform.ToMapCoordinates(marker.Coordinates);
                if (map.MapId != self.MapId)
                    continue;

                var distance = DistanceText((int) (map.Position - self.Position).Length());
                var screen = _eye.MapToScreen(map).Position;
                DrawWaypoint(handle, bounds, scale, selfScreen, screen, marker, distance, pulse);
            }
        }

        handle.SetTransform(Matrix3x2.Identity);
    }

    private string DistanceText(int meters)
    {
        if (_distances.TryGetValue(meters, out var text))
            return text;

        text = Loc.GetString("skewed-hud-marker-distance", ("meters", meters));
        _distances[meters] = text;
        return text;
    }

    private void DrawCaption(DrawingHandleScreen handle, Vector2 center, string text, float scale, Color color)
    {
        var size = handle.GetDimensions(_font, text, scale);
        var position = center - size / 2f;
        handle.DrawString(_font, position + new Vector2(scale), text, scale, Shadow);
        handle.DrawString(_font, position, text, scale, color);
    }

    #region Waypoint

    /// <summary>
    /// The ellipse is centered on the player and kept well inside the viewport, clear of the chat and the hotbar.
    /// </summary>
    private void DrawWaypoint(DrawingHandleScreen handle,
        UIBox2 bounds,
        float scale,
        Vector2 selfScreen,
        Vector2 screen,
        SquadMarker marker,
        string distance,
        float pulse)
    {
        var center = Vector2.Clamp(selfScreen, bounds.TopLeft, bounds.BottomRight);
        var radiusY = bounds.Height * EllipseShare;
        var radii = new Vector2(MathF.Min(bounds.Width * EllipseShare, radiusY * EllipseAspect), radiusY);
        var delta = screen - center;
        var reach = (delta / radii).LengthSquared();
        var half = DiamondSize * scale / 2f;

        if (reach <= 1f)
        {
            var lifted = screen - new Vector2(0f, WaypointLift * scale);
            handle.DrawLine(screen, lifted + new Vector2(0f, half), marker.Color.WithAlpha(0.6f));
            DrawDiamondShape(handle, screen, 3f * scale, marker.Color, true);
            DrawDiamond(handle, lifted, scale, marker, pulse);
            DrawCaption(handle, lifted - new Vector2(0f, half + 9f * scale), distance, scale, marker.Color);
            return;
        }

        var edge = center + delta / MathF.Sqrt(reach);
        var outward = Vector2.Normalize(delta);
        var position = edge - outward * (half + 6f * scale);
        DrawChevron(handle, edge + outward * 4f * scale, outward, scale, marker.Color);
        DrawDiamond(handle, position, scale, marker, pulse);
        DrawCaption(handle, position + new Vector2(0f, half + 9f * scale), distance, scale, marker.Color);
    }

    private void DrawDiamond(DrawingHandleScreen handle, Vector2 center, float scale, SquadMarker marker, float pulse)
    {
        var half = DiamondSize * scale / 2f;
        DrawDiamondShape(handle, center, half * (1f + PulseGrowth * pulse), marker.Color.WithAlpha(0.7f * (1f - pulse)), false);
        DrawDiamondShape(handle, center, half, Color.Black.WithAlpha(BackingAlpha), true);
        DrawDiamondShape(handle, center, half, marker.Color, false);
        DrawDiamondShape(handle, center, half - scale, marker.Color, false);

        var icon = IconSize * scale / 2f;
        handle.DrawTextureRect(marker.Icon, new UIBox2(center - new Vector2(icon), center + new Vector2(icon)), marker.Color);
    }

    private void DrawDiamondShape(DrawingHandleScreen handle, Vector2 center, float half, Color color, bool filled)
    {
        _vertices.Clear();
        _vertices.Add(center + new Vector2(0f, -half));
        _vertices.Add(center + new Vector2(half, 0f));
        _vertices.Add(center + new Vector2(0f, half));
        _vertices.Add(center + new Vector2(-half, 0f));
        handle.DrawPrimitives(filled ? DrawPrimitiveTopology.TriangleFan : DrawPrimitiveTopology.LineLoop, _vertices, color);
    }

    private void DrawChevron(DrawingHandleScreen handle, Vector2 position, Vector2 direction, float scale, Color color)
    {
        var size = IconSize * scale;
        var angle = MathF.Atan2(direction.Y, direction.X) - MathF.PI / 2f;
        handle.SetTransform(Matrix3x2.CreateRotation(angle) * Matrix3x2.CreateTranslation(position));
        handle.DrawTextureRect(_chevron, new UIBox2(-size / 2f, -size / 2f, size / 2f, size / 2f), color);
        handle.SetTransform(Matrix3x2.Identity);
    }

    #endregion
}

/// <summary>
/// An order of the local player's squad, ready to draw.
/// </summary>
public readonly record struct SquadMarker(Texture Icon, Color Color, EntityCoordinates Coordinates);
