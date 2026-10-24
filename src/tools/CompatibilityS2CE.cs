using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;

namespace S2CE.Tools
{
    internal sealed record CompatibilityResult(string Path, string Build, string Sha256,
        bool IsSupported, bool IsThemed, string Details);

    // Detection is read-only. Current themed copies are identified by their
    // verified original backup; arbitrary files must match an original profile.
    internal static class CompatibilityS2CE
    {
        public static CompatibilityResult CheckCurrent() => Inspect(PathS2CE.originalSai2, true);
        public static CompatibilityResult CheckFile(string path) => Inspect(path, false);

        private static CompatibilityResult Inspect(string path, bool current)
        {
            string fullPath = System.IO.Path.GetFullPath(path);
            string build = "未知构建";
            string hash = "";
            try
            {
                byte[] binary = current ? new FileS2CE().ReadOriginal() : File.ReadAllBytes(fullPath);
                hash = Convert.ToHexString(SHA256.HashData(binary));
                var info = FileVersionInfo.GetVersionInfo(fullPath);
                if (!string.IsNullOrWhiteSpace(info.FileVersion)) build = "文件版本 " + info.FileVersion;
                build = BinaryProfileS2CE.Describe(binary);
                // Include address availability/bounds and the version-specific
                // instruction baseline in the compatibility decision.
                BinaryProfileS2CE.GetAddresses(binary);
                BinaryProfileS2CE.ApplyControlTextColor(binary, (byte[])binary.Clone(), [0xC1, 0xC1, 0xC1]);
                bool themed = current && (PathS2CE.SourceExecutable is not null ? File.Exists(PathS2CE.sai2) :
                    !binary.AsSpan().SequenceEqual(File.ReadAllBytes(fullPath)));
                return new(fullPath, build, hash, true, themed,
                    themed ? "已识别换肤副本，原文件及状态校验通过。" : "精确构建、颜色地址和适配规则校验通过。");
            }
            catch (Exception exception) when (exception is IOException or InvalidDataException or UnauthorizedAccessException
                or JsonException or InvalidOperationException or ArgumentException)
            {
                return new(fullPath, build, hash, false, false,
                    "未适配或文件状态异常，已禁止换肤。" + Environment.NewLine + exception.Message);
            }
        }
    }
}
