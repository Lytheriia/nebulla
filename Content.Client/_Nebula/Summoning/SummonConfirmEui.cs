// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Eui;
using Content.Shared._Nebula.Summoning;
using Content.Shared.Eui;
using JetBrains.Annotations;
using Robust.Client.Graphics;

namespace Content.Client._Nebula.Summoning;

[UsedImplicitly]
public sealed class SummonConfirmEui : BaseEui
{
    private readonly SummonConfirmWindow _window = new();
    private bool _answered;

    public SummonConfirmEui()
    {
        _window.OnChoice += Answer;
        _window.OnClose += () => Answer(false);
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is not SummonConfirmEuiState confirm)
            return;

        _window.SetContent(confirm.Title, confirm.Text, confirm.Delay);
    }

    public override void Opened()
    {
        IoCManager.Resolve<IClyde>().RequestWindowAttention();
        _window.OpenCentered();
    }

    public override void Closed()
    {
        _answered = true;
        _window.Close();
    }

    private void Answer(bool accepted)
    {
        if (_answered)
            return;

        SendMessage(new SummonConfirmChoiceMessage(accepted));

        if (accepted)
        {
            _window.DisableButtons();
            return;
        }

        _answered = true;
        _window.Close();
    }
}
