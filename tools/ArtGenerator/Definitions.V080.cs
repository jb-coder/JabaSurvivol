using SkiaSharp;

namespace JabaSurvivol.ArtGenerator;

internal static partial class Definitions
{
    private static void AddV080Environment(ICollection<SpriteDefinition> definitions)
    {
        AddFloor(definitions, "floor_asphalt_a", 512, Palette.Floor, 11);
        AddFloor(definitions, "floor_asphalt_b", 512, Palette.Floor, 29);
        AddFloor(definitions, "floor_concrete", 512, Palette.FloorGrid, 47);

        AddDecal(definitions, "decal_road_line", DrawRoadLine);
        AddDecal(definitions, "decal_road_arrow", DrawRoadArrow);
        AddDecal(definitions, "decal_crack", DrawCrack);
        AddDecal(definitions, "decal_oil", DrawOil);
        AddDecal(definitions, "decal_puddle", DrawPuddle);
        AddDecal(definitions, "decal_manhole", DrawManhole);
        AddDecal(definitions, "decal_blood", DrawBlood);
        AddDecal(definitions, "decal_graffiti", DrawGraffiti);
        AddDecal(definitions, "decal_paper", DrawPaper);
        AddDecal(definitions, "decal_tire_marks", DrawTireMarks);

        AddProp(definitions, "prop_car", 512, 256, DrawCar);
        AddProp(definitions, "prop_dumpster", 256, 256, DrawDumpster);
        AddProp(definitions, "prop_barricade", 256, 128, DrawBarricade);
        AddProp(definitions, "prop_cone", 128, 128, DrawCone);
        AddProp(definitions, "prop_sandbags", 256, 256, DrawSandbags);
        AddProp(definitions, "prop_cables", 256, 256, DrawCables);
        AddProp(definitions, "prop_streetlight", 256, 512, DrawStreetlight);
        AddProp(definitions, "prop_neon_sign", 256, 512, DrawNeonSign);

        AddPerimeter(definitions, "wall_fence_h", 256, 64, canvas => DrawFence(canvas, 256, 64, true));
        AddPerimeter(definitions, "wall_fence_v", 64, 256, canvas => DrawFence(canvas, 64, 256, false));
        AddPerimeter(definitions, "wall_concrete_h", 256, 64, canvas => DrawConcreteBarrier(canvas, 256, 64, true));
        AddPerimeter(definitions, "wall_concrete_v", 64, 256, canvas => DrawConcreteBarrier(canvas, 64, 256, false));
        AddPerimeter(definitions, "wall_storefront", 512, 256, DrawStorefront);

        AddLightPool(definitions, "light_cyan", Palette.Cyan);
        AddLightPool(definitions, "light_magenta", Palette.Magenta);
        AddLightPool(definitions, "light_amber", Palette.Amber);
        AddLightPool(definitions, "light_green", Palette.Green);
    }

    private static void AddFloor(
        ICollection<SpriteDefinition> definitions,
        string key,
        int size,
        SKColor baseColor,
        int seed)
    {
        definitions.Add(new SpriteDefinition(
            $"Environment/{key}.png", size, size, 0.5f, 0.5f, "environment",
            canvas => DrawFloor(canvas, size, baseColor, seed)));
    }

    private static void AddDecal(
        ICollection<SpriteDefinition> definitions,
        string key,
        Action<SKCanvas> draw)
    {
        definitions.Add(new SpriteDefinition(
            $"Environment/{key}.png", 256, 256, 0.5f, 0.5f, "environment",
            draw));
    }

    private static void AddProp(
        ICollection<SpriteDefinition> definitions,
        string key,
        int width,
        int height,
        Action<SKCanvas> draw)
    {
        definitions.Add(new SpriteDefinition(
            $"Environment/{key}.png", width, height, 0.5f, 0.5f, "environment",
            draw));
    }

    private static void AddPerimeter(
        ICollection<SpriteDefinition> definitions,
        string key,
        int width,
        int height,
        Action<SKCanvas> draw)
    {
        definitions.Add(new SpriteDefinition(
            $"Environment/{key}.png", width, height, 0.5f, 0.5f, "environment",
            draw));
    }

    private static void AddLightPool(
        ICollection<SpriteDefinition> definitions,
        string key,
        SKColor color)
    {
        definitions.Add(new SpriteDefinition(
            $"Environment/{key}.png", 256, 256, 0.5f, 0.5f, "environment",
            canvas => DrawLightPool(canvas, 256, color)));
    }

    private static void DrawFloor(SKCanvas canvas, int size, SKColor baseColor, int seed)
    {
        using var background = Draw.Fill(baseColor);
        canvas.DrawRect(new SKRect(0, 0, size, size), background);

        var random = new Random(seed);
        using var speck = Draw.Fill(Palette.White.WithAlpha(10));
        for (var i = 0; i < 150; i++)
        {
            var x = random.Next(8, size - 8);
            var y = random.Next(8, size - 8);
            var radius = random.Next(1, 4);
            canvas.DrawCircle(x, y, radius, speck);
        }

        using var stain = Draw.Fill(Palette.Background.WithAlpha(70));
        for (var i = 0; i < 12; i++)
        {
            var x = random.Next(30, size - 30);
            var y = random.Next(30, size - 30);
            canvas.DrawOval(x, y, random.Next(12, 38), random.Next(8, 22), stain);
        }

        using var crack = Draw.Stroke(Palette.FloorGrid, 2f);
        for (var i = 0; i < 5; i++)
        {
            var x = random.Next(30, size - 30);
            var y = random.Next(30, size - 30);
            canvas.DrawLine(x, y, x + random.Next(-30, 30), y + random.Next(-30, 30), crack);
        }

        if (size >= 512)
        {
            using var seam = Draw.Stroke(Palette.FloorGrid.WithAlpha(90), 2f);
            canvas.DrawLine(0, 128, size, 128, seam);
            canvas.DrawLine(0, 384, size, 384, seam);
            canvas.DrawLine(128, 0, 128, size, seam);
            canvas.DrawLine(384, 0, 384, size, seam);
        }

        DrawGlowAccent(canvas, size);
    }

    private static void DrawGlowAccent(SKCanvas canvas, int size)
    {
        Draw.GlowLine(canvas, 20, size - 36, size * 0.32f, size - 80, Palette.Cyan.WithAlpha(24), 2f, 25);
        Draw.GlowLine(canvas, size * 0.62f, 58, size - 24, 30, Palette.Magenta.WithAlpha(20), 2f, 20);
    }

    private static void DrawRoadLine(SKCanvas canvas)
    {
        Draw.RoundedRect(canvas, 24, 112, 232, 132, 6, Palette.Amber.WithAlpha(160), Palette.Outline, 3f);
        Draw.RoundedRect(canvas, 24, 132, 232, 146, 4, Palette.Amber.WithAlpha(70), Palette.Outline, 2f);
    }

    private static void DrawRoadArrow(SKCanvas canvas)
    {
        Draw.Polygon(
            canvas,
            new[]
            {
                new SKPoint(128, 42),
                new SKPoint(206, 118),
                new SKPoint(158, 118),
                new SKPoint(158, 210),
                new SKPoint(98, 210),
                new SKPoint(98, 118),
                new SKPoint(50, 118)
            },
            Palette.Amber.WithAlpha(130),
            Palette.Outline,
            3f);
    }

    private static void DrawCrack(SKCanvas canvas)
    {
        using var paint = Draw.Stroke(Palette.Outline, 4f);
        canvas.DrawLine(24, 190, 92, 144, paint);
        canvas.DrawLine(92, 144, 128, 166, paint);
        canvas.DrawLine(128, 166, 180, 92, paint);
        canvas.DrawLine(128, 166, 122, 226, paint);
        using var highlight = Draw.Stroke(Palette.Steel.WithAlpha(80), 1.5f);
        canvas.DrawLine(30, 186, 90, 148, highlight);
    }

    private static void DrawOil(SKCanvas canvas)
    {
        using var shader = SKShader.CreateRadialGradient(
            new SKPoint(128, 128),
            88,
            new[]
            {
                new SKColor(8, 12, 20, 180),
                new SKColor(12, 28, 40, 90),
                new SKColor(0, 0, 0, 0)
            },
            new[] { 0f, 0.6f, 1f },
            SKShaderTileMode.Clamp);
        using var paint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Shader = shader };
        canvas.DrawOval(128, 128, 96, 72, paint);
        Draw.GlowLine(canvas, 72, 144, 154, 96, Palette.Cyan.WithAlpha(70), 2f, 30);
    }

    private static void DrawPuddle(SKCanvas canvas)
    {
        using var shader = SKShader.CreateRadialGradient(
            new SKPoint(128, 128),
            92,
            new[]
            {
                new SKColor(40, 120, 150, 90),
                new SKColor(30, 70, 100, 45),
                new SKColor(0, 0, 0, 0)
            },
            new[] { 0f, 0.7f, 1f },
            SKShaderTileMode.Clamp);
        using var paint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Shader = shader };
        canvas.DrawOval(128, 128, 104, 68, paint);
        Draw.GlowLine(canvas, 58, 118, 176, 100, Palette.Cyan.WithAlpha(80), 2f, 40);
        Draw.GlowLine(canvas, 82, 156, 190, 146, Palette.Magenta.WithAlpha(60), 2f, 30);
    }

    private static void DrawManhole(SKCanvas canvas)
    {
        Draw.Ellipse(canvas, 128, 128, 66, 66, Palette.Steel, Palette.Outline, 8f);
        Draw.Ellipse(canvas, 128, 128, 52, 52, Palette.FloorGrid, Palette.Outline, 4f);
        for (var i = 0; i < 8; i++)
        {
            var angle = (MathF.PI * 2f * i) / 8f;
            Draw.GlowLine(
                canvas,
                128 + (MathF.Cos(angle) * 24),
                128 + (MathF.Sin(angle) * 24),
                128 + (MathF.Cos(angle) * 50),
                128 + (MathF.Sin(angle) * 50),
                Palette.Cyan.WithAlpha(60),
                2f,
                20);
        }
    }

    private static void DrawBlood(SKCanvas canvas)
    {
        Draw.Ellipse(canvas, 118, 132, 38, 24, Palette.Red.WithAlpha(90));
        Draw.Ellipse(canvas, 152, 154, 16, 12, Palette.Red.WithAlpha(75));
        Draw.Ellipse(canvas, 102, 166, 10, 8, Palette.Red.WithAlpha(60));
    }

    private static void DrawGraffiti(SKCanvas canvas)
    {
        Draw.GlowLine(canvas, 36, 70, 98, 42, Palette.Magenta.WithAlpha(180), 6f, 50);
        Draw.GlowLine(canvas, 98, 42, 150, 98, Palette.Magenta.WithAlpha(180), 6f, 50);
        Draw.GlowLine(canvas, 150, 98, 210, 58, Palette.Cyan.WithAlpha(180), 5f, 50);
        Draw.GlowLine(canvas, 70, 148, 190, 148, Palette.Amber.WithAlpha(140), 4f, 40);
    }

    private static void DrawPaper(SKCanvas canvas)
    {
        Draw.Polygon(
            canvas,
            new[]
            {
                new SKPoint(76, 96),
                new SKPoint(166, 82),
                new SKPoint(180, 142),
                new SKPoint(92, 158)
            },
            Palette.White.WithAlpha(140),
            Palette.Outline,
            3f);
        Draw.GlowLine(canvas, 98, 112, 154, 104, Palette.Steel.WithAlpha(90), 2f, 20);
        Draw.GlowLine(canvas, 100, 128, 160, 120, Palette.Steel.WithAlpha(70), 2f, 20);
    }

    private static void DrawTireMarks(SKCanvas canvas)
    {
        using var paint = Draw.Stroke(Palette.Background.WithAlpha(120), 10f);
        canvas.DrawLine(48, 190, 196, 78, paint);
        canvas.DrawLine(68, 210, 216, 98, paint);
        using var faint = Draw.Stroke(Palette.FloorGrid.WithAlpha(90), 2f);
        canvas.DrawLine(52, 188, 196, 82, faint);
        canvas.DrawLine(72, 208, 212, 102, faint);
    }

    private static void DrawCar(SKCanvas canvas)
    {
        Draw.SoftShadow(canvas, 256, 146, 210, 72);
        Draw.RoundedRect(canvas, 44, 58, 468, 198, 44, Palette.Steel, Palette.Outline, 9f);
        Draw.RoundedRect(canvas, 86, 74, 430, 182, 30, Palette.FloorGrid, Palette.Outline, 6f);
        Draw.RoundedRect(canvas, 150, 84, 250, 172, 22, Palette.Cyan.WithAlpha(35), Palette.Outline, 4f);
        Draw.RoundedRect(canvas, 268, 84, 366, 172, 22, Palette.Cyan.WithAlpha(25), Palette.Outline, 4f);
        Draw.GlowLine(canvas, 72, 86, 72, 170, Palette.Cyan, 4f, 90);
        Draw.GlowLine(canvas, 440, 86, 440, 170, Palette.Red, 4f, 80);
        Draw.Ellipse(canvas, 116, 72, 18, 10, Palette.Outline);
        Draw.Ellipse(canvas, 396, 72, 18, 10, Palette.Outline);
        Draw.Ellipse(canvas, 116, 184, 18, 10, Palette.Outline);
        Draw.Ellipse(canvas, 396, 184, 18, 10, Palette.Outline);
    }

    private static void DrawDumpster(SKCanvas canvas)
    {
        Draw.SoftShadow(canvas, 128, 186, 84, 28);
        Draw.RoundedRect(canvas, 46, 62, 210, 184, 16, Palette.Green.WithAlpha(80), Palette.Outline, 8f);
        Draw.RoundedRect(canvas, 54, 52, 202, 82, 8, Palette.Steel, Palette.Outline, 5f);
        Draw.GlowLine(canvas, 64, 74, 192, 74, Palette.Green, 4f, 80);
        Draw.RoundedRect(canvas, 66, 100, 190, 154, 8, Palette.FloorGrid, Palette.Outline, 4f);
        Draw.Rivet(canvas, 70, 174, Palette.Green);
        Draw.Rivet(canvas, 186, 174, Palette.Green);
    }

    private static void DrawBarricade(SKCanvas canvas)
    {
        Draw.SoftShadow(canvas, 128, 104, 96, 16);
        Draw.RoundedRect(canvas, 20, 36, 236, 92, 8, Palette.Amber.WithAlpha(120), Palette.Outline, 6f);
        for (var i = 0; i < 4; i++)
        {
            Draw.GlowLine(canvas, 34 + (i * 56), 44, 56 + (i * 56), 84, Palette.Outline, 8f, 100);
        }

        Draw.RoundedRect(canvas, 42, 88, 56, 118, 4, Palette.Steel, Palette.Outline, 4f);
        Draw.RoundedRect(canvas, 200, 88, 214, 118, 4, Palette.Steel, Palette.Outline, 4f);
    }

    private static void DrawCone(SKCanvas canvas)
    {
        Draw.SoftShadow(canvas, 64, 102, 30, 12);
        Draw.Polygon(
            canvas,
            new[]
            {
                new SKPoint(64, 20),
                new SKPoint(92, 96),
                new SKPoint(36, 96)
            },
            Palette.Orange,
            Palette.Outline,
            5f);
        Draw.RoundedRect(canvas, 28, 94, 100, 112, 5, Palette.Outline, Palette.Outline, 4f);
        Draw.GlowLine(canvas, 46, 68, 82, 68, Palette.White, 4f, 60);
    }

    private static void DrawSandbags(SKCanvas canvas)
    {
        Draw.SoftShadow(canvas, 128, 200, 90, 22);
        for (var row = 0; row < 3; row++)
        {
            for (var column = 0; column < 4 - row; column++)
            {
                var x = 58 + (column * 44) + (row * 22);
                var y = 170 - (row * 38);
                Draw.Ellipse(canvas, x, y, 28, 18, Palette.Amber.WithAlpha(80), Palette.Outline, 4f);
                Draw.GlowLine(canvas, x - 16, y - 2, x + 16, y - 2, Palette.Amber.WithAlpha(70), 2f, 30);
            }
        }
    }

    private static void DrawCables(SKCanvas canvas)
    {
        using var builder = new SKPathBuilder();
        builder.MoveTo(18, 190);
        builder.CubicTo(72, 120, 118, 214, 172, 136);
        builder.CubicTo(202, 94, 220, 116, 238, 78);
        using var path = builder.Detach();
        using var glow = Draw.Glow(Palette.Cyan.WithAlpha(60), 9f, 30);
        canvas.DrawPath(path, glow);
        using var line = Draw.Stroke(Palette.Steel, 4f);
        canvas.DrawPath(path, line);
        Draw.Ellipse(canvas, 18, 190, 8, 8, Palette.Cyan, Palette.Outline, 3f);
        Draw.Ellipse(canvas, 238, 78, 8, 8, Palette.Cyan, Palette.Outline, 3f);
    }

    private static void DrawStreetlight(SKCanvas canvas)
    {
        Draw.SoftShadow(canvas, 128, 492, 54, 14);
        Draw.RoundedRect(canvas, 114, 80, 142, 486, 10, Palette.Steel, Palette.Outline, 6f);
        Draw.RoundedRect(canvas, 86, 42, 198, 82, 10, Palette.FloorGrid, Palette.Outline, 5f);
        DrawRadialGlow(canvas, 142, 82, 84, Palette.Cyan, 70);
        Draw.GlowLine(canvas, 102, 60, 182, 60, Palette.Cyan, 4f, 100);
    }

    private static void DrawNeonSign(SKCanvas canvas)
    {
        Draw.RoundedRect(canvas, 42, 36, 214, 476, 18, Palette.FloorGrid, Palette.Outline, 8f);
        DrawRadialGlow(canvas, 128, 120, 96, Palette.Magenta, 60);
        Draw.GlowLine(canvas, 78, 92, 178, 92, Palette.Magenta, 8f, 120);
        Draw.GlowLine(canvas, 78, 122, 178, 122, Palette.Cyan, 8f, 120);
        Draw.GlowLine(canvas, 78, 152, 178, 152, Palette.Amber, 8f, 120);
        Draw.RoundedRect(canvas, 74, 210, 182, 300, 8, Palette.Cyan.WithAlpha(30), Palette.Cyan, 4f);
    }

    private static void DrawFence(SKCanvas canvas, int width, int height, bool horizontal)
    {
        using var background = Draw.Fill(Palette.Floor.WithAlpha(220));
        canvas.DrawRect(new SKRect(0, 0, width, height), background);
        using var mesh = Draw.Stroke(Palette.Steel.WithAlpha(120), 2f);
        if (horizontal)
        {
            for (var x = 0; x < width; x += 24)
            {
                canvas.DrawLine(x, 6, x + 16, height - 6, mesh);
                canvas.DrawLine(x + 16, 6, x, height - 6, mesh);
            }

            Draw.GlowLine(canvas, 0, 8, width, 8, Palette.Cyan.WithAlpha(80), 4f, 50);
            Draw.GlowLine(canvas, 0, height - 8, width, height - 8, Palette.Steel, 3f, 40);
        }
        else
        {
            for (var y = 0; y < height; y += 24)
            {
                canvas.DrawLine(6, y, width - 6, y + 16, mesh);
                canvas.DrawLine(width - 6, y, 6, y + 16, mesh);
            }

            Draw.GlowLine(canvas, width - 8, 0, width - 8, height, Palette.Cyan.WithAlpha(80), 4f, 50);
            Draw.GlowLine(canvas, 8, 0, 8, height, Palette.Steel, 3f, 40);
        }
    }

    private static void DrawConcreteBarrier(SKCanvas canvas, int width, int height, bool horizontal)
    {
        using var background = Draw.Fill(Palette.Steel);
        canvas.DrawRect(new SKRect(0, 0, width, height), background);
        using var band = Draw.Fill(Palette.Amber.WithAlpha(140));
        if (horizontal)
        {
            for (var x = -height; x < width; x += 56)
            {
                using var builder = new SKPathBuilder();
                builder.MoveTo(x, height);
                builder.LineTo(x + height, 0);
                builder.LineTo(x + height + 28, 0);
                builder.LineTo(x + 28, height);
                builder.Close();
                using var path = builder.Detach();
                canvas.DrawPath(path, band);
            }
        }
        else
        {
            for (var y = -height; y < height; y += 56)
            {
                using var builder = new SKPathBuilder();
                builder.MoveTo(0, y);
                builder.LineTo(width, y + width);
                builder.LineTo(width, y + width + 28);
                builder.LineTo(0, y + 28);
                builder.Close();
                using var path = builder.Detach();
                canvas.DrawPath(path, band);
            }
        }

        using var outline = Draw.Stroke(Palette.Outline, 6f);
        canvas.DrawRect(new SKRect(0, 0, width, height), outline);
    }

    private static void DrawStorefront(SKCanvas canvas)
    {
        Draw.RoundedRect(canvas, 20, 20, 492, 236, 12, Palette.FloorGrid, Palette.Outline, 8f);
        Draw.RoundedRect(canvas, 42, 42, 250, 214, 8, Palette.Cyan.WithAlpha(24), Palette.Cyan, 4f);
        Draw.RoundedRect(canvas, 270, 42, 470, 214, 8, Palette.Magenta.WithAlpha(22), Palette.Magenta, 4f);
        Draw.GlowLine(canvas, 56, 72, 236, 72, Palette.Cyan, 5f, 80);
        Draw.GlowLine(canvas, 284, 72, 456, 72, Palette.Magenta, 5f, 80);
        Draw.GlowLine(canvas, 56, 190, 236, 190, Palette.Amber, 4f, 60);
        Draw.GlowLine(canvas, 284, 190, 456, 190, Palette.Green, 4f, 60);
    }

    private static void DrawLightPool(SKCanvas canvas, int size, SKColor color)
    {
        DrawRadialGlow(canvas, size / 2f, size / 2f, size * 0.46f, color, 60);
    }

    private static void DrawRadialGlow(
        SKCanvas canvas,
        float centerX,
        float centerY,
        float radius,
        SKColor color,
        byte alpha)
    {
        using var shader = SKShader.CreateRadialGradient(
            new SKPoint(centerX, centerY),
            radius,
            new[]
            {
                color.WithAlpha(alpha),
                color.WithAlpha((byte)(alpha * 0.55f)),
                color.WithAlpha(0)
            },
            new[] { 0f, 0.55f, 1f },
            SKShaderTileMode.Clamp);
        using var paint = new SKPaint { IsAntialias = true, Style = SKPaintStyle.Fill, Shader = shader };
        canvas.DrawOval(centerX, centerY, radius, radius, paint);
    }

    private static void DrawPlayerPart(
        SKCanvas canvas,
        float center,
        float scale,
        CharacterPalette palette,
        CharacterShape shape,
        CharacterPart part)
    {
        switch (part)
        {
            case CharacterPart.Shadow:
                Draw.SoftShadow(
                    canvas,
                    center + (4f * scale),
                    center + (18f * scale),
                    96f * scale,
                    48f * scale);
                break;
            case CharacterPart.Leg:
                DrawPlayerLeg(canvas, center, scale, palette, shape);
                break;
            case CharacterPart.Torso:
                DrawPlayerTorso(canvas, center, scale, palette, shape);
                break;
            case CharacterPart.Head:
                DrawPlayerHead(canvas, center, scale, palette, shape);
                break;
            case CharacterPart.Arm:
                DrawPlayerArm(canvas, center, scale, palette, shape);
                break;
        }
    }

    private static void DrawPlayerTorso(
        SKCanvas canvas,
        float center,
        float scale,
        CharacterPalette palette,
        CharacterShape shape)
    {
        var depth = shape.TorsoWidth * scale;
        var width = shape.TorsoHeight * scale;
        var left = center - (depth / 2f);
        var right = center + (depth / 2f);
        var top = center - (width / 2f) + (shape.TorsoOffsetY * scale);
        var bottom = center + (width / 2f) + (shape.TorsoOffsetY * scale);
        var outline = 5f * scale;

        Draw.RoundedRect(
            canvas,
            left - (8f * scale),
            top + (38f * scale),
            left + (34f * scale),
            bottom - (38f * scale),
            12f * scale,
            palette.Plate,
            Palette.Outline,
            outline);

        Draw.Polygon(
            canvas,
            new[]
            {
                new SKPoint(left + (12f * scale), top + (8f * scale)),
                new SKPoint(center - (18f * scale), top),
                new SKPoint(right - (22f * scale), top + (10f * scale)),
                new SKPoint(right, center - (42f * scale)),
                new SKPoint(right + (4f * scale), center),
                new SKPoint(right, center + (42f * scale)),
                new SKPoint(right - (22f * scale), bottom - (10f * scale)),
                new SKPoint(center - (18f * scale), bottom),
                new SKPoint(left + (12f * scale), bottom - (8f * scale)),
                new SKPoint(left - (4f * scale), center + (50f * scale)),
                new SKPoint(left - (4f * scale), center - (50f * scale))
            },
            palette.Body,
            Palette.Outline,
            outline);

        for (var side = -1; side <= 1; side += 2)
        {
            Draw.Polygon(
                canvas,
                new[]
                {
                    new SKPoint(left + (10f * scale), center + (side * 78f * scale)),
                    new SKPoint(center - (14f * scale), center + (side * 86f * scale)),
                    new SKPoint(center + (22f * scale), center + (side * 78f * scale)),
                    new SKPoint(center + (34f * scale), center + (side * 54f * scale)),
                    new SKPoint(center + (18f * scale), center + (side * 32f * scale)),
                    new SKPoint(left + (14f * scale), center + (side * 36f * scale)),
                    new SKPoint(left, center + (side * 56f * scale))
                },
                palette.Plate,
                Palette.Outline,
                outline);
            Draw.Polygon(
                canvas,
                new[]
                {
                    new SKPoint(left + (20f * scale), center + (side * 68f * scale)),
                    new SKPoint(center - (10f * scale), center + (side * 76f * scale)),
                    new SKPoint(center + (12f * scale), center + (side * 69f * scale)),
                    new SKPoint(center + (19f * scale), center + (side * 57f * scale)),
                    new SKPoint(left + (22f * scale), center + (side * 51f * scale))
                },
                palette.Accent,
                Palette.Outline,
                3f * scale);
        }

        Draw.Polygon(
            canvas,
            new[]
            {
                new SKPoint(center - (20f * scale), center - (50f * scale)),
                new SKPoint(right - (26f * scale), center - (38f * scale)),
                new SKPoint(right + (2f * scale), center),
                new SKPoint(right - (26f * scale), center + (38f * scale)),
                new SKPoint(center - (20f * scale), center + (50f * scale)),
                new SKPoint(center - (36f * scale), center + (28f * scale)),
                new SKPoint(center - (36f * scale), center - (28f * scale))
            },
            Palette.Steel,
            Palette.Outline,
            outline);

        Draw.Polygon(
            canvas,
            new[]
            {
                new SKPoint(center - (24f * scale), center - (10f * scale)),
                new SKPoint(center + (18f * scale), center - (10f * scale)),
                new SKPoint(center + (40f * scale), center),
                new SKPoint(center + (18f * scale), center + (10f * scale)),
                new SKPoint(center - (24f * scale), center + (10f * scale)),
                new SKPoint(center - (10f * scale), center)
            },
            palette.Accent,
            Palette.Outline,
            3f * scale);

        using var rim = Draw.Stroke(Palette.Ice.WithAlpha(230), 3f * scale);
        canvas.DrawLine(
            left + (18f * scale),
            center - (72f * scale),
            center - (12f * scale),
            center - (80f * scale),
            rim);
        canvas.DrawLine(
            center - (8f * scale),
            center - (79f * scale),
            center + (18f * scale),
            center - (73f * scale),
            rim);
        canvas.DrawLine(
            center - (14f * scale),
            center - (44f * scale),
            right - (28f * scale),
            center - (33f * scale),
            rim);

        using var visorCore = Draw.Stroke(Palette.Ice, 3f * scale);
        canvas.DrawLine(
            center - (10f * scale),
            center,
            center + (20f * scale),
            center,
            visorCore);
        Draw.Rivet(canvas, left + (18f * scale), center - (22f * scale), Palette.Steel);
        Draw.Rivet(canvas, left + (18f * scale), center + (22f * scale), Palette.Steel);
    }

    private static void DrawPlayerHead(
        SKCanvas canvas,
        float center,
        float scale,
        CharacterPalette palette,
        CharacterShape shape)
    {
        var radius = shape.HeadRadius * scale;
        var outline = 5f * scale;

        Draw.Polygon(
            canvas,
            new[]
            {
                new SKPoint(center - (radius * 1.05f), center - (radius * 0.55f)),
                new SKPoint(center - (radius * 0.45f), center - radius),
                new SKPoint(center + (radius * 0.45f), center - (radius * 0.90f)),
                new SKPoint(center + (radius * 0.95f), center - (radius * 0.45f)),
                new SKPoint(center + (radius * 1.10f), center),
                new SKPoint(center + (radius * 0.95f), center + (radius * 0.45f)),
                new SKPoint(center + (radius * 0.45f), center + (radius * 0.90f)),
                new SKPoint(center - (radius * 0.45f), center + radius),
                new SKPoint(center - (radius * 1.05f), center + (radius * 0.55f)),
                new SKPoint(center - (radius * 1.15f), center)
            },
            palette.Body,
            Palette.Outline,
            outline);

        Draw.Polygon(
            canvas,
            new[]
            {
                new SKPoint(center - (radius * 0.70f), center - (radius * 0.45f)),
                new SKPoint(center - (radius * 0.20f), center - (radius * 0.72f)),
                new SKPoint(center + (radius * 0.30f), center - (radius * 0.60f)),
                new SKPoint(center + (radius * 0.18f), center + (radius * 0.60f)),
                new SKPoint(center - (radius * 0.20f), center + (radius * 0.72f)),
                new SKPoint(center - (radius * 0.70f), center + (radius * 0.45f))
            },
            palette.Plate,
            Palette.Outline,
            3f * scale);

        Draw.Polygon(
            canvas,
            new[]
            {
                new SKPoint(center + (radius * 0.05f), center - (radius * 0.62f)),
                new SKPoint(center + (radius * 0.76f), center - (radius * 0.46f)),
                new SKPoint(center + (radius * 1.08f), center - (radius * 0.16f)),
                new SKPoint(center + (radius * 1.08f), center + (radius * 0.16f)),
                new SKPoint(center + (radius * 0.76f), center + (radius * 0.46f)),
                new SKPoint(center + (radius * 0.05f), center + (radius * 0.62f))
            },
            palette.Accent,
            Palette.Outline,
            outline);
        Draw.Polygon(
            canvas,
            new[]
            {
                new SKPoint(center + (radius * 0.24f), center - (radius * 0.39f)),
                new SKPoint(center + (radius * 0.72f), center - (radius * 0.28f)),
                new SKPoint(center + (radius * 0.88f), center - (radius * 0.12f)),
                new SKPoint(center + (radius * 0.30f), center - (radius * 0.13f))
            },
            palette.Visor);

        using var rim = Draw.Stroke(Palette.Ice.WithAlpha(230), 3f * scale);
        canvas.DrawLine(
            center - (radius * 0.82f),
            center - (radius * 0.52f),
            center - (radius * 0.38f),
            center - (radius * 0.88f),
            rim);
        canvas.DrawLine(
            center - (radius * 0.32f),
            center - (radius * 0.88f),
            center + (radius * 0.26f),
            center - (radius * 0.79f),
            rim);
        canvas.DrawLine(
            center + (radius * 0.28f),
            center - (radius * 0.37f),
            center + (radius * 0.67f),
            center - (radius * 0.27f),
            rim);
    }

    private static void DrawPlayerArm(
        SKCanvas canvas,
        float center,
        float scale,
        CharacterPalette palette,
        CharacterShape shape)
    {
        var length = shape.ArmLength * scale;
        var thickness = shape.ArmThickness * scale;
        var left = center - (78f * scale);
        var right = left + length;
        var elbowX = left + (length * 0.46f);
        var outline = 5f * scale;

        Draw.RoundedRect(
            canvas,
            left,
            center - (thickness / 2f),
            elbowX + (8f * scale),
            center + (thickness / 2f),
            thickness * 0.42f,
            palette.Body,
            Palette.Outline,
            outline);
        Draw.Polygon(
            canvas,
            new[]
            {
                new SKPoint(elbowX - (4f * scale), center - (thickness * 0.54f)),
                new SKPoint(right - (20f * scale), center - (thickness * 0.46f)),
                new SKPoint(right - (8f * scale), center - (thickness * 0.22f)),
                new SKPoint(right - (8f * scale), center + (thickness * 0.22f)),
                new SKPoint(right - (20f * scale), center + (thickness * 0.46f)),
                new SKPoint(elbowX - (4f * scale), center + (thickness * 0.54f))
            },
            palette.Plate,
            Palette.Outline,
            outline);
        Draw.Ellipse(
            canvas,
            elbowX,
            center,
            thickness * 0.48f,
            thickness * 0.52f,
            Palette.Steel,
            Palette.Outline,
            4f * scale);
        Draw.Ellipse(
            canvas,
            left + (5f * scale),
            center,
            thickness * 0.78f,
            thickness * 0.84f,
            palette.Plate,
            Palette.Outline,
            outline);
        Draw.RoundedRect(
            canvas,
            right - (24f * scale),
            center - (thickness * 0.50f),
            right + (4f * scale),
            center + (thickness * 0.50f),
            8f * scale,
            palette.Body,
            Palette.Outline,
            outline);
        Draw.Ellipse(
            canvas,
            right + (7f * scale),
            center,
            thickness * 0.42f,
            thickness * 0.38f,
            Palette.Steel,
            Palette.Outline,
            4f * scale);

        Draw.GlowLine(
            canvas,
            left + (4f * scale),
            center - (thickness * 0.46f),
            left + (4f * scale),
            center + (thickness * 0.46f),
            palette.Accent,
            4f * scale,
            65);
        Draw.GlowLine(
            canvas,
            elbowX + (16f * scale),
            center,
            right - (28f * scale),
            center,
            palette.Accent,
            4f * scale,
            65);
        using var rim = Draw.Stroke(Palette.Ice.WithAlpha(220), 3f * scale);
        canvas.DrawLine(
            elbowX + (8f * scale),
            center - (thickness * 0.40f),
            right - (24f * scale),
            center - (thickness * 0.34f),
            rim);
    }

    private static void DrawPlayerLeg(
        SKCanvas canvas,
        float center,
        float scale,
        CharacterPalette palette,
        CharacterShape shape)
    {
        var length = shape.LegLength * scale;
        var thickness = shape.LegWidth * scale;
        var hipX = center + (90f * scale);
        var footX = hipX - length;
        var kneeX = footX + (length * 0.58f);
        var top = center - (thickness / 2f);
        var bottom = center + (thickness / 2f);
        var outline = 5f * scale;

        Draw.RoundedRect(
            canvas,
            footX + (12f * scale),
            top,
            hipX,
            bottom,
            thickness * 0.42f,
            palette.Body,
            Palette.Outline,
            outline);
        Draw.RoundedRect(
            canvas,
            kneeX - (4f * scale),
            top - (2f * scale),
            hipX - (4f * scale),
            bottom + (2f * scale),
            thickness * 0.34f,
            palette.Plate,
            Palette.Outline,
            outline);
        Draw.Polygon(
            canvas,
            new[]
            {
                new SKPoint(footX - (10f * scale), center - (thickness * 0.66f)),
                new SKPoint(footX + (34f * scale), center - (thickness * 0.62f)),
                new SKPoint(footX + (48f * scale), center - (thickness * 0.34f)),
                new SKPoint(footX + (48f * scale), center + (thickness * 0.34f)),
                new SKPoint(footX + (34f * scale), center + (thickness * 0.62f)),
                new SKPoint(footX - (10f * scale), center + (thickness * 0.66f))
            },
            palette.Plate,
            Palette.Outline,
            outline);
        Draw.RoundedRect(
            canvas,
            footX - (7f * scale),
            center - (thickness * 0.50f),
            footX + (15f * scale),
            center + (thickness * 0.50f),
            7f * scale,
            Palette.Steel,
            Palette.Outline,
            4f * scale);
        Draw.Ellipse(
            canvas,
            kneeX,
            center,
            thickness * 0.56f,
            thickness * 0.66f,
            palette.Plate,
            Palette.Outline,
            outline);

        Draw.GlowLine(
            canvas,
            footX + (22f * scale),
            center,
            kneeX - (18f * scale),
            center,
            palette.Accent,
            4f * scale,
            60);
        Draw.GlowLine(
            canvas,
            kneeX + (18f * scale),
            center,
            hipX - (20f * scale),
            center,
            palette.Accent,
            4f * scale,
            60);
        using var rim = Draw.Stroke(Palette.Ice.WithAlpha(220), 3f * scale);
        canvas.DrawLine(
            footX + (4f * scale),
            center - (thickness * 0.52f),
            footX + (32f * scale),
            center - (thickness * 0.50f),
            rim);
        canvas.DrawLine(
            kneeX + (8f * scale),
            top - (1f * scale),
            hipX - (18f * scale),
            top - (1f * scale),
            rim);
    }

    private static void DrawTopDownLeg(
        SKCanvas canvas,
        float center,
        float scale,
        CharacterPalette palette,
        CharacterShape shape)
    {
        var length = shape.LegLength * scale;
        var thickness = shape.LegWidth * scale;
        var hipX = center + (86f * scale);
        var footX = Math.Max(16f * scale, hipX - length);
        var top = center - (thickness / 2f);
        var bottom = center + (thickness / 2f);

        Draw.RoundedRect(
            canvas,
            footX,
            top,
            hipX,
            bottom,
            thickness * 0.45f,
            palette.Body,
            Palette.Outline,
            7f * scale);
        Draw.RoundedRect(
            canvas,
            footX + (length * 0.42f),
            top - (2f * scale),
            footX + (length * 0.62f),
            bottom + (2f * scale),
            thickness * 0.35f,
            palette.Plate,
            Palette.Outline,
            5f * scale);
        Draw.RoundedRect(
            canvas,
            footX - (4f * scale),
            top - (5f * scale),
            footX + (24f * scale),
            bottom + (5f * scale),
            thickness * 0.35f,
            palette.Plate,
            Palette.Outline,
            5f * scale);
        Draw.GlowLine(
            canvas,
            footX + (10f * scale),
            center,
            hipX - (12f * scale),
            center,
            palette.Accent,
            2.5f * scale,
            55);
    }
    private static void DrawTopDownTorso(
        SKCanvas canvas,
        float center,
        float scale,
        CharacterPalette palette,
        CharacterShape shape)
    {
        var depth = shape.TorsoWidth * scale;
        var width = shape.TorsoHeight * scale;
        var left = center - (depth / 2f);
        var right = center + (depth / 2f);
        var top = center - (width / 2f);
        var bottom = center + (width / 2f);

        Draw.RoundedRect(
            canvas,
            left,
            top,
            right,
            bottom,
            width * 0.28f,
            palette.Body,
            Palette.Outline,
            8f * scale);
        Draw.Ellipse(
            canvas,
            center + (depth * 0.18f),
            center,
            depth * 0.24f,
            width * 0.23f,
            palette.Plate,
            Palette.Outline,
            5f * scale);

        var shoulderY = width * 0.38f;
        Draw.Ellipse(
            canvas,
            center - (depth * 0.08f),
            center - shoulderY,
            depth * 0.34f,
            width * 0.17f,
            palette.Plate,
            Palette.Outline,
            6f * scale);
        Draw.Ellipse(
            canvas,
            center - (depth * 0.08f),
            center + shoulderY,
            depth * 0.34f,
            width * 0.17f,
            palette.Plate,
            Palette.Outline,
            6f * scale);

        Draw.GlowLine(
            canvas,
            center - (depth * 0.30f),
            center,
            center + (depth * 0.36f),
            center,
            palette.Accent,
            3f * scale,
            70);
        Draw.GlowLine(
            canvas,
            center - (depth * 0.20f),
            center - shoulderY,
            center + (depth * 0.20f),
            center - shoulderY,
            palette.Accent.WithAlpha(80),
            2f * scale,
            40);
        Draw.GlowLine(
            canvas,
            center - (depth * 0.20f),
            center + shoulderY,
            center + (depth * 0.20f),
            center + shoulderY,
            palette.Accent.WithAlpha(80),
            2f * scale,
            40);

        if (shape.Ragged)
        {
            Draw.Polygon(
                canvas,
                new[]
                {
                    new SKPoint(left + (2f * scale), top + (4f * scale)),
                    new SKPoint(left - (16f * scale), top + (14f * scale)),
                    new SKPoint(left + (2f * scale), top + (24f * scale))
                },
                palette.Body,
                Palette.Outline,
                4f * scale);
            Draw.Polygon(
                canvas,
                new[]
                {
                    new SKPoint(left + (2f * scale), bottom - (24f * scale)),
                    new SKPoint(left - (14f * scale), bottom - (12f * scale)),
                    new SKPoint(left + (2f * scale), bottom - (2f * scale))
                },
                palette.Body,
                Palette.Outline,
                4f * scale);
        }
    }

    private static void DrawTopDownHead(
        SKCanvas canvas,
        float center,
        float scale,
        CharacterPalette palette,
        CharacterShape shape)
    {
        var radius = shape.HeadRadius * scale;
        Draw.Ellipse(canvas, center, center, radius * 1.05f, radius, palette.Body, Palette.Outline, 6f * scale);
        Draw.Ellipse(
            canvas,
            center + (radius * 0.28f),
            center,
            radius * 0.56f,
            radius * 0.68f,
            palette.Plate,
            Palette.Outline,
            4f * scale);
        Draw.GlowLine(
            canvas,
            center + (radius * 0.12f),
            center - (radius * 0.42f),
            center + (radius * 0.62f),
            center - (radius * 0.08f),
            palette.Accent,
            2.5f * scale,
            70);

        if (shape.Horns)
        {
            Draw.Polygon(
                canvas,
                new[]
                {
                    new SKPoint(center - (radius * 0.2f), center - (radius * 0.8f)),
                    new SKPoint(center + (radius * 0.28f), center - (radius * 1.45f)),
                    new SKPoint(center + (radius * 0.42f), center - (radius * 0.7f))
                },
                palette.Plate,
                Palette.Outline,
                4f * scale);
            Draw.Polygon(
                canvas,
                new[]
                {
                    new SKPoint(center - (radius * 0.2f), center + (radius * 0.8f)),
                    new SKPoint(center + (radius * 0.28f), center + (radius * 1.45f)),
                    new SKPoint(center + (radius * 0.42f), center + (radius * 0.7f))
                },
                palette.Plate,
                Palette.Outline,
                4f * scale);
        }
    }

    private static void DrawTopDownArm(
        SKCanvas canvas,
        float center,
        float scale,
        CharacterPalette palette,
        CharacterShape shape)
    {
        var length = shape.ArmLength * scale;
        var thickness = shape.ArmThickness * scale;
        var left = center - (70f * scale);
        var right = Math.Min(center + (118f * scale), left + length);

        Draw.RoundedRect(
            canvas,
            left,
            center - (thickness / 2f),
            right,
            center + (thickness / 2f),
            thickness * 0.45f,
            palette.Body,
            Palette.Outline,
            7f * scale);
        Draw.Ellipse(
            canvas,
            left + (10f * scale),
            center,
            thickness * 0.52f,
            thickness * 0.52f,
            palette.Plate,
            Palette.Outline,
            5f * scale);
        Draw.Ellipse(
            canvas,
            right - (6f * scale),
            center,
            thickness * 0.56f,
            thickness * 0.56f,
            palette.Plate,
            Palette.Outline,
            5f * scale);
        Draw.GlowLine(
            canvas,
            left + (16f * scale),
            center,
            right - (14f * scale),
            center,
            palette.Accent,
            2.5f * scale,
            55);
    }
}
