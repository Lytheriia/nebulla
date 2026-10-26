// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Client.UserInterface.Controls;
using Robust.Client.Graphics;

namespace Content.Client._Nebula.UserInterface.Controls;

/// <summary>
/// Sector of <see cref="NebulaRadialMenu"/>: an annulus piece with a gap to its neighbours and rounded corners.
/// </summary>
public sealed class NebulaRadialMenuButton : RadialMenuButton, IRadialMenuItemWithSector
{
    private const float Gap = 4f;
    private const float CornerRadius = 8f;
    private const int CornerPoints = 6;
    private const float ArcStep = MathF.Tau / 128f;

    private float _angleSectorFrom;
    private float _angleSectorTo;
    private float _outerRadius;
    private float _innerRadius;
    private float _angleOffset;
    private Vector2? _parentCenter;

    private DrawVertexUV2DColor[] _strip = [];

    public Color SectorColor { get; set; } = NebulaRadialMenu.SectorColor;

    public Color SectorHoverColor { get; set; } = NebulaRadialMenu.SectorHoverColor;

    float IRadialMenuItemWithSector.AngleSectorFrom { set => _angleSectorFrom = value; }

    float IRadialMenuItemWithSector.AngleSectorTo { set => _angleSectorTo = value; }

    float IRadialMenuItemWithSector.OuterRadius { set => _outerRadius = value; }

    float IRadialMenuItemWithSector.InnerRadius { set => _innerRadius = value; }

    float IRadialMenuItemWithSector.AngleOffset { set => _angleOffset = value; }

    Vector2 IRadialMenuItemWithSector.ParentCenter { set => _parentCenter = value; }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        if (_parentCenter is not { } parentCenter)
            return;

        var center = (parentCenter - Position) * UIScale;
        var color = (DrawMode == DrawModeEnum.Hover ? SectorHoverColor : SectorColor) * handle.Modulate;
        var from = _angleSectorFrom + _angleOffset;
        var inner = _innerRadius * UIScale;
        var outer = _outerRadius * UIScale;

        if (new Angle(_angleSectorFrom).EqualsApprox(new Angle(_angleSectorTo)))
            BuildRing(center, inner, outer, from, color);
        else
            BuildSector(center, inner, outer, from, _angleSectorTo + _angleOffset, color);

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleStrip, Texture.White, _strip);
    }

    protected override bool HasPoint(Vector2 point)
    {
        if (_parentCenter is not { } parentCenter)
            return base.HasPoint(point);

        var fromCenter = point + Position - parentCenter;
        var distSquared = fromCenter.LengthSquared();

        if (distSquared >= _outerRadius * _outerRadius
            || distSquared <= _innerRadius * _innerRadius)
            return false;

        var angle = MathF.Atan2(-fromCenter.Y, fromCenter.X) - _angleOffset;
        if (angle < 0)
            angle += MathF.Tau;

        return angle >= _angleSectorFrom && angle < _angleSectorTo;
    }

    #region Geometry

    private void BuildRing(Vector2 center, float inner, float outer, float from, Color color)
    {
        var points = (int) MathF.Ceiling(MathF.Tau / ArcStep) + 1;
        EnsureStrip(points * 2);

        var outerIndex = 0;
        var innerIndex = 1;
        Sweep(ref outerIndex, center, Vector2.Zero, outer, from, MathF.Tau, points, color);
        Sweep(ref innerIndex, center, Vector2.Zero, inner, from, MathF.Tau, points, color);
    }

    /// <summary>
    /// Outer and inner edges are built as chains of the same length (corner, arc, corner) and joined into a strip.
    /// </summary>
    private void BuildSector(Vector2 center, float inner, float outer, float from, float to, Color color)
    {
        var gap = Gap * UIScale / 2f;
        var corner = MathF.Min(CornerRadius * UIScale, (outer - inner) / 2f);
        var offset = gap + corner;

        var outerFrom = Fillet(from, 1f, outer - corner, offset, gap, outer);
        var outerTo = Fillet(to, -1f, outer - corner, offset, gap, outer);
        var innerFrom = Fillet(from, 1f, inner + corner, offset, gap, inner);
        var innerTo = Fillet(to, -1f, inner + corner, offset, gap, inner);

        var outerSpan = Wrap(Atan(outerTo.Circle) - Atan(outerFrom.Circle));
        var arcPoints = Math.Max(2, (int) MathF.Ceiling(outerSpan / ArcStep) + 1);
        EnsureStrip((CornerPoints * 2 + arcPoints) * 2);

        WriteChain(0, center, outerFrom, outerTo, outer, corner, arcPoints, color);
        WriteChain(1, center, innerFrom, innerTo, inner, corner, arcPoints, color);
    }

    /// <summary>
    /// Corner circle tangent to the edge circle of <paramref name="radius"/> and to the line
    /// <paramref name="gap"/> away from the sector boundary at <paramref name="angle"/>.
    /// </summary>
    private static (Vector2 Center, Vector2 Line, Vector2 Circle) Fillet(
        float angle,
        float side,
        float centerRadius,
        float offset,
        float gap,
        float radius)
    {
        var along = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
        var across = new Vector2(-along.Y, along.X) * side;
        var distance = MathF.Sqrt(MathF.Max(centerRadius * centerRadius - offset * offset, 0f));
        var filletCenter = along * distance + across * offset;

        return (filletCenter, along * distance + across * gap, filletCenter * (radius / centerRadius));
    }

    private void WriteChain(
        int index,
        Vector2 center,
        (Vector2 Center, Vector2 Line, Vector2 Circle) from,
        (Vector2 Center, Vector2 Line, Vector2 Circle) to,
        float radius,
        float corner,
        int arcPoints,
        Color color)
    {
        var fromStart = Atan(from.Line - from.Center);
        Sweep(ref index, center, from.Center, corner, fromStart, Wrap(Atan(from.Circle - from.Center) - fromStart), CornerPoints, color);

        // Narrow sectors can make the inner corners meet, the arc between them then collapses to a point.
        var arcStart = Atan(from.Circle);
        Sweep(ref index, center, Vector2.Zero, radius, arcStart, MathF.Max(Wrap(Atan(to.Circle) - arcStart), 0f), arcPoints, color);

        var toStart = Atan(to.Circle - to.Center);
        Sweep(ref index, center, to.Center, corner, toStart, Wrap(Atan(to.Line - to.Center) - toStart), CornerPoints, color);
    }

    /// <summary>
    /// Writes an arc into every second vertex of the strip, points are in natural coordinates with Y up.
    /// </summary>
    private void Sweep(
        ref int index,
        Vector2 center,
        Vector2 origin,
        float radius,
        float start,
        float delta,
        int points,
        Color color)
    {
        for (var i = 0; i < points; i++)
        {
            var angle = start + delta * i / (points - 1);
            var point = origin + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
            _strip[index] = new DrawVertexUV2DColor(center + new Vector2(point.X, -point.Y), color);
            index += 2;
        }
    }

    private void EnsureStrip(int length)
    {
        if (_strip.Length != length)
            _strip = new DrawVertexUV2DColor[length];
    }

    private static float Atan(Vector2 vector)
    {
        return MathF.Atan2(vector.Y, vector.X);
    }

    /// <summary>
    /// Wraps an angle difference into (-pi, pi].
    /// </summary>
    private static float Wrap(float angle)
    {
        while (angle > MathF.PI)
        {
            angle -= MathF.Tau;
        }

        while (angle <= -MathF.PI)
        {
            angle += MathF.Tau;
        }

        return angle;
    }

    #endregion
}
