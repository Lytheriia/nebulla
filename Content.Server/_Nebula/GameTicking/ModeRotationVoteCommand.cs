// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.Administration;
using Content.Shared.Administration;
using Robust.Shared.Console;

namespace Content.Server._Nebula.GameTicking;

[AdminCommand(AdminFlags.Host)]
public sealed partial class ModeRotationVoteCommand : LocalizedEntityCommands
{
    [Dependency] private ModeRotationSystem _rotation = default!;

    public override string Command => "rotationvote";

    public override void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (!_rotation.RestartVotes())
        {
            shell.WriteError(Loc.GetString("cmd-rotationvote-not-in-lobby"));
            return;
        }

        shell.WriteLine(Loc.GetString("cmd-rotationvote-success"));
    }
}
