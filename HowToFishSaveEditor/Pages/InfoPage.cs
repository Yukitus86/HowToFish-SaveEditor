using System.Diagnostics;
using static HowToFishSaveEditor.L;

namespace HowToFishSaveEditor.Pages;

public sealed class InfoPage : Page
{
    public InfoPage(SaveContext ctx) : base(ctx, "Info", $"How to Fish Save Editor · Version {MainForm.VersionText}")
    {
        var body = Ui.FlowBody();

        var paths = new FieldGrid(() => { }, 140, 520);
        paths.Info(T("Saves", "Spielstände"), SaveContext.SavesRoot);
        paths.Info(T("Profile", "Profil"), ctx.ProfileFolder ?? "—");
        paths.Info(T("World file", "Welt-Datei"), ctx.WorldPath ?? "—");
        paths.Info("Backups", SaveContext.BackupRoot);
        body.Controls.Add(Ui.Card(T("Paths", "Pfade"), Ui.Stack(paths, Ui.ButtonRow(
            Theme.Btn(T("Open save folder", "Spielstand-Ordner öffnen"), click: (_, _) => Open(ctx.ProfileFolder ?? SaveContext.SavesRoot)),
            Theme.Btn(T("Open backup folder", "Backup-Ordner öffnen"), click: (_, _) => Open(SaveContext.BackupRoot))))));

        var tips = Ui.Stack(
            Ui.Hint(T("• Close the game before saving – the game autosaves and would overwrite your changes.",
                      "• Spiel vor dem Speichern schließen – das Spiel speichert automatisch und würde deine Änderungen überschreiben."), Theme.Warn),
            Ui.Hint(T("• Before every save the original files are copied into the backup folder (one timestamped folder per save).",
                      "• Vor jedem Speichern werden die Originaldateien in den Backup-Ordner kopiert (pro Speichervorgang ein Zeitstempel-Ordner).")),
            Ui.Hint(T("• World files (<name>.txt) contain money, inventory, boat, upgrades and world items.",
                      "• Welt-Dateien (<Name>.txt) enthalten Geld, Inventar, Boot, Upgrades und Welt-Items.")),
            Ui.Hint(T("• local.txt is per profile: journal, unlocked skins, appearance and settings.",
                      "• local.txt ist profilweit: Journal, freigeschaltete Skins, Aussehen und Einstellungen.")),
            Ui.Hint(T("• Steam Cloud: if Steam reports a conflict on the next launch, choose the local files.",
                      "• Steam Cloud: Fragt Steam beim nächsten Start nach einem Konflikt, wähle die lokalen Dateien.")),
            Ui.Hint(T("• Multiplayer: world values belong to the host; every player has their own entry (\"Player\" tab).",
                      "• Multiplayer: Werte der Welt gelten für den Host; jeder Spieler hat einen eigenen Eintrag (Tab \"Spieler\").")));
        body.Controls.Add(Ui.Card(T("Notes", "Hinweise"), tips));

        body.Controls.Add(Ui.Card(T("About", "Über"), Ui.Stack(
            Ui.Hint(T($"Version {MainForm.VersionText} – unofficial fan tool, not made by Dazed Games.",
                      $"Version {MainForm.VersionText} – inoffizielles Fan-Tool, nicht von Dazed Games.")),
            Ui.Hint(T("Item IDs, skins, baits and upgrade limits were read from the game files. Game updates may change IDs – use Raw JSON when in doubt.",
                      "Item-IDs, Skins, Köder und Upgrade-Grenzen wurden aus den Spieldateien ausgelesen. Nach Spiel-Updates können sich IDs ändern – im Zweifel Raw JSON nutzen.")))));

        Body.Controls.Add(body);
    }

    private static void Open(string path)
    {
        Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{path}\"") { UseShellExecute = true });
    }
}
