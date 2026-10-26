// SPDX-License-Identifier: AGPL-3.0-or-later
// Ported from https://github.com/Goob-Station/Goob-Station/blob/c5be8fd82d71fa28a4556d9bdc40b86a987ed361/Content.Goobstation.Server/CrewMonitoring/CrewMonitorScanningSystem.cs

using Content.Goobstation.Server.RelayedDeathrattle;
using Content.Goobstation.Shared.CrewMonitoring;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Implants;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Server.CrewMonitoring;

public sealed partial class CrewMonitorScanningSystem : EntitySystem
{
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private EntityWhitelistSystem _whitelist = default!;
    [Dependency] private SharedSubdermalImplantSystem _implant = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private IPrototypeManager _proto = default!;

    [SubscribeLocalEvent]
    private void OnScanAttempt(Entity<CrewMonitorScanningComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target is not { } target
            || !args.CanReach
            || !HasComp<HumanoidProfileComponent>(target))
            return;

        _popup.PopupEntity(Loc.GetString("injector-component-needle-injecting-user"), target, args.User);
        if (args.User != target)
        {
            var userName = Identity.Entity(args.User, EntityManager);
            _popup.PopupEntity(Loc.GetString("implanter-component-implanting-target", ("user", userName)), args.User, target, PopupType.LargeCaution);
        }

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, ent.Comp.DoAfterTime, new CrewMonitorScanningDoAfterEvent(), ent, target, ent)
        {
            NeedHand = true,
            BreakOnMove = true,
        });
    }

    [SubscribeLocalEvent]
    private void OnScanComplete(Entity<CrewMonitorScanningComponent> ent, ref CrewMonitorScanningDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        var name = Identity.Name(target, EntityManager, args.User);
        var implantName = _proto.Index(ent.Comp.Implant).Name;

        if (ent.Comp.ScannedEntities.Contains(target))
        {
            _popup.PopupEntity(Loc.GetString("implanter-component-implant-already", ("implant", implantName), ("target", name)), target, args.User);
            return;
        }

        if (_whitelist.IsWhitelistFail(ent.Comp.Whitelist, target))
        {
            _popup.PopupEntity(Loc.GetString("implanter-component-implant-failed", ("implant", implantName), ("target", name)), target, args.User);
            return;
        }

        ent.Comp.ScannedEntities.Add(target);
        _implant.AddImplant(target, ent.Comp.Implant);

        if (ent.Comp.ApplyDeathrattle)
            EnsureComp<RelayedDeathrattleComponent>(target).Target = ent;

        args.Handled = true;
    }
}
