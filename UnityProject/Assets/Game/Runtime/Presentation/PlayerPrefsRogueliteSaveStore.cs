using UnityEngine;

namespace OCC.Combat.Presentation
{
    public sealed class PlayerPrefsRogueliteSaveStore : IRogueliteSaveStore
    {
        public bool HasKey(string key) => PlayerPrefs.HasKey(key);
        public string GetString(string key, string defaultValue = "") => PlayerPrefs.GetString(key, defaultValue);
        public void SetString(string key, string value) => PlayerPrefs.SetString(key, value);
        public void DeleteKey(string key) => PlayerPrefs.DeleteKey(key);
        public void Flush() => PlayerPrefs.Save();
    }
}
