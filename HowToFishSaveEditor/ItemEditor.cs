using System.Text.Json.Nodes;
using static HowToFishSaveEditor.L;

namespace HowToFishSaveEditor;

/// <summary>Editor for one SavedItem. Shows only the fields that matter for the selected item type.</summary>
public sealed class ItemEditor : FlowLayoutPanel
{
    public static bool ShowAllFields;

    private readonly JsonObject _item;
    private readonly Action _changed;
    private readonly Action _itemChanged;

    public ItemEditor(JsonObject item, Action changed, Action itemChanged = null)
    {
        _item = item;
        _changed = changed;
        _itemChanged = itemChanged;
        FlowDirection = FlowDirection.TopDown;
        WrapContents = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        Margin = Padding.Empty;
        Build();
    }

    public static IEnumerable<Opt> ItemOptions() =>
        GameData.Items.Select(i => new Opt(i.Id, $"{i.Name}   ·  {i.CategoryName}"));

    private void Build()
    {
        SuspendLayout();
        foreach (Control c in Controls.Cast<Control>().ToList()) c.Dispose();
        Controls.Clear();

        var def = GameData.Item((byte)J.Long(_item, "ItemID"));
        bool all = ShowAllFields || def == null;

        var g = new FieldGrid(_changed);
        g.Combo("Item", _item, "ItemID", ItemOptions(), T("Which item is in this slot.", "Welches Item in diesem Slot liegt."), v =>
        {
            var nd = GameData.Item((byte)v);
            if (J.Long(_item, "SkinIndex") >= (nd?.Skins?.Length ?? 1)) J.Set(_item, "SkinIndex", 0);
            if (nd != null && !nd.IsMelee) J.Set(_item, "Sharpness", 0);
            BeginInvoke(() => { Build(); _itemChanged?.Invoke(); });
        });

        if (def?.HasSkins == true)
            g.Combo("Skin", _item, "SkinIndex", def.Skins.Select((_, i) => new Opt(i, def.SkinLabel(i))),
                T("Skin of the item. Tip: the \"Skins\" tab unlocks skins permanently.",
                  "Skin des Items. Tipp: im Tab \"Skins\" kannst du Skins auch dauerhaft freischalten."));
        else if (all)
            g.Int(T("Skin index", "Skin-Index"), _item, "SkinIndex", 0, 255);

        if (all || def.IsCreature)
        {
            g.Bool(T("Drip (shiny)", "Drip (Shiny)"), _item, "IsDripCreature", T("Drip/shiny variant of the creature (worth a lot more).", "Drip-/Shiny-Variante der Kreatur (deutlich wertvoller)."));
            g.Float(T("Weight", "Gewicht"), _item, "Weight", 0, 100000, 3, T("Random weight of the creature (1.0 = normal). Affects the sell value.", "Zufallsgewicht der Kreatur (1.0 = Normal). Beeinflusst den Verkaufswert."));
            g.Float(T("Cookedness", "Garstufe"), _item, "Cookness", 0, 100, 3, T("How much the item is grilled (0 = raw).", "Wie stark das Item gegrillt ist (0 = roh)."), 0.05m);
            g.Float(T("Kill score multiplier", "Kill-Score-Multiplikator"), _item, "KillScoreMultiplier", 0, 100000, 3, T("Bonus multiplier from the kill score.", "Bonus-Multiplikator aus dem Kill-Score."));
        }
        g.Float(T("Betting multiplier", "Wett-Multiplikator"), _item, "BettingMultiplier", 0, 100000, 3, T("Multiplier from the casino (1.0 = normal).", "Multiplikator aus dem Casino (1.0 = normal)."));
        Controls.Add(g);

        if (all || def.IsGun || def.IsMelee)
        {
            var w = new FieldGrid(_changed);
            if (all || def.IsMelee)
                w.Int(T("Sharpness level", "Schärfe-Stufe"), _item, "Sharpness", 0, GameData.MaxSharpness, T("Upgrade level for knife / brass knuckles (max. 15).", "Upgrade-Stufe für Messer / Schlagring (max. 15)."));
            if (all || def.IsGun)
            {
                w.Combo(T("Sight", "Visier"), _item, "Sight", GameData.Sights.Select((s, i) => new Opt(i, s)));
                w.Combo(T("Barrel attachment", "Lauf-Aufsatz"), _item, "BarrelAttachment", GameData.Barrels.Select((s, i) => new Opt(i, s)));
                int maxAmmo = Math.Max(1, def?.BulletLevels ?? 13) - 1;
                w.Int(T("Ammo level", "Munitions-Stufe"), _item, "AmmoType", 0, maxAmmo, T($"Damage upgrade of the ammo (0 – {maxAmmo}).", $"Schadens-Upgrade der Munition (0 – {maxAmmo})."));
                w.Bool(T("Extended magazine", "Erweitertes Magazin"), _item, "ExtendedMag");
                w.Bool(T("Laser sight", "Laservisier"), _item, "LaserSight");
            }
            if (all || def.QuiverLevels > 0)
                w.Int(T("Quiver level", "Köcher-Stufe"), _item, "Quiver", 0, Math.Max(1, def?.QuiverLevels ?? 7) - 1, T("Draw speed of the bow.", "Spann-Geschwindigkeit des Bogens."));

            var header = new Label { Text = T("Upgrades & attachments", "Upgrades & Aufsätze"), Font = Theme.Bold, ForeColor = Theme.Accent, AutoSize = true, Margin = new Padding(0, 14, 0, 6) };
            Controls.Add(header);
            Controls.Add(w);
            if (all || def.IsGun)
                Controls.Add(Theme.Btn(T("Max out weapon", "Waffe maximieren"), Theme.ButtonKind.Accent, (_, _) =>
                {
                    J.Set(_item, "Sight", 1);
                    J.Set(_item, "BarrelAttachment", 2);
                    J.Set(_item, "AmmoType", Math.Max(1, def?.BulletLevels ?? 13) - 1);
                    J.Set(_item, "ExtendedMag", true);
                    J.Set(_item, "LaserSight", true);
                    if (def?.QuiverLevels > 0) J.Set(_item, "Quiver", def.QuiverLevels - 1);
                    _changed();
                    Build();
                }, T("Red dot, suppressor, highest ammo, extended mag, laser (bow: + quiver).",
                     "Red-Dot, Schalldämpfer, höchste Munition, erw. Magazin, Laser (Bogen: + Köcher).")));
            if (def?.IsMelee == true)
                Controls.Add(Theme.Btn(T("Max out sharpness", "Schärfe maximieren"), Theme.ButtonKind.Accent, (_, _) =>
                {
                    J.Set(_item, "Sharpness", GameData.MaxSharpness);
                    _changed();
                    Build();
                }));
        }

        Theme.Apply(this);
        ResumeLayout();
    }
}
