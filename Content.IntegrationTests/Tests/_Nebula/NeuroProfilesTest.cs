// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._Nebula.Hud;
using Content.Shared.Radio;
using Robust.Shared.Localization;

namespace Content.IntegrationTests.Tests._Nebula;

/// <summary>
/// Checks that every neural interface profile has its header and a real radio channel to lose.
/// </summary>
[TestFixture]
[TestOf(typeof(NeuroProfilePrototype))]
public sealed class NeuroProfilesTest
{
    [Test]
    public async Task ProfilesAreValid()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var protoMan = server.ProtoMan;
        var loc = server.ResolveDependency<ILocalizationManager>();

        await server.WaitAssertion(() =>
        {
            List<string> errors = [];
            foreach (var profile in protoMan.EnumeratePrototypes<NeuroProfilePrototype>())
            {
                if (!loc.HasString(profile.Header))
                    errors.Add($"Profile '{profile.ID}': missing localization '{profile.Header}'.");

                if (!protoMan.HasIndex<RadioChannelPrototype>(profile.Channel))
                    errors.Add($"Profile '{profile.ID}': unknown radio channel '{profile.Channel}'.");
            }

            Assert.That(errors, Is.Empty, "\n" + string.Join("\n", errors));
        });

        await pair.CleanReturnAsync();
    }
}
