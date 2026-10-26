// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Actions;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Mindshield;
using Content.Shared.Popups;

namespace Content.Shared._Nebula.Security.SecDispatch;

public abstract partial class SharedSecDispatchSystem : EntitySystem
{
    [Dependency] protected SharedPopupSystem Popup = default!;
    [Dependency] private ActionContainerSystem _actionContainer = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private MindShieldSystem _mindShield = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    private const string MaskSlot = "mask";

    #region Component events

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<SecDispatchComponent> ent, ref MapInitEvent args)
    {
        _actionContainer.EnsureAction(ent.Owner, ref ent.Comp.ActionEntity, ent.Comp.Action);
        Dirty(ent);
    }

    [SubscribeLocalEvent]
    private void OnGetItemActions(Entity<SecDispatchComponent> ent, ref GetItemActionsEvent args)
    {
        if (args.SlotFlags is not { } flags
            || (flags & SlotFlags.MASK) == 0)
            return;

        args.AddAction(ent.Comp.ActionEntity);
    }

    [SubscribeLocalEvent]
    private void OnUnequipped(Entity<SecDispatchComponent> ent, ref GotUnequippedEvent args)
    {
        _ui.CloseUi(ent.Owner, SecDispatchUiKey.Key, args.EquipTarget);
    }

    [SubscribeLocalEvent]
    private void OnDispatchAction(Entity<SecDispatchComponent> ent, ref SecDispatchActionEvent args)
    {
        if (args.Handled)
            return;

        if (!HasMindShield(args.Performer))
        {
            Popup.PopupEntity(Loc.GetString("sec-dispatch-no-mindshield-popup"),
                args.Performer, args.Performer, PopupType.SmallCaution);
            return;
        }

        args.Handled = true;
        _ui.TryToggleUi(ent.Owner, SecDispatchUiKey.Key, args.Performer);
    }

    #endregion
    #region Checks

    protected bool IsWornBy(EntityUid mask, EntityUid user)
    {
        return _inventory.TryGetSlotEntity(user, MaskSlot, out var worn) && worn == mask;
    }

    /// <summary>
    /// The shield itself sits on an implant or clothing, the wearer only gets <c>MindShieldStatusComponent</c>.
    /// Fake and broken shields don't count.
    /// </summary>
    protected bool HasMindShield(EntityUid uid)
    {
        _mindShield.GetMindshieldStatus(uid, out var mindshielded, out _, out var broken);
        return mindshielded && !broken;
    }

    #endregion
}
