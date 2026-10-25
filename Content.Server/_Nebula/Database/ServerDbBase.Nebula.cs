// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Text.Json;
using Content.Shared.Preferences;

namespace Content.Server.Database;

public abstract partial class ServerDbBase
{
    /// <summary>
    /// Stores the alternate job titles as a JSON object of job id to title id.
    /// </summary>
    private static string SerializeJobAltTitles(HumanoidCharacterProfile humanoid)
    {
        var raw = new Dictionary<string, string>(humanoid.JobAlternateTitles.Count);
        foreach (var (job, title) in humanoid.JobAlternateTitles)
        {
            raw[job.Id] = title.Id;
        }

        return JsonSerializer.Serialize(raw);
    }

    /// <summary>
    /// Stores the wardrobe as a JSON array of entity prototype ids.
    /// </summary>
    private static string SerializeWardrobe(HumanoidCharacterProfile humanoid)
    {
        return JsonSerializer.Serialize(humanoid.Wardrobe.ConvertAll(item => item.Id));
    }
}
