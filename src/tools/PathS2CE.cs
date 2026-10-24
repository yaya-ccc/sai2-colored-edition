namespace S2CE.Tools
{
    class PathS2CE {
        public static string? SourceExecutable { get; private set; }
        public static string originalSai2 => SourceExecutable ?? "./ref/sai2.exe";
        public static string oldSai2 => "./ref/sai2.old.exe";
        public static string sai2 => SourceExecutable is null ? "./ref/sai2.exe" :
            System.IO.Path.Combine(System.IO.Path.GetDirectoryName(SourceExecutable)!, "sai2-colored.exe");
        public static string state => SourceExecutable is null ? "./ref/sai2.theme-state.json" :
            System.IO.Path.Combine(System.IO.Path.GetDirectoryName(SourceExecutable)!, "sai2-colored.theme-state.json");
        public static string sourceConfiguration => System.IO.Path.ChangeExtension(originalSai2, ".ini");
        public static string generatedConfiguration => System.IO.Path.ChangeExtension(sai2, ".ini");

        public static void UseSource(string? path)
        {
            if (path is not null && string.Equals(System.IO.Path.GetFileName(path), "sai2-colored.exe", StringComparison.OrdinalIgnoreCase))
                throw new System.IO.InvalidDataException("Select the original sai2.exe, not the generated colored copy.");
            SourceExecutable = path is null ? null : System.IO.Path.GetFullPath(path);
        }
    }
}
