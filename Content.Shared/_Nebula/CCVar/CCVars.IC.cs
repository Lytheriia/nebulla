// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Configuration;

namespace Content.Shared._Nebula.CCVar;

public sealed partial class NCCVars
{

    /// <summary>
    ///      Sets the maximum Custom Species length.
    /// </summary>
    public static readonly CVarDef<int> MaxCustomSpeciesLength =
        CVarDef.Create("ic.custom_species_length", 16, CVar.SERVER | CVar.REPLICATED);
}
