using System;
using System.Globalization;
using System.Linq;

namespace OCC.Combat
{
    public sealed class RogueliteUiPreferences
    {
        public const string Version = "ui1";

        public float MasterVolume { get; private set; } = 1f;
        public float AnimationIntensity { get; private set; } = 1f;
        public bool ScreenShake { get; private set; } = true;
        public bool FloatingText { get; private set; } = true;
        public bool HighContrast { get; private set; }
        public bool LargeText { get; private set; }
        public bool KeyHints { get; private set; } = true;

        public RogueliteUiPreferences Configure(float masterVolume, float animationIntensity, bool screenShake, bool floatingText, bool highContrast, bool largeText, bool keyHints)
        {
            MasterVolume = Clamp01(masterVolume);
            AnimationIntensity = Clamp01(animationIntensity);
            ScreenShake = screenShake;
            FloatingText = floatingText;
            HighContrast = highContrast;
            LargeText = largeText;
            KeyHints = keyHints;
            return this;
        }

        public string ToDataString()
        {
            return string.Join("|", Version,
                MasterVolume.ToString("0.00", CultureInfo.InvariantCulture),
                AnimationIntensity.ToString("0.00", CultureInfo.InvariantCulture),
                Flag(ScreenShake), Flag(FloatingText), Flag(HighContrast), Flag(LargeText), Flag(KeyHints));
        }

        public static RogueliteUiPreferences FromDataString(string data)
        {
            string[] parts = (data ?? string.Empty).Split('|');
            if (parts.Length != 8 || parts[0] != Version) return new RogueliteUiPreferences();
            if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float volume) ||
                !float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float animation))
                return new RogueliteUiPreferences();
            return new RogueliteUiPreferences().Configure(volume, animation, IsSet(parts[3]), IsSet(parts[4]), IsSet(parts[5]), IsSet(parts[6]), IsSet(parts[7]));
        }

        public static bool CanTravelTo(RogueliteMapRun run, RogueliteMapNode node)
        {
            return run != null && node != null && node.Id != run.CurrentNodeId && run.IsNodeAvailable(node.Id);
        }

        public static bool StartsCombat(RogueliteMapRun run, RogueliteMapNode node)
        {
            return CanTravelTo(run, node) && node.IsCombat && !run.CompletedNodes.Contains(node.Id);
        }

        public static bool CanOpenCombatBriefing(RogueliteMapRun run, RogueliteMapNode node)
        {
            return run != null && node != null && node.IsCombat && !run.CompletedNodes.Contains(node.Id) &&
                (node.Id == run.CurrentNodeId || CanTravelTo(run, node));
        }

        private static string Flag(bool value) => value ? "1" : "0";
        private static bool IsSet(string value) => value == "1";
        private static float Clamp01(float value) => Math.Max(0f, Math.Min(1f, value));
    }
}
