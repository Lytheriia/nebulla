// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Goob-Station/Goob-Station/blob/c5be8fd82d71fa28a4556d9bdc40b86a987ed361/Resources/Prototypes/_DV/Entities/Mobs/Species/harpy.yml

using Content.Shared._DV.Harpy;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client._DV.Harpy;

/// <summary>
/// Shows music notes over a singing harpy, the layer is added on first use so the body layers stay untouched.
/// </summary>
public sealed partial class HarpySingingVisualizerSystem : VisualizerSystem<HarpySingerComponent>
{
    private const string SingingLayer = "singingLayer";

    private static readonly SpriteSpecifier SingingSprite =
        new SpriteSpecifier.Rsi(new ResPath("/Textures/_DV/Effects/harpysinger.rsi"), "singing_music_notes");

    protected override void OnAppearanceChange(EntityUid uid, HarpySingerComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite is not { } sprite
            || !AppearanceSystem.TryGetData<bool>(uid, HarpyVisualLayers.Singing, out var singing, args.Component))
        {
            return;
        }

        if (!SpriteSystem.LayerMapTryGet((uid, sprite), SingingLayer, out var layer, false))
        {
            if (!singing)
                return;

            layer = SpriteSystem.AddLayer((uid, sprite), SingingSprite);
            SpriteSystem.LayerMapSet((uid, sprite), SingingLayer, layer);
        }

        SpriteSystem.LayerSetVisible((uid, sprite), layer, singing);
    }
}
