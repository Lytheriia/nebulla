// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Client._Nebula.Stylesheets;
using Content.Client._Nebula.UserInterface.Controls;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;

namespace Content.Client._Nebula.UserInterface;

/// <summary>
/// Drags rows between drop zones of one control. The owner calls <see cref="FrameUpdate"/> from its own
/// and must not rebuild the rows while <see cref="Dragging"/>: key up comes only to the row the drag started on,
/// a rebuilt row would never get it. <see cref="Finished"/> tells when rebuilding is safe again.
/// </summary>
/// <param name="ghostFactory">Builds the plate that follows the cursor, the Nebula popup plate when null.</param>
public sealed class NebulaDragDrop<T>(Func<string, Control>? ghostFactory = null)
{
    private const float Threshold = 6f;
    private static readonly Vector2 GhostOffset = new(12, 4);

    private readonly IUserInterfaceManager _ui = IoCManager.Resolve<IUserInterfaceManager>();
    private readonly IInputManager _input = IoCManager.Resolve<IInputManager>();
    private readonly Dictionary<Control, Action<T>> _zones = [];

    private readonly Func<string, Control> _ghostFactory = ghostFactory ?? CreateGhost;
    private T? _payload;
    private string _label = string.Empty;
    private Vector2 _start;
    private bool _pending;
    private Control? _ghost;

    public event Action? Finished;

    public bool Dragging => _pending;

    /// <summary>
    /// Makes the row draggable, carrying <paramref name="payload"/> and showing <paramref name="label"/> by the cursor.
    /// </summary>
    public void Attach(Control row, T payload, string label)
    {
        row.OnKeyBindDown += args =>
        {
            if (args.Function != EngineKeyFunctions.UIClick)
                return;

            _payload = payload;
            _label = label;
            _start = _ui.MousePositionScaled.Position;
            _pending = true;
        };
        row.OnKeyBindUp += args =>
        {
            if (args.Function == EngineKeyFunctions.UIClick)
                Drop(args);
        };
    }

    /// <summary>
    /// The drop target is found by the control under the cursor, so a zone that ignores the mouse is made to pass it:
    /// otherwise its empty area resolves to whatever is behind it.
    /// </summary>
    public void AddZone(Control zone, Action<T> onDrop)
    {
        if (zone.MouseFilter == Control.MouseFilterMode.Ignore)
            zone.MouseFilter = Control.MouseFilterMode.Pass;

        _zones[zone] = onDrop;
    }

    public void ClearZones()
    {
        _zones.Clear();
    }

    public void FrameUpdate()
    {
        if (!_pending)
            return;

        if (!_input.IsKeyDown(Keyboard.Key.MouseLeft))
        {
            Cancel();
            return;
        }

        var mouse = _ui.MousePositionScaled.Position;
        if (_ghost == null)
        {
            if ((mouse - _start).Length() < Threshold)
                return;

            _ghost = _ghostFactory(_label);
            _ghost.MouseFilter = Control.MouseFilterMode.Ignore;
            _ui.PopupRoot.AddChild(_ghost);
        }

        LayoutContainer.SetPosition(_ghost, mouse + GhostOffset);
    }

    public void Cancel()
    {
        if (!_pending)
            return;

        Reset();
        Finished?.Invoke();
    }

    private void Reset()
    {
        _pending = false;
        _payload = default;
        _ghost?.Orphan();
        _ghost = null;
    }

    private void Drop(GUIBoundKeyEventArgs args)
    {
        var payload = _payload;
        Action<T>? target = null;
        if (_ghost != null)
        {
            for (var control = _ui.MouseGetControl(args.PointerLocation); control != null; control = control.Parent)
            {
                if (_zones.TryGetValue(control, out target))
                    break;
            }
        }

        if (!_pending)
            return;

        Reset();
        if (target != null && payload != null)
            target(payload);

        Finished?.Invoke();
    }

    private static Control CreateGhost(string label)
    {
        var ghost = new ShapedPanel
        {
            Children = { new Label { Text = label, Margin = new Thickness(10, 4) } },
        };
        ghost.AddStyleClass(NebulaStyleClass.Popup);
        return ghost;
    }
}
