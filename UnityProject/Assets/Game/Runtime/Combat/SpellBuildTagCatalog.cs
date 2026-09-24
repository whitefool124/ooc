using System;
using System.Collections.Generic;

namespace OCC.Combat
{
    /// <summary>
    /// Primary build labels for the 80 implemented personal spells. These labels organize
    /// the encyclopedia and test-arena loadouts; they do not modify combat values.
    /// </summary>
    public static class SpellBuildTagCatalog
    {
        public const string Dash = "突进穿刺";
        public const string Breach = "地块破坏回流";
        public const string Fireground = "燃烧火场";

        private static readonly Dictionary<string, string> Primary = Build();
        private static readonly Dictionary<string, string> MainTerms = BuildTerms(
            "突进:M01 M02 M03 M05 M19 U01 U06 U18|燃烧:M07 M16 M17 M20 R03 R04 R05 R08 R16 R18 U02 U08 U12 U13 U14 U15 U19 U20|破障:M08 M09 M15 R06 R07 R19 U05|火场:M18 R11 R12 R13 R14 R15 R17 R20 U11|裂痕:U04");
        private static readonly Dictionary<string, string> SideTerms = BuildTerms(
            "穿刺:M03|破势:M06 M10 R10 U03 U20|燃烧:R20|震步:U18|火场:U19");

        public static string For(string spellId)
        {
            return !string.IsNullOrEmpty(spellId) && Primary.TryGetValue(spellId, out string tag)
                ? tag : string.Empty;
        }

        public static string MainTerm(string spellId) => TermFor(MainTerms, spellId);
        public static string SideTerm(string spellId) => TermFor(SideTerms, spellId);

        private static string TermFor(Dictionary<string, string> terms, string spellId) =>
            !string.IsNullOrEmpty(spellId) && terms.TryGetValue(spellId, out string value) ? value : string.Empty;

        private static Dictionary<string, string> Build()
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            Add(result, Dash, "M01 M02 M03 M04 M05 M07 M10 M13 M18 M19 M20 M21 M22 M23 M24 M25 M26 U01 U06 U12 U13 U14 U15 U16 U18 U20 U23 U24 U25 U26");
            Add(result, Breach, "M06 M08 M09 M11 M12 M14 M15 R01 R02 R06 R07 R09 R10 R19 R21 R22 R23 R24 R25 R26 U03 U04 U05 U07 U09 U10 U21 U22");
            Add(result, Fireground, "M16 M17 R03 R04 R05 R08 R11 R12 R13 R14 R15 R16 R17 R18 R20 U02 U08 U11 U17 U19");
            Add(result, "通用战术", "U27 U28");
            return result;
        }

        private static void Add(Dictionary<string, string> result, string tag, string suffixes)
        {
            foreach (string suffix in suffixes.Split(' '))
                result.Add("F-P-" + suffix, tag);
        }

        private static Dictionary<string, string> BuildTerms(string groups)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string group in groups.Split('|'))
            {
                string[] pair = group.Split(':');
                Add(result, pair[0], pair[1]);
            }
            return result;
        }
    }
}
