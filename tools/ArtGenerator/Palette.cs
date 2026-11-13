using SkiaSharp;

namespace JabaSurvivol.ArtGenerator;

internal static class Palette
{
    public const string Version = "2026-11-13.1";

    public static readonly SKColor Background = Hex("#04070C");
    public static readonly SKColor Floor = Hex("#0A0F18");
    public static readonly SKColor FloorGrid = Hex("#141C2A");
    public static readonly SKColor Outline = Hex("#05080E");
    public static readonly SKColor Steel = Hex("#6B7C93");
    public static readonly SKColor White = Hex("#F2FBFF");
    public static readonly SKColor Cyan = Hex("#3DF2FF");
    public static readonly SKColor Ice = Hex("#B8FBFF");
    public static readonly SKColor Amber = Hex("#FFD24A");
    public static readonly SKColor Orange = Hex("#FF8A2A");
    public static readonly SKColor Red = Hex("#FF3F4F");
    public static readonly SKColor Magenta = Hex("#F25AE6");
    public static readonly SKColor Purple = Hex("#A855F7");
    public static readonly SKColor Green = Hex("#59FF9E");
    public static readonly SKColor Lime = Hex("#C8FF66");
    public static readonly SKColor EliteGold = Hex("#FFE066");

    public static readonly CharacterPalette Player = new(
        Body: Hex("#12202E"),
        Plate: Hex("#1B3446"),
        Accent: Cyan,
        Visor: Ice);

    public static readonly CharacterPalette Walker = new(
        Body: Hex("#3A1218"),
        Plate: Hex("#6E1B24"),
        Accent: Red,
        Visor: Hex("#FFB3B8"));

    public static readonly CharacterPalette Runner = new(
        Body: Hex("#3A2410"),
        Plate: Hex("#8A4A14"),
        Accent: Orange,
        Visor: Hex("#FFD9A8"));

    public static readonly CharacterPalette Brute = new(
        Body: Hex("#2A1438"),
        Plate: Hex("#5A2A78"),
        Accent: Purple,
        Visor: Hex("#E9C7FF"));

    public static readonly CharacterPalette Warden = new(
        Body: Hex("#2A0A20"),
        Plate: Hex("#6E1248"),
        Accent: Magenta,
        Visor: Amber);

    public static readonly CharacterPalette Turret = new(
        Body: Hex("#0E2A22"),
        Plate: Hex("#1E5A44"),
        Accent: Green,
        Visor: Lime);

    public static SKColor Hex(string value)
    {
        return SKColor.Parse(value);
    }
}

internal sealed record CharacterPalette(
    SKColor Body,
    SKColor Plate,
    SKColor Accent,
    SKColor Visor);
