// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Popups;
using Content.Client.UserInterface.Screens;
using Content.Client.UserInterface.Systems.Actions.Widgets;
using Content.Client.UserInterface.Systems.Alerts.Widgets;
using Content.Client.UserInterface.Systems.Hotbar.Widgets;
using Content.Client.UserInterface.Systems.Inventory.Widgets;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Shared._Nebula.CCVar;
using Robust.Client.UserInterface;

namespace Content.Client._Nebula.Death;

public sealed partial class NebulaDeathSystem
{
    [Dependency] private IUserInterfaceManager _ui = default!;

    private const float InterfaceFadeTime = 1.5f;

    /// <summary>
    /// The chat and the top menu come back once the effect has played: dead players ghost and talk through them.
    /// </summary>
    private const float InterfaceReturnStart = FadeTime + 2f;
    private const float InterfaceReturnTime = 0.5f;

    /// <summary>
    /// How visible the hidden part of the interface is, 1 when nothing is hidden.
    /// Modules that attach their own widgets to the screen fade them along.
    /// </summary>
    public event Action<float>? InterfaceFaded;

    private readonly List<HiddenControl> _hidden = [];
    private readonly List<HiddenControl> _returning = [];
    private PopupOverlay? _popups;
    private bool _hideInterface;
    private bool _interfaceHidden;

    /// <summary>
    /// Modulate fades panels and text, but sprite views ignore it, so a faded out control is also made invisible.
    /// Only controls visible at death are touched: the rest belong to whoever shows them, like the ghost panel
    /// that appears after leaving the body.
    /// </summary>
    private sealed class HiddenControl(Control control)
    {
        public readonly Control Control = control;
        public readonly bool WasVisible = control.Visible;

        public void Set(float alpha)
        {
            if (!WasVisible)
                return;

            Control.Modulate = Color.White.WithAlpha(alpha);
            Control.Visible = alpha > 0f;
        }
    }

    private void OnHideInterfaceChanged(bool hide)
    {
        _hideInterface = hide;
        if (!hide)
            RestoreInterface();
    }

    /// <summary>
    /// Fades out the game interface and the popups, everything but the death effect and the HUD break-up.
    /// Widgets of other modules follow <see cref="InterfaceFaded"/>.
    /// </summary>
    private void HideInterface()
    {
        RestoreInterface();
        if (!_hideInterface
            || _ui.ActiveScreen is not InGameScreen screen)
            return;

        if (_overlay.TryGetOverlay(out _popups))
            _overlay.RemoveOverlay(_popups);

        _interfaceHidden = true;
        AddHidden(screen.GetWidget<ActionsBar>());
        AddHidden(screen.GetWidget<InventoryGui>());
        AddHidden(screen.GetWidget<HotbarGui>());
        AddHidden(screen.GetWidget<AlertsUI>());
        AddReturning(screen.ChatBox);
        AddReturning(screen.GetWidget<GameTopMenuBar>());
    }

    private void UpdateInterface(float elapsed)
    {
        var fade = 1f - Math.Clamp(elapsed / InterfaceFadeTime, 0f, 1f);
        var back = Math.Clamp((elapsed - InterfaceReturnStart) / InterfaceReturnTime, 0f, 1f);
        foreach (var hidden in _hidden)
        {
            hidden.Set(fade);
        }

        if (_interfaceHidden)
            InterfaceFaded?.Invoke(fade);

        foreach (var hidden in _returning)
        {
            hidden.Set(Math.Max(fade, back));
        }
    }

    private void RestoreInterface()
    {
        foreach (var hidden in _hidden)
        {
            hidden.Set(1f);
        }

        foreach (var hidden in _returning)
        {
            hidden.Set(1f);
        }

        _hidden.Clear();
        _returning.Clear();
        if (_interfaceHidden)
            InterfaceFaded?.Invoke(1f);

        _interfaceHidden = false;
        if (_popups != null && !_overlay.HasOverlay<PopupOverlay>())
            _overlay.AddOverlay(_popups);

        _popups = null;
    }

    private void AddHidden(Control? control)
    {
        if (control != null)
            _hidden.Add(new HiddenControl(control));
    }

    private void AddReturning(Control? control)
    {
        if (control != null)
            _returning.Add(new HiddenControl(control));
    }
}
