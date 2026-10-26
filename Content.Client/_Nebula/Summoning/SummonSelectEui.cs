// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Eui;
using Content.Shared._Nebula.Summoning;
using Content.Shared.Eui;
using JetBrains.Annotations;

namespace Content.Client._Nebula.Summoning;

[UsedImplicitly]
public sealed class SummonSelectEui : BaseEui
{
    private readonly SummonSelectWindow _window = new();
    private bool _answered;

    public SummonSelectEui()
    {
        _window.OnSelect += target =>
        {
            _answered = true;
            SendMessage(new SummonSelectChoiceMessage(target));
            _window.Close();
        };
        _window.OnClose += () =>
        {
            if (_answered)
                return;

            _answered = true;
            SendMessage(new SummonSelectCancelMessage());
        };
    }

    public override void HandleState(EuiStateBase state)
    {
        if (state is SummonSelectEuiState select)
            _window.SetCandidates(select.Candidates);
    }

    public override void Opened()
    {
        _window.OpenCentered();
    }

    public override void Closed()
    {
        _answered = true;
        _window.Close();
    }
}
