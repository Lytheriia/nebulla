// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared._Nebula.Summoning;

[RegisterComponent]
public sealed partial class SummonRuneComponent : Component
{
    [DataField(readOnly: true)]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(40);

    /// <summary>
    /// Cooldown counted from the summon attempt when the target refuses.
    /// </summary>
    [DataField(readOnly: true)]
    public TimeSpan RefusalCooldown = TimeSpan.FromSeconds(20);

    [DataField(readOnly: true)]
    public float LeashRange = 7f;

    [ViewVariables]
    public TimeSpan LastAttempt;

    [ViewVariables]
    public TimeSpan NextUse;

    [ViewVariables]
    public EntityUid? Summoned;
}
