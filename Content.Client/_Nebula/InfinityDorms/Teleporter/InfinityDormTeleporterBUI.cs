// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Nebula.InfinityDorms.PlayersList;
using Content.Shared._Nebula.InfinityDorm.Messages;
using Content.Shared._Nebula.InfinityDorms;
using Content.Shared._Nebula.InfinityDorms.Components;
using Content.Shared._Nebula.InfinityDorms.States;
using Robust.Client.UserInterface;

namespace Content.Client._Nebula.InfinityDorms.Teleporter;

public sealed partial class InfinityDormTeleporterBUI : BoundUserInterface
{
    private InfinityDormTeleporterWindow? _window;
    private InfinityDormPlayersWindow? _playersWindow;
    private List<List<InfinityDormVisitorData>> _players = [];


    public InfinityDormTeleporterBUI(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<InfinityDormTeleporterWindow>();

        EntMan.TryGetComponent<InfinityDormTeleporterComponent>(Owner, out var component);

        if (component != null)
        {
            _window.MaxValueForRoomId = component.MaxValueForRoomId;
            _window.MinValueForRoomId = component.MinValueForRoomId;
        }

        _window.ConfirmPressed += () =>
        {
            Teleport();
            _window.Close();
        };

        _window.PlayersListPressed += OpenPlayersWindow;
    }

    private void Teleport()
    {
        if (_window == null || _window.CurrentRoomId == null)
            return;

        SendMessage(new InfinityDormTeleportMessage(_window.CurrentRoomId, _window.SelectedNumber));
    }

    private void OpenPlayersWindow()
    {
        if (_playersWindow is { Disposed: false })
        {
            _playersWindow.MoveToFront();
            return;
        }

        _playersWindow = new InfinityDormPlayersWindow();
        _playersWindow.OnClose += () => _playersWindow = null;
        _playersWindow.Populate(_players);
        _playersWindow.OpenCentered();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not InfinityDormTeleportBUIState cast)
            return;

        _players = cast.Players;
        _playersWindow?.Populate(_players);
    }
}
