// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Server._Nebula.Summoning;

/// <summary>
/// Grid that has had a summoning rune on it. Lets summoned entities be sent back before the grid's children are deleted.
/// </summary>
[RegisterComponent]
public sealed partial class SummonRuneGridComponent : Component;
