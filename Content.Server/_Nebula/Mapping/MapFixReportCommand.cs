// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Text.Json;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Mapping;
using Robust.Shared.Console;
using Robust.Shared.ContentPack;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Components;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Server._Nebula.Mapping;

/// <summary>
/// Loads every map file under a resource folder pre-init, runs the structure aligner and writes which yaml uids
/// need which rotation. <c>Tools/_Nebula/fix_maps.py</c> applies that to the files line by line, so upstream maps
/// get fixed without a full resave that would conflict with every later merge.
/// </summary>
[AdminCommand(AdminFlags.Mapping)]
public sealed partial class MapFixReportCommand : LocalizedEntityCommands
{
    [Dependency] private IResourceManager _res = default!;
    [Dependency] private MapLoaderSystem _loader = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedStructureAlignerSystem _aligner = default!;

    public override string Command => "nebula_mapfix_report";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number"));
            return;
        }

        var output = new ResPath(args[0]).ToRootedPath();
        Dictionary<string, Dictionary<int, double>> report = [];
        var failed = 0;

        foreach (var path in FindMaps(args.Skip(1)))
        {
            if (!TryReport(path, out var rotations))
            {
                failed++;
                continue;
            }

            if (rotations.Count > 0)
                report[path.ToString()] = rotations;
        }

        using (var writer = _res.UserData.OpenWriteText(output))
        {
            writer.Write(JsonSerializer.Serialize(report));
        }

        shell.WriteLine(Loc.GetString("cmd-nebula_mapfix_report-done",
            ("files", report.Count),
            ("entities", report.Values.Sum(r => r.Count)),
            ("failed", failed),
            ("path", output.ToString())));
    }

    /// <summary>
    /// Each argument is a map file or a folder searched recursively.
    /// </summary>
    private IEnumerable<ResPath> FindMaps(IEnumerable<string> args)
    {
        SortedSet<string> found = [];
        foreach (var arg in args)
        {
            var path = new ResPath(arg).ToRootedPath();
            if (path.Extension == "yml")
            {
                found.Add(path.ToString());
                continue;
            }

            foreach (var file in _res.ContentFindFiles(path))
            {
                if (file.Extension == "yml")
                    found.Add(file.ToString());
            }
        }

        return found.Select(p => new ResPath(p));
    }

    private bool TryReport(ResPath path, out Dictionary<int, double> rotations)
    {
        rotations = [];
        var mapUid = _map.CreateMap(out var mapId, runMapInit: false);
        var options = new MapLoadOptions
        {
            MergeMap = mapId,
            DeserializationOptions = DeserializationOptions.Default with { StoreYamlUids = true },
        };

        try
        {
            if (!_loader.TryLoadGeneric(path, out var result, options))
                return false;

            var maps = result.Maps.Select(m => m.Comp.MapId).Append(mapId).ToHashSet();
            Dictionary<EntityUid, Angle> before = [];
            var query = EntityManager.AllEntityQueryEnumerator<StructureAlignerComponent, YamlUidComponent, TransformComponent>();
            while (query.MoveNext(out var uid, out _, out _, out var xform))
            {
                if (maps.Contains(xform.MapID))
                    before[uid] = xform.LocalRotation;
            }

            foreach (var map in maps)
            {
                _aligner.AlignAll(map);
            }

            foreach (var (uid, angle) in before)
            {
                var now = EntityManager.GetComponent<TransformComponent>(uid).LocalRotation;
                if (!now.EqualsApprox(angle))
                    rotations[EntityManager.GetComponent<YamlUidComponent>(uid).Uid] = now.Theta;
            }

            foreach (var map in result.Maps)
            {
                EntityManager.DeleteEntity(map);
            }

            return true;
        }
        finally
        {
            EntityManager.DeleteEntity(mapUid);
        }
    }
}
