using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace S2CE.Components;

internal static class NativeColorPicker
{
    private static readonly int[] customColors = Enumerable.Repeat(0x00FFFFFF, 16).ToArray();

    public static Color? Show(Window owner, Color initial)
    {
        IntPtr colors = Marshal.AllocHGlobal(16 * sizeof(int));
        try
        {
            Marshal.Copy(customColors, 0, colors, 16);
            var dialog = new ChooseColorData {
                Size = Marshal.SizeOf<ChooseColorData>(), Owner = new WindowInteropHelper(owner).Handle,
                Color = (uint)(initial.R | initial.G << 8 | initial.B << 16), CustomColors = colors, Flags = 3
            };
            if (!ChooseColor(ref dialog)) return null;
            Marshal.Copy(colors, customColors, 0, 16);
            return Color.FromRgb((byte)dialog.Color, (byte)(dialog.Color >> 8), (byte)(dialog.Color >> 16));
        }
        finally { Marshal.FreeHGlobal(colors); }
    }

    [DllImport("comdlg32.dll", EntryPoint = "ChooseColorW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ChooseColor(ref ChooseColorData data);

    [StructLayout(LayoutKind.Sequential)]
    private struct ChooseColorData
    {
        public int Size;
        public IntPtr Owner, Instance;
        public uint Color;
        public IntPtr CustomColors;
        public uint Flags;
        public IntPtr CustomData, Hook, Template;
    }
}
