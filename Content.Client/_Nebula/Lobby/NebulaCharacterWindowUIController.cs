// SPDX-License-Identifier: AGPL-3.0-or-later

// Ported from https://github.com/Goob-Station/Goob-Station/blob/master/Content.Client/UserInterface/Systems/EscapeMenu/EscapeUIController.cs

using Content.Client._Nebula.Lobby.UI;
using Content.Client.Gameplay;
using Content.Client.Guidebook;
using Content.Client.Lobby;
using Content.Client.Lobby.UI;
using Content.Client.Players.PlayTimeTracking;
using Content.Shared.Humanoid.Markings;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Client._Nebula.Lobby;

/// <summary>
/// Opens the character setup in a window during the round, from the escape menu.
/// </summary>
public sealed partial class NebulaCharacterWindowUIController : UIController, IOnStateExited<GameplayState>
{
    [Dependency] private IClientPreferencesManager _preferences = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IFileDialogManager _dialogManager = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IResourceCache _resourceCache = default!;
    [Dependency] private JobRequirementsManager _requirements = default!;
    [Dependency] private MarkingManager _markings = default!;
    [UISystemDependency] private readonly GuidebookSystem? _guide = default!;

    private NebulaCharacterSetupWindow? _window;
    private NebulaCharacterSetup? _setup;
    private HumanoidProfileEditor? _editor;
    private NebulaSavePanel? _savePanel;

    public void OnStateExited(GameplayState state)
    {
        CloseWindow();
    }

    public void OpenWindow()
    {
        if (_window is { IsOpen: true })
        {
            _window.MoveToFront();
            return;
        }

        var editor = new HumanoidProfileEditor(
            _preferences,
            _cfg,
            EntityManager,
            _dialogManager,
            LogManager,
            _player,
            _proto,
            _resourceCache,
            _requirements,
            _markings);

        if (_guide != null)
            editor.OnOpenGuidebook += _guide.OpenHelp;

        editor.Save += SaveProfile;

        var setup = new NebulaCharacterSetup(editor);

        setup.CloseButton.OnPressed += _ =>
        {
            if (editor.Profile != null && editor.IsDirty)
            {
                OpenSavePanel();
                return;
            }

            CloseWindow();
        };

        setup.SelectCharacter += slot =>
        {
            _preferences.SelectCharacter(slot);
            ReloadSetup();
        };

        setup.DeleteCharacter += slot =>
        {
            _preferences.DeleteCharacter(slot);

            if (editor.CharacterSlot == slot)
                ReloadSetup();
            else
                setup.ReloadCharacterPickers();
        };

        _editor = editor;
        _setup = setup;
        _window = new NebulaCharacterSetupWindow(setup);
        _window.OnClose += CloseWindow;

        if (_preferences.ServerDataLoaded)
            ReloadSetup();

        _window.OpenCentered();
    }

    private void CloseWindow()
    {
        var window = _window;
        _window = null;
        _setup = null;
        _editor = null;

        _savePanel?.Close();
        window?.Dispose();
    }

    private void SaveProfile()
    {
        if (_editor?.Profile is not { } profile
            || _editor.CharacterSlot is not { } slot)
            return;

        _preferences.UpdateCharacter(profile, slot);
        ReloadSetup();
    }

    private void ReloadSetup()
    {
        _setup?.ReloadCharacterPickers();
        _editor?.SetProfile(
            _preferences.Preferences?.SelectedCharacter,
            _preferences.Preferences?.SelectedCharacterIndex);
    }

    private void OpenSavePanel()
    {
        if (_savePanel is { IsOpen: true })
            return;

        var panel = new NebulaSavePanel();
        _savePanel = panel;

        panel.SaveButton.OnPressed += _ =>
        {
            SaveProfile();
            panel.Close();
            CloseWindow();
        };

        panel.NoSaveButton.OnPressed += _ =>
        {
            panel.Close();
            CloseWindow();
        };

        panel.OnClose += () => _savePanel = null;
        panel.OpenCentered();
    }
}
