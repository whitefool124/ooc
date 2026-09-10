using System;
using System.Linq;

namespace OCC.Combat
{
    public readonly struct RogueliteMapResources
    {
        public int Parts { get; }
        public int Aether { get; }
        public int Supplies { get; }
        public int Scouting { get; }
        public bool UsesRogue11 { get; }
        public int Gold { get; }
        public int StageContribution { get; }
        public int StageTime { get; }
        public int CurrentHealth { get; }
        public int AcademyFood { get; }

        public RogueliteMapResources(int parts, int aether, int supplies, int scouting, bool usesRogue11 = false, int gold = 0, int stageContribution = 0, int stageTime = 0, int currentHealth = 0, int academyFood = 0)
        {
            Parts = parts;
            Aether = aether;
            Supplies = supplies;
            Scouting = scouting;
            UsesRogue11 = usesRogue11; Gold = gold; StageContribution = stageContribution; StageTime = stageTime;
            CurrentHealth = currentHealth; AcademyFood = academyFood;
        }

        public static RogueliteMapResources Capture(RogueliteMapRun run)
        {
            if (run == null) throw new ArgumentNullException(nameof(run));
            return new RogueliteMapResources(run.Parts, run.Aether, run.Supplies,
                run.ScoutingBeacons, run.UsesRogue11, run.Gold, run.StageContribution, run.StageTime,
                run.CurrentHealth, run.FirstRunExperience?.AcademyFoodCount ?? 0);
        }
    }

    public readonly struct RogueliteMapInteractionResult
    {
        public string SubjectId { get; }
        public string PreviousNodeId { get; }
        public bool StartsCombat { get; }
        public bool SafeRevisit { get; }
        public RogueliteMapResources ResourcesBefore { get; }
        public RogueliteMapResources ResourcesAfter { get; }

        public RogueliteMapInteractionResult(string subjectId, string previousNodeId,
            bool startsCombat, bool safeRevisit, RogueliteMapResources resourcesBefore,
            RogueliteMapResources resourcesAfter)
        {
            SubjectId = subjectId ?? string.Empty;
            PreviousNodeId = previousNodeId ?? string.Empty;
            StartsCombat = startsCombat;
            SafeRevisit = safeRevisit;
            ResourcesBefore = resourcesBefore;
            ResourcesAfter = resourcesAfter;
        }
    }

    /// <summary>
    /// Applies map-run interactions and reports the flow and presentation consequences without owning either.
    /// </summary>
    public sealed class RogueliteMapInteractionService
    {
        public RogueliteMapInteractionResult SelectNode(RogueliteMapRun run, string nodeId)
        {
            RequireRun(run);
            RogueliteMapResources before = RogueliteMapResources.Capture(run);
            RogueliteMapNode node = run.MapNode(nodeId);
            bool resumesCurrentCombat = node.Id == run.CurrentNodeId &&
                RogueliteUiPreferences.CanOpenCombatBriefing(run, node);
            bool startsCombat = resumesCurrentCombat || RogueliteUiPreferences.StartsCombat(run, node);
            bool safeRevisit = run.CompletedNodes.Contains(nodeId);
            string previousNodeId = run.CurrentNodeId;
            if (!resumesCurrentCombat) run.SelectNode(nodeId);
            return Result(run, nodeId, previousNodeId, startsCombat, safeRevisit, before);
        }

        public RogueliteMapInteractionResult ChooseContent(RogueliteMapRun run, string choiceId)
        {
            RequireRun(run);
            RogueliteMapResources before = RogueliteMapResources.Capture(run);
            run.ChooseCurrentNodeContent(choiceId);
            return Result(run, choiceId, run.CurrentNodeId, run.HasPendingContentCombat, false, before);
        }

        public RogueliteMapInteractionResult ClaimReward(RogueliteMapRun run, string rewardId)
        {
            RequireRun(run);
            RogueliteMapResources before = RogueliteMapResources.Capture(run);
            run.ClaimReward(rewardId);
            return Result(run, rewardId, run.CurrentNodeId, false, false, before);
        }

        public void ClaimFireSpell(RogueliteMapRun run, string spellId)
        {
            RequireRun(run);
            run.ClaimFireSpell(spellId);
        }

        public void EquipFireSpell(RogueliteMapRun run, string spellId, int slot)
        {
            RequireRun(run);
            run.EquipFireSpell(spellId, slot);
        }

        public bool TryEquipNextFireSpell(RogueliteMapRun run, int slot)
        {
            if (run == null || run.OwnedFireSpellIds.Count == 0 ||
                slot < 0 || slot >= run.EquippedFireSpellIds.Count) return false;
            string current = run.EquippedFireSpellIds[slot];
            int currentIndex = -1;
            for (int index = 0; index < run.OwnedFireSpellIds.Count; index++)
                if (string.Equals(run.OwnedFireSpellIds[index], current, StringComparison.Ordinal))
                {
                    currentIndex = index;
                    break;
                }
            for (int offset = 1; offset <= run.OwnedFireSpellIds.Count; offset++)
            {
                string candidate = run.OwnedFireSpellIds[(Math.Max(-1, currentIndex) + offset) % run.OwnedFireSpellIds.Count];
                if (run.EquippedFireSpellIds.Where((id, index) => index != slot).Contains(candidate)) continue;
                if (!FireSpellCatalog.IsWeaponCompatible(FireSpellCatalog.Get(candidate), run.EquippedWeapon)) continue;
                run.EquipFireSpell(candidate, slot);
                return true;
            }
            return false;
        }

        public void EquipReward(RogueliteMapRun run, string rewardId)
        {
            RequireRun(run);
            run.EquipReward(rewardId);
        }

        public RogueliteMapInteractionResult CalibrateAether(RogueliteMapRun run)
        {
            RequireRun(run);
            RogueliteMapResources before = RogueliteMapResources.Capture(run);
            run.CalibrateAether();
            return Result(run, "aether_calibration", run.CurrentNodeId, false, false, before);
        }

        public RogueliteMapInteractionResult AcknowledgeFirstRunOrigin(RogueliteMapRun run)
        {
            RequireRun(run); RogueliteMapResources before = RogueliteMapResources.Capture(run);
            run.AcknowledgeFirstRunOrigin();
            return Result(run, FirstRunExperienceCatalog.OriginNodeId, run.CurrentNodeId, false, false, before);
        }

        public RogueliteMapInteractionResult CompleteFirstRunForge(RogueliteMapRun run, string targetId)
        {
            RequireRun(run); RogueliteMapResources before = RogueliteMapResources.Capture(run);
            run.CompleteFirstRunForge(targetId);
            return Result(run, targetId, run.CurrentNodeId, false, false, before);
        }

        public RogueliteMapInteractionResult CompleteFirstRunSpecialization(RogueliteMapRun run, string targetId)
        {
            RequireRun(run); RogueliteMapResources before = RogueliteMapResources.Capture(run);
            run.CompleteFirstRunSpecialization(targetId);
            return Result(run, targetId, run.CurrentNodeId, false, false, before);
        }

        public RogueliteMapInteractionResult CompleteFirstRunHealthCheck(RogueliteMapRun run)
        {
            RequireRun(run); RogueliteMapResources before = RogueliteMapResources.Capture(run);
            run.CompleteFirstRunHealthCheck();
            return Result(run, "MEDICAL-CHECK", run.CurrentNodeId, false, false, before);
        }

        public RogueliteMapInteractionResult UseFirstRunHeal(RogueliteMapRun run)
        {
            RequireRun(run); RogueliteMapResources before = RogueliteMapResources.Capture(run);
            run.UseFirstRunHeal();
            return Result(run, "MEDICAL-HEAL", run.CurrentNodeId, false, false, before);
        }

        public RogueliteMapInteractionResult ChooseFirstRunMeal(RogueliteMapRun run, string mealId)
        {
            RequireRun(run); RogueliteMapResources before = RogueliteMapResources.Capture(run);
            run.ChooseFirstRunMeal(mealId);
            return Result(run, mealId, run.CurrentNodeId, false, false, before);
        }

        public RogueliteMapInteractionResult PurchaseFirstRunOffer(RogueliteMapRun run, string offerId)
        {
            RequireRun(run); RogueliteMapResources before = RogueliteMapResources.Capture(run);
            run.PurchaseFirstRunOffer(offerId);
            return Result(run, offerId, run.CurrentNodeId, false, false, before);
        }

        public RogueliteMapInteractionResult CompleteFirstRunExperience(RogueliteMapRun run)
        {
            RequireRun(run); RogueliteMapResources before = RogueliteMapResources.Capture(run);
            run.CompleteFirstRunExperience();
            return Result(run, FirstRunExperienceCatalog.ShopNodeId, run.CurrentNodeId, false, false, before);
        }

        public RogueliteMapInteractionResult AbandonReward(RogueliteMapRun run)
        {
            RequireRun(run); RogueliteMapResources before = RogueliteMapResources.Capture(run);
            run.AbandonCurrentReward();
            return Result(run, "abandoned-reward", run.CurrentNodeId, false, false, before);
        }

        private static RogueliteMapInteractionResult Result(RogueliteMapRun run, string subjectId,
            string previousNodeId, bool startsCombat, bool safeRevisit, RogueliteMapResources before) =>
            new RogueliteMapInteractionResult(subjectId, previousNodeId, startsCombat, safeRevisit,
                before, RogueliteMapResources.Capture(run));

        private static void RequireRun(RogueliteMapRun run)
        {
            if (run == null) throw new ArgumentNullException(nameof(run));
        }
    }
}
