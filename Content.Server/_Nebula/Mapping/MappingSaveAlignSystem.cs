// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Mapping;
using Robust.Shared.Map.Events;

namespace Content.Server._Nebula.Mapping;

/// <summary>
/// Runs the structure aligner on pre-init maps right before they are saved, so a mapper can't save doors facing the
/// wrong way. Covers every save path: the mapping menu, <c>savemap</c>, <c>savegrid</c> and autosave.
/// </summary>
public sealed partial class MappingSaveAlignSystem : EntitySystem
{
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedStructureAlignerSystem _aligner = default!;

    [SubscribeLocalEvent]
    private void OnBeforeSerialization(BeforeSerializationEvent ev)
    {
        foreach (var mapId in ev.MapIds)
        {
            if (_map.IsInitialized(mapId))
                continue;

            _aligner.AlignAll(mapId);
        }
    }
}
