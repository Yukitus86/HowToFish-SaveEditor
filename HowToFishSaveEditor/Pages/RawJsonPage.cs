using System.Text.Json;
using System.Text.Json.Nodes;
using static HowToFishSaveEditor.L;

namespace HowToFishSaveEditor.Pages;

public sealed class RawJsonPage : Page
{
    private readonly ComboBox _which = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 260 };
    private readonly TextBox _text = new() { Multiline = true, ScrollBars = ScrollBars.Both, WordWrap = false, Dock = DockStyle.Fill, Font = Theme.Mono, AcceptsTab = true, MaxLength = 0 };
    private readonly Label _state = new() { AutoSize = true, Margin = new Padding(8, 8, 0, 0) };
    private int _worldIndex = -1;

    public RawJsonPage(SaveContext ctx) : base(ctx, "Raw JSON", T("Expert mode: edit the whole save directly. \"Apply\" checks the JSON and takes it over into the editor.", "Experten-Modus: den kompletten Spielstand direkt bearbeiten. \"Übernehmen\" prüft das JSON und übernimmt es in den Editor."))
    {
        if (ctx.World != null)
        {
            _worldIndex = _which.Items.Count;
            _which.Items.Add(T("World", "Welt") + "  (" + Path.GetFileName(ctx.WorldPath) + ")");
        }
        if (ctx.Local != null) _which.Items.Add(T("Profile", "Profil") + "  (local.txt)");
        _which.SelectedIndexChanged += (_, _) => Load();

        var bar = new FlowLayoutPanel { Dock = DockStyle.Top, AutoSize = true, Padding = new Padding(0, 0, 0, 6) };
        bar.Controls.Add(_which);
        _which.Margin = new Padding(0, 2, 10, 8);
        bar.Controls.Add(Theme.Btn(T("Apply", "Übernehmen"), Theme.ButtonKind.Accent, (_, _) => Apply()));
        bar.Controls.Add(Theme.Btn(T("Discard", "Verwerfen"), click: (_, _) => Load()));
        bar.Controls.Add(_state);
        _text.TextChanged += (_, _) => Validate();

        Body.Controls.Add(_text);
        Body.Controls.Add(bar);
        if (_which.Items.Count > 0) _which.SelectedIndex = 0;
    }

    private bool IsWorld => _which.SelectedIndex == _worldIndex;

    private void Load()
    {
        var o = IsWorld ? Ctx.World : Ctx.Local;
        _text.Text = o?.ToJsonString(J.Pretty).Replace("\n", "\r\n") ?? "";
        Validate();
    }

    private JsonObject Validate()
    {
        try
        {
            var node = JsonNode.Parse(_text.Text) as JsonObject ?? throw new JsonException(T("Root must be an object.", "Wurzel muss ein Objekt sein."));
            _state.Text = T("✓ valid JSON", "✓ gültiges JSON");
            _state.ForeColor = Theme.Ok;
            return node;
        }
        catch (Exception ex)
        {
            _state.Text = "✗ " + ex.Message.Split('\n')[0];
            _state.ForeColor = Theme.Danger;
            return null;
        }
    }

    private void Apply()
    {
        var o = Validate();
        if (o == null) return;
        if (IsWorld)
        {
            if (string.IsNullOrWhiteSpace(J.Str(o, "Name")))
            {
                MessageBox.Show(T("The world needs a \"Name\" field.", "Die Welt braucht ein Feld \"Name\"."), "Raw JSON", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Ctx.ReplaceWorld(o);
        }
        else Ctx.ReplaceLocal(o);
        _state.Text = T("✓ applied – click \"Save\" to write it to disk", "✓ übernommen – zum Schreiben auf Platte \"Speichern\" klicken");
    }
}
