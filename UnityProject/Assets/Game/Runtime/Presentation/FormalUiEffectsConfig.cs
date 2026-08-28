using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    [Serializable]
    public sealed class OccPeripheralAssetEntry
    {
        public string id;
        public string resourcePath;
    }

    [Serializable]
    public sealed class OccPeripheralFeedbackEntry
    {
        public string id;
        public string resourcePath;
        public int frameCount;
        public int framesPerSecond;
    }

    [Serializable]
    public sealed class OccPeripheralUiData
    {
        public string schema;
        public string startupBackdrop;
        public string scanlineSprite;
        public string transitionSprite;
        public OccPeripheralAssetEntry[] backdrops;
        public OccPeripheralAssetEntry[] decorations;
        public OccPeripheralAssetEntry[] illustrations;
        public OccPeripheralAssetEntry[] chapterDividers;
        public OccPeripheralAssetEntry[] chapterMarkers;
        public OccPeripheralFeedbackEntry[] feedback;
        public float startupHoldSeconds;
        public float transitionSeconds;
        public float ambientScanSeconds;
    }

    public static class FormalUiEffectsConfig
    {
        public const string ResourcePath = "Config/OccPeripheralUiV01";
        public const string RequiredSchema = "occ.ui.peripheral.v0.1";
        private static OccPeripheralUiData data;
        private static Dictionary<string, OccPeripheralAssetEntry> backdrops;
        private static Dictionary<string, OccPeripheralAssetEntry> decorations;
        private static Dictionary<string, OccPeripheralAssetEntry> illustrations;
        private static Dictionary<string, OccPeripheralAssetEntry> chapterDividers;
        private static Dictionary<string, OccPeripheralAssetEntry> chapterMarkers;
        private static Dictionary<string, OccPeripheralFeedbackEntry> feedback;

        public static OccPeripheralUiData Data
        {
            get { if (data == null) Load(); return data; }
        }

        public static string BackdropPath(string id)
        {
            EnsureLoaded();
            if (!backdrops.TryGetValue(id, out OccPeripheralAssetEntry entry)) throw new KeyNotFoundException("Missing peripheral UI backdrop: " + id);
            return entry.resourcePath;
        }

        public static string DecorationPath(string id)
        {
            EnsureLoaded();
            if (!decorations.TryGetValue(id, out OccPeripheralAssetEntry entry)) throw new KeyNotFoundException("Missing peripheral UI decoration: " + id);
            return entry.resourcePath;
        }

        public static string IllustrationPath(string id)
        {
            EnsureLoaded();
            if (!illustrations.TryGetValue(id, out OccPeripheralAssetEntry entry)) throw new KeyNotFoundException("Missing peripheral UI illustration: " + id);
            return entry.resourcePath;
        }

        public static string ChapterDividerPath(string id)
        {
            EnsureLoaded();
            if (!chapterDividers.TryGetValue(id, out OccPeripheralAssetEntry entry)) throw new KeyNotFoundException("Missing peripheral UI chapter divider: " + id);
            return entry.resourcePath;
        }

        public static string ChapterMarkerPath(string id)
        {
            EnsureLoaded();
            if (!chapterMarkers.TryGetValue(id, out OccPeripheralAssetEntry entry)) throw new KeyNotFoundException("Missing peripheral UI chapter marker: " + id);
            return entry.resourcePath;
        }

        public static OccPeripheralFeedbackEntry Feedback(string id)
        {
            EnsureLoaded();
            if (!feedback.TryGetValue(id, out OccPeripheralFeedbackEntry entry)) throw new KeyNotFoundException("Missing peripheral UI feedback: " + id);
            return entry;
        }

        public static IReadOnlyList<string> Validate()
        {
            OccPeripheralUiData value = Data;
            var failures = new List<string>();
            if (value.schema != RequiredSchema) failures.Add("schema");
            if (string.IsNullOrWhiteSpace(value.startupBackdrop)) failures.Add("startupBackdrop");
            if (string.IsNullOrWhiteSpace(value.scanlineSprite)) failures.Add("scanlineSprite");
            if (string.IsNullOrWhiteSpace(value.transitionSprite)) failures.Add("transitionSprite");
            if (value.transitionSeconds <= 0f || value.ambientScanSeconds <= 0f) failures.Add("timing");
            AddDuplicateFailures(value.backdrops.Select(entry => entry.id), "backdrop", failures);
            AddDuplicateFailures(value.decorations.Select(entry => entry.id), "decoration", failures);
            AddDuplicateFailures(value.illustrations.Select(entry => entry.id), "illustration", failures);
            AddDuplicateFailures(value.chapterDividers.Select(entry => entry.id), "chapterDivider", failures);
            AddDuplicateFailures(value.chapterMarkers.Select(entry => entry.id), "chapterMarker", failures);
            AddDuplicateFailures(value.feedback.Select(entry => entry.id), "feedback", failures);
            foreach (OccPeripheralFeedbackEntry entry in value.feedback)
                if (entry.frameCount < 2 || entry.framesPerSecond <= 0) failures.Add("feedbackFrames:" + entry.id);
            return failures;
        }

        private static void EnsureLoaded() { if (data == null) Load(); }

        private static void Load()
        {
            TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null) throw new InvalidOperationException("Missing peripheral UI config: " + ResourcePath);
            data = JsonUtility.FromJson<OccPeripheralUiData>(asset.text);
            if (data == null) throw new InvalidOperationException("Invalid peripheral UI config: " + ResourcePath);
            data.backdrops = data.backdrops ?? Array.Empty<OccPeripheralAssetEntry>();
            data.decorations = data.decorations ?? Array.Empty<OccPeripheralAssetEntry>();
            data.illustrations = data.illustrations ?? Array.Empty<OccPeripheralAssetEntry>();
            data.chapterDividers = data.chapterDividers ?? Array.Empty<OccPeripheralAssetEntry>();
            data.chapterMarkers = data.chapterMarkers ?? Array.Empty<OccPeripheralAssetEntry>();
            data.feedback = data.feedback ?? Array.Empty<OccPeripheralFeedbackEntry>();
            backdrops = data.backdrops.ToDictionary(entry => entry.id, StringComparer.Ordinal);
            decorations = data.decorations.ToDictionary(entry => entry.id, StringComparer.Ordinal);
            illustrations = data.illustrations.ToDictionary(entry => entry.id, StringComparer.Ordinal);
            chapterDividers = data.chapterDividers.ToDictionary(entry => entry.id, StringComparer.Ordinal);
            chapterMarkers = data.chapterMarkers.ToDictionary(entry => entry.id, StringComparer.Ordinal);
            feedback = data.feedback.ToDictionary(entry => entry.id, StringComparer.Ordinal);
        }

        private static void AddDuplicateFailures(IEnumerable<string> values, string kind, ICollection<string> failures)
        {
            foreach (IGrouping<string, string> duplicate in values.GroupBy(value => value, StringComparer.Ordinal).Where(group => group.Count() > 1))
                failures.Add("duplicate:" + kind + ":" + duplicate.Key);
        }
    }
}
