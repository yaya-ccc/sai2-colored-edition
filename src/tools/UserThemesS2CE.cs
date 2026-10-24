using System.IO;

namespace S2CE.Tools;

internal static class UserThemesS2CE
{
    public static string DirectoryPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "S2CE", "themes");

    public static string SaveChanges(string file, ThemePaletteS2CE palette, string? directory = null)
    {
        string target = Path.GetFullPath(file);
        string folder = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory ?? DirectoryPath));
        if (!string.Equals(Path.GetDirectoryName(target), folder, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(Path.GetExtension(target), ".json", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("只能修改用户主题目录中的 JSON 主题。");
        if (!File.Exists(target)) throw new FileNotFoundException("主题文件已不存在，请另存为新主题。", target);
        string temporary = Path.Combine(folder, "." + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            File.WriteAllText(temporary, palette.ToJson());
            File.Replace(temporary, target, null);
            return target;
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    public static string SaveNew(string name, ThemePaletteS2CE palette, string? directory = null)
    {
        name = name.Trim();
        string deviceName = name.Split('.')[0].ToUpperInvariant();
        bool reserved = deviceName is "CON" or "PRN" or "AUX" or "NUL" ||
            deviceName.Length == 4 && (deviceName.StartsWith("COM") || deviceName.StartsWith("LPT")) &&
            char.IsAsciiDigit(deviceName[3]) && deviceName[3] != '0';
        if (name.Length is 0 or > 80 || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            name.EndsWith('.') || reserved || name == "classic_sai2")
            throw new InvalidDataException("请使用 1–80 个字符的主题名称，不要包含路径或文件名特殊字符。");
        string folder = Path.GetFullPath(directory ?? DirectoryPath);
        Directory.CreateDirectory(folder);
        string target = Path.Combine(folder, name + ".json");
        if (File.Exists(target)) throw new InvalidDataException("已有同名主题，请换个名称；原主题会保留。");
        string temporary = Path.Combine(folder, "." + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            File.WriteAllText(temporary, palette.ToJson());
            File.Move(temporary, target, false);
            return target;
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}
