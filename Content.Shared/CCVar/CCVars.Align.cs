// SPDX-License-Identifier: MIT
// Ported from https://github.com/space-wizards/space-station-14/blob/2561c7c9fee699d44cf6d314026f34258ec00bb6/Content.Shared/CCVar/CCVars.Misc.cs

using Content.Shared.Mapping;
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
        CVarDef.Create("align.map_init", false, CVar.SERVER | CVar.REPLICATED);
}
