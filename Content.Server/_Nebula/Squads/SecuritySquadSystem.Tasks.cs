// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._Nebula.Hud;
using Content.Shared._Nebula.Squads;
using Content.Shared.CriminalRecords;
using Content.Shared.Security;
using Content.Shared.StationRecords;
using Content.Shared.StationRecords.Events;
using Robust.Shared.Player;

namespace Content.Server._Nebula.Squads;

public sealed partial class SecuritySquadSystem
{
    private const string EyesSlot = "eyes";

    private readonly record struct WantedEntry(int Id, string Name, string Reason);

    /// <summary>
    /// Wanted list of each station as of the last refresh, so a record change that leaves it alone refreshes nothing.
    /// </summary>
    private readonly Dictionary<EntityUid, List<WantedEntry>> _wanted = [];

    #region Component events

    /// <summary>
    /// Criminal records have no broadcast event of their own, a changed status shows up as a modified record.
    /// </summary>
    [SubscribeLocalEvent]
    private void OnRecordModified(ref RecordModifiedEvent args)
    {
        var station = args.Key.OriginStation;
        if (TryGetSquad(args.Key, out var squads, out var squad))
            RefreshSquad(squads, squad);

        var wanted = GetWanted(station);
        if (_wanted.TryGetValue(station, out var known) && known.SequenceEqual(wanted))
            return;

        RefreshAllTasks(station, wanted);
    }

    #endregion

    #region Network events

    [SubscribeNetworkEvent]
    private void OnTaskAdd(SecurityNeuroTaskAddEvent ev, EntitySessionEventArgs args)
    {
        var title = ev.Title?.Trim() ?? string.Empty;
        if (!TryGetActiveInterface(args.SenderSession, out var ent)
            || title.Length == 0
            || title.Length > SquadLimits.MaxTitleLength
            || ent.Comp.Personal.Count >= SquadLimits.MaxPersonalTasks)
            return;

        ent.Comp.Personal.Add(new SecurityNeuroPersonalTask { Id = ent.Comp.NextPersonalId++, Title = title });
        RefreshTasks(ent);
    }

    [SubscribeNetworkEvent]
    private void OnTaskRemove(SecurityNeuroTaskRemoveEvent ev, EntitySessionEventArgs args)
    {
        if (!TryGetActiveInterface(args.SenderSession, out var ent)
            || ent.Comp.Personal.RemoveAll(t => t.Id == ev.Id) == 0)
            return;

        ent.Comp.Unpinned.Remove(new SecurityNeuroTaskKey(SecurityNeuroTaskKind.Personal, ev.Id));
        RefreshTasks(ent);
    }

    [SubscribeNetworkEvent]
    private void OnTaskPin(SecurityNeuroTaskPinEvent ev, EntitySessionEventArgs args)
    {
        if (!TryGetActiveInterface(args.SenderSession, out var ent)
            || !ent.Comp.Tasks.Exists(t => t.Key == ev.Key))
            return;

        if (ev.Pinned)
            ent.Comp.Unpinned.Remove(ev.Key);
        else
            ent.Comp.Unpinned.Add(ev.Key);

        RefreshTasks(ent);
    }

    #endregion

    #region API

    /// <summary>
    /// Gives a task to the whole squad or to one squad candidate, exactly one of them must be set.
    /// </summary>
    public bool TryCreateTask(EntityUid station,
        int? squadId,
        StationRecordKey? member,
        string title,
        string description,
        [NotNullWhen(true)] out SquadTask? task)
    {
        task = null;
        title = title.Trim();
        description = description.Trim();
        if (!TryComp<StationSquadsComponent>(station, out var squads)
            || squads.Tasks.Count >= SquadLimits.MaxTasks
            || squadId == null == (member == null)
            || title.Length == 0
            || title.Length > SquadLimits.MaxTitleLength
            || description.Length > SquadLimits.MaxDescriptionLength)
            return false;

        if (squadId is { } id && !squads.Squads.Exists(s => s.Id == id))
            return false;

        if (member is { } key
            && (!_records.TryGetRecord<GeneralStationRecord>(key, out var record) || !IsSquadCandidate(record.JobPrototype)))
            return false;

        task = new SquadTask
        {
            Id = squads.NextTaskId++,
            Squad = squadId,
            Member = member,
            Title = title,
            Description = description,
        };
        squads.Tasks.Add(task);

        RefreshAllTasks(station);
        UpdateTablets(station);
        return true;
    }

    public bool TryDeleteTask(EntityUid station, int taskId, [NotNullWhen(true)] out SquadTask? task)
    {
        task = null;
        if (!TryComp<StationSquadsComponent>(station, out var squads)
            || squads.Tasks.Find(t => t.Id == taskId) is not { } found)
            return false;

        task = found;
        squads.Tasks.Remove(found);
        RefreshAllTasks(station);
        UpdateTablets(station);
        return true;
    }

    #endregion

    #region Helpers

    /// <summary>
    /// The task window only works while the HUD is on, so the server checks the same equipment and profile.
    /// </summary>
    private bool TryGetActiveInterface(ICommonSession session, out Entity<SecurityNeuroComponent> ent)
    {
        ent = default;
        if (session.AttachedEntity is not { } uid
            || !TryComp<SecurityNeuroComponent>(uid, out var neuro)
            || !TryComp<NeuroLinkComponent>(uid, out var link)
            || !_inventory.TryGetSlotEntity(uid, EyesSlot, out var eyes)
            || !TryComp<SkewedHudComponent>(eyes, out var hud)
            || hud.Profiles.Count > 0 && !hud.Profiles.Contains(link.Profile))
            return false;

        ent = (uid, neuro);
        return true;
    }

    private void RefreshAllTasks(EntityUid station, List<WantedEntry>? wanted = null)
    {
        wanted ??= GetWanted(station);
        _wanted[station] = wanted;
        foreach (var mob in GetMobs(station).Values)
        {
            RefreshTasks(mob, wanted);
        }
    }

    /// <summary>
    /// Everyone wanted on the station, collected once per refresh pass.
    /// </summary>
    private List<WantedEntry> GetWanted(EntityUid station)
    {
        List<WantedEntry> wanted = [];
        foreach (var (id, criminal) in _records.GetRecordsOfType<CriminalRecord>(station))
        {
            if (criminal.Status != SecurityStatus.Wanted
                || !_records.TryGetRecord<GeneralStationRecord>(new StationRecordKey(id, station), out var general))
                continue;

            wanted.Add(new WantedEntry((int) id, general.Name, criminal.Reason ?? string.Empty));
        }

        return wanted;
    }

    /// <summary>
    /// Collects the owner's tasks: from the head of security for their squad or them personally,
    /// their own personal tasks and everyone wanted on their station. The tasks are sent only when they changed.
    /// </summary>
    private void RefreshTasks(Entity<SecurityNeuroComponent> ent, List<WantedEntry>? wanted = null)
    {
        List<SecurityNeuroTask> tasks = [];
        if (HasComp<NeuroSignalLostComponent>(ent))
        {
            // Without a link only the personal tasks, kept on the interface itself, change.
            foreach (var task in ent.Comp.Tasks)
            {
                if (task.Key.Kind != SecurityNeuroTaskKind.Personal)
                    tasks.Add(task);
            }
        }
        else if (ent.Comp.Record is { } key)
        {
            if (TryComp<StationSquadsComponent>(key.OriginStation, out var squads))
            {
                var squad = squads.Squads.Find(s => s.Members.Contains(key));
                foreach (var task in squads.Tasks)
                {
                    var own = task.Member is { } member
                        ? member.Equals(key)
                        : squad != null && task.Squad == squad.Id;
                    if (own)
                        tasks.Add(BuildTask(ent, SecurityNeuroTaskKind.Order, task.Id, task.Title, task.Description));
                }
            }

            foreach (var entry in wanted ?? GetWanted(key.OriginStation))
            {
                tasks.Add(BuildTask(ent, SecurityNeuroTaskKind.Wanted, entry.Id, entry.Name, entry.Reason));
            }
        }

        foreach (var personal in ent.Comp.Personal)
        {
            tasks.Add(BuildTask(ent, SecurityNeuroTaskKind.Personal, personal.Id, personal.Title, string.Empty));
        }

        ent.Comp.Unpinned.RemoveWhere(k => !tasks.Exists(t => t.Key == k));
        if (tasks.SequenceEqual(ent.Comp.Tasks))
            return;

        ent.Comp.Tasks = tasks;
        DirtyField(ent, ent.Comp, nameof(SecurityNeuroComponent.Tasks));
    }

    private static SecurityNeuroTask BuildTask(Entity<SecurityNeuroComponent> ent,
        SecurityNeuroTaskKind kind,
        int id,
        string title,
        string description)
    {
        var key = new SecurityNeuroTaskKey(kind, id);
        return new SecurityNeuroTask
        {
            Key = key,
            Title = title,
            Description = description,
            Pinned = !ent.Comp.Unpinned.Contains(key),
        };
    }

    #endregion
}
