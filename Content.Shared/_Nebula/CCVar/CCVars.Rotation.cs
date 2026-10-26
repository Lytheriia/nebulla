// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Configuration;

namespace Content.Shared._Nebula.CCVar;

public sealed partial class NCCVars
{
    /// <summary>
    /// Seconds after round start before SecretPlus fires its first midround event.
    /// </summary>
    public static readonly CVarDef<float> SecretPlusFirstEventDelay =
        CVarDef.Create("nebula.secretplus.first_event_delay", 300f, CVar.SERVERONLY);

    public static readonly CVarDef<float> SecretPlusRoundstartChaosMultiplier =
        CVarDef.Create("nebula.secretplus.roundstart_chaos_multiplier", 1f, CVar.SERVERONLY);

    /// <summary>
    /// Automatic game mode and map votes in the lobby. Off by default so tests and dev servers keep the plain lobby.
    /// </summary>
    public static readonly CVarDef<bool> RotationEnabled =
        CVarDef.Create("nebula.rotation.enabled", false, CVar.SERVERONLY);

    public static readonly CVarDef<string> RotationPrototype =
        CVarDef.Create("nebula.rotation.prototype", "Default", CVar.SERVERONLY);

    /// <summary>
    /// Seconds in the lobby before the votes start.
    /// </summary>
    public static readonly CVarDef<int> RotationVoteDelay =
        CVarDef.Create("nebula.rotation.vote_delay", 30, CVar.SERVERONLY);

    /// <summary>
    /// Duration of each game mode vote stage, in seconds.
    /// </summary>
    public static readonly CVarDef<int> RotationStageDuration =
        CVarDef.Create("nebula.rotation.stage_duration", 30, CVar.SERVERONLY);

    /// <summary>
    /// Pause the lobby countdown while a game mode vote is still running.
    /// </summary>
    public static readonly CVarDef<bool> RotationHoldLobby =
        CVarDef.Create("nebula.rotation.hold_lobby", true, CVar.SERVERONLY);
}
