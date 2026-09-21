using System;
using System.Linq;
using System.Text;
using NUnit.Framework;
using OCC.Combat.Roguelite;

namespace OCC.Combat.Tests
{
    public sealed class AcademyMapProgressCleanupTests
    {
        private sealed class Store : IRogueliteSaveStore
        {
            public readonly System.Collections.Generic.Dictionary<string, string> Values = new System.Collections.Generic.Dictionary<string, string>();
            public bool HasKey(string key) => Values.ContainsKey(key);
            public string GetString(string key, string fallback = "") => Values.TryGetValue(key, out string value) ? value : fallback;
            public void SetString(string key, string value) => Values[key] = value;
            public void DeleteKey(string key) => Values.Remove(key);
            public void Flush() { }
        }

        [Test]
        public void ConnectedTowerAndVaultAreAvailableWithoutCollectingAnything()
        {
            RogueRunDto dto = RogueRunDto.CreateNew("connected-room", 961);
            dto.CurrentNodeId = "elite_foundry";
            dto.VisitedNodeIds.Add("elite_foundry");
            RogueliteMapRun run = RogueliteMapRun.FromRogue11(dto);

            Assert.That(run.CompletedNodes, Is.Empty);
            Assert.That(run.IsNodeAvailable("transmission_tower"), Is.True);
            run.SelectNode("transmission_tower");
            run.SelectNode("aether_refinery");
            Assert.That(run.IsNodeAvailable("core_vault"), Is.True);
            run.SelectNode("core_vault");
            Assert.That(run.IsNodeAvailable("core_finale"), Is.False);
        }

        [Test]
        public void EarlyFinaleAndHudUseCompletedNodesRatherThanVisits()
        {
            RogueRunDto dto = RogueRunDto.CreateNew("completed-gate", 962);
            dto.CurrentNodeId = "core_vault";
            string[] nodes = RogueliteMapCatalog.Nodes.Where(n => n.Type != RogueliteMapNodeType.Start && n.Type != RogueliteMapNodeType.Finale)
                .Take(12).Select(n => n.Id).ToArray();
            dto.VisitedNodeIds.AddRange(nodes);
            if (!dto.VisitedNodeIds.Contains("core_vault")) dto.VisitedNodeIds.Add("core_vault");
            // 走进 11 个节点但只完成 10 个：终考门槛按“已完成”而不是“已访问”计算。
            dto.CompletedNodeIds.AddRange(nodes.Take(AcademyMapTuning.BossMinimumProgress));
            RogueliteMapRun run = RogueliteMapRun.FromRogue11(dto);

            Assert.That(run.AcademyProgress, Is.GreaterThanOrEqualTo(12));
            Assert.That(run.CanChallengeAcademyFinale, Is.True);
            Assert.That(run.IsNodeAvailable("core_finale"), Is.True);
            RogueMapStatusPresentation status = new RogueMapStatusPresentation(run);
            Assert.That(status.CompletedNodes, Is.EqualTo(AcademyMapTuning.BossMinimumProgress));
            Assert.That(status.EarlyFinaleReady, Is.True);
            Assert.That(RogueliteMapVisualPresentation.RestrictionText(run, run.MapNode("core_finale")),
                Is.EqualTo("可以直接前往"));

            // 只访问、不完成就不会开门。
            dto.CompletedNodeIds.Clear();
            dto.CompletedNodeIds.AddRange(nodes.Take(AcademyMapTuning.BossMinimumProgress - 1));
            run = RogueliteMapRun.FromRogue11(dto);
            Assert.That(run.AcademyProgress, Is.GreaterThanOrEqualTo(12));
            Assert.That(run.CanChallengeAcademyFinale, Is.False);
            Assert.That(new RogueMapStatusPresentation(run).EarlyFinaleReady, Is.False);

            dto.CompletedNodeIds.Add(nodes[AcademyMapTuning.BossMinimumProgress - 1]);
            run = RogueliteMapRun.FromRogue11(dto);
            Assert.That(run.IsNodeAvailable("core_finale"), Is.True);
            Assert.That(new RogueMapStatusPresentation(run).EarlyFinaleReady, Is.True);
        }

        [Test]
        public void ScheduledFinaleStillOpensAtTimeTwentyEight()
        {
            RogueRunDto dto = RogueRunDto.CreateNew("scheduled-finale", 963);
            dto.CurrentNodeId = "core_vault";
            dto.VisitedNodeIds.Add("core_vault");
            dto.StageTime = 28;
            RogueliteMapRun run = RogueliteMapRun.FromRogue11(dto);
            Assert.That(run.CompletedNodes, Is.Empty);
            Assert.That(run.IsNodeAvailable("core_finale"), Is.True);
            Assert.That(new RogueMapStatusPresentation(run).ForcedFinaleReady, Is.True);
        }

        [Test]
        public void FirstRunStillUsesItsOwnElitePrerequisites()
        {
            RogueliteMapRun run = RogueliteMapRun.CreateFirstRunV1(964);
            RogueMapStatusPresentation status = new RogueMapStatusPresentation(run);
            Assert.That(status.CompletedNodes, Is.Zero);
            Assert.That(status.RequiredCompletedNodes, Is.EqualTo(10));
            Assert.That(status.EarlyFinaleReady, Is.False);
            Assert.That(run.IsNodeAvailable("X"), Is.False);
        }

        [Test]
        public void OldWireNamesAndMarkersMigrateWithoutRerollingSavedContent()
        {
            const int seed = 965;
            RogueRunDto dto = RogueRunDto.CreateNew("old-wire", seed);
            dto.CurrentNodeId = "records_archive";
            dto.VisitedNodeIds.Add("records_archive");
            dto.PendingContentChoiceId = "EV16_assessment";
            dto.PendingContentCombatMissionId = "relay_event";
            dto.Gold = 37;
            dto.StageContribution = 9;
            var assignments = new RogueliteMapRun(seed).NodeContentAssignments.ToDictionary(p => p.Key, p => p.Value);
            string previous = assignments["records_archive"];
            string other = assignments.FirstOrDefault(p => p.Value == "EV16").Key;
            if (other != null) assignments[other] = previous;
            assignments["records_archive"] = "EV16";
            dto.NodeContentAssignments.AddRange(assignments.Select(p => p.Key + "=" + p.Value));
            string[] fields = Rogue11Serializer.Serialize(dto).Split('|');
            fields[19] = B("permit_archive");
            fields[22] = List("start", "permit_archive");
            fields[24] = List("permit:EV03", "permit:tower_lift");
            fields[26] = B("EV16_permit");
            fields[29] = List(assignments.Select(p => (p.Key == "records_archive" ? "permit_archive" : p.Key) + "=" + p.Value).ToArray());

            RogueRunDto restoredDto = Rogue11Serializer.Deserialize(string.Join("|", fields));
            RogueliteMapRun run = RogueliteMapRun.FromRogue11(restoredDto);
            Assert.That(run.CurrentNodeId, Is.EqualTo("records_archive"));
            Assert.That(run.PendingContentChoiceId, Is.EqualTo("EV16_assessment"));
            Assert.That(run.HasPendingContentCombat, Is.True);
            Assert.That(run.NodeContentAssignments.OrderBy(p => p.Key), Is.EqualTo(assignments.OrderBy(p => p.Key)));
            Assert.That(run.ClaimedRewards, Is.Empty);
            Assert.That(run.Gold, Is.EqualTo(37));
            Assert.That(run.StageContribution, Is.EqualTo(9));
            Assert.That(run.Seed, Is.EqualTo(seed));
            Assert.That(RogueliteMapRunValidator.Validate(run).IsValid, Is.True);
            run.CompletePendingContentCombat();
            Assert.That(run.Gold, Is.EqualTo(40));
            Assert.That(run.StageContribution, Is.EqualTo(11));
            Assert.That(run.ClaimedRewards, Is.Empty);
            string saved = Rogue11Serializer.Serialize(restoredDto);
            Assert.That(Rogue11Serializer.Deserialize(saved).ClaimedContentIds, Is.Empty);
        }

        [Test]
        public void LegacyMapCounterIsDiscardedAndOldNodeProgressIsPreserved()
        {
            RogueliteMapRun source = new RogueliteMapRun(966);
            source.SelectNode("supply_checkpoint");
            source.SelectNode("field_workshop");
            source.SelectNode("records_archive");
            source.ChooseCurrentNodeContent("survey");
            string[] fields = source.ToJson().Split('|');
            fields[3] = "permit_archive";
            fields[6] = "99";
            fields[16] = fields[16].Replace("records_archive", "permit_archive");
            fields[17] = fields[17].Replace("records_archive", "permit_archive");
            fields[18] = "permit:EV03";

            RogueliteMapRun restored = RogueliteMapRun.FromJson(string.Join("|", fields));
            Assert.That(restored.CurrentNodeId, Is.EqualTo("records_archive"));
            Assert.That(restored.CompletedNodes, Does.Contain("records_archive"));
            Assert.That(restored.ClaimedRewards, Is.Empty);
            Assert.That(restored.ScoutingBeacons, Is.EqualTo(source.ScoutingBeacons));
            Assert.That(restored.Parts, Is.EqualTo(source.Parts));
            Assert.That(restored.ToJson().Split('|')[6], Is.EqualTo("0"));
            Assert.That(RogueliteMapRunValidator.Validate(restored).IsValid, Is.True);

            string legacy = string.Join("|", fields);
            Assert.That(RogueliteMapRunValidator.ValidateSerializedCurrent(legacy).IsValid, Is.True);
            Store store = new Store();
            store.Values[RogueliteSaveGateway.MapRunKey] = legacy;
            RogueliteSaveGateway gateway = new RogueliteSaveGateway(store);
            Assert.That(gateway.TryLoadMapRun(out RogueliteMapRun loaded), Is.True, gateway.LastError);
            Assert.That(loaded.CurrentNodeId, Is.EqualTo("records_archive"));
            Assert.That(loaded.CompletedNodes, Does.Contain("records_archive"));
            Assert.That(loaded.ClaimedRewards, Is.Empty);
            Assert.That(store.Values[RogueliteSaveGateway.MapRunKey], Does.StartWith("rogue11|"));
            Assert.That(store.Values.Values, Does.Contain(legacy), "The original migration backup must be retained.");
        }

        [Test]
        public void UnknownRewardMarkersRemainInvalidRatherThanBeingSilentlyDiscarded()
        {
            RogueRunDto dto = RogueRunDto.CreateNew("bad-marker", 967);
            Assert.That(RogueliteMapRunValidator.Validate(RogueliteMapRun.FromRogue11(dto)).IsValid, Is.True);
            dto.ClaimedContentIds.Add("permit:unknown");
            RogueliteMapRun run = RogueliteMapRun.FromRogue11(Rogue11Serializer.Deserialize(Rogue11Serializer.Serialize(dto)));
            Assert.That(run.ClaimedRewards, Does.Contain("permit:unknown"));
            Assert.That(RogueliteMapRunValidator.Validate(run).IsValid, Is.False);
        }

        [Test]
        public void ActiveNodeCopyAndRewardsContainNoRetiredProgressConcept()
        {
            string[] forbidden = { "核心许可", "权限卡", "许可档案" };
            var copy = RogueliteMapCatalog.Nodes.Select(n => n.DisplayName + " " + n.Summary)
                .Concat(AcademyNodeContentCatalog.Events.SelectMany(e => e.Choices).Select(c => c.DisplayName + " " + c.Preview))
                .Concat(AcademyNodeContentCatalog.FunctionChoices(RogueliteMapCatalog.Node("tower_lift")).Select(c => c.DisplayName + " " + c.Preview));
            foreach (string text in copy)
                foreach (string word in forbidden) Assert.That(text, Does.Not.Contain(word));
        }

        private static string B(string text) => Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
        private static string List(params string[] values) => B(string.Join(",", values.Select(B)));
    }
}
