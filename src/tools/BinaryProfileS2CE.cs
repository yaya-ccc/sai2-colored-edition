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
            return GetProfile(binary, hash);
        }

        private static BinaryProfile GetProfile(byte[] binary, string hash)
        {
            var profiles = ReadProfiles();
            var profile = profiles.SingleOrDefault(p => p.Length == binary.Length &&
                string.Equals(p.Sha256, hash, StringComparison.OrdinalIgnoreCase));
            if (profile is null)
                throw new InvalidDataException($"Unsupported SAI2 executable (SHA-256: {hash}). " +
                    "A verified address table is required for this exact build. No files were changed.");

            return profile;
        }

        public static Dictionary<string, int> GetAddresses(byte[] binary)
            => GetAddresses(GetProfile(binary), binary.Length);

        // Resolve the profile once and validate without copying or patching the executable.
        public static string Validate(byte[] binary, out string sha256)
        {
            sha256 = Convert.ToHexString(SHA256.HashData(binary));
            var profile = GetProfile(binary, sha256);
            GetAddresses(profile, binary.Length);
            if (profile.ControlTextFix is { } fix)
                ValidateControlTextLayout(binary, binary, fix);
            return profile.DisplayName;
        }

        private static Dictionary<string, int> GetAddresses(BinaryProfile profile, int binaryLength)
        {
            var addresses = JsonSerializer.Deserialize<Dictionary<string, string>>(
                File.ReadAllText(Path.Combine("./ref/init/data", Path.GetFileName(profile.AddressFile))))
                ?.ConvertToDecimalAddressDictionary()
                ?? throw new InvalidDataException("The address table is empty.");
            if (addresses.Values.Any(address => address < 0 || address >= binaryLength))
                throw new InvalidDataException("The address table contains an out-of-bounds offset.");
            return addresses;
        }

        public static void ApplyControlTextColor(byte[] original, byte[] themed, byte[] color)
        {
            var fix = GetProfile(original).ControlTextFix;
            if (fix is null) return;
            if (color.Length < 3)
                throw new ArgumentException("The control-text color requires three channels.", nameof(color));
            if (themed.Length != original.Length)
                throw new InvalidDataException("The themed executable has a different length.");
            ValidateControlTextLayout(original, themed, fix);
            int cave = fix.Cave.GetDecimalAddress();
            // The original leaf converter maps internal RGB to GDI COLORREF.
            // Only the three verified UI text paths are redirected. Other
            // graphics/color conversion callers continue to use the original.
            byte[] conversion = [0x89, 0xC8, 0xC1, 0xC0, 0x08, 0x0F, 0xC8, 0xC3];
            byte[] helper = [0x85, 0xC9, 0x75, 0x05, 0xB9,
                color[0], color[1], color[2], 0,
                .. conversion];
            foreach (string address in fix.Calls)
            {
                int site = address.GetDecimalAddress();
                BinaryPrimitives.WriteInt32LittleEndian(themed.AsSpan(site + 1, 4), cave - site - 5);
            }
            helper.CopyTo(themed, cave);
            int selection = fix.SelectionBackground.GetDecimalAddress();
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

        private static void ValidateControlTextLayout(byte[] original, byte[] themed, ControlTextFix fix)
        {
            int cave = fix.Cave.GetDecimalAddress();
            int converter = fix.Converter.GetDecimalAddress();
            ReadOnlySpan<byte> conversion = [0x89, 0xC8, 0xC1, 0xC0, 0x08, 0x0F, 0xC8, 0xC3];
            int helperLength = 9 + conversion.Length;
            var originalCave = CheckedSlice(original, cave, helperLength);
            if (!CheckedSlice(original, converter, conversion.Length).SequenceEqual(conversion) ||
                originalCave.ContainsAnyExcept((byte)0xCC) ||
                !CheckedSlice(themed, cave, helperLength).SequenceEqual(originalCave))
                throw new InvalidDataException("The verified control-text code layout does not match.");
            foreach (string address in fix.Calls)
            {
                int site = address.GetDecimalAddress();
                var call = CheckedSlice(original, site, 5);
                if (call[0] != 0xE8 ||
                    BinaryPrimitives.ReadInt32LittleEndian(call[1..]) != converter - site - 5 ||
                    !call.SequenceEqual(CheckedSlice(themed, site, 5)))
                    throw new InvalidDataException("A verified UI text conversion call does not match.");
            }
            int selection = fix.SelectionBackground.GetDecimalAddress();
            ReadOnlySpan<byte> selectedBackground = [0xD0, 0xD0, 0xD0, 0];
            if (!CheckedSlice(original, selection, 4).SequenceEqual(selectedBackground) ||
                !CheckedSlice(themed, selection, 4).SequenceEqual(selectedBackground))
                throw new InvalidDataException("The verified text selection background does not match.");
        }

        private static ReadOnlySpan<byte> CheckedSlice(byte[] binary, int offset, int length)
        {
            if (offset < 0 || offset > binary.Length - length)
                throw new InvalidDataException("The control-text patch contains an out-of-bounds offset.");
            return binary.AsSpan(offset, length);
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
