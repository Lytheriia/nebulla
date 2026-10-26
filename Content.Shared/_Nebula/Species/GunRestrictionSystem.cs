// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._Nebula.Species;

public sealed partial class GunRestrictionSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;

    [SubscribeLocalEvent]
    private void OnShotAttempted(Entity<GunRestrictionComponent> ent, ref ShotAttemptedEvent args)
    {
        if (!_whitelist.IsWhitelistPass(ent.Comp.Whitelist, args.Used))
            return;

        args.Cancel();

        if (ent.Comp.Messages.Count == 0 || _timing.CurTime < ent.Comp.NextPopup)
            return;

        ent.Comp.NextPopup = _timing.CurTime + ent.Comp.PopupCooldown;
        var random = SharedRandomExtensions.PredictedRandom(_timing, GetNetEntity(ent));
        _popup.PopupEntity(Loc.GetString(random.Pick(ent.Comp.Messages)), ent, ent);
    }
}
