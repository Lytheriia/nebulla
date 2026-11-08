// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/ArcaneSS14/arcane-new/blob/dcb9510767a539afab95d2f0c854c9932e63a0d3/Content.Shared/CCVar/CCVars.Misc.cs

using Content.Shared._Arcane.Mapping;
using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    ///     If the cvar is enabled, every <see cref="StructureAlignerComponent"/> will be Aligned when the map initializes.
    /// </summary>
    /// <remarks>
    ///     May be considered a stopgap measure when unupgraded maps are in rotation?
    /// </remarks>
    public static readonly CVarDef<bool> MapInitAlign =
        CVarDef.Create("align.map_init", true, CVar.SERVER | CVar.REPLICATED);
}
