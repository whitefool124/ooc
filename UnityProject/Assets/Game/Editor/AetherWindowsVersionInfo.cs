using System;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

namespace OCC.Combat.EditorTools
{
    // Unity's Windows template retains its engine version resource with blank
    // product fields. Fill the application metadata without replacing icons.
    public static class AetherWindowsVersionInfo
    {
        public static void Apply(string executable)
        {
            byte[] fixedInfo;
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                foreach (uint value in new uint[] { 0xFEEF04BD, 0x10000, 0x10000, 0, 0x10000, 0,
                    0x3f, 0, 0x40004, 1, 0, 0, 0 }) writer.Write(value);
                fixedInfo = stream.ToArray();
            }
            byte[] table = Block("040904b0", null, 1,
                Text("CompanyName", "DefaultCompany"), Text("FileDescription", "以太"),
                Text("ProductName", "以太"), Text("InternalName", "以太"),
                Text("OriginalFilename", "以太.exe"), Text("FileVersion", "1.0.0.0"), Text("ProductVersion", "1.0.0.0"));
            byte[] resource = Block("VS_VERSION_INFO", fixedInfo, 0,
                Block("StringFileInfo", null, 1, table),
                Block("VarFileInfo", null, 1, Block("Translation", new byte[] { 9, 4, 176, 4 }, 0)));
            IntPtr update = BeginUpdateResource(executable, false);
            if (update == IntPtr.Zero) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            bool committed = false;
            try
            {
                if (!UpdateResource(update, new IntPtr(16), new IntPtr(1), 1033, resource, (uint)resource.Length))
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                if (!EndUpdateResource(update, false))
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                committed = true;
            }
            finally { if (!committed) EndUpdateResource(update, true); }
            var info = System.Diagnostics.FileVersionInfo.GetVersionInfo(executable);
            if (info.ProductName != "以太" || info.FileDescription != "以太")
                throw new InvalidOperationException("Windows product metadata readback failed.");
        }
        private static byte[] Text(string key, string value) => Block(key, Encoding.Unicode.GetBytes(value + "\0"), 1);
        private static byte[] Block(string key, byte[] value, ushort type, params byte[][] children)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write((ushort)0);
                writer.Write((ushort)(value == null ? 0 : type == 1 ? value.Length / 2 : value.Length));
                writer.Write(type); writer.Write(Encoding.Unicode.GetBytes(key + "\0"));
                Align(writer);
                if (value != null) writer.Write(value);
                foreach (byte[] child in children) { Align(writer); writer.Write(child); }
                long length = stream.Length; stream.Position = 0; writer.Write(checked((ushort)length));
                return stream.ToArray();
            }
        }
        private static void Align(BinaryWriter writer)
        { while (writer.BaseStream.Position % 4 != 0) writer.Write((byte)0); }
        [DllImport("kernel32.dll", EntryPoint = "BeginUpdateResourceW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr BeginUpdateResource(string file, bool deleteExisting);
        [DllImport("kernel32.dll", EntryPoint = "UpdateResourceW", SetLastError = true)]
        private static extern bool UpdateResource(IntPtr update, IntPtr type, IntPtr name, ushort language, byte[] data, uint size);
        [DllImport("kernel32.dll", EntryPoint = "EndUpdateResourceW", SetLastError = true)]
        private static extern bool EndUpdateResource(IntPtr update, bool discard);
    }
}
