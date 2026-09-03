using System;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    public static class CombatHudTypography
    {
        public const int ResourceValueFontSize = FormalUiTheme.BodyFontSize;
        public const int TimelineNameFontSize = FormalUiTheme.BodyFontSize;
        public const int TimelineDetailFontSize = FormalUiTheme.BodyFontSize;
        public const int CommandLabelFontSize = FormalUiTheme.BodyFontSize;
        public const int CostValueFontSize = FormalUiTheme.BodyFontSize;
        public const int MaximumDecisionLineLength = 21;

        public static TextAnchor ResourceValueAlignment => TextAnchor.MiddleRight;

        public static string CompactDecisionSummary(string summary, string damageBreakdown)
        {
            if (string.IsNullOrWhiteSpace(summary)) return "等待指令";
            string[] source = summary.Replace("\r", string.Empty).Split('\n');
            string first = Compact(source[0], MaximumDecisionLineLength);
            string second = source.Length > 1 ? source[1] : string.Empty;
            if (second.StartsWith("目标 · ", StringComparison.Ordinal)) second = second.Substring(5);
            int forecast = second.IndexOf(" · 预计 ", StringComparison.Ordinal);
            if (forecast >= 0) second = second.Substring(0, forecast);
            string damage = FinalHealthDamage(damageBreakdown);
            if (!string.IsNullOrEmpty(damage)) second += (string.IsNullOrEmpty(second) ? string.Empty : " · ") + damage;
            second = Compact(second, MaximumDecisionLineLength);
            return string.IsNullOrEmpty(second) ? first : first + "\n" + second;
        }

        public static string PlayerEventLine(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            int from = value.IndexOf('从');
            int gained = value.IndexOf("获得", StringComparison.Ordinal);
            if (from >= 0 && gained > from)
            {
                string source = value.Substring(from + 1, gained - from - 1);
                if (source.IndexOf(':') >= 0 || source.IndexOf('-') >= 0 || source.IndexOf('_') >= 0)
                    return value.Substring(0, from) + value.Substring(gained);
            }
            return value;
        }

        private static string FinalHealthDamage(string breakdown)
        {
            if (string.IsNullOrWhiteSpace(breakdown)) return string.Empty;
            const string marker = "生命伤害 ";
            int start = breakdown.LastIndexOf(marker, StringComparison.Ordinal);
            if (start < 0) return string.Empty;
            start += marker.Length;
            int end = start;
            while (end < breakdown.Length && char.IsDigit(breakdown[end])) end++;
            return end > start ? "生命伤害 " + breakdown.Substring(start, end - start) : string.Empty;
        }

        private static string Compact(string value, int maximumLength)
        {
            value = value?.Trim() ?? string.Empty;
            return value.Length <= maximumLength ? value : value.Substring(0, maximumLength - 1) + "…";
        }
    }
}
