// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Nebula.Summoning;

[Serializable, NetSerializable]
public sealed class SummonConfirmEuiState(string title, string text, TimeSpan delay) : EuiStateBase
{
    public string Title { get; } = title;
    public string Text { get; } = text;

    /// <summary>
    /// How long the buttons stay disabled after the window opens.
    /// </summary>
    public TimeSpan Delay { get; } = delay;
}

[Serializable, NetSerializable]
public sealed class SummonConfirmChoiceMessage(bool accepted) : EuiMessageBase
{
    public bool Accepted { get; } = accepted;
}

[Serializable, NetSerializable]
public struct SummonCandidate
{
    public required NetEntity Entity;
    public required string Label;
}

[Serializable, NetSerializable]
public sealed class SummonSelectEuiState(List<SummonCandidate> candidates) : EuiStateBase
{
    public List<SummonCandidate> Candidates { get; } = candidates;
}

[Serializable, NetSerializable]
public sealed class SummonSelectChoiceMessage(NetEntity target) : EuiMessageBase
{
    public NetEntity Target { get; } = target;
}

[Serializable, NetSerializable]
public sealed class SummonSelectCancelMessage : EuiMessageBase;

[Serializable, NetSerializable]
public sealed class SummonRefusalMessage(string text) : EuiMessageBase
{
    public string Text { get; } = text;
}
