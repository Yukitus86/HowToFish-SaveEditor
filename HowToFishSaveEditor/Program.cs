namespace HowToFishSaveEditor;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        L.Load();
        // Dev switches: --saves <dir> uses another Saves folder, --shots <dir> renders every page to PNG and exits.
        for (int i = 0; i + 1 < args.Length; i++)
        {
            if (args[i] == "--lang") L.Set(args[i + 1] == "de" ? Lang.German : Lang.English, persist: false);
            if (args[i] == "--saves") SaveContext.SavesRoot = args[i + 1];
            if (args[i] == "--backups") SaveContext.BackupRoot = args[i + 1];
            if (args[i] == "--shots") MainForm.ShotsDir = args[i + 1];
        }
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetColorMode(SystemColorMode.Dark);
        Application.Run(new MainForm());
    }
}
