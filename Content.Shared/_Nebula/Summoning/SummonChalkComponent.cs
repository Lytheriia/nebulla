// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.GameTicking.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._Nebula.Summoning;

/// <summary>
/// Single-use chalk that draws a summoning rune on a floor tile.
/// </summary>
[RegisterComponent]
public sealed partial class SummonChalkComponent : Component
{
    [DataField(readOnly: true)]
    public EntProtoId Rune = "SummonRune";

    [DataField(readOnly: true)]
    public TimeSpan DrawDelay = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Presets in which the chalk works. Secret is resolved to the preset it picked.
    /// </summary>
    [DataField(readOnly: true, serverOnly: true)]
    public HashSet<ProtoId<GamePresetPrototype>> AllowedPresets = ["Extended", "Greenshift", "Sandbox"];
}
