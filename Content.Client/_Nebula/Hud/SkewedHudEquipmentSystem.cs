// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Overlays;
using Content.Shared._Nebula.Hud;
using Content.Shared.Inventory.Events;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._Nebula.Hud;

/// <summary>
/// Turns the neural interface HUD on while the local player has <see cref="SecurityNeuroComponent"/>
/// and wears equipment with <see cref="SkewedHudComponent"/>.
/// </summary>
public sealed partial class SkewedHudEquipmentSystem : EquipmentHudSystem<SkewedHudComponent>
{
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private SkewedHudSystem _hud = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    /// <summary>
    /// Recomputes <see cref="SkewedHudSystem.Active"/> from scratch, dropping whatever turned it on before.
    /// </summary>
    public void Refresh()
    {
        _hud.Active = false;
        RefreshOverlay();
    }

    [SubscribeLocalEvent]
    private void OnLinkStartup(Entity<NeuroLinkComponent> ent, ref ComponentStartup args)
    {
        if (ent.Owner == _player.LocalEntity)
            Refresh();
    }

    [SubscribeLocalEvent]
    private void OnLinkRemove(Entity<NeuroLinkComponent> ent, ref ComponentRemove args)
    {
        if (ent.Owner == _player.LocalEntity)
            _hud.Active = false;
    }

    protected override void UpdateInternal(RefreshEquipmentHudEvent<SkewedHudComponent> args)
    {
        base.UpdateInternal(args);

        if (!TryComp<NeuroLinkComponent>(_player.LocalEntity, out var link)
            || !_proto.Resolve(link.Profile, out var profile)
            || !args.Components.Exists(c => c.Profiles.Count == 0 || c.Profiles.Contains(link.Profile)))
        {
            _hud.Active = false;
            return;
        }

        _hud.SetProfile(profile);
        _hud.Active = true;
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        _hud.Active = false;
    }
}
