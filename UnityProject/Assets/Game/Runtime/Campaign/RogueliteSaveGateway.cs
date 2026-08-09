using System;

namespace OCC.Combat
{
    public enum RogueliteSaveLoadStatus
    {
        None,
        Missing,
        Success,
        CorruptData,
        InvalidSemantics,
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
        public const string WriteLockSuffix = ".write_lock";

        private readonly IRogueliteSaveStore store;
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

        public bool TryLoadStory(out RogueliteStoryPackage package) => TryLoad(StoryKey, RogueliteStoryPackage.FromJson, null, null, out package);
        public bool TryLoadShortRun(out ShortRogueliteRun run) => TryLoad(ShortRunKey, ShortRogueliteRun.FromJson, null, null, out run);
        public bool TryLoadMapRun(out RogueliteMapRun run) => TryLoad(MapRunKey, RogueliteMapRun.FromJson,
            value => RogueliteMapRunValidator.Validate(value), RogueliteMapRunValidator.ValidateSerializedCurrent, out run);

        public RogueliteUiPreferences LoadUiPreferences()
        {
            LastError = string.Empty;
            try { return RogueliteUiPreferences.FromDataString(store.GetString(UiPreferencesKey, string.Empty)); }
            catch (Exception exception) { LastError = Describe(UiPreferencesKey, exception); return new RogueliteUiPreferences(); }
        }

        public bool SaveStory(RogueliteStoryPackage package) => SaveVerified(StoryKey, package?.ToJson(), RogueliteStoryPackage.FromJson, null, null);
        public bool SaveShortRun(ShortRogueliteRun run) => SaveVerified(ShortRunKey, run?.ToJson(), ShortRogueliteRun.FromJson, null, null);
        public bool SaveMapRun(RogueliteMapRun run)
        {
            string value;
            try { value = run?.ToJson(); }
            catch (Exception exception) { LastError = Describe(MapRunKey, exception); return false; }
            return SaveVerified(MapRunKey, value, RogueliteMapRun.FromJson,
                candidate => RogueliteMapRunValidator.Validate(candidate), RogueliteMapRunValidator.ValidateSerializedCurrent);
        }
        public bool SaveUiPreferences(RogueliteUiPreferences preferences) => SaveVerified(UiPreferencesKey, preferences?.ToDataString(), RogueliteUiPreferences.FromDataString, null, null);

        public bool DeleteStory() => Delete(StoryKey);
        public bool DeleteShortRun() => Delete(ShortRunKey);
        public bool DeleteMapRun() => Delete(MapRunKey);

        public static string CorruptBackupKey(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("Save key required.", nameof(key));
            return key + CorruptBackupSuffix;
        }

        public static string WriteLockKey(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("Save key required.", nameof(key));
            return key + WriteLockSuffix;
        }

        private bool Has(string key)
        {
            LastError = string.Empty;
            try { return store.HasKey(key); }
            catch (Exception exception) { LastError = Describe(key, exception); return false; }
        }

        private bool TryLoad<T>(string key, Func<string, T> parser, Func<T, RogueliteMapRunValidationResult> validator,
            Func<string, RogueliteMapRunValidationResult> rawValidator, out T value) where T : class
        {
            ResetResult();
            value = null;
            string rawValue;
            try
            {
                if (!store.HasKey(key))
                {
                    LastLoadStatus = RogueliteSaveLoadStatus.Missing;
                    return false;
                }
                rawValue = store.GetString(key, string.Empty);
            }
            catch (Exception exception)
            {
                Fail(key, RogueliteSaveLoadStatus.StoreError, exception);
                return false;
            }

            try
            {
                ThrowIfInvalid(rawValidator?.Invoke(rawValue));
                value = parser(rawValue);
                if (value == null) throw new InvalidOperationException("Save parser returned null.");
                ThrowIfInvalid(validator?.Invoke(value));
                LastLoadStatus = RogueliteSaveLoadStatus.Success;
                return true;
            }
            catch (RogueliteSaveSemanticException exception)
            {
                value = null;
                Fail(key, RogueliteSaveLoadStatus.InvalidSemantics, exception);
                ProtectFailedValue(key, rawValue);
                return false;
            }
            catch (Exception exception)
            {
                value = null;
                Fail(key, RogueliteSaveLoadStatus.CorruptData, exception);
                ProtectFailedValue(key, rawValue);
                return false;
            }
        }

        private bool SaveVerified<T>(string key, string value, Func<string, T> parser,
            Func<T, RogueliteMapRunValidationResult> validator, Func<string, RogueliteMapRunValidationResult> rawValidator) where T : class
        {
            LastError = string.Empty;
            LastFailedKey = string.Empty;
            if (value == null) { LastError = key + ": value is null"; return false; }

            bool hadOriginal;
            string original;
            try
            {
                if (store.HasKey(WriteLockKey(key)))
                {
                    LastError = key + ": write blocked by persistent protection; delete the slot explicitly before replacing it";
                    return false;
                }
                hadOriginal = store.HasKey(key);
                original = hadOriginal ? store.GetString(key, string.Empty) : string.Empty;
            }
            catch (Exception exception) { LastError = Describe(key, exception); return false; }

            try
            {
                VerifyValue(value, parser, validator, rawValidator);
            }
            catch (Exception exception)
            {
                LastError = Describe(key, exception);
                return false;
            }

            bool writeCompleted = false;
            try
            {
                store.SetString(key, value);
                writeCompleted = true;
                store.Flush();
                string readback = store.GetString(key, string.Empty);
                if (!string.Equals(readback, value, StringComparison.Ordinal)) throw new InvalidOperationException("Save readback did not match the serialized value.");
                VerifyValue(readback, parser, validator, rawValidator);
                return true;
            }
            catch (Exception exception)
            {
                LastError = Describe(key, exception);
                if (writeCompleted)
                {
                    string failedReadback = SafeRead(key, value);
                    ProtectFailedValue(key, failedReadback);
                    RestoreOriginal(key, hadOriginal, original);
                }
                return false;
            }
        }

        private static void VerifyValue<T>(string value, Func<string, T> parser,
            Func<T, RogueliteMapRunValidationResult> validator, Func<string, RogueliteMapRunValidationResult> rawValidator) where T : class
        {
            ThrowIfInvalid(rawValidator?.Invoke(value));
            T reparsed = parser(value);
            if (reparsed == null) throw new InvalidOperationException("Save parser returned null.");
            ThrowIfInvalid(validator?.Invoke(reparsed));
            if (reparsed is RogueliteMapRun mapRun && mapRun.ToJson() != value)
                throw new RogueliteSaveSemanticException(Invalid("serialization.text_changed"));
        }

        private bool Delete(string key)
        {
            LastError = string.Empty;
            try
            {
                store.DeleteKey(key);
                store.DeleteKey(WriteLockKey(key));
                store.Flush();
                if (LastFailedKey == key) ResetResult();
                return true;
            }
            catch (Exception exception) { LastError = Describe(key, exception); return false; }
        }

        private void ProtectFailedValue(string key, string rawValue)
        {
            try
            {
                string backupKey = CorruptBackupKey(key);
                if (!store.HasKey(backupKey)) store.SetString(backupKey, rawValue ?? string.Empty);
                if (!store.HasKey(WriteLockKey(key))) store.SetString(WriteLockKey(key), "protected-v1");
                store.Flush();
            }
            catch (Exception backupException)
            {
                LastError += " | protection failed: " + backupException.GetType().Name + " - " + backupException.Message;
            }
        }

        private void RestoreOriginal(string key, bool hadOriginal, string original)
        {
            try
            {
                if (hadOriginal) store.SetString(key, original);
                else store.DeleteKey(key);
                store.Flush();
            }
            catch (Exception restoreException)
            {
                LastError += " | rollback failed: " + restoreException.GetType().Name + " - " + restoreException.Message;
            }
        }

        private string SafeRead(string key, string fallback)
        {
            try { return store.GetString(key, fallback); }
            catch { return fallback; }
        }

        private void ResetResult()
        {
            LastError = string.Empty;
            LastLoadStatus = RogueliteSaveLoadStatus.None;
            LastFailedKey = string.Empty;
        }

        private void Fail(string key, RogueliteSaveLoadStatus status, Exception exception)
        {
            LastLoadStatus = status;
            LastFailedKey = key;
            LastError = Describe(key, exception);
        }

        private static void ThrowIfInvalid(RogueliteMapRunValidationResult result)
        {
            if (result != null && !result.IsValid) throw new RogueliteSaveSemanticException(result);
        }

        private static RogueliteMapRunValidationResult Invalid(string error)
        {
            RogueliteMapRunValidationResult result = new RogueliteMapRunValidationResult();
            result.Add(error);
            return result;
        }

        private static string Describe(string key, Exception exception) => key + ": " + exception.GetType().Name + " - " + exception.Message;
    }
}
