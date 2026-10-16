using UnityEngine;

namespace OCC.Combat.Presentation
{
    public static class FirstExperienceSaveRouting
    {
        public const string ActiveSlotKey = "OCC.FirstExperiencePrototype.activeSlot.v2";
        private const string SlotMapPrefix = "occ.first_experience.map_run.slot.";

#if UNITY_EDITOR
        private static string verificationPrefix = string.Empty;
        // Editor acceptance uses the production disk store without overwriting a player's occupied slots.
        public static System.IDisposable BeginVerificationScope(string id)
        {
            if (string.IsNullOrWhiteSpace(id) || id.IndexOfAny(new[] { '.', '/', '\\' }) >= 0)
                throw new System.ArgumentException("A simple verification id is required.", nameof(id));
            if (!string.IsNullOrEmpty(verificationPrefix)) throw new System.InvalidOperationException("A verification scope is already active.");
            verificationPrefix = "occ.verification." + id + ".";
            return new VerificationScope();
        }
        private sealed class VerificationScope : System.IDisposable
        {
            public void Dispose() { verificationPrefix = string.Empty; }
        }
#endif
        public static string ResolveFrontEndKey(string key)
        {
#if UNITY_EDITOR
            return verificationPrefix + key;
#else
            return key;
#endif
        }

        public static int ActiveSlot => Mathf.Clamp(PlayerPrefs.GetInt(ResolveFrontEndKey(ActiveSlotKey), -1), -1, 2);

        public static void SelectSlot(int slot)
        {
            if (slot < 0 || slot > 2) throw new System.ArgumentOutOfRangeException(nameof(slot));
            PlayerPrefs.SetInt(ResolveFrontEndKey(ActiveSlotKey), slot);
            PlayerPrefs.Save();
        }

        public static bool HasMapRun(int slot) => PlayerPrefs.HasKey(ResolveMapKey(RogueliteSaveGateway.MapRunKey, slot));

        public static void DeleteMapRun(int slot)
        {
            PlayerPrefs.DeleteKey(ResolveMapKey(RogueliteSaveGateway.MapRunKey, slot));
            PlayerPrefs.Save();
        }

        public static string ResolveActiveKey(string key) => ResolveMapKey(key, ActiveSlot);

        public static string ResolveMapKey(string key, int slot)
        {
            if (slot < 0 || slot > 2 || string.IsNullOrEmpty(key) ||
                !key.StartsWith(RogueliteSaveGateway.MapRunKey, System.StringComparison.Ordinal)) return key;
            return ResolveFrontEndKey(SlotMapPrefix + slot + key.Substring(RogueliteSaveGateway.MapRunKey.Length));
        }
    }

    public sealed class PlayerPrefsRogueliteSaveStore : IRogueliteSaveStore
    {
        public bool HasKey(string key) => PlayerPrefs.HasKey(FirstExperienceSaveRouting.ResolveActiveKey(key));
        public string GetString(string key, string defaultValue = "") => PlayerPrefs.GetString(FirstExperienceSaveRouting.ResolveActiveKey(key), defaultValue);
        public void SetString(string key, string value) => PlayerPrefs.SetString(FirstExperienceSaveRouting.ResolveActiveKey(key), value);
        public void DeleteKey(string key) => PlayerPrefs.DeleteKey(FirstExperienceSaveRouting.ResolveActiveKey(key));
        public void Flush() => PlayerPrefs.Save();
    }
}
