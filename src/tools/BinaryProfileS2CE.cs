using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Buffers.Binary;
using S2CE.Extensions;

namespace S2CE.Tools
{
    static class BinaryProfileS2CE
    {
        private static BinaryProfile[] ReadProfiles() =>
            JsonSerializer.Deserialize<BinaryProfile[]>(File.ReadAllText("./ref/init/data/BinaryProfiles.json"))
            ?? throw new InvalidDataException("The binary profile list is empty.");

        public static string Describe(byte[] binary) => GetProfile(binary).DisplayName;

        public static string[] SupportedBuilds() => ReadProfiles().Select(profile => profile.DisplayName).ToArray();

        private static BinaryProfile GetProfile(byte[] binary)
        {
            string hash = Convert.ToHexString(SHA256.HashData(binary));
            var profiles = ReadProfiles();
            var profile = profiles.SingleOrDefault(p => p.Length == binary.Length &&
                string.Equals(p.Sha256, hash, StringComparison.OrdinalIgnoreCase));
            if (profile is null)
                throw new InvalidDataException($"Unsupported SAI2 executable (SHA-256: {hash}). " +
                    "A verified address table is required for this exact build. No files were changed.");

            return profile;
        }

        public static Dictionary<string, int> GetAddresses(byte[] binary)
        {
            var profile = GetProfile(binary);
            var addresses = JsonSerializer.Deserialize<Dictionary<string, string>>(
                File.ReadAllText(Path.Combine("./ref/init/data", Path.GetFileName(profile.AddressFile))))
                ?.ConvertToDecimalAddressDictionary()
                ?? throw new InvalidDataException("The address table is empty.");
            if (addresses.Values.Any(address => address < 0 || address >= binary.Length))
                throw new InvalidDataException("The address table contains an out-of-bounds offset.");
            return addresses;
        }

        public static void ApplyControlTextColor(byte[] original, byte[] themed, byte[] color)
        {
            var fix = GetProfile(original).ControlTextFix;
            if (fix is null) return;
            int cave = fix.Cave.GetDecimalAddress();
            int converter = fix.Converter.GetDecimalAddress();
            // The original leaf converter maps internal RGB to GDI COLORREF.
            // Only the three verified UI text paths are redirected. Other
            // graphics/color conversion callers continue to use the original.
            byte[] conversion = [0x89, 0xC8, 0xC1, 0xC0, 0x08, 0x0F, 0xC8, 0xC3];
            byte[] helper = [0x85, 0xC9, 0x75, 0x05, 0xB9,
                color[0], color[1], color[2], 0,
                .. conversion];
            if (!original.AsSpan(converter, conversion.Length).SequenceEqual(conversion) ||
                original.AsSpan(cave, helper.Length).ToArray().Any(value => value != 0xCC) ||
                !themed.AsSpan(cave, helper.Length).SequenceEqual(original.AsSpan(cave, helper.Length)))
                throw new InvalidDataException("The verified control-text code layout does not match.");
            foreach (string address in fix.Calls)
            {
                int site = address.GetDecimalAddress();
                if (original[site] != 0xE8 ||
                    BinaryPrimitives.ReadInt32LittleEndian(original.AsSpan(site + 1, 4)) != converter - site - 5 ||
                    !original.AsSpan(site, 5).SequenceEqual(themed.AsSpan(site, 5)))
                    throw new InvalidDataException("A verified UI text conversion call does not match.");
                BinaryPrimitives.WriteInt32LittleEndian(themed.AsSpan(site + 1, 4), cave - site - 5);
            }
            helper.CopyTo(themed, cave);
            int selection = fix.SelectionBackground.GetDecimalAddress();
            byte[] selectedBackground = [0xD0, 0xD0, 0xD0, 0];
            if (!original.AsSpan(selection, 4).SequenceEqual(selectedBackground) ||
                !themed.AsSpan(selection, 4).SequenceEqual(selectedBackground))
                throw new InvalidDataException("The verified text selection background does not match.");
            static double Linear(byte channel)
            {
                double value = channel / 255.0;
                return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
            }
            double luminance = 0.2126 * Linear(color[2]) + 0.7152 * Linear(color[1]) + 0.0722 * Linear(color[0]);
            // The original D0D0D0 selection is almost indistinguishable from
            // light theme text. Choose the higher-contrast neutral background.
            byte selectionChannel = luminance > 0.179 ? (byte)0 : (byte)255;
            themed[selection] = themed[selection + 1] = themed[selection + 2] = selectionChannel;
        }

        private sealed class BinaryProfile
        {
            public string Id { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public int Length { get; set; }
            public string Sha256 { get; set; } = "";
            public string AddressFile { get; set; } = "";
            public ControlTextFix? ControlTextFix { get; set; }
        }

        private sealed class ControlTextFix
        {
            public string Cave { get; set; } = "";
            public string Converter { get; set; } = "";
            public string SelectionBackground { get; set; } = "";
            public string[] Calls { get; set; } = [];
        }
    }
}
