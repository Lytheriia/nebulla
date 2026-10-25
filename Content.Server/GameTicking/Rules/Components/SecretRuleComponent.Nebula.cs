// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.GameTicking.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.GameTicking.Rules.Components;

public sealed partial class SecretRuleComponent
{
    /// <summary>
    /// The preset secret picked for this round.
    /// </summary>
    [ViewVariables(VVAccess.ReadOnly)]
    public ProtoId<GamePresetPrototype>? SelectedPreset;
}
