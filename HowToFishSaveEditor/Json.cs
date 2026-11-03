using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace HowToFishSaveEditor;

/// <summary>Small helpers to read/write Unity JsonUtility values without losing unknown fields.</summary>
public static class J
{
    public static readonly JsonSerializerOptions Compact = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    public static readonly JsonSerializerOptions Pretty = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static double ToDouble(JsonNode n, double def = 0)
    {
        if (n is not JsonValue v) return def;
        if (v.TryGetValue(out double d)) return d;
        if (v.TryGetValue(out float f)) return f;
        if (v.TryGetValue(out long l)) return l;
        if (v.TryGetValue(out int i)) return i;
        if (v.TryGetValue(out byte b)) return b;
        if (v.TryGetValue(out ulong u)) return u;
        if (v.TryGetValue(out bool bo)) return bo ? 1 : 0;
        return def;
    }

    public static long ToLong(JsonNode n, long def = 0)
    {
        if (n is not JsonValue v) return def;
        if (v.TryGetValue(out long l)) return l;
        if (v.TryGetValue(out int i)) return i;
        if (v.TryGetValue(out byte b)) return b;
        if (v.TryGetValue(out ulong u)) return unchecked((long)u);
        return (long)Math.Round(ToDouble(v, def));
    }

    public static double Num(JsonObject o, string k, double def = 0) => o == null ? def : ToDouble(o[k], def);
    public static long Long(JsonObject o, string k, long def = 0) => o == null ? def : ToLong(o[k], def);

    public static bool Bool(JsonObject o, string k, bool def = false)
    {
        if (o?[k] is not JsonValue v) return def;
        if (v.TryGetValue(out bool b)) return b;
        return ToDouble(v) != 0;
    }

    public static string Str(JsonObject o, string k) => o?[k] is JsonValue v && v.TryGetValue(out string s) ? s : "";

    public static void Set(JsonObject o, string k, long v) => o[k] = JsonValue.Create(v);
    public static void SetF(JsonObject o, string k, float v) => o[k] = JsonValue.Create(v);
    public static void Set(JsonObject o, string k, bool v) => o[k] = JsonValue.Create(v);
    public static void Set(JsonObject o, string k, string v) => o[k] = JsonValue.Create(v);

    public static JsonArray Arr(JsonObject o, string k)
    {
        if (o[k] is JsonArray a) return a;
        a = new JsonArray();
        o[k] = a;
        return a;
    }

    public static JsonObject Obj(JsonObject o, string k, Func<JsonObject> create = null)
    {
        if (o[k] is JsonObject x) return x;
        x = create?.Invoke() ?? new JsonObject();
        o[k] = x;
        return x;
    }

    /// <summary>A SavedItem with the same field order the game writes.</summary>
    public static JsonObject NewItem(byte itemId, byte slot, bool exists = true) => new()
    {
        ["Exists"] = exists,
        ["InventorySlot"] = slot,
        ["ItemID"] = itemId,
        ["Cookness"] = 0f,
        ["BettingMultiplier"] = 1f,
        ["Weight"] = 1f,
        ["KillScoreMultiplier"] = 1f,
        ["IsDripCreature"] = false,
        ["SkinIndex"] = 0,
        ["Sharpness"] = 0,
        ["Quiver"] = 0,
        ["Sight"] = 0,
        ["BarrelAttachment"] = 0,
        ["AmmoType"] = 0,
        ["ExtendedMag"] = false,
        ["LaserSight"] = false,
    };
}
