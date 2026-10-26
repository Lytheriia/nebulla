// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Goob-Station/Goob-Station/blob/c5be8fd82d71fa28a4556d9bdc40b86a987ed361/Content.Shared/Medical/SuitSensors/SuitSensorComponent.cs

namespace Content.Shared.Medical.SuitSensors;

public sealed partial class SuitSensorComponent
{
    /// <summary>
    ///     Is this suit sensor for commands, BSO use only
    /// </summary>
    [DataField]
    public bool CommandTracker = false;
}
