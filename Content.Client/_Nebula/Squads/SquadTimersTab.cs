// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Client._Nebula.Squads.Cyber;
using Content.Client.GameTicking;
using Content.Shared._Nebula.Squads;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client._Nebula.Squads;

/// <summary>
/// Running brig timers of the station, counted down locally between states.
/// A timer that ran out or disappeared from the state stays for a while marked as finished.
/// </summary>
public sealed partial class SquadTimersTab : BoxContainer
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IEntityManager _entMan = default!;

    private static readonly TimeSpan FinishedTime = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan LowTime = TimeSpan.FromSeconds(20);
    private const float FlashPeriod = 0.25f;

    private readonly BoxContainer _list;
    private readonly ClientGameTicker _ticker;
    private readonly Dictionary<NetEntity, Entry> _timers = [];

    private sealed class Entry(string name, TimeSpan end)
    {
        public string Name = name;
        public TimeSpan End = end;
        public TimeSpan? GoneAt;
        public Label? Remaining;
        public CyberPanel? Card;

        /// <summary>
        /// Whole seconds the label shows, -1 once finished, so the text is only set when it changes.
        /// </summary>
        public long ShownSeconds = long.MinValue;
    }

    public SquadTimersTab()
    {
        IoCManager.InjectDependencies(this);
        _ticker = _entMan.System<ClientGameTicker>();

        Orientation = LayoutOrientation.Vertical;
        VerticalExpand = true;

        _list = new BoxContainer { Orientation = LayoutOrientation.Vertical, SeparationOverride = 6, Margin = new Thickness(12, 2, 12, 12) };
        var title = CyberDraw.Label(Loc.GetString("squad-tablet-timers").ToUpperInvariant(), CyberPalette.Red, 11, true);
        title.Margin = new Thickness(14, 10, 12, 6);

        var section = SquadUi.Section();
        section.AddChild(title);
        section.AddChild(new ScrollContainer { VerticalExpand = true, HScrollEnabled = false, Children = { _list } });
        AddChild(section);
    }

    public void UpdateState(SquadTabletState state)
    {
        foreach (var (entity, entry) in _timers)
        {
            if (!state.Timers.Exists(t => t.Entity == entity))
                entry.GoneAt ??= _timing.CurTime;
        }

        foreach (var timer in state.Timers)
        {
            if (_timers.TryGetValue(timer.Entity, out var entry))
            {
                entry.Name = timer.Name;
                entry.End = timer.End;
                entry.GoneAt = null;
            }
            else
                _timers[timer.Entity] = new Entry(timer.Name, timer.End);
        }

        Rebuild();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        List<NetEntity>? expired = null;
        foreach (var (entity, entry) in _timers)
        {
            if (FinishedAt(entry) is { } finished && _timing.CurTime - finished > FinishedTime)
            {
                (expired ??= []).Add(entity);
                continue;
            }

            UpdateTimer(entry);
        }

        if (expired == null)
            return;

        foreach (var entity in expired)
        {
            _timers.Remove(entity);
        }

        Rebuild();
    }

    private void Rebuild()
    {
        _list.RemoveAllChildren();
        if (_timers.Count == 0)
        {
            _list.AddChild(SquadUi.Muted(Loc.GetString("squad-tablet-timers-empty")));
            return;
        }

        var sorted = _timers.Values.ToList();
        sorted.Sort((a, b) => a.End.CompareTo(b.End));
        foreach (var entry in sorted)
        {
            _list.AddChild(CreateTimer(entry));
        }
    }

    private Control CreateTimer(Entry entry)
    {
        var card = new CyberPanel
        {
            Orientation = LayoutOrientation.Horizontal,
            Fill = CyberPalette.PanelRaised,
            Cut = 0f,
            SeparationOverride = 16,
            MinHeight = 44,
        };

        var name = CyberDraw.Label(entry.Name.ToUpperInvariant(), CyberPalette.Text, 13, true);
        name.HorizontalExpand = true;
        name.ClipText = true;
        name.VerticalAlignment = VAlignment.Center;
        name.Margin = new Thickness(14, 0, 0, 0);

        var shiftEnd = entry.End - _ticker.RoundStartTimeSpan;
        var end = CyberDraw.Label(Loc.GetString("squad-tablet-timer-end", ("time", shiftEnd.ToString(@"hh\:mm\:ss"))),
            CyberPalette.Muted,
            11);
        end.VerticalAlignment = VAlignment.Center;

        entry.Remaining = CyberDraw.Label(string.Empty, CyberPalette.Cyan, 18, true);
        entry.Remaining.MinWidth = 100;
        entry.Remaining.Align = Label.AlignMode.Right;
        entry.Remaining.VerticalAlignment = VAlignment.Center;
        entry.Remaining.Margin = new Thickness(0, 0, 14, 0);
        entry.Card = card;
        entry.ShownSeconds = long.MinValue;

        card.AddChild(name);
        card.AddChild(end);
        card.AddChild(entry.Remaining);
        UpdateTimer(entry);
        return card;
    }

    /// <summary>
    /// Cyan while running, yellow in the last seconds, flashing red once done.
    /// </summary>
    private void UpdateTimer(Entry entry)
    {
        if (entry.Remaining == null || entry.Card == null)
            return;

        var finished = FinishedAt(entry);
        var color = finished != null
            ? CyberPalette.Red
            : entry.End - _timing.CurTime < LowTime ? CyberPalette.Warning : CyberPalette.Cyan;

        var shown = finished != null ? -1 : (long) (entry.End - _timing.CurTime).TotalSeconds;
        if (shown != entry.ShownSeconds)
        {
            entry.ShownSeconds = shown;
            entry.Remaining.Text = RemainingText(entry);
        }

        entry.Remaining.FontColorOverride = color;
        entry.Card.Stripe = color;
        entry.Card.Fill = finished is { } at && (_timing.CurTime - at).TotalSeconds % FlashPeriod < FlashPeriod / 2
            ? CyberPalette.Red.WithAlpha(0.18f)
            : CyberPalette.PanelRaised;
    }

    private string RemainingText(Entry entry)
    {
        if (FinishedAt(entry) != null)
            return Loc.GetString("squad-tablet-timer-finished");

        var left = entry.End - _timing.CurTime;
        return left.ToString(left.TotalHours >= 1 ? @"h\:mm\:ss" : @"mm\:ss");
    }

    /// <summary>
    /// A timer is finished when its end passed or the server stopped reporting it, whichever came first.
    /// </summary>
    private TimeSpan? FinishedAt(Entry entry)
    {
        if (entry.End <= _timing.CurTime)
            return entry.GoneAt is { } gone && gone < entry.End ? gone : entry.End;

        return entry.GoneAt;
    }
}
