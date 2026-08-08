using System;
using System.Collections.Generic;

namespace OCC.Combat
{
    public enum RogueliteSaveLoadStatus
    {
        None,
        Missing,
        Success,
        CorruptData,
        StoreError
    }

    public interface IRogueliteSaveStore
    {
        bool HasKey(string key);
        string GetString(string key, string defaultValue = "");
        void SetString(string key, string value);
        void DeleteKey(string key);
        void Flush();
    }

    public sealed class RogueliteSaveGateway
    {
        public const string StoryKey = "occ.roguelite.iron_echoes";
        public const string ShortRunKey = "occ.roguelite.short_run";
        public const string MapRunKey = "occ.roguelite.map_run";
        public const string UiPreferencesKey = "occ.roguelite.ui_preferences";
        public const string CorruptBackupSuffix = ".corrupt_backup";

        private readonly IRogueliteSaveStore store;
        private readonly HashSet<string> writeProtectedKeys = new HashSet<string>(StringComparer.Ordinal);
        public string LastError { get; private set; } = string.Empty;
        public RogueliteSaveLoadStatus LastLoadStatus { get; private set; }
        public string LastFailedKey { get; private set; } = string.Empty;

        public RogueliteSaveGateway(IRogueliteSaveStore store)
        {
            this.store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public bool HasStory => Has(StoryKey);
        public bool HasShortRun => Has(ShortRunKey);
        public bool HasMapRun => Has(MapRunKey);

        public bool TryLoadStory(out RogueliteStoryPackage package) => TryLoad(StoryKey, RogueliteStoryPackage.FromJson, out package);
        public bool TryLoadShortRun(out ShortRogueliteRun run) => TryLoad(ShortRunKey, ShortRogueliteRun.FromJson, out run);
        public bool TryLoadMapRun(out RogueliteMapRun run) => TryLoad(MapRunKey, RogueliteMapRun.FromJson, out run);

        public RogueliteUiPreferences LoadUiPreferences()
        {
            LastError = string.Empty;
            try { return RogueliteUiPreferences.FromDataString(store.GetString(UiPreferencesKey, string.Empty)); }
            catch (Exception exception) { LastError = Describe(UiPreferencesKey, exception); return new RogueliteUiPreferences(); }
        }

        public bool SaveStory(RogueliteStoryPackage package) => Save(StoryKey, package?.ToJson());
        public bool SaveShortRun(ShortRogueliteRun run) => Save(ShortRunKey, run?.ToJson());
        public bool SaveMapRun(RogueliteMapRun run) => Save(MapRunKey, run?.ToJson());
        public bool SaveUiPreferences(RogueliteUiPreferences preferences) => Save(UiPreferencesKey, preferences?.ToDataString());

        public bool DeleteStory() => Delete(StoryKey);
        public bool DeleteShortRun() => Delete(ShortRunKey);
        public bool DeleteMapRun() => Delete(MapRunKey);

        private bool Has(string key)
        {
            LastError = string.Empty;
            try { return store.HasKey(key); }
            catch (Exception exception) { LastError = Describe(key, exception); return false; }
        }

        private bool TryLoad<T>(string key, Func<string, T> parser, out T value) where T : class
        {
            LastError = string.Empty;
            LastLoadStatus = RogueliteSaveLoadStatus.None;
            LastFailedKey = string.Empty;
            value = null;
            try
            {
                if (!store.HasKey(key))
                {
                    LastLoadStatus = RogueliteSaveLoadStatus.Missing;
                    return false;
                }

                string rawValue = store.GetString(key, string.Empty);
                try
                {
                    value = parser(rawValue);
                    if (value == null) throw new InvalidOperationException("Save parser returned null.");
                    LastLoadStatus = RogueliteSaveLoadStatus.Success;
                    writeProtectedKeys.Remove(key);
                    return true;
                }
                catch (Exception exception)
                {
                    LastLoadStatus = RogueliteSaveLoadStatus.CorruptData;
                    LastFailedKey = key;
                    writeProtectedKeys.Add(key);
                    LastError = Describe(key, exception);
                    PreserveCorruptValue(key, rawValue);
                    return false;
                }
            }
            catch (Exception exception)
            {
                LastLoadStatus = RogueliteSaveLoadStatus.StoreError;
                LastFailedKey = key;
                writeProtectedKeys.Add(key);
                LastError = Describe(key, exception);
                return false;
            }
        }

        private bool Save(string key, string value)
        {
            LastError = string.Empty;
            if (value == null) { LastError = key + ": value is null"; return false; }
            if (writeProtectedKeys.Contains(key))
            {
                LastError = key + ": write blocked after failed load; delete the slot explicitly before replacing it";
                return false;
            }
            try { store.SetString(key, value); store.Flush(); return true; }
            catch (Exception exception) { LastError = Describe(key, exception); return false; }
        }

        private bool Delete(string key)
        {
            LastError = string.Empty;
            try
            {
                store.DeleteKey(key);
                store.Flush();
                writeProtectedKeys.Remove(key);
                if (LastFailedKey == key)
                {
                    LastFailedKey = string.Empty;
                    LastLoadStatus = RogueliteSaveLoadStatus.None;
                }
                return true;
            }
            catch (Exception exception) { LastError = Describe(key, exception); return false; }
        }

        public static string CorruptBackupKey(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("Save key required.", nameof(key));
            return key + CorruptBackupSuffix;
        }

        private void PreserveCorruptValue(string key, string rawValue)
        {
            try
            {
                string backupKey = CorruptBackupKey(key);
                if (store.HasKey(backupKey)) return;
                store.SetString(backupKey, rawValue ?? string.Empty);
                store.Flush();
            }
            catch (Exception backupException)
            {
                LastError += " | corrupt backup failed: " + backupException.GetType().Name + " - " + backupException.Message;
            }
        }

        private static string Describe(string key, Exception exception) => key + ": " + exception.GetType().Name + " - " + exception.Message;
    }
}
