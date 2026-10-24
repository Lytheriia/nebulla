// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json;
using Content.Server.Database;
using Content.Shared._Nebula.Roles;
using Content.Shared.Roles;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server.Preferences.Managers;

public sealed partial class ServerPreferencesManager
{
    /// <summary>
    /// Raised when a player selects another character or saves a profile.
    /// </summary>
    public event Action<NetUserId>? PreferencesChanged;

    /// <summary>
    /// Reads the alternate job titles stored as a JSON object, broken or empty data gives no titles.
    /// </summary>
    private static Dictionary<ProtoId<JobPrototype>, ProtoId<JobAlternateTitlePrototype>> ParseJobAltTitles(Profile profile)
    {
        var titles = new Dictionary<ProtoId<JobPrototype>, ProtoId<JobAlternateTitlePrototype>>();
        if (string.IsNullOrEmpty(profile.JobAlternateTitles))
            return titles;

        Dictionary<string, string>? raw;
        try
        {
            raw = JsonSerializer.Deserialize<Dictionary<string, string>>(profile.JobAlternateTitles);
        }
        catch (JsonException)
        {
            return titles;
        }

        if (raw == null)
            return titles;

        foreach (var (job, title) in raw)
        {
            titles[job] = title;
        }

        return titles;
    }
}
