using System.Text.Json.Nodes;
using static HowToFishSaveEditor.L;

namespace HowToFishSaveEditor.Pages;

public sealed class PlayerPage : Page
{
    public PlayerPage(SaveContext ctx) : base(ctx, T("Player", "Spieler"), T("Stats, upgrades and baits per player (in singleplayer that is only you).", "Werte, Upgrades und Köder pro Spieler (in Singleplayer gibt es nur dich)."))
    {
        var body = Ui.FlowBody();
        var player = ctx.CurrentPlayer;
        Body.Controls.Add(body);
        Body.Controls.Add(PlayerSelector(ctx, Rebuild));
        if (player == null)
        {
            body.Controls.Add(Ui.Hint(NoPlayerText));
            return;
        }

        var g = new FieldGrid(WorldChanged);
        var hp = g.Int(T("Health", "Leben"), player, "Health", 0, 100);
        var food = g.Int(T("Fullness", "Sättigung"), player, "Fullness", 0, 100);
        g.Bool(T("Tutorial finished", "Tutorial abgeschlossen"), player, "HasFinishedTutorial");
        body.Controls.Add(Ui.Card(T("Vitals", "Vitalwerte"), Ui.Stack(g, Ui.ButtonRow(
            Theme.Btn(T("Full health & fed", "Voll heilen & satt"), click: (_, _) => { hp.Value = 100; food.Value = 100; })))));

        var u = new FieldGrid(WorldChanged);
        var slots = u.Int(T("Extra inventory slots", "Extra-Inventarslots"), player, "ExtraSlots", 0, GameData.MaxExtraSlots,
            T($"Additional pocket slots (start: {GameData.StartingSlots} slots, max. +{GameData.MaxExtraSlots}).", $"Zusätzliche Taschen-Slots (Start: {GameData.StartingSlots} Slots, max. +{GameData.MaxExtraSlots})."));
        var wish = u.Int(T("Wishbone level", "Wishbone-Stufe"), player, "WishboneIndex", 0, GameData.MaxWishbone, T("Raises the chance for drip/shiny creatures (max. 5).", "Erhöht die Chance auf Drip/Shiny-Kreaturen (max. 5)."));
        body.Controls.Add(Ui.Card("Upgrades", Ui.Stack(u, Ui.ButtonRow(
            Theme.Btn(T("Max all upgrades", "Alle Upgrades max"), Theme.ButtonKind.Accent, (_, _) => { slots.Value = GameData.MaxExtraSlots; wish.Value = GameData.MaxWishbone; })))));

        body.Controls.Add(BaitCard(player));
    }

    public static string NoPlayerText => T("No player is stored in this world yet.", "In dieser Welt ist noch kein Spieler gespeichert.");

    public static Control PlayerSelector(SaveContext ctx, Action changed)
    {
        var players = ctx.Players;
        var cb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 320 };
        string self = Path.GetFileName(ctx.ProfileFolder?.TrimEnd('\\', '/') ?? "");
        for (int i = 0; i < (players?.Count ?? 0); i++)
        {
            var id = (players[i] as JsonObject)?["SteamID"]?.ToJsonString() ?? "?";
            cb.Items.Add(id == self ? T($"You  ({id})", $"Du  ({id})") : T($"Player {i + 1}  ({id})", $"Spieler {i + 1}  ({id})"));
        }
        if (cb.Items.Count > 0) cb.SelectedIndex = Math.Clamp(ctx.PlayerIndex, 0, cb.Items.Count - 1);
        cb.SelectedIndexChanged += (_, _) => { ctx.PlayerIndex = cb.SelectedIndex; changed(); };
        var g = new FieldGrid(() => { }, 80, 320);
        g.AddRow(T("Player", "Spieler"), cb);
        var wrap = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 0, 0, 12) };
        wrap.Controls.Add(g);
        return wrap;
    }

    private Control BaitCard(JsonObject player)
    {
        var baits = J.Arr(player, "OwnedBaits");
        var grid = new DataGridView { Width = 440, Height = 30 + 28 * (GameData.Baits.Length - 1) + 2, ScrollBars = ScrollBars.None };
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = T("Bait", "Köder"), ReadOnly = true, Width = 230 });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = T("Price", "Preis"), ReadOnly = true, Width = 80, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
        grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = T("Amount", "Anzahl"), Width = 110, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
        grid.SelectionMode = DataGridViewSelectionMode.CellSelect;

        // Index 0 is the "empty" bait slot and is not shown.
        for (int i = 1; i < GameData.Baits.Length; i++)
        {
            long have = i < baits.Count ? J.ToLong(baits[i]) : 0;
            grid.Rows.Add(GameData.Baits[i].Name, $"$ {GameData.Baits[i].Cost:N0}", have);
            grid.Rows[^1].Tag = i;
        }

        void SetCount(int index, long value)
        {
            while (baits.Count <= index) baits.Add(0);
            baits[index] = (int)Math.Clamp(value, 0, int.MaxValue);
            WorldChanged();
        }

        static bool ParseCount(object v, out long n) =>
            long.TryParse(Convert.ToString(v)?.Replace(".", "").Replace(",", ""), out n) && n >= 0;

        grid.CellValidating += (_, e) =>
        {
            if (e.ColumnIndex != 2 || ParseCount(e.FormattedValue, out long _)) return;
            e.Cancel = true;
            MessageBox.Show(T("Please enter a number ≥ 0.", "Bitte eine Zahl ≥ 0 eingeben."), T("Bait", "Köder"), MessageBoxButtons.OK, MessageBoxIcon.Information);
        };
        grid.CellValueChanged += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex != 2) return;
            var row = grid.Rows[e.RowIndex];
            if (ParseCount(row.Cells[2].Value, out long v)) SetCount((int)row.Tag, v);
        };

        void SetAll(long v)
        {
            foreach (DataGridViewRow r in grid.Rows) r.Cells[2].Value = v;
        }

        var buttons = Ui.ButtonRow(
            Theme.Btn(T("All to 99", "Alle auf 99"), click: (_, _) => SetAll(99)),
            Theme.Btn(T("All to 999", "Alle auf 999"), Theme.ButtonKind.Accent, (_, _) => SetAll(999)),
            Theme.Btn(T("All to 0", "Alle auf 0"), Theme.ButtonKind.Danger, (_, _) => SetAll(0)));
        return Ui.Card(T("Baits", "Köder"), Ui.Stack(grid, buttons),
            T("Double-click \"Amount\" to edit. Default baits (Ham / French Fry) are unlimited and not stored.",
              "Doppelklick auf \"Anzahl\" zum Bearbeiten. Standard-Köder (Ham / French Fry) sind unendlich und werden nicht gespeichert."));
    }
}
