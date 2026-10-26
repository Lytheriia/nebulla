// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Prototypes;

namespace Content.Shared._Nebula.Wardrobe;

public static class WardrobeHelpers
{
    /// <summary>
    /// Finds the category and the set a wardrobe item belongs to.
    /// </summary>
    public static bool TryFindItem(IPrototypeManager prototypes, EntProtoId item,
        [NotNullWhen(true)] out WardrobeCategoryPrototype? category,
        [NotNullWhen(true)] out WardrobeItemSet? set)
    {
        foreach (var proto in prototypes.EnumeratePrototypes<WardrobeCategoryPrototype>())
        {
            if (!proto.TryGetSet(item, out set))
                continue;

            category = proto;
            return true;
        }

        category = null;
        set = null;
        return false;
    }
}
