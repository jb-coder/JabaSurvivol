using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using SkiaSharp;

namespace JabaSurvivol.ArtGenerator;

public sealed class ArtManifest
{
    public int SchemaVersion { get; init; } = 1;

    public string PaletteVersion { get; init; } = Palette.Version;

    public List<ArtManifestEntry> Assets { get; init; } = new();
}

public sealed class ArtManifestEntry
{
    public required string Path { get; init; }

    public required string ResourcePath { get; init; }

    public int Width { get; init; }

    public int Height { get; init; }

    public float PivotX { get; init; }

    public float PivotY { get; init; }

    public required string Category { get; init; }

    public required string PaletteVersion { get; init; }

    public required string Sha256 { get; init; }
}

public static class ArtPipeline
{
    public const string DefaultOutputRoot = "unity/JabaSurvivol/Assets/Resources/Art";
    public const string DefaultManifestPath = "unity/JabaSurvivol/Assets/_Project/Art/art-manifest.json";
    public const string DefaultContactSheetPath = "artifacts/art/contact-sheet.png";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    private static readonly char[] NewLine = { '\n' };

    public static string FindRepositoryRoot(string? startPath = null)
    {
        var directory = new DirectoryInfo(startPath ?? Environment.CurrentDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "JabaSurvivol.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Unable to locate the repository root.");
    }

    public static ArtManifest Generate(string outputRoot, string manifestPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputRoot);
        ArgumentException.ThrowIfNullOrWhiteSpace(manifestPath);

        Directory.CreateDirectory(outputRoot);

        var definitions = Definitions.Create();
        var assets = new List<ArtManifestEntry>(definitions.Count);

        foreach (var definition in definitions.OrderBy(item => item.Path, StringComparer.Ordinal))
        {
            var fullPath = Path.Combine(outputRoot, definition.Path.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            using var bitmap = new SKBitmap(new SKImageInfo(
                definition.Width,
                definition.Height,
                SKColorType.Rgba8888,
                SKAlphaType.Premul));

            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.Transparent);
            definition.Draw(canvas);
            canvas.Flush();

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            var bytes = data.ToArray();
            File.WriteAllBytes(fullPath, bytes);

            assets.Add(new ArtManifestEntry
            {
                Path = definition.Path,
                ResourcePath = "Art/" + definition.Path[..^4],
                Width = definition.Width,
                Height = definition.Height,
                PivotX = definition.PivotX,
                PivotY = definition.PivotY,
                Category = definition.Category,
                PaletteVersion = Palette.Version,
                Sha256 = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()
            });
        }

        var manifest = new ArtManifest { Assets = assets };
        var manifestBytes = JsonSerializer.SerializeToUtf8Bytes(manifest, JsonOptions);
        var normalized = System.Text.Encoding.UTF8.GetString(manifestBytes).Replace("\r\n", "\n", StringComparison.Ordinal);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(manifestPath))!);
        File.WriteAllText(manifestPath, normalized, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        return manifest;
    }

    public static IReadOnlyList<string> Check(string outputRoot, string manifestPath)
    {
        var errors = new List<string>();

        if (!File.Exists(manifestPath))
        {
            errors.Add($"Manifest not found: {manifestPath}");
            return errors;
        }

        var manifest = JsonSerializer.Deserialize<ArtManifest>(File.ReadAllText(manifestPath), JsonOptions);
        if (manifest == null)
        {
            errors.Add("Manifest could not be parsed.");
            return errors;
        }

        var expectedPaths = new HashSet<string>(StringComparer.Ordinal);

        foreach (var entry in manifest.Assets)
        {
            expectedPaths.Add(entry.Path.Replace('\\', '/'));
            var fullPath = Path.Combine(outputRoot, entry.Path.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(fullPath))
            {
                errors.Add($"Missing asset: {entry.Path}");
                continue;
            }

            var bytes = File.ReadAllBytes(fullPath);
            var hash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
            if (!string.Equals(hash, entry.Sha256, StringComparison.Ordinal))
            {
                errors.Add($"Hash mismatch: {entry.Path}");
            }

            using var bitmap = SKBitmap.Decode(bytes);
            if (bitmap == null)
            {
                errors.Add($"Invalid PNG: {entry.Path}");
                continue;
            }

            if (bitmap.Width != entry.Width || bitmap.Height != entry.Height)
            {
                errors.Add($"Dimension mismatch: {entry.Path} ({bitmap.Width}x{bitmap.Height})");
            }

            if (entry.PivotX is < 0f or > 1f || entry.PivotY is < 0f or > 1f)
            {
                errors.Add($"Pivot out of range: {entry.Path}");
            }
        }

        if (Directory.Exists(outputRoot))
        {
            foreach (var file in Directory.EnumerateFiles(outputRoot, "*.png", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(outputRoot, file).Replace('\\', '/');
                if (!expectedPaths.Contains(relative))
                {
                    errors.Add($"Unlisted asset: {relative}");
                }
            }
        }

        return errors;
    }

    public static void WriteContactSheet(string outputRoot, string manifestPath, string contactSheetPath)
    {
        var manifest = JsonSerializer.Deserialize<ArtManifest>(File.ReadAllText(manifestPath), JsonOptions)
                       ?? throw new InvalidOperationException("Manifest could not be parsed.");

        const int tile = 170;
        const int labelHeight = 28;
        const int columns = 6;
        var rows = (manifest.Assets.Count + columns - 1) / columns;
        var width = columns * tile;
        var height = Math.Max(tile, rows * (tile + labelHeight));

        using var bitmap = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(Palette.Background);

        using var labelPaint = new SKPaint
        {
            IsAntialias = true,
            Color = Palette.White
        };
        using var font = new SKFont(SKTypeface.Default, 11f);

        var index = 0;
        foreach (var entry in manifest.Assets.OrderBy(item => item.Category, StringComparer.Ordinal)
                     .ThenBy(item => item.Path, StringComparer.Ordinal))
        {
            var column = index % columns;
            var row = index / columns;
            var x = column * tile;
            var y = row * (tile + labelHeight);
            var fullPath = Path.Combine(outputRoot, entry.Path.Replace('/', Path.DirectorySeparatorChar));
            using var image = SKBitmap.Decode(fullPath);
            if (image != null)
            {
                var scale = Math.Min((tile - 16f) / image.Width, (tile - 16f) / image.Height);
                var drawWidth = image.Width * scale;
                var drawHeight = image.Height * scale;
                var drawX = x + (tile - drawWidth) / 2f;
                var drawY = y + (tile - drawHeight) / 2f;
                using var imagePaint = new SKPaint { IsAntialias = true };
                canvas.DrawBitmap(
                    image,
                    new SKRect(drawX, drawY, drawX + drawWidth, drawY + drawHeight),
                    new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None),
                    imagePaint);
            }

            canvas.DrawText(
                entry.Path.Replace("Characters/", "", StringComparison.Ordinal),
                x + 8f,
                y + tile + 18f,
                SKTextAlign.Left,
                font,
                labelPaint);

            index++;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(contactSheetPath))!);
        using var imageData = SKImage.FromBitmap(bitmap).Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(contactSheetPath, imageData.ToArray());
    }
}

internal sealed record SpriteDefinition(
    string Path,
    int Width,
    int Height,
    float PivotX,
    float PivotY,
    string Category,
    Action<SKCanvas> Draw);
