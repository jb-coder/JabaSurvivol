using System.Security.Cryptography;
using SkiaSharp;

namespace JabaSurvivol.ArtGenerator.Tests;

public sealed class ArtPipelineTests
{
    private static readonly string Root = ArtPipeline.FindRepositoryRoot();
    private static readonly string OutputRoot = Path.Combine(Root, ArtPipeline.DefaultOutputRoot);
    private static readonly string ManifestPath = Path.Combine(Root, ArtPipeline.DefaultManifestPath);

    [Fact]
    public void Committed_assets_match_the_manifest()
    {
        var errors = ArtPipeline.Check(OutputRoot, ManifestPath);

        Assert.Empty(errors);
    }

    [Fact]
    public void Manifest_covers_every_combat_category()
    {
        var manifest = ReadManifest();

        Assert.Contains(manifest.Assets, asset => asset.Category == "character");
        Assert.Contains(manifest.Assets, asset => asset.Category == "weapon");
        Assert.Contains(manifest.Assets, asset => asset.Category == "projectile");
        Assert.Contains(manifest.Assets, asset => asset.Category == "effect");
        Assert.Contains(manifest.Assets, asset => asset.Category == "ui");
        Assert.Contains(manifest.Assets, asset => asset.Category == "environment");
    }

    [Fact]
    public void Manifest_entries_have_valid_dimensions_and_pivots()
    {
        var manifest = ReadManifest();

        foreach (var asset in manifest.Assets)
        {
            Assert.True(asset.Width > 0, asset.Path);
            Assert.True(asset.Height > 0, asset.Path);
            Assert.InRange(asset.PivotX, 0f, 1f);
            Assert.InRange(asset.PivotY, 0f, 1f);
            Assert.Matches("^[0-9a-f]{64}$", asset.Sha256);
            Assert.False(string.IsNullOrWhiteSpace(asset.ResourcePath));
        }
    }

    [Fact]
    public void Generator_is_deterministic_for_two_runs()
    {
        var first = Path.Combine(Path.GetTempPath(), $"jaba-art-{Guid.NewGuid():N}");
        var second = Path.Combine(Path.GetTempPath(), $"jaba-art-{Guid.NewGuid():N}");

        try
        {
            var firstManifest = Path.Combine(first, "manifest.json");
            var secondManifest = Path.Combine(second, "manifest.json");

            ArtPipeline.Generate(first, firstManifest);
            ArtPipeline.Generate(second, secondManifest);

            var firstBytes = File.ReadAllBytes(firstManifest);
            var secondBytes = File.ReadAllBytes(secondManifest);
            Assert.Equal(SHA256.HashData(firstBytes), SHA256.HashData(secondBytes));

            foreach (var asset in ReadManifest().Assets)
            {
                var firstAsset = Path.Combine(first, asset.Path.Replace('/', Path.DirectorySeparatorChar));
                var secondAsset = Path.Combine(second, asset.Path.Replace('/', Path.DirectorySeparatorChar));
                Assert.Equal(
                    SHA256.HashData(File.ReadAllBytes(firstAsset)),
                    SHA256.HashData(File.ReadAllBytes(secondAsset)));
            }
        }
        finally
        {
            Directory.Delete(first, recursive: true);
            Directory.Delete(second, recursive: true);
        }
    }

    [Fact]
    public void Check_reports_a_modified_asset()
    {
        var temporaryRoot = Path.Combine(Path.GetTempPath(), $"jaba-art-check-{Guid.NewGuid():N}");
        var temporaryManifest = Path.Combine(temporaryRoot, "manifest.json");

        try
        {
            ArtPipeline.Generate(temporaryRoot, temporaryManifest);
            var asset = ReadManifest().Assets.First();
            var assetPath = Path.Combine(temporaryRoot, asset.Path.Replace('/', Path.DirectorySeparatorChar));
            var bytes = File.ReadAllBytes(assetPath);
            bytes[bytes.Length - 1] ^= 0xFF;
            File.WriteAllBytes(assetPath, bytes);

            var errors = ArtPipeline.Check(temporaryRoot, temporaryManifest);

            Assert.Contains(errors, error => error.Contains("Hash mismatch", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(temporaryRoot, recursive: true);
        }
    }

    [Fact]
    public void Character_sprites_keep_transparent_padding_and_visible_pixels()
    {
        using var bitmap = SKBitmap.Decode(Path.Combine(OutputRoot, "Characters", "player_torso.png"));

        Assert.NotNull(bitmap);
        var transparent = 0;
        var visible = 0;
        for (var y = 0; y < bitmap.Height; y += 4)
        {
            for (var x = 0; x < bitmap.Width; x += 4)
            {
                var alpha = bitmap.GetPixel(x, y).Alpha;
                if (alpha == 0)
                {
                    transparent++;
                }
                else
                {
                    visible++;
                }
            }
        }

        Assert.True(transparent > 0);
        Assert.True(visible > 0);
    }

    private static ArtManifest ReadManifest()
    {
        return System.Text.Json.JsonSerializer.Deserialize<ArtManifest>(
                   File.ReadAllText(ManifestPath),
                   new System.Text.Json.JsonSerializerOptions
                   {
                       PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                   })
               ?? throw new InvalidOperationException("Manifest could not be parsed.");
    }
}
