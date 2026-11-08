// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Collections.Generic;
using Content.Client.IconSmoothing;
using Content.IntegrationTests.Fixtures;
using Content.Shared.Doors.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using Robust.Shared.Utility;

namespace Content.IntegrationTests.Tests._Nebula;

/// <summary>
/// Structure sprites switch states at runtime (smoothing, doors, visualizers), so a missing state only shows up
/// in game. Spawns every structure on the client and checks the states those systems will ask for.
/// </summary>
[TestFixture]
[TestOf(typeof(IconSmoothSystem))]
public sealed class StructureSpriteStatesTest : GameTest
{
    private static readonly string[] Roots =
    [
        "/Textures/Structures/",
        "/Textures/_Arcane/Structures/",
        "/Textures/_DV/CosmicCult/",
    ];

    /// <summary>
    /// Prototypes whose RSI never had these states: curtains and fence gates have no opening/closing frames,
    /// tesla parts have no "_open" state. Remove entries as the sprites get fixed.
    /// </summary>
    private static readonly HashSet<string> KnownViolations =
    [
        "CurtainsBlack", "CurtainsBlackOpen", "CurtainsBlue", "CurtainsBlueOpen", "CurtainsCyan", "CurtainsCyanOpen",
        "CurtainsGreen", "CurtainsGreenOpen", "CurtainsOrange", "CurtainsOrangeOpen", "CurtainsPink", "CurtainsPinkOpen",
        "CurtainsPurple", "CurtainsPurpleOpen", "CurtainsRed", "CurtainsRedOpen", "CurtainsSkyBlue",
        "CurtainsSkyBlueOpen", "CurtainsWhite", "CurtainsWhiteOpen", "HospitalCurtains", "HospitalCurtainsOpen",
        "FenceMetalGate", "FenceWoodHighGate", "FenceWoodSmallGate",
        "TeslaCoil", "TeslaGroundingRod",
    ];

    [Test]
    public async Task StructureSpriteStatesExist()
    {
        var factory = CEntMan.ComponentFactory;
        var spriteName = factory.CompName<SpriteComponent>();
        var sprites = Client.System<SpriteSystem>();
        var reflection = Client.ResolveDependency<IReflectionManager>();
        var cache = Client.ResolveDependency<IResourceCache>();
        List<string> errors = [];

        await Client.WaitAssertion(() =>
        {
            foreach (var proto in CProtoMan.EnumeratePrototypes<EntityPrototype>())
            {
                if (proto.Abstract
                    || KnownViolations.Contains(proto.ID)
                    || Pair.IsTestPrototype(proto)
                    || !proto.TryComp<SpriteComponent>(spriteName, out var protoSprite)
                    || protoSprite.BaseRSI is not { } baseRsi
                    || !InRoots(baseRsi.Path.ToString()))
                    continue;

                var uid = CEntMan.SpawnEntity(proto.ID, MapCoordinates.Nullspace);
                var sprite = CEntMan.GetComponent<SpriteComponent>(uid);
                CheckIconSmooth(proto.ID, uid, sprite, errors);
                CheckDoor(proto.ID, (uid, sprite), sprites, errors);
                CheckVisualizer(proto.ID, (uid, sprite), sprites, reflection, cache, errors);
                CheckIcon(proto.ID, uid, cache, errors);
                CEntMan.DeleteEntity(uid);
            }
        });

        Assert.That(errors, Is.Empty, "\n" + string.Join("\n", errors));
    }

    private void CheckIconSmooth(string id, EntityUid uid, SpriteComponent sprite, List<string> errors)
    {
        if (!CEntMan.TryGetComponent<IconSmoothComponent>(uid, out var smooth)
            || sprite.BaseRSI is not { } rsi)
            return;

        var count = smooth.Mode switch
        {
            IconSmoothingMode.Corners => 8,
            IconSmoothingMode.CardinalFlags => 16,
            IconSmoothingMode.Diagonal => 2,
            _ => 0,
        };
        for (var i = 0; i < count; i++)
        {
            RequireState(id, "IconSmooth", rsi, $"{smooth.StateBase}{i}", errors);
        }
    }

    private void CheckDoor(string id, Entity<SpriteComponent> ent, SpriteSystem sprites, List<string> errors)
    {
        if (!CEntMan.TryGetComponent<DoorComponent>(ent, out var door))
            return;

        List<(Enum Layer, string State)> states = [.. door.OpenSpriteStates, .. door.ClosedSpriteStates];
        states.Add((DoorVisualLayers.Base, door.OpeningSpriteState));
        states.Add((DoorVisualLayers.Base, door.ClosingSpriteState));
        if (CEntMan.TryGetComponent<AirlockComponent>(ent, out var airlock))
        {
            states.Add((DoorVisualLayers.BaseUnlit, airlock.ClosedSpriteState));
            states.Add((DoorVisualLayers.BaseUnlit, airlock.OpeningSpriteState));
            states.Add((DoorVisualLayers.BaseUnlit, airlock.ClosingSpriteState));
            states.Add((DoorVisualLayers.BaseUnlit, airlock.DenySpriteState));
        }

        foreach (var (layer, state) in states)
        {
            if (sprites.TryGetLayer(ent.AsNullable(), layer, out var spriteLayer, false)
                && spriteLayer.ActualRsi is { } rsi)
                RequireState(id, $"Door {layer}", rsi, state, errors);
        }
    }

    private void CheckVisualizer(
        string id,
        Entity<SpriteComponent> ent,
        SpriteSystem sprites,
        IReflectionManager reflection,
        IResourceCache cache,
        List<string> errors)
    {
        if (!CEntMan.TryGetComponent<GenericVisualizerComponent>(ent, out var visualizer))
            return;

        foreach (var layers in visualizer.Visuals.Values)
        {
            foreach (var (layerKey, values) in layers)
            {
                var found = reflection.TryParseEnumReference(layerKey, out var key, false)
                    ? sprites.TryGetLayer(ent.AsNullable(), key, out var layer, false)
                    : sprites.TryGetLayer(ent.AsNullable(), layerKey, out layer, false);

                foreach (var data in values.Values)
                {
                    if (data.State is not { } state)
                        continue;

                    var rsi = data.RsiPath is { } path
                        ? Rsi(cache, path)
                        : found ? layer.ActualRsi : null;
                    if (rsi != null)
                        RequireState(id, $"GenericVisualizer {layerKey}", rsi, state, errors);
                }
            }
        }
    }

    private void CheckIcon(string id, EntityUid uid, IResourceCache cache, List<string> errors)
    {
        if (!CEntMan.TryGetComponent<IconComponent>(uid, out var icon))
            return;

        if (Rsi(cache, icon.Icon.RsiPath.ToString()) is not { } rsi)
        {
            errors.Add($"{id}: Icon RSI {icon.Icon.RsiPath} not found");
            return;
        }

        RequireState(id, "Icon", rsi, icon.Icon.RsiState, errors);
    }

    private static RSI Rsi(IResourceCache cache, string path)
    {
        var full = path.StartsWith('/') ? new ResPath(path) : SpriteSpecifierSerializer.TextureRoot / path;
        return cache.TryGetResource<RSIResource>(full, out var resource) ? resource.RSI : null;
    }

    private static void RequireState(string id, string source, RSI rsi, string state, List<string> errors)
    {
        if (!rsi.TryGetState(state, out _))
            errors.Add($"{id}: {source} needs state '{state}' missing in {rsi.Path}");
    }

    private static bool InRoots(string path)
    {
        foreach (var root in Roots)
        {
            if (path.StartsWith(root))
                return true;
        }

        return false;
    }
}
