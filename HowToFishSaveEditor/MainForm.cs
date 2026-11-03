using HowToFishSaveEditor.Pages;
using static HowToFishSaveEditor.L;

namespace HowToFishSaveEditor;

public sealed class MainForm : Form
{
    public const string VersionText = "1.0";

    private sealed class NavEntry
    {
        public string Icon;
        public Func<string> Title;
        public bool NeedsWorld;
        public bool NeedsLocal;
        public Func<SaveContext, Page> Create;
        public Label Button;
    }

    private readonly SaveContext _ctx = new();
    private readonly ComboBox _langBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };
    private readonly ComboBox _profileBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
    private readonly ComboBox _worldBox = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
    private readonly Panel _content = new() { Dock = DockStyle.Fill };
    private readonly FlowLayoutPanel _nav = new() { Dock = DockStyle.Left, Width = 200, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Theme.Side, Padding = new Padding(0, 14, 0, 0) };
    private readonly Label _status = new() { AutoSize = true, ForeColor = Theme.Muted, Margin = new Padding(12, 6, 12, 0) };
    private readonly Label _dirty = new() { AutoSize = true, Margin = new Padding(12, 6, 12, 0) };
    private readonly Label _game = new() { AutoSize = true, Margin = new Padding(12, 6, 12, 0) };
    private readonly Label _langCap = Caption(), _profileCap = Caption(), _worldCap = Caption();
    private readonly Button _saveBtn, _reloadBtn;
    private readonly List<NavEntry> _entries = new();
    private readonly System.Windows.Forms.Timer _gameTimer = new() { Interval = 3000 };
    private NavEntry _active;
    private bool _loading;

    public MainForm()
    {
        Icon = Theme.AppIcon();
        BackColor = Theme.Bg;
        ForeColor = Theme.Text;
        Font = Theme.Base;
        MinimumSize = new Size(1150, 700);
        Size = new Size(1400, 880);
        StartPosition = FormStartPosition.CenterScreen;
        KeyPreview = true;

        _saveBtn = Theme.Btn("", Theme.ButtonKind.Accent, (_, _) => Save(), "Ctrl+S");
        _reloadBtn = Theme.Btn("", click: (_, _) => Reload(), tip: "F5");

        AddNav("🌍", () => T("World", "Welt"), true, false, c => new WorldPage(c));
        AddNav("🧍", () => T("Player", "Spieler"), true, false, c => new PlayerPage(c));
        AddNav("🎒", () => T("Inventory", "Inventar"), true, false, c => new InventoryPage(c));
        AddNav("🏝", () => T("World items", "Welt-Items"), true, false, c => new WorldItemsPage(c));
        AddNav("📖", () => "Journal", false, true, c => new JournalPage(c));
        AddNav("🎨", () => "Skins", false, true, c => new SkinsPage(c));
        AddNav("👤", () => T("Character", "Charakter"), false, true, c => new CharacterPage(c));
        AddNav("{ }", () => "Raw JSON", false, false, c => new RawJsonPage(c));
        AddNav("ℹ", () => "Info", false, false, c => new InfoPage(c));

        Controls.Add(_content);
        Controls.Add(_nav);
        Controls.Add(BuildHeader());
        Controls.Add(BuildStatusBar());

        _langBox.Items.AddRange(new object[] { "English", "Deutsch" });
        _langBox.SelectedIndex = De ? 1 : 0;
        _langBox.SelectedIndexChanged += (_, _) => L.Set(_langBox.SelectedIndex == 1 ? Lang.German : Lang.English);
        L.Changed += OnLanguageChanged;

        _profileBox.SelectedIndexChanged += (_, _) => { if (!_loading) OpenProfile(); };
        _worldBox.SelectedIndexChanged += (_, _) => { if (!_loading) OpenWorld(); };
        _ctx.DirtyChanged += UpdateDirty;
        _gameTimer.Tick += (_, _) => UpdateGameState();
        _gameTimer.Start();

        Theme.Apply(this);
        ApplyTexts();
        Load += (_, _) => Init();
    }

    [System.Runtime.InteropServices.DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        // Dark title bar (Win10 1809+); caption/border color only on Win11, ignored elsewhere.
        int on = 1, caption = ColorTranslator.ToWin32(Theme.Side), text = ColorTranslator.ToWin32(Theme.Text);
        DwmSetWindowAttribute(Handle, 20, ref on, sizeof(int));      // DWMWA_USE_IMMERSIVE_DARK_MODE
        DwmSetWindowAttribute(Handle, 35, ref caption, sizeof(int)); // DWMWA_CAPTION_COLOR
        DwmSetWindowAttribute(Handle, 34, ref caption, sizeof(int)); // DWMWA_BORDER_COLOR
        DwmSetWindowAttribute(Handle, 36, ref text, sizeof(int));    // DWMWA_TEXT_COLOR
    }

    private static Label Caption() => new() { AutoSize = true, ForeColor = Theme.Muted, Margin = new Padding(10, 12, 6, 0) };

    private void AddNav(string icon, Func<string> title, bool needsWorld, bool needsLocal, Func<SaveContext, Page> create)
    {
        var e = new NavEntry { Icon = icon, Title = title, NeedsWorld = needsWorld, NeedsLocal = needsLocal, Create = create };
        // Labels instead of Buttons: the dark-mode button renderer always draws a border.
        var b = new Label
        {
            AutoSize = false,
            Width = 200,
            Height = 42,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(18, 0, 0, 0),
            Margin = Padding.Empty,
            BackColor = Theme.Side,
            ForeColor = Theme.Muted,
            Font = new Font("Segoe UI", 10.5f),
            Cursor = Cursors.Hand,
            UseMnemonic = false,
        };
        b.MouseEnter += (_, _) => { if (_active != e) b.BackColor = Theme.Hover; };
        b.MouseLeave += (_, _) => { if (_active != e) b.BackColor = Theme.Side; };
        b.Paint += (_, pe) =>
        {
            if (_active != e) return;
            using var br = new SolidBrush(Theme.Accent);
            pe.Graphics.FillRectangle(br, 0, 6, 4, b.Height - 12);
        };
        b.Click += (_, _) => ShowPage(e);
        e.Button = b;
        _entries.Add(e);
        _nav.Controls.Add(b);
    }

    private Control BuildHeader()
    {
        var header = new TableLayoutPanel { Dock = DockStyle.Top, Height = 64, ColumnCount = 3, BackColor = Theme.Side, Padding = new Padding(14, 10, 14, 6) };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var title = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = Padding.Empty };
        title.Controls.Add(new PictureBox { Image = Theme.Logo(), SizeMode = PictureBoxSizeMode.Zoom, Size = new Size(42, 42), Margin = new Padding(0, 0, 10, 0) });
        title.Controls.Add(new Label { Text = "How to Fish", Font = new Font("Segoe UI Semibold", 15f), AutoSize = true, Margin = new Padding(0, 7, 4, 0) });
        title.Controls.Add(new Label { Text = "Save Editor", Font = new Font("Segoe UI Light", 15f), ForeColor = Theme.Muted, AutoSize = true, Margin = new Padding(0, 7, 10, 0) });
        title.Controls.Add(new Label { Text = "v" + VersionText, Font = new Font("Segoe UI Semibold", 8f), ForeColor = Theme.Bg, BackColor = Theme.Accent, AutoSize = true, Padding = new Padding(6, 2, 6, 2), Margin = new Padding(0, 16, 0, 0) });

        var right = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = Padding.Empty };
        right.Controls.Add(_langCap);
        _langBox.Margin = new Padding(0, 8, 0, 0);
        right.Controls.Add(_langBox);
        right.Controls.Add(_profileCap);
        _profileBox.Margin = new Padding(0, 8, 0, 0);
        right.Controls.Add(_profileBox);
        right.Controls.Add(_worldCap);
        _worldBox.Margin = new Padding(0, 8, 12, 0);
        right.Controls.Add(_worldBox);
        _reloadBtn.Margin = new Padding(0, 4, 8, 0);
        _saveBtn.Margin = new Padding(0, 4, 0, 0);
        right.Controls.Add(_reloadBtn);
        right.Controls.Add(_saveBtn);

        header.Controls.Add(title, 0, 0);
        header.Controls.Add(new Panel { Width = 1, Margin = Padding.Empty }, 1, 0);
        header.Controls.Add(right, 2, 0);
        return header;
    }

    private Control BuildStatusBar()
    {
        var bar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 30, BackColor = Theme.Side, WrapContents = false };
        bar.Controls.Add(_dirty);
        bar.Controls.Add(_game);
        bar.Controls.Add(_status);
        return bar;
    }

    // ---------------------------------------------------------------- language

    private void ApplyTexts()
    {
        _langCap.Text = T("Language", "Sprache");
        _profileCap.Text = T("Profile", "Profil");
        _worldCap.Text = T("World", "Welt");
        _saveBtn.Text = T("💾  Save", "💾  Speichern");
        _reloadBtn.Text = T("⟳  Reload", "⟳  Neu laden");
        foreach (var e in _entries) e.Button.Text = $"{e.Icon}   {e.Title()}";
        if (_profileBox.Tag is List<string> profiles)
            for (int i = 0; i < profiles.Count; i++)
                _profileBox.Items[i] = ProfileLabel(profiles[i]);
        UpdateDirty();
        UpdateGameState();
    }

    private void OnLanguageChanged()
    {
        _loading = true;
        _langBox.SelectedIndex = De ? 1 : 0;
        ApplyTexts();
        _loading = false;
        _status.Text = "";
        if (_active != null) ShowPage(_active);
    }

    private static string ProfileLabel(string folder) =>
        folder == SaveContext.SavesRoot ? T("(Saves – legacy)", "(Saves – alt)") : Path.GetFileName(folder);

    // ---------------------------------------------------------------- loading

    private void Init()
    {
        var profiles = SaveContext.FindProfiles();
        if (profiles.Count == 0)
        {
            SetStatus(T($"No saves found in {SaveContext.SavesRoot}", $"Keine Spielstände gefunden unter {SaveContext.SavesRoot}"));
            ShowPage(_entries[^1]);
            UpdateGameState();
            return;
        }
        _loading = true;
        foreach (var p in profiles) _profileBox.Items.Add(ProfileLabel(p));
        _profileBox.Tag = profiles;
        _profileBox.SelectedIndex = 0;
        _loading = false;
        OpenProfile();
        UpdateGameState();
        if (ShotsDir != null) BeginInvoke(RenderShots);
    }

    public static string ShotsDir;

    private void RenderShots()
    {
        Directory.CreateDirectory(ShotsDir);
        for (int i = 0; i < _entries.Count; i++)
        {
            ShowPage(_entries[i]);
            Application.DoEvents();
            Shot($"{i:00}");
            // second shot with another list entry selected (inventory weapon slot etc.)
            if (FindList(_content) is { Items.Count: > 2 } list)
            {
                list.SelectedIndex = 2;
                Application.DoEvents();
                Shot($"{i:00}b");
            }
        }
        // live language switch while a page is open
        ShowPage(_entries[1]);
        L.Set(De ? Lang.English : Lang.German, persist: false);
        Application.DoEvents();
        Shot("switched");
        Close();

        void Shot(string name)
        {
            // DrawToBitmap renders the whole window incl. a classic frame – crop to the client area.
            using var full = new Bitmap(Width, Height);
            DrawToBitmap(full, new Rectangle(Point.Empty, Size));
            var origin = PointToScreen(Point.Empty);
            var client = new Rectangle(origin.X - Left, origin.Y - Top, ClientSize.Width, ClientSize.Height);
            using var bmp = full.Clone(client, full.PixelFormat);
            bmp.Save(Path.Combine(ShotsDir, name + ".png"));
        }

        static ListBox FindList(Control c) =>
            c is ListBox l && c is not CheckedListBox ? l : c.Controls.Cast<Control>().Select(FindList).FirstOrDefault(x => x != null);
    }

    private bool ConfirmDiscard()
    {
        if (!_ctx.IsDirty) return true;
        var r = MessageBox.Show(T("There are unsaved changes.\n\nSave now?", "Es gibt ungespeicherte Änderungen.\n\nJetzt speichern?"),
            T("Unsaved changes", "Ungespeicherte Änderungen"), MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
        if (r == DialogResult.Cancel) return false;
        if (r == DialogResult.Yes) return Save();
        return true;
    }

    private string _lastProfile, _lastWorld;

    private void OpenProfile()
    {
        var profiles = (List<string>)_profileBox.Tag;
        var folder = profiles[_profileBox.SelectedIndex];
        if (folder == _lastProfile) return;
        if (!ConfirmDiscard())
        {
            _loading = true;
            _profileBox.SelectedIndex = profiles.IndexOf(_lastProfile);
            _loading = false;
            return;
        }
        try
        {
            _ctx.OpenProfile(folder);
            _lastProfile = folder;
            _lastWorld = null;
        }
        catch (Exception ex)
        {
            ShowError(T("Could not load profile", "Profil konnte nicht geladen werden"), ex);
        }
        FillWorlds();
    }

    private void FillWorlds(string select = null)
    {
        _loading = true;
        _worldBox.Items.Clear();
        var worlds = _ctx.FindWorlds();
        _worldBox.Tag = worlds;
        foreach (var w in worlds) _worldBox.Items.Add(Path.GetFileNameWithoutExtension(w));
        int idx = select == null ? 0 : Math.Max(0, worlds.FindIndex(w => string.Equals(w, select, StringComparison.OrdinalIgnoreCase)));
        if (worlds.Count > 0) _worldBox.SelectedIndex = idx;
        _loading = false;
        _lastWorld = null;
        OpenWorld();
    }

    private void OpenWorld()
    {
        var worlds = (List<string>)_worldBox.Tag ?? new List<string>();
        var path = _worldBox.SelectedIndex >= 0 && _worldBox.SelectedIndex < worlds.Count ? worlds[_worldBox.SelectedIndex] : null;
        if (path != null && path == _lastWorld) return;
        if (_ctx.WorldDirty && !ConfirmDiscard())
        {
            _loading = true;
            _worldBox.SelectedIndex = worlds.IndexOf(_lastWorld);
            _loading = false;
            return;
        }
        try
        {
            _ctx.OpenWorld(path);
            _lastWorld = path;
            SetStatus(path == null
                ? T("Profile loaded – no world found.", "Profil geladen – keine Welt vorhanden.")
                : T($"Loaded: {Path.GetFileName(path)}", $"Geladen: {Path.GetFileName(path)}"));
        }
        catch (Exception ex)
        {
            ShowError(T("Could not load world", "Welt konnte nicht geladen werden"), ex);
            _ctx.OpenWorld(null);
        }
        ShowPage(_active ?? _entries[0]);
    }

    private void Reload()
    {
        if (!ConfirmDiscard()) return;
        var world = _ctx.WorldPath;
        try
        {
            _ctx.OpenProfile(_lastProfile);
            _lastWorld = null;
            FillWorlds(world);
            SetStatus(T("Reloaded.", "Neu geladen."));
        }
        catch (Exception ex)
        {
            ShowError(T("Reload failed", "Neu laden fehlgeschlagen"), ex);
        }
    }

    private bool Save()
    {
        if (!_ctx.IsDirty)
        {
            SetStatus(T("Nothing to save.", "Nichts zu speichern."));
            return true;
        }
        if (SaveContext.GameRunning &&
            MessageBox.Show(T("How to Fish is running!\n\nThe game overwrites saves when autosaving / quitting. Please close the game first.\n\nSave anyway?",
                              "How to Fish läuft gerade!\n\nDas Spiel überschreibt Spielstände beim Autosave bzw. Beenden. Bitte das Spiel zuerst schließen.\n\nTrotzdem speichern?"),
                T("Game is running", "Spiel läuft"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return false;
        try
        {
            ValidateChildren();
            CommitGrids(_content);
            _ctx.Save();
            _lastWorld = _ctx.WorldPath;
            FillWorldsKeep();
            SetStatus(T($"Saved {DateTime.Now:HH:mm:ss}  ·  Backup: {_ctx.LastBackupFolder}", $"Gespeichert {DateTime.Now:HH:mm:ss}  ·  Backup: {_ctx.LastBackupFolder}"));
            return true;
        }
        catch (Exception ex)
        {
            ShowError(T("Save failed", "Speichern fehlgeschlagen"), ex);
            return false;
        }
    }

    private static void CommitGrids(Control c)
    {
        if (c is DataGridView g && g.IsCurrentCellInEditMode) g.EndEdit();
        foreach (Control child in c.Controls) CommitGrids(child);
    }

    /// <summary>Refreshes the world list after a save (a rename changes the file name) without reloading data.</summary>
    private void FillWorldsKeep()
    {
        _loading = true;
        var worlds = _ctx.FindWorlds();
        _worldBox.Items.Clear();
        _worldBox.Tag = worlds;
        foreach (var w in worlds) _worldBox.Items.Add(Path.GetFileNameWithoutExtension(w));
        int idx = worlds.FindIndex(w => string.Equals(w, _ctx.WorldPath, StringComparison.OrdinalIgnoreCase));
        if (idx >= 0) _worldBox.SelectedIndex = idx;
        _loading = false;
    }

    // ---------------------------------------------------------------- pages

    private void ShowPage(NavEntry e)
    {
        _active = e;
        Page page;
        if (e.NeedsWorld && _ctx.World == null)
            page = Page.Placeholder(_ctx, e.Title(), T("No world loaded. Pick a world at the top.", "Keine Welt geladen. Wähle oben eine Welt aus."));
        else if (e.NeedsLocal && _ctx.Local == null)
            page = Page.Placeholder(_ctx, e.Title(), T("local.txt was not found in this profile.", "local.txt wurde in diesem Profil nicht gefunden."));
        else
        {
            try { page = e.Create(_ctx); }
            catch (Exception ex)
            {
                page = Page.Placeholder(_ctx, e.Title(), T("Error while building the page:\n", "Fehler beim Aufbau der Seite:\n") + ex.Message);
            }
        }

        _content.SuspendLayout();
        foreach (Control c in _content.Controls.Cast<Control>().ToList()) c.Dispose();
        _content.Controls.Clear();
        Theme.Apply(page);
        _content.Controls.Add(page);
        _content.ResumeLayout();

        foreach (var n in _entries)
        {
            bool on = n == _active;
            n.Button.ForeColor = on ? Theme.Text : Theme.Muted;
            n.Button.BackColor = on ? Theme.Card : Theme.Side;
            n.Button.Invalidate();
        }
    }

    public void RefreshPage() => BeginInvoke(() => ShowPage(_active));

    // ---------------------------------------------------------------- status

    private void UpdateDirty()
    {
        var parts = new List<string>();
        if (_ctx.WorldDirty) parts.Add(T("World", "Welt"));
        if (_ctx.LocalDirty) parts.Add(T("Profile", "Profil"));
        _dirty.Text = parts.Count == 0
            ? T("● All saved", "● Alles gespeichert")
            : T($"● Unsaved: {string.Join(", ", parts)}", $"● Ungespeichert: {string.Join(", ", parts)}");
        _dirty.ForeColor = parts.Count == 0 ? Theme.Ok : Theme.Warn;
        Text = (parts.Count > 0 ? "* " : "") + "How to Fish – Save Editor";
    }

    private void UpdateGameState()
    {
        bool running = SaveContext.GameRunning;
        _game.Text = running ? T("▲ Game is running – close it before saving!", "▲ Spiel läuft – vor dem Speichern schließen!") : "";
        _game.ForeColor = Theme.Danger;
    }

    private void SetStatus(string text) => _status.Text = text;

    private static void ShowError(string title, Exception ex) =>
        MessageBox.Show(ex.Message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);

    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == (Keys.Control | Keys.S)) { Save(); return true; }
        if (keyData == Keys.F5) { Reload(); return true; }
        return base.ProcessCmdKey(ref msg, keyData);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!ConfirmDiscard()) e.Cancel = true;
        base.OnFormClosing(e);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        L.Changed -= OnLanguageChanged;
        base.OnFormClosed(e);
    }
}
