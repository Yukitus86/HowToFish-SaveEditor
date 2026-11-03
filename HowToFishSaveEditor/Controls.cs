using System.Text.Json.Nodes;

namespace HowToFishSaveEditor;

/// <summary>Two-column "label | editor" grid whose editors write straight into a JsonObject.</summary>
public sealed class FieldGrid : TableLayoutPanel
{
    private readonly Action _changed;

    public FieldGrid(Action changed, int labelWidth = 185, int fieldWidth = 240)
    {
        _changed = changed;
        ColumnCount = 2;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, labelWidth));
        ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, fieldWidth));
        Margin = Padding.Empty;
        Padding = Padding.Empty;
    }

    public Label AddRow(string label, Control ctl, string tip = null)
    {
        var lbl = new Label
        {
            Text = label,
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = Theme.Muted,
            Margin = new Padding(0, 2, 8, 2),
        };
        ctl.Dock = DockStyle.Fill;
        ctl.Margin = new Padding(0, 3, 0, 3);
        int row = RowCount++;
        RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(lbl, 0, row);
        Controls.Add(ctl, 1, row);
        if (tip != null)
        {
            Theme.Tip.SetToolTip(lbl, tip);
            Theme.Tip.SetToolTip(ctl, tip);
        }
        return lbl;
    }

    public NumericUpDown Int(string label, JsonObject o, string key, decimal min, decimal max, string tip = null, Action<long> after = null)
    {
        decimal cur = J.Long(o, key);
        var n = new NumericUpDown { Minimum = Math.Min(min, cur), Maximum = Math.Max(max, cur), ThousandsSeparator = true };
        n.Value = cur;
        n.ValueChanged += (_, _) =>
        {
            J.Set(o, key, (long)n.Value);
            _changed();
            after?.Invoke((long)n.Value);
        };
        AddRow(label, n, tip);
        return n;
    }

    public NumericUpDown Float(string label, JsonObject o, string key, decimal min, decimal max, int decimals = 3, string tip = null, decimal step = 0.1m)
    {
        var n = FloatBox(o, key, min, max, decimals, step);
        AddRow(label, n, tip);
        return n;
    }

    public NumericUpDown FloatBox(JsonObject o, string key, decimal min, decimal max, int decimals = 3, decimal step = 0.1m)
    {
        decimal cur;
        try { cur = Math.Round((decimal)J.Num(o, key), decimals); }
        catch (OverflowException) { cur = 0; }
        var n = new NumericUpDown
        {
            DecimalPlaces = decimals,
            Increment = step,
            Minimum = Math.Min(min, cur),
            Maximum = Math.Max(max, cur),
        };
        n.Value = cur;
        n.ValueChanged += (_, _) =>
        {
            J.SetF(o, key, (float)n.Value);
            _changed();
        };
        return n;
    }

    public CheckBox Bool(string label, JsonObject o, string key, string tip = null, Action<bool> after = null)
    {
        var c = new CheckBox { Checked = J.Bool(o, key), Text = "", AutoSize = false, Height = 26 };
        c.CheckedChanged += (_, _) =>
        {
            J.Set(o, key, c.Checked);
            _changed();
            after?.Invoke(c.Checked);
        };
        AddRow(label, c, tip);
        return c;
    }

    public ComboBox Combo(string label, JsonObject o, string key, IEnumerable<Opt> options, string tip = null, Action<long> after = null)
    {
        var cb = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, MaxDropDownItems = 20 };
        var list = options.ToList();
        long cur = J.Long(o, key);
        if (!list.Any(x => x.Value == cur)) list.Add(new Opt(cur, L.T($"Unknown ({cur})", $"Unbekannt ({cur})")));
        cb.Items.AddRange(list.Cast<object>().ToArray());
        cb.SelectedItem = list.First(x => x.Value == cur);
        cb.SelectedIndexChanged += (_, _) =>
        {
            if (cb.SelectedItem is not Opt opt) return;
            J.Set(o, key, opt.Value);
            _changed();
            after?.Invoke(opt.Value);
        };
        AddRow(label, cb, tip);
        return cb;
    }

    public TextBox TextField(string label, JsonObject o, string key, string tip = null)
    {
        var t = new TextBox { Text = J.Str(o, key) };
        t.TextChanged += (_, _) =>
        {
            J.Set(o, key, t.Text);
            _changed();
        };
        AddRow(label, t, tip);
        return t;
    }

    public Label Info(string label, string value, string tip = null)
    {
        var l = new Label { Text = value, AutoSize = false, Height = 26, TextAlign = ContentAlignment.MiddleLeft };
        AddRow(label, l, tip);
        return l;
    }

    /// <summary>Unity Vector2/3/4 as a row of float boxes.</summary>
    public void Vector(string label, JsonObject o, string key, string[] axes, decimal min, decimal max, int decimals = 3, string tip = null)
    {
        var v = J.Obj(o, key);
        var flow = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = Padding.Empty, Padding = Padding.Empty, Height = 30 };
        int w = (240 - (axes.Length - 1) * 4) / axes.Length;
        foreach (var a in axes)
        {
            var n = FloatBox(v, a, min, max, decimals);
            n.Width = w;
            n.Margin = new Padding(0, 0, 4, 0);
            Theme.Tip.SetToolTip(n, a);
            flow.Controls.Add(n);
        }
        AddRow(label, flow, tip);
    }
}

public static class Ui
{
    /// <summary>Rounded-less "card" with a header and content.</summary>
    public static Control Card(string title, Control content, string subtitle = null, int width = 0)
    {
        var t = new TableLayoutPanel
        {
            ColumnCount = 1,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            BackColor = Theme.Card,
            Padding = new Padding(18, 14, 18, 16),
            Margin = new Padding(0, 0, 16, 16),
        };
        if (width > 0) t.MinimumSize = new Size(width, 0);
        if (!string.IsNullOrEmpty(title))
            t.Controls.Add(new Label { Text = title, Font = Theme.Bold, ForeColor = Theme.Accent, AutoSize = true, Margin = new Padding(0, 0, 0, subtitle == null ? 10 : 2) });
        if (subtitle != null)
            t.Controls.Add(new Label { Text = subtitle, ForeColor = Theme.Muted, Font = Theme.Small, AutoSize = true, MaximumSize = new Size(Math.Max(width, 430) - 36, 0), Margin = new Padding(0, 0, 0, 10) });
        content.Margin = Padding.Empty;
        t.Controls.Add(content);
        return t;
    }

    public static FlowLayoutPanel ButtonRow(params Control[] buttons)
    {
        var f = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = true, Margin = new Padding(0, 4, 0, 0), MaximumSize = new Size(440, 0) };
        f.Controls.AddRange(buttons);
        return f;
    }

    public static FlowLayoutPanel Stack(params Control[] items)
    {
        var f = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.TopDown, WrapContents = false, Margin = Padding.Empty };
        f.Controls.AddRange(items);
        return f;
    }

    public static FlowLayoutPanel FlowBody() => new()
    {
        Dock = DockStyle.Fill,
        AutoScroll = true,
        WrapContents = true,
        FlowDirection = FlowDirection.LeftToRight,
        Padding = Padding.Empty,
    };

    public static Label Hint(string text, Color? color = null) => new()
    {
        Text = text,
        AutoSize = true,
        ForeColor = color ?? Theme.Muted,
        Font = Theme.Small,
        MaximumSize = new Size(440, 0),
        Margin = new Padding(0, 4, 0, 8),
    };
}

/// <summary>Base for every page: title + subtitle + body (Fill).</summary>
public class Page : Panel
{
    protected readonly SaveContext Ctx;
    protected readonly Panel Body = new() { Dock = DockStyle.Fill };

    public Page(SaveContext ctx, string title, string subtitle)
    {
        Ctx = ctx;
        Dock = DockStyle.Fill;
        Padding = new Padding(26, 18, 18, 10);
        var head = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, Margin = Padding.Empty };
        head.Controls.Add(new Label { Text = title, Font = Theme.Title, AutoSize = true, Margin = new Padding(0, 0, 0, 2) });
        head.Controls.Add(new Label { Text = subtitle, ForeColor = Theme.Muted, AutoSize = true, Margin = new Padding(2, 0, 0, 16) });
        Controls.Add(Body);
        Controls.Add(head);
    }

    protected void WorldChanged() => Ctx.MarkWorld();
    protected void LocalChanged() => Ctx.MarkLocal();

    /// <summary>Re-creates the page (used after bulk actions).</summary>
    protected void Rebuild() => (FindForm() as MainForm)?.RefreshPage();

    public static Page Placeholder(SaveContext ctx, string title, string text)
    {
        var p = new Page(ctx, title, "");
        p.Body.Controls.Add(new Label { Text = text, AutoSize = true, ForeColor = Theme.Muted, Font = Theme.Bold });
        return p;
    }
}
