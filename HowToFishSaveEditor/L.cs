using System.Globalization;
using System.Text.Json.Nodes;

namespace HowToFishSaveEditor;

public enum Lang { English, German }

/// <summary>UI language. English is the default; the choice is stored in %LOCALAPPDATA%\HowToFishSaveEditor\settings.json.</summary>
public static class L
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HowToFishSaveEditor", "settings.json");

    public static Lang Current { get; private set; } = Lang.English;
    public static bool De => Current == Lang.German;

    public static event Action Changed;

    /// <summary>Picks the English or German text.</summary>
    public static string T(string en, string de) => De ? de : en;

    public static void Load()
    {
        try
        {
            if (File.Exists(SettingsPath) && JsonNode.Parse(File.ReadAllText(SettingsPath)) is JsonObject o)
                Current = J.Str(o, "Language") == "de" ? Lang.German : Lang.English;
        }
        catch
        {
            Current = Lang.English;
        }
        ApplyCulture();
    }

    /// <summary>Number and date formatting follow the UI language, not the Windows region.</summary>
    private static void ApplyCulture()
    {
        var culture = CultureInfo.GetCultureInfo(De ? "de-DE" : "en-US");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.CurrentCulture = culture;
    }

    public static void Set(Lang lang, bool persist = true)
    {
        if (lang == Current) return;
        Current = lang;
        ApplyCulture();
        if (persist) Save();
        Changed?.Invoke();
    }

    private static void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
            File.WriteAllText(SettingsPath, new JsonObject { ["Language"] = De ? "de" : "en" }.ToJsonString());
        }
        catch
        {
            // not being able to persist the language is not worth an error dialog
        }
    }
}
