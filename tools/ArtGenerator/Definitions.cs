using SkiaSharp;

namespace JabaSurvivol.ArtGenerator;

internal static class Definitions
{
    private const int CharacterSize = 256;
    private const int BossSize = 512;
    private const int IconSize = 128;
    private const int EffectSize = 256;

    public static List<SpriteDefinition> Create()
    {
        var definitions = new List<SpriteDefinition>();

        AddCharacter(definitions, "player", Palette.Player, PlayerShape);
        AddCharacter(definitions, "zombie_walker", Palette.Walker, WalkerShape);
        AddCharacter(definitions, "zombie_runner", Palette.Runner, RunnerShape);
        AddCharacter(definitions, "zombie_brute", Palette.Brute, BruteShape);
        AddCharacter(definitions, "boss_warden", Palette.Warden, WardenShape, BossSize);

        AddTurret(definitions);
        AddWeapons(definitions);
        AddProjectiles(definitions);
        AddEffects(definitions);
        AddIcons(definitions);
        AddEnvironment(definitions);

        return definitions;
    }

    private static readonly CharacterShape PlayerShape = new(
        TorsoWidth: 112f,
        TorsoHeight: 154f,
        TorsoOffsetY: 4f,
        HeadRadius: 38f,
        HeadOffsetX: 34f,
        HeadOffsetY: 0f,
        ArmLength: 92f,
        ArmThickness: 22f,
        LegWidth: 28f,
        LegLength: 176f,
        ShoulderWidth: 152f,
        Ragged: false,
        Horns: false,
        Core: false);

    private static readonly CharacterShape WalkerShape = new(
        TorsoWidth: 118f,
        TorsoHeight: 148f,
        TorsoOffsetY: 4f,
        HeadRadius: 40f,
        HeadOffsetX: 30f,
        HeadOffsetY: -2f,
        ArmLength: 98f,
        ArmThickness: 24f,
        LegWidth: 30f,
        LegLength: 168f,
        ShoulderWidth: 148f,
        Ragged: true,
        Horns: false,
        Core: false);

    private static readonly CharacterShape RunnerShape = new(
        TorsoWidth: 88f,
        TorsoHeight: 148f,
        TorsoOffsetY: 8f,
        HeadRadius: 34f,
        HeadOffsetX: 40f,
        HeadOffsetY: 0f,
        ArmLength: 108f,
        ArmThickness: 19f,
        LegWidth: 24f,
        LegLength: 174f,
        ShoulderWidth: 112f,
        Ragged: true,
        Horns: false,
        Core: false);

    private static readonly CharacterShape BruteShape = new(
        TorsoWidth: 162f,
        TorsoHeight: 158f,
        TorsoOffsetY: 2f,
        HeadRadius: 32f,
        HeadOffsetX: 38f,
        HeadOffsetY: 0f,
        ArmLength: 112f,
        ArmThickness: 34f,
        LegWidth: 38f,
        LegLength: 158f,
        ShoulderWidth: 202f,
        Ragged: true,
        Horns: false,
        Core: false);

    private static readonly CharacterShape WardenShape = new(
        TorsoWidth: 250f,
        TorsoHeight: 250f,
        TorsoOffsetY: 0f,
        HeadRadius: 58f,
        HeadOffsetX: 62f,
        HeadOffsetY: 0f,
        ArmLength: 170f,
        ArmThickness: 46f,
        LegWidth: 56f,
        LegLength: 130f,
        ShoulderWidth: 336f,
        Ragged: false,
        Horns: true,
        Core: true);

    private static void AddCharacter(
        ICollection<SpriteDefinition> definitions,
        string key,
        CharacterPalette palette,
        CharacterShape shape,
        int size = CharacterSize)
    {
        var category = "character";
        definitions.Add(new SpriteDefinition(
            $"Characters/{key}_shadow.png", size, size, 0.5f, 0.5f, category,
            canvas => DrawCharacterPart(canvas, size, palette, shape, CharacterPart.Shadow)));
        definitions.Add(new SpriteDefinition(
            $"Characters/{key}_leg.png", size, size, 0.5f, 0.9f, category,
            canvas => DrawCharacterPart(canvas, size, palette, shape, CharacterPart.Leg)));
        definitions.Add(new SpriteDefinition(
            $"Characters/{key}_torso.png", size, size, 0.5f, 0.5f, category,
            canvas => DrawCharacterPart(canvas, size, palette, shape, CharacterPart.Torso)));
        definitions.Add(new SpriteDefinition(
            $"Characters/{key}_head.png", size, size, 0.5f, 0.5f, category,
            canvas => DrawCharacterPart(canvas, size, palette, shape, CharacterPart.Head)));
        definitions.Add(new SpriteDefinition(
            $"Characters/{key}_arm.png", size, size, 0.5f, 0.5f, category,
            canvas => DrawCharacterPart(canvas, size, palette, shape, CharacterPart.Arm)));

        if (shape.Horns)
        {
            definitions.Add(new SpriteDefinition(
                $"Characters/{key}_core.png", size, size, 0.5f, 0.5f, category,
                canvas => DrawBossCore(canvas, size)));
        }
    }

    private static void DrawCharacterPart(
        SKCanvas canvas,
        int size,
        CharacterPalette palette,
        CharacterShape shape,
        CharacterPart part)
    {
        var scale = size / 256f;
        var center = size / 2f;

        switch (part)
        {
            case CharacterPart.Shadow:
                Draw.SoftShadow(canvas, center, center + (18f * scale), 90f * scale, 48f * scale);
                return;

            case CharacterPart.Leg:
                DrawLeg(canvas, center, scale, palette, shape);
                return;

            case CharacterPart.Torso:
                DrawTorso(canvas, center, scale, palette, shape);
                return;

            case CharacterPart.Head:
                DrawHead(canvas, center, scale, palette, shape);
                return;

            case CharacterPart.Arm:
                DrawArm(canvas, center, scale, palette, shape);
                return;
        }
    }

    private static void DrawLeg(SKCanvas canvas, float center, float scale, CharacterPalette palette, CharacterShape shape)
    {
        var width = shape.LegWidth * scale;
        var top = 26f * scale;
        var bottom = top + (shape.LegLength * scale);
        var left = center - (width / 2f);
        var right = center + (width / 2f);

        Draw.RoundedRect(canvas, left, top, right, bottom, width * 0.45f, palette.Body, Palette.Outline, 7f * scale);
        Draw.RoundedRect(
            canvas,
            left - (4f * scale),
            bottom - (26f * scale),
            right + (8f * scale),
            bottom + (10f * scale),
            width * 0.4f,
            palette.Plate,
            Palette.Outline,
            6f * scale);

        Draw.GlowLine(canvas, center, top + (24f * scale), center, bottom - (34f * scale), palette.Accent, 3f * scale, 70);
        Draw.Rivet(canvas, center, top + (10f * scale), palette.Plate);
    }

    private static void DrawTorso(SKCanvas canvas, float center, float scale, CharacterPalette palette, CharacterShape shape)
    {
        var width = shape.TorsoWidth * scale;
        var height = shape.TorsoHeight * scale;
        var left = center - (width / 2f);
        var right = center + (width / 2f);
        var top = center - (height / 2f) + (shape.TorsoOffsetY * scale);
        var bottom = top + height;

        Draw.GradientRect(
            canvas,
            new SKRect(left, top, right, bottom),
            palette.Plate,
            palette.Body,
            Palette.Outline,
            24f * scale,
            8f * scale);

        var shoulderWidth = shape.ShoulderWidth * scale;
        var shoulderTop = center - (shoulderWidth / 2f);
        var shoulderBottom = center + (shoulderWidth / 2f);
        Draw.RoundedRect(
            canvas,
            left - (12f * scale),
            shoulderTop,
            left + (34f * scale),
            shoulderBottom,
            18f * scale,
            palette.Plate,
            Palette.Outline,
            7f * scale);
        Draw.RoundedRect(
            canvas,
            right - (34f * scale),
            shoulderTop,
            right + (12f * scale),
            shoulderBottom,
            18f * scale,
            palette.Plate,
            Palette.Outline,
            7f * scale);

        Draw.GlowLine(canvas, right - (18f * scale), top + (24f * scale), right - (18f * scale), bottom - (24f * scale), palette.Accent, 4f * scale);
        Draw.GlowLine(canvas, left + (20f * scale), center, right - (24f * scale), center, palette.Accent.WithAlpha(100), 2.5f * scale, 40);

        if (shape.Ragged)
        {
            for (var i = 0; i < 4; i++)
            {
                var y = top + ((i + 0.5f) * (height / 4f));
                Draw.Polygon(
                    canvas,
                    new[]
                    {
                        new SKPoint(left + (2f * scale), y),
                        new SKPoint(left - ((16f + (i * 4f)) * scale), y + (8f * scale)),
                        new SKPoint(left + (2f * scale), y + (18f * scale))
                    },
                    palette.Body,
                    Palette.Outline,
                    4f * scale);
            }
        }

        if (shape.Core)
        {
            Draw.Ellipse(canvas, center + (30f * scale), center, 34f * scale, 52f * scale, palette.Accent.WithAlpha(55));
            Draw.Ellipse(canvas, center + (30f * scale), center, 18f * scale, 30f * scale, Palette.Amber.WithAlpha(120));
        }

        Draw.Rivet(canvas, left + (20f * scale), top + (20f * scale), palette.Accent);
        Draw.Rivet(canvas, left + (20f * scale), bottom - (20f * scale), palette.Accent);
        Draw.Rivet(canvas, right - (20f * scale), top + (20f * scale), palette.Accent);
        Draw.Rivet(canvas, right - (20f * scale), bottom - (20f * scale), palette.Accent);
    }

    private static void DrawHead(SKCanvas canvas, float center, float scale, CharacterPalette palette, CharacterShape shape)
    {
        var headX = center + (shape.HeadOffsetX * scale);
        var headY = center + (shape.HeadOffsetY * scale);
        var radius = shape.HeadRadius * scale;

        if (shape.Horns)
        {
            Draw.Polygon(
                canvas,
                new[]
                {
                    new SKPoint(headX - (radius * 0.4f), headY - (radius * 0.7f)),
                    new SKPoint(headX - (radius * 1.5f), headY - (radius * 1.45f)),
                    new SKPoint(headX - (radius * 0.95f), headY - (radius * 0.15f))
                },
                palette.Plate,
                Palette.Outline,
                6f * scale);
            Draw.Polygon(
                canvas,
                new[]
                {
                    new SKPoint(headX - (radius * 0.4f), headY + (radius * 0.7f)),
                    new SKPoint(headX - (radius * 1.5f), headY + (radius * 1.45f)),
                    new SKPoint(headX - (radius * 0.95f), headY + (radius * 0.15f))
                },
                palette.Plate,
                Palette.Outline,
                6f * scale);
        }

        Draw.Ellipse(canvas, headX, headY, radius, radius, palette.Body, Palette.Outline, 8f * scale);
        Draw.Ellipse(canvas, headX, headY, radius * 0.72f, radius * 0.72f, palette.Plate);
        Draw.Ellipse(canvas, headX + (radius * 0.38f), headY, radius * 0.30f, radius * 0.22f, palette.Visor, Palette.Outline, 4f * scale);
        Draw.GlowLine(canvas, headX + (radius * 0.72f), headY, headX + (radius * 0.92f), headY, palette.Accent, 4f * scale, 90);
        Draw.Rivet(canvas, headX - (radius * 0.35f), headY - (radius * 0.45f), palette.Accent);
    }

    private static void DrawArm(SKCanvas canvas, float center, float scale, CharacterPalette palette, CharacterShape shape)
    {
        var length = shape.ArmLength * scale;
        var thickness = shape.ArmThickness * scale;

        canvas.Save();
        canvas.Translate(center - (length * 0.38f), center);
        canvas.RotateDegrees(-8f);
        Draw.RoundedRect(canvas, 0f, -thickness / 2f, length, thickness / 2f, thickness * 0.45f, palette.Body, Palette.Outline, 7f * scale);
        Draw.Ellipse(canvas, length, 0f, thickness * 0.78f, thickness * 0.72f, palette.Plate, Palette.Outline, 6f * scale);
        Draw.GlowLine(canvas, thickness, 0f, length - (thickness * 0.7f), 0f, palette.Accent, 3f * scale, 70);
        Draw.Rivet(canvas, length * 0.42f, 0f, palette.Accent);
        canvas.Restore();
    }

    private static void DrawBossCore(SKCanvas canvas, int size)
    {
        var center = size / 2f;
        var radius = size * 0.17f;
        using var shader = SKShader.CreateRadialGradient(
            new SKPoint(center, center),
            radius * 1.4f,
            new[]
            {
                Palette.Amber,
                Palette.Magenta.WithAlpha(180),
                Palette.Magenta.WithAlpha(40),
                SKColors.Transparent
            },
            new[] { 0f, 0.35f, 0.72f, 1f },
            SKShaderTileMode.Clamp);

        using var paint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Shader = shader };
        canvas.DrawCircle(center, center, radius * 1.4f, paint);
        Draw.Ellipse(canvas, center, center, radius * 0.62f, radius * 0.62f, Palette.White);
    }

    private static void AddTurret(ICollection<SpriteDefinition> definitions)
    {
        definitions.Add(new SpriteDefinition(
            "Characters/turret_base.png", CharacterSize, CharacterSize, 0.5f, 0.5f, "character",
            canvas => DrawTurretBase(canvas, CharacterSize)));
        definitions.Add(new SpriteDefinition(
            "Characters/turret_head.png", CharacterSize, CharacterSize, 0.5f, 0.5f, "character",
            canvas => DrawTurretHead(canvas, CharacterSize)));
        definitions.Add(new SpriteDefinition(
            "Characters/turret_barrel.png", 128, 64, 0.16f, 0.5f, "character",
            canvas => DrawTurretBarrel(canvas, 128, 64)));
    }

    private static void DrawTurretBase(SKCanvas canvas, int size)
    {
        var center = size / 2f;
        var scale = size / 256f;
        Draw.SoftShadow(canvas, center, center + (16f * scale), 84f * scale, 42f * scale);
        Draw.Ellipse(canvas, center, center, 88f * scale, 70f * scale, Palette.Turret.Body, Palette.Outline, 9f * scale);
        Draw.Ellipse(canvas, center, center, 62f * scale, 48f * scale, Palette.Turret.Plate);
        Draw.Ellipse(canvas, center, center, 38f * scale, 28f * scale, Palette.Turret.Body, Palette.Outline, 5f * scale);
        Draw.GlowLine(canvas, center - (60f * scale), center + (28f * scale), center + (60f * scale), center + (28f * scale), Palette.Green, 4f * scale);
        for (var i = 0; i < 4; i++)
        {
            var angle = (MathF.PI / 2f) * i;
            Draw.Rivet(
                canvas,
                center + (MathF.Cos(angle) * 72f * scale),
                center + (MathF.Sin(angle) * 54f * scale),
                Palette.Green);
        }
    }

    private static void DrawTurretHead(SKCanvas canvas, int size)
    {
        var center = size / 2f;
        var scale = size / 256f;
        Draw.Ellipse(canvas, center, center, 58f * scale, 58f * scale, Palette.Turret.Body, Palette.Outline, 8f * scale);
        Draw.Ellipse(canvas, center, center, 40f * scale, 40f * scale, Palette.Turret.Plate);
        Draw.Ellipse(canvas, center + (20f * scale), center, 18f * scale, 14f * scale, Palette.Turret.Visor, Palette.Outline, 4f * scale);
        Draw.GlowLine(canvas, center + (36f * scale), center, center + (56f * scale), center, Palette.Green, 4f * scale);
        Draw.Rivet(canvas, center - (22f * scale), center - (22f * scale), Palette.Green);
        Draw.Rivet(canvas, center - (22f * scale), center + (22f * scale), Palette.Green);
    }

    private static void DrawTurretBarrel(SKCanvas canvas, int width, int height)
    {
        var centerY = height / 2f;
        Draw.RoundedRect(canvas, 8f, centerY - 12f, width - 10f, centerY + 12f, 10f, Palette.Turret.Plate, Palette.Outline, 6f);
        Draw.GlowLine(canvas, 14f, centerY, width - 16f, centerY, Palette.Green, 5f);
        Draw.Ellipse(canvas, width - 10f, centerY, 8f, 8f, Palette.Turret.Visor, Palette.Outline, 3f);
    }

    private static void AddWeapons(ICollection<SpriteDefinition> definitions)
    {
        foreach (var style in Enum.GetValues<WeaponStyle>())
        {
            definitions.Add(new SpriteDefinition(
                $"Weapons/{style.ToString().ToLowerInvariant()}.png", 256, 128, 0.18f, 0.5f, "weapon",
                canvas => DrawWeapon(canvas, 256, 128, style)));
        }
    }

    private enum WeaponStyle
    {
        Pistol,
        Smg,
        Shotgun,
        Rifle,
        Lmg
    }

    private static void DrawWeapon(SKCanvas canvas, int width, int height, WeaponStyle style)
    {
        var centerY = height / 2f;
        var accent = style switch
        {
            WeaponStyle.Pistol => Palette.Cyan,
            WeaponStyle.Smg => Palette.Green,
            WeaponStyle.Shotgun => Palette.Amber,
            WeaponStyle.Rifle => Palette.Magenta,
            _ => Palette.Orange
        };

        var bodyLeft = style == WeaponStyle.Pistol ? 54f : 42f;
        var bodyRight = style switch
        {
            WeaponStyle.Pistol => 176f,
            WeaponStyle.Shotgun => 214f,
            WeaponStyle.Rifle => 206f,
            WeaponStyle.Lmg => 220f,
            _ => 198f
        };
        var bodyHeight = style == WeaponStyle.Lmg ? 46f : 34f;

        Draw.GradientRect(
            canvas,
            new SKRect(bodyLeft, centerY - (bodyHeight / 2f), bodyRight, centerY + (bodyHeight / 2f)),
            Palette.Steel,
            Palette.Outline,
            Palette.Outline,
            10f,
            7f);

        if (style == WeaponStyle.Pistol)
        {
            Draw.RoundedRect(canvas, 86f, centerY + (bodyHeight / 2f) - 2f, 112f, centerY + 48f, 8f, Palette.Outline, Palette.Outline, 5f);
            Draw.RoundedRect(canvas, 174f, centerY - 9f, 222f, centerY + 9f, 6f, Palette.Steel, Palette.Outline, 5f);
        }
        else if (style == WeaponStyle.Smg)
        {
            Draw.RoundedRect(canvas, 104f, centerY + (bodyHeight / 2f) - 2f, 132f, centerY + 52f, 7f, Palette.Outline, Palette.Outline, 5f);
            Draw.RoundedRect(canvas, 198f, centerY - 7f, 238f, centerY + 7f, 5f, Palette.Steel, Palette.Outline, 5f);
        }
        else if (style == WeaponStyle.Shotgun)
        {
            Draw.RoundedRect(canvas, 70f, centerY - 20f, 218f, centerY - 7f, 6f, Palette.Steel, Palette.Outline, 5f);
            Draw.RoundedRect(canvas, 70f, centerY + 7f, 218f, centerY + 20f, 6f, Palette.Steel, Palette.Outline, 5f);
            Draw.RoundedRect(canvas, 116f, centerY + 16f, 158f, centerY + 30f, 5f, Palette.Outline, Palette.Outline, 4f);
            Draw.RoundedRect(canvas, 220f, centerY - 22f, 244f, centerY + 22f, 5f, Palette.Steel, Palette.Outline, 5f);
        }
        else if (style == WeaponStyle.Rifle)
        {
            Draw.RoundedRect(canvas, 74f, centerY - 34f, 132f, centerY - 16f, 5f, Palette.Outline, Palette.Outline, 4f);
            Draw.RoundedRect(canvas, 206f, centerY - 6f, 248f, centerY + 6f, 4f, Palette.Steel, Palette.Outline, 4f);
            Draw.RoundedRect(canvas, 128f, centerY + 16f, 152f, centerY + 34f, 4f, Palette.Outline, Palette.Outline, 4f);
        }
        else
        {
            Draw.Ellipse(canvas, 128f, centerY + 30f, 30f, 30f, Palette.Outline, Palette.Outline, 5f);
            Draw.Ellipse(canvas, 128f, centerY + 30f, 20f, 20f, Palette.Orange.WithAlpha(150));
            Draw.RoundedRect(canvas, 220f, centerY - 7f, 250f, centerY + 7f, 4f, Palette.Steel, Palette.Outline, 4f);
            Draw.RoundedRect(canvas, 150f, centerY + 16f, 168f, centerY + 52f, 4f, Palette.Outline, Palette.Outline, 4f);
        }

        Draw.GlowLine(canvas, bodyLeft + 10f, centerY - (bodyHeight / 2f) + 5f, bodyRight - 10f, centerY - (bodyHeight / 2f) + 5f, accent, 3f);
        Draw.GlowLine(canvas, bodyRight - 4f, centerY - 10f, bodyRight + 24f, centerY - 10f, accent, 4f, 100);
        Draw.GlowLine(canvas, bodyRight - 4f, centerY + 10f, bodyRight + 24f, centerY + 10f, accent, 4f, 100);
        Draw.Rivet(canvas, bodyLeft + 14f, centerY, accent);
    }

    private static void AddProjectiles(ICollection<SpriteDefinition> definitions)
    {
        AddProjectile(definitions, "pistol", 128, 64, Palette.Amber, false);
        AddProjectile(definitions, "smg", 112, 56, Palette.Green, false);
        AddProjectile(definitions, "rifle", 144, 56, Palette.Magenta, false);
        AddProjectile(definitions, "lmg", 144, 72, Palette.Orange, false);
        AddProjectile(definitions, "pellet", 64, 64, Palette.Amber, true);
        AddProjectile(definitions, "turret", 128, 64, Palette.Green, false);
    }

    private static void AddProjectile(
        ICollection<SpriteDefinition> definitions,
        string key,
        int width,
        int height,
        SKColor color,
        bool pellet)
    {
        definitions.Add(new SpriteDefinition(
            $"Projectiles/{key}.png", width, height, 0.5f, 0.5f, "projectile",
            canvas => DrawProjectile(canvas, width, height, color, pellet)));
    }

    private static void DrawProjectile(SKCanvas canvas, int width, int height, SKColor color, bool pellet)
    {
        var centerY = height / 2f;
        if (pellet)
        {
            var center = width / 2f;
            Draw.Ellipse(canvas, center, center, 13f, 13f, color.WithAlpha(90));
            Draw.Ellipse(canvas, center, center, 7f, 7f, Palette.White);
            return;
        }

        Draw.Ellipse(canvas, width / 2f, centerY, width * 0.42f, height * 0.28f, color.WithAlpha(55));
        Draw.RoundedRect(canvas, 16f, centerY - 5f, width - 18f, centerY + 5f, 5f, color, Palette.White, 2f);
        Draw.Ellipse(canvas, width - 20f, centerY, 7f, 7f, Palette.White);
    }

    private static void AddEffects(ICollection<SpriteDefinition> definitions)
    {
        definitions.Add(new SpriteDefinition(
            "Effects/muzzle_flash.png", EffectSize, EffectSize, 0.5f, 0.5f, "effect",
            canvas => DrawMuzzleFlash(canvas, EffectSize, Palette.Amber)));
        definitions.Add(new SpriteDefinition(
            "Effects/muzzle_flash_alt.png", EffectSize, EffectSize, 0.5f, 0.5f, "effect",
            canvas => DrawMuzzleFlash(canvas, EffectSize, Palette.Cyan)));
        definitions.Add(new SpriteDefinition(
            "Effects/impact.png", EffectSize, EffectSize, 0.5f, 0.5f, "effect",
            canvas => DrawImpact(canvas, EffectSize, Palette.Amber)));
        definitions.Add(new SpriteDefinition(
            "Effects/death_burst.png", EffectSize, EffectSize, 0.5f, 0.5f, "effect",
            canvas => DrawDeathBurst(canvas, EffectSize, Palette.Red)));
        definitions.Add(new SpriteDefinition(
            "Effects/elite_aura.png", EffectSize, EffectSize, 0.5f, 0.5f, "effect",
            canvas => DrawEliteAura(canvas, EffectSize)));
        definitions.Add(new SpriteDefinition(
            "Effects/hit_flash.png", EffectSize, EffectSize, 0.5f, 0.5f, "effect",
            canvas => DrawHitFlash(canvas, EffectSize)));
    }

    private static void DrawMuzzleFlash(SKCanvas canvas, int size, SKColor color)
    {
        var center = size / 2f;
        var spikes = 8;
        using var builder = new SKPathBuilder();
        for (var i = 0; i < spikes * 2; i++)
        {
            var angle = (MathF.PI * 2f * i) / (spikes * 2f);
            var radius = i % 2 == 0 ? size * 0.42f : size * 0.16f;
            var point = new SKPoint(
                center + (MathF.Cos(angle) * radius),
                center + (MathF.Sin(angle) * radius));
            if (i == 0)
            {
                builder.MoveTo(point);
            }
            else
            {
                builder.LineTo(point);
            }
        }

        builder.Close();
        using var path = builder.Detach();
        using var glowPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = color.WithAlpha(70) };
        canvas.DrawCircle(center, center, size * 0.46f, glowPaint);
        using var fillPaint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Color = color.WithAlpha(210) };
        canvas.DrawPath(path, fillPaint);
        Draw.Ellipse(canvas, center, center, size * 0.13f, size * 0.13f, Palette.White);
    }

    private static void DrawImpact(SKCanvas canvas, int size, SKColor color)
    {
        var center = size / 2f;
        for (var i = 0; i < 7; i++)
        {
            var angle = (MathF.PI * 2f * i) / 7f;
            var inner = size * 0.10f;
            var outer = size * (i % 2 == 0 ? 0.34f : 0.24f);
            Draw.GlowLine(
                canvas,
                center + (MathF.Cos(angle) * inner),
                center + (MathF.Sin(angle) * inner),
                center + (MathF.Cos(angle) * outer),
                center + (MathF.Sin(angle) * outer),
                color,
                4f,
                70);
        }

        Draw.Ellipse(canvas, center, center, size * 0.10f, size * 0.10f, Palette.White);
    }

    private static void DrawDeathBurst(SKCanvas canvas, int size, SKColor color)
    {
        var center = size / 2f;
        using var ringPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 8f,
            Color = color.WithAlpha(180)
        };
        canvas.DrawCircle(center, center, size * 0.30f, ringPaint);
        using var innerPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 3f,
            Color = Palette.White.WithAlpha(160)
        };
        canvas.DrawCircle(center, center, size * 0.20f, innerPaint);
        for (var i = 0; i < 6; i++)
        {
            var angle = (MathF.PI * 2f * i) / 6f;
            Draw.GlowLine(
                canvas,
                center + (MathF.Cos(angle) * size * 0.18f),
                center + (MathF.Sin(angle) * size * 0.18f),
                center + (MathF.Cos(angle) * size * 0.42f),
                center + (MathF.Sin(angle) * size * 0.42f),
                color,
                3f,
                80);
        }
    }

    private static void DrawEliteAura(SKCanvas canvas, int size)
    {
        var center = size / 2f;
        using var glowPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 18f,
            Color = Palette.EliteGold.WithAlpha(45)
        };
        canvas.DrawCircle(center, center, size * 0.39f, glowPaint);
        using var ringPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 6f,
            Color = Palette.EliteGold.WithAlpha(220)
        };
        canvas.DrawCircle(center, center, size * 0.39f, ringPaint);
        for (var i = 0; i < 8; i++)
        {
            var angle = (MathF.PI * 2f * i) / 8f;
            Draw.GlowLine(
                canvas,
                center + (MathF.Cos(angle) * size * 0.43f),
                center + (MathF.Sin(angle) * size * 0.43f),
                center + (MathF.Cos(angle) * size * 0.49f),
                center + (MathF.Sin(angle) * size * 0.49f),
                Palette.EliteGold,
                3f,
                120);
        }
    }

    private static void DrawHitFlash(SKCanvas canvas, int size)
    {
        var center = size / 2f;
        Draw.Ellipse(canvas, center, center, size * 0.38f, size * 0.38f, Palette.White.WithAlpha(30));
        Draw.Ellipse(canvas, center, center, size * 0.26f, size * 0.26f, Palette.White.WithAlpha(55));
        Draw.Ellipse(canvas, center, center, size * 0.10f, size * 0.10f, Palette.White.WithAlpha(180));
    }

    private static void AddIcons(ICollection<SpriteDefinition> definitions)
    {
        AddIcon(definitions, "skill_turret", IconKind.Turret);
        AddIcon(definitions, "skill_dash", IconKind.Dash);
        AddIcon(definitions, "skill_shockwave", IconKind.Shockwave);
        AddIcon(definitions, "reward_supplies", IconKind.Supplies);
        AddIcon(definitions, "reward_xp", IconKind.Xp);
        AddIcon(definitions, "reward_upgrade", IconKind.Upgrade);
        AddIcon(definitions, "reward_ammo", IconKind.Ammo);
        AddIcon(definitions, "reward_armor", IconKind.Armor);

        foreach (var style in Enum.GetValues<WeaponStyle>())
        {
            definitions.Add(new SpriteDefinition(
                $"UI/weapon_{style.ToString().ToLowerInvariant()}.png", IconSize, IconSize, 0.5f, 0.5f, "ui",
                canvas =>
                {
                    canvas.Save();
                    canvas.Translate(0f, 30f);
                    canvas.Scale(0.58f);
                    DrawWeapon(canvas, 256, 128, style);
                    canvas.Restore();
                }));
        }
    }

    private enum IconKind
    {
        Turret,
        Dash,
        Shockwave,
        Supplies,
        Xp,
        Upgrade,
        Ammo,
        Armor
    }

    private static void AddIcon(ICollection<SpriteDefinition> definitions, string key, IconKind kind)
    {
        definitions.Add(new SpriteDefinition(
            $"UI/{key}.png", IconSize, IconSize, 0.5f, 0.5f, "ui",
            canvas => DrawIcon(canvas, IconSize, kind)));
    }

    private static void DrawIcon(SKCanvas canvas, int size, IconKind kind)
    {
        var center = size / 2f;
        var accent = kind switch
        {
            IconKind.Turret => Palette.Green,
            IconKind.Dash => Palette.Cyan,
            IconKind.Shockwave => Palette.Magenta,
            IconKind.Supplies => Palette.Amber,
            IconKind.Xp => Palette.Lime,
            IconKind.Upgrade => Palette.Purple,
            IconKind.Ammo => Palette.Orange,
            _ => Palette.Green
        };

        Draw.Ellipse(canvas, center, center, size * 0.43f, size * 0.43f, Palette.Background, accent.WithAlpha(120), 5f);

        switch (kind)
        {
            case IconKind.Turret:
                Draw.Ellipse(canvas, center, center + (10f), 28f, 18f, accent.WithAlpha(90));
                Draw.Ellipse(canvas, center, center, 24f, 24f, Palette.Steel, Palette.Outline, 4f);
                Draw.RoundedRect(canvas, center, center - 7f, center + 44f, center + 7f, 5f, accent, Palette.Outline, 4f);
                break;

            case IconKind.Dash:
                Draw.Polygon(
                    canvas,
                    new[]
                    {
                        new SKPoint(center - 34f, center - 30f),
                        new SKPoint(center + 4f, center),
                        new SKPoint(center - 34f, center + 30f),
                        new SKPoint(center - 12f, center)
                    },
                    accent,
                    Palette.Outline,
                    4f);
                Draw.Polygon(
                    canvas,
                    new[]
                    {
                        new SKPoint(center - 4f, center - 24f),
                        new SKPoint(center + 32f, center),
                        new SKPoint(center - 4f, center + 24f),
                        new SKPoint(center + 12f, center)
                    },
                    Palette.White,
                    Palette.Outline,
                    3f);
                break;

            case IconKind.Shockwave:
                for (var i = 0; i < 3; i++)
                {
                    using var ringPaint = new SKPaint
                    {
                        IsAntialias = true,
                        Style = SKPaintStyle.Stroke,
                        StrokeWidth = 4f,
                        Color = (i == 0 ? Palette.White : accent).WithAlpha((byte)(210 - (i * 55)))
                    };
                    canvas.DrawCircle(center, center, 18f + (i * 16f), ringPaint);
                }

                break;

            case IconKind.Supplies:
                Draw.RoundedRect(canvas, center - 32f, center - 24f, center + 32f, center + 28f, 6f, Palette.Outline, accent, 4f);
                Draw.GlowLine(canvas, center - 28f, center - 10f, center + 28f, center - 10f, accent, 4f);
                Draw.Ellipse(canvas, center, center + 6f, 14f, 14f, accent, Palette.Outline, 3f);
                break;

            case IconKind.Xp:
                Draw.Polygon(
                    canvas,
                    new[]
                    {
                        new SKPoint(center, center - 38f),
                        new SKPoint(center + 12f, center - 8f),
                        new SKPoint(center + 42f, center - 6f),
                        new SKPoint(center + 18f, center + 14f),
                        new SKPoint(center + 26f, center + 42f),
                        new SKPoint(center, center + 26f),
                        new SKPoint(center - 26f, center + 42f),
                        new SKPoint(center - 18f, center + 14f),
                        new SKPoint(center - 42f, center - 6f),
                        new SKPoint(center - 12f, center - 8f)
                    },
                    accent,
                    Palette.Outline,
                    3f);
                break;

            case IconKind.Upgrade:
                Draw.Polygon(
                    canvas,
                    new[]
                    {
                        new SKPoint(center, center - 42f),
                        new SKPoint(center + 36f, center - 12f),
                        new SKPoint(center + 20f, center - 12f),
                        new SKPoint(center + 20f, center + 36f),
                        new SKPoint(center - 20f, center + 36f),
                        new SKPoint(center - 20f, center - 12f),
                        new SKPoint(center - 36f, center - 12f)
                    },
                    accent,
                    Palette.Outline,
                    4f);
                break;

            case IconKind.Ammo:
                Draw.RoundedRect(canvas, center - 34f, center - 22f, center + 34f, center + 28f, 6f, Palette.Outline, accent, 4f);
                for (var i = 0; i < 3; i++)
                {
                    Draw.RoundedRect(
                        canvas,
                        center - 24f + (i * 18f),
                        center - 10f,
                        center - 14f + (i * 18f),
                        center + 14f,
                        4f,
                        accent,
                        Palette.Outline,
                        3f);
                }

                break;

            case IconKind.Armor:
                Draw.Polygon(
                    canvas,
                    new[]
                    {
                        new SKPoint(center, center - 42f),
                        new SKPoint(center + 36f, center - 24f),
                        new SKPoint(center + 28f, center + 18f),
                        new SKPoint(center, center + 42f),
                        new SKPoint(center - 28f, center + 18f),
                        new SKPoint(center - 36f, center - 24f)
                    },
                    accent,
                    Palette.Outline,
                    4f);
                Draw.GlowLine(canvas, center, center - 28f, center, center + 28f, Palette.White, 3f);
                break;
        }
    }

    private static void AddEnvironment(ICollection<SpriteDefinition> definitions)
    {
        definitions.Add(new SpriteDefinition(
            "Environment/floor_tile.png", 256, 256, 0.5f, 0.5f, "environment",
            DrawFloorTile));
        definitions.Add(new SpriteDefinition(
            "Environment/wall_horizontal.png", 256, 64, 0.5f, 0.5f, "environment",
            canvas => DrawWall(canvas, 256, 64, true)));
        definitions.Add(new SpriteDefinition(
            "Environment/wall_vertical.png", 64, 256, 0.5f, 0.5f, "environment",
            canvas => DrawWall(canvas, 64, 256, false)));
        definitions.Add(new SpriteDefinition(
            "Environment/prop_barrel.png", 256, 256, 0.5f, 0.5f, "environment",
            DrawBarrel));
        definitions.Add(new SpriteDefinition(
            "Environment/prop_crate.png", 256, 256, 0.5f, 0.5f, "environment",
            DrawCrate));
        definitions.Add(new SpriteDefinition(
            "Environment/prop_debris.png", 256, 256, 0.5f, 0.5f, "environment",
            DrawDebris));
    }

    private static void DrawFloorTile(SKCanvas canvas)
    {
        using var background = Draw.Fill(Palette.Floor);
        canvas.DrawRect(new SKRect(0, 0, 256, 256), background);
        using var grid = Draw.Stroke(Palette.FloorGrid, 2f);
        canvas.DrawLine(0, 64, 256, 64, grid);
        canvas.DrawLine(0, 128, 256, 128, grid);
        canvas.DrawLine(0, 192, 256, 192, grid);
        canvas.DrawLine(64, 0, 64, 256, grid);
        canvas.DrawLine(128, 0, 128, 256, grid);
        canvas.DrawLine(192, 0, 192, 256, grid);
        Draw.GlowLine(canvas, 24, 232, 118, 190, Palette.Cyan.WithAlpha(45), 2f, 30);
        Draw.GlowLine(canvas, 146, 64, 226, 28, Palette.Magenta.WithAlpha(35), 2f, 25);
        Draw.Rivet(canvas, 18, 18, Palette.Steel);
        Draw.Rivet(canvas, 238, 238, Palette.Steel);
    }

    private static void DrawWall(SKCanvas canvas, int width, int height, bool horizontal)
    {
        using var background = Draw.Fill(Palette.Floor);
        canvas.DrawRect(new SKRect(0, 0, width, height), background);
        var inset = 6f;
        Draw.GradientRect(
            canvas,
            new SKRect(inset, inset, width - inset, height - inset),
            Palette.FloorGrid,
            Palette.Background,
            Palette.Outline,
            8f,
            5f);
        if (horizontal)
        {
            Draw.GlowLine(canvas, 12, height - 10, width - 12, height - 10, Palette.Cyan, 4f, 80);
            Draw.GlowLine(canvas, 12, 10, width - 12, 10, Palette.Steel, 2f, 40);
            for (var x = 32; x < width; x += 64)
            {
                Draw.Rivet(canvas, x, height / 2f, Palette.Cyan);
            }
        }
        else
        {
            Draw.GlowLine(canvas, width - 10, 12, width - 10, height - 12, Palette.Cyan, 4f, 80);
            Draw.GlowLine(canvas, 10, 12, 10, height - 12, Palette.Steel, 2f, 40);
            for (var y = 32; y < height; y += 64)
            {
                Draw.Rivet(canvas, width / 2f, y, Palette.Cyan);
            }
        }
    }

    private static void DrawBarrel(SKCanvas canvas)
    {
        var center = 128f;
        Draw.SoftShadow(canvas, center, center + 22f, 64f, 32f);
        Draw.Ellipse(canvas, center, center, 58f, 70f, Palette.Orange.WithAlpha(70));
        Draw.Ellipse(canvas, center, center, 48f, 58f, Palette.Steel, Palette.Outline, 8f);
        Draw.Ellipse(canvas, center, center, 32f, 42f, Palette.Background);
        Draw.Ellipse(canvas, center, center, 20f, 28f, Palette.Orange.WithAlpha(120));
        Draw.GlowLine(canvas, center - 34f, center + 36f, center + 34f, center + 36f, Palette.Orange, 4f);
        Draw.Rivet(canvas, center, center - 52f, Palette.Amber);
    }

    private static void DrawCrate(SKCanvas canvas)
    {
        var center = 128f;
        Draw.SoftShadow(canvas, center, center + 26f, 62f, 30f);
        Draw.GradientRect(
            canvas,
            new SKRect(66, 66, 190, 190),
            Palette.Steel,
            Palette.Background,
            Palette.Outline,
            12f,
            8f);
        Draw.GlowLine(canvas, 76, 84, 180, 84, Palette.Amber, 4f);
        Draw.GlowLine(canvas, 76, 172, 180, 172, Palette.Amber, 4f);
        Draw.GlowLine(canvas, 84, 76, 84, 180, Palette.Amber, 3f, 70);
        Draw.GlowLine(canvas, 172, 76, 172, 180, Palette.Amber, 3f, 70);
        Draw.Rivet(canvas, 80, 80, Palette.Amber);
        Draw.Rivet(canvas, 176, 176, Palette.Amber);
    }

    private static void DrawDebris(SKCanvas canvas)
    {
        var center = 128f;
        Draw.SoftShadow(canvas, center, center + 20f, 58f, 26f);
        Draw.Polygon(
            canvas,
            new[]
            {
                new SKPoint(72, 150),
                new SKPoint(102, 82),
                new SKPoint(132, 126),
                new SKPoint(166, 96),
                new SKPoint(186, 166),
                new SKPoint(112, 182)
            },
            Palette.FloorGrid,
            Palette.Outline,
            6f);
        Draw.GlowLine(canvas, 98, 154, 132, 124, Palette.Cyan.WithAlpha(80), 2f, 40);
        Draw.GlowLine(canvas, 144, 148, 174, 120, Palette.Magenta.WithAlpha(70), 2f, 35);
    }
}
