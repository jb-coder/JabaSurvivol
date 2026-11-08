using System.Text;
using System.Text.Json;
using JabaSurvivol.Contracts;

namespace Game.Api.Telemetry;

public static class TelemetryValidator
{
    private static readonly string[] Archetypes = ["walker", "runner", "brute"];
    private static readonly string[] Rewards = ["supplies", "xp", "ammo_crate", "armor_plate"];
    private static readonly string[] Weapons = ["pistol", "smg", "shotgun", "rifle", "lmg"];

    public static bool TryValidateEnvelope(
        TelemetryEnvelope? envelope,
        DateTimeOffset now,
        out string error)
    {
        if (envelope?.Events is null)
        {
            error = "The request must contain an events array.";
            return false;
        }

        if (envelope.Events.Count is < 1 or > TelemetryContract.MaximumBatchSize)
        {
            error = $"The batch must contain between 1 and {TelemetryContract.MaximumBatchSize} events.";
            return false;
        }

        for (var index = 0; index < envelope.Events.Count; index++)
        {
            if (!TryValidateEvent(envelope.Events[index], now, out var eventError))
            {
                error = $"events[{index}]: {eventError}";
                return false;
            }
        }

        error = string.Empty;
        return true;
    }

    public static bool TryValidateEvent(
        TelemetryEventRequest? telemetryEvent,
        DateTimeOffset now,
        out string error)
    {
        if (telemetryEvent is null)
        {
            error = "The event is missing.";
            return false;
        }

        if (telemetryEvent.EventId == Guid.Empty)
        {
            error = "eventId must be a non-empty UUID.";
            return false;
        }

        if (telemetryEvent.SessionId == Guid.Empty)
        {
            error = "sessionId must be a non-empty UUID.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(telemetryEvent.EventType) ||
            telemetryEvent.EventType.Length > TelemetryContract.MaximumEventTypeLength ||
            !TelemetryContract.EventTypes.Contains(telemetryEvent.EventType))
        {
            error = "eventType is not allowlisted.";
            return false;
        }

        if (telemetryEvent.OccurredAt < now - TelemetryContract.MaximumAge ||
            telemetryEvent.OccurredAt > now + TelemetryContract.MaximumFutureSkew)
        {
            error = "occurredAt is outside the accepted time window.";
            return false;
        }

        if (telemetryEvent.Payload.ValueKind != JsonValueKind.Object)
        {
            error = "payload must be a JSON object.";
            return false;
        }

        var payloadJson = telemetryEvent.Payload.GetRawText();
        if (Encoding.UTF8.GetByteCount(payloadJson) > TelemetryContract.MaximumPayloadBytes)
        {
            error = $"payload must not exceed {TelemetryContract.MaximumPayloadBytes} bytes.";
            return false;
        }

        return TryValidatePayload(telemetryEvent.EventType, telemetryEvent.Payload, out error);
    }

    private static bool TryValidatePayload(
        string eventType,
        JsonElement payload,
        out string error)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in payload.EnumerateObject())
        {
            if (!keys.Add(property.Name))
            {
                error = $"payload contains the duplicate key '{property.Name}'.";
                return false;
            }
        }

        return eventType switch
        {
            TelemetryContract.RunStart => ValidateRunStart(payload, keys, out error),
            TelemetryContract.RunEnd => ValidateRunEnd(payload, keys, out error),
            TelemetryContract.RoundClear => ValidateRoundClear(payload, keys, out error),
            TelemetryContract.ZombieKill => ValidateZombieKill(payload, keys, out error),
            TelemetryContract.BossDefeat => ValidateBossDefeat(payload, keys, out error),
            TelemetryContract.LevelUp => ValidateLevelUp(payload, keys, out error),
            TelemetryContract.RewardSelection => ValidateRewardSelection(payload, keys, out error),
            TelemetryContract.WeaponSwitch => ValidateWeaponSwitch(payload, keys, out error),
            _ => Fail("eventType is not allowlisted.", out error)
        };
    }

    private static bool ValidateRunStart(JsonElement payload, HashSet<string> keys, out string error)
    {
        return HasOnlyKeys(keys, out error, "startingRound") &&
               TryInt(payload, "startingRound", 1, 10, out error);
    }

    private static bool ValidateRunEnd(JsonElement payload, HashSet<string> keys, out string error)
    {
        return HasOnlyKeys(keys, out error, "highestRound", "totalKills", "durationSeconds") &&
               TryInt(payload, "highestRound", 1, 10, out error) &&
               TryInt(payload, "totalKills", 0, 100_000, out error) &&
               TryInt(payload, "durationSeconds", 1, 86_400, out error);
    }

    private static bool ValidateRoundClear(JsonElement payload, HashSet<string> keys, out string error)
    {
        return HasOnlyKeys(keys, out error, "round", "durationSeconds") &&
               TryInt(payload, "round", 1, 10, out error) &&
               TryInt(payload, "durationSeconds", 0, 86_400, out error);
    }

    private static bool ValidateZombieKill(JsonElement payload, HashSet<string> keys, out string error)
    {
        return HasOnlyKeys(keys, out error, "round", "archetype") &&
               TryInt(payload, "round", 1, 10, out error) &&
               TryEnum(payload, "archetype", Archetypes, out error);
    }

    private static bool ValidateBossDefeat(JsonElement payload, HashSet<string> keys, out string error)
    {
        return HasOnlyKeys(keys, out error, "round") &&
               TryInt(payload, "round", 1, 10, out error);
    }

    private static bool ValidateLevelUp(JsonElement payload, HashSet<string> keys, out string error)
    {
        return HasOnlyKeys(keys, out error, "level") &&
               TryInt(payload, "level", 1, 30, out error);
    }

    private static bool ValidateRewardSelection(JsonElement payload, HashSet<string> keys, out string error)
    {
        return HasOnlyKeys(keys, out error, "round", "rewardType") &&
               TryInt(payload, "round", 1, 10, out error) &&
               TryEnum(payload, "rewardType", Rewards, out error);
    }

    private static bool ValidateWeaponSwitch(JsonElement payload, HashSet<string> keys, out string error)
    {
        return HasOnlyKeys(keys, out error, "weapon") &&
               TryEnum(payload, "weapon", Weapons, out error);
    }

    private static bool HasOnlyKeys(HashSet<string> keys, out string error, params string[] allowed)
    {
        var allowedSet = new HashSet<string>(allowed, StringComparer.OrdinalIgnoreCase);
        foreach (var key in keys)
        {
            if (!allowedSet.Contains(key))
            {
                error = $"payload contains the unsupported field '{key}'.";
                return false;
            }
        }

        error = string.Empty;
        return true;
    }

    private static bool TryInt(JsonElement payload, string name, int minimum, int maximum, out string error)
    {
        if (!TryGetProperty(payload, name, out var value) ||
            value.ValueKind != JsonValueKind.Number ||
            !value.TryGetInt32(out var number) ||
            number < minimum ||
            number > maximum)
        {
            error = $"payload.{name} must be an integer between {minimum} and {maximum}.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool TryEnum(JsonElement payload, string name, string[] allowed, out string error)
    {
        if (!TryGetProperty(payload, name, out var value) ||
            value.ValueKind != JsonValueKind.String)
        {
            error = $"payload.{name} must be a string.";
            return false;
        }

        var text = value.GetString();
        if (text is null || !allowed.Contains(text, StringComparer.Ordinal))
        {
            error = $"payload.{name} must be one of: {string.Join(", ", allowed)}.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
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

    private static bool Fail(string message, out string error)
    {
        error = message;
        return false;
    }
}
