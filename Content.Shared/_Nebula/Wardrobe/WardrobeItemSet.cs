// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;

namespace Content.Shared._Nebula.Wardrobe;

/// <summary>
/// Wardrobe items that share the same restrictions. An item that needs its own restrictions goes into its own set.
/// </summary>
[DataDefinition]
public sealed partial class WardrobeItemSet
{
    public const int MaxTier = 3;

    /// <summary>
    /// Boosty tier required to take the items, 0 for everyone.
    /// </summary>
    [DataField]
    public int Tier;

    /// <summary>
    /// If not empty, the items are only given to jobs from these departments or <see cref="Jobs"/>.
    /// </summary>
    [DataField]
    public List<ProtoId<DepartmentPrototype>> Departments = [];

    /// <summary>
    /// If not empty, the items are only given to these jobs or jobs from <see cref="Departments"/>.
    /// </summary>
    [DataField]
    public List<ProtoId<JobPrototype>> Jobs = [];

    /// <summary>
    /// Components removed from the spawned items, e.g. armor of sponsor clothing.
    /// </summary>
    [DataField(customTypeSerializer: typeof(CustomListSerializer<string, ComponentNameSerializer>))]
    public List<string> RemoveComponents = [];

    [DataField(required: true)]
    public List<EntProtoId> Items = [];

    /// <summary>
    /// Checks a Boosty rank against the required tier, null meaning no subscription.
    /// </summary>
    public bool IsAllowed(int? rank)
    {
        return Math.Clamp(rank ?? 0, 0, MaxTier) >= Tier;
    }

    /// <summary>
    /// Checks whether the items can be given to the job, items without job restrictions are given to anyone.
    /// </summary>
    public bool IsAllowedFor(ProtoId<JobPrototype>? job, IPrototypeManager prototypes)
    {
        if (Departments.Count == 0 && Jobs.Count == 0)
            return true;

        if (job is not { } jobId)
            return false;

        if (Jobs.Contains(jobId))
            return true;

        foreach (var department in Departments)
        {
            if (prototypes.Index(department).Roles.Contains(jobId))
                return true;
        }

        return false;
    }
}
