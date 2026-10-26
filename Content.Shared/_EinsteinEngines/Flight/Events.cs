// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Goob-Station/Goob-Station/blob/c5be8fd82d71fa28a4556d9bdc40b86a987ed361/Content.Shared/_EinsteinEngines/Flight/Events.cs

using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared._EinsteinEngines.Flight.Events;

/// <summary>
/// Raised on an entity when its flight starts or stops.
/// </summary>
public sealed class FlightEvent(EntityUid uid, bool isFlying, bool isAnimated) : EntityEventArgs
{
    public EntityUid Uid { get; } = uid;
    public bool IsFlying { get; } = isFlying;
    public bool IsAnimated { get; } = isAnimated;
}

[ByRefEvent]
public sealed class FlightAttemptEvent : CancellableEntityEventArgs;

/// <summary>
/// Sent to clients when standing up ends a flight, standing changes are only raised on the server.
/// </summary>
[Serializable, NetSerializable]
public sealed class ToggleFlightVisualsEvent(NetEntity uid, bool isFlying, bool isAnimated) : EntityEventArgs
{
    public NetEntity Uid { get; } = uid;
    public bool IsFlying { get; } = isFlying;
    public bool IsAnimated { get; } = isAnimated;
}

public sealed partial class ToggleFlightEvent : InstantActionEvent;
