using System.Text.Json.Nodes;
using static HowToFishSaveEditor.L;

namespace HowToFishSaveEditor.Pages;

public sealed class InventoryPage : Page
{
    private readonly JsonObject _player;
    private readonly ListBox _slots = new() { Width = 270, IntegralHeight = false, DrawMode = DrawMode.OwnerDrawFixed, ItemHeight = 34 };
    private readonly Panel _editor = new() { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(18, 0, 0, 0) };

    private sealed record Slot(int Index, JsonObject Item, string Label); // Index -1 = hand

    public InventoryPage(SaveContext ctx) : base(ctx, T("Inventory", "Inventar"), T("Items in your slots and in your hand – incl. skins, weapon attachments and upgrades.", "Items in deinen Slots und in der Hand – inkl. Skins, Waffen-Aufsätzen und Upgrades."))
    {
        _player = ctx.CurrentPlayer;
        var top = PlayerPage.PlayerSelector(ctx, Rebuild);
        if (_player == null)
        {
            Body.Controls.Add(Ui.Hint(PlayerPage.NoPlayerText));
            Body.Controls.Add(top);
            return;
        }

        _slots.Dock = DockStyle.Left;
        _slots.DrawItem += DrawSlot;
        _slots.SelectedIndexChanged += (_, _) => ShowSlot();

        Body.Controls.Add(_editor);
        Body.Controls.Add(_slots);
        Body.Controls.Add(top);
        FillSlots(0);
    }

    private int SlotCount => GameData.StartingSlots + (int)Math.Clamp(J.Long(_player, "ExtraSlots"), 0, 20);

    private void FillSlots(int select)
    {
        var inv = J.Arr(_player, "InventoryItems");
        var list = new List<Slot>();
        var held = J.Obj(_player, "HeldItem", () => J.NewItem(0, 0, false));
        list.Add(new Slot(-1, J.Bool(held, "Exists") ? held : null, T("Hand", "Hand")));
        int count = SlotCount;
        int maxUsed = inv.OfType<JsonObject>().Select(i => (int)J.Long(i, "InventorySlot")).DefaultIfEmpty(-1).Max();
        for (int s = 0; s < Math.Max(count, maxUsed + 1); s++)
        {
            var item = inv.OfType<JsonObject>().FirstOrDefault(i => J.Long(i, "InventorySlot") == s);
            list.Add(new Slot(s, item, s < count ? $"Slot {s + 1}" : T($"Slot {s + 1} (locked)", $"Slot {s + 1} (gesperrt)")));
        }
        _slots.BeginUpdate();
        _slots.Items.Clear();
        foreach (var sl in list) _slots.Items.Add(sl);
        _slots.EndUpdate();
        _slots.SelectedIndex = Math.Clamp(select, 0, _slots.Items.Count - 1);
    }

    private void DrawSlot(object sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;
        var sl = (Slot)_slots.Items[e.Index];
        bool sel = (e.State & DrawItemState.Selected) != 0;
        using var bg = new SolidBrush(sel ? Theme.AccentDark : (e.Index % 2 == 0 ? Theme.Input : Theme.Card));
        e.Graphics.FillRectangle(bg, e.Bounds);
        var r = e.Bounds;
        TextRenderer.DrawText(e.Graphics, sl.Label, Theme.Small, new Rectangle(r.X + 10, r.Y, 110, r.Height), Theme.Muted, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        string name = sl.Item == null ? T("— empty —", "— leer —") : GameData.ItemName(J.Long(sl.Item, "ItemID"));
        if (sl.Item != null && J.Bool(sl.Item, "IsDripCreature")) name += " ✦";
        TextRenderer.DrawText(e.Graphics, name, Theme.Base, new Rectangle(r.X + 110, r.Y, r.Width - 115, r.Height), sl.Item == null ? Theme.Muted : Theme.Text, TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.EndEllipsis);
    }

    private void ShowSlot()
    {
        _editor.SuspendLayout();
        foreach (Control c in _editor.Controls.Cast<Control>().ToList()) c.Dispose();
        _editor.Controls.Clear();
        if (_slots.SelectedItem is not Slot sl) { _editor.ResumeLayout(); return; }

        int selIndex = _slots.SelectedIndex;
        var stack = Ui.Stack();
        if (sl.Item == null)
        {
            var pick = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 };
            pick.Items.AddRange(ItemEditor.ItemOptions().Cast<object>().ToArray());
            pick.SelectedItem = pick.Items.Cast<Opt>().First(o => o.Value == 61);
            var g = new FieldGrid(() => { });
            g.AddRow(T("New item", "Neues Item"), pick);
            stack.Controls.Add(g);
            stack.Controls.Add(Ui.ButtonRow(Theme.Btn(T("Put item in slot", "Item in Slot legen"), Theme.ButtonKind.Accent, (_, _) =>
            {
                var id = (byte)((Opt)pick.SelectedItem).Value;
                if (sl.Index < 0)
                    _player["HeldItem"] = J.NewItem(id, 0);
                else
                    J.Arr(_player, "InventoryItems").Add(J.NewItem(id, (byte)sl.Index));
                WorldChanged();
                FillSlots(selIndex);
            })));
            if (sl.Index >= SlotCount)
                stack.Controls.Add(Ui.Hint(T("This slot is locked – raise the extra inventory slots in the \"Player\" tab first.", "Dieser Slot ist gesperrt – erhöhe zuerst die Extra-Inventarslots im Tab \"Spieler\"."), Theme.Warn));
            _editor.Controls.Add(Ui.Card(sl.Label, stack, T("Slot is empty.", "Slot ist leer.")));
        }
        else
        {
            stack.Controls.Add(new ItemEditor(sl.Item, WorldChanged, () => _slots.Invalidate()));
            stack.Controls.Add(Ui.ButtonRow(
                Theme.Btn(T("Clear slot", "Slot leeren"), Theme.ButtonKind.Danger, (_, _) =>
                {
                    if (sl.Index < 0) J.Set(sl.Item, "Exists", false);
                    else J.Arr(_player, "InventoryItems").Remove(sl.Item);
                    WorldChanged();
                    FillSlots(selIndex);
                })));
            if (sl.Index >= SlotCount)
                stack.Controls.Add(Ui.Hint(T("Warning: this slot is outside your unlocked slots.", "Achtung: Dieser Slot liegt außerhalb deiner freigeschalteten Slots."), Theme.Warn));
            _editor.Controls.Add(Ui.Card(sl.Label, stack, sl.Index < 0 ? T("Item you hold in your hand when loading.", "Item, das du beim Laden in der Hand hältst.") : null));
        }
        Theme.Apply(_editor);
        _editor.ResumeLayout();
    }
}
