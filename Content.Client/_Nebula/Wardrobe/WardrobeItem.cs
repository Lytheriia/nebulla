// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Client._Nebula.Stylesheets;
using Content.Client._Nebula.UserInterface;
using Content.Client._Nebula.UserInterface.Controls;
using Content.Client.Guidebook.Controls;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;

namespace Content.Client._Nebula.Wardrobe;

/// <summary>
/// A single item in the wardrobe, drawn as a toggleable list row with the item sprite and its tier.
/// </summary>
public sealed class WardrobeItem : ShapedButton, ISearchableControl
{
    private const float LockedAlpha = 0.45f;

    private readonly string _name;
    private readonly string? _description;
    private readonly int _tier;
    private readonly TextureRect? _tierIcon;

    public readonly EntProtoId Item;

    public event Action<WardrobeItem, bool>? Toggled;

    public WardrobeItem(EntityPrototype entity, int tier, SpriteSystem sprite)
    {
        AddStyleClass(NebulaStyleClass.ButtonListItem);
        ToggleMode = true;
        HorizontalExpand = true;
        MinHeight = 44;

        Item = entity.ID;
        _name = entity.Name;
        _description = entity.Description;
        _tier = tier;

        Text = _name;
        Content.HorizontalAlignment = HAlignment.Stretch;
        Content.Margin = new Thickness(6, 0, 8, 0);
        Content.SeparationOverride = 8;
        Label.HorizontalExpand = true;
        Label.ClipText = true;
        OnToggled += args => Toggled?.Invoke(this, args.Pressed);

        var texture = new TextureRect
        {
            Texture = sprite.Frame0(entity),
            Stretch = TextureRect.StretchMode.KeepAspectCentered,
            SetSize = new Vector2(32, 32),
            VerticalAlignment = VAlignment.Center,
        };
        Content.AddChild(texture);
        texture.SetPositionFirst();

        if (tier <= 0)
            return;

        _tierIcon = new TextureRect
        {
            Texture = NebulaIcons.Get("workspace_premium"),
            Stretch = TextureRect.StretchMode.KeepAspectCentered,
            SetSize = new Vector2(16, 16),
            VerticalAlignment = VAlignment.Center,
        };
        _tierIcon.AddStyleClass(NebulaStyleClass.Icon);

        var tierLabel = new Label
        {
            Text = Loc.GetString("nebula-wardrobe-item-tier", ("tier", tier)),
            VerticalAlignment = VAlignment.Center,
        };
        tierLabel.AddStyleClass(NebulaStyleClass.TextMuted);

        Content.AddChild(_tierIcon);
        Content.AddChild(tierLabel);
    }

    /// <summary>
    /// Updates the selection. A selected item can always be deselected, even if it is not available anymore.
    /// </summary>
    public void SetState(bool pressed, bool locked)
    {
        Pressed = pressed;
        Disabled = !pressed && locked;
        Content.Modulate = locked ? Color.White.WithAlpha(LockedAlpha) : Color.White;

        if (_tierIcon != null)
            _tierIcon.Texture = NebulaIcons.Get(locked ? "lock" : "workspace_premium");

        ToolTip = locked
            ? Loc.GetString("nebula-wardrobe-tier-restriction", ("tier", _tier))
            : _description;
    }

    public bool CheckMatchesSearch(string query)
    {
        return _name.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    public void SetHiddenState(bool state, string query)
    {
        Visible = CheckMatchesSearch(query) ? state : !state;
    }
}
