// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using System.Numerics;
using Content.Client._Nebula.Stylesheets;
using Content.Client.UserInterface.Controls;
using Content.Goobstation.UIKit.UserInterface.Controls;
using Content.Shared._Nebula.CCVar;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client._Nebula.UserInterface.Controls;

/// <summary>
/// <see cref="SimpleRadialMenu"/> in the Nebula style: gapped rounded sectors around a round center button.
/// Colors are taken from the default dark theme and don't follow the palette picked in the options.
/// </summary>
public sealed partial class NebulaRadialMenu : RadialMenu
{
    [Dependency] private IClyde _clyde = default!;
    [Dependency] private IEntityManager _entManager = default!;
    [Dependency] private IInputManager _inputManager = default!;

    private const float SectorAlpha = 0.95f;
    private const float CenterRadius = 28f;
    private const float CenterIconSize = 24f;
    private const int CenterPoints = 48;

    private static readonly NebulaPalette Palette =
        NebulaPalette.Generate(Color.FromHex(NCCVars.InterfaceAccent.DefaultValue), dark: true);

    public static readonly Color SectorColor = Palette.Layer1.WithAlpha(SectorAlpha);
    public static readonly Color SectorHoverColor = Palette.SecondaryContainer.WithAlpha(SectorAlpha);
    public static readonly Color DangerColor =
        NebulaPalette.Mix(Palette.Layer1, Palette.ErrorContainer, 0.6f).WithAlpha(SectorAlpha);
    public static readonly Color DangerHoverColor =
        NebulaPalette.Mix(Palette.Layer1, Palette.ErrorContainer, 0.35f).WithAlpha(SectorAlpha);
    private static readonly Color CenterIconColor = Palette.OnSurface;

    private static readonly RadialMenuOptionComparer Comparer = new();

    private readonly DrawVertexUV2DColor[] _centerFan = new DrawVertexUV2DColor[CenterPoints];
    private readonly Texture _closeIcon;
    private readonly Texture _backIcon;

    private RadialContainer? _rootLayer;
    private EntityUid? _trackedEntity;

    /// <summary>
    /// Whether pressing an action option closes the menu.
    /// </summary>
    public bool CloseOnPressed = true;

    public NebulaRadialMenu()
    {
        IoCManager.InjectDependencies(this);

        HorizontalExpand = true;
        VerticalExpand = true;
        MinSize = new Vector2(450f, 450f);

        _closeIcon = NebulaIcons.Get("close");
        _backIcon = NebulaIcons.Get("arrow_back");
    }

    /// <summary>
    /// Keeps the menu over <paramref name="owner"/> and closes it once the entity is gone.
    /// </summary>
    public void Track(EntityUid owner)
    {
        _trackedEntity = owner;
    }

    public void SetButtons(IEnumerable<RadialMenuOptionBase> models, SimpleRadialMenuSettings? settings = null)
    {
        foreach (var child in Children.ToArray())
        {
            if (child != ContextualButton && child != MenuOuterAreaButton)
                RemoveChild(child);
        }

        var sprites = _entManager.System<SpriteSystem>();
        _rootLayer = null;
        CreateLayer(models, (settings ?? new SimpleRadialMenuSettings()).DefaultContainerRadius, sprites);
    }

    public void OpenOverMouseScreenPosition()
    {
        OpenCenteredAt(_inputManager.MouseScreenPosition.Position / _clyde.ScreenSize);
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var center = PixelSize / 2f;
        var radius = CenterRadius * UIScale;
        var color = (ContextualButton.DrawMode == BaseButton.DrawModeEnum.Hover ? SectorHoverColor : SectorColor)
                    * handle.Modulate;

        for (var i = 0; i < CenterPoints; i++)
        {
            var angle = MathF.Tau * i / CenterPoints;
            _centerFan[i] = new DrawVertexUV2DColor(center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius, color);
        }

        handle.DrawPrimitives(DrawPrimitiveTopology.TriangleFan, Texture.White, _centerFan);

        var icon = _rootLayer is { Visible: false } ? _backIcon : _closeIcon;
        var halfIcon = new Vector2(CenterIconSize * UIScale / 2f);
        handle.DrawTextureRect(icon, new UIBox2(center - halfIcon, center + halfIcon), CenterIconColor);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_trackedEntity is not { } tracked)
            return;

        if (!_entManager.TryGetComponent(tracked, out TransformComponent? xform)
            || !xform.Coordinates.IsValid(_entManager))
        {
            Close();
            return;
        }

        var coords = _entManager.System<SpriteSystem>().GetSpriteScreenCoordinates((tracked, null, xform));
        if (!coords.IsValid)
        {
            Close();
            return;
        }

        OpenScreenAt(coords.Position, _clyde);
    }

    #region Helpers

    /// <summary>
    /// The first added layer must be visible, <see cref="RadialMenu"/> looks for a visible one on every added child.
    /// </summary>
    private RadialContainer CreateLayer(IEnumerable<RadialMenuOptionBase> models, float radius, SpriteSystem sprites)
    {
        var layer = new RadialContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
            InitialRadius = radius,
            ReserveSpaceForHiddenChildren = false,
            Visible = _rootLayer == null,
        };
        AddChild(layer);
        _rootLayer ??= layer;

        foreach (var model in models.Order(Comparer))
        {
            var button = CreateButton(model, sprites);

            if (model is RadialMenuNestedLayerOption nested)
                button.TargetLayer = CreateLayer(nested.Nested, nested.ContainerRadius ?? radius, sprites);

            layer.AddChild(button);
        }

        return layer;
    }

    private NebulaRadialMenuButton CreateButton(RadialMenuOptionBase model, SpriteSystem sprites)
    {
        var button = new NebulaRadialMenuButton
        {
            SetSize = new Vector2(64f, 64f),
            ToolTip = model.ToolTip,
        };

        if (model.BackgroundColor is { } background)
            button.SectorColor = background;

        if (model.HoverBackgroundColor is { } hover)
            button.SectorHoverColor = hover;

        if (CreateIcon(model.IconSpecifier, sprites) is { } icon)
        {
            icon.Modulate = model.Color;
            button.AddChild(icon);
        }

        if (model is RadialMenuActionOptionBase action)
        {
            button.OnPressed += _ =>
            {
                action.OnPressed();

                if (CloseOnPressed)
                    Close();
            };
        }

        return button;
    }

    private static Control? CreateIcon(RadialMenuIconSpecifier? specifier, SpriteSystem sprites)
    {
        switch (specifier)
        {
            case RadialMenuTextureIconSpecifier textureSpecifier:
                var texture = sprites.Frame0(textureSpecifier.Sprite);
                return new TextureRect
                {
                    Texture = texture,
                    TextureScale = texture.Width <= 32 ? new Vector2(2f) : Vector2.One,
                };
            case RadialMenuEntityIconSpecifier entitySpecifier:
                var spriteView = new StaticSpriteView
                {
                    SetSize = new Vector2(48f, 48f),
                    VerticalAlignment = VAlignment.Center,
                    HorizontalAlignment = HAlignment.Center,
                    Stretch = StaticSpriteView.StretchMode.Fill,
                    ColorAlphaOverride = 1f,
                };
                spriteView.SetEntity(entitySpecifier.Entity);
                return spriteView;
            case RadialMenuEntityPrototypeIconSpecifier protoSpecifier:
                var protoView = new EntityPrototypeView
                {
                    SetSize = new Vector2(48f, 48f),
                    VerticalAlignment = VAlignment.Center,
                    HorizontalAlignment = HAlignment.Center,
                    Stretch = SpriteView.StretchMode.Fill,
                };
                protoView.SetPrototype(protoSpecifier.ProtoId);
                return protoView;
            default:
                return null;
        }
    }

    #endregion
}
