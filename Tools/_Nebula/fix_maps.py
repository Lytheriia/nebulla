#!/usr/bin/env python3
"""Fixes map files after an upstream (Trauma) merge without resaving them, so later merges don't conflict.

Two problems are fixed, both with line-level edits only:
  * tiles: a saved tile variant >= the tile prototype's `variants` is drawn as the missing tile texture.
    Out-of-range cells get a variant re-picked by `placementVariants`, like the `variantize` command.
  * doors: airlocks, firelocks, shutters etc. must face along the adjacent walls (StructureAligner). The rotations
    come from the server command `nebula_mapfix_report`; only the `rot:` line of those entities changes.

Usage, from the repo root:
  python3 Tools/_Nebula/fix_maps.py [--check] [--tiles-only | --doors-only] [path ...]
    path          map file or folder under Resources/, default Maps
    --check       report only, exit 1 if anything needs fixing
Doors need a built server: dotnet build Content.Server -c DebugOpt
"""
import argparse
import base64
import collections
import glob
import json
import math
import os
import queue
import random
import re
import struct
import subprocess
import sys
import threading
import time

import yaml

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
RES = os.path.join(ROOT, "Resources")
SERVER = os.path.join(ROOT, "bin", "Content.Server")
REPORT = "nebula_mapfix.json"


class Loader(yaml.SafeLoader):
    pass


Loader.add_multi_constructor("!", lambda l, s, n: l.construct_mapping(n) if isinstance(n, yaml.MappingNode) else None)


# Tiles

def load_tiles():
    tiles, alias = {}, {}
    for f in glob.glob(os.path.join(RES, "Prototypes", "**", "*.yml"), recursive=True):
        try:
            docs = yaml.load(open(f, encoding="utf-8-sig"), Loader=Loader)
        except yaml.YAMLError:
            continue
        for d in docs if isinstance(docs, list) else []:
            if not isinstance(d, dict) or not isinstance(d.get("id"), str):
                continue
            if d.get("type") == "tile":
                tiles[d["id"]] = d
            elif d.get("type") == "tileAlias":
                alias[d["id"]] = d.get("target")
    return tiles, alias


def tile_field(tiles, tid, key, seen=()):
    d = tiles.get(tid)
    if not d or tid in seen:
        return None
    if key in d:
        return d[key]
    parents = d.get("parent")
    for p in parents if isinstance(parents, list) else [parents] if parents else []:
        v = tile_field(tiles, p, key, seen + (tid,))
        if v is not None:
            return v
    return None


def pick_variant(weights, count, rnd):
    if not weights:
        return rnd.randrange(count)
    r = rnd.random() * sum(weights)
    acc = 0.0
    for i, w in enumerate(weights):
        acc += w
        if acc >= r:
            return i
    return 0


CHUNK = re.compile(r"(tiles: )([A-Za-z0-9+/=]+)(\n\s+version: (\d+))")


def fix_tiles(text, path, tiles, alias, fix):
    m = re.search(r"^tilemap:\n((?:  .*\n)+)", text, re.M)
    if not m:
        return text, 0
    tilemap = {int(a): b.strip().strip('"') for a, b in re.findall(r"^\s+(-?\d+): (.+)$", m.group(1), re.M)}
    rnd = random.Random(path)
    count = 0

    def repl(cm):
        nonlocal count
        version = int(cm.group(4))
        raw = bytearray(base64.b64decode(cm.group(2)))
        step = 7 if version >= 7 else 6 if version >= 6 else 4
        var_off = 5 if version >= 6 else 3
        for o in range(0, len(raw) - step + 1, step):
            tid = struct.unpack_from("<i" if version >= 6 else "<H", raw, o)[0]
            name = tilemap.get(tid)
            name = alias.get(name, name)
            if name not in tiles:
                continue
            variants = tile_field(tiles, name, "variants") or 1
            if raw[o + var_off] < variants:
                continue
            count += 1
            if fix:
                raw[o + var_off] = pick_variant(tile_field(tiles, name, "placementVariants"), variants, rnd)
        return cm.group(1) + base64.b64encode(bytes(raw)).decode() + cm.group(3)

    new = CHUNK.sub(repl, text)
    return (new if fix else text), count


# Doors

def run_report(paths):
    """Starts the server, runs nebula_mapfix_report and returns {resource path: {yaml uid: theta}}."""
    if not os.path.exists(os.path.join(SERVER, "Content.Server.dll")):
        sys.exit("no server build: dotnet build Content.Server -c DebugOpt")
    args = ["dotnet", "Content.Server.dll",
            "--cvar", "game.lobbyenabled=true", "--cvar", "game.dummyticker=true",
            "--cvar", "net.port=31298", "--cvar", "status.enabled=false", "--cvar", "hub.advertise=false",
            "--cvar", "database.engine=sqlite", "--cvar", "database.sqlite_dbpath=" + os.path.join(SERVER, "mapfix.db")]
    proc = subprocess.Popen(args, cwd=SERVER, stdin=subprocess.PIPE, stdout=subprocess.PIPE,
                            stderr=subprocess.STDOUT, text=True, bufsize=1)
    lines = queue.Queue()
    threading.Thread(target=lambda: [lines.put(l) for l in proc.stdout], daemon=True).start()

    def wait(pattern, timeout):
        end = time.time() + timeout
        while time.time() < end:
            try:
                line = lines.get(timeout=1)
            except queue.Empty:
                if proc.poll() is not None:
                    sys.exit("server exited")
                continue
            if re.search(r"Unhandled exception|FATL", line):
                proc.kill()
                sys.exit("server failed: " + line.strip())
            if re.search(pattern, line):
                return line
        proc.kill()
        sys.exit(f"timeout waiting for {pattern}")

    try:
        wait(r"-> Ready", 600)
        proc.stdin.write(f"nebula_mapfix_report /{REPORT} " + " ".join("/" + p for p in paths) + "\n")
        proc.stdin.flush()
        print(wait(r"nebula_mapfix|written|записан", 3600).strip())
    finally:
        if proc.poll() is None:
            proc.stdin.write("quit\n")
            proc.stdin.flush()
            try:
                proc.wait(60)
            except subprocess.TimeoutExpired:
                proc.kill()
    return json.load(open(os.path.join(SERVER, "data", REPORT), encoding="utf-8"))


def format_rot(theta):
    theta = math.remainder(theta, 2 * math.pi)
    return None if abs(theta) < 1e-6 else f"{theta!r} rad"


def fix_doors(text, rotations):
    """Sets the Transform rot of the given yaml uids. Returns the new text and how many entities changed."""
    lines = text.split("\n")
    index = {}
    for i, line in enumerate(lines):
        m = re.match(r"^  - uid: (\d+)\s*$", line)
        if m:
            index[int(m.group(1))] = i
    changed = 0
    # Bottom-up, so inserted or removed lines don't shift indices still to be used.
    for uid in sorted((int(u) for u in rotations), key=lambda u: index.get(u, -1), reverse=True):
        start = index.get(uid)
        if start is None:
            continue
        i = start + 1
        while i < len(lines) and not re.match(r"^  - |^- ", lines[i]):
            if re.match(r"^    - type: Transform\s*$", lines[i]):
                break
            i += 1
        else:
            continue
        j = i + 1
        rot_line = None
        insert_at = i + 1
        while j < len(lines) and lines[j].startswith("      "):
            if lines[j].startswith("      rot: "):
                rot_line = j
            elif lines[j].startswith("      parent: "):
                insert_at = j + 1
            j += 1
        value = format_rot(rotations[str(uid)])
        if rot_line is not None and value is None:
            del lines[rot_line]
        elif rot_line is not None:
            lines[rot_line] = "      rot: " + value
        elif value is not None:
            lines.insert(insert_at, "      rot: " + value)
        else:
            continue
        changed += 1
    return "\n".join(lines), changed


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("paths", nargs="*", default=["Maps"])
    ap.add_argument("--check", action="store_true")
    group = ap.add_mutually_exclusive_group()
    group.add_argument("--tiles-only", action="store_true")
    group.add_argument("--doors-only", action="store_true")
    args = ap.parse_args()
    paths = [p.strip("/") for p in args.paths]
    fix = not args.check

    files = set()
    for p in paths:
        full = os.path.join(RES, p)
        if full.endswith(".yml"):
            files.add(full)
        else:
            files.update(glob.glob(os.path.join(full, "**", "*.yml"), recursive=True))
    files = sorted(files)
    report = {} if args.tiles_only else run_report(paths)
    tiles, alias = ({}, {}) if args.doors_only else load_tiles()

    total = collections.Counter()
    for path in files:
        res_path = "/" + os.path.relpath(path, RES).replace(os.sep, "/")
        text = open(path, encoding="utf-8-sig").read()
        new, bad_tiles = (text, 0) if args.doors_only else fix_tiles(text, path, tiles, alias, fix)
        doors = report.get(res_path, {})
        if fix and doors:
            new, _ = fix_doors(new, doors)
        if bad_tiles or doors:
            print(f"{res_path}: tiles {bad_tiles}, doors {len(doors)}")
            total.update(files=1, tiles=bad_tiles, doors=len(doors))
        if fix and new != text:
            with open(path, "w", encoding="utf-8") as f:
                f.write(new)

    verb = "fixed" if fix else "need fixing"
    print(f"{verb}: {total['tiles']} tiles, {total['doors']} doors in {total['files']} files")
    sys.exit(1 if args.check and total["files"] else 0)


if __name__ == "__main__":
    main()
