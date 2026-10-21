#!/usr/bin/env python3

# Import future so people on py2 still get the clear error that they need to upgrade.
from __future__ import print_function
import os
import subprocess
import sys

from pathlib import Path

ROOT = Path(__file__).resolve().parent
CLIENT = "Content.Trauma.Client"
SERVER = "Content.Trauma.Server"
CONFIGS = ["Debug", "Release", "Tools"]
WINDOWS = os.name == "nt"

if WINDOWS:
    os.system("")

RESET = "\033[0m"
TITLE = "\033[1;38;5;183m"
ACCENT = "\033[1;38;5;141m"
TEXT = "\033[38;5;189m"
DIM = "\033[38;5;97m"
FAIL = "\033[1;38;5;204m"


def getch() -> str:
    if WINDOWS:
        import msvcrt
        ch = msvcrt.getwch()
        if ch in ("\x00", "\xe0"):
            msvcrt.getwch()
            return ""
        return ch

    import termios
    import tty
    fd = sys.stdin.fileno()
    old = termios.tcgetattr(fd)
    try:
        tty.setraw(fd)
        return sys.stdin.read(1)
    finally:
        termios.tcsetattr(fd, termios.TCSADRAIN, old)


def run(cmd, cwd=ROOT) -> bool:
    print("{}$ {}{}".format(DIM, " ".join(cmd), RESET))
    sys.stdout.flush()
    try:
        code = subprocess.run(cmd, cwd=str(cwd)).returncode
    except KeyboardInterrupt:
        return False
    except FileNotFoundError:
        print("{}{} not found in PATH.{}".format(FAIL, cmd[0], RESET))
        return False
    if code != 0:
        print("{}Command exited with code {}.{}".format(FAIL, code, RESET))
    return code == 0


def setup() -> bool:
    return run([sys.executable, "git_helper.py"], cwd=ROOT / "BuildChecker")


def update_repo() -> bool:
    if not run(["git", "pull", "--ff-only"]):
        print("{}Pull failed: commit/stash local changes or rebase manually.{}".format(FAIL, RESET))
        return False
    return setup()


def build(config) -> bool:
    return run(["dotnet", "build", "-c", config])


def launch(projects, config, rebuild) -> bool:
    if rebuild and not build(config):
        return False

    procs = []
    for project in projects:
        cmd = ["dotnet", "run", "--no-build", "--project", project, "-c", config]
        print("{}$ {}{}".format(DIM, " ".join(cmd), RESET))
        flags = subprocess.CREATE_NEW_CONSOLE if WINDOWS and len(projects) > 1 and project == SERVER else 0
        procs.append(subprocess.Popen(cmd, cwd=str(ROOT), creationflags=flags))

    try:
        for proc in procs:
            proc.wait()
    except KeyboardInterrupt:
        for proc in procs:
            proc.terminate()
        for proc in procs:
            proc.wait()
    return all(proc.returncode == 0 for proc in procs)


def draw(config, rebuild):
    configs = "  ".join(
        "{}[{}]{}".format(ACCENT, c, RESET) if c == config else "{} {} {}".format(DIM, c, RESET)
        for c in CONFIGS)
    mode = "{}with build{}".format(ACCENT, RESET) if rebuild else "{}no build{}".format(DIM, RESET)

    def item(key, label):
        return "  {}{:>3}{}  {}{}{}".format(ACCENT, key, RESET, TEXT, label, RESET)

    lines = [
        "\033[2J\033[H",
        "{}  ✦ ─────────────── N E B U L A ─────────────── ✦{}".format(TITLE, RESET),
        "",
        "  {}config{}   {}".format(TEXT, RESET, configs),
        "  {}mode{}     {}".format(TEXT, RESET, mode),
        "",
        "{}  ── run ─────────────────────────────────────────{}".format(DIM, RESET),
        item("1", "Client"),
        item("2", "Server"),
        item("3", "Server + client"),
        item("4", "Build only"),
        "",
        "{}  ── settings ────────────────────────────────────{}".format(DIM, RESET),
        item("c", "Switch config"),
        item("b", "Toggle build before run"),
        "",
        "{}  ── update ──────────────────────────────────────{}".format(DIM, RESET),
        item("u", "Update repo (git pull + engine)"),
        item("e", "Update engine (pinned version)"),
        "",
        item("q", "Quit"),
        "",
    ]
    print("\n".join(lines))


def tui():
    config = "Debug"
    rebuild = True
    actions = {
        "1": lambda: launch([CLIENT], config, rebuild),
        "2": lambda: launch([SERVER], config, rebuild),
        "3": lambda: launch([SERVER, CLIENT], config, rebuild),
        "4": lambda: build(config),
        "u": update_repo,
        "e": setup,
    }

    while True:
        draw(config, rebuild)
        key = getch()
        if key in ("q", "Q", "\x1b", "\x03", "\x04"):
            print("\033[2J\033[H", end="")
            return
        if key == "c":
            config = CONFIGS[(CONFIGS.index(config) + 1) % len(CONFIGS)]
        elif key == "b":
            rebuild = not rebuild
        elif key in actions:
            print("\033[2J\033[H", end="")
            ok = actions[key]()
            status = "{}✦ done{}".format(ACCENT, RESET) if ok else "{}✘ failed{}".format(FAIL, RESET)
            print("\n{}  {}press any key...{}".format(status, DIM, RESET))
            sys.stdout.flush()
            getch()


if __name__ == "__main__":
    if sys.stdin.isatty() and sys.stdout.isatty():
        tui()
    else:
        sys.exit(0 if setup() else 1)
