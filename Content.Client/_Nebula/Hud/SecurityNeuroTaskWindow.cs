// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Client._Nebula.Squads.Cyber;
using Content.Client._Nebula.UserInterface;
using Content.Shared._Nebula.Hud;
using Content.Shared._Nebula.Squads;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client._Nebula.Hud;

/// <summary>
/// Neural interface tasks, projected over the world: pinned ones go to the HUD, available ones wait.
/// Tasks are dragged between the lists, personal tasks also into the bin.
/// </summary>
public sealed class SecurityNeuroTaskWindow : CyberWindow
{
    private const float BinHeight = 46f;

    public event Action<SecurityNeuroTaskKey, bool>? PinPressed;
    public event Action<int>? RemovePressed;
    public event Action<string>? AddPressed;

    private readonly BoxContainer _pinned;
    private readonly BoxContainer _available;
    private readonly Control _pinnedZone;
    private readonly Control _availableZone;
    private readonly CyberPanel _bin;
    private readonly NebulaDragDrop<SecurityNeuroTask> _drag = new(CyberGhost.Create);
    private readonly CyberButton _add;
    private List<SecurityNeuroTask> _tasks = [];
    private SecurityNeuroTaskInputWindow? _input;

    public SecurityNeuroTaskWindow()
    {
        Title = Loc.GetString("neuro-tasks-title");
        Tag = "NT-SEC // NEURO";
        Accent = CyberPalette.Cyan;
        Hologram = true;
        SetSize = new Vector2(660, 520);
        MinSize = new Vector2(500, 380);

        _pinned = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, SeparationOverride = 3 };
        _available = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, SeparationOverride = 3 };
        _pinnedZone = CreateColumn(Loc.GetString("neuro-tasks-pinned"), _pinned);
        _availableZone = CreateColumn(Loc.GetString("neuro-tasks-available"), _available);

        var binText = CyberDraw.Label(Loc.GetString("neuro-tasks-bin").ToUpperInvariant(), CyberPalette.RedText, 12, true);
        binText.VerticalAlignment = VAlignment.Center;
        binText.Margin = new Thickness(14, 0);
        binText.ClipText = true;
        binText.HorizontalExpand = true;
        _bin = new CyberPanel
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            Fill = Color.FromHex("#28000a").WithAlpha(0.9f),
            Border = CyberPalette.Red,
            BorderThickness = 2f,
            Stripe = CyberPalette.Red,
            Cut = 8f,
            HorizontalExpand = true,
            MinHeight = BinHeight,
            Children = { binText },
        };

        _add = new CyberButton(Loc.GetString("neuro-tasks-add")) { MinHeight = BinHeight };
        _add.OnPressed += _ => OpenInput();

        Contents.AddChild(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            VerticalExpand = true,
            SeparationOverride = 10,
            Children = { _pinnedZone, _availableZone },
        });
        Contents.AddChild(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 10,
            Children = { _bin, _add },
        });

        _drag.Finished += Rebuild;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        _drag.FrameUpdate();
    }

    public override void Close()
    {
        _drag.Cancel();
        _input?.Close();
        base.Close();
    }

    public void UpdateTasks(List<SecurityNeuroTask> tasks)
    {
        _tasks = tasks;
        if (!_drag.Dragging)
            Rebuild();
    }

    private void Rebuild()
    {
        _pinned.RemoveAllChildren();
        _available.RemoveAllChildren();
        _drag.ClearZones();
        _drag.AddZone(_pinnedZone, task => SetPinned(task, true));
        _drag.AddZone(_availableZone, task => SetPinned(task, false));
        _drag.AddZone(_bin, task =>
        {
            if (task.Key.Kind == SecurityNeuroTaskKind.Personal)
                RemovePressed?.Invoke(task.Key.Id);
        });

        var personal = 0;
        foreach (var task in _tasks)
        {
            (task.Pinned ? _pinned : _available).AddChild(CreateTask(task));
            if (task.Key.Kind == SecurityNeuroTaskKind.Personal)
                personal++;
        }

        if (_pinned.ChildCount == 0)
            _pinned.AddChild(Hint(Loc.GetString("neuro-tasks-pinned-empty")));

        if (_available.ChildCount == 0)
            _available.AddChild(Hint(Loc.GetString("neuro-tasks-available-empty")));

        _add.Disabled = personal >= SquadLimits.MaxPersonalTasks;
    }

    private void SetPinned(SecurityNeuroTask task, bool pinned)
    {
        if (task.Pinned != pinned)
            PinPressed?.Invoke(task.Key, pinned);
    }

    private Control CreateTask(SecurityNeuroTask task)
    {
        var (kind, color) = task.Key.Kind switch
        {
            SecurityNeuroTaskKind.Order => ("neuro-tasks-kind-order", CyberPalette.Red),
            SecurityNeuroTaskKind.Wanted => ("neuro-tasks-kind-wanted", CyberPalette.Warning),
            _ => ("neuro-tasks-kind-personal", CyberPalette.Cyan),
        };

        var row = new CyberButton(task.Title, CyberButtonStyle.Row)
        {
            Accent = color,
            MinHeight = 30,
            ToolTip = task.Description.Length > 0 ? task.Description : null,
        };
        row.Label.HorizontalExpand = true;
        row.Label.ClipText = true;

        var tag = CyberDraw.Label(Loc.GetString(kind).ToUpperInvariant(), color, 10, true);
        tag.VerticalAlignment = Control.VAlignment.Center;
        row.Content.AddChild(tag);
        tag.SetPositionFirst();

        _drag.Attach(row, task, task.Title);
        return row;
    }

    private void OpenInput()
    {
        if (_input is { Disposed: false })
        {
            _input.Close();
            return;
        }

        _input = new SecurityNeuroTaskInputWindow();
        _input.Confirmed += title => AddPressed?.Invoke(title);
        _input.OnClose += () => _input = null;
        _input.OpenCentered();
    }

    private static Control CreateColumn(string title, BoxContainer list)
    {
        var column = new CyberPanel
        {
            Fill = Color.FromHex("#000c10").WithAlpha(0.6f),
            Stripe = CyberPalette.CyanDim,
            HorizontalExpand = true,
            VerticalExpand = true,
        };
        var label = CyberDraw.Label(title.ToUpperInvariant(), CyberPalette.Cyan, 11, true);
        label.Margin = new Thickness(14, 10, 12, 6);
        list.Margin = new Thickness(12, 2, 10, 10);
        column.AddChild(label);
        column.AddChild(new ScrollContainer { VerticalExpand = true, HScrollEnabled = false, Children = { list } });
        return column;
    }

    private static Control Hint(string text)
    {
        return CyberDraw.Wrapped(text, CyberPalette.Muted);
    }
}

/// <summary>
/// Asks for the title of a new personal task.
/// </summary>
public sealed class SecurityNeuroTaskInputWindow : CyberWindow
{
    public event Action<string>? Confirmed;

    private readonly LineEdit _input;

    public SecurityNeuroTaskInputWindow()
    {
        Title = Loc.GetString("neuro-tasks-add-title");
        Tag = "NT-SEC // NEURO";
        Accent = CyberPalette.Cyan;
        Hologram = true;
        MinSize = new Vector2(480, 150);
        SetSize = new Vector2(500, 150);

        _input = CyberDraw.Input(new LineEdit
        {
            HorizontalExpand = true,
            PlaceHolder = Loc.GetString("neuro-tasks-add-placeholder"),
            IsValid = text => text.Length <= SquadLimits.MaxTitleLength,
        });

        var confirm = new CyberButton(Loc.GetString("neuro-tasks-add-confirm")) { HorizontalAlignment = HAlignment.Right };

        void Submit()
        {
            var title = _input.Text.Trim();
            if (title.Length == 0)
                return;

            Confirmed?.Invoke(title);
            Close();
        }

        confirm.OnPressed += _ => Submit();
        _input.OnTextEntered += _ => Submit();

        Contents.AddChild(_input);
        Contents.AddChild(confirm);
    }

    protected override void Opened()
    {
        base.Opened();
        _input.GrabKeyboardFocus();
    }
}
