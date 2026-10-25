// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Client._Nebula.Lobby;
using Content.Shared.Clothing.Components;
using Content.Shared.Humanoid;
using Content.Shared.Inventory;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Shared.Map;

namespace Content.Client.Lobby.UI.ProfileEditorControls;

public sealed partial class ProfilePreviewSpriteView
{
    /// <summary>
    /// Reloads the dummy dressed either in the job clothing or in the wardrobe items, in underwear or naked.
    /// </summary>
    public void LoadPreview(HumanoidCharacterProfile profile, JobPrototype? jobOverride, bool wardrobe, NebulaPreviewClothing clothing)
    {
        if (clothing == NebulaPreviewClothing.Naked)
            profile = WithoutUndergarments(profile);

        var dressed = clothing == NebulaPreviewClothing.All;
        LoadPreview(profile, jobOverride, dressed && !wardrobe);

        if (dressed && wardrobe)
            GiveDummyWardrobe(profile);
    }

    private static HumanoidCharacterProfile WithoutUndergarments(HumanoidCharacterProfile profile)
    {
        var markings = profile.Appearance.Markings.ToDictionary(
            organ => organ.Key,
            organ => organ.Value
                .Where(layer => layer.Key is not (HumanoidVisualLayers.UndergarmentTop or HumanoidVisualLayers.UndergarmentBottom))
                .ToDictionary(layer => layer.Key, layer => layer.Value));

        return profile.WithCharacterAppearance(profile.Appearance.WithMarkings(markings));
    }

    /// <summary>
    /// Puts every selected wardrobe item into the first free slot that fits it.
    /// </summary>
    private void GiveDummyWardrobe(HumanoidCharacterProfile profile)
    {
        var inventory = EntMan.System<InventorySystem>();
        if (!inventory.TryGetSlots(PreviewDummy, out var slots))
            return;

        foreach (var id in profile.Wardrobe)
        {
            if (!_prototypeManager.HasIndex(id))
                continue;

            var item = EntMan.SpawnEntity(id, MapCoordinates.Nullspace);
            if (!EntMan.TryGetComponent<ClothingComponent>(item, out var clothing)
                || !slots.Any(slot => (slot.SlotFlags & clothing.Slots) != 0
                    && inventory.TryEquip(PreviewDummy, item, slot.Name, silent: true, force: true)))
            {
                EntMan.DeleteEntity(item);
            }
        }
    }
}
