// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared._Nebula.Hud;
using Content.Shared.Input;
using Robust.Shared.Input.Binding;

namespace Content.Client._Nebula.Hud;

public sealed partial class SecurityNeuroSystem
{
    private SecurityNeuroTaskWindow? _window;

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OpenNeuroTasks, InputCmdHandler.FromDelegate(_ => ToggleWindow()))
            .Register<SecurityNeuroSystem>();

    }

    public override void Shutdown()
    {
        base.Shutdown();

        CommandBinds.Unregister<SecurityNeuroSystem>();
        _window?.Close();
    }

    /// <summary>
    /// The window works only while the HUD is on, like the task window of the interface itself.
    /// </summary>
    private void ToggleWindow()
    {
        if (_window is { Disposed: false })
        {
            _window.Close();
            return;
        }

        if (!_hud.Active
            || _player.LocalEntity is not { } uid
            || !_neuroQuery.TryComp(uid, out var neuro))
            return;

        _window = new SecurityNeuroTaskWindow();
        _window.PinPressed += (key, pinned) => RaiseNetworkEvent(new SecurityNeuroTaskPinEvent(key, pinned));
        _window.RemovePressed += id => RaiseNetworkEvent(new SecurityNeuroTaskRemoveEvent(id));
        _window.AddPressed += title => RaiseNetworkEvent(new SecurityNeuroTaskAddEvent(title));
        _window.OnClose += () => _window = null;
        _window.UpdateTasks(neuro.Tasks.ToList());
        _window.OpenCentered();
    }

    private void UpdateWindow(SecurityNeuroComponent neuro)
    {
        _window?.UpdateTasks(neuro.Tasks.ToList());
    }

    private void CloseInactiveWindow()
    {
        if (_window != null && !_hud.Active)
            _window.Close();
    }
}
