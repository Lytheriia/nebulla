// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Goob-Station/Goob-Station/blob/c5be8fd82d71fa28a4556d9bdc40b86a987ed361/Content.Client/_EinsteinEngines/Flight/FlightSystem.cs

using Content.Client._EinsteinEngines.Flight.Components;
using Content.Shared._EinsteinEngines.Flight;
using Content.Shared._EinsteinEngines.Flight.Events;
using Robust.Client.GameObjects;

namespace Content.Client._EinsteinEngines.Flight;

public sealed partial class FlightSystem : SharedFlightSystem
{
    private const string NoAnimation = "default";

    [SubscribeNetworkEvent]
    private void OnToggleFlightVisuals(ToggleFlightVisualsEvent args)
    {
        if (TryGetEntity(args.Uid, out var uid) && TryComp<FlightComponent>(uid, out var flight))
            UpdateVisuals((uid.Value, flight), args.IsFlying, args.IsAnimated);
    }

    [SubscribeLocalEvent]
    private void OnFlight(Entity<FlightComponent> ent, ref FlightEvent args)
    {
        UpdateVisuals(ent, args.IsFlying, ent.Comp.IsAnimated);
    }

    private void UpdateVisuals(Entity<FlightComponent> ent, bool isFlying, bool isAnimated)
    {
        if (!isFlying)
        {
            RemComp<FlightVisualsComponent>(ent);
            return;
        }

        if (!isAnimated
            || ent.Comp.AnimationKey == NoAnimation
            || HasComp<FlightVisualsComponent>(ent)
            || !TryComp<SpriteComponent>(ent, out var sprite))
        {
            return;
        }

        if (!ent.Comp.IsLayerAnimated || ent.Comp.Layer is not { } layerPath || GetLayer(sprite, layerPath) is not { } layer)
            return;

        var visuals = new FlightVisualsComponent
        {
            AnimationKey = ent.Comp.AnimationKey,
            Multiplier = ent.Comp.ShaderMultiplier,
            Offset = ent.Comp.ShaderOffset,
            Speed = ent.Comp.ShaderSpeed,
            TargetLayer = layer,
        };
        AddComp(ent, visuals);
    }

    private static int? GetLayer(SpriteComponent sprite, string rsiPath)
    {
        var index = 0;
        foreach (var layer in sprite.AllLayers)
        {
            if (layer.Rsi?.Path.ToString() == rsiPath)
                return index;

            index++;
        }

        return null;
    }
}
