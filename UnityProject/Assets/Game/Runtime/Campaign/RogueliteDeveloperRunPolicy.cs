using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    public sealed class RogueliteDeveloperAdvanceReport
    {
        public List<string> Steps { get; } = new List<string>();
        public int NodesResolved { get; set; }
        public string StoppedAt { get; set; } = string.Empty;
        public string StopReason { get; set; } = string.Empty;
        public bool ReachedFinale { get; set; }
        public bool RoundSettled { get; set; }
        public string Summary =>
            "推进 " + NodesResolved + " 个节点；停在 " + (string.IsNullOrEmpty(StoppedAt) ? "—" : StoppedAt) + "；" + StopReason +
            (RoundSettled ? "；本轮已结算" : string.Empty);
    }

    /// <summary>
    /// 开发线专用的“一键过关 / 连续推进到首领”。
    /// 它只调用真实的节点、内容与结算接口，不写死结果，也不出现在正式玩家 UI 中。
    /// </summary>
    public static class RogueliteDeveloperRunPolicy
    {
        /// <summary>当前节点已经处理完但奖励还挂着时，直接结算它。</summary>
        public static bool TrySettlePendingReward(RogueliteMapRun run, RogueliteDeveloperAdvanceReport report)
        {
            if (run == null || !run.AwaitingReward) return false;
            string rewardId = run.CurrentRewards.Select(value => value.Id).FirstOrDefault(id => !run.ClaimedRewards.Contains(id));
            if (string.IsNullOrEmpty(rewardId))
            {
                run.AbandonCurrentReward();
                report?.Steps.Add("放弃悬空奖励 @" + run.CurrentNodeId);
                return true;
            }
            run.ClaimReward(rewardId);
            report?.Steps.Add("领取奖励 " + rewardId + " @" + run.CurrentNodeId);
            return true;
        }

        /// <summary>
        /// 把当前节点结算掉：战斗直接胜利、事件取可行选项、设施走一次服务。
        /// 已经清理过的节点不会被重复结算。
        /// </summary>
        public static bool TryResolveCurrentNode(RogueliteMapRun run, RogueliteDeveloperAdvanceReport report,
            bool settlePendingReward = true)
        {
            if (run == null) throw new ArgumentNullException(nameof(run));
            string nodeId = run.CurrentNodeId;
            if (run.CompletedNodes.Contains(nodeId) && !run.HasPendingContentCombat)
            {
                if (settlePendingReward) TrySettlePendingReward(run, report);
                return false;
            }
            RogueliteMapNode node = run.MapNode(nodeId);
            if (run.HasPendingContentCombat)
            {
                string mission = run.PendingContentCombatMissionId;
                run.CompletePendingContentCombat();
                report?.Steps.Add("完成事件战 " + mission + " @" + nodeId);
                if (settlePendingReward) TrySettlePendingReward(run, report);
                return true;
            }
            if (node.IsCombat)
            {
                RogueliteEncounterDefinition encounter = RogueliteEncounterCatalog.For(run, nodeId);
                run.CompleteCurrentCombat();
                report?.Steps.Add("战斗胜利 " + encounter.VariantKey + " @" + nodeId);
                if (settlePendingReward) TrySettlePendingReward(run, report);
                return true;
            }
            return ResolveContentNode(run, node, report, settlePendingReward);
        }

        private static bool ResolveContentNode(RogueliteMapRun run, RogueliteMapNode node,
            RogueliteDeveloperAdvanceReport report, bool settlePendingReward = true)
        {
            IReadOnlyList<RogueliteNodeContentChoice> choices = run.CurrentContentChoices;
            if (choices.Count == 0)
            {
                run.CompleteCurrentNode();
                report?.Steps.Add("直接结算 " + node.Id);
                return true;
            }
            RogueliteNodeContentChoice choice = choices.FirstOrDefault(value => IsAffordable(run, value)) ?? choices[0];
            run.ChooseCurrentNodeContent(choice.Id);
            report?.Steps.Add("选择 " + choice.Id + " @" + node.Id);
            if (run.HasPendingContentCombat) return true;
            if (settlePendingReward) TrySettlePendingReward(run, report);
            return true;
        }

        private static bool IsAffordable(RogueliteMapRun run, RogueliteNodeContentChoice choice)
        {
            if (run.UsesRogue11)
            {
                if (run.Gold < choice.GoldCost || run.StageContribution < choice.ContributionCost) return false;
                if (choice.HealthGain < 0 && run.CurrentHealth + choice.HealthGain <= 0) return false;
                if (!string.IsNullOrEmpty(choice.RewardId) && run.ClaimedRewards.Contains(choice.RewardId)) return false;
                return true;
            }
            return run.Parts >= choice.PartsCost && run.Aether >= choice.AetherCost;
        }

        /// <summary>
        /// 连续推进到首领：每步只走一格真实相邻节点；终考门槛一开就直接收束到最后一段，
        /// 然后停在首领战入口，由“一键过关”结算。
        /// </summary>
        public static RogueliteDeveloperAdvanceReport AdvanceToFinale(RogueliteMapRun run, int maximumSteps = 64)
        {
            if (run == null) throw new ArgumentNullException(nameof(run));
            RogueliteDeveloperAdvanceReport report = new RogueliteDeveloperAdvanceReport();
            TrySettlePendingReward(run, report);
            for (int step = 0; step < maximumSteps; step++)
            {
                if (run.IsComplete)
                {
                    report.StoppedAt = run.CurrentNodeId;
                    report.StopReason = "本轮已经结算";
                    report.RoundSettled = true;
                    return report;
                }
                if (run.CanChallengeAcademyFinale)
                {
                    report.Steps.AddRange(TravelTo(run, RogueliteAcademyLayerCatalog.FinaleNodeId));
                    report.Steps.Add("进入终考 @" + RogueliteAcademyLayerCatalog.FinaleNodeId);
                    report.ReachedFinale = run.CurrentNodeId == RogueliteAcademyLayerCatalog.FinaleNodeId;
                    report.StoppedAt = run.CurrentNodeId;
                    report.StopReason = report.ReachedFinale ? "已经进入首领战，按“一键过关”结算" : "已经走到终考门口";
                    return report;
                }
                RogueliteMapNode next = NextNodeTowardsFinale(run);
                if (next == null)
                {
                    report.StoppedAt = run.CurrentNodeId;
                    report.StopReason = "没有可继续推进的相邻节点";
                    return report;
                }
                try
                {
                    if (run.CompletedNodes.Contains(next.Id))
                    {
                        report.Steps.AddRange(TravelTo(run, next.Id));
                        report.Steps.Add("经过 " + next.Id);
                    }
                    else
                    {
                        run.SelectNode(next.Id);
                        if (!TryResolveCurrentNode(run, report)) report.Steps.Add("跳过 " + next.Id);
                        report.NodesResolved++;
                    }
                }
                catch (InvalidOperationException error)
                {
                    report.StoppedAt = next.Id;
                    report.StopReason = "推进中断：" + error.Message;
                    return report;
                }
            }
            report.StoppedAt = run.CurrentNodeId;
            report.StopReason = "达到单次推进步数上限";
            if (run.CanChallengeAcademyFinale)
            {
                report.Steps.AddRange(TravelTo(run, RogueliteAcademyLayerCatalog.FinaleNodeId));
                report.Steps.Add("进入终考 @" + RogueliteAcademyLayerCatalog.FinaleNodeId);
                report.ReachedFinale = run.CurrentNodeId == RogueliteAcademyLayerCatalog.FinaleNodeId;
                report.StoppedAt = run.CurrentNodeId;
                report.StopReason = report.ReachedFinale ? "已经进入首领战，按“一键过关”结算" : "已经走到终考门口";
            }
            return report;
        }

        /// <summary>
        /// 终考未开放时的推进目标：距离终点最近的、还没清理过的学院层节点。
        /// 已经清理过的节点可以重复经过，但不会再次结算。
        /// </summary>
        public static RogueliteMapNode NextNodeTowardsFinale(RogueliteMapRun run)
        {
            Dictionary<string, int> distance = DistancesToFinale();
            return run.AvailableNodes
                .Where(node => node.Type != RogueliteMapNodeType.Finale && !run.CompletedNodes.Contains(node.Id))
                .OrderBy(node => distance.TryGetValue(node.Id, out int value) ? value : int.MaxValue)
                .ThenBy(node => node.IsCombat ? 0 : 1)
                .ThenBy(node => node.Id, StringComparer.Ordinal)
                .FirstOrDefault();
        }

        /// <summary>在完整学院层图上算每个节点到终点的最短距离（忽略完成状态）。</summary>
        private static Dictionary<string, int> DistancesToFinale()
        {
            Dictionary<string, int> distance = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                [RogueliteAcademyLayerCatalog.FinaleNodeId] = 0
            };
            Queue<string> open = new Queue<string>();
            open.Enqueue(RogueliteAcademyLayerCatalog.FinaleNodeId);
            while (open.Count > 0)
            {
                string current = open.Dequeue();
                foreach (string next in RogueliteMapCatalog.Node(current).NextIds)
                {
                    if (!RogueliteAcademyLayerCatalog.IsAcademyLayerNode(next)) continue;
                    if (distance.ContainsKey(next)) continue;
                    distance[next] = distance[current] + 1;
                    open.Enqueue(next);
                }
            }
            return distance;
        }

        /// <summary>
        /// 沿最短路径走到目标节点。每走一步都按剩余路径推进，允许重复经过已经清理过的节点。
        /// </summary>
        public static IReadOnlyList<string> TravelTo(RogueliteMapRun run, string targetNodeId)
        {
            List<string> steps = new List<string>();
            if (run.CurrentNodeId == targetNodeId) return steps;
            int guard = 0;
            while (run.CurrentNodeId != targetNodeId && guard++ < 128)
            {
                IReadOnlyList<string> path = PathTo(run, targetNodeId);
                if (path.Count < 2)
                    throw new InvalidOperationException("No reachable path to " + targetNodeId + " from " + run.CurrentNodeId + ".");
                string step = path[1];
                bool revisit = run.CompletedNodes.Contains(step) && !run.HasPendingContentCombat;
                try
                {
                    if (revisit) run.TravelToVisitedAcademyNode(step);
                    else run.SelectNode(step);
                }
                catch (InvalidOperationException error)
                {
                    throw new InvalidOperationException("Cannot step into " + step + ": " + error.Message);
                }
                if (revisit && step != targetNodeId) steps.Add("经过 " + step);
            }
            return steps;
        }

        private static IReadOnlyList<string> PathTo(RogueliteMapRun run, string targetNodeId)
        {
            Queue<string> open = new Queue<string>();
            Dictionary<string, string> previous = new Dictionary<string, string>(StringComparer.Ordinal) { [run.CurrentNodeId] = null };
            open.Enqueue(run.CurrentNodeId);
            while (open.Count > 0)
            {
                string current = open.Dequeue();
                if (current == targetNodeId) break;
                foreach (string candidate in RogueliteMapCatalog.Node(current).NextIds)
                {
                    if (previous.ContainsKey(candidate)) continue;
                    if (!RogueliteAcademyLayerCatalog.IsAcademyLayerNode(candidate)) continue;
                    // 中间步必须是现在真的能走进去的：已经清理过（可以回走），或当前合法的推进目标。
                    // 目标节点允许作为终点直接踏入，终考开门后就是这种情况。
                    if (candidate != targetNodeId && !run.CompletedNodes.Contains(candidate) && !run.IsAcademyLayerNodeAvailable(candidate)) continue;
                    previous[candidate] = current;
                    open.Enqueue(candidate);
                }
            }
            if (!previous.ContainsKey(targetNodeId)) return Array.Empty<string>();
            List<string> path = new List<string>();
            for (string step = targetNodeId; step != null; step = previous[step]) path.Add(step);
            path.Reverse();
            return path;
        }

        /// <summary>一键过关：直接打赢当前战斗并结算掉落。settlePendingReward=false 时停在战后奖励界面，等玩家自己挑。</summary>
        public static RogueliteDeveloperAdvanceReport ForceWinCurrentCombat(RogueliteMapRun run, bool settlePendingReward = true)
        {
            if (run == null) throw new ArgumentNullException(nameof(run));
            RogueliteDeveloperAdvanceReport report = new RogueliteDeveloperAdvanceReport();
            if (run.HasPendingContentCombat || run.MapNode(run.CurrentNodeId).IsCombat)
            {
                if (TryResolveCurrentNode(run, report, settlePendingReward)) report.NodesResolved = 1;
                report.StoppedAt = run.CurrentNodeId;
                report.StopReason = settlePendingReward ? "当前战斗已按胜利结算" : "当前战斗已按胜利结算，战后奖励待处理";
                if (run.CurrentNodeId == RogueliteAcademyLayerCatalog.FinaleNodeId) report.ReachedFinale = true;
                report.RoundSettled = run.IsComplete;
                return report;
            }
            report.StoppedAt = run.CurrentNodeId;
            report.StopReason = "当前节点不是战斗节点";
            return report;
        }
    }
}
