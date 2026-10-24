// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client._Nebula.Sponsor;
using Content.Client._Nebula.Stylesheets;
using Content.Client._Nebula.UserInterface.Controls;
using Content.Client._Nebula.UserInterface;
using Content.Shared._Nebula.CCVar;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;

namespace Content.Client._Nebula.Lobby.UI;

/// <summary>
/// Boosty button for the lobby side panel, shows the level for subscribers.
/// Opens the page from <see cref="NCCVars.InfoLinksBoosty"/>.
/// </summary>
public sealed partial class NebulaSupportButton : Control
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IEntitySystemManager _systems = default!;
    [Dependency] private IUriOpener _uriOpener = default!;

    private readonly ShapedButton _button;

    public NebulaSupportButton()
    {
        IoCManager.InjectDependencies(this);

        _button = new ShapedButton { HorizontalExpand = true };
        _button.AddStyleClass(NebulaStyleClass.ButtonSupport);
        _button.OnPressed += _ => _uriOpener.OpenUri(_cfg.GetCVar(NCCVars.InfoLinksBoosty));
        AddChild(_button);
    }

    protected override void EnteredTree()
    {
        base.EnteredTree();

        Visible = _cfg.GetCVar(NCCVars.InfoLinksBoosty) != string.Empty;

        var sponsors = _systems.GetEntitySystem<SponsorSystem>();
        sponsors.StatusChanged += Refresh;
        sponsors.RequestStatus();
        Refresh();
    }

    protected override void ExitedTree()
    {
        base.ExitedTree();

        _systems.GetEntitySystem<SponsorSystem>().StatusChanged -= Refresh;
    }

    private void Refresh()
    {
        var rank = _systems.GetEntitySystem<SponsorSystem>().Rank;

        _button.Icon = NebulaIcons.Get(rank != null ? "workspace_premium" : "favorite");
        _button.Text = rank is { } level
            ? Loc.GetString("nebula-lobby-boosty-subscriber", ("level", level))
            : Loc.GetString("nebula-lobby-boosty-support");
    }
}
