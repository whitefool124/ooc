using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OCC.Combat
{
    public enum RogueliteRunProgram { EvergreenAcademy, FirstRunV1, LegacyGrandfathered }
    // Complete = 固定教学段已经收束（旧存档口径）；RandomLayer = 已经交接进随机层；
    // RunSettled = 整轮在首领战后完成单轮结算。Complete 只在读取旧存档时出现，运行期一律推进到 RandomLayer。
    public enum FirstRunLifecycle { OriginPending, Active, EliteDefeat, ShopOpened, Complete, RandomLayer }
    public enum FirstRunOutcome { None, EliteDefeat, EliteVictory }
    public enum FirstRunNodeType { Origin, Combat, Event, Workshop, Medical, Elite, Shop }

    [Flags]
    public enum FirstRunNodeFlags
    {
        None = 0,
        Locked = 1,
        Reachable = 2,
        Recommended = 4,
        Completed = 8,
        Revisitable = 16,
        Used = 32,
        SoldOut = 64,
        Current = 128
    }

    public sealed class FirstRunNodeSnapshot
    {
        public string Id { get; }
        public FirstRunNodeType Type { get; }
        public List<string> NextIds { get; } = new List<string>();
        public FirstRunNodeFlags Flags { get; internal set; }

        public FirstRunNodeSnapshot(string id, FirstRunNodeType type, IEnumerable<string> nextIds, FirstRunNodeFlags flags = FirstRunNodeFlags.None)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            Type = type;
            NextIds.AddRange(nextIds ?? Array.Empty<string>());
            Flags = flags;
        }
    }

    public sealed class FirstRunRewardGroupSnapshot
    {
        public string Id { get; }
        public string NodeId { get; }
        public List<string> CandidateIds { get; } = new List<string>();
        public string SelectedId { get; internal set; } = string.Empty;
        public bool Abandoned { get; internal set; }

        public FirstRunRewardGroupSnapshot(string id, string nodeId, IEnumerable<string> candidateIds)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            NodeId = nodeId ?? throw new ArgumentNullException(nameof(nodeId));
            CandidateIds.AddRange(candidateIds ?? Array.Empty<string>());
        }
    }

    public sealed class FirstRunEventSnapshot
    {
        public string NodeId { get; }
        public List<string> OptionIds { get; } = new List<string>();
        public string SelectedOptionId { get; internal set; } = string.Empty;

        public FirstRunEventSnapshot(string nodeId, IEnumerable<string> optionIds)
        {
            NodeId = nodeId ?? throw new ArgumentNullException(nameof(nodeId));
            OptionIds.AddRange(optionIds ?? Array.Empty<string>());
        }
    }

    public sealed class FirstRunEncounterSnapshot
    {
        public string NodeId { get; }
        public string LevelSlotId { get; }
        public string EnemyScriptId { get; }
        public string DropSlotId { get; }
        public string DialogueSetId { get; }
        public List<string> MechanicSlotIds { get; } = new List<string>();
        public List<string> IdealRouteIds { get; } = new List<string>();
        public List<string> BuildFeedbackTags { get; } = new List<string>();

        public FirstRunEncounterSnapshot(string nodeId, string levelSlotId, string enemyScriptId, string dropSlotId,
            string dialogueSetId, IEnumerable<string> mechanicSlotIds, IEnumerable<string> idealRouteIds,
            IEnumerable<string> buildFeedbackTags)
        {
            NodeId = nodeId ?? throw new ArgumentNullException(nameof(nodeId));
            LevelSlotId = levelSlotId ?? string.Empty;
            EnemyScriptId = enemyScriptId ?? string.Empty;
            DropSlotId = dropSlotId ?? string.Empty;
            DialogueSetId = dialogueSetId ?? string.Empty;
            MechanicSlotIds.AddRange(mechanicSlotIds ?? Array.Empty<string>());
            IdealRouteIds.AddRange(idealRouteIds ?? Array.Empty<string>());
            BuildFeedbackTags.AddRange(buildFeedbackTags ?? Array.Empty<string>());
        }
    }

    public sealed class FirstRunWorkshopSnapshot
    {
        public bool ForgeCompleted { get; internal set; }
        public bool SpecializationCompleted { get; internal set; }
        public string ForgedTargetId { get; internal set; } = string.Empty;
        public string SpecializedTargetId { get; internal set; } = string.Empty;
    }

    public sealed class FirstRunMedicalSnapshot
    {
        public bool HealthCheckCompleted { get; internal set; }
        public bool HealUsed { get; internal set; }
        public bool MealUsed { get; internal set; }
        public string SelectedMealId { get; internal set; } = string.Empty;
        public List<string> MealCandidateIds { get; } = new List<string>();
    }

    public sealed class FirstRunShopOfferSnapshot
    {
        public string OfferId { get; }
        public string DefinitionId { get; }
        public int Price { get; }
        public string CurrencyId { get; }
        public bool Sold { get; internal set; }

        public FirstRunShopOfferSnapshot(string offerId, string definitionId, int price, string currencyId, bool sold = false)
        {
            OfferId = offerId ?? throw new ArgumentNullException(nameof(offerId));
            DefinitionId = definitionId ?? throw new ArgumentNullException(nameof(definitionId));
            Price = price;
            CurrencyId = currencyId ?? string.Empty;
            Sold = sold;
        }
    }

    public sealed class FirstRunShopSnapshot
    {
        public bool Opened { get; internal set; }
        public List<FirstRunShopOfferSnapshot> Offers { get; } = new List<FirstRunShopOfferSnapshot>();
    }

    public sealed class FirstRunOriginSnapshot
    {
        public string StoryId { get; internal set; } = FirstRunExperienceCatalog.OriginStoryId;
        public string TalentId { get; internal set; } = FirstRunExperienceCatalog.OriginTalentId;
        public string SpellId { get; internal set; } = FirstRunExperienceCatalog.OriginSpellId;
        public bool Acknowledged { get; internal set; }
    }

    public sealed class FirstRunExperienceState
    {
        public string ContentVersion { get; internal set; } = FirstRunExperienceCatalog.ContentVersion;
        public FirstRunLifecycle Lifecycle { get; internal set; } = FirstRunLifecycle.OriginPending;
        public FirstRunOutcome Outcome { get; internal set; }
        public string CurrentNodeId { get; internal set; } = FirstRunExperienceCatalog.OriginNodeId;
        public string PendingRewardGroupId { get; internal set; } = string.Empty;
        public bool EliteRewardClaimed { get; internal set; }
        public bool EliteRewardAbandoned { get; internal set; }
        public string EliteSelectedPassiveId { get; internal set; } = string.Empty;
        public bool RunSealed { get; internal set; }
        public bool RoundSettled { get; internal set; }
        public int ForgeMaterialCount { get; internal set; }
        public int SpecializationMaterialCount { get; internal set; }
        public int AcademyFoodCount { get; internal set; }
        public FirstRunOriginSnapshot Origin { get; } = new FirstRunOriginSnapshot();
        public FirstRunWorkshopSnapshot Workshop { get; } = new FirstRunWorkshopSnapshot();
        public FirstRunMedicalSnapshot Medical { get; } = new FirstRunMedicalSnapshot();
        public FirstRunShopSnapshot Shop { get; } = new FirstRunShopSnapshot();
        public List<FirstRunNodeSnapshot> Nodes { get; } = new List<FirstRunNodeSnapshot>();
        public List<FirstRunRewardGroupSnapshot> RewardGroups { get; } = new List<FirstRunRewardGroupSnapshot>();
        public List<FirstRunEventSnapshot> Events { get; } = new List<FirstRunEventSnapshot>();
        public List<FirstRunEncounterSnapshot> Encounters { get; } = new List<FirstRunEncounterSnapshot>();
        public List<string> GrantedRelicSlotIds { get; } = new List<string>();
        public Dictionary<string, string> LootProgress { get; } = new Dictionary<string, string>(StringComparer.Ordinal);
        internal bool RequiresRuntimeBackfill { get; set; }

        public bool IsTerminal => RunSealed || RoundSettled;
        /// <summary>固定教学段已经收束，本局进入固定段之后的随机层。</summary>
        public bool IsRandomLayer => Lifecycle == FirstRunLifecycle.RandomLayer || Lifecycle == FirstRunLifecycle.Complete;
        public FirstRunNodeSnapshot Node(string id) => Nodes.Single(value => value.Id == id);
        public FirstRunRewardGroupSnapshot RewardForNode(string nodeId) => RewardGroups.SingleOrDefault(value => value.NodeId == nodeId);
        public FirstRunEventSnapshot EventForNode(string nodeId) => Events.SingleOrDefault(value => value.NodeId == nodeId);
        public FirstRunEncounterSnapshot EncounterForNode(string nodeId) => Encounters.SingleOrDefault(value => value.NodeId == nodeId);

        public void AcknowledgeOrigin()
        {
            RequireMutable();
            if (Origin.Acknowledged) return;
            Origin.Acknowledged = true;
            Lifecycle = FirstRunLifecycle.Active;
            SetCompleted(FirstRunExperienceCatalog.OriginNodeId);
            RecomputeNodeFlags();
        }

        public IReadOnlyList<string> FindTravelPath(string targetNodeId)
        {
            FirstRunNodeSnapshot target = Nodes.SingleOrDefault(value => value.Id == targetNodeId);
            if (target == null || !CanOccupy(target)) return Array.Empty<string>();
            if (targetNodeId == CurrentNodeId) return new[] { CurrentNodeId };
            Queue<string> open = new Queue<string>();
            Dictionary<string, string> previous = new Dictionary<string, string>(StringComparer.Ordinal);
            open.Enqueue(CurrentNodeId); previous[CurrentNodeId] = null;
            while (open.Count > 0)
            {
                string current = open.Dequeue();
                foreach (string next in Node(current).NextIds)
                {
                    if (previous.ContainsKey(next) || !CanOccupy(Node(next))) continue;
                    previous[next] = current;
                    if (next == targetNodeId)
                    {
                        List<string> path = new List<string>();
                        for (string step = next; step != null; step = previous[step]) path.Add(step);
                        path.Reverse();
                        return path;
                    }
                    open.Enqueue(next);
                }
            }
            return Array.Empty<string>();
        }

        public void TravelTo(string nodeId)
        {
            RequireMutable();
            if (!string.IsNullOrEmpty(PendingRewardGroupId)) throw new InvalidOperationException("A pending reward must be claimed before travel.");
            IReadOnlyList<string> path = FindTravelPath(nodeId);
            if (path.Count == 0) throw new InvalidOperationException("First-run node is locked or has no unlocked route.");
            CurrentNodeId = nodeId;
            if (nodeId == FirstRunExperienceCatalog.ShopNodeId)
            {
                Shop.Opened = true;
                Lifecycle = FirstRunLifecycle.ShopOpened;
            }
            RecomputeNodeFlags();
        }

        public void CompleteCombat(string nodeId)
        {
            RequireCurrent(nodeId);
            FirstRunNodeSnapshot node = Node(nodeId);
            if (node.Type != FirstRunNodeType.Combat && node.Type != FirstRunNodeType.Elite)
                throw new InvalidOperationException("Current first-run node is not combat.");
            if (Has(node, FirstRunNodeFlags.Completed)) throw new InvalidOperationException("Combat was already completed.");
            SetCompleted(nodeId);
            if (node.Type == FirstRunNodeType.Elite)
            {
                Outcome = FirstRunOutcome.EliteVictory;
            }
            else
            {
                FirstRunRewardGroupSnapshot reward = RewardForNode(nodeId) ?? throw new InvalidOperationException("Combat reward snapshot is missing.");
                PendingRewardGroupId = reward.Id;
            }
            RecomputeNodeFlags();
        }

        public void ClaimReward(string candidateId)
        {
            RequireMutable();
            FirstRunRewardGroupSnapshot group = RewardGroups.SingleOrDefault(value => value.Id == PendingRewardGroupId);
            if (group == null || !group.CandidateIds.Contains(candidateId)) throw new InvalidOperationException("Reward candidate is not available.");
            if (!string.IsNullOrEmpty(group.SelectedId)) throw new InvalidOperationException("Reward was already claimed.");
            group.SelectedId = candidateId;
            PendingRewardGroupId = string.Empty;
            RecomputeNodeFlags();
        }

        public void ClaimEliteReward(string passiveId)
        {
            RequireMutable();
            if (Outcome != FirstRunOutcome.EliteVictory || EliteRewardClaimed || EliteRewardAbandoned) throw new InvalidOperationException("Elite reward is not available.");
            if (!FirstRunExperienceCatalog.ElitePassiveRewardIds.Contains(passiveId)) throw new InvalidOperationException("Elite passive reward is not available.");
            EliteSelectedPassiveId = passiveId;
            EliteRewardClaimed = true;
            RecomputeNodeFlags();
        }

        public void AbandonReward()
        {
            RequireMutable();
            if (Outcome == FirstRunOutcome.EliteVictory && !EliteRewardClaimed && !EliteRewardAbandoned)
            {
                EliteRewardAbandoned = true;
                RecomputeNodeFlags();
                return;
            }
            FirstRunRewardGroupSnapshot group = RewardGroups.SingleOrDefault(value => value.Id == PendingRewardGroupId);
            if (group == null || !string.IsNullOrEmpty(group.SelectedId) || group.Abandoned)
                throw new InvalidOperationException("First-run reward is not available.");
            group.Abandoned = true;
            PendingRewardGroupId = string.Empty;
            RecomputeNodeFlags();
        }

        public void ChooseEvent(string nodeId, string optionId)
        {
            RequireCurrent(nodeId);
            FirstRunEventSnapshot snapshot = EventForNode(nodeId) ?? throw new InvalidOperationException("Event snapshot is missing.");
            if (!snapshot.OptionIds.Contains(optionId)) throw new InvalidOperationException("Event option is not available.");
            if (!string.IsNullOrEmpty(snapshot.SelectedOptionId)) throw new InvalidOperationException("Event was already resolved.");
            snapshot.SelectedOptionId = optionId;
            if (nodeId == "EV1")
            {
                ForgeMaterialCount++;
            }
            else if (nodeId == "EV2") AcademyFoodCount++;
            else if (nodeId == "EV3")
            {
                SpecializationMaterialCount++;
            }
            SetCompleted(nodeId);
            RecomputeNodeFlags();
        }

        public void CompleteForge(string targetId)
        {
            RequireCurrent("W");
            if (Workshop.ForgeCompleted || ForgeMaterialCount <= 0) throw new InvalidOperationException("First-run forge is unavailable.");
            Workshop.ForgeCompleted = true;
            Workshop.ForgedTargetId = RequireTarget(targetId);
            ForgeMaterialCount--;
            CompleteWorkshopIfReady();
        }

        public void CompleteSpecialization(string targetId)
        {
            RequireCurrent("W");
            if (Workshop.SpecializationCompleted || SpecializationMaterialCount <= 0) throw new InvalidOperationException("First-run specialization is unavailable.");
            Workshop.SpecializationCompleted = true;
            Workshop.SpecializedTargetId = RequireTarget(targetId);
            SpecializationMaterialCount--;
            CompleteWorkshopIfReady();
        }

        public void CompleteHealthCheck()
        {
            RequireCurrent("M");
            if (Medical.HealthCheckCompleted) return;
            Medical.HealthCheckCompleted = true;
            SetCompleted("M");
            RecomputeNodeFlags();
        }

        public void MarkHealUsed()
        {
            RequireCurrent("M");
            if (!Medical.HealthCheckCompleted || Medical.HealUsed) throw new InvalidOperationException("First-run treatment is unavailable.");
            Medical.HealUsed = true;
            RecomputeNodeFlags();
        }

        public void ChooseMeal(string mealId)
        {
            RequireCurrent("M");
            if (!Medical.HealthCheckCompleted || Medical.MealUsed || !Medical.MealCandidateIds.Contains(mealId))
                throw new InvalidOperationException("First-run meal is unavailable.");
            Medical.MealUsed = true;
            Medical.SelectedMealId = mealId;
            RecomputeNodeFlags();
        }

        public void Purchase(string offerId)
        {
            RequireCurrent(FirstRunExperienceCatalog.ShopNodeId);
            FirstRunShopOfferSnapshot offer = Shop.Offers.SingleOrDefault(value => value.OfferId == offerId);
            if (offer == null || offer.Sold) throw new InvalidOperationException("First-run shop offer is unavailable.");
            offer.Sold = true;
            RecomputeNodeFlags();
        }

        public void CompleteExperience()
        {
            RequireCurrent(FirstRunExperienceCatalog.ShopNodeId);
            if (!Shop.Opened || Outcome != FirstRunOutcome.EliteVictory || (!EliteRewardClaimed && !EliteRewardAbandoned))
                throw new InvalidOperationException("First-run experience cannot be completed before the shop is opened.");
            SetCompleted(FirstRunExperienceCatalog.ShopNodeId);
            // 离开商店不再结束本局：固定教学段到此收束，同一局继续进入随机层。
            Lifecycle = FirstRunLifecycle.RandomLayer;
            RecomputeNodeFlags();
        }

        /// <summary>整轮在首领战结算完成时调用；只有它让 IsTerminal 成立。</summary>
        public void SettleRound()
        {
            if (RunSealed) throw new InvalidOperationException("First-run experience is sealed.");
            RoundSettled = true;
        }

        public void SealEliteDefeat()
        {
            RequireCurrent("X");
            if (Has(Node("X"), FirstRunNodeFlags.Completed)) throw new InvalidOperationException("Elite combat already ended in victory.");
            Outcome = FirstRunOutcome.EliteDefeat;
            Lifecycle = FirstRunLifecycle.EliteDefeat;
            RunSealed = true;
            PendingRewardGroupId = string.Empty;
            RecomputeNodeFlags();
        }

        public void RecomputeNodeFlags()
        {
            foreach (FirstRunNodeSnapshot node in Nodes)
            {
                FirstRunNodeFlags retained = node.Flags & (FirstRunNodeFlags.Completed | FirstRunNodeFlags.Used | FirstRunNodeFlags.SoldOut);
                bool revisitable = node.Type == FirstRunNodeType.Origin || node.Type == FirstRunNodeType.Workshop ||
                    node.Type == FirstRunNodeType.Medical || node.Type == FirstRunNodeType.Shop || Has(node, FirstRunNodeFlags.Completed);
                bool unlocked = IsUnlocked(node.Id);
                node.Flags = retained | (revisitable ? FirstRunNodeFlags.Revisitable : FirstRunNodeFlags.None) |
                    (unlocked ? FirstRunNodeFlags.Reachable : FirstRunNodeFlags.Locked) |
                    (node.Id == CurrentNodeId ? FirstRunNodeFlags.Current : FirstRunNodeFlags.None) |
                    (IsRecommended(node.Id) ? FirstRunNodeFlags.Recommended : FirstRunNodeFlags.None);
            }
            FirstRunNodeSnapshot workshop = Nodes.SingleOrDefault(value => value.Id == "W");
            if (workshop != null && Workshop.ForgeCompleted && Workshop.SpecializationCompleted) workshop.Flags |= FirstRunNodeFlags.Used;
            FirstRunNodeSnapshot medical = Nodes.SingleOrDefault(value => value.Id == "M");
            if (medical != null && Medical.HealthCheckCompleted && Medical.HealUsed && Medical.MealUsed) medical.Flags |= FirstRunNodeFlags.Used;
            FirstRunNodeSnapshot shop = Nodes.SingleOrDefault(value => value.Id == FirstRunExperienceCatalog.ShopNodeId);
            if (shop != null && Shop.Offers.Count > 0 && Shop.Offers.All(value => value.Sold)) shop.Flags |= FirstRunNodeFlags.SoldOut;
        }

        private void CompleteWorkshopIfReady()
        {
            if (Workshop.ForgeCompleted && Workshop.SpecializationCompleted) SetCompleted("W");
            RecomputeNodeFlags();
        }

        private bool IsUnlocked(string id)
        {
            if (RunSealed) return id == CurrentNodeId || Has(Node(id), FirstRunNodeFlags.Completed);
            switch (id)
            {
                case "O": return true;
                case "B1": return Origin.Acknowledged;
                case "EV1":
                case "EV2": return RewardClaimedFor("B1");
                case "B2": return Completed("EV1") && Completed("EV2");
                case "W":
                case "EV3": return RewardClaimedFor("B2");
                case "B3": return Completed("EV3");
                case "M": return RewardClaimedFor("B3");
                // 工坊与医务室是可选服务，不应成为精英战的隐藏必修门槛。
                // 第三场战斗及其奖励结算完毕后，就允许从当前已开放路线进入精英战。
                case "X": return RewardClaimedFor("B3");
                case "S": return Completed("X") && Outcome == FirstRunOutcome.EliteVictory && (EliteRewardClaimed || EliteRewardAbandoned);
                default: return false;
            }
        }

        private bool IsRecommended(string id)
        {
            if (!IsUnlocked(id) || Completed(id)) return false;
            if (id == "B1" || id == "B2" || id == "B3" || id == "S") return true;
            if (id == "EV1" || id == "EV2" || id == "EV3") return true;
            if (id == "W") return !Workshop.ForgeCompleted || !Workshop.SpecializationCompleted;
            if (id == "M") return !Medical.HealthCheckCompleted;
            if (id == "X") return true;
            return false;
        }

        private bool RewardClaimedFor(string nodeId)
        {
            FirstRunRewardGroupSnapshot reward = RewardForNode(nodeId);
            return Completed(nodeId) && reward != null && (!string.IsNullOrEmpty(reward.SelectedId) || reward.Abandoned);
        }

        private bool Completed(string id) => Has(Node(id), FirstRunNodeFlags.Completed);
        private static bool Has(FirstRunNodeSnapshot node, FirstRunNodeFlags flag) => (node.Flags & flag) != 0;
        private static bool CanOccupy(FirstRunNodeSnapshot node) => !Has(node, FirstRunNodeFlags.Locked) || Has(node, FirstRunNodeFlags.Completed);
        private void SetCompleted(string id) => Node(id).Flags |= FirstRunNodeFlags.Completed;
        private void RequireCurrent(string id)
        {
            RequireMutable();
            if (!string.Equals(CurrentNodeId, id, StringComparison.Ordinal)) throw new InvalidOperationException("First-run action is not at the current node.");
        }
        private void RequireMutable()
        {
            if (RunSealed || Lifecycle == FirstRunLifecycle.Complete) throw new InvalidOperationException("First-run experience is sealed.");
        }
        private static string RequireTarget(string id) => !string.IsNullOrWhiteSpace(id) ? id : throw new ArgumentException("A target id is required.", nameof(id));
    }

    public static class FirstRunExperienceCatalog
    {
        public const string ContentVersion = "first-run-v1-character-and-unified-damage";
        public const string OriginNodeId = "O";
        public const string ShopNodeId = "S";
        public const string OriginStoryId = "ORIGIN-STORY-01";
        public const string OriginTalentId = "ORIGIN-TALENT-01";
        public const string OriginSpellId = "ORIGIN-SPELL-01";
        public static readonly IReadOnlyList<string> ElitePassiveRewardIds = new[] { "PASSIVE-ELITE-01", "PASSIVE-ELITE-02", "PASSIVE-ELITE-03" };

        public static readonly IReadOnlyList<RogueliteMapNode> MapNodes = new[]
        {
            new RogueliteMapNode("O", RogueliteMapNodeType.Start, "学生基础配置", "确认维克多的首次体验固定配置。", 0, 2, "B1"),
            new RogueliteMapNode("B1", RogueliteMapNodeType.Combat, "普通战斗 01 · 雨后灯庭", "学院实地对抗课程：击倒寻迹兽与高年级火矢生。", 1, 2, "O", "B2", "EV1", "EV2"),
            new RogueliteMapNode("EV1", RogueliteMapNodeType.Event, "受阻的温室传令", "领取轻装传令衣与承力合金。", 1, 1, "B1"),
            new RogueliteMapNode("EV2", RogueliteMapNodeType.Event, "温室勤务签领", "领取导位罗盘、学院食材与勤务报酬。", 1, 3, "B1"),
            new RogueliteMapNode("B2", RogueliteMapNodeType.Combat, "普通战斗 02 · 温室藏品间", "击倒替身偶与侧锋生，并可搜刮中央备件箱。", 2, 2, "B1", "B3", "W", "EV3"),
            new RogueliteMapNode("W", RogueliteMapNodeType.Workshop, "工坊", "一次锻造与一次术式专精。", 2, 1, "B2"),
            new RogueliteMapNode("EV3", RogueliteMapNodeType.Event, "折光庭校验签领", "领取增幅刻墨，并在学院贡献与截击铃之间选择。", 2, 3, "B2"),
            new RogueliteMapNode("B3", RogueliteMapNodeType.Combat, "普通战斗 03 · 雨痕晶庭", "击倒替身偶与火矢生；可破晶搜刮学院储能芯。", 3, 2, "B2", "X", "M"),
            new RogueliteMapNode("M", RogueliteMapNodeType.Medical, "医务室", "健康确认、治疗与固定餐食。", 3, 3, "B3"),
            new RogueliteMapNode("X", RogueliteMapNodeType.Elite, "精英战斗 01 · 三材承压场", "击倒楔角；失败不会开放商店。", 3, 1, "B3", "S"),
            new RogueliteMapNode("S", RogueliteMapNodeType.Shop, "精英后商店", "首次展开即完成固定教学段。", 4, 1, "X")
        };

        public static RogueliteMapNode MapNode(string id) => MapNodes.Single(value => value.Id == id);

        public static FirstRunExperienceState CreatePhaseA()
        {
            FirstRunExperienceState state = new FirstRunExperienceState();
            AddNode(state, "O", FirstRunNodeType.Origin, "B1");
            AddNode(state, "B1", FirstRunNodeType.Combat, "O", "B2", "EV1", "EV2");
            AddNode(state, "EV1", FirstRunNodeType.Event, "B1");
            AddNode(state, "EV2", FirstRunNodeType.Event, "B1");
            AddNode(state, "B2", FirstRunNodeType.Combat, "B1", "B3", "W", "EV3");
            AddNode(state, "W", FirstRunNodeType.Workshop, "B2");
            AddNode(state, "EV3", FirstRunNodeType.Event, "B2");
            AddNode(state, "B3", FirstRunNodeType.Combat, "B2", "X", "M");
            AddNode(state, "M", FirstRunNodeType.Medical, "B3");
            AddNode(state, "X", FirstRunNodeType.Elite, "B3", "S");
            AddNode(state, "S", FirstRunNodeType.Shop, "X");

            state.RewardGroups.Add(new FirstRunRewardGroupSnapshot("REWARD-GROUP-1", "B1",
                new[] { "F-P-M03", "F-P-R19", "F-P-U07" }));
            state.RewardGroups.Add(new FirstRunRewardGroupSnapshot("REWARD-GROUP-2", "B2",
                new[] { "F-P-U04", "F-P-U01", "F-P-U18" }));
            state.RewardGroups.Add(new FirstRunRewardGroupSnapshot("REWARD-GROUP-3", "B3",
                new[] { "ACA-EQ-MH03", "ACA-EQ-DG02", "F-P-M12" }));
            state.Events.Add(new FirstRunEventSnapshot("EV1", new[] { "FIRST-EV1-ACCEPT-DELIVERY" }));
            state.Events.Add(new FirstRunEventSnapshot("EV2", new[] { "FIRST-EV2-CONTRIBUTION", "FIRST-EV2-GOLD" }));
            state.Events.Add(new FirstRunEventSnapshot("EV3", new[] { "FIRST-EV3-CONTRIBUTION", "FIRST-EV3-REACTION-BELL" }));
            state.Encounters.Add(new FirstRunEncounterSnapshot("B1", RainLanternCourtRuntime.LevelId,
                RainLanternCourtRuntime.EncounterId, "FIRST-B1-DROP-LOCKED", "FIRST-B1-DIALOGUE",
                new[] { "FIRST-B1-WATER", "FIRST-B1-LAMP_VINE", "FIRST-B1-ORIGIN" },
                new[] { "FIRST-B1-ROUTE-VINE", "FIRST-B1-ROUTE-WATER", "FIRST-B1-ROUTE-COVER" },
                Array.Empty<string>()));
            state.Encounters.Add(new FirstRunEncounterSnapshot("B2", FirstRegionLevelCatalog.GreenhouseCollectionRoom.Id,
                "first_b2_greenhouse_collection_room", "FIRST-B2-CENTRAL-CHEST", "FIRST-B2-DIALOGUE",
                new[] { "FIRST-B2-LAMP_VINE", "FIRST-B2-AETHER_CRYSTAL", "FIRST-B2-CRYSTAL_SHARD" },
                new[] { "FIRST-B2-ROUTE-LOOT", "FIRST-B2-ROUTE-DETONATE", "FIRST-B2-ROUTE-SOUTH" },
                new[] { "F-P-M03", "F-P-R19", "F-P-U07", "ACA-EQ-CH04", "G-T09", "ACA-EQ-CR04" }));
            state.Encounters.Add(new FirstRunEncounterSnapshot("B3", FirstRegionLevelCatalog.RainPrismCourt.Id,
                "first_b3_rain_prism_court", "FIRST-B3-CHEST", "FIRST-B3-DIALOGUE",
                new[] { "FIRST-B3-WATER", "FIRST-B3-SEALED-CRYSTAL", "FIRST-B3-BURNING" },
                new[] { "FIRST-B3-ROUTE-BREACH", "FIRST-B3-ROUTE-COOLING", "FIRST-B3-ROUTE-DRY" },
                new[] { "F-P-U04", "F-P-U01", "F-P-U18", "SPEC-AMPLIFY", "G-T10", "ACA-EQ-CR01" }));
            state.Encounters.Add(new FirstRunEncounterSnapshot("X", FirstRegionLevelCatalog.ThreeMaterialPressure.Id,
                "first_elite_three_material_pressure", "FIRST-X-FIXED-REWARD", "FIRST-X-DIALOGUE",
                new[] { "FIRST-X-WATER", "FIRST-X-LAMP-VINE", "FIRST-X-CRYSTAL", "FIRST-X-VENT" },
                new[] { "FIRST-X-ROUTE-BREACH", "FIRST-X-ROUTE-MOMENTUM", "FIRST-X-ROUTE-DRY" },
                new[] { "F-P-U04", "F-P-U01", "F-P-U18", "ACA-EQ-MH03", "ACA-EQ-DG02", "F-P-M12" }));
            state.Medical.MealCandidateIds.AddRange(new[] { "MEAL-POWER", "MEAL-AETHER", "MEAL-GUARD" });
            state.Shop.Offers.Add(new FirstRunShopOfferSnapshot("ECO-SHOP-WEDGE", "G-T08", 3, "gold"));
            state.Shop.Offers.Add(new FirstRunShopOfferSnapshot("ECO-SHOP-SPINDLE", "G-T02", 6, "gold"));
            state.Shop.Offers.Add(new FirstRunShopOfferSnapshot("ECO-SHOP-GOGGLES", "ACA-EQ-HD01", 4, "gold"));
            state.RecomputeNodeFlags();
            if (state.ContentVersion == "first-run-v1-b2-formal") UpgradeB2FormalSnapshot(state);
            return state;
        }

        private static void UpgradeB2FormalSnapshot(FirstRunExperienceState state)
        {
            FirstRunExperienceState canonical = FirstRunExperienceCatalog.CreatePhaseA();
            foreach (string nodeId in new[] { "B2", "B3" })
            {
                FirstRunRewardGroupSnapshot old = state.RewardForNode(nodeId);
                FirstRunRewardGroupSnapshot current = canonical.RewardForNode(nodeId);
                int selected = old == null ? -1 : old.CandidateIds.IndexOf(old.SelectedId);
                if (selected >= 0) current.SelectedId = current.CandidateIds[Math.Min(selected, current.CandidateIds.Count - 1)];
                state.RewardGroups.RemoveAll(value => value.NodeId == nodeId);
                state.RewardGroups.Add(current);
            }
            FirstRunEventSnapshot oldEvent = state.EventForNode("EV3");
            FirstRunEventSnapshot currentEvent = canonical.EventForNode("EV3");
            int option = oldEvent == null ? -1 : oldEvent.OptionIds.IndexOf(oldEvent.SelectedOptionId);
            if (option >= 0) currentEvent.SelectedOptionId = currentEvent.OptionIds[Math.Min(option, currentEvent.OptionIds.Count - 1)];
            state.Events.RemoveAll(value => value.NodeId == "EV3");
            state.Events.Add(currentEvent);
            state.Encounters.RemoveAll(value => value.NodeId == "B3" || value.NodeId == "X");
            state.Encounters.Add(canonical.EncounterForNode("B3"));
            state.Encounters.Add(canonical.EncounterForNode("X"));
            if (state.EliteRewardClaimed && string.IsNullOrEmpty(state.EliteSelectedPassiveId) && !state.EliteRewardAbandoned)
            {
                state.EliteRewardClaimed = false;
                state.Lifecycle = state.Shop.Opened ? FirstRunLifecycle.ShopOpened : FirstRunLifecycle.Active;
            }
            state.ContentVersion = FirstRunExperienceCatalog.ContentVersion;
            state.RequiresRuntimeBackfill = true;
            state.RecomputeNodeFlags();
        }

        private static void AddNode(FirstRunExperienceState state, string id, FirstRunNodeType type, params string[] nextIds) =>
            state.Nodes.Add(new FirstRunNodeSnapshot(id, type, nextIds));

    }

    public static class FirstRunExperienceCodec
    {
        public static string Serialize(FirstRunExperienceState state)
        {
            if (state == null) return string.Empty;
            List<string> lines = new List<string>
            {
                Row("H", state.ContentVersion, state.Lifecycle.ToString(), state.Outcome.ToString(), state.CurrentNodeId,
                    state.PendingRewardGroupId, Bool(state.EliteRewardClaimed), Bool(state.RunSealed), state.ForgeMaterialCount.ToString(),
                    state.SpecializationMaterialCount.ToString(), state.AcademyFoodCount.ToString(), Bool(state.EliteRewardAbandoned),
                    Bool(state.RoundSettled)),
                Row("O", state.Origin.StoryId, state.Origin.TalentId, state.Origin.SpellId, Bool(state.Origin.Acknowledged)),
                Row("W", Bool(state.Workshop.ForgeCompleted), Bool(state.Workshop.SpecializationCompleted), state.Workshop.ForgedTargetId, state.Workshop.SpecializedTargetId),
                Row("M", Bool(state.Medical.HealthCheckCompleted), Bool(state.Medical.HealUsed), Bool(state.Medical.MealUsed), state.Medical.SelectedMealId, List(state.Medical.MealCandidateIds)),
                Row("S", Bool(state.Shop.Opened)),
                Row("L", List(state.GrantedRelicSlotIds)),
                Row("Z", state.EliteSelectedPassiveId)
            };
            lines.AddRange(state.Nodes.OrderBy(value => value.Id, StringComparer.Ordinal).Select(value => Row("N", value.Id, value.Type.ToString(), ((int)value.Flags).ToString(), List(value.NextIds))));
            lines.AddRange(state.RewardGroups.OrderBy(value => value.Id, StringComparer.Ordinal).Select(value => Row("R", value.Id, value.NodeId, value.SelectedId, List(value.CandidateIds), Bool(value.Abandoned))));
            lines.AddRange(state.Events.OrderBy(value => value.NodeId, StringComparer.Ordinal).Select(value => Row("E", value.NodeId, value.SelectedOptionId, List(value.OptionIds))));
            lines.AddRange(state.Encounters.OrderBy(value => value.NodeId, StringComparer.Ordinal).Select(value => Row("C", value.NodeId, value.LevelSlotId, value.EnemyScriptId, value.DropSlotId, value.DialogueSetId, List(value.MechanicSlotIds), List(value.IdealRouteIds), List(value.BuildFeedbackTags))));
            lines.AddRange(state.Shop.Offers.OrderBy(value => value.OfferId, StringComparer.Ordinal).Select(value => Row("P", value.OfferId, value.DefinitionId, value.Price.ToString(), value.CurrencyId, Bool(value.Sold))));
            lines.AddRange(state.LootProgress.OrderBy(value => value.Key, StringComparer.Ordinal).Select(value => Row("Q", value.Key, value.Value)));
            return string.Join("\n", lines);
        }

        public static FirstRunExperienceState Deserialize(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            FirstRunExperienceState state = new FirstRunExperienceState();
            state.Nodes.Clear(); state.RewardGroups.Clear(); state.Events.Clear(); state.Encounters.Clear();
            state.Medical.MealCandidateIds.Clear(); state.Shop.Offers.Clear(); state.GrantedRelicSlotIds.Clear(); state.LootProgress.Clear();
            foreach (string line in raw.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] f = line.Split('\t');
                string kind = f[0];
                if (kind == "H" && (f.Length == 11 || f.Length == 12 || f.Length == 13))
                {
                    state.ContentVersion = U(f[1]); state.Lifecycle = Parse<FirstRunLifecycle>(f[2]); state.Outcome = Parse<FirstRunOutcome>(f[3]);
                    state.CurrentNodeId = U(f[4]); state.PendingRewardGroupId = U(f[5]); state.EliteRewardClaimed = Boolean(f[6]); state.RunSealed = Boolean(f[7]);
                    state.ForgeMaterialCount = Int(f[8]); state.SpecializationMaterialCount = Int(f[9]); state.AcademyFoodCount = Int(f[10]);
                    if (f.Length >= 12) state.EliteRewardAbandoned = Boolean(f[11]);
                    if (f.Length >= 13) state.RoundSettled = Boolean(f[12]);
                }
                else if (kind == "O" && f.Length == 5) { state.Origin.StoryId = U(f[1]); state.Origin.TalentId = U(f[2]); state.Origin.SpellId = U(f[3]); state.Origin.Acknowledged = Boolean(f[4]); }
                else if (kind == "W" && f.Length == 5) { state.Workshop.ForgeCompleted = Boolean(f[1]); state.Workshop.SpecializationCompleted = Boolean(f[2]); state.Workshop.ForgedTargetId = U(f[3]); state.Workshop.SpecializedTargetId = U(f[4]); }
                else if (kind == "M" && f.Length == 6) { state.Medical.HealthCheckCompleted = Boolean(f[1]); state.Medical.HealUsed = Boolean(f[2]); state.Medical.MealUsed = Boolean(f[3]); state.Medical.SelectedMealId = U(f[4]); state.Medical.MealCandidateIds.AddRange(ParseList(f[5])); }
                else if (kind == "S" && f.Length == 2) state.Shop.Opened = Boolean(f[1]);
                else if (kind == "L" && f.Length == 2) state.GrantedRelicSlotIds.AddRange(ParseList(f[1]));
                else if (kind == "Z" && f.Length == 2) state.EliteSelectedPassiveId = U(f[1]);
                else if (kind == "N" && f.Length == 5) state.Nodes.Add(new FirstRunNodeSnapshot(U(f[1]), Parse<FirstRunNodeType>(f[2]), ParseList(f[4]), (FirstRunNodeFlags)Int(f[3])));
                else if (kind == "R" && (f.Length == 5 || f.Length == 6)) { FirstRunRewardGroupSnapshot value = new FirstRunRewardGroupSnapshot(U(f[1]), U(f[2]), ParseList(f[4])); value.SelectedId = U(f[3]); if (f.Length == 6) value.Abandoned = Boolean(f[5]); state.RewardGroups.Add(value); }
                else if (kind == "E" && f.Length == 4) { FirstRunEventSnapshot value = new FirstRunEventSnapshot(U(f[1]), ParseList(f[3])); value.SelectedOptionId = U(f[2]); state.Events.Add(value); }
                else if (kind == "C" && f.Length == 9) state.Encounters.Add(new FirstRunEncounterSnapshot(U(f[1]), U(f[2]), U(f[3]), U(f[4]), U(f[5]), ParseList(f[6]), ParseList(f[7]), ParseList(f[8])));
                else if (kind == "P" && f.Length == 6) state.Shop.Offers.Add(new FirstRunShopOfferSnapshot(U(f[1]), U(f[2]), Int(f[3]), U(f[4]), Boolean(f[5])));
                else if (kind == "Q" && f.Length == 3) state.LootProgress[U(f[1])] = U(f[2]);
                else throw new InvalidOperationException("Invalid first-run snapshot row.");
            }
            return state;
        }

        private static string Row(string kind, params string[] fields) => kind + "\t" + string.Join("\t", fields.Select(B));
        private static string List(IEnumerable<string> values) => string.Join(",", (values ?? Array.Empty<string>()).Select(B));
        private static IEnumerable<string> ParseList(string value) => string.IsNullOrEmpty(U(value)) ? Array.Empty<string>() : U(value).Split(',').Select(U).ToArray();
        private static string Bool(bool value) => value ? "1" : "0";
        private static bool Boolean(string value) => U(value) == "1";
        private static int Int(string value) => int.Parse(U(value));
        private static T Parse<T>(string value) where T : struct => (T)Enum.Parse(typeof(T), U(value));
        private static string B(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? string.Empty));
        private static string U(string value) => Encoding.UTF8.GetString(Convert.FromBase64String(value ?? string.Empty));
    }
}
