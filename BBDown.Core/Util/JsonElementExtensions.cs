using System.Text.Json;

namespace BBDown.Core.Util;

public static class JsonElementExtensions
{
    public static string GetStringSafe(this JsonElement element, string propertyName, string defaultValue = "")
    {
        if (element.ValueKind != JsonValueKind.Object)
            return defaultValue;
        if (!element.TryGetProperty(propertyName, out var prop))
            return defaultValue;
        return prop.ValueKind == JsonValueKind.String ? prop.GetString() ?? defaultValue : defaultValue;
    }

    public static int GetInt32Safe(this JsonElement element, string propertyName, int defaultValue = 0)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return defaultValue;
        if (!element.TryGetProperty(propertyName, out var prop))
            return defaultValue;
        return prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var v) ? v : defaultValue;
    }

    public static long GetInt64Safe(this JsonElement element, string propertyName, long defaultValue = 0)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return defaultValue;
        if (!element.TryGetProperty(propertyName, out var prop))
            return defaultValue;
        return prop.ValueKind == JsonValueKind.Number && prop.TryGetInt64(out var v) ? v : defaultValue;
    }

    public static double GetDoubleSafe(this JsonElement element, string propertyName, double defaultValue = 0)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return defaultValue;
        if (!element.TryGetProperty(propertyName, out var prop))
            return defaultValue;
        return prop.ValueKind == JsonValueKind.Number && prop.TryGetDouble(out var v) ? v : defaultValue;
    }

    public static bool GetBooleanSafe(this JsonElement element, string propertyName, bool defaultValue = false)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return defaultValue;
        if (!element.TryGetProperty(propertyName, out var prop))
            return defaultValue;
        return prop.ValueKind == JsonValueKind.True || (prop.ValueKind == JsonValueKind.False ? prop.GetBoolean() : defaultValue);
    }

    public static JsonElement GetPropertySafe(this JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object)
            throw new InvalidOperationException($"Expected JSON object, got {element.ValueKind}");
        if (!element.TryGetProperty(propertyName, out var prop))
            throw new KeyNotFoundException($"JSON property not found: '{propertyName}' (available keys: {string.Join(", ", element.EnumerateObject().Select(p => p.Name))})");
        return prop;
    }

    public static JsonElement? TryGetPropertySafe(this JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object)
            return null;
        return element.TryGetProperty(propertyName, out var prop) ? prop : null;
    }
}
