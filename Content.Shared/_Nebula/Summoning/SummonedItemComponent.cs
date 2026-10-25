// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared._Nebula.Summoning;

/// <summary>
/// Item that was on a summoned entity when it was summoned and goes back with it.
/// </summary>
[RegisterComponent]
public sealed partial class SummonedItemComponent : Component
{
    [ViewVariables]
    public EntityUid Summoned;
}
