// SPDX-License-Identifier: AGPL-3.0-or-later

// Ported from Goob-Station: https://github.com/Goob-Station/Goob-Station

using Content.Shared.Antag;
using Robust.Shared.Prototypes;

namespace Content.Shared._Nebula.StationEvents.SecretPlus;

/// <summary>
/// Category of a station event, lets a SecretPlus preset forbid whole groups of events.
/// </summary>
[Prototype]
public sealed partial class EventTypePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;
}

/// <summary>
/// SecretPlus data for one game rule. The ID is the game rule entity prototype ID.
/// Kept out of the rule components so upstream rule prototypes stay untouched.
/// </summary>
[Prototype]
public sealed partial class SecretPlusRulePrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// How much the chaos score changes when this rule runs.
    /// Ignored for rules whose antags have a <see cref="SecretPlusAntagPrototype"/>.
    /// </summary>
    [DataField]
    public float? Chaos;

    [DataField]
    public ProtoId<EventTypePrototype> EventType = "Neutral";

    /// <summary>
    /// Stops picking more roundstart antags after this one, like zombies.
    /// </summary>
    [DataField]
    public bool LoneRun;
}

/// <summary>
/// Chaos score per spawned antag. The ID is the <see cref="AntagSpecifierPrototype"/> ID.
/// </summary>
[Prototype]
public sealed partial class SecretPlusAntagPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public float Chaos;
}
