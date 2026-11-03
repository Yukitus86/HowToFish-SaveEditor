using System.Text.Json.Nodes;
using static HowToFishSaveEditor.L;

namespace HowToFishSaveEditor.Pages;

public sealed class SkinsPage : Page
{
    private readonly JsonObject _local;
    private readonly ListBox _targets = new() { Dock = DockStyle.Left, Width = 280, IntegralHeight = false, DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 32 };
    private readonly CheckedListBox _skins = new() { Width = 380, Height = 330, CheckOnClick = true, IntegralHeight = false };
    private readonly Label _title = new() { Font = Theme.Bold, ForeColor = Theme.Accent, AutoSize = true, Margin = new Padding(0, 0, 0, 8) };
    private bool _filling;

    private sealed record Target(byte Id, string Name, string[] Skins, int[] Rarity);

    public SkinsPage(SaveContext ctx) : base(ctx, "Skins", T("Unlocked weapon, tool and boat skins (per profile). The default skin is always available.", "Freigeschaltete Waffen-, Werkzeug- und Boot-Skins (profilweit). Der Standard-Skin ist immer verfügbar."))
    {
        _local = ctx.Local;
        foreach (var d in GameData.Items.Where(i => i.HasSkins).OrderBy(i => i.Name))
            _targets.Items.Add(new Target(d.Id, d.Name, d.Skins, d.SkinRarity));
        _targets.Items.Add(new Target(GameData.BoatSkinItemId, T("Boat", "Boot"), GameData.BoatSkins, GameData.BoatSkinRarity));
        _targets.DrawItem += DrawTarget;
        _targets.SelectedIndexChanged += (_, _) => ShowTarget();
        _skins.ItemCheck += OnCheck;

        var right = new Panel { Dock = DockStyle.Fill, Padding = new Padding(18, 0, 0, 0) };
        var stack = Ui.Stack(_skins, Ui.ButtonRow(
            Theme.Btn(T("All", "Alle"), click: (_, _) => CheckAll(true)),
            Theme.Btn(T("None", "Keine"), click: (_, _) => CheckAll(false))));
        var global = Ui.Card(T("Everything at once", "Alles auf einmal"), Ui.ButtonRow(
            Theme.Btn(T("Unlock all skins of all items + boat", "Alle Skins aller Items + Boot freischalten"), Theme.ButtonKind.Accent, (_, _) => UnlockEverything(true)),
            Theme.Btn(T("Lock all skins", "Alle Skins sperren"), Theme.ButtonKind.Danger, (_, _) => UnlockEverything(false))));
        var flow = Ui.FlowBody();
        flow.Controls.Add(Ui.Card(null, Ui.Stack(_title, stack)));
        flow.Controls.Add(global);
        right.Controls.Add(flow);

        Body.Controls.Add(right);
        Body.Controls.Add(_targets);
        _targets.SelectedIndex = 0;
    }

    private List<byte> Unlocked(Target t)
    {
        if (t.Id == GameData.BoatSkinItemId)
            return J.Arr(_local, "UnlockedBoatSkins").Select(n => (byte)J.ToLong(n)).ToList();
        var e = J.Arr(_local, "UnlockedItemSkins").OfType<JsonObject>().FirstOrDefault(o => J.Long(o, "ID") == t.Id);
        return e == null ? new() : J.Arr(e, "SkinsUnlocked").Select(n => (byte)J.ToLong(n)).ToList();
    }

    private void Store(Target t, IEnumerable<byte> skins)
    {
        var sorted = new JsonArray(skins.Distinct().Where(s => s > 0).OrderBy(s => s).Select(s => (JsonNode)s).ToArray());
        if (t.Id == GameData.BoatSkinItemId)
        {
            _local["UnlockedBoatSkins"] = sorted;
        }
        else
        {
            var arr = J.Arr(_local, "UnlockedItemSkins");
            var e = arr.OfType<JsonObject>().FirstOrDefault(o => J.Long(o, "ID") == t.Id);
            if (e == null)
            {
                if (sorted.Count == 0) return;
                e = new JsonObject { ["ID"] = t.Id };
                arr.Add(e);
            }
            e["SkinsUnlocked"] = sorted;
        }
        LocalChanged();
    }

    private void DrawTarget(object sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;
        var t = (Target)_targets.Items[e.Index];
        bool sel = (e.State & DrawItemState.Selected) != 0;
        using var bg = new SolidBrush(sel ? Theme.AccentDark : (e.Index % 2 == 0 ? Theme.Input : Theme.Card));
        e.Graphics.FillRectangle(bg, e.Bounds);
        var r = e.Bounds;
        int have = Unlocked(t).Count(s => s > 0 && s < t.Skins.Length);
        TextRenderer.DrawText(e.Graphics, t.Name, Theme.Base, new Rectangle(r.X + 10, r.Y, r.Width - 80, r.Height), Theme.Text, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        TextRenderer.DrawText(e.Graphics, $"{have}/{t.Skins.Length - 1}", Theme.Small, new Rectangle(r.Right - 70, r.Y, 62, r.Height), have == t.Skins.Length - 1 ? Theme.Ok : Theme.Muted, TextFormatFlags.VerticalCenter | TextFormatFlags.Right);
    }

    private void ShowTarget()
    {
        if (_targets.SelectedItem is not Target t) return;
        _filling = true;
        _title.Text = $"{t.Name} – Skins";
        _skins.Items.Clear();
        var have = Unlocked(t);
        for (int i = 1; i < t.Skins.Length; i++)
            _skins.Items.Add($"{t.Skins[i]}   ({GameData.RarityName(t.Rarity[i])})", have.Contains((byte)i));
        _filling = false;
    }

    private void OnCheck(object sender, ItemCheckEventArgs e)
    {
        if (_filling || _targets.SelectedItem is not Target t) return;
        var set = new HashSet<byte>();
        for (int i = 0; i < _skins.Items.Count; i++)
        {
            bool on = i == e.Index ? e.NewValue == CheckState.Checked : _skins.GetItemChecked(i);
            if (on) set.Add((byte)(i + 1));
        }
        // keep unknown indices that the editor does not know about
        set.UnionWith(Unlocked(t).Where(s => s >= t.Skins.Length));
        Store(t, set);
        _targets.Invalidate();
    }

    private void CheckAll(bool on)
    {
        if (_targets.SelectedItem is not Target t) return;
        Store(t, on ? Enumerable.Range(1, t.Skins.Length - 1).Select(i => (byte)i) : Unlocked(t).Where(s => s >= t.Skins.Length));
        ShowTarget();
        _targets.Invalidate();
    }

    private void UnlockEverything(bool on)
    {
        foreach (Target t in _targets.Items)
            Store(t, on ? Enumerable.Range(1, t.Skins.Length - 1).Select(i => (byte)i) : Array.Empty<byte>());
        ShowTarget();
        _targets.Invalidate();
    }
}
