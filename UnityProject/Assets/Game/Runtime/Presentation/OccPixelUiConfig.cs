using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    [Serializable]
    public sealed class OccPixelUiSkinEntry
    {
        public string id;
        public string resourcePath;
    }

    [Serializable]
    public sealed class OccPixelUiStateEntry
    {
        public string component;
        public string state;
        public string skin;
    }

    [Serializable]
    public sealed class OccPixelUiLayoutEntry
    {
        public string id;
        public string anchor;
        public float x;
        public float y;
        public float width;
        public float height;

        public Vector2 Position => new Vector2(x, y);
        public Vector2 Size => new Vector2(width, height);
    }

    [Serializable]
    public sealed class OccPixelUiPaletteEntry
    {
        public string id;
        public string hex;
    }

    [Serializable]
    public sealed class OccPixelUiConfigData
    {
        public string schema;
        public string visualBaseline;
        public int referenceWidth;
        public int referenceHeight;
        public int compactHeightThreshold;
        public int battlefieldWidth;
        public int hudWidth;
        public int logicalPixelScale;
        public OccPixelUiSkinEntry[] skins;
        public OccPixelUiStateEntry[] states;
        public OccPixelUiLayoutEntry[] layouts;
        public OccPixelUiPaletteEntry[] palette;
    }

    public static class OccPixelUiConfig
    {
        public const string ResourcePath = "Config/OccPixelUiV02";
        public const string RequiredSchema = "occ.pixel.ui.v0.2";
        private static OccPixelUiConfigData data;
        private static Dictionary<string, OccPixelUiSkinEntry> skins;
        private static Dictionary<string, OccPixelUiStateEntry> states;
        private static Dictionary<string, OccPixelUiLayoutEntry> layouts;
        private static Dictionary<string, Color> palette;

        public static OccPixelUiConfigData Data
        {
            get
            {
                if (data == null) Load();
                return data;
            }
        }

        public static string SkinPath(string id)
        {
            EnsureLoaded();
            if (!skins.TryGetValue(id, out OccPixelUiSkinEntry entry)) throw new KeyNotFoundException("Missing pixel UI skin mapping: " + id);
            return entry.resourcePath;
        }

        public static string StateSkin(string component, string state)
        {
            EnsureLoaded();
            string key = component + "." + state;
            if (!states.TryGetValue(key, out OccPixelUiStateEntry entry)) throw new KeyNotFoundException("Missing pixel UI state mapping: " + key);
            return entry.skin;
        }

        public static OccPixelUiLayoutEntry Layout(string id)
        {
            EnsureLoaded();
            if (!layouts.TryGetValue(id, out OccPixelUiLayoutEntry entry)) throw new KeyNotFoundException("Missing pixel UI layout: " + id);
            return entry;
        }

        public static Color Palette(string id)
        {
            EnsureLoaded();
            if (!palette.TryGetValue(id, out Color color)) throw new KeyNotFoundException("Missing pixel UI palette entry: " + id);
            return color;
        }

        public static IReadOnlyList<string> Validate()
        {
            OccPixelUiConfigData value = Data;
            var failures = new List<string>();
            if (value.schema != RequiredSchema) failures.Add("schema");
            if (value.referenceWidth != UiLayoutContract.ReferenceWidth || value.referenceHeight != UiLayoutContract.ReferenceHeight) failures.Add("referenceResolution");
            if (value.battlefieldWidth != 1440 || value.hudWidth != 480 || value.battlefieldWidth + value.hudWidth != value.referenceWidth) failures.Add("battlefieldHudSplit");
            if (value.logicalPixelScale < 4) failures.Add("logicalPixelScale");
            AddDuplicateFailures(value.skins.Select(entry => entry.id), "skin", failures);
            AddDuplicateFailures(value.states.Select(entry => entry.component + "." + entry.state), "state", failures);
            AddDuplicateFailures(value.layouts.Select(entry => entry.id), "layout", failures);
            AddDuplicateFailures(value.palette.Select(entry => entry.id), "palette", failures);
            foreach (OccPixelUiStateEntry state in value.states)
                if (!skins.ContainsKey(state.skin)) failures.Add("stateSkin:" + state.component + "." + state.state);
            foreach (OccPixelUiLayoutEntry layout in value.layouts)
                if (layout.width <= 0 || layout.height <= 0) failures.Add("layoutSize:" + layout.id);
            return failures;
        }

        private static void EnsureLoaded()
        {
            if (data == null) Load();
        }

        private static void Load()
        {
            TextAsset asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null) throw new InvalidOperationException("Missing pixel UI config: " + ResourcePath);
            data = JsonUtility.FromJson<OccPixelUiConfigData>(asset.text);
            if (data == null) throw new InvalidOperationException("Invalid pixel UI config JSON: " + ResourcePath);
            data.skins = data.skins ?? Array.Empty<OccPixelUiSkinEntry>();
            data.states = data.states ?? Array.Empty<OccPixelUiStateEntry>();
            data.layouts = data.layouts ?? Array.Empty<OccPixelUiLayoutEntry>();
            data.palette = data.palette ?? Array.Empty<OccPixelUiPaletteEntry>();
            skins = data.skins.ToDictionary(entry => entry.id, StringComparer.Ordinal);
            states = data.states.ToDictionary(entry => entry.component + "." + entry.state, StringComparer.Ordinal);
            layouts = data.layouts.ToDictionary(entry => entry.id, StringComparer.Ordinal);
            palette = new Dictionary<string, Color>(StringComparer.Ordinal);
            foreach (OccPixelUiPaletteEntry entry in data.palette)
            {
                if (!ColorUtility.TryParseHtmlString(entry.hex, out Color color)) throw new InvalidOperationException("Invalid pixel UI palette color: " + entry.id);
                palette.Add(entry.id, color);
            }
        }

        private static void AddDuplicateFailures(IEnumerable<string> values, string kind, ICollection<string> failures)
        {
            foreach (IGrouping<string, string> duplicate in values.GroupBy(value => value, StringComparer.Ordinal).Where(group => group.Count() > 1))
                failures.Add("duplicate:" + kind + ":" + duplicate.Key);
        }
    }

    public static class FormalUiArtRegistry
    {
        private static IReadOnlyList<FormalArtEntry> entries;
        public static IReadOnlyList<FormalArtEntry> Entries => entries ?? (entries = OccPixelUiConfig.Data.skins
            .Select(entry => new FormalArtEntry("ui.skin." + entry.id, entry.id, entry.resourcePath)).ToArray());

        public static string RequiredPath(string runtimeId)
        {
            return FormalArtRegistry.Required(Entries, runtimeId).ResourcePath;
        }
    }
}
