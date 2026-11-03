using System.Text.Json.Nodes;
using static HowToFishSaveEditor.L;

namespace HowToFishSaveEditor.Pages;

public sealed class JournalPage : Page
{
    private readonly JsonArray _list;
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill };
    private readonly Label _summary = new() { AutoSize = true, ForeColor = Theme.Muted, Margin = new Padding(8, 8, 0, 0) };
    private bool _filling;

    public JournalPage(SaveContext ctx) : base(ctx, "Journal", T("Caught / killed creatures (per profile, applies to all worlds). Drip = shiny variant.", "Gefangene / getötete Kreaturen (profilweit, gilt für alle Welten). Drip = Shiny-Variante."))
    {
        _list = J.Arr(ctx.Local, "CreaturesToList");

        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = T("Creature", "Kreatur"), ReadOnly = true, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 200 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = T("Category", "Kategorie"), ReadOnly = true, Width = 130 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "ID", ReadOnly = true, Width = 60 });
        _grid.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = T("Caught", "Gefangen"), Width = 100 });
        _grid.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Drip ✦", Width = 100 });
        _grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
        _grid.CurrentCellDirtyStateChanged += (_, _) => { if (_grid.IsCurrentCellDirty) _grid.CommitEdit(DataGridViewDataErrorContexts.Commit); };
        _grid.CellValueChanged += OnCellChanged;

        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Margin = Padding.Empty, Padding = new Padding(0, 0, 0, 6) };
        bar.Controls.Add(Theme.Btn(T("All caught", "Alle gefangen"), Theme.ButtonKind.Accent, (_, _) => SetAll(true, null)));
        bar.Controls.Add(Theme.Btn(T("All caught + drip", "Alle gefangen + Drip"), Theme.ButtonKind.Accent, (_, _) => SetAll(true, true)));
        bar.Controls.Add(Theme.Btn(T("Remove drip", "Drip entfernen"), click: (_, _) => SetAll(null, false)));
        bar.Controls.Add(Theme.Btn(T("Clear journal", "Journal leeren"), Theme.ButtonKind.Danger, (_, _) =>
        {
            if (MessageBox.Show(T("Reset the whole journal?", "Komplettes Journal zurücksetzen?"), "Journal", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            _list.Clear();
            LocalChanged();
            Fill();
        }));
        bar.Controls.Add(_summary);

        Body.Controls.Add(_grid);
        Body.Controls.Add(bar);
        Fill();
    }

    private JsonObject Entry(long id) => _list.OfType<JsonObject>().FirstOrDefault(o => J.Long(o, "ID") == id);

    private void Fill()
    {
        _filling = true;
        _grid.Rows.Clear();
        var ids = GameData.Items.Where(i => i.InJournal).Select(i => (long)i.Id).ToList();
        ids.AddRange(_list.OfType<JsonObject>().Select(o => J.Long(o, "ID")).Where(id => !ids.Contains(id)));
        foreach (var id in ids.OrderBy(i => GameData.Item((byte)i)?.Category ?? ItemCategory.Misc).ThenBy(GameData.ItemName))
        {
            var e = Entry(id);
            var def = GameData.Item((byte)id);
            _grid.Rows.Add(GameData.ItemName(id), def?.CategoryName ?? "?", id, J.Bool(e, "BeenKilled"), J.Bool(e, "KilledDrip"));
            _grid.Rows[^1].Tag = id;
        }
        _filling = false;
        UpdateSummary();
    }

    private void UpdateSummary()
    {
        int total = _grid.Rows.Count;
        int caught = _grid.Rows.Cast<DataGridViewRow>().Count(r => r.Cells[3].Value is true);
        int drip = _grid.Rows.Cast<DataGridViewRow>().Count(r => r.Cells[4].Value is true);
        _summary.Text = T($"Caught: {caught}/{total}    Drip: {drip}/{total}", $"Gefangen: {caught}/{total}    Drip: {drip}/{total}");
    }

    private void Write(long id, bool killed, bool drip)
    {
        var e = Entry(id);
        if (!killed && !drip)
        {
            if (e != null) _list.Remove(e);
        }
        else
        {
            if (e == null)
            {
                e = new JsonObject { ["ID"] = (byte)id, ["BeenKilled"] = false, ["KilledDrip"] = false };
                _list.Add(e);
            }
            J.Set(e, "BeenKilled", killed);
            J.Set(e, "KilledDrip", drip);
        }
        LocalChanged();
    }

    private void OnCellChanged(object sender, DataGridViewCellEventArgs e)
    {
        if (_filling || e.RowIndex < 0 || e.ColumnIndex < 3) return;
        var row = _grid.Rows[e.RowIndex];
        bool killed = row.Cells[3].Value is true, drip = row.Cells[4].Value is true;
        if (e.ColumnIndex == 4 && drip && !killed)
        {
            killed = true;
            _filling = true;
            row.Cells[3].Value = true;
            _filling = false;
        }
        Write((long)row.Tag, killed, drip);
        UpdateSummary();
    }

    private void SetAll(bool? killed, bool? drip)
    {
        foreach (DataGridViewRow r in _grid.Rows)
        {
            bool k = killed ?? (r.Cells[3].Value is true);
            bool d = drip ?? (r.Cells[4].Value is true);
            if (d) k = true;
            Write((long)r.Tag, k, d);
        }
        Fill();
    }
}
