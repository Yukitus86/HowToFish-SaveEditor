using System.Text.Json.Nodes;
using static HowToFishSaveEditor.L;

namespace HowToFishSaveEditor.Pages;

public sealed class WorldPage : Page
{
    public WorldPage(SaveContext ctx) : base(ctx, T("World", "Welt"), T("Money, progress, boat and unlocks of the selected world.", "Geld, Fortschritt, Boot und Freischaltungen der gewählten Welt."))
    {
        var w = ctx.World;
        var body = Ui.FlowBody();

        // General
        var g = new FieldGrid(WorldChanged);
        g.TextField(T("World name", "Weltname"), w, "Name", T("Also used as the file name when saving (the file gets renamed).", "Wird beim Speichern auch als Dateiname verwendet (die Datei wird umbenannt)."));
        g.Combo(T("Difficulty", "Schwierigkeit"), w, "Difficulty", GameData.Difficulties.Select((d, i) => new Opt(i, d)));
        var hours = new Label();
        var pt = g.Float(T("Playtime (seconds)", "Spielzeit (Sekunden)"), w, "Playtime", 0, 1_000_000_000, 1, T("Total playtime of this world in seconds.", "Gesamte Spielzeit dieser Welt in Sekunden."), 60);
        void UpdateHours() => hours.Text = TimeSpan.FromSeconds((double)pt.Value).ToString(@"d\d\ hh\h\ mm\m");
        pt.ValueChanged += (_, _) => UpdateHours();
        g.AddRow("", hours);
        hours.ForeColor = Theme.Muted;
        UpdateHours();
        long ticks = J.Long(w, "LastSave");
        g.Info(T("Last saved", "Zuletzt gespeichert"), ticks > 0 && ticks < DateTime.MaxValue.Ticks ? new DateTime(ticks).ToString(De ? "dd.MM.yyyy HH:mm:ss" : "yyyy-MM-dd HH:mm:ss") : "—");
        g.Bool(T("Steam lobby", "Steam-Lobby"), w, "UseSteam", T("World is hosted through Steam (multiplayer).", "Welt wird über Steam gehostet (Multiplayer)."));
        g.Bool(T("Public", "Öffentlich"), w, "IsPublic", T("Lobby is publicly visible.", "Lobby ist öffentlich sichtbar."));
        body.Controls.Add(Ui.Card(T("General", "Allgemein"), g));

        // Money
        var m = new FieldGrid(WorldChanged);
        var money = m.Int(T("Dollars ($)", "Dollar ($)"), w, "Money", 0, int.MaxValue);
        var euro = m.Int("Euro (€)", w, "Euro", 0, int.MaxValue);
        var mb = Ui.ButtonRow(
            Theme.Btn($"$ {1_000_000:N0}", click: (_, _) => money.Value = Math.Max(money.Value, 1_000_000)),
            Theme.Btn($"$ {99_999_999:N0}", click: (_, _) => money.Value = 99_999_999),
            Theme.Btn($"€ {100_000:N0}", click: (_, _) => euro.Value = Math.Max(euro.Value, 100_000)),
            Theme.Btn($"€ {9_999_999:N0}", click: (_, _) => euro.Value = 9_999_999));
        body.Controls.Add(Ui.Card(T("Money", "Geld"), Ui.Stack(m, mb), T("Euro is the second currency (later islands, bow, quiver …).", "Euro ist die zweite Währung (spätere Inseln, Bogen, Köcher …).")));

        // Progress
        var p = new FieldGrid(WorldChanged);
        var islands = GameData.Islands.Select((s, i) => new Opt(i, s)).ToList();
        p.Combo(T("Highest unlocked island", "Höchste freigeschaltete Insel"), w, "MaxIsland", islands, T("How far you can travel.", "Bis zu welcher Insel du reisen kannst."));
        p.Combo(T("Current island", "Aktuelle Insel"), w, "SpawnedIsland", islands, T("The island you start on when loading.", "Auf welcher Insel du beim Laden startest."));
        p.Bool(T("Has mini island", "Mini-Insel vorhanden"), w, "HasMiniIsland");
        p.Int(T("Mini island index", "Mini-Insel Index"), w, "SpawnedMiniIsland", 0, 255, T("255 = no mini island.", "255 = keine Mini-Insel."));
        p.Vector(T("Mini island position", "Mini-Insel Position"), w, "MiniIslandPosition", new[] { "x", "y" }, -100000, 100000, 2);
        p.Bool(T("Final boss killed", "Endboss besiegt"), w, "FinalBossKilled");
        p.Bool(T("Game finished", "Spiel abgeschlossen"), w, "HasFinishedGame");
        body.Controls.Add(Ui.Card(T("Progress", "Fortschritt"), p));

        // Boat
        var b = new FieldGrid(WorldChanged);
        b.Bool(T("Boat unlocked", "Boot freigeschaltet"), w, "UnlockedBoat");
        b.Bool(T("Boat radar", "Boot-Radar"), w, "UnlockedBoatRadar");
        b.Combo("Motor", w, "MotorIndex", GameData.Motors.Select((s, i) => new Opt(i, s)));
        b.Combo(T("Boat skin", "Boot-Skin"), w, "BoatSkin", GameData.BoatSkins.Select((s, i) => new Opt(i, $"{s}  ({GameData.RarityName(GameData.BoatSkinRarity[i])})")),
            T("Currently selected boat skin. Unlock skins in the \"Skins\" tab.", "Aktiv ausgewählter Boot-Skin. Freischalten im Tab \"Skins\"."));
        b.Bool(T("Grill unlocked", "Grill freigeschaltet"), w, "UnlockedGrill");
        b.Bool(T("World positions updated", "Welt-Positionen aktualisiert"), w, "HasUpdatedWorldPositions", T("Internal migration flag of the game – normally leave it alone.", "Interne Migrations-Flag des Spiels – normalerweise nicht ändern."));
        body.Controls.Add(Ui.Card(T("Boat & unlocks", "Boot & Freischaltungen"), b));

        // Quick actions
        var quick = Ui.ButtonRow(
            Theme.Btn(T("Unlock everything", "Alles freischalten"), Theme.ButtonKind.Accent, (_, _) => UnlockAll(w),
                T("Boat, radar, best motor, grill and all islands.", "Boot, Radar, bester Motor, Grill und alle Inseln.")),
            Theme.Btn(T("Delete all world items", "Alle Welt-Items löschen"), Theme.ButtonKind.Danger, (_, _) =>
            {
                if (MessageBox.Show(T("Delete all items lying around in the world?", "Alle herumliegenden Items in der Welt löschen?"), T("World items", "Welt-Items"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
                J.Arr(w, "WorldItems").Clear();
                WorldChanged();
                Rebuild();
            }));
        body.Controls.Add(Ui.Card(T("Quick actions", "Schnellaktionen"), quick));

        Body.Controls.Add(body);
    }

    private void UnlockAll(JsonObject w)
    {
        J.Set(w, "UnlockedBoat", true);
        J.Set(w, "UnlockedBoatRadar", true);
        J.Set(w, "MotorIndex", GameData.Motors.Length - 1);
        J.Set(w, "UnlockedGrill", true);
        J.Set(w, "MaxIsland", GameData.IslandCount - 1);
        WorldChanged();
        Rebuild();
    }
}
