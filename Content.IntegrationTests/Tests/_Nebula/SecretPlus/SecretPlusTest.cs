// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server._Nebula.StationEvents.SecretPlus;
using Content.Shared._Nebula.StationEvents.SecretPlus;
using Content.Shared.Antag.Components;
using Content.Shared.EntityTable;
using Content.Shared.GameTicking.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Nebula;

/// <summary>
/// Every rule SecretPlus can run needs a chaos score, from its <see cref="SecretPlusRulePrototype"/> or its antags.
/// </summary>
[TestFixture]
[TestOf(typeof(SecretPlusSystem))]
public sealed class SecretPlusTest
{
    [Test]
    public async Task RulesHaveChaosScores()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var protoMan = server.ProtoMan;
        var compFactory = server.ResolveDependency<IComponentFactory>();
        var entTable = server.System<EntityTableSystem>();

        await server.WaitAssertion(() =>
        {
            List<string> errors = [];
            HashSet<EntProtoId> checkedRules = [];

            foreach (var proto in protoMan.EnumeratePrototypes<EntityPrototype>())
            {
                if (proto.Abstract || !proto.TryComp<SecretPlusComponent>(out var secretPlus, compFactory))
                    continue;

                var rules = protoMan.Index(secretPlus.RoundStartAntagsWeightTable).Weights.Keys
                    .Concat(protoMan.Index(secretPlus.PrimaryAntagsWeightTable).Weights.Keys)
                    .Select(id => new EntProtoId(id))
                    .Concat(entTable.GetSpawns(secretPlus.ScheduledGameRules));

                foreach (var rule in rules)
                {
                    if (!checkedRules.Add(rule))
                        continue;

                    if (!protoMan.TryIndex(rule, out var ruleProto)
                        || !ruleProto.TryComp<GameRuleComponent>(out _, compFactory))
                    {
                        errors.Add($"{proto.ID} uses {rule}, which is not a game rule");
                        continue;
                    }

                    var hasAntagChaos = ruleProto.TryComp<AntagSelectionComponent>(out var selection, compFactory)
                        && selection.Antags.Any(antag => protoMan.HasIndex<SecretPlusAntagPrototype>(antag.Proto.Id));
                    var hasRuleChaos = protoMan.TryIndex<SecretPlusRulePrototype>(rule.Id, out var data)
                        && data.Chaos != null;

                    if (!hasAntagChaos && !hasRuleChaos)
                        errors.Add($"{rule} is used by {proto.ID} but has no chaos score, add a secretPlusRule or secretPlusAntag");
                }
            }

            Assert.That(errors, Is.Empty, "\n" + string.Join("\n", errors));
        });

        await pair.CleanReturnAsync();
    }
}
