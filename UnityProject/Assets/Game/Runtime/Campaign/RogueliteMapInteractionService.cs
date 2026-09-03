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
        public int AccessCards { get; }
        public bool UsesRogue11 { get; }
        public int Gold { get; }
        public int StageContribution { get; }
        public int StageTime { get; }

        public RogueliteMapResources(int parts, int aether, int supplies, int scouting, int accessCards, bool usesRogue11 = false, int gold = 0, int stageContribution = 0, int stageTime = 0)
        {
            Parts = parts;
            Aether = aether;
            Supplies = supplies;
            Scouting = scouting;
            AccessCards = accessCards;
            UsesRogue11 = usesRogue11; Gold = gold; StageContribution = stageContribution; StageTime = stageTime;
        }

        public static RogueliteMapResources Capture(RogueliteMapRun run)
        {
            if (run == null) throw new ArgumentNullException(nameof(run));
            return new RogueliteMapResources(run.Parts, run.Aether, run.Supplies,
                run.ScoutingBeacons, run.AccessCards, run.UsesRogue11, run.Gold, run.StageContribution, run.StageTime);
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
            RogueliteMapNode node = RogueliteMapCatalog.Node(nodeId);
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
