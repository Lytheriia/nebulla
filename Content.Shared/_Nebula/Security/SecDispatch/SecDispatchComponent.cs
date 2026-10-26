// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Radio;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Nebula.Security.SecDispatch;

/// <summary>
/// Mask with a built-in transmitter: grants an action that opens a radial menu of preset radio reports.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SecDispatchComponent : Component
{
    [DataField(readOnly: true)]
    public EntProtoId Action = "ActionSecDispatch";

    [DataField, AutoNetworkedField]
    public EntityUid? ActionEntity;

    [DataField(readOnly: true)]
    public ProtoId<RadioChannelPrototype> Channel = "Security";

    /// <summary>
    /// Cooldown put on the action after a report is sent.
    /// </summary>
    [DataField(readOnly: true)]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Played to every player whose worn headset received a combat report from this mask.
    /// </summary>
    [DataField(readOnly: true)]
    public SoundSpecifier? CombatSound;

    /// <summary>
    /// Reports in menu order: clockwise starting from 9 o'clock, so with eight entries the first four fill the top half.
    /// </summary>
    [DataField(readOnly: true, required: true)]
    public List<SecDispatchEntry> Entries = [];
}
