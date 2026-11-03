using System.Text.Json.Nodes;
using static HowToFishSaveEditor.L;

namespace HowToFishSaveEditor.Pages;

public sealed class WorldItemsPage : Page
{
    private readonly JsonArray _items;
    private readonly ListBox _list = new() { Width = 300, IntegralHeight = false, DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 30, Dock = DockStyle.Left };
    private readonly Panel _editor = new() { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(18, 0, 0, 0) };

    public WorldItemsPage(SaveContext ctx) : base(ctx, T("World items", "Welt-Items"), T("Everything lying around on the island / in the boat (caught fish, weapons, trophies …).", "Alles, was auf der Insel / im Boot herumliegt (gefangene Fische, Waffen, Trophäen …)."))
    {
        _items = J.Arr(ctx.World, "WorldItems");
        _list.DrawItem += DrawEntry;
        _list.SelectedIndexChanged += (_, _) => ShowItem();
        Body.Controls.Add(_editor);
        Body.Controls.Add(_list);
        Fill(0);
    }

    private void Fill(int select)
    {
        _list.BeginUpdate();
        _list.Items.Clear();
        for (int i = 0; i < _items.Count; i++) _list.Items.Add(i);
        _list.EndUpdate();
        if (_list.Items.Count > 0) _list.SelectedIndex = Math.Clamp(select, 0, _list.Items.Count - 1);
        else ShowItem();
    }

    private void DrawEntry(object sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;
        bool sel = (e.State & DrawItemState.Selected) != 0;
        using var bg = new SolidBrush(sel ? Theme.AccentDark : (e.Index % 2 == 0 ? Theme.Input : Theme.Card));
        e.Graphics.FillRectangle(bg, e.Bounds);
        var wi = _items[e.Index] as JsonObject;
        var item = wi?["Item"] as JsonObject;
        string name = GameData.ItemName(J.Long(item, "ItemID"));
        if (J.Bool(item, "IsDripCreature")) name += " ✦";
        if (J.Bool(wi, "IsDeadCreature")) name += T("  (dead)", "  (tot)");
        var r = e.Bounds;
        TextRenderer.DrawText(e.Graphics, $"#{e.Index + 1}", Theme.Small, new Rectangle(r.X + 8, r.Y, 40, r.Height), Theme.Muted, TextFormatFlags.VerticalCenter);
        TextRenderer.DrawText(e.Graphics, name, Theme.Base, new Rectangle(r.X + 48, r.Y, r.Width - 52, r.Height), Theme.Text, TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    private void ShowItem()
    {
        _editor.SuspendLayout();
        foreach (Control c in _editor.Controls.Cast<Control>().ToList()) c.Dispose();
        _editor.Controls.Clear();

        int idx = _list.SelectedIndex;
        var actions = Ui.ButtonRow(
            Theme.Btn(T("Delete all", "Alle löschen"), Theme.ButtonKind.Danger, (_, _) =>
            {
                if (_items.Count == 0 || MessageBox.Show(T($"Delete all {_items.Count} world items?", $"Alle {_items.Count} Welt-Items löschen?"), T("World items", "Welt-Items"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
                _items.Clear();
                WorldChanged();
                Fill(0);
            }));

        if (idx < 0 || _items[idx] is not JsonObject wi)
        {
            _editor.Controls.Add(Ui.Card(T("No world items", "Keine Welt-Items"), Ui.Stack(Ui.Hint(T("There are no items lying around in this world.", "In dieser Welt liegen keine Items herum.")), actions)));
        }
        else
        {
            var item = J.Obj(wi, "Item", () => J.NewItem(20, 0));
            var stack = Ui.Stack(new ItemEditor(item, WorldChanged, () => _list.Invalidate()));

            // narrower labels, wider fields: same total width as the item editor, room for 4 quaternion boxes
            var g = new FieldGrid(WorldChanged, 150, 275);
            g.Bool(T("Dead creature", "Tote Kreatur"), wi, "IsDeadCreature", T("Creature lies there dead (otherwise it is alive / flopping).", "Kreatur liegt tot herum (sonst zappelt / lebt sie)."), _ => _list.Invalidate());
            g.Vector("Position", wi, "Position", new[] { "x", "y", "z" }, -100000, 100000, 2);
            g.Vector("Rotation (Quaternion)", wi, "Rotation", new[] { "x", "y", "z", "w" }, -1, 1, 3);
            stack.Controls.Add(new Label { Text = "Position", Font = Theme.Bold, ForeColor = Theme.Accent, AutoSize = true, Margin = new Padding(0, 14, 0, 6) });
            stack.Controls.Add(g);
            actions.Controls.Add(Theme.Btn(T("Duplicate", "Duplizieren"), Theme.ButtonKind.Normal, (_, _) =>
            {
                var copy = wi.DeepClone().AsObject();
                if (copy["Position"] is JsonObject p) J.SetF(p, "y", (float)J.Num(p, "y") + 0.5f);
                _items.Insert(idx + 1, copy);
                WorldChanged();
                Fill(idx + 1);
            }, T("Places a copy slightly above.", "Legt eine Kopie leicht darüber ab.")));
            actions.Controls.Add(Theme.Btn(T("Delete", "Löschen"), Theme.ButtonKind.Danger, (_, _) =>
            {
                _items.RemoveAt(idx);
                WorldChanged();
                Fill(idx);
            }));
            actions.Controls.SetChildIndex(actions.Controls[0], actions.Controls.Count - 1);
            stack.Controls.Add(actions);
            _editor.Controls.Add(Ui.Card(T($"World item #{idx + 1}", $"Welt-Item #{idx + 1}"), stack));
        }
        Theme.Apply(_editor);
        _editor.ResumeLayout();
    }
}
