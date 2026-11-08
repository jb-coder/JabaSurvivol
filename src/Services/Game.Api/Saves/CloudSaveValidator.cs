using System.Text.Json;
using JabaSurvivol.Contracts;
using JabaSurvivol.Core;

namespace Game.Api.Saves;

public static class CloudSaveValidator
{
    private static readonly JsonSerializerOptions ProfileOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        IncludeFields = true
    };

    public static bool TryValidate(
        JsonElement document,
        out int schemaVersion,
        out string error)
    {
        schemaVersion = 0;

        if (document.ValueKind != JsonValueKind.Object)
        {
            error = "The document must be a JSON object.";
            return false;
        }

        if (!TryGetProperty(document, "schemaVersion", out var schemaElement) ||
            schemaElement.ValueKind != JsonValueKind.Number ||
            !schemaElement.TryGetInt32(out schemaVersion))
        {
            error = "The document must contain a numeric schemaVersion.";
            return false;
        }

        if (schemaVersion != SaveContract.SupportedSchemaVersion)
        {
            error = $"Schema version {schemaVersion} is not supported.";
            return false;
        }

        if (!TryReadInt(document, "supplies", out var supplies) || supplies < 0)
        {
            error = "The document must contain a non-negative supplies value.";
            return false;
        }

        if (!TryReadInt(document, "xp", out var xp) || xp < 0)
        {
            error = "The document must contain a non-negative xp value.";
            return false;
        }

        if (!TryReadInt(document, "level", out var level) || level < 1 || level > Profile.MaxLevel)
        {
            error = $"level must be between 1 and {Profile.MaxLevel}.";
            return false;
        }

        if (!TryReadInt(document, "skillPoints", out var skillPoints) ||
            skillPoints < 0 ||
            skillPoints > Profile.MaxSkillPoints)
        {
            error = $"skillPoints must be between 0 and {Profile.MaxSkillPoints}.";
            return false;
        }

        if (!TryReadInt(document, "highestRound", out var highestRound) ||
            highestRound < 1 ||
            highestRound > 10)
        {
            error = "highestRound must be between 1 and 10.";
            return false;
        }

        if (!HasValidArray(document, "unlockedWeapons") ||
            !HasValidArray(document, "weaponUpgrades") ||
            !HasValidArray(document, "unlockedArmor") ||
            !HasValidArray(document, "armorUpgrades") ||
            !HasValidArray(document, "allocatedSkills"))
        {
            error = "Profile list fields must be arrays when present.";
            return false;
        }

        try
        {
            var profile = JsonSerializer.Deserialize<Profile>(document.GetRawText(), ProfileOptions);
            if (profile is null)
            {
                error = "The document could not be read as a profile.";
                return false;
            }

            profile.Validate();
        }
        catch (JsonException)
        {
            error = "The document contains invalid profile values.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool TryReadInt(JsonElement document, string name, out int value)
    {
        value = 0;
        return TryGetProperty(document, name, out var element) &&
               element.ValueKind == JsonValueKind.Number &&
               element.TryGetInt32(out value);
    }

    private static bool HasValidArray(JsonElement document, string name)
    {
        return !TryGetProperty(document, name, out var element) ||
               element.ValueKind == JsonValueKind.Array;
    }

    public static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
