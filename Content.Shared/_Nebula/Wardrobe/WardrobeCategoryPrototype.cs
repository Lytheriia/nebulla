// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Prototypes;

namespace Content.Shared._Nebula.Wardrobe;

/// <summary>
/// A tab of the wardrobe with the clothing players can take with them, grouped in sets that share restrictions.
/// </summary>
[Prototype("nebulaWardrobeCategory")]
public sealed partial class WardrobeCategoryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public LocId Name;

    /// <summary>
    /// Position of the tab in the wardrobe, lower goes first.
    /// </summary>
    [DataField]
    public int Order;

    /// <summary>
    /// How many items of this category a profile can select.
    /// </summary>
    [DataField]
    public int MaxLimit = 1;

    [DataField(required: true)]
    public List<WardrobeItemSet> Sets = [];

    private Dictionary<EntProtoId, WardrobeItemSet>? _index;

    /// <summary>
    /// Finds the set an item of this category belongs to.
    /// </summary>
    public bool TryGetSet(EntProtoId item, [NotNullWhen(true)] out WardrobeItemSet? set)
    {
        if (_index == null)
        {
            _index = [];
            foreach (var itemSet in Sets)
            {
                foreach (var id in itemSet.Items)
                {
                    _index.TryAdd(id, itemSet);
                }
            }
        }

        return _index.TryGetValue(item, out set);
    }
}
