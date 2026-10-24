using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace S2CE.Tools
{
    class FileS2CE
    {
        private string? loadedHash;
        private byte[]? loadedConfiguration;
        private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));

        public byte[] ReadOriginal()
        {
            if (PathS2CE.SourceExecutable is not null)
            {
                byte[] source = File.ReadAllBytes(PathS2CE.SourceExecutable);
                loadedHash = Hash(source);
                loadedConfiguration = File.ReadAllBytes(PathS2CE.sourceConfiguration);
                CheckGeneratedCopy(loadedHash);
                return source;
            }
            byte[] current = File.ReadAllBytes(PathS2CE.sai2);
            loadedHash = Hash(current);
            if (!IsOldFileExists())
            {
                if (File.Exists(PathS2CE.state))
                    throw new InvalidDataException("The theme backup is missing. Restore your original sai2.exe and remove sai2.theme-state.json.");
                return current;
            }
            byte[] original = File.ReadAllBytes(PathS2CE.oldSai2);
            string originalHash = Hash(original);
            if (loadedHash == originalHash) return original;
            if (File.Exists(PathS2CE.state))
            {
                var state = JsonSerializer.Deserialize<ThemeState>(File.ReadAllText(PathS2CE.state));
                if (state?.OriginalSha256 == originalHash && state.ThemedSha256 == loadedHash)
                    return original;
            }
            throw new InvalidDataException("sai2.old.exe does not match the current sai2.exe. " +
                "Move the old backup and sai2.theme-state.json out of ref before importing another SAI2 build. No files were changed.");
        }

        private void CheckCurrent()
        {
            if (loadedHash is null || Hash(File.ReadAllBytes(PathS2CE.originalSai2)) != loadedHash)
                throw new IOException("sai2.exe changed while the theme was being prepared. Please try again.");
            if (PathS2CE.SourceExecutable is not null && (loadedConfiguration is null ||
                !loadedConfiguration.AsSpan().SequenceEqual(File.ReadAllBytes(PathS2CE.sourceConfiguration))))
                throw new IOException("The original SAI2 INI changed while preparing the theme. Please try again.");
        }

        private static void CheckGeneratedCopy(string sourceHash)
        {
            bool outputExists = File.Exists(PathS2CE.sai2);
            bool stateExists = File.Exists(PathS2CE.state);
            bool configurationExists = File.Exists(PathS2CE.generatedConfiguration);
            if (!outputExists && !stateExists && !configurationExists) return;
            if (outputExists && stateExists && configurationExists)
            {
                var state = JsonSerializer.Deserialize<ThemeState>(File.ReadAllText(PathS2CE.state));
                if (state?.OriginalSha256 == sourceHash && state.ThemedSha256 == Hash(File.ReadAllBytes(PathS2CE.sai2)) &&
                    state.ConfigurationSha256 == Hash(File.ReadAllBytes(PathS2CE.generatedConfiguration)))
                    return;
            }
            throw new InvalidDataException("The existing sai2-colored.exe, sai2-colored.ini or state does not match this source. " +
                "It will not be overwritten or deleted. Move it aside before generating a new copy.");
        }

        private static void ReplaceFile(string path, byte[] bytes)
        {
            string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllBytes(temporary, bytes);
                File.Move(temporary, path, true);
            }
            finally
            {
                if (File.Exists(temporary)) File.Delete(temporary);
            }
        }

        public void SaveTheme(byte[] original, byte[] themed)
        {
            CheckCurrent();
            string originalHash = Hash(original);
            string themedHash = Hash(themed);
            if (PathS2CE.SourceExecutable is not null)
            {
                CheckGeneratedCopy(originalHash);
                ReplaceFile(PathS2CE.generatedConfiguration, loadedConfiguration!);
                ReplaceFile(PathS2CE.sai2, themed);
                ReplaceFile(PathS2CE.state, JsonSerializer.SerializeToUtf8Bytes(new ThemeState {
                    OriginalSha256 = originalHash, ThemedSha256 = themedHash,
                    ConfigurationSha256 = Hash(loadedConfiguration!) }));
                return;
            }
            if (!IsOldFileExists())
            {
                using var backup = new FileStream(PathS2CE.oldSai2, FileMode.CreateNew, FileAccess.Write);
                backup.Write(original);
            }
            else if (Hash(File.ReadAllBytes(PathS2CE.oldSai2)) != originalHash)
                throw new IOException("The theme backup changed while preparing the theme.");
            ReplaceFile(PathS2CE.sai2, themed);
            var state = new ThemeState { OriginalSha256 = originalHash, ThemedSha256 = themedHash };
            ReplaceFile(PathS2CE.state, JsonSerializer.SerializeToUtf8Bytes(state));
        }

        public void UpdateOriginalFile()
        {
            byte[] original = ReadOriginal();
            CheckCurrent();
            if (PathS2CE.SourceExecutable is not null)
            {
                CheckGeneratedCopy(Hash(original));
                if (File.Exists(PathS2CE.sai2)) File.Delete(PathS2CE.sai2);
                if (File.Exists(PathS2CE.generatedConfiguration)) File.Delete(PathS2CE.generatedConfiguration);
                if (File.Exists(PathS2CE.state)) File.Delete(PathS2CE.state);
                return;
            }
            if (IsOldFileExists()) ReplaceFile(PathS2CE.sai2, original);
            if (File.Exists(PathS2CE.state)) File.Delete(PathS2CE.state);
            if (IsOldFileExists()) File.Delete(PathS2CE.oldSai2);
        }

        public bool IsOriginalFileExists(bool with_msg = false) => File.Exists(PathS2CE.sai2);
        public bool IsOldFileExists(bool with_msg = false) => File.Exists(PathS2CE.oldSai2);

        private sealed class ThemeState
        {
            public string OriginalSha256 { get; set; } = "";
            public string ThemedSha256 { get; set; } = "";
            public string? ConfigurationSha256 { get; set; }
        }
    }
}
