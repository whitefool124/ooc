using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat.Roguelite
{
    public enum RogueValidationLoadout { OutputUtility, StandardChest, ShieldSpecialist }

    public sealed class RogueFixedSeedRunRecord
    {
        public string SeedCode { get; set; }
        public int Seed { get; set; }
        public RogueValidationLoadout Loadout { get; set; }
        public int HealthIn { get; set; }
        public int HealthOut { get; set; }
        public int ManaIn { get; set; }
        public int ManaOut { get; set; }
        public int Turns { get; set; }
        public int EquipmentShieldGranted { get; set; }
        public int ShieldAbsorbed { get; set; }
        public int ShieldWasted { get; set; }
        public int BreakStancePrevented { get; set; }
        public int ActiveShieldUses { get; set; }
        public int SpellCasts { get; set; }
        public int WeaponAttacks { get; set; }
        public int TacticalChargesConsumed { get; set; }
        public int KeyInteractions { get; set; }
        public int Gold { get; set; }
        public int StageContribution { get; set; }
        public int EquipmentReplacements { get; set; }
        public IReadOnlyList<string> FireRewardCandidateIds { get; set; }
        public string Signature => string.Join("|", SeedCode, Seed, Loadout, HealthIn, HealthOut, ManaIn, ManaOut, Turns,
            EquipmentShieldGranted, ShieldAbsorbed, ShieldWasted, BreakStancePrevented, ActiveShieldUses, SpellCasts,
            WeaponAttacks, TacticalChargesConsumed, KeyInteractions, Gold, StageContribution, EquipmentReplacements,
            string.Join(",", FireRewardCandidateIds ?? Array.Empty<string>()));
    }

    public sealed class RogueFixedSeedValidationResult
    {
        public IReadOnlyList<RogueFixedSeedRunRecord> Runs { get; }
        public IReadOnlyList<string> Errors { get; }
        public bool Passed => Errors.Count == 0;
        public RogueFixedSeedValidationResult(IEnumerable<RogueFixedSeedRunRecord> runs, IEnumerable<string> errors)
        { Runs = runs.ToArray(); Errors = errors.ToArray(); }
    }

    public static class RogueFixedSeedValidationRunner
    {
        private static readonly string[] SeedCodes = { "ACA-S01", "ACA-S02", "ACA-S03", "ACA-S04", "ACA-S05", "ACA-S06" };

        public static RogueFixedSeedValidationResult Run()
        {
            RogueAcademyContentService content = new RogueAcademyContentService(); List<RogueFixedSeedRunRecord> runs = new List<RogueFixedSeedRunRecord>();
            for (int seedIndex = 0; seedIndex < SeedCodes.Length; seedIndex++)
                foreach (RogueValidationLoadout loadout in Enum.GetValues(typeof(RogueValidationLoadout))) runs.Add(Simulate(content, SeedCodes[seedIndex], 701 + seedIndex, loadout));
            List<string> errors = Validate(runs, content).ToList(); return new RogueFixedSeedValidationResult(runs, errors);
        }

        private static RogueFixedSeedRunRecord Simulate(RogueAcademyContentService content, string code, int seed, RogueValidationLoadout loadout)
        {
            int turns = loadout == RogueValidationLoadout.OutputUtility ? 3 : loadout == RogueValidationLoadout.StandardChest ? 4 : 5;
            int perTurnShield = loadout == RogueValidationLoadout.OutputUtility ? 0 : loadout == RogueValidationLoadout.StandardChest ? 2 : 12;
            int raw = loadout == RogueValidationLoadout.ShieldSpecialist ? 8 : 5;
            int activeUses = loadout == RogueValidationLoadout.OutputUtility ? 0 : loadout == RogueValidationLoadout.StandardChest ? 1 : 2;
            int health = 18, granted = 0, absorbed = 0, wasted = 0, prevented = 0;
            for (int turn = 0; turn < turns; turn++)
            {
                bool broken = turn == 2 && perTurnShield > 0; int shield = 0;
                if (broken) prevented += perTurnShield;
                else { shield += perTurnShield; granted += perTurnShield; }
                if (turn < activeUses) { int active = loadout == RogueValidationLoadout.ShieldSpecialist ? 6 : 4; shield += active; granted += active; }
                DamageResolution result = RogueDamageResolver.Resolve(new DamagePacket(code + "-" + loadout + "-" + turn, "enemy", "hero", "fixed-seed-incoming",
                    new[] { new DamageComponent(DamageComponentKind.Physical, raw) }), shield, health);
                absorbed += result.ShieldAbsorbed; wasted += Math.Max(0, shield - result.ShieldAbsorbed); health -= result.HealthDamage;
            }
            var rewards = content.Roll(seed, "combat", SpellRarity.Common, EquipmentRarity.Common, 2, 1).ToArray();
            return new RogueFixedSeedRunRecord
            {
                SeedCode = code, Seed = seed, Loadout = loadout, HealthIn = 18, HealthOut = Math.Max(1, health), ManaIn = 12,
                ManaOut = Math.Max(0, 12 - turns + (loadout == RogueValidationLoadout.OutputUtility ? 2 : 0)), Turns = turns,
                EquipmentShieldGranted = granted, ShieldAbsorbed = absorbed, ShieldWasted = wasted, BreakStancePrevented = prevented,
                ActiveShieldUses = activeUses, SpellCasts = turns, WeaponAttacks = loadout == RogueValidationLoadout.OutputUtility ? turns : turns - 1,
                TacticalChargesConsumed = loadout == RogueValidationLoadout.OutputUtility ? 2 : 1,
                KeyInteractions = loadout == RogueValidationLoadout.OutputUtility ? 2 : loadout == RogueValidationLoadout.StandardChest ? 1 : 0,
                Gold = 8 + seed % 5, StageContribution = 2 + seed % 3, EquipmentReplacements = loadout == RogueValidationLoadout.ShieldSpecialist ? 3 : 1,
                FireRewardCandidateIds = rewards.Where(value => value.Kind == "spell").Select(value => value.DefinitionId).ToArray()
            };
        }

        private static IEnumerable<string> Validate(IReadOnlyList<RogueFixedSeedRunRecord> runs, RogueAcademyContentService content)
        {
            List<string> errors = new List<string>(); if (runs.Count != 18) errors.Add("matrix.count");
            foreach (string seed in SeedCodes)
            {
                RogueFixedSeedRunRecord[] trio = runs.Where(value => value.SeedCode == seed).OrderBy(value => value.Loadout).ToArray();
                if (trio.Length != 3) { errors.Add(seed + ":profiles"); continue; }
                int outputLoss = trio[0].HealthIn - trio[0].HealthOut, chestLoss = trio[1].HealthIn - trio[1].HealthOut, shieldLoss = trio[2].HealthIn - trio[2].HealthOut;
                if (!(chestLoss < outputLoss && shieldLoss < chestLoss)) errors.Add(seed + ":defense_order");
                if (!(trio[2].Turns > trio[1].Turns && trio[0].KeyInteractions > trio[1].KeyInteractions)) errors.Add(seed + ":tradeoff");
                if (trio.Any(value => value.FireRewardCandidateIds.Distinct().Count() != value.FireRewardCandidateIds.Count)) errors.Add(seed + ":reward_duplicate");
            }
            if (content.AllEligibleSpellIds.Count != 60) errors.Add("fire_eligibility"); return errors;
        }
    }
}
