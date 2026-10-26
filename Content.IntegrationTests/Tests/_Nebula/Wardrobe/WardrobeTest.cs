// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server.Database;
using Content.Server.GameTicking;
using Content.Server._Nebula.Wardrobe;
using Content.Shared._Nebula.Wardrobe;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Preferences;
using Content.Shared.Station.Systems;
using Content.Shared.Storage;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Nebula.Wardrobe;

[TestFixture]
[TestOf(typeof(NebulaWardrobeSystem))]
public sealed class WardrobeTest : GameTest
{
    private const string Map = "WardrobeTestMap";
    private const string Bag = "ClothingBackpackDuffel";

    [TestPrototypes]
    private static readonly string Prototypes = $@"
- type: gameMap
  id: {Map}
  mapName: {Map}
  mapPath: /Maps/Test/empty.yml
  minPlayers: 0
  stations:
    Empty:
      stationProto: StandardNanotrasenStation
      components:
        - type: StationNameSetup
          mapNameTemplate: ""Empty""
        - type: StationJobs
          availableJobs:
            Passenger: [ -1, -1 ]
";

    public override PoolSettings PoolSettings => new()
    {
        DummyTicker = false,
        Connected = true,
        InLobby = true,
    };

    /// <summary>
    /// Every item is an existing entity in a single category, and every category has a name and room for an item.
    /// </summary>
    [Test]
    public async Task AllItemsValid()
    {
        var protoMan = Server.ProtoMan;
        var loc = Server.ResolveDependency<ILocalizationManager>();
        var errors = new SortedSet<string>();
        Dictionary<EntProtoId, string> seen = [];

        await Server.WaitAssertion(() =>
        {
            foreach (var category in protoMan.EnumeratePrototypes<WardrobeCategoryPrototype>())
            {
                if (!loc.HasString(category.Name))
                    errors.Add($"{category.ID}: no localized name {category.Name}");

                if (category.MaxLimit < 1)
                    errors.Add($"{category.ID}: maxLimit must be at least 1");

                foreach (var set in category.Sets)
                {
                    if (set.Tier < 0 || set.Tier > WardrobeItemSet.MaxTier)
                        errors.Add($"{category.ID}: tier {set.Tier} is out of range");

                    foreach (var item in set.Items)
                    {
                        if (!protoMan.HasIndex(item))
                            errors.Add($"{category.ID}: unknown entity {item}");

                        if (!seen.TryAdd(item, category.ID))
                            errors.Add($"{category.ID}: {item} is already in {seen[item]}");
                    }
                }
            }
        });

        Assert.That(errors, Is.Empty, string.Join("\n", errors));
    }

    /// <summary>
    /// Validation drops unknown items and items over the category limit, selecting pushes out the oldest item.
    /// </summary>
    [Test]
    public async Task ProfileValidation()
    {
        var protoMan = Server.ProtoMan;
        var session = Server.PlayerMan.Sessions.Single();

        await Server.WaitAssertion(() =>
        {
            var category = protoMan.EnumeratePrototypes<WardrobeCategoryPrototype>()
                .First(c => c.Sets.Sum(s => s.Items.Count) > c.MaxLimit);
            var items = category.Sets.SelectMany(s => s.Items).Take(category.MaxLimit + 1).ToList();

            var profile = new HumanoidCharacterProfile
            {
                Wardrobe = ["NebulaWardrobeTestMissingItem", ..items],
            };
            profile.EnsureValid(session, Server.InstanceDependencyCollection);

            Assert.That(profile.Wardrobe, Is.EqualTo(items.Take(category.MaxLimit)));

            profile = profile.WithWardrobeItem(items[^1], true, protoMan);
            Assert.That(profile.Wardrobe, Is.EqualTo(items.Skip(1)));

            profile = profile.WithWardrobeItem(items[^1], false, protoMan);
            Assert.That(profile.Wardrobe, Is.EqualTo(items.Skip(1).Take(category.MaxLimit - 1)));
        });
    }

    /// <summary>
    /// The wardrobe is saved in the database, and on spawn the allowed items are given in a bag in hand
    /// while sponsor items are skipped for a player without a subscription.
    /// </summary>
    [Test]
    public async Task SpawnGivesBag()
    {
        var protoMan = Server.ProtoMan;
        var categories = protoMan.EnumeratePrototypes<WardrobeCategoryPrototype>().ToList();
        var unrestricted = categories.SelectMany(c => c.Sets)
            .Where(s => s.Departments.Count == 0 && s.Jobs.Count == 0)
            .ToList();

        var free = unrestricted.First(s => s.Tier == 0).Items[0];
        WardrobeHelpers.TryFindItem(protoMan, free, out var freeCategory, out _);
        var sponsor = unrestricted.Where(s => s.Tier > 0)
            .SelectMany(s => s.Items)
            .First(item => WardrobeHelpers.TryFindItem(protoMan, item, out var category, out _) && category != freeCategory);

        await Pair.SetWardrobe([free, sponsor]);

        var user = Pair.Client.User!.Value;
        var db = Server.ResolveDependency<IServerDbManager>();
        var stored = await db.GetPlayerPreferencesAsync(user, default);
        var storedProfile = stored?.Profiles.Single(p => p.Slot == 0);
        Assert.That(storedProfile?.Wardrobe, Does.Contain(free.Id).And.Contain(sponsor.Id));

        Server.CfgMan.SetCVar(CCVars.GameMap, Map);
        var ticker = Server.System<ServerGameTicker>();

        try
        {
            await Server.WaitPost(() => ticker.StartRound());
            await Pair.RunTicksSync(10);

            var session = Server.PlayerMan.GetSessionById(user);
            var station = Server.System<StationSystem>().GetStations().Single();

            await Server.WaitPost(() => ticker.MakeJoinGame(session, station, "Passenger"));
            await Pair.RunTicksSync(10);

            await Server.WaitAssertion(() =>
            {
                Assert.That(ticker.PlayerGameStatuses[user], Is.EqualTo(PlayerGameStatus.JoinedGame));
                var mob = session.AttachedEntity!.Value;

                var bag = Server.System<SharedHandsSystem>().EnumerateHeld(mob)
                    .Single(held => Server.EntMan.GetComponent<MetaDataComponent>(held).EntityPrototype?.ID == Bag);
                var contents = Server.EntMan.GetComponent<StorageComponent>(bag).Container.ContainedEntities
                    .Select(item => Server.EntMan.GetComponent<MetaDataComponent>(item).EntityPrototype?.ID)
                    .ToList();

                Assert.That(contents, Is.EqualTo(new[] { free.Id }));
            });
        }
        finally
        {
            await Server.WaitPost(() => ticker.RestartRound());
        }
    }
}
