using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using S2CE;
using S2CE.Tools;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: dotnet run --project tests/Compatibility -- <repository> <20240814-sai2.exe> [evidence-directory]");
    return 2;
}
string repository = Path.GetFullPath(args[0]);
byte[] target = File.ReadAllBytes(Path.GetFullPath(args[1]));
byte[] legacy = File.ReadAllBytes(Path.Combine(repository, "ref/sai2.exe"));
string initialDirectory = Directory.GetCurrentDirectory();
string temporaryBase = Path.GetFullPath(Path.GetTempPath());
string temporary = Path.Combine(temporaryBase, "s2ce-tests-" + Guid.NewGuid().ToString("N"));
string? evidence = args.Length > 2 ? Path.GetFullPath(args[2]) : null;
Directory.CreateDirectory(temporary);
int assertions = 0;
void Assert(bool condition, string message)
{
    assertions++;
    if (!condition) throw new Exception(message);
}
foreach (int code in new[] { 4551, 577, 1260 })
{
    var blocked = new System.ComponentModel.Win32Exception(code);
    Assert(LaunchFailureS2CE.Describe(blocked, true, false).Contains("主题副本已生成"), "Launch block lost generation success");
    Assert(LaunchFailureS2CE.Describe(blocked, true, false).Contains("Windows"), "Launch block omitted Windows policy");
    Assert(LaunchFailureS2CE.Describe(blocked, true, true).Contains("原版 SAI"), "Restore block reported wrong executable");
    Assert(!LaunchFailureS2CE.Describe(blocked, false, false).Contains("已生成"), "Generation failure reported success");
}
foreach (int code in new[] { 2, 5, 193, 225 })
    Assert(!LaunchFailureS2CE.Describe(new System.ComponentModel.Win32Exception(code), true, false).Contains("签名策略"),
        "Unrelated launch failure misclassified as signature policy");

void Reset(byte[] binary)
{
    File.WriteAllBytes("ref/sai2.exe", binary);
    File.Delete("ref/sai2.old.exe");
    File.Delete("ref/sai2.theme-state.json");
}
void RejectedWithoutChanges(Action action, string label, string directory = "ref")
{
    var before = Directory.GetFiles(directory).ToDictionary(path => Path.GetFileName(path)!, File.ReadAllBytes);
    bool rejected = false;
    try { action(); }
    catch (InvalidDataException) { rejected = true; }
    Assert(rejected, label + " was not rejected");
    Assert(before.Count == Directory.GetFiles(directory).Length, label + " changed the file count");
    foreach (var pair in before)
        Assert(pair.Value.SequenceEqual(File.ReadAllBytes(Path.Combine(directory, pair.Key!))), label + " modified " + pair.Key);
}
try
{
    Directory.CreateDirectory(Path.Combine(temporary, "ref/init/data"));
    Directory.CreateDirectory(Path.Combine(temporary, "themes"));
    foreach (string path in Directory.GetFiles(Path.Combine(repository, "ref/init/data"), "*.json"))
        File.Copy(path, Path.Combine(temporary, "ref/init/data", Path.GetFileName(path)));
    // Distinct non-grey channels catch accidental RGB/BGR conversion.
    var colors = new Dictionary<string, string> {
        ["Primary"] = "#123456", ["Secondary"] = "#234567", ["Ternary"] = "#345678",
        ["Text"] = "#456789", ["SelectablePrimary"] = "#56789A", ["SelectableSecondary"] = "#6789AB",
        ["AffectColorCircle"] = "0"
    };
    Directory.SetCurrentDirectory(temporary);
    File.WriteAllText("themes/probe.json", JsonSerializer.Serialize(colors));
    colors["AffectColorCircle"] = "1";
    File.WriteAllText("themes/circle.json", JsonSerializer.Serialize(colors));
    colors["Primary"] = "#234567";
    File.WriteAllText("themes/alternate.json", JsonSerializer.Serialize(colors));

    var app = new MainTheme();
    foreach (byte[] source in new[] { legacy, target })
    {
        byte[] beforeValidation = (byte[])source.Clone();
        string build = BinaryProfileS2CE.Validate(source, out string hash);
        Assert(build == BinaryProfileS2CE.Describe(source), "Validation returned a different build");
        Assert(hash == Convert.ToHexString(SHA256.HashData(source)), "Validation returned a different hash");
        Assert(source.SequenceEqual(beforeValidation), "Read-only validation changed the original binary");
    }

    // A late validation failure must leave even the in-memory patch untouched.
    var profiles = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText("ref/init/data/BinaryProfiles.json"))!;
    var textFix = profiles.AsArray().Single(profile => profile!["ControlTextFix"] is not null)!["ControlTextFix"]!;
    int selectionOffset = Convert.ToInt32(textFix["SelectionBackground"]!.GetValue<string>(), 16);
    byte[] invalidLayout = (byte[])target.Clone();
    invalidLayout[selectionOffset] ^= 1;
    byte[] beforePatch = (byte[])invalidLayout.Clone();
    bool layoutRejected = false;
    try { BinaryProfileS2CE.ApplyControlTextColor(target, invalidLayout, [0x12, 0x34, 0x56]); }
    catch (InvalidDataException) { layoutRejected = true; }
    Assert(layoutRejected, "An altered selection background was accepted");
    Assert(invalidLayout.SequenceEqual(beforePatch), "A rejected patch partially modified the output buffer");

    string validProfiles = File.ReadAllText("ref/init/data/BinaryProfiles.json");
    try
    {
        foreach (string field in new[] { "Cave", "Converter", "SelectionBackground", "Calls" })
        {
            foreach (int offset in new[] { target.Length - 1, target.Length })
            {
                var invalidProfiles = System.Text.Json.Nodes.JsonNode.Parse(validProfiles)!;
                var fix = invalidProfiles.AsArray().Single(profile => profile!["ControlTextFix"] is not null)!["ControlTextFix"]!;
                string address = "0x" + offset.ToString("X8");
                if (field == "Calls") fix[field]![0] = address;
                else fix[field] = address;
                File.WriteAllText("ref/init/data/BinaryProfiles.json", invalidProfiles.ToJsonString());
                Reset(target);
                var result = CompatibilityS2CE.CheckCurrent();
                Assert(!result.IsSupported, "An out-of-bounds " + field + " patch was accepted");
                Assert(result.Sha256 == Convert.ToHexString(SHA256.HashData(target)), "Rejected layout lost its binary hash");
                RejectedWithoutChanges(() => BinaryProfileS2CE.ApplyControlTextColor(target, (byte[])target.Clone(), [1, 2, 3]),
                    "Out-of-bounds " + field);
            }
        }
    }
    finally { File.WriteAllText("ref/init/data/BinaryProfiles.json", validProfiles); }

    foreach (var (name, source, expectedBorder) in new[] {
        ("2024.02.22", legacy, 0x534678), ("2024.08.14", target, 0x534C78)
    })
    {
        Reset(source);
        var addresses = BinaryProfileS2CE.GetAddresses(source);
        Assert(addresses.Count == 294, name + " missing addresses");
        Assert(addresses["GlobalBorders"] == expectedBorder, name + " wrong address table");
        app.Apply("probe", false);
        byte[] output = File.ReadAllBytes("ref/sai2.exe");
        Assert(output.Length == source.Length, name + " changed file size");
        Assert(output.AsSpan(expectedBorder, 3).SequenceEqual(new byte[] { 0x78, 0x56, 0x34 }), name + " GlobalBorders was not recolored");
        Assert(output.AsSpan(addresses["ActiveCanvasBackground"], 3).SequenceEqual(new byte[] { 0x56, 0x34, 0x12 }), name + " code color was not recolored");
        Assert(source.SequenceEqual(File.ReadAllBytes("ref/sai2.old.exe")), name + " backup does not match source");
        Assert(output.AsSpan(0, 0x400).SequenceEqual(source.AsSpan(0, 0x400)), name + " PE header changed");
        int pe = BitConverter.ToInt32(source, 0x3c);
        int count = BitConverter.ToUInt16(source, pe + 6);
        int sections = pe + 24 + BitConverter.ToUInt16(source, pe + 20);
        for (int index = 0; index < count; index++)
        {
            int section = sections + index * 40;
            string sectionName = System.Text.Encoding.ASCII.GetString(source, section, 8).TrimEnd('\0');
            if (sectionName is ".text" or ".data" or ".appskin" or ".srclibs") continue;
            int length = BitConverter.ToInt32(source, section + 16);
            int offset = BitConverter.ToInt32(source, section + 20);
            Assert(output.AsSpan(offset, length).SequenceEqual(source.AsSpan(offset, length)), name + " changed " + sectionName);
        }
        app.Apply("probe", false);
        Assert(output.SequenceEqual(File.ReadAllBytes("ref/sai2.exe")), name + " repeat application differs");
        app.Apply("circle", false);
        byte[] circle = File.ReadAllBytes("ref/sai2.exe");
        app.Apply("circle", false);
        Assert(circle.SequenceEqual(File.ReadAllBytes("ref/sai2.exe")), name + " color-circle output is nondeterministic");
        app.Apply("alternate", false);
        byte[] switched = File.ReadAllBytes("ref/sai2.exe");
        app.Apply("classic_sai2", false);
        Assert(source.SequenceEqual(File.ReadAllBytes("ref/sai2.exe")), name + " classic restore differs from source");
        Assert(!File.Exists("ref/sai2.old.exe") && !File.Exists("ref/sai2.theme-state.json"), name + " restore left stale state");
        app.Apply("alternate", false);
        Assert(switched.SequenceEqual(File.ReadAllBytes("ref/sai2.exe")), name + " theme switching compounded changes");
        if (evidence is not null)
        {
            Directory.CreateDirectory(evidence);
            File.WriteAllBytes(Path.Combine(evidence, name + "-patched.exe"), circle);
        }
        app.Apply("classic_sai2", false);
        Console.WriteLine("PASS " + name + ": recolor, protected sections, repeat/switch, color circle, exact restore");
    }

    foreach (var (name, source) in new[] { ("2024.02.22", legacy), ("2024.08.14", target) })
    {
        string installation = Path.Combine(temporary, "install-" + name);
        Directory.CreateDirectory(installation);
        string executable = Path.Combine(installation, "sai2.exe");
        File.WriteAllBytes(executable, source);
        File.WriteAllText(Path.Combine(installation, "sai2.ini"), "[Locale]\nlang=ja\n");
        PathS2CE.UseSource(executable);
        var check = CompatibilityS2CE.CheckCurrent();
        Assert(check.IsSupported && check.Build.Contains(name), name + " not auto-detected");
        Assert(check.Sha256 == Convert.ToHexString(SHA256.HashData(source)), name + " incorrect reported hash");
        Assert(Directory.GetFiles(installation).Length == 2, name + " detection wrote files");
        app.Apply("probe", false);
        byte[] first = File.ReadAllBytes(PathS2CE.sai2);
        Assert(!first.SequenceEqual(source), name + " generated copy was not themed");
        Assert(File.ReadAllBytes(executable).SequenceEqual(source), name + " modified the original executable");
        Assert(File.ReadAllText(Path.Combine(installation, "sai2.ini")) == "[Locale]\nlang=ja\n", name + " modified settings");
        Assert(File.ReadAllText(PathS2CE.generatedConfiguration) == "[Locale]\nlang=ja\n", name + " did not create a matching INI for the renamed executable");
        Assert(!File.Exists(Path.Combine(installation, "sai2.old.exe")), name + " made an unnecessary source backup");
        Assert(CompatibilityS2CE.CheckCurrent() is { IsSupported: true, IsThemed: true }, name + " themed copy not detected");
        Assert(CompatibilityS2CE.CheckFile(executable).IsSupported, name + " source check failed");
        Assert(!CompatibilityS2CE.CheckFile(PathS2CE.sai2).IsSupported, name + " arbitrary modified file was accepted as an original");
        app.Apply("circle", false);
        byte[] switched = File.ReadAllBytes(PathS2CE.sai2);
        app.Apply("circle", false);
        Assert(switched.SequenceEqual(File.ReadAllBytes(PathS2CE.sai2)), name + " generated output is nondeterministic");
        app.Apply("classic_sai2", false);
        Assert(!File.Exists(PathS2CE.sai2) && !File.Exists(PathS2CE.state) && !File.Exists(PathS2CE.generatedConfiguration), name + " classic left generated state");
        Assert(File.ReadAllBytes(executable).SequenceEqual(source), name + " classic altered the source");
        File.WriteAllText(PathS2CE.generatedConfiguration, "An unrelated existing INI");
        Assert(!CompatibilityS2CE.CheckCurrent().IsSupported, name + " ignored an unmanaged INI");
        RejectedWithoutChanges(() => app.Apply("probe", false), "Unmanaged INI", installation);
        RejectedWithoutChanges(() => app.Apply("classic_sai2", false), "Unmanaged INI cleanup", installation);
        File.Delete(PathS2CE.generatedConfiguration);
        File.WriteAllBytes(PathS2CE.sai2, source); // An unrelated same-name file must be preserved.
        Assert(!CompatibilityS2CE.CheckCurrent().IsSupported, name + " ignored an unmanaged output");
        RejectedWithoutChanges(() => app.Apply("probe", false), "Unmanaged output", installation);
        RejectedWithoutChanges(() => app.Apply("classic_sai2", false), "Unmanaged restore", installation);
        File.Delete(PathS2CE.sai2);
        app.Apply("probe", false);
        string generatedIni = File.ReadAllText(PathS2CE.generatedConfiguration);
        File.AppendAllText(PathS2CE.generatedConfiguration, "externally changed");
        Assert(!CompatibilityS2CE.CheckCurrent().IsSupported, name + " ignored an altered generated INI");
        RejectedWithoutChanges(() => app.Apply("classic_sai2", false), "Altered generated INI cleanup", installation);
        File.WriteAllText(PathS2CE.generatedConfiguration, generatedIni);
        byte[] altered = File.ReadAllBytes(PathS2CE.sai2);
        altered[^1] ^= 1;
        File.WriteAllBytes(PathS2CE.sai2, altered);
        Assert(!CompatibilityS2CE.CheckCurrent().IsSupported, name + " ignored an altered output");
        RejectedWithoutChanges(() => app.Apply("probe", false), "Altered generated output", installation);
        RejectedWithoutChanges(() => app.Apply("classic_sai2", false), "Altered generated restore", installation);
        PathS2CE.UseSource(null);
        Console.WriteLine("PASS " + name + ": automatic detection, separate executable, source preserved, safe cleanup/conflict handling");
    }

    Reset(target);
    byte[] unknown = (byte[])target.Clone();
    unknown[^1] ^= 1;
    File.WriteAllBytes("ref/sai2.exe", unknown);
    Assert(!CompatibilityS2CE.CheckCurrent().IsSupported, "Unknown build was reported as supported");
    Assert(!CompatibilityS2CE.CheckFile("missing-sai2.exe").IsSupported, "Missing executable was reported as supported");
    RejectedWithoutChanges(() => app.Apply("probe", false), "Unknown executable");
    RejectedWithoutChanges(() => app.Apply("classic_sai2", false), "Unknown classic executable");
    Reset(target);
    File.WriteAllBytes("ref/sai2.old.exe", legacy);
    RejectedWithoutChanges(() => app.Apply("probe", false), "Stale backup");
    RejectedWithoutChanges(() => app.Apply("classic_sai2", false), "Stale classic backup");
    Reset(target);
    app.Apply("probe", false);
    byte[] changedBackup = File.ReadAllBytes("ref/sai2.old.exe");
    changedBackup[^1] ^= 1;
    File.WriteAllBytes("ref/sai2.old.exe", changedBackup);
    RejectedWithoutChanges(() => app.Apply("circle", false), "Modified backup");
    Reset(target);
    app.Apply("probe", false);
    byte[] changedOutput = File.ReadAllBytes("ref/sai2.exe");
    changedOutput[^1] ^= 1;
    File.WriteAllBytes("ref/sai2.exe", changedOutput);
    RejectedWithoutChanges(() => app.Apply("circle", false), "Modified themed executable");
    Reset(target);
    // Preparation failures must not create a backup or mutate the input.
    try { app.Apply("missing", false); throw new Exception("Missing theme was accepted"); }
    catch (FileNotFoundException) { }
    Assert(!File.Exists("ref/sai2.old.exe"), "Missing theme created a backup");
    Assert(File.ReadAllBytes("ref/sai2.exe").SequenceEqual(target), "Missing theme modified executable");
    Assert(ThemePaletteS2CE.TryNormalize(" aabbcc ", out string normalized) && normalized == "#AABBCC", "Hex input was not normalized");
    Assert(!ThemePaletteS2CE.TryNormalize("#12345", out _), "Incomplete color was accepted");
    Assert(!ThemePaletteS2CE.TryNormalize("#12345678", out _), "Alpha color was accepted as RGB");
    var customColors = new Dictionary<string, string> {
        ["Primary"] = "#1A2433", ["Secondary"] = "#25354B", ["Ternary"] = "#344A66",
        ["Text"] = "#DDE8F5", ["SelectablePrimary"] = "#6EAAFF", ["SelectableSecondary"] = "#496D9B"
    };
    var customPalette = new ThemePaletteS2CE(customColors, false);
    string userThemes = Path.Combine(temporary, "user-themes");
    string customFile = UserThemesS2CE.SaveNew("夜蓝 主题", customPalette, userThemes);
    byte[] savedTheme = File.ReadAllBytes(customFile);
    var reopened = ThemePaletteS2CE.Load(customFile);
    Assert(reopened.Colors.Count == 6 && customColors.All(pair => reopened.Colors[pair.Key] == pair.Value), "Saved theme lost color roles");
    Assert(!reopened.AffectColorCircle, "Saved theme lost color-circle setting");
    bool duplicateRejected = false;
    try { UserThemesS2CE.SaveNew("夜蓝 主题", new ThemePaletteS2CE(customColors, true), userThemes); }
    catch (InvalidDataException) { duplicateRejected = true; }
    Assert(duplicateRejected && savedTheme.SequenceEqual(File.ReadAllBytes(customFile)), "Duplicate name overwrote an existing user theme");
    foreach (string invalidName in new[] { "..\\escape", "CON", "LPT1", "", "name." })
    {
        bool rejected = false;
        try { UserThemesS2CE.SaveNew(invalidName, customPalette, userThemes); }
        catch (InvalidDataException) { rejected = true; }
        Assert(rejected, "Invalid theme name was accepted: " + invalidName);
    }
    Assert(Directory.GetFiles(userThemes).Length == 1, "Failed saves created extra theme files");
    string circleFile = UserThemesS2CE.SaveNew("色环主题", new ThemePaletteS2CE(customColors, true), userThemes);
    Assert(ThemePaletteS2CE.Load(circleFile).AffectColorCircle, "Enabled color-circle setting did not survive saving");
    var editedColors = customColors.ToDictionary(pair => pair.Key, pair => pair.Value);
    editedColors["Primary"] = "#203047";
    var editedPalette = new ThemePaletteS2CE(editedColors, true);
    string editedFile = UserThemesS2CE.SaveChanges(customFile, editedPalette, userThemes);
    var edited = ThemePaletteS2CE.Load(editedFile);
    Assert(editedFile == customFile && edited.Colors["Primary"] == "#203047" && edited.AffectColorCircle,
        "Editing did not update the existing theme and circle setting");
    Assert(Directory.GetFiles(userThemes).Length == 2, "Editing duplicated a theme or left temporary files");
    Assert(ThemePaletteS2CE.Load(circleFile).Colors["Primary"] == customColors["Primary"], "Editing changed another theme");
    string copiedFile = UserThemesS2CE.SaveNew("夜蓝 副本", customPalette, userThemes);
    Assert(ThemePaletteS2CE.Load(customFile).Colors["Primary"] == "#203047" &&
        ThemePaletteS2CE.Load(copiedFile).Colors["Primary"] == "#1A2433", "Save-as changed the edited original");
    string externalTheme = Path.Combine(temporary, "external.json");
    File.WriteAllText(externalTheme, customPalette.ToJson());
    bool externalEditRejected = false;
    try { UserThemesS2CE.SaveChanges(externalTheme, editedPalette, userThemes); }
    catch (InvalidDataException) { externalEditRejected = true; }
    Assert(externalEditRejected && ThemePaletteS2CE.Load(externalTheme).Colors["Primary"] == "#1A2433",
        "Editing overwrote a theme outside the user directory");
    bool missingEditRejected = false;
    try { UserThemesS2CE.SaveChanges(Path.Combine(userThemes, "missing.json"), editedPalette, userThemes); }
    catch (FileNotFoundException) { missingEditRejected = true; }
    Assert(missingEditRejected && Directory.GetFiles(userThemes).Length == 3, "Editing recreated a missing theme");
    UserThemesS2CE.SaveChanges(customFile, customPalette, userThemes);
    Assert(savedTheme.SequenceEqual(File.ReadAllBytes(customFile)), "Repeated theme updates did not round-trip");
    var contrastColors = customColors.ToDictionary(pair => pair.Key, _ => "#000000");
    contrastColors["Text"] = "#FFFFFF";
    Assert(Math.Abs(new ThemePaletteS2CE(contrastColors, false).MinimumTextContrast() - 21) < 0.001, "White-on-black contrast differs");
    contrastColors["Text"] = "#000000";
    Assert(new ThemePaletteS2CE(contrastColors, false).MinimumTextContrast() == 1, "Matching text/background contrast differs");
    Reset(target);
    app.Apply("user:夜蓝 主题", false, customFile);
    byte[] customOutput = File.ReadAllBytes("ref/sai2.exe");
    var customAddresses = BinaryProfileS2CE.GetAddresses(target);
    Assert(customOutput.AsSpan(customAddresses["GlobalBorders"], 3).SequenceEqual(new byte[] { 0x66, 0x4A, 0x34 }), "External user theme panel color was not applied");
    Assert(customOutput.AsSpan(customAddresses["ActiveCanvasBackground"], 3).SequenceEqual(new byte[] { 0x33, 0x24, 0x1A }), "External user theme primary color was not applied");
    Assert(savedTheme.SequenceEqual(File.ReadAllBytes(customFile)), "Applying a custom theme modified its definition");
    app.Apply("classic_sai2", false);
    Assert(File.ReadAllBytes("ref/sai2.exe").SequenceEqual(target), "Custom theme did not restore exactly");
    string invalidThemeFile = Path.Combine(userThemes, "invalid.json");
    customColors["Text"] = "#oops";
    File.WriteAllText(invalidThemeFile, JsonSerializer.Serialize(customColors));
    RejectedWithoutChanges(() => app.Apply("user:invalid", false, invalidThemeFile), "Invalid custom palette");
    string preferencesFile = Path.Combine(temporary, "preferences", "preferences.json");
    var preferences = new LauncherPreferencesS2CE(preferencesFile);
    Assert(preferences.Read() == new LauncherPreferences(), "Fresh launcher should require a source selection");
    Assert(!File.Exists(preferencesFile), "Reading preferences wrote a file");
    var selected = new LauncherPreferences("C:\\绘图 工具\\sai2.exe", "example_retro_theme");
    preferences.Save(selected);
    Assert(preferences.Read() == selected, "Launcher did not remember source/theme selection");
    preferences.Save(selected with { ThemeId = "example_cappuccino_theme" });
    Assert(preferences.Read().ThemeId == "example_cappuccino_theme", "Theme preference was not updated");
    Assert(Directory.GetFiles(Path.GetDirectoryName(preferencesFile)!).Length == 1, "Preference write left temporary files");
    File.WriteAllText(preferencesFile, "{broken json");
    Assert(preferences.Read() == new LauncherPreferences(), "Corrupt preferences prevent startup");
    Console.WriteLine($"PASS all {assertions} assertions; unsupported builds and conflicting backups leave files unchanged");
    return 0;
}
finally
{
    PathS2CE.UseSource(null);
    Directory.SetCurrentDirectory(initialDirectory);
    string resolved = Path.GetFullPath(temporary);
    if (resolved.StartsWith(temporaryBase, StringComparison.OrdinalIgnoreCase)
        && Path.GetFileName(resolved).StartsWith("s2ce-tests-", StringComparison.Ordinal))
        Directory.Delete(resolved, true);
}
