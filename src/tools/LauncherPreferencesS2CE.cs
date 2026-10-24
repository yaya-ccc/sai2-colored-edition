using System.IO;
using System.Text.Json;

namespace S2CE.Tools;

internal sealed record LauncherPreferences(string? SourcePath = null, string? ThemeId = null);

internal sealed class LauncherPreferencesS2CE(string? file = null)
{
    private readonly string path = file ?? Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "S2CE", "preferences.json");

    public LauncherPreferences Read()
    {
        try { return JsonSerializer.Deserialize<LauncherPreferences>(File.ReadAllText(path)) ?? new(); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        { return new(); }
    }

    public void Save(LauncherPreferences preferences)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(preferences));
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
