// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Numerics;
using Content.Server._Nebula.Squads;
using Content.Server.CriminalRecords.Systems;
using Content.Server.Station.Systems;
using Content.Shared._Nebula.Hud;
using Content.Shared._Nebula.Squads;
using Content.Shared.CriminalRecords;
using Content.Shared.Mobs;
using Content.Shared.Radio.Components;
using Content.Shared.Security;
using Content.Shared.Station.Components;
using Content.Shared.StationRecords;
using Content.Shared.StationRecords.Components;
using Content.Shared.StationRecords.Systems;
using Robust.Shared.Map;

namespace Content.IntegrationTests.Tests._Nebula;

/// <summary>
/// Checks the squad model: limits, moving members between squads, removed records, disbanding, orders and tasks,
/// that every member's neural interface gets the rest of the squad and that a radio jammer or leaving the station freezes everything but the personal tasks.
/// </summary>
[TestFixture]
[TestOf(typeof(SecuritySquadSystem))]
public sealed class SecuritySquadsTest
{
    [Test]
    public async Task SquadMembership()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.EntMan;
        var squads = entMan.System<SecuritySquadSystem>();
        var records = entMan.System<StationRecordsSystem>();
        var stations = entMan.System<ServerStationSystem>();
        var map = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            List<string> errors = [];

            var station = entMan.Spawn();
            entMan.AddComponent<StationDataComponent>(station);
            entMan.AddComponent<StationRecordsComponent>(station);
            stations.AddGridToStation(station, map.Grid);

            var alpha = records.AddRecordEntry(station, new GeneralStationRecord { Name = "Alpha" });
            var bravo = records.AddRecordEntry(station, new GeneralStationRecord { Name = "Bravo" });
            var charlie = records.AddRecordEntry(station, new GeneralStationRecord { Name = "Charlie" });

            var alphaMob = SpawnMember(alpha);
            var bravoMob = SpawnMember(bravo);

            if (!squads.TryCreateSquad(station, "Red", Color.Red, out var red))
            {
                Assert.Fail("Could not create the first squad.");
                return;
            }

            if (squads.TryCreateSquad(station, " Red ", Color.Blue, out _))
                errors.Add("A squad with a taken name was created.");

            if (squads.TryCreateSquad(station, "Other", Color.Red, out _))
                errors.Add("A squad with a taken color was created.");

            squads.TrySetSquad(alpha, red.Id);
            squads.TrySetSquad(bravo, red.Id);
            squads.TrySetSquad(charlie, red.Id);
            Expect(alphaMob, "Bravo", "Charlie");
            Expect(bravoMob, "Alpha", "Charlie");
            if (alphaMob.Squad.Find(m => m.Name == "Charlie").State != MobState.Dead)
                errors.Add("A member without a body should be shown dead.");

            squads.TryCreateSquad(station, "Blue", Color.Blue, out var blue);
            squads.TrySetSquad(bravo, blue!.Id);
            Expect(alphaMob, "Charlie");
            Expect(bravoMob);

            records.RemoveRecord(charlie);
            Expect(alphaMob);
            if (squads.TryGetSquad(charlie, out _))
                errors.Add("A removed record stayed in its squad.");

            for (var i = 0; i < SquadLimits.MaxOrdersPerSquad + 1; i++)
            {
                if (!squads.TryPlaceOrder(station, blue.Id, "Hold", map.GridCoords, out _))
                    errors.Add("Could not place an order.");
            }

            if (bravoMob.Marks.Count != SquadLimits.MaxOrdersPerSquad)
                errors.Add($"Expected {SquadLimits.MaxOrdersPerSquad} marks, got {bravoMob.Marks.Count}.");

            if (alphaMob.Marks.Count != 0)
                errors.Add("A member of another squad got the marks.");

            if (squads.TryPlaceOrder(station, blue.Id, "Hold", new EntityCoordinates(map.MapUid, default), out _))
                errors.Add("An order off a grid was placed.");

            if (squads.TryPlaceOrder(station, blue.Id, "Hold", new EntityCoordinates(map.Grid, new Vector2(500f, 500f)), out _))
                errors.Add("An order in the space around the station was placed.");

            squads.TryCancelOrder(station, bravoMob.Marks[0].Id, out _);
            if (bravoMob.Marks.Count != SquadLimits.MaxOrdersPerSquad - 1)
                errors.Add("A cancelled order is still shown.");

            squads.TrySetSquad(alpha, blue.Id);
            Expect(bravoMob, "Alpha");
            if (alphaMob.Marks.Count != bravoMob.Marks.Count)
                errors.Add("A new member did not get the squad's marks.");

            squads.DeleteSquad(station, blue.Id);
            Expect(alphaMob);
            Expect(bravoMob);
            if (alphaMob.Marks.Count != 0)
                errors.Add("A disbanded squad left its marks.");
            if (squads.TryGetSquad(alpha, out _))
                errors.Add("A member of a disbanded squad is still in a squad.");

            var created = 1;
            for (var i = 0; i < SquadLimits.MaxSquads + 1; i++)
            {
                if (squads.TryCreateSquad(station, $"Squad {i}", new Color(0.1f * i, 0.5f, 0.5f), out _))
                    created++;
            }

            if (created != SquadLimits.MaxSquads)
                errors.Add($"Expected {SquadLimits.MaxSquads} squads at most, got {created}.");

            Assert.That(errors, Is.Empty, "\n" + string.Join("\n", errors));

            SecurityNeuroComponent SpawnMember(StationRecordKey key)
            {
                var mob = entMan.SpawnEntity("MobHuman", map.GridCoords);
                var neuro = entMan.EnsureComponent<SecurityNeuroComponent>(mob);
                neuro.Record = key;
                return neuro;
            }

            void Expect(SecurityNeuroComponent neuro, params string[] names)
            {
                var actual = neuro.Squad.ConvertAll(m => m.Name);
                if (!actual.SequenceEqual(names))
                    errors.Add($"Expected squad [{string.Join(", ", names)}], got [{string.Join(", ", actual)}].");
            }
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task SquadTasks()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.EntMan;
        var squads = entMan.System<SecuritySquadSystem>();
        var records = entMan.System<StationRecordsSystem>();
        var criminal = entMan.System<CriminalRecordsSystem>();
        var map = await pair.CreateTestMap();

        await server.WaitAssertion(() =>
        {
            List<string> errors = [];

            var station = entMan.Spawn();
            entMan.AddComponent<StationDataComponent>(station);
            entMan.AddComponent<StationRecordsComponent>(station);

            var alpha = AddRecord("Alpha", "SecurityOfficer");
            var bravo = AddRecord("Bravo", "SecurityOfficer");
            var charlie = AddRecord("Charlie", "Passenger");
            var chief = AddRecord("Chief", "HeadOfSecurity");

            var alphaMob = SpawnMember(alpha);
            var bravoMob = SpawnMember(bravo);
            var chiefMob = SpawnMember(chief);
            chiefMob.Commander = true;

            squads.TryCreateSquad(station, "Red", Color.Red, out var red);
            squads.TrySetSquad(alpha, red!.Id);
            squads.TrySetSquad(bravo, red.Id);

            if (!squads.TryCreateTask(station, red.Id, null, "Patrol", "The whole brig", out var shared))
                errors.Add("Could not create a squad task.");

            if (!squads.TryCreateTask(station, null, alpha, "Guard", "", out _))
                errors.Add("Could not create a personal task.");

            if (squads.TryCreateTask(station, null, null, "Nobody", "", out _))
                errors.Add("A task without a receiver was created.");

            if (squads.TryCreateTask(station, red.Id, alpha, "Both", "", out _))
                errors.Add("A task with two receivers was created.");

            if (squads.TryCreateTask(station, null, charlie, "Passenger", "", out _))
                errors.Add("A task for a non-security record was created.");

            if (squads.TryCreateTask(station, red.Id, null, new string('a', SquadLimits.MaxTitleLength + 1), "", out _))
                errors.Add("A task with a too long title was created.");

            ExpectTasks(alphaMob, SecurityNeuroTaskKind.Order, 2);
            ExpectTasks(bravoMob, SecurityNeuroTaskKind.Order, 1);
            if (!alphaMob.Tasks.TrueForAll(t => t.Pinned))
                errors.Add("New tasks should be pinned.");

            records.AddRecordEntry(charlie, new CriminalRecord());
            criminal.TryChangeStatus(charlie, SecurityStatus.Wanted, "theft");
            ExpectTasks(alphaMob, SecurityNeuroTaskKind.Wanted, 1);
            ExpectTasks(chiefMob, SecurityNeuroTaskKind.Wanted, 1);

            if (chiefMob.Summary.Count != 1 || chiefMob.Summary[0].Total != 2)
                errors.Add($"Expected one squad of two in the commander summary, got {chiefMob.Summary.Count}.");

            if (alphaMob.Summary.Count != 0)
                errors.Add("An officer got the commander summary.");

            squads.TryDeleteTask(station, shared!.Id, out _);
            ExpectTasks(bravoMob, SecurityNeuroTaskKind.Order, 0);

            squads.DeleteSquad(station, red.Id);
            ExpectTasks(alphaMob, SecurityNeuroTaskKind.Order, 1);

            Assert.That(errors, Is.Empty, "\n" + string.Join("\n", errors));

            StationRecordKey AddRecord(string name, string job)
            {
                return records.AddRecordEntry(station, new GeneralStationRecord { Name = name, JobPrototype = job });
            }

            SecurityNeuroComponent SpawnMember(StationRecordKey key)
            {
                var mob = entMan.SpawnEntity("MobHuman", map.GridCoords);
                var neuro = entMan.EnsureComponent<SecurityNeuroComponent>(mob);
                neuro.Record = key;
                return neuro;
            }

            void ExpectTasks(SecurityNeuroComponent neuro, SecurityNeuroTaskKind kind, int count)
            {
                var actual = neuro.Tasks.Count(t => t.Key.Kind == kind);
                if (actual != count)
                    errors.Add($"Expected {count} {kind} tasks, got {actual}.");
            }
        });

        await pair.CleanReturnAsync();
    }

    [Test]
    public async Task SignalLoss()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.EntMan;
        var squads = entMan.System<SecuritySquadSystem>();
        var records = entMan.System<StationRecordsSystem>();
        var stations = entMan.System<ServerStationSystem>();
        var transform = entMan.System<SharedTransformSystem>();
        var map = await pair.CreateTestMap();
        var away = await pair.CreateTestMap();

        List<string> errors = [];
        EntityUid alphaMob = default;
        EntityUid jammer = default;
        EntityUid station = default;
        StationRecordKey charlie = default;
        var squadId = 0;

        await server.WaitAssertion(() =>
        {
            station = entMan.Spawn();
            entMan.AddComponent<StationDataComponent>(station);
            entMan.AddComponent<StationRecordsComponent>(station);
            stations.AddGridToStation(station, map.Grid);

            var alpha = records.AddRecordEntry(station, new GeneralStationRecord { Name = "Alpha" });
            var bravo = records.AddRecordEntry(station, new GeneralStationRecord { Name = "Bravo" });
            charlie = records.AddRecordEntry(station, new GeneralStationRecord { Name = "Charlie" });

            alphaMob = entMan.SpawnEntity("MobHuman", map.GridCoords);
            entMan.EnsureComponent<NeuroLinkComponent>(alphaMob).Profile = "Security";
            entMan.EnsureComponent<SecurityNeuroComponent>(alphaMob).Record = alpha;

            squads.TryCreateSquad(station, "Red", Color.Red, out var red);
            squadId = red!.Id;
            squads.TrySetSquad(alpha, squadId);
            squads.TrySetSquad(bravo, squadId);

            jammer = entMan.SpawnEntity("RadioJammer", map.GridCoords);
            entMan.EnsureComponent<ActiveRadioJammerComponent>(jammer);
        });

        await pair.RunSeconds(2);

        await server.WaitAssertion(() =>
        {
            if (!entMan.HasComponent<NeuroSignalLostComponent>(alphaMob))
                errors.Add("The interface next to an active jammer kept its signal.");

            squads.TrySetSquad(charlie, squadId);
            Expect("Bravo");

            squads.TryCreateTask(station, squadId, null, "Patrol", "", out _);
            if (entMan.GetComponent<SecurityNeuroComponent>(alphaMob).Tasks.Count != 0)
                errors.Add("A task reached the interface without a signal.");

            entMan.DeleteEntity(jammer);
        });

        await pair.RunSeconds(1);

        await server.WaitAssertion(() =>
        {
            if (entMan.HasComponent<NeuroSignalLostComponent>(alphaMob))
                errors.Add("The signal did not come back after the jammer was gone.");

            Expect("Bravo", "Charlie");
            if (entMan.GetComponent<SecurityNeuroComponent>(alphaMob).Tasks.Count != 1)
                errors.Add("The task given without a signal did not arrive once it was back.");

            transform.SetCoordinates(alphaMob, away.GridCoords);
        });

        await pair.RunSeconds(2);

        await server.WaitAssertion(() =>
        {
            if (!entMan.HasComponent<NeuroSignalLostComponent>(alphaMob))
                errors.Add("A station interface kept its signal on a map without a station.");

            Assert.That(errors, Is.Empty, "\n" + string.Join("\n", errors));
        });

        await pair.CleanReturnAsync();

        void Expect(params string[] names)
        {
            var actual = entMan.GetComponent<SecurityNeuroComponent>(alphaMob).Squad.ConvertAll(m => m.Name);
            if (!actual.SequenceEqual(names))
                errors.Add($"Expected squad [{string.Join(", ", names)}], got [{string.Join(", ", actual)}].");
        }
    }
}
