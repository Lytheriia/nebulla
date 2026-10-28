// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Radio;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared._Nebula.Hud;

/// <summary>
/// A kind of neural interface: how its HUD looks, which channel links it to its servers and who gets it.
/// What the HUD shows besides the location, the code and the owner's state comes from the module of the profile.
/// </summary>
[Prototype]
public sealed partial class NeuroProfilePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Line over the HUD, gets the shift time as <c>$time</c>.
    /// </summary>
    [DataField(readOnly: true, required: true)]
    public LocId Header;

    /// <summary>
    /// Short name of the network, shown under the signal loss banner.
    /// </summary>
    [DataField(readOnly: true, required: true)]
    public string Tag = string.Empty;

    [DataField(readOnly: true)]
    public Color Accent = Color.FromHex("#3fd8ff");

    [DataField(readOnly: true)]
    public Color AccentDim = Color.FromHex("#3fd8ff").WithAlpha(0.65f);

    /// <summary>
    /// Urgent lines and the glitch ghosts.
    /// </summary>
    [DataField(readOnly: true)]
    public Color Alert = Color.FromHex("#ff4040");

    /// <summary>
    /// A radio jammer that does not exclude this channel cuts the interface off its servers.
    /// </summary>
    [DataField(readOnly: true, required: true)]
    public ProtoId<RadioChannelPrototype> Channel;

    /// <summary>
    /// The servers are on the station: away from every map that holds a station grid, like on the CentComm map
    /// or an expedition, the link is lost as under a jammer. Off for interfaces with their own servers.
    /// </summary>
    [DataField(readOnly: true)]
    public bool StationOnly;

    /// <summary>
    /// The nearest beacon of the current grid in the common block of the HUD.
    /// </summary>
    [DataField(readOnly: true)]
    public bool ShowLocation = true;

    /// <summary>
    /// The station alert level in the common block of the HUD, useless to those who are not crew.
    /// </summary>
    [DataField(readOnly: true)]
    public bool ShowCode = true;

    /// <summary>
    /// Jobs that spawn with the interface. Antagonists get <see cref="NeuroLinkComponent"/> from their prototypes.
    /// </summary>
    [DataField(readOnly: true)]
    public HashSet<ProtoId<JobPrototype>> Jobs = [];

    /// <summary>
    /// Every job of these departments spawns with the interface.
    /// </summary>
    [DataField(readOnly: true)]
    public HashSet<ProtoId<DepartmentPrototype>> Departments = [];
}
