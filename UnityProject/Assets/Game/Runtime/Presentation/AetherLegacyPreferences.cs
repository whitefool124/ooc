using System;
using System.Text;
using System.Runtime.InteropServices;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    // Product branding changes Unity's Windows PlayerPrefs namespace. Import only
    // this game's known data, through PlayerPrefs so its in-memory cache agrees.
    public static class AetherLegacyPreferences
    {
        public const string Marker = "OCC.Branding.MyProjectImported.v1";
        public static int ImportedCount { get; private set; }

        public static string LogicalKey(string registryName)
        {
            int suffix = registryName.LastIndexOf("_h", StringComparison.Ordinal);
            string key = suffix > 0 ? registryName.Substring(0, suffix) : registryName;
            return key.StartsWith("OCC.FirstExperiencePrototype.", StringComparison.Ordinal) ||
                key.StartsWith("occ.first_experience.map_run.slot.", StringComparison.Ordinal) ||
                key.StartsWith("occ.roguelite.", StringComparison.Ordinal) ||
                key == "OCC.Visual.DeskLampBrightness.v1" || key == "OCC.LootUnclaimedExitReminderShown" ? key : null;
        }

        public static bool IsFloat(string key) => key == "OCC.FirstExperiencePrototype.volume.v1" ||
            key == "OCC.Visual.DeskLampBrightness.v1";

        public static string DecodeString(byte[] bytes) => Encoding.UTF8.GetString(bytes).TrimEnd('\0');
        // Unity's Windows registry backend has written floats as both 32-bit
        // values and 64-bit doubles under REG_DWORD. Keep the actual payload.
        public static float DecodeFloat(byte[] bytes) => bytes.Length == 8 ? (float)BitConverter.ToDouble(bytes, 0) :
            bytes.Length == 4 ? BitConverter.ToSingle(bytes, 0) : throw new ArgumentException("Invalid float preference payload.");

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        private static void Import()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            if (Application.productName != "以太" || PlayerPrefs.GetInt(Marker, 0) != 0) return;
            try
            {
                int opened = RegOpenKeyEx(new IntPtr(unchecked((int)0x80000001)), @"Software\DefaultCompany\My project", 0, 1, out IntPtr legacy);
                if (opened != 0 && opened != 2) throw new System.ComponentModel.Win32Exception(opened);
                if (opened == 0)
                try
                {
                    for (uint index = 0; ; index++)
                    {
                        var name = new StringBuilder(16384);
                        uint nameLength = 16384, size = 0;
                        int result = RegEnumValue(legacy, index, name, ref nameLength, IntPtr.Zero, out uint type, null, ref size);
                        if (result == 259) break;
                        if (result != 0) throw new System.ComponentModel.Win32Exception(result);
                        string key = LogicalKey(name.ToString());
                        if (key == null || PlayerPrefs.HasKey(key)) continue;
                        if (size > 16 * 1024 * 1024) throw new InvalidOperationException("Oversized legacy preference: " + key);
                        var bytes = new byte[size];
                        result = RegQueryValueEx(legacy, name.ToString(), IntPtr.Zero, out type, bytes, ref size);
                        if (result != 0) throw new System.ComponentModel.Win32Exception(result);
                        if (type == 3) PlayerPrefs.SetString(key, DecodeString(bytes));
                        else if (type == 1) PlayerPrefs.SetString(key, Encoding.Unicode.GetString(bytes).TrimEnd('\0'));
                        else if (type == 4 && IsFloat(key)) PlayerPrefs.SetFloat(key, DecodeFloat(bytes));
                        else if (type == 4 && bytes.Length == 4)
                        {
                            int bits = BitConverter.ToInt32(bytes, 0);
                            PlayerPrefs.SetInt(key, bits);
                        }
                        else throw new InvalidOperationException("Unsupported legacy preference type for " + key);
                        ImportedCount++;
                    }
                }
                finally { RegCloseKey(legacy); }
                // One-time marker prevents deleted slots from resurrecting later.
                PlayerPrefs.SetInt(Marker, 1);
                PlayerPrefs.Save();
                Debug.Log("AETHER_LEGACY_PREFERENCES_IMPORTED " + ImportedCount);
            }
            catch (Exception error)
            {
                // Keep the source intact and leave migration retryable.
                PlayerPrefs.Save();
                Debug.LogError("以太旧版存档迁移未完成：" + error.Message);
            }
#endif
        }
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        [DllImport("advapi32.dll", EntryPoint = "RegOpenKeyExW", CharSet = CharSet.Unicode)]
        private static extern int RegOpenKeyEx(IntPtr key, string subkey, uint options, uint access, out IntPtr result);
        [DllImport("advapi32.dll", EntryPoint = "RegEnumValueW", CharSet = CharSet.Unicode)]
        private static extern int RegEnumValue(IntPtr key, uint index, StringBuilder name, ref uint nameLength,
            IntPtr reserved, out uint type, byte[] data, ref uint size);
        [DllImport("advapi32.dll", EntryPoint = "RegQueryValueExW", CharSet = CharSet.Unicode)]
        private static extern int RegQueryValueEx(IntPtr key, string name, IntPtr reserved, out uint type, [Out] byte[] data, ref uint size);
        [DllImport("advapi32.dll")]
        private static extern int RegCloseKey(IntPtr key);
#endif
    }
}
