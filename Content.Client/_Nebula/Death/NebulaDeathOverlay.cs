// SPDX-FileCopyrightText: 2026 Lytheriia
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Client.Resources;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Client._Nebula.Death;

/// <summary>
/// Full screen pass of the dying effect with the values <see cref="NebulaDeathSystem"/> sets every frame,
/// plus the "signal lost" caption of the neural interface.
/// </summary>
public sealed class NebulaDeathOverlay : Overlay
{
    public static readonly Color DefaultAccent = Color.FromHex("#3fd8ff");
    public static readonly Color DefaultAlert = Color.FromHex("#ff4040");

    private static readonly ProtoId<ShaderPrototype> DeathShader = "NebulaDeath";
    /// <summary>
    /// Above popups and the world UI overlays, under the neural interface HUD so its break-up stays visible.
    /// </summary>
    public const int ZOrder = 100;

    private const float CaptionShift = 3f;
    private const float FlickerChance = 0.15f;
    private const float FlickerAlpha = 0.35f;

    private readonly ShaderInstance _shader;
    private readonly IRobustRandom _random;
    private readonly IEyeManager _eye;
    private readonly Font _captionFont;
    private readonly Font _subFont;

    public override OverlaySpace Space => OverlaySpace.ScreenSpace;
    public override bool RequestScreenTexture => true;

    public float Grey;
    public float Blur;
    public float Vignette;
    public float Darkness;
    public float Glitch;

    /// <summary>
    /// 0 hides the caption, 1 shows it fully.
    /// </summary>
    public float Caption;
    public string CaptionText = string.Empty;
    public string CaptionSub = string.Empty;

    /// <summary>
    /// Colors of the neural interface profile: the caption is drawn in the alert color, its ghosts split into both.
    /// </summary>
    public Color Accent = DefaultAccent;
    public Color Alert = DefaultAlert;

    /// <summary>
    /// Off with reduced motion: the caption stays steady and the shader does no glitch.
    /// </summary>
    public bool Motion = true;

    public NebulaDeathOverlay(IPrototypeManager proto, IResourceCache cache, IRobustRandom random, IEyeManager eye)
    {
        _shader = proto.Index(DeathShader).InstanceUnique();
        _random = random;
        _eye = eye;
        _captionFont = cache.GetFont("/Fonts/RobotoMono/RobotoMono-Bold.ttf", 28);
        _subFont = cache.GetFont("/Fonts/RobotoMono/RobotoMono-Bold.ttf", 12);
        ZIndex = ZOrder;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return (Grey > 0f || Darkness > 0f || Caption > 0f)
            && args.ViewportControl == _eye.MainViewport;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (ScreenTexture == null)
            return;

        var handle = args.ScreenHandle;
        _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
        _shader.SetParameter("grey", Grey);
        _shader.SetParameter("blur", Blur);
        _shader.SetParameter("vignette", Vignette);
        _shader.SetParameter("darkness", Darkness);
        _shader.SetParameter("glitch", Motion ? Glitch : 0f);
        _shader.SetParameter("time", (float) _random.NextDouble() * 100f);
        handle.UseShader(_shader);
        handle.DrawRect(args.ViewportBounds, Color.White);
        handle.UseShader(null);

        if (Caption > 0f)
            DrawCaption(handle, args);
    }

    private void DrawCaption(DrawingHandleScreen handle, in OverlayDrawArgs args)
    {
        var scale = (args.ViewportControl as Control)?.UIScale ?? 1f;
        var center = ((UIBox2) args.ViewportBounds).Center;
        var alpha = Caption;
        if (Motion && _random.Prob(FlickerChance))
            alpha *= FlickerAlpha;

        var size = handle.GetDimensions(_captionFont, CaptionText, scale);
        var position = center - size / 2f;
        if (Motion)
        {
            var shift = new Vector2(CaptionShift * scale, 0f);
            handle.DrawString(_captionFont, position + shift, CaptionText, scale, Accent.WithAlpha(0.6f * alpha));
            handle.DrawString(_captionFont, position - shift, CaptionText, scale, Alert.WithAlpha(0.6f * alpha));
        }

        handle.DrawString(_captionFont, position, CaptionText, scale, Alert.WithAlpha(alpha));

        var subSize = handle.GetDimensions(_subFont, CaptionSub, scale);
        handle.DrawString(_subFont,
            new Vector2(center.X - subSize.X / 2f, position.Y + size.Y + 6f * scale),
            CaptionSub,
            scale,
            Alert.WithAlpha(0.75f * alpha));
    }
}
