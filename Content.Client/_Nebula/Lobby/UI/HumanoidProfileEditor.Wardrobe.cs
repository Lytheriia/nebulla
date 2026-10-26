// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Nebula.Lobby;
using Content.Shared._Nebula.CCVar;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client.Lobby.UI;

/// <summary>
/// Wardrobe tab and preview modes of the profile editor.
/// </summary>
public sealed partial class HumanoidProfileEditor
{
    private static readonly NebulaPreviewClothing[] PreviewClothingModes =
        [NebulaPreviewClothing.All, NebulaPreviewClothing.Underwear, NebulaPreviewClothing.Naked];

    private bool _previewWardrobe;
    private NebulaPreviewClothing _previewClothing;

    private void InitializeWardrobe()
    {
        TabContainer.SetTabTitle(WardrobeTab, Loc.GetString("humanoid-profile-editor-wardrobe-tab"));
        Wardrobe.Toggled += OnWardrobeToggled;
        Wardrobe.TierUpdated += RefreshWardrobe;

        _previewWardrobe = _cfgManager.GetCVar(NCCVars.PreviewWardrobe);
        _previewClothing = (NebulaPreviewClothing) Math.Clamp(_cfgManager.GetCVar(NCCVars.PreviewClothing),
            (int) NebulaPreviewClothing.All, (int) NebulaPreviewClothing.Naked);

        PreviewSourceButton.AddItem(Loc.GetString("nebula-wardrobe-preview-job"), 0);
        PreviewSourceButton.AddItem(Loc.GetString("nebula-wardrobe-preview-wardrobe"), 1);
        PreviewSourceButton.SelectId(_previewWardrobe ? 1 : 0);
        PreviewSourceButton.OnItemSelected += args =>
        {
            PreviewSourceButton.SelectId(args.Id);
            _previewWardrobe = args.Id == 1;
            _cfgManager.SetCVar(NCCVars.PreviewWardrobe, _previewWardrobe);
            _cfgManager.SaveToFile();
            ReloadPreview();
        };

        foreach (var clothing in PreviewClothingModes)
        {
            PreviewClothingButton.AddItem(Loc.GetString($"nebula-wardrobe-preview-{clothing.ToString().ToLowerInvariant()}"), (int) clothing);
        }

        PreviewClothingButton.SelectId((int) _previewClothing);
        PreviewClothingButton.OnItemSelected += args =>
        {
            PreviewClothingButton.SelectId(args.Id);
            _previewClothing = (NebulaPreviewClothing) args.Id;
            _cfgManager.SetCVar(NCCVars.PreviewClothing, args.Id);
            _cfgManager.SaveToFile();
            ReloadPreview();
        };
    }

    public void SelectWardrobeTab()
    {
        TabContainer.CurrentTab = WardrobeTab.GetPositionInParent();
    }

    private void RefreshWardrobe()
    {
        if (Profile != null)
            Wardrobe.Refresh(Profile);
    }

    private void OnWardrobeToggled(EntProtoId item, bool pressed)
    {
        if (Profile == null)
            return;

        Profile = Profile.WithWardrobeItem(item, pressed, _prototypeManager);
        SetDirty();
        RefreshWardrobe();

        if (_previewWardrobe)
            ReloadPreview();
    }
}
