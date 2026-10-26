// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.DoAfter;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._Nebula.Summoning;

[Serializable, NetSerializable]
public sealed partial class SummonDrawDoAfterEvent : DoAfterEvent
{
    [DataField(required: true)]
    public NetCoordinates Coordinates;

    private SummonDrawDoAfterEvent()
    {
    }

    public SummonDrawDoAfterEvent(NetCoordinates coordinates)
    {
        Coordinates = coordinates;
    }

    public override DoAfterEvent Clone() => this;
}
