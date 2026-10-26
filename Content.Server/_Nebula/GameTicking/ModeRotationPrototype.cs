// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.GameTicking.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._Nebula.GameTicking;

/// <summary>
/// Rules of the automatic game mode vote in the lobby.
/// The first rule matching the player count and the recent round history decides what happens.
/// </summary>
[Prototype]
public sealed partial class ModeRotationPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Groups presets for history matching. A preset outside every category breaks any streak.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<string, List<ProtoId<GamePresetPrototype>>> Categories = [];

    [DataField(required: true)]
    public List<ModeRotationRule> Rules = [];

    /// <summary>
    /// Returns the first rule that applies, or null if none does.
    /// </summary>
    /// <param name="history">Presets of the latest rounds, most recent first.</param>
    public ModeRotationRule? Evaluate(int players, IReadOnlyList<string> history)
    {
        foreach (var rule in Rules)
        {
            if (players < rule.MinPlayers
                || rule.MaxPlayers is { } max && players > max
                || !HistoryMatches(rule.History, history))
                continue;

            return rule;
        }

        return null;
    }

    public string? GetCategory(string preset)
    {
        foreach (var (category, presets) in Categories)
        {
            if (presets.Contains(preset))
                return category;
        }

        return null;
    }

    private bool HistoryMatches(List<string> pattern, IReadOnlyList<string> history)
    {
        if (pattern.Count > history.Count)
            return false;

        for (var i = 0; i < pattern.Count; i++)
        {
            if (GetCategory(history[i]) != pattern[i])
                return false;
        }

        return true;
    }
}

[DataDefinition]
public sealed partial class ModeRotationRule
{
    [DataField]
    public int MinPlayers;

    [DataField]
    public int? MaxPlayers;

    /// <summary>
    /// Categories the latest rounds must have, most recent first.
    /// </summary>
    [DataField]
    public List<string> History = [];

    /// <summary>
    /// Sets this preset without a vote.
    /// </summary>
    [DataField]
    public ProtoId<GamePresetPrototype>? Force;

    [DataField]
    public List<ModeRotationOption> Options = [];
}

/// <summary>
/// One vote option: either a preset, or a named group that opens a second vote between <see cref="Then"/>.
/// </summary>
[DataDefinition]
public sealed partial class ModeRotationOption
{
    [DataField]
    public ProtoId<GamePresetPrototype>? Preset;

    /// <summary>
    /// Option name for a group. Presets use their own name.
    /// </summary>
    [DataField]
    public LocId? Name;

    [DataField]
    public List<ProtoId<GamePresetPrototype>> Then = [];
}
