using SkiaSharp;

namespace JabaSurvivol.ArtGenerator;

internal static class Draw
{
    public static SKPaint Fill(SKColor color)
    {
        return new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Color = color
        };
    }

    public static SKPaint Stroke(SKColor color, float width)
    {
        return new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = width,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
            Color = color
        };
    }

    public static SKPaint Glow(SKColor color, float width, byte alpha = 80)
    {
        return new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = width,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
            Color = color.WithAlpha(alpha)
        };
    }

    public static void RoundedRect(
        SKCanvas canvas,
        float left,
        float top,
        float right,
        float bottom,
        float radius,
        SKColor fill,
        SKColor outline,
        float outlineWidth = 6f,
        SKColor? highlight = null)
    {
        using var builder = new SKPathBuilder();
        builder.AddRoundRect(new SKRect(left, top, right, bottom), radius, radius);
        using var path = builder.Detach();

        using var outlinePaint = Stroke(outline, outlineWidth);
        canvas.DrawPath(path, outlinePaint);

        using var fillPaint = Fill(fill);
        canvas.DrawPath(path, fillPaint);

        if (highlight is { } highlightColor)
        {
            using var highlightPaint = Stroke(highlightColor, 2.5f);
            canvas.DrawLine(left + radius, top + outlineWidth, right - radius, top + outlineWidth, highlightPaint);
        }
    }

    public static void Ellipse(
        SKCanvas canvas,
        float centerX,
        float centerY,
        float radiusX,
        float radiusY,
        SKColor fill,
        SKColor? outline = null,
        float outlineWidth = 6f)
    {
        if (outline is { } outlineColor)
        {
            using var outlinePaint = Stroke(outlineColor, outlineWidth);
            canvas.DrawOval(centerX, centerY, radiusX, radiusY, outlinePaint);
        }

        using var fillPaint = Fill(fill);
        canvas.DrawOval(centerX, centerY, radiusX, radiusY, fillPaint);
    }

    public static void Polygon(
        SKCanvas canvas,
        SKPoint[] points,
        SKColor fill,
        SKColor? outline = null,
        float outlineWidth = 6f)
    {
        using var builder = new SKPathBuilder();
        builder.MoveTo(points[0]);
        for (var i = 1; i < points.Length; i++)
        {
            builder.LineTo(points[i]);
        }

        builder.Close();
        using var path = builder.Detach();

        if (outline is { } outlineColor)
        {
            using var outlinePaint = Stroke(outlineColor, outlineWidth);
            canvas.DrawPath(path, outlinePaint);
        }

        using var fillPaint = Fill(fill);
        canvas.DrawPath(path, fillPaint);
    }

    public static void GlowLine(
        SKCanvas canvas,
        float x1,
        float y1,
        float x2,
        float y2,
        SKColor color,
        float width,
        byte glowAlpha = 85)
    {
        using var glowPaint = Glow(color, width + 6f, glowAlpha);
        canvas.DrawLine(x1, y1, x2, y2, glowPaint);

        using var linePaint = Stroke(color, width);
        canvas.DrawLine(x1, y1, x2, y2, linePaint);
    }

    public static void SoftShadow(
        SKCanvas canvas,
        float centerX,
        float centerY,
        float radiusX,
        float radiusY)
    {
        using var shader = SKShader.CreateRadialGradient(
            new SKPoint(centerX, centerY),
            Math.Max(radiusX, radiusY),
            new[]
            {
                new SKColor(0, 0, 0, 120),
                new SKColor(0, 0, 0, 70),
                new SKColor(0, 0, 0, 0)
            },
            new[] { 0f, 0.65f, 1f },
            SKShaderTileMode.Clamp);

        using var paint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Shader = shader
        };

        canvas.DrawOval(centerX, centerY, radiusX, radiusY, paint);
    }

    public static void GradientRect(
        SKCanvas canvas,
        SKRect rect,
        SKColor top,
        SKColor bottom,
        SKColor outline,
        float radius,
        float outlineWidth = 6f)
    {
        using var shader = SKShader.CreateLinearGradient(
            new SKPoint(rect.Left, rect.Top),
            new SKPoint(rect.Left, rect.Bottom),
            new[] { top, bottom },
            SKShaderTileMode.Clamp);

        using var builder = new SKPathBuilder();
        builder.AddRoundRect(rect, radius, radius);
        using var path = builder.Detach();

        using var outlinePaint = Stroke(outline, outlineWidth);
        canvas.DrawPath(path, outlinePaint);

        using var fillPaint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Shader = shader
        };
        canvas.DrawPath(path, fillPaint);

        fillPaint.Shader = null;
    }

    public static void Panel(SKCanvas canvas, SKRect rect, SKColor baseColor, SKColor accent)
    {
        GradientRect(canvas, rect, baseColor, baseColor.WithAlpha(210), Palette.Outline, 14f, 7f);
        using var accentPaint = Stroke(accent, 3f);
        canvas.DrawLine(rect.Left + 14f, rect.Top + 10f, rect.Right - 14f, rect.Top + 10f, accentPaint);
    }

    public static void Rivet(SKCanvas canvas, float x, float y, SKColor color)
    {
        Ellipse(canvas, x, y, 5f, 5f, color, Palette.Outline, 2f);
    }
}
