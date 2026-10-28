// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Client._Nebula.Squads.Cyber;
using Content.Shared._Nebula.Squads;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Nebula.Squads;

/// <summary>
/// Squad tablet of the head of security: a tab bar on top and the chosen tab below.
/// </summary>
public sealed class SquadTabletWindow : CyberWindow
{
    private const int TimersTab = 2;

    public readonly SquadMonitorTab Monitor = new();
    public readonly SquadRosterTab Roster = new();
    public readonly SquadTimersTab Timers = new();
    public readonly SquadTasksTab Tasks = new();

    private readonly CyberTabBar _bar = new();
    private readonly List<Control> _tabs = [];

    public SquadTabletWindow()
    {
        Title = Loc.GetString("squad-tablet-title");
        Tag = "NT-SEC // HOS";
        SetSize = new Vector2(1180, 740);
        MinSize = new Vector2(1150, 720);

        var body = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical, VerticalExpand = true };
        Contents.AddChild(_bar);
        Contents.AddChild(body);

        AddTab(body, "squad-tablet-tab-monitor", Monitor);
        AddTab(body, "squad-tablet-tab-squads", Roster);
        AddTab(body, "squad-tablet-tab-timers", Timers);
        AddTab(body, "squad-tablet-tab-tasks", Tasks);

        _bar.TabSelected += SelectTab;
        _bar.Select(0);
    }

    public void UpdateState(SquadTabletState state)
    {
        Monitor.UpdateState(state);
        Roster.UpdateState(state);
        Timers.UpdateState(state);
        Tasks.UpdateState(state);
    }

    /// <summary>
    /// Toast over any tab and a blinking dot on the timers tab until it is opened.
    /// </summary>
    public void TimerFinished(string name)
    {
        ShowToast(Loc.GetString("squad-tablet-timer-toast-head"), Loc.GetString("squad-tablet-timer-toast", ("name", name)));
        _bar.SetPing(TimersTab, true);
    }

    private void AddTab(BoxContainer body, string name, Control tab)
    {
        _bar.AddTab(Loc.GetString(name));
        tab.Margin = new Thickness(0, 10, 0, 0);
        body.AddChild(tab);
        _tabs.Add(tab);
    }

    private void SelectTab(int index)
    {
        for (var i = 0; i < _tabs.Count; i++)
        {
            _tabs[i].Visible = i == index;
        }
    }
}
