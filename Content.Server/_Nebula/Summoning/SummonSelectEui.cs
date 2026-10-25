// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.EUI;
using Content.Shared._Nebula.Summoning;
using Content.Shared.Eui;

namespace Content.Server._Nebula.Summoning;

public sealed class SummonSelectEui(List<SummonCandidate> candidates, Action<NetEntity> onSelect) : BaseEui
{
    public override void Opened()
    {
        base.Opened();
        StateDirty();
    }

    public override EuiStateBase GetNewState()
    {
        return new SummonSelectEuiState(candidates);
    }

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is SummonSelectCancelMessage)
        {
            Close();
            return;
        }

        if (msg is not SummonSelectChoiceMessage choice)
            return;

        Close();

        if (candidates.Exists(c => c.Entity == choice.Target))
            onSelect(choice.Target);
    }
}
