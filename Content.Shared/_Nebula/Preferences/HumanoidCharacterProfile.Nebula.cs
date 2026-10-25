// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using Content.Shared._Nebula.Roles;
using Content.Shared._Nebula.Wardrobe;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.Preferences;

public sealed partial class HumanoidCharacterProfile
{
    /// <summary>
    /// Alternate title picked for each job, jobs without an entry use their own name.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<JobPrototype>, ProtoId<JobAlternateTitlePrototype>> JobAlternateTitles { get; set; } = [];

    public HumanoidCharacterProfile WithJobAltTitle(ProtoId<JobPrototype> job, ProtoId<JobAlternateTitlePrototype>? title)
    {
        var titles = new Dictionary<ProtoId<JobPrototype>, ProtoId<JobAlternateTitlePrototype>>(JobAlternateTitles);

        if (title is { } value)
            titles[job] = value;
        else
            titles.Remove(job);

        return new(this) { JobAlternateTitles = titles };
    }

    public bool TryGetJobAltTitle(
        ProtoId<JobPrototype> job,
        IPrototypeManager prototypes,
        [NotNullWhen(true)] out JobAlternateTitlePrototype? title)
    {
        title = null;
        return JobAlternateTitles.TryGetValue(job, out var id)
               && prototypes.TryIndex(id, out title)
               && title.Job == job;
    }

    private bool JobAltTitlesEqual(HumanoidCharacterProfile other)
    {
        if (JobAlternateTitles.Count != other.JobAlternateTitles.Count)
            return false;

        foreach (var (job, title) in JobAlternateTitles)
        {
            if (!other.JobAlternateTitles.TryGetValue(job, out var otherTitle) || otherTitle != title)
                return false;
        }

        return true;
    }

    private void ValidateJobAltTitles(IPrototypeManager prototypes)
    {
        foreach (var job in JobAlternateTitles.Keys)
        {
            if (!TryGetJobAltTitle(job, prototypes, out _))
                JobAlternateTitles.Remove(job);
        }
    }

    /// <summary>
    /// Clothing picked in the wardrobe, given in a bag on spawn. Restrictions are checked when the items are given.
    /// </summary>
    [DataField]
    public List<EntProtoId> Wardrobe { get; set; } = [];

    /// <summary>
    /// Selects or deselects a wardrobe item, pushing out the oldest item of its category when it is full.
    /// </summary>
    public HumanoidCharacterProfile WithWardrobeItem(EntProtoId item, bool selected, IPrototypeManager prototypes)
    {
        List<EntProtoId> wardrobe = [.. Wardrobe];
        wardrobe.Remove(item);

        if (selected && WardrobeHelpers.TryFindItem(prototypes, item, out var category, out _))
        {
            var inCategory = wardrobe.FindAll(other => category.TryGetSet(other, out _));
            for (var i = 0; i <= inCategory.Count - category.MaxLimit; i++)
            {
                wardrobe.Remove(inCategory[i]);
            }

            wardrobe.Add(item);
        }

        return new(this) { Wardrobe = wardrobe };
    }

    private void ValidateWardrobe(IPrototypeManager prototypes)
    {
        Dictionary<string, int> counts = [];
        var valid = new List<EntProtoId>(Wardrobe.Count);

        foreach (var item in Wardrobe)
        {
            if (valid.Contains(item) || !WardrobeHelpers.TryFindItem(prototypes, item, out var category, out _))
                continue;

            var count = counts.GetValueOrDefault(category.ID);
            if (count >= category.MaxLimit)
                continue;

            counts[category.ID] = count + 1;
            valid.Add(item);
        }

        Wardrobe = valid;
    }
}
