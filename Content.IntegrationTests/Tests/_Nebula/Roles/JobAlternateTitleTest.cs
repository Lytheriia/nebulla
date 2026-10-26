// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

#nullable enable
using System.Collections.Generic;
using System.Linq;
using Content.IntegrationTests.Fixtures;
using Content.Server.Administration.Logs;
using Content.Server.GameTicking;
using Content.Server.Mind;
using Content.Shared._Nebula.Roles;
using Content.Shared.Access.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.Roles;
using Content.Shared.Roles.Jobs;
using Content.Shared.Station.Systems;
using Content.Shared.StationRecords;
using Content.Shared.StationRecords.Components;
using Content.Shared.StationRecords.Systems;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.IntegrationTests.Tests._Nebula.Roles;

[TestFixture]
[TestOf(typeof(JobAlternateTitleSystem))]
public sealed class JobAlternateTitleTest : GameTest
{
    private const string Map = "JobAlternateTitleTestMap";

    private static readonly Gender[] Genders = [Gender.Male, Gender.Female, Gender.Epicene, Gender.Neuter];

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
        AdminLogsEnabled = true,
    };

    /// <summary>
    /// Every title points at a selectable job, has a short unique name for every gender and is cached by the system.
    /// </summary>
    [Test]
    public async Task AllTitlesValid()
    {
        var protoMan = Server.ProtoMan;
        var loc = Server.ResolveDependency<ILocalizationManager>();
        var maxLength = Server.CfgMan.GetCVar(CCVars.MaxIdJobLength);
        var system = Server.System<JobAlternateTitleSystem>();
        var errors = new SortedSet<string>();
        var namesByJob = new Dictionary<(string Job, Gender Gender), Dictionary<string, string>>();

        await Server.WaitAssertion(() =>
        {
            foreach (var title in protoMan.EnumeratePrototypes<JobAlternateTitlePrototype>())
            {
                if (!protoMan.TryIndex(title.Job, out var job))
                {
                    errors.Add($"{title.ID}: job {title.Job} does not exist");
                    continue;
                }

                if (!job.SetPreference)
                    errors.Add($"{title.ID}: job {job.ID} can't be picked in the character editor");

                if (!system.GetTitles(job.ID).Contains(title))
                    errors.Add($"{title.ID}: missing from the cached titles of {job.ID}");

                foreach (var gender in Genders)
                {
                    if (!loc.TryGetString(title.Name, out var name, ("gender", gender)) || string.IsNullOrWhiteSpace(name))
                    {
                        errors.Add($"{title.ID}: no localized name for {gender}");
                        continue;
                    }

                    if (name.Length > maxLength)
                        errors.Add($"{title.ID}: '{name}' is longer than the ID card limit of {maxLength}");

                    if (name == job.LocalizedName)
                        errors.Add($"{title.ID}: '{name}' repeats the job name");

                    if (!namesByJob.TryGetValue((job.ID, gender), out var names))
                        namesByJob[(job.ID, gender)] = names = new Dictionary<string, string>();

                    if (names.TryGetValue(name, out var other))
                        errors.Add($"{title.ID}: '{name}' duplicates {other} for {gender}");
                    else
                        names[name] = title.ID;
                }
            }
        });

        Assert.That(errors, Is.Empty, string.Join('\n', errors));
    }

    /// <summary>
    /// A player joining late as a random job with a random alternate title gets it on the ID card,
    /// in the station record and in the arrival announcement.
    /// </summary>
    [Test]
    public async Task LateJoinRandomTitle()
    {
        var protoMan = Server.ProtoMan;
        var random = TestContext.CurrentContext.Random;

        var titles = protoMan.EnumeratePrototypes<JobAlternateTitlePrototype>()
            .Where(title => protoMan.Index(title.Job).JobEntity == null)
            .OrderBy(title => title.ID)
            .ToList();
        Assert.That(titles, Is.Not.Empty);

        var title = titles[random.Next(titles.Count)];
        var gender = random.NextBool() ? Gender.Male : Gender.Female;
        TestContext.Out.WriteLine($"Joining as {title.Job} with title {title.ID} and gender {gender}");

        Server.CfgMan.SetCVar(CCVars.GameMap, Map);
        await Pair.SetJobAltTitle(title.Job, title.ID, gender);

        var ticker = Server.System<ServerGameTicker>();
        Assert.That(ticker.RunLevel, Is.EqualTo(GameRunLevel.PreRoundLobby));

        try
        {
            await Server.WaitPost(() => ticker.StartRound());
            await Pair.RunTicksSync(10);

            var user = Pair.Client.User!.Value;
            var session = Server.PlayerMan.GetSessionById(user);
            var station = Server.System<StationSystem>().GetStations().Single();

            await Server.WaitPost(() => ticker.MakeJoinGame(session, station, title.Job));
            await Pair.RunTicksSync(10);

            var expected = string.Empty;
            string? cardTitle = null;
            string? recordTitle = null;
            ProtoId<JobPrototype>? jobId = null;

            await Server.WaitPost(() =>
            {
                expected = title.LocalizedName(gender);

                if (session.AttachedEntity is not { } mob)
                    return;

                var mind = Server.System<MindSystem>().GetMind(mob);
                if (Server.System<SharedJobSystem>().MindTryGetJobId(mind, out var job))
                    jobId = job;

                if (!Server.System<SharedIdCardSystem>().TryFindIdCard(mob, out var card))
                    return;

                cardTitle = card.Comp.LocalizedJobTitle;

                if (Server.EntMan.TryGetComponent(card, out StationRecordKeyStorageComponent? keyStorage)
                    && keyStorage.Key is { } key
                    && Server.System<StationRecordsSystem>().TryGetRecord<GeneralStationRecord>(key, out var record))
                    recordTitle = record.JobTitle;
            });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ticker.PlayerGameStatuses[user], Is.EqualTo(PlayerGameStatus.JoinedGame));
                Assert.That(jobId, Is.EqualTo(title.Job), $"{title.ID}: spawned with the wrong job");
                Assert.That(cardTitle, Is.EqualTo(expected), $"{title.ID}: wrong job title on the ID card");
                Assert.That(recordTitle, Is.EqualTo(expected), $"{title.ID}: wrong job title in the station record");
            }

            var adminLogs = Server.ResolveDependency<IAdminLogManager>();
            await PoolManager.WaitUntil(Server, async () =>
            {
                var logs = await adminLogs.CurrentRoundLogs(new LogFilter
                {
                    Types = new HashSet<LogType> { LogType.Chat },
                    Search = expected,
                });

                return logs.Any(log => log.Message.Contains("Station Announcement"));
            });
        }
        finally
        {
            await Server.WaitPost(() => ticker.RestartRound());
        }
    }
}
