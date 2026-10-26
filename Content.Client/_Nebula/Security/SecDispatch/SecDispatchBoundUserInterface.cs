// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Nebula.UserInterface.Controls;
using Content.Client.UserInterface.Controls;
using Content.Shared._Nebula.Security.SecDispatch;
using Robust.Client.UserInterface;

namespace Content.Client._Nebula.Security.SecDispatch;

public sealed partial class SecDispatchBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private NebulaRadialMenu? _menu;

    protected override void Open()
    {
        base.Open();

        if (!EntMan.TryGetComponent<SecDispatchComponent>(Owner, out var comp))
            return;

        _menu = this.CreateWindow<NebulaRadialMenu>();
        _menu.SetButtons(GetOptions(comp));
        _menu.OpenOverMouseScreenPosition();
    }

    private IEnumerable<RadialMenuOptionBase> GetOptions(SecDispatchComponent comp)
    {
        var count = comp.Entries.Count;
        var placeholder = Loc.GetString("sec-dispatch-location-placeholder");
        var list = new List<RadialMenuOptionBase>(count);

        for (var i = 0; i < count; i++)
        {
            var entry = comp.Entries[i];
            var name = Loc.GetString(entry.Name);
            var message = Loc.GetString(entry.Message, ("location", placeholder));

            var option = new RadialMenuActionOption<int>(SendDispatch, i)
            {
                IconSpecifier = RadialMenuIconSpecifier.With(entry.Icon),
                ToolTip = $"{name}\n{message}",
                Order = ((count / 4 - 1 - i) % count + count) % count,
            };

            if (entry.Combat)
            {
                option.BackgroundColor = NebulaRadialMenu.DangerColor;
                option.HoverBackgroundColor = NebulaRadialMenu.DangerHoverColor;
            }

            list.Add(option);
        }

        return list;
    }

    private void SendDispatch(int index)
    {
        SendMessage(new SecDispatchMessage(index));
    }
}
