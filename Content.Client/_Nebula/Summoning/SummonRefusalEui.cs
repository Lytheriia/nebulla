// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Eui;
using Content.Shared._Nebula.Summoning;
using JetBrains.Annotations;

namespace Content.Client._Nebula.Summoning;

[UsedImplicitly]
public sealed class SummonRefusalEui : BaseEui
{
    private readonly SummonRefusalWindow _window = new();
    private bool _sent;

    public SummonRefusalEui()
    {
        _window.OnSend += Send;
        _window.OnClose += () => Send(string.Empty);
    }

    public override void Opened()
    {
        _window.OpenCentered();
        _window.FocusInput();
    }

    public override void Closed()
    {
        _sent = true;
        _window.Close();
    }

    private void Send(string text)
    {
        if (_sent)
            return;

        _sent = true;
        SendMessage(new SummonRefusalMessage(text));
        _window.Close();
    }
}
