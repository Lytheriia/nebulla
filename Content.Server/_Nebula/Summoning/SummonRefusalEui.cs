// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Server.EUI;
using Content.Shared._Nebula.Summoning;
using Content.Shared.Eui;

namespace Content.Server._Nebula.Summoning;

/// <summary>
/// Lets a player who refused a summon send a reason to the summoner.
/// </summary>
public sealed class SummonRefusalEui(Action<string> onSend) : BaseEui
{
    private bool _sent;

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not SummonRefusalMessage refusal)
            return;

        Send(refusal.Text);
        Close();
    }

    public override void Closed()
    {
        base.Closed();
        Send(string.Empty);
    }

    private void Send(string text)
    {
        if (_sent)
            return;

        _sent = true;
        onSend(text);
    }
}
