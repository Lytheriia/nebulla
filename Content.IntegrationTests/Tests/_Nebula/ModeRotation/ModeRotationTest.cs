// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server._Nebula.GameTicking;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Nebula.ModeRotation;

/// <summary>
/// Checks the mode rotation prototypes and the decisions of the default rotation.
/// </summary>
[TestFixture]
[TestOf(typeof(ModeRotationPrototype))]
public sealed class ModeRotationTest
{
    private const string DefaultRotation = "Default";

    private static readonly (int Players, string[] History, string Expected)[] Cases =
    [
        (12, [], "SecretPlusLow, Greenshift"),
        (12, ["Greenshift", "Greenshift"], "SecretPlusLow, Greenshift"),
        (12, ["SecretPlusLow", "SecretPlusLow"], "force Greenshift"),
        (19, ["SecretPlusLow", "SecretPlusLow"], "force Greenshift"),
        (20, ["SecretPlusLow", "SecretPlusLow"], "Greenshift, SecretPlusLow"),
        (25, [], "(SecretPlusMid | SecretPlusHigh), Greenshift"),
        (25, ["SecretPlusMid"], "(SecretPlusMid | SecretPlusLow), Greenshift"),
        (25, ["SecretPlusMid", "Nukeops"], "(SecretPlusMid | SecretPlusLow), Greenshift"),
        (25, ["Nukeops", "SecretPlusMid"], "(SecretPlusMid | SecretPlusHigh), Greenshift"),
        (25, ["SecretPlusMid", "SecretPlusHigh"], "Greenshift, SecretPlusLow"),
        (25, ["Greenshift", "Greenshift"], "SecretPlusMid, SecretPlusLow"),
        (25, ["Greenshift", "SecretPlusMid"], "(SecretPlusMid | SecretPlusHigh), Greenshift"),
        (25, ["Greenshift"], "(SecretPlusMid | SecretPlusHigh), Greenshift"),
    ];

    [Test]
    public async Task RotationPrototypesAreValid()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var protoMan = server.ProtoMan;
        var loc = server.ResolveDependency<ILocalizationManager>();

        await server.WaitAssertion(() =>
        {
            List<string> errors = [];

            foreach (var rotation in protoMan.EnumeratePrototypes<ModeRotationPrototype>())
            {
                HashSet<string> categorized = [];
                foreach (var preset in rotation.Categories.Values.SelectMany(p => p))
                {
                    if (!protoMan.HasIndex(preset))
                        errors.Add($"{rotation.ID}: category preset {preset} does not exist");

                    if (!categorized.Add(preset))
                        errors.Add($"{rotation.ID}: preset {preset} is in more than one category");
                }

                for (var i = 0; i < rotation.Rules.Count; i++)
                {
                    var rule = rotation.Rules[i];
                    foreach (var category in rule.History)
                    {
                        if (!rotation.Categories.ContainsKey(category))
                            errors.Add($"{rotation.ID} rule {i}: unknown category {category}");
                    }

                    if (rule.Force is { } force && !protoMan.HasIndex(force))
                        errors.Add($"{rotation.ID} rule {i}: forced preset {force} does not exist");

                    if (rule.Force == null && rule.Options.Count < 2)
                        errors.Add($"{rotation.ID} rule {i}: needs a forced preset or at least two options");

                    if (rule.Force != null && rule.Options.Count > 0)
                        errors.Add($"{rotation.ID} rule {i}: a forced preset ignores the options");

                    foreach (var option in rule.Options)
                    {
                        if ((option.Preset == null) == (option.Name == null))
                            errors.Add($"{rotation.ID} rule {i}: an option needs either a preset or a name with presets to pick from");

                        if (option.Name is { } name && !loc.HasString(name))
                            errors.Add($"{rotation.ID} rule {i}: missing localization {name}");

                        if (option.Preset != null && option.Then.Count > 0)
                            errors.Add($"{rotation.ID} rule {i}: preset {option.Preset} ignores its then list");

                        if (option.Name != null && option.Then.Count < 2)
                            errors.Add($"{rotation.ID} rule {i}: group {option.Name} needs at least two presets");

                        if (option.Preset is { } preset && !protoMan.HasIndex(preset))
                            errors.Add($"{rotation.ID} rule {i}: preset {preset} does not exist");

                        foreach (var then in option.Then)
                        {
                            if (!protoMan.HasIndex(then))
                                errors.Add($"{rotation.ID} rule {i}: preset {then} does not exist");
                        }
                    }
                }
            }

            Assert.That(errors, Is.Empty, "\n" + string.Join("\n", errors));
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task DefaultRotationDecisions()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var rotation = server.ProtoMan.Index<ModeRotationPrototype>(DefaultRotation);

        List<string> errors = [];
        foreach (var (players, history, expected) in Cases)
        {
            var actual = Describe(rotation.Evaluate(players, history));
            if (actual != expected)
                errors.Add($"{players} players, history [{string.Join(", ", history)}]: expected \"{expected}\", got \"{actual}\"");
        }

        Assert.That(errors, Is.Empty, "\n" + string.Join("\n", errors));

        await pair.CleanReturnAsync();
    }

    private static string Describe(ModeRotationRule rule)
    {
        if (rule == null)
            return "no rule";

        if (rule.Force is { } force)
            return $"force {force}";

        return string.Join(", ", rule.Options.Select(option => option.Preset is { } preset
            ? preset.Id
            : $"({string.Join(" | ", option.Then)})"));
    }
}
