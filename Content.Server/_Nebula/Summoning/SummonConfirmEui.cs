// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.EUI;
using Content.Shared._Nebula.Summoning;
using Content.Shared.Eui;
using Robust.Shared.Timing;

namespace Content.Server._Nebula.Summoning;

/// <summary>
/// Yes/no prompt. An accept sent before the delay passes is rejected and the client gets the remaining delay.
/// </summary>
/// <param name="onClosed">Invoked when the prompt closes without an answer, e.g. on disconnect.</param>
public sealed partial class SummonConfirmEui(
    string title,
    string text,
    TimeSpan delay,
    Action onAccept,
    Action? onDeny,
    Action? onClosed = null) : BaseEui
{
    [Dependency] private IGameTiming _timing = default!;

    private TimeSpan _openedAt;
    private bool _answered;

    public override void Opened()
    {
        base.Opened();

        IoCManager.InjectDependencies(this);
        _openedAt = _timing.CurTime;
        StateDirty();
    }

    public override EuiStateBase GetNewState()
    {
        var remaining = _openedAt + delay - _timing.CurTime;
        return new SummonConfirmEuiState(title, text, remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not SummonConfirmChoiceMessage choice)
            return;

        if (choice.Accepted && _timing.CurTime < _openedAt + delay)
        {
            StateDirty();
            return;
        }

        _answered = true;
        Close();

        if (choice.Accepted)
            onAccept();
        else
            onDeny?.Invoke();
    }

    public override void Closed()
    {
        base.Closed();

        if (_answered)
            return;

        _answered = true;
        onClosed?.Invoke();
    }
}
