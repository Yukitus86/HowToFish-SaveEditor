using System.Text.Json.Nodes;
using static HowToFishSaveEditor.L;

namespace HowToFishSaveEditor.Pages;

public sealed class CharacterPage : Page
{
    private static (string Key, string Label)[] Colors => new[]
    {
        ("LocalSkinColor", T("Skin color", "Hautfarbe")),
        ("LocalHatColor", T("Hat / hair – color 1", "Hut / Haare – Farbe 1")),
        ("LocalHatColor2", T("Hat / hair – color 2", "Hut / Haare – Farbe 2")),
        ("LocalHatColor3", T("Hat / hair – color 3", "Hut / Haare – Farbe 3")),
        ("LocalOutfitColor", T("Outfit – color 1", "Outfit – Farbe 1")),
        ("LocalOutfitColor2", T("Outfit – color 2", "Outfit – Farbe 2")),
        ("LocalOutfitColor3", T("Outfit – color 3", "Outfit – Farbe 3")),
        ("LocalAccessoryColor", T("Accessory – color 1", "Accessoire – Farbe 1")),
        ("LocalAccessoryColor2", T("Accessory – color 2", "Accessoire – Farbe 2")),
        ("LocalAccessoryColor3", T("Accessory – color 3", "Accessoire – Farbe 3")),
    };

    public CharacterPage(SaveContext ctx) : base(ctx, T("Character & settings", "Charakter & Einstellungen"), T("Appearance and local settings (local.txt – applies to all worlds).", "Aussehen und lokale Einstellungen (local.txt – gilt für alle Welten)."))
    {
        var l = ctx.Local;
        var body = Ui.FlowBody();

        int Len(string key) => l[key] is JsonArray a && a.Count > 0 ? a.Count : 17;

        var g = new FieldGrid(LocalChanged);
        g.Int(T("Hat / hairstyle", "Hut / Frisur"), l, "LocalHatIndex", 0, Len("SeenHats") - 1);
        g.Int("Outfit", l, "LocalOutfitIndex", 0, Len("SeenOutfits") - 1);
        g.Int(T("Accessory", "Accessoire"), l, "LocalAccessoryIndex", 0, Len("SeenAccesories") - 1);
        g.Bool(T("Bean mode", "Bohnen-Modus (Bean)"), l, "IsBean", T("Play as a bean instead of a human.", "Spielt als Bohne statt als Mensch."));
        body.Controls.Add(Ui.Card(T("Appearance", "Aussehen"), Ui.Stack(g, Ui.ButtonRow(
            Theme.Btn(T("Mark all clothes as seen", "Alle Kleidung als gesehen markieren"), Theme.ButtonKind.Normal, (_, _) =>
            {
                foreach (var key in new[] { "SeenHats", "SeenOutfits", "SeenAccesories" })
                    if (l[key] is JsonArray a)
                        for (int i = 0; i < a.Count; i++) a[i] = true;
                LocalChanged();
            }, T("Removes the \"new\" markers in the wardrobe.", "Entfernt die \"Neu\"-Markierungen im Kleiderschrank."))))));

        var s = new FieldGrid(LocalChanged);
        s.Bool(T("Invert mouse X", "Maus X invertieren"), l, "InvertXInput");
        s.Bool(T("Invert mouse Y", "Maus Y invertieren"), l, "InvertYInput");
        s.Int(T("Game language (index)", "Spielsprache (Index)"), l, "SavedLanguage", -1, 50, T("-1 = automatic / system language. Otherwise the index in the game's language list.", "-1 = automatisch / Systemsprache. Sonst Index der Sprachliste im Spiel."));
        body.Controls.Add(Ui.Card(T("Settings", "Einstellungen"), s));

        var c = new FieldGrid(LocalChanged, 185, 260);
        foreach (var (key, label) in Colors)
            c.Vector(label, l, key, new[] { "x", "y", "z" }, -10, 10, 3, T("Position in the game's color picker (0/0/0 = default color).", "Position im Farbwähler des Spiels (0/0/0 = Standardfarbe)."));
        body.Controls.Add(Ui.Card(T("Colors", "Farben"), Ui.Stack(c, Ui.ButtonRow(
            Theme.Btn(T("Reset all colors to default", "Alle Farben auf Standard"), Theme.ButtonKind.Danger, (_, _) =>
            {
                foreach (var (key, _) in Colors) l[key] = new JsonObject { ["x"] = 0f, ["y"] = 0f, ["z"] = 0f };
                LocalChanged();
                Rebuild();
            }))), T("The values are coordinates in the game's color picker texture, not RGB values.", "Die Werte sind Koordinaten im Farbwähler-Texture des Spiels, keine RGB-Werte.")));

        Body.Controls.Add(body);
    }
}
