using System;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    public static class AcademyTutorialStore
    {
        private static string legacyKey;
        private static FirstRunExperienceState legacy;
        private static string Key => FirstExperienceSaveRouting.ResolveFrontEndKey(
            "OCC.AcademyTutorial.v1.slot." + FirstExperienceSaveRouting.ActiveSlot);

        public static AcademyTutorialProgress Load()
        {
            string json = PlayerPrefs.GetString(Key, "");
            if (string.IsNullOrEmpty(json)) return new AcademyTutorialProgress();
            try { return JsonUtility.FromJson<AcademyTutorialProgress>(json) ?? new AcademyTutorialProgress(); }
            catch (ArgumentException) { return new AcademyTutorialProgress { enabled = false }; }
        }
        public static bool Save(AcademyTutorialProgress progress)
        {
            string json = JsonUtility.ToJson(progress);
            PlayerPrefs.SetString(Key, json); PlayerPrefs.Save();
            return PlayerPrefs.GetString(Key, "") == json;
        }
        public static void SetEnabled(bool enabled)
        {
            var progress = Load(); progress.enabled = enabled; Save(progress);
        }
        public static void Record(string id, string fact)
        {
            if (FirstExperienceSaveRouting.ActiveSlot < 0) return;
            var progress = Load();
            if (progress.Observe(id, fact)) Save(progress);
        }
        public static void RecordDeviceSettingReadback()
        {
            PlayerPrefs.SetInt(FirstExperienceSaveRouting.ResolveFrontEndKey("OCC.AcademyTutorial.DeviceSettingsReadback.v1"), 1); PlayerPrefs.Save();
            CopyDeviceSettingEvidence();
        }
        public static void CopyDeviceSettingEvidence()
        {
            if (PlayerPrefs.GetInt(FirstExperienceSaveRouting.ResolveFrontEndKey("OCC.AcademyTutorial.DeviceSettingsReadback.v1"), 0) != 1) return;
            Record("A01", "settings-saved"); Record("A01", "settings-readback");
            Record("A02", "settings-preview"); Record("A02", "settings-return");
        }
        public static FirstRunExperienceState LegacyProgress(FirstRunExperienceState oldFixedProgress)
        {
            if (legacy == null || legacyKey != Key)
            {
                legacyKey = Key;
                var data = Load();
                legacy = new FirstRunExperienceState
                { PendingTutorialId = data.legacyPending, PendingTutorialAction = data.legacyAction, TutorialActionReady = data.legacyReady };
                foreach (string id in data.legacyCompleted) legacy.CompletedTutorialIds.Add(id);
                if (oldFixedProgress != null)
                    foreach (string id in oldFixedProgress.CompletedTutorialIds) legacy.CompletedTutorialIds.Add(id);
            }
            return legacy;
        }
        public static bool SaveLegacy()
        {
            if (legacy == null || legacyKey != Key) return true;
            var data = Load(); data.legacyCompleted = new System.Collections.Generic.List<string>(legacy.CompletedTutorialIds);
            data.legacyPending = legacy.PendingTutorialId; data.legacyAction = legacy.PendingTutorialAction;
            data.legacyReady = legacy.TutorialActionReady; return Save(data);
        }
    }
}
