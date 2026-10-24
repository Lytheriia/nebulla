// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Nebula.Roles;
using Content.Shared.Access.Systems;
using Content.Shared.GameTicking;
using Content.Shared.StationRecords;
using Content.Shared.StationRecords.Components;
using Content.Shared.StationRecords.Systems;

namespace Content.Server._Nebula.Roles;

/// <summary>
/// Puts the alternate job title from the profile on the spawned player's ID card and station record.
/// </summary>
public sealed partial class JobAlternateTitleSpawnSystem : EntitySystem
{
    [Dependency] private JobAlternateTitleSystem _titles = default!;
    [Dependency] private SharedIdCardSystem _idCard = default!;
    [Dependency] private StationRecordsSystem _records = default!;

    [Dependency] private EntityQuery<StationRecordKeyStorageComponent> _keyStorageQuery = default!;

    [SubscribeLocalEvent(after: [typeof(StationRecordsSystem)])]
    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        if (string.IsNullOrEmpty(args.JobId)
            || !_titles.TryGetTitle(args.Profile, args.JobId, out var title)
            || !_idCard.TryFindIdCard(args.Mob, out var card))
            return;

        var name = title.LocalizedName(args.Profile.Gender);
        _idCard.TryChangeJobTitle(card, name, card.Comp);

        if (!_keyStorageQuery.TryComp(card, out var keyStorage)
            || keyStorage.Key is not { } key
            || !_records.TryGetRecord<GeneralStationRecord>(key, out var record))
            return;

        record.JobTitle = name;
        _records.Synchronize(key);
    }
}
