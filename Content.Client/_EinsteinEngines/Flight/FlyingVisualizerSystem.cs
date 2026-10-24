// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Goob-Station/Goob-Station/blob/c5be8fd82d71fa28a4556d9bdc40b86a987ed361/Content.Client/_EinsteinEngines/Flight/FlyingVisualizerSystem.cs

using Content.Client._EinsteinEngines.Flight.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client._EinsteinEngines.Flight;

/// <summary>
/// Applies the flight shader to the animated sprite layer.
/// </summary>
public sealed partial class FlyingVisualizerSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototype = default!;

    [SubscribeLocalEvent]
    private void OnStartup(Entity<FlightVisualsComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.TargetLayer is not { } layer || !TryComp<SpriteComponent>(ent, out var sprite))
            return;

        var shader = _prototype.Index<ShaderPrototype>(ent.Comp.AnimationKey).InstanceUnique();
        shader.SetParameter("Speed", ent.Comp.Speed);
        shader.SetParameter("Offset", ent.Comp.Offset);
        shader.SetParameter("Multiplier", ent.Comp.Multiplier);
        ent.Comp.Shader = shader;

        sprite.LayerSetShader(layer, shader);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(Entity<FlightVisualsComponent> ent, ref ComponentShutdown args)
    {
        if (ent.Comp.TargetLayer is { } layer && TryComp<SpriteComponent>(ent, out var sprite))
            sprite.LayerSetShader(layer, (ShaderInstance?) null);
    }
}
