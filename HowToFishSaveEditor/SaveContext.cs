using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;

namespace HowToFishSaveEditor;

/// <summary>Holds the currently opened profile (local.txt) and world (&lt;name&gt;.txt).</summary>
public sealed class SaveContext
{
    public static string GameDataRoot => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow", "Dazed Games", "How to Fish");
    public static string SavesRoot { get; set; } = Path.Combine(GameDataRoot, "Saves");
    public static string BackupRoot { get; set; } = Path.Combine(GameDataRoot, "SaveEditorBackups");

    public string ProfileFolder { get; private set; }
    public string LocalPath => ProfileFolder == null ? null : Path.Combine(ProfileFolder, "local.txt");
    public string WorldPath { get; private set; }
    public JsonObject Local { get; private set; }
    public JsonObject World { get; private set; }
    public bool LocalDirty { get; private set; }
    public bool WorldDirty { get; private set; }
    public bool IsDirty => LocalDirty || WorldDirty;
    public int PlayerIndex { get; set; }
    public string LastBackupFolder { get; private set; }

    public event Action DirtyChanged;

    public static bool GameRunning
    {
        get
        {
            try { return Process.GetProcessesByName("How to Fish").Length > 0; }
            catch { return false; }
        }
    }

    public static List<string> FindProfiles()
    {
        var result = new List<string>();
        if (!Directory.Exists(SavesRoot)) return result;
        foreach (var dir in Directory.GetDirectories(SavesRoot).OrderByDescending(Directory.GetLastWriteTime))
            if (Directory.GetFiles(dir, "*.txt").Length > 0) result.Add(dir);
        // Legacy layout: saves directly inside Saves\
        if (Directory.GetFiles(SavesRoot, "*.txt").Length > 0) result.Add(SavesRoot);
        return result;
    }

    public List<string> FindWorlds()
    {
        if (ProfileFolder == null || !Directory.Exists(ProfileFolder)) return new();
        return Directory.GetFiles(ProfileFolder, "*.txt")
            .Where(f => !Path.GetFileName(f).Equals("local.txt", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(File.GetLastWriteTime)
            .ToList();
    }

    public void OpenProfile(string folder)
    {
        ProfileFolder = folder;
        Local = File.Exists(LocalPath) ? Parse(LocalPath) : null;
        World = null;
        WorldPath = null;
        PlayerIndex = 0;
        LocalDirty = WorldDirty = false;
        DirtyChanged?.Invoke();
    }

    public void OpenWorld(string path)
    {
        World = path == null ? null : Parse(path);
        WorldPath = path;
        PlayerIndex = 0;
        WorldDirty = false;
        DirtyChanged?.Invoke();
    }

    public void ReloadLocal()
    {
        Local = File.Exists(LocalPath) ? Parse(LocalPath) : null;
        LocalDirty = false;
        DirtyChanged?.Invoke();
    }

    public void ReplaceWorld(JsonObject o) { World = o; MarkWorld(); }
    public void ReplaceLocal(JsonObject o) { Local = o; MarkLocal(); }

    public void MarkWorld() { WorldDirty = true; DirtyChanged?.Invoke(); }
    public void MarkLocal() { LocalDirty = true; DirtyChanged?.Invoke(); }

    public static JsonObject Parse(string path)
    {
        var text = File.ReadAllText(path);
        return JsonNode.Parse(text) as JsonObject ?? throw new InvalidDataException(L.T($"{Path.GetFileName(path)} does not contain a JSON object.", $"{Path.GetFileName(path)} enthält kein JSON-Objekt."));
    }

    public JsonArray Players => World == null ? null : J.Arr(World, "Players");

    public JsonObject CurrentPlayer
    {
        get
        {
            var p = Players;
            if (p == null || p.Count == 0) return null;
            if (PlayerIndex < 0 || PlayerIndex >= p.Count) PlayerIndex = 0;
            return p[PlayerIndex] as JsonObject;
        }
    }

    /// <summary>Writes all dirty files. Originals are copied into a timestamped backup folder first.</summary>
    public void Save()
    {
        var stamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var backupDir = Path.Combine(BackupRoot, Path.GetFileName(ProfileFolder.TrimEnd('\\', '/')), stamp);

        if (WorldDirty && World != null)
        {
            var name = J.Str(World, "Name").Trim();
            if (name.Length == 0) throw new InvalidDataException(L.T("The world name must not be empty (the game would discard the save).", "Der Weltname darf nicht leer sein (das Spiel verwirft sonst den Spielstand)."));
            if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) throw new InvalidDataException(L.T("The world name contains characters that are not allowed in file names.", "Der Weltname enthält Zeichen, die in Dateinamen nicht erlaubt sind."));
            if (J.Long(World, "LastSave") <= 0) J.Set(World, "LastSave", DateTime.Now.Ticks);

            var target = Path.Combine(ProfileFolder, name + ".txt");
            bool rename = !string.Equals(Path.GetFullPath(target), Path.GetFullPath(WorldPath), StringComparison.OrdinalIgnoreCase);
            if (rename && File.Exists(target)) throw new IOException(L.T($"A world named \"{name}\" already exists.", $"Es gibt bereits eine Welt namens \"{name}\"."));

            Backup(WorldPath, backupDir);
            Write(target, World);
            if (rename)
            {
                MoveToBackup(WorldPath, backupDir);
                MoveToBackup(WorldPath + ".backup", backupDir);
                WorldPath = target;
            }
            WorldDirty = false;
        }

        if (LocalDirty && Local != null)
        {
            Backup(LocalPath, backupDir);
            Write(LocalPath, Local);
            LocalDirty = false;
        }

        LastBackupFolder = Directory.Exists(backupDir) ? backupDir : LastBackupFolder;
        DirtyChanged?.Invoke();
    }

    private static void Write(string path, JsonObject o)
    {
        var tmp = path + ".editortmp";
        File.WriteAllText(tmp, o.ToJsonString(J.Compact), new UTF8Encoding(false));
        // Validate before replacing the real file
        if (JsonNode.Parse(File.ReadAllText(tmp)) is not JsonObject) throw new InvalidDataException(L.T("Written file is invalid.", "Geschriebene Datei ist ungültig."));
        File.Move(tmp, path, true);
    }

    private static void Backup(string path, string dir)
    {
        if (path == null || !File.Exists(path)) return;
        Directory.CreateDirectory(dir);
        File.Copy(path, Path.Combine(dir, Path.GetFileName(path)), true);
    }

    private static void MoveToBackup(string path, string dir)
    {
        if (!File.Exists(path)) return;
        Directory.CreateDirectory(dir);
        File.Move(path, Path.Combine(dir, Path.GetFileName(path)), true);
    }
}
