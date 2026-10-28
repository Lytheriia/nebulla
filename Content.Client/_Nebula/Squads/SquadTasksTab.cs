// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Nebula.Squads.Cyber;
using Content.Shared._Nebula.Squads;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client._Nebula.Squads;

/// <summary>
/// Tasks of the head of security. A chosen squad shows its shared tasks and, dimmed, the personal tasks
/// of its members; a chosen member shows their personal tasks and, dimmed, their squad's.
/// The right panel creates a task for the chosen squad or member, or deletes the chosen task.
/// </summary>
public sealed class SquadTasksTab : BoxContainer
{
    private const float DimAlpha = 0.45f;

    public event Action<int?, uint?, string, string>? CreateTaskPressed;
    public event Action<int>? DeleteTaskPressed;

    private readonly BoxContainer _targets;
    private readonly BoxContainer _tasks;
    private readonly Label _target;
    private readonly LineEdit _title;
    private readonly TextEdit _description;
    private readonly CyberButton _create;
    private readonly CyberButton _delete;
    private SquadTabletState? _state;
    private int? _squad;
    private uint? _record;
    private int? _task;

    public SquadTasksTab()
    {
        Orientation = LayoutOrientation.Horizontal;
        SeparationOverride = 10;
        VerticalExpand = true;

        _targets = new BoxContainer { Orientation = LayoutOrientation.Vertical, SeparationOverride = 3 };
        _tasks = new BoxContainer { Orientation = LayoutOrientation.Vertical, SeparationOverride = 3 };

        _target = CyberDraw.Label(string.Empty, CyberPalette.Red, 12, true);
        _target.ClipText = true;

        _title = CyberDraw.Input(new LineEdit
        {
            HorizontalExpand = true,
            PlaceHolder = Loc.GetString("squad-tablet-task-title-placeholder"),
            IsValid = text => text.Length <= SquadLimits.MaxTitleLength,
        });
        _title.OnTextChanged += _ => UpdateButtons();

        _description = new TextEdit
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            Placeholder = new Rope.Leaf(Loc.GetString("squad-tablet-task-description-placeholder")),
            Margin = new Thickness(8, 6),
        };
        _description.OnTextChanged += _ => UpdateButtons();

        var descriptionBox = new CyberPanel
        {
            Fill = CyberPalette.Input,
            Border = CyberPalette.CyanDim,
            Stripe = CyberPalette.Cyan,
            Cut = 0f,
            VerticalExpand = true,
            MinHeight = 120,
            Children = { _description },
        };

        _create = new CyberButton(Loc.GetString("squad-tablet-task-create"));
        _create.OnPressed += _ => Create();

        _delete = new CyberButton(Loc.GetString("squad-tablet-task-delete")) { Accent = CyberPalette.Red };
        _delete.OnPressed += _ =>
        {
            if (_task is { } task)
                DeleteTaskPressed?.Invoke(task);
        };

        var panel = SquadUi.Section();
        panel.AddChild(new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
            SeparationOverride = 10,
            VerticalExpand = true,
            Margin = new Thickness(16, 12, 14, 14),
            Children =
            {
                _target,
                _title,
                descriptionBox,
                CyberDraw.Wrapped(Loc.GetString("squad-tablet-task-description-hint"), CyberPalette.Muted),
                new BoxContainer
                {
                    Orientation = LayoutOrientation.Horizontal,
                    HorizontalAlignment = HAlignment.Right,
                    SeparationOverride = 8,
                    Children = { _delete, _create },
                },
            },
        });

        AddChild(SquadUi.Column(Loc.GetString("squad-tablet-task-targets"), _targets));
        AddChild(SquadUi.Column(Loc.GetString("squad-tablet-task-list"), _tasks, width: 270f));
        AddChild(panel);
        UpdateButtons();
    }

    public void UpdateState(SquadTabletState state)
    {
        var same = SquadUi.SameLayout(_state, state);
        _state = state;
        if (same)
            return;

        if (_squad is { } squad && !SquadUi.TryGetSquad(state, squad, out _))
            _squad = null;

        if (_record is { } record && FindMember(record) == null)
            _record = null;

        if (_task is { } task && !state.Tasks.Exists(t => t.Id == task))
            _task = null;

        Rebuild();
    }

    #region Build

    private void Rebuild()
    {
        _targets.RemoveAllChildren();
        _tasks.RemoveAllChildren();
        if (_state == null)
            return;

        foreach (var squad in _state.Squads)
        {
            var id = squad.Id;
            var row = new CyberButton(string.Empty, CyberButtonStyle.Row) { Accent = squad.Color, Active = _squad == id, MinHeight = 30 };
            row.Label.Text = squad.Name.ToUpperInvariant();
            row.Label.FontOverride = CyberDraw.Font(12, true);
            row.Content.RemoveAllChildren();
            row.Content.AddChild(SquadUi.ColorDot(squad.Color));
            row.Content.AddChild(row.Label);
            row.OnPressed += _ => Select(id, null);
            _targets.AddChild(row);

            foreach (var member in squad.Members)
            {
                _targets.AddChild(CreateMember(member));
            }
        }

        if (_state.Free.Count > 0)
            _targets.AddChild(SquadUi.Muted(Loc.GetString("squad-tablet-free").ToUpperInvariant()));

        foreach (var member in _state.Free)
        {
            _targets.AddChild(CreateMember(member));
        }

        if (_squad is { } squadId && SquadUi.TryGetSquad(_state, squadId, out var shown))
        {
            foreach (var task in _state.Tasks)
            {
                if (task.Squad == squadId)
                    _tasks.AddChild(CreateTask(task, false));
                else if (task.Record is { } record && shown.Members.Exists(m => m.Record == record))
                    _tasks.AddChild(CreateTask(task, true));
            }
        }
        else if (_record is { } recordId)
        {
            var own = FindSquadOf(recordId);
            foreach (var task in _state.Tasks)
            {
                if (task.Record == recordId)
                    _tasks.AddChild(CreateTask(task, false));
                else if (own != null && task.Squad == own)
                    _tasks.AddChild(CreateTask(task, true));
            }
        }

        if (_tasks.ChildCount == 0)
        {
            var hint = _squad == null && _record == null ? "squad-tablet-task-pick" : "squad-tablet-task-none";
            _tasks.AddChild(SquadUi.Muted(Loc.GetString(hint)));
        }

        _target.Text = TargetName().ToUpperInvariant();
        UpdateButtons();
    }

    private Control CreateMember(SquadTabletMember member)
    {
        var record = member.Record;
        var row = new CyberButton(member.Name, CyberButtonStyle.Row) { Active = _record == record, MinHeight = 26 };
        row.Content.Margin = new Thickness(22, 3, 8, 3);
        row.Label.FontOverride = CyberDraw.Font(11);
        row.Label.HorizontalExpand = true;
        row.Label.ClipText = true;
        row.OnPressed += _ => Select(null, record);
        return row;
    }

    private Control CreateTask(SquadTabletTask task, bool dim)
    {
        var text = task.Description.Length == 0
            ? task.Title
            : Loc.GetString("squad-tablet-task-described", ("title", task.Title));
        var row = new CyberButton(text, CyberButtonStyle.Row)
        {
            Accent = CyberPalette.Red,
            Active = _task == task.Id,
            MinHeight = 30,
        };
        row.Label.ClipText = true;
        row.Label.HorizontalExpand = true;
        if (dim)
            row.Content.Modulate = Color.White.WithAlpha(DimAlpha);

        row.OnPressed += _ =>
        {
            _task = task.Id;
            _title.Text = task.Title;
            _description.TextRope = new Rope.Leaf(task.Description);
            Rebuild();
        };
        return row;
    }

    #endregion

    #region Helpers

    private void Select(int? squad, uint? record)
    {
        _squad = squad;
        _record = record;
        _task = null;
        Rebuild();
    }

    private void Create()
    {
        var title = _title.Text.Trim();
        if (title.Length == 0 || (_squad == null && _record == null))
            return;

        CreateTaskPressed?.Invoke(_squad, _record, title, Rope.Collapse(_description.TextRope).Trim());
        _title.Text = string.Empty;
        _description.TextRope = Rope.Leaf.Empty;
        _task = null;
    }

    private void UpdateButtons()
    {
        _create.Disabled = (_squad == null && _record == null)
            || _title.Text.Trim().Length == 0
            || Rope.CalcTotalLength(_description.TextRope) > SquadLimits.MaxDescriptionLength
            || _state?.Tasks.Count >= SquadLimits.MaxTasks;
        _delete.Disabled = _task == null;
    }

    private string TargetName()
    {
        if (_state == null)
            return string.Empty;

        if (_squad is { } squad && SquadUi.TryGetSquad(_state, squad, out var found))
            return Loc.GetString("squad-tablet-task-for-squad", ("name", found.Name));

        if (_record is { } record && FindMember(record) is { } member)
            return Loc.GetString("squad-tablet-task-for-member", ("name", member.Name));

        return Loc.GetString("squad-tablet-task-for-none");
    }

    private SquadTabletMember? FindMember(uint record)
    {
        if (_state == null)
            return null;

        foreach (var squad in _state.Squads)
        {
            var index = squad.Members.FindIndex(m => m.Record == record);
            if (index >= 0)
                return squad.Members[index];
        }

        var free = _state.Free.FindIndex(m => m.Record == record);
        return free >= 0 ? _state.Free[free] : null;
    }

    private int? FindSquadOf(uint record)
    {
        if (_state == null)
            return null;

        foreach (var squad in _state.Squads)
        {
            if (squad.Members.Exists(m => m.Record == record))
                return squad.Id;
        }

        return null;
    }

    #endregion
}
