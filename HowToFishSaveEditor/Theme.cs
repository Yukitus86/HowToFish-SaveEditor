using System.ComponentModel;

namespace HowToFishSaveEditor;

public static class Theme
{
    public static readonly Color Bg = Color.FromArgb(22, 24, 29);
    public static readonly Color Side = Color.FromArgb(28, 31, 37);
    public static readonly Color Card = Color.FromArgb(33, 37, 44);
    public static readonly Color Input = Color.FromArgb(43, 48, 57);
    public static readonly Color Hover = Color.FromArgb(50, 56, 67);
    public static readonly Color Border = Color.FromArgb(60, 66, 78);
    public static readonly Color Text = Color.FromArgb(226, 230, 236);
    public static readonly Color Muted = Color.FromArgb(146, 155, 170);
    public static readonly Color Accent = Color.FromArgb(52, 168, 222);
    public static readonly Color AccentDark = Color.FromArgb(28, 104, 146);
    public static readonly Color Danger = Color.FromArgb(222, 84, 76);
    public static readonly Color Warn = Color.FromArgb(232, 172, 64);
    public static readonly Color Ok = Color.FromArgb(78, 190, 112);

    public static readonly Font Base = new("Segoe UI", 9.75f);
    public static readonly Font Bold = new("Segoe UI Semibold", 10.5f);
    public static readonly Font Title = new("Segoe UI Semibold", 16f);
    public static readonly Font Small = new("Segoe UI", 8.75f);
    public static readonly Font Mono = new("Consolas", 10f);
    public static readonly ToolTip Tip = new() { AutoPopDelay = 20000, InitialDelay = 350, ReshowDelay = 100 };

    public enum ButtonKind { Normal, Accent, Danger, Nav }

    public static Button Btn(string text, ButtonKind kind = ButtonKind.Normal, EventHandler click = null, string tip = null)
    {
        var b = new Button
        {
            Text = text,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Padding = new Padding(10, 4, 10, 4),
            Margin = new Padding(0, 0, 8, 8),
            Font = Base,
            UseVisualStyleBackColor = false,
        };
        var (bg, border, hover) = kind switch
        {
            ButtonKind.Accent => (AccentDark, Accent, Accent),
            ButtonKind.Danger => (Color.FromArgb(70, 36, 38), Danger, Color.FromArgb(110, 44, 44)),
            _ => (Input, Border, Hover),
        };
        b.BackColor = bg;
        b.ForeColor = Text;
        b.FlatAppearance.BorderColor = border;
        b.FlatAppearance.MouseOverBackColor = hover;
        b.FlatAppearance.MouseDownBackColor = border;
        if (click != null) b.Click += click;
        if (tip != null) Tip.SetToolTip(b, tip);
        return b;
    }

    /// <summary>Recursively colors input controls; containers inherit Back/ForeColor from the form.</summary>
    public static void Apply(Control root)
    {
        foreach (Control c in root.Controls) Apply(c);
        switch (root)
        {
            case Button b:
                b.UseMnemonic = false;
                break;
            case Label l:
                l.UseMnemonic = false;
                break;
            case NumericUpDown n:
                n.BackColor = Input; n.ForeColor = Text; n.BorderStyle = BorderStyle.FixedSingle;
                NoWheelWhenUnfocused(n);
                break;
            case TextBox t:
                t.BackColor = Input; t.ForeColor = Text; t.BorderStyle = BorderStyle.FixedSingle;
                break;
            case ComboBox cb:
                cb.BackColor = Input; cb.ForeColor = Text; cb.FlatStyle = FlatStyle.Flat;
                NoWheelWhenUnfocused(cb);
                break;
            case CheckedListBox clb:
                clb.BackColor = Input; clb.ForeColor = Text; clb.BorderStyle = BorderStyle.None;
                break;
            case ListBox lb:
                lb.BackColor = Input; lb.ForeColor = Text; lb.BorderStyle = BorderStyle.None;
                break;
            case CheckBox ch:
                ch.ForeColor = Text;
                break;
            case DataGridView g:
                StyleGrid(g);
                break;
        }
    }

    private static void NoWheelWhenUnfocused(Control c)
    {
        c.MouseWheel += (_, e) =>
        {
            if (!c.Focused && e is HandledMouseEventArgs h) h.Handled = true;
        };
    }

    public static void StyleGrid(DataGridView g)
    {
        g.BackgroundColor = Card;
        g.BorderStyle = BorderStyle.None;
        g.GridColor = Border;
        g.EnableHeadersVisualStyles = false;
        g.RowHeadersVisible = false;
        g.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        g.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        g.ColumnHeadersHeight = 32;
        g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
        g.ColumnHeadersDefaultCellStyle.BackColor = Side;
        g.ColumnHeadersDefaultCellStyle.ForeColor = Muted;
        g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Side;
        g.ColumnHeadersDefaultCellStyle.Font = Bold;
        g.DefaultCellStyle.BackColor = Card;
        g.DefaultCellStyle.ForeColor = Text;
        g.DefaultCellStyle.SelectionBackColor = AccentDark;
        g.DefaultCellStyle.SelectionForeColor = Text;
        g.DefaultCellStyle.Font = Base;
        g.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(37, 41, 49);
        g.RowTemplate.Height = 28;
        g.AllowUserToResizeRows = false;
        g.AllowUserToAddRows = false;
        g.AllowUserToDeleteRows = false;
        g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        g.MultiSelect = false;
    }

    private static Stream Resource(string name) => typeof(Theme).Assembly.GetManifestResourceStream(name);

    public static Icon AppIcon() => new(Resource("app.ico"));

    public static Image Logo() => Image.FromStream(Resource("logo.png"));
}

public sealed record Opt(long Value, string Text)
{
    public override string ToString() => Text;
}
