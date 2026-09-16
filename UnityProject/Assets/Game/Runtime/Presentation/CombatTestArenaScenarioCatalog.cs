using System;
using System.Collections.Generic;
using System.Linq;
using OCC.Combat.Roguelite;

namespace OCC.Combat.Presentation
{
    /// <summary>
    /// Play-mode-only encounter definitions.  These are intentionally code-owned
    /// test fixtures: the current stage content remains in the content sheets,
    /// while the arena gives designers a fast, repeatable way to exercise a full
    /// combat loadout against the candidate spatial grammars.
    /// </summary>
    public sealed class CombatTestArenaScenario
    {
        public string Id { get; }
        public string ShortName { get; }
        public string DisplayName { get; }
        public string SelectionSummary { get; }
        public FirstRegionLevelDefinition Level { get; }
        public IReadOnlyList<string> SpellIds { get; }
        public IReadOnlyList<string> ArtifactIds { get; }
        public bool IsHighPressure { get; }
        public bool IsSystemTest { get; }
        public bool IsSkillTest { get; }

        public CombatTestArenaScenario(string id, string shortName, string displayName, string summary,
            FirstRegionLevelDefinition level, IEnumerable<string> spellIds, IEnumerable<string> artifactIds,
            bool isHighPressure = false, bool isSystemTest = false, bool isSkillTest = false)
        {
            Id = id;
            ShortName = shortName;
            DisplayName = displayName;
            SelectionSummary = summary;
            Level = level;
            SpellIds = spellIds.ToArray();
            ArtifactIds = artifactIds.ToArray();
            IsHighPressure = isHighPressure;
            IsSystemTest = isSystemTest;
            IsSkillTest = isSkillTest;
            if (SpellIds.Count != RogueRuntimeConstants.SpellSlotCount)
                throw new ArgumentException("A test scenario must fill all eight spell slots.", nameof(spellIds));
            if (ArtifactIds.Count != RogueRuntimeConstants.ItemQuickbarSize)
                throw new ArgumentException("A test scenario must fill all four tactical slots.", nameof(artifactIds));
        }
    }

    public static class CombatTestArenaScenarioCatalog
    {
        private enum ArenaBuild { Breach, Dash, Fire }

        private sealed class ArenaLoadout
        {
            public string Label { get; }
            public IReadOnlyList<string> SpellIds { get; }
            public IReadOnlyList<string> ArtifactIds { get; }

            public ArenaLoadout(string label, IEnumerable<string> spellIds, IEnumerable<string> artifactIds)
            {
                Label = label;
                SpellIds = spellIds.ToArray();
                ArtifactIds = artifactIds.ToArray();
            }
        }

        private static int randomizedBuildSequence;
        private static LevelTerrainPlacement L(int x, int y) => new LevelTerrainPlacement(x, y, LevelTerrainKind.LightCover);
        private static LevelTerrainPlacement H(int x, int y) => new LevelTerrainPlacement(x, y, LevelTerrainKind.HeavyCover);
        private static LevelTerrainPlacement V(int x, int y) => new LevelTerrainPlacement(x, y, LevelTerrainKind.LampVine);
        private static LevelTerrainPlacement W(int x, int y) => new LevelTerrainPlacement(x, y, LevelTerrainKind.Water);
        private static LevelTerrainPlacement C(int x, int y) => new LevelTerrainPlacement(x, y, LevelTerrainKind.AetherCrystal);

        private static readonly string[] FlankSpells =
        {
            "BASE-FIRE-MELEE", "BASE-FIRE-RANGED", "BASE-AETHER-SHIELD", "BASE-MANA-RECOVER",
            "F-P-R04", "F-P-U19", "F-P-U18", "PASSIVE-ELITE-02"
        };

        private static readonly string[] ControlSpells =
        {
            "BASE-FIRE-MELEE", "BASE-FIRE-RANGED", "BASE-AETHER-SHIELD", "BASE-MANA-RECOVER",
            "F-P-U04", "F-P-U18", "F-P-R03", "F-P-M07"
        };

        private static readonly string[] FireSpells =
        {
            "BASE-FIRE-MELEE", "BASE-FIRE-RANGED", "F-P-R12", "BASE-MANA-RECOVER",
            "PASSIVE-ELITE-01", "F-P-U08", "F-P-U11", "F-P-R03"
        };

        private static readonly string[] RestraintSpells =
        {
            "BASE-FIRE-MELEE", "BASE-FIRE-RANGED", "F-P-U07", "BASE-MANA-RECOVER",
            "F-P-M02", "F-P-M14", "F-P-U09", "F-P-U18"
        };

        private static readonly string[] CrossfireSpells =
        {
            "BASE-FIRE-MELEE", "BASE-FIRE-RANGED", "F-P-R12", "BASE-MANA-RECOVER",
            "F-P-U01", "F-P-M11", "F-P-R06", "F-P-R19"
        };

        private static readonly string[] ArbalistSpells =
        {
            "BASE-FIRE-MELEE", "BASE-FIRE-RANGED", "BASE-AETHER-SHIELD", "BASE-MANA-RECOVER",
            "F-P-U01", "F-P-M11", "F-P-R06", "F-P-U16"
        };

        private static readonly string[] PressureSpells =
        {
            "BASE-FIRE-MELEE", "BASE-FIRE-RANGED", "F-P-U07", "BASE-MANA-RECOVER",
            "F-P-U03", "F-P-U18", "F-P-M09", "PASSIVE-ELITE-03"
        };

        private static readonly string[] FinisherSpells =
        {
            "BASE-FIRE-MELEE", "F-P-R03", "F-P-U07", "BASE-MANA-RECOVER",
            "F-P-R12", "F-P-M18", "F-P-M19", "F-P-M20"
        };

        private static readonly CombatTestArenaScenario[] Scenarios = BuildScenarios();

        private static CombatTestArenaScenario[] BuildScenarios()
        {
            return new[]
            {
                CreateFlankDrill(), CreateTrackerDrill(), CreateBarrierDrill(),
                CreateFireDrill(), CreateArbalistDrill(), CreateRestraintDrill(),
                CreateMaintenanceDrill(), CreateContainmentDrill(), CreateCrossfireDrill(),
                CreateEliteMaintenance(), CreateEliteCrosslock(), CreateElitePressure(), CreateAcademyCoreBoss(),
                CreateReactionPressureTest(), CreateProtectionPressureTest(), CreateEfficiencyPressureTest()
            }.Concat(CreateSpellLabs()).ToArray();
        }

        private static IEnumerable<CombatTestArenaScenario> CreateSpellLabs()
        {
            yield return CreateSpellLab("m_a", "近战术式Ⅰ", new[] { "F-P-M01", "F-P-M02", "F-P-M03", "F-P-M04", "F-P-M05", "F-P-M06", "F-P-M08", "F-P-R03" },
                new[] { "G-T02", "G-T09", "G-T18", "G-T11" });
            yield return CreateSpellLab("m_b", "近战术式Ⅱ", new[] { "F-P-M09", "F-P-M10", "F-P-M11", "F-P-M12", "F-P-M13", "F-P-M14", "F-P-M15", "F-P-R03" });
            yield return CreateSpellLab("m_c", "近战术式Ⅲ", new[] { "F-P-M07", "F-P-M16", "F-P-M17", "F-P-M18", "F-P-M19", "F-P-M20", "F-P-R03", "F-P-R12" });
            yield return CreateSpellLab("u_a", "通用术式Ⅰ", Range("U", 1, 8),
                new[] { "G-T05", "G-T09", "G-T18", "G-T11" });
            yield return CreateSpellLab("u_b", "通用术式Ⅱ", new[] { "F-P-U11", "F-P-U12", "F-P-U13", "F-P-U14", "F-P-U15", "F-P-U16", "F-P-R03", "F-P-R12" });
            yield return CreateSpellLab("u_c", "通用术式Ⅲ", new[] { "F-P-U09", "F-P-U10", "F-P-U17", "F-P-U18", "F-P-U19", "F-P-U20", "F-P-R03", "F-P-R12" });
            yield return CreateSpellLab("r_a", "远程术式Ⅰ", Range("R", 1, 8));
            yield return CreateSpellLab("r_b", "远程术式Ⅱ", new[] { "F-P-R10", "F-P-R11", "F-P-R12", "F-P-R13", "F-P-R14", "F-P-R15", "F-P-R16", "F-P-R03" });
            yield return CreateSpellLab("r_c", "远程术式Ⅲ", new[] { "F-P-R09", "F-P-R17", "F-P-R18", "F-P-R19", "F-P-R20", "F-P-R03", "F-P-U18", "F-P-U19" },
                new[] { "G-T19", "G-T09", "G-T18", "G-T11" });
        }

        private static string[] Range(string group, int first, int count) => Enumerable.Range(first, count)
            .Select(index => $"F-P-{group}{index:00}").ToArray();

        private static CombatTestArenaScenario CreateSpellLab(string suffix, string name, string[] spells, string[] artifacts = null)
        {
            HashSet<GridPosition> walkable = Cells("C2 D2 E2 F2 G2 H2 B3 C3 D3 E3 F3 G3 H3 B4 C4 D4 E4 F4 G4 H4 B5 C5 D5 E5 F5 G5 H5 C6 D6 E6 F6 G6 H6");
            string id = "arena_l_" + suffix;
            FirstRegionLevelDefinition level = Level(id, "L｜" + name,
                "在同一紧凑场地验证整组术式的射程、条件、位移、场地覆盖与友伤预览。",
                new GridPosition(2, 3), new[] { E("shieldguard", 4, 3), E("pyromancer", 7, 1), E("barrier_mender", 7, 5) },
                new[] { L(3, 2), L(3, 4), H(5, 2), H(5, 4), W(4, 1), W(4, 5), C(6, 3) }, walkable,
                "近身目标、远距目标与支援目标同时公开", "掩体、水区和晶簇提供通用条件，不绑定单关机关");
            return new CombatTestArenaScenario(id, name, "L｜" + name,
                "8 个术式全满；近、中、远目标及通用场地同时可用。", level, spells,
                artifacts ?? new[] { "G-T01", "G-T09", "G-T18", "G-T11" }, false, false, true);
        }

        public const string DefaultScenarioId = "arena_n01_flank";
        public static CombatTestArenaScenario[] All => (CombatTestArenaScenario[])Scenarios.Clone();

        public static CombatTestArenaScenario Get(string id) => Scenarios.FirstOrDefault(value => value.Id == id)
            ?? Scenarios.First(value => value.Id == DefaultScenarioId);

        public static CombatSceneSessionBuild Build(string id)
            => Build(id, null);

        /// <summary>Used by the play-mode arena entry. Each run favours one complete player build.</summary>
        public static CombatSceneSessionBuild BuildRandomized(string id)
        {
            CombatTestArenaScenario scenario = Get(id);
            int seed = unchecked(Environment.TickCount ^ scenario.Id.GetHashCode() ^ ++randomizedBuildSequence * 486187739);
            return Build(id, RollLoadout(new Random(seed)));
        }

        private static CombatSceneSessionBuild Build(string id, ArenaLoadout randomizedLoadout)
        {
            CombatTestArenaScenario scenario = Get(id);
            FirstRegionLevelBuild build = FirstRegionLevelBuilder.Build(scenario.Level);
            CombatState state = build.State;
            state.ConfigureRuleset(CombatRuleset.Roguelite);
            RogueAcademyContentService academyContent = new RogueAcademyContentService();
            foreach (UnitState enemy in state.Units.Values.Where(unit => !unit.IsHero)) academyContent.ApplyEnemyBaseline(state, enemy);
            if (state.Units.Values.Any(unit => unit.EnemyArchetypeId == "breach_ram"))
                state.AttachThreeMaterialPressure(new ThreeMaterialPressureRuntime());
            if (scenario.Id == "arena_s01_reaction")
                state.AttachPressureTest(new CombatPressureTestRuntime("enemy_0"));
            if (scenario.Id == "arena_s02_protect")
            {
                GridPosition protectedPosition = new GridPosition(5, 3);
                TileState protectedTile = state.Map.GetTile(protectedPosition);
                protectedTile.Cover = CoverType.Heavy;
                protectedTile.IsObjective = true;
                protectedTile.IsDevice = true;
                protectedTile.Durability = 12;
                state.ConfigureObjectives(new EliminationObjective(scenario.Id + "_elimination"),
                    new ProtectionObjective(protectedPosition, scenario.Id + "_protection"));
                state.AttachPressureTest(new CombatPressureTestRuntime(null, "enemy_0", protectedPosition));
            }
            if (scenario.Id == "arena_s03_efficiency")
                state.AttachPressureTest(new CombatPressureTestRuntime(efficiencyTurnLimit: 3));

            UnitState hero = state.GetUnit("hero");
            hero.ConfigureMana(12);
            IReadOnlyList<string> spellIds = randomizedLoadout?.SpellIds ?? scenario.SpellIds;
            IReadOnlyList<string> artifactIds = randomizedLoadout?.ArtifactIds ?? scenario.ArtifactIds;
            RogueSpellLoadout spells = RogueSpellLoadout.Restore(spellIds, spellIds, true);
            state.AttachRogueSpellRuntime(new RogueSpellCombatRuntime(state, spells));

            RogueEquipmentRuntime equipment = RogueEquipmentRuntime.CreateStarter(8137);
            for (int slot = 0; slot < artifactIds.Count; slot++)
            {
                string instanceId = "arena-" + scenario.Id + "-artifact-" + slot;
                RogueTacticalItemInstance item = equipment.CreateTacticalItem(instanceId, artifactIds[slot], slot, "test-arena");
                if (!equipment.AddTacticalToBackpack(item) || !equipment.AssignQuickbar(slot, instanceId))
                    throw new InvalidOperationException("Test-arena tactical loadout could not be installed: " + instanceId);
            }
            state.AttachRogueEquipmentRuntime(equipment);

            string summary = randomizedLoadout == null ? scenario.SelectionSummary :
                scenario.SelectionSummary + "\n随机构筑：" + randomizedLoadout.Label + "（6 张同流派术式、2 张通用补位；3 件同流派法宝、1 件通用补位）";
            MissionPreparation preparation = new MissionPreparation().Configure(scenario.Id,
                scenario.Level.ObjectiveSummary, summary);
            return new CombatSceneSessionBuild(state, preparation, scenario.Level);
        }

        private static ArenaLoadout RollLoadout(Random random)
        {
            ArenaBuild build = (ArenaBuild)random.Next(3);
            string[] coreSpells;
            string[] supportSpells;
            string[] coreArtifacts;
            string label;
            switch (build)
            {
                case ArenaBuild.Breach:
                    label = "地块破坏回流";
                    coreSpells = new[] { "F-P-R19", "F-P-U04", "F-P-R10", "F-P-M06", "F-P-U03", "F-P-M08", "F-P-R06", "F-P-R09", "F-P-U18" };
                    supportSpells = new[] { "F-P-U07", "F-P-U09", "F-P-U11", "F-P-M15" };
                    coreArtifacts = new[] { "F-T01", "G-T08", "G-T17", "G-T07", "G-T09" };
                    break;
                case ArenaBuild.Dash:
                    label = "突进穿刺";
                    coreSpells = new[] { "F-P-M01", "F-P-M02", "F-P-M03", "F-P-M04", "F-P-M05", "F-P-M08", "F-P-M09", "F-P-M15", "F-P-M18", "F-P-M19" };
                    supportSpells = new[] { "F-P-U07", "F-P-U09", "F-P-U11", "F-P-R03" };
                    coreArtifacts = new[] { "G-T02", "G-T09", "G-T13", "G-T15", "G-T18" };
                    break;
                default:
                    label = "燃烧火场";
                    coreSpells = new[] { "F-P-R01", "F-P-R03", "F-P-R04", "F-P-R05", "F-P-R07", "F-P-R08", "F-P-R11", "F-P-R12", "F-P-R13", "F-P-R14", "F-P-R15", "F-P-R16", "F-P-R17", "F-P-R18", "F-P-R20" };
                    supportSpells = new[] { "F-P-U08", "F-P-U11", "F-P-U07", "F-P-U09" };
                    coreArtifacts = new[] { "F-T01", "G-T05", "G-T11", "G-T18", "G-T09" };
                    break;
            }

            string[] artifacts = TakeRandom(coreArtifacts, 3, random).ToArray();
            string[] utility = new[] { "G-T01", "G-T03", "G-T09", "G-T18" }.Where(id => !artifacts.Contains(id)).ToArray();
            return new ArenaLoadout(label, TakeRandom(coreSpells, 6, random).Concat(TakeRandom(supportSpells, 2, random)),
                artifacts.Concat(TakeRandom(utility, 1, random)));
        }

        private static IEnumerable<string> TakeRandom(IEnumerable<string> source, int count, Random random)
            => source.OrderBy(_ => random.Next()).ThenBy(id => id, StringComparer.Ordinal).Take(count);

        private static CombatTestArenaScenario CreateFlankDrill()
        {
            HashSet<GridPosition> walkable = Cells("D2 E2 F2 G2 C3 D3 E3 G3 H3 B4 C4 D4 E4 F4 G4 H4 C5 D5 E5 G5 H5 D6 E6 F6 G6");
            FirstRegionLevelDefinition level = Level("arena_n01_flank", "N01｜中庭侧锋对练",
                "击倒盾术陪练生与侧锋陪练生。中线会卡住，北侧与南侧都能绕开。",
                new GridPosition(1, 3), new[] { E("shieldguard", 5, 3), E("raider", 6, 5) },
                new[] { L(2, 3), L(4, 5), L(4, 1), L(6, 1), H(5, 2), H(5, 4) }, walkable,
                "中线压盾，北截侧锋，南线绕侧", "盾术守中，侧锋从北线逼近；任一侧线都能改变先手。 ");
            return new CombatTestArenaScenario("arena_n01_flank", "侧锋对练", "N01｜中庭侧锋对练",
                "25 格可走区；可试罗盘拉近→火种＋协同标记→武器命中推位留火。", level, FlankSpells,
                new[] { "G-T09", "G-T07", "G-T01", "G-T13" });
        }

        private static CombatTestArenaScenario CreateTrackerDrill()
        {
            HashSet<GridPosition> walkable = Cells("C2 D2 E2 F2 G2 B3 C3 D3 E3 G3 H3 B4 C4 D4 E4 G4 B5 C5 D5 E5 F5 G5 H5 C6 D6 E6 F6 G6");
            FirstRegionLevelDefinition level = Level("arena_n02_tracker", "N02｜宿舍外寻迹测试",
                "击倒盾术陪练生与缚环寻迹兽。灯藤会遮住攻击线，但不会提供永久安全。",
                new GridPosition(1, 3), new[] { E("shieldguard", 6, 5), E("tether_hound", 6, 1) },
                new[] { L(2, 3), L(2, 5), L(2, 1), L(5, 5), L(5, 1), V(4, 2), V(4, 3), V(4, 4), H(5, 2), H(5, 3) }, walkable,
                "藤带拆压，北截盾术，南诱寻迹兽", "灯藤延迟视线与追击；北、南干路依旧可走。 ");
            return new CombatTestArenaScenario("arena_n02_tracker", "寻迹测试", "N02｜宿舍外寻迹测试",
                "28 格可走区；可试诱导灯只引开寻迹兽，再用灯藤高移动成本与缚位框维持拆分。", level, ControlSpells,
                new[] { "G-T15", "G-T10", "G-T03", "G-T09" });
        }

        private static CombatTestArenaScenario CreateBarrierDrill()
        {
            HashSet<GridPosition> walkable = Cells("C2 D2 E2 F2 G2 B3 C3 D3 F3 G3 H3 B4 C4 D4 F4 G4 H4 B5 C5 D5 E5 F5 G5 H5 C6 D6 E6 F6 G6");
            FirstRegionLevelDefinition level = Level("arena_n03_barrier", "N03｜护障课程示范",
                "击倒护障助教与盾术陪练生。用柜体、站位或位移切断维护线。",
                new GridPosition(1, 3), new[] { E("barrier_mender", 6, 5), E("shieldguard", 5, 3) },
                new[] { L(2, 3), L(3, 5), L(3, 1), L(6, 2), H(4, 2), H(4, 3) }, walkable,
                "北拆支援，中继切线，南侧压盾", "护障先于盾术行动；在 G5 建立重掩体会公开截断本轮续盾线。 ");
            return new CombatTestArenaScenario("arena_n03_barrier", "护障示范", "N03｜护障课程示范",
                "29 格可走区；可试移至 E5→在 G5 建墙，花满 3 AP 换一次公开续盾断档。", level, ControlSpells,
                new[] { "G-T08", "G-T07", "G-T09", "G-T04" });
        }

        private static CombatTestArenaScenario CreateFireDrill()
        {
            HashSet<GridPosition> walkable = Cells("C2 D2 E2 F2 G2 B3 C3 D3 E3 F3 H3 B4 C4 D4 E4 F4 G4 H4 B5 C5 D5 E5 G5 H5 C6 D6 E6 F6 G6");
            FirstRegionLevelDefinition level = Level("arena_n04_fire", "N04｜锅炉房火线演练",
                "击倒火矢陪练生与侧锋陪练生。浅水带能清出安全落脚点，火线会迫使双方改道。",
                new GridPosition(1, 3), new[] { E("pyromancer", 6, 1), E("raider", 6, 4) },
                new[] { L(2, 2), L(2, 4), H(4, 2), H(4, 4), W(3, 3), W(4, 3), W(5, 3) }, walkable,
                "水带越线，掩体逼近，侧廊追火矢", "浅水与火场互相覆盖；站在水带上安全，但会暴露在两侧攻击线上。 ");
            return new CombatTestArenaScenario("arena_n04_fire", "火线演练", "N04｜锅炉房火线演练",
                "29 格可走区；可试火路覆盖水带→冷凝器清通道→封装筒续火→导位器拉回侧锋。", level, FireSpells,
                new[] { "G-T11", "G-T07", "G-T09", "F-T01" });
        }

        private static CombatTestArenaScenario CreateArbalistDrill()
        {
            HashSet<GridPosition> walkable = Cells("D2 E2 F2 G2 H2 B3 C3 D3 E3 F3 H3 B4 C4 D4 F4 G4 H4 B5 C5 D5 E5 F5 G5 H5 C6 D6 E6 F6 G6 H6");
            FirstRegionLevelDefinition level = Level("arena_n05_arbalist", "N05｜回廊重弩校准",
                "击倒重弩陪练生与侧锋陪练生。重柜切断远射线，短掩体只够完成一次换位。",
                new GridPosition(1, 3), new[] { E("rune_arbalist", 7, 1), E("raider", 6, 4) },
                new[] { L(2, 2), L(2, 4), L(5, 2), H(4, 3), H(6, 3), H(6, 5) }, walkable,
                "柜后换线，近身压弩，侧锋封退路", "直线留给重弩，柜后留给逼近；破坏柜体会同时打开双方射线。 ");
            return new CombatTestArenaScenario("arena_n05_arbalist", "重弩校准", "N05｜回廊重弩校准",
                "30 格可走区；可试 D4 拆 E4 重柜后越线，再贴入重弩 1 格死区迫其公开退距。", level, ArbalistSpells,
                new[] { "G-T07", "G-T08", "G-T14", "G-T15" });
        }

        private static CombatTestArenaScenario CreateRestraintDrill()
        {
            HashSet<GridPosition> walkable = Cells("C2 D2 E2 F2 G2 B3 C3 D3 E3 F3 G3 H3 B4 C4 D4 E4 F4 G4 H4 B5 C5 D5 F5 G5 H5 C6 D6 E6 F6 G6");
            FirstRegionLevelDefinition level = Level("arena_n06_restraint", "N06｜石索约束考核",
                "击倒约束助教与侧锋陪练生。石索封步后仍可解缚、换位或守住窄口。",
                new GridPosition(1, 3), new[] { E("stone_snare", 5, 2), E("raider", 6, 4) },
                new[] { L(2, 3), L(3, 1), L(4, 4), L(3, 2), H(5, 4), C(4, 3) }, walkable,
                "晶体争位，窄口抗缚，侧路换线", "石索控制通路、侧锋贴身；晶体是可争夺资源，不是唯一解。 ");
            return new CombatTestArenaScenario("arena_n06_restraint", "约束考核", "N06｜石索约束考核",
                "30 格可走区；可试 D4 承受石索→罗盘拉助教到 D3→灼缚解离后立即换线。", level, RestraintSpells,
                new[] { "G-T03", "G-T09", "G-T13", "G-T06" });
        }

        private static CombatTestArenaScenario CreateMaintenanceDrill()
        {
            HashSet<GridPosition> walkable = Cells("C2 D2 E2 F2 G2 H2 B3 C3 D3 F3 G3 H3 B4 C4 D4 E4 F4 G4 H4 B5 C5 D5 E5 G5 H5 C6 D6 E6 F6 G6");
            FirstRegionLevelDefinition level = Level("arena_n07_maintenance", "N07｜档案廊维护链",
                "击倒护障助教、巡查员与盾术陪练生。先切断维护线，还是先处理显影威胁，由站位决定。",
                new GridPosition(1, 3), new[] { E("shieldguard", 5, 3), E("barrier_mender", 6, 5), E("lantern_revealer", 7, 1) },
                new[] { L(2, 2), L(2, 4), L(6, 2), H(4, 2), H(4, 4), V(5, 1), V(5, 5) }, walkable,
                "中路断援，北压显影，南追维护", "三敌分处不同攻击线；灯藤能遮线，也可能掩护维护者。 ");
            return new CombatTestArenaScenario("arena_n07_maintenance", "维护链", "N07｜档案廊维护链",
                "30 格可走区；可试罗盘将盾术拉出 4 格维护范围，再以烙印＋贴身占位拆开三种意图。", level, ControlSpells,
                new[] { "G-T08", "G-T09", "G-T14", "G-T15" });
        }

        private static CombatTestArenaScenario CreateContainmentDrill()
        {
            HashSet<GridPosition> walkable = Cells("C2 D2 E2 F2 G2 B3 C3 D3 E3 F3 G3 H3 B4 C4 D4 E4 F4 H4 B5 C5 D5 E5 F5 G5 H5 C6 D6 E6 F6 G6");
            FirstRegionLevelDefinition level = Level("arena_n08_containment", "N08｜器材库收束演练",
                "击倒寻迹兽、承压检验偶与约束助教。不要让束缚与近身破势在同一回合闭合。",
                new GridPosition(1, 3), new[] { E("tether_hound", 6, 1), E("sigil_mauler", 7, 3), E("stone_snare", 6, 5) },
                new[] { L(2, 2), L(2, 4), L(4, 1), L(4, 5), H(4, 3), H(6, 3), C(3, 3) }, walkable,
                "中轴卡偶，上下拆缚，晶体换资源", "上下两路施加束缚，中轴重偶负责惩罚停留；三者不会修改行动条。 ");
            return new CombatTestArenaScenario("arena_n08_containment", "收束演练", "N08｜器材库收束演练",
                "30 格可走区；北路进位后用缚位框截住寻迹兽一轮，再以震测铅锤推开中轴重偶并撤出夹击线。", level, RestraintSpells,
                new[] { "G-T03", "G-T07", "G-T09", "G-T17" });
        }

        private static CombatTestArenaScenario CreateCrossfireDrill()
        {
            HashSet<GridPosition> walkable = Cells("C2 D2 E2 F2 G2 H2 B3 C3 D3 E3 F3 H3 B4 C4 D4 E4 F4 G4 H4 B5 C5 E5 F5 G5 H5 C6 D6 E6 F6 G6");
            FirstRegionLevelDefinition level = Level("arena_n09_crossfire", "N09｜终段交叉火线",
                "击倒盾术、火矢与侧锋陪练生。火线会重画安全区，掩体破坏会改变全场射线。",
                new GridPosition(1, 3), new[] { E("shieldguard", 5, 3), E("pyromancer", 7, 1), E("raider", 6, 5) },
                new[] { L(2, 2), L(2, 4), L(5, 1), H(4, 2), H(4, 4), H(6, 3), W(3, 3), W(4, 3) }, walkable,
                "水带抢中，拆柜开线，侧路分敌", "盾术占中、火矢控远、侧锋封退；每次破坏或点火都改变下一轮站位。 ");
            return new CombatTestArenaScenario("arena_n09_crossfire", "交叉火线", "N09｜终段交叉火线",
                "30 格可走区；火路覆盖水带并压住中轴盾术，冷凝出入口后可追击抢效率，或以熔障爆点削柜后择一路解构推进。", level, CrossfireSpells,
                new[] { "G-T11", "G-T08", "G-T09", "G-T01" });
        }

        private static CombatTestArenaScenario CreateEliteMaintenance()
        {
            HashSet<GridPosition> walkable = Cells("C2 D2 E2 F2 G2 H2 B3 C3 D3 E3 F3 G3 H3 B4 C4 D4 E4 F4 G4 H4 B5 C5 D5 E5 F5 G5 H5 C6 D6 E6 F6 G6 H6");
            FirstRegionLevelDefinition level = Level("arena_e01_maintenance", "E01｜刻阵工坊断供",
                "击倒刻阵教官、护障助教与承压检验偶。切断续盾线，再决定从哪一侧处理破势近战。",
                new GridPosition(1, 3), new[] { E("elite_vanguard", 5, 3), E("barrier_mender", 7, 1), E("sigil_mauler", 6, 5) },
                new[] { L(2, 2), L(2, 4), L(6, 2), H(4, 2), H(4, 4), C(5, 1) }, walkable,
                "断供换线，双侧破势，晶体抢窗", "教官占中、助教续盾、检验偶封侧；三者的维护与接敌线全程公开。 ",
                true);
            return new CombatTestArenaScenario("arena_e01_maintenance", "工坊断供", "E01｜刻阵工坊断供",
                "32 格可走区；罗盘可把教官拉出维护距离，再以 U03 附着主手攻击、M09 重击完成断供；也可破晶或削柜换线。", level, PressureSpells,
                new[] { "G-T08", "G-T09", "G-T14", "G-T16" }, true);
        }

        private static CombatTestArenaScenario CreateEliteCrosslock()
        {
            HashSet<GridPosition> walkable = Cells("C2 D2 E2 F2 G2 H2 B3 C3 D3 E3 F3 G3 H3 B4 C4 D4 F4 G4 H4 B5 C5 D5 E5 F5 G5 H5 C6 D6 E6 F6 G6 H6");
            FirstRegionLevelDefinition level = Level("arena_e02_crosslock", "E02｜塔前对角封锁",
                "击倒刻阵教官、重弩陪练生与约束助教。两条远程线交叉，但不能同时覆盖两侧外廊。",
                new GridPosition(1, 3), new[] { E("elite_vanguard", 5, 3), E("rune_arbalist", 7, 1), E("stone_snare", 7, 5) },
                new[] { L(2, 2), L(2, 4), L(5, 1), L(5, 5), H(4, 3), H(6, 2), H(6, 4) }, walkable,
                "双廊切角，中轴诱敌，拆柜反射线", "重弩与石索覆盖相反外廊；教官只惩罚直穿中央。 ",
                true);
            return new CombatTestArenaScenario("arena_e02_crosslock", "对角封锁", "E02｜塔前对角封锁",
                "31 格可走区；北压重弩、南拆石索或中轴诱教官后换线。", level, CrossfireSpells,
                new[] { "G-T07", "G-T08", "G-T18", "G-T15" }, true);
        }

        private static CombatTestArenaScenario CreateElitePressure()
        {
            HashSet<GridPosition> walkable = Cells("C2 D2 E2 F2 G2 H2 B3 C3 D3 E3 F3 G3 H3 B4 C4 D4 E4 F4 G4 H4 B5 C5 D5 E5 F5 G5 H5 C6 D6 E6 F6 G6 H6");
            FirstRegionLevelDefinition level = Level("arena_e03_pressure", "E03｜楔角稳压测试",
                "击倒贯阵承压机·楔角与火矢陪练生。公开冲压线会先撞晶簇；水沟、灯藤和干路产生三种卸压结果。",
                new GridPosition(1, 3), new[] { E("breach_ram", 7, 3), E("pyromancer", 7, 1) },
                new[] { W(3, 1), W(3, 2), W(3, 3), W(3, 4), W(3, 5), V(5, 1), V(5, 2), V(5, 4), V(5, 5), C(6, 3), L(2, 5) }, walkable,
                "水沟冷却，藤带藏线，撞晶开窗", "楔角锁定主角并按五点移动预算冲压；碰撞对象与干湿地卸压结果在行动前公开。 ",
                true);
            return new CombatTestArenaScenario("arena_e03_pressure", "楔角稳压", "E03｜楔角稳压测试",
                "32 格可走区；诱撞晶簇、借水冷却或从南侧干路等卸压。", level, ControlSpells,
                new[] { "G-T09", "G-T10", "G-T01", "G-T13" }, true);
        }

        private static CombatTestArenaScenario CreateAcademyCoreBoss()
        {
            HashSet<GridPosition> walkable = Cells("C2 D2 E2 F2 G2 H2 I2 J2 B3 C3 D3 E3 F3 G3 H3 I3 J3 B4 C4 D4 E4 F4 G4 H4 I4 J4 B5 C5 D5 E5 F5 G5 H5 I5 J5 B6 C6 D6 E6 F6 G6 H6 I6 J6 C7 D7 E7 F7 G7 H7 I7");
            FirstRegionLevelDefinition level = Level("arena_b01_core", "B01｜学院封存塔核心",
                "击倒核心守备及三条外围维护链。首回合公开核心、教官、护障和显影四个意图。",
                new GridPosition(1, 3), new[] { E("core_overseer", 5, 3), E("elite_vanguard", 6, 6), E("barrier_mender", 9, 1), E("lantern_revealer", 3, 1) },
                new[] { L(2, 3), L(3, 5), L(8, 1), L(8, 5), H(4, 2), H(6, 2), H(4, 4), H(6, 4), C(7, 3) }, walkable,
                "中心核心，三链环绕，双缺口换序", "上路连向显影与护障，下路连向教官；先拆哪条链决定核心周围的安全缺口。 ",
                false, true, 11, 8);
            return new CombatTestArenaScenario("arena_b01_core", "封存塔核心", "B01｜学院封存塔核心",
                "51 格可走区；四条公开意图与两种拆链顺序；预设火路、踏行、超限与断击完整终结链。", level, FinisherSpells,
                new[] { "G-T08", "G-T09", "G-T12", "G-T16" }, true);
        }

        private static CombatTestArenaScenario CreateReactionPressureTest()
        {
            HashSet<GridPosition> walkable = Cells("C2 D2 E2 F2 G2 H2 B3 C3 D3 E3 F3 G3 H3 B4 C4 D4 E4 F4 G4 H4 B5 C5 D5 E5 F5 G5 H5 C6 D6 E6 F6 G6");
            FirstRegionLevelDefinition level = Level("arena_s01_reaction", "S01｜警戒火线反应",
                "击倒重弩与侧锋。主角移动结束进入重弩 2–4 格正交射界时触发一次公开反应；射线先命中谁就打谁。",
                new GridPosition(1, 3), new[] { E("rune_arbalist", 7, 3), E("raider", 5, 3) },
                new[] { L(2, 3), L(2, 1), L(2, 5), H(4, 2), H(4, 4), L(6, 1), L(6, 5) }, walkable,
                "中央诱导友伤，上下切线，相邻钻入死区", "高风险移动格直接显示警戒结果；反应每个主角回合限一次，不改变行动条。 ");
            return new CombatTestArenaScenario("arena_s01_reaction", "警戒火线", "S01｜警戒火线反应",
                "移动到 D4 可诱导重弩先命中 F4 的侧锋；上下线可切断射界，相邻格是重弩死区。", level,
                FlankSpells, new[] { "G-T09", "G-T10", "G-T07", "G-T01" }, false, true);
        }

        private static CombatTestArenaScenario CreateProtectionPressureTest()
        {
            HashSet<GridPosition> walkable = Cells("C2 D2 E2 F2 G2 H2 B3 C3 D3 E3 F3 G3 H3 B4 C4 D4 E4 F4 G4 H4 B5 C5 D5 E5 F5 G5 H5 C6 D6 E6 F6 G6");
            FirstRegionLevelDefinition level = Level("arena_s02_protect", "S02｜稳压器守护",
                "击倒两名进攻者并保护 F4 稳压器。稳压器耐久 12；侧锋相邻时每次公开造成 6 点耐久伤害，归零立即失败。",
                new GridPosition(1, 3), new[] { E("raider", 6, 3), E("stone_snare", 7, 1) },
                new[] { new LevelTerrainPlacement(5, 3, LevelTerrainKind.AetherObjective),
                    L(2, 3), L(3, 1), L(3, 5), H(4, 2), H(4, 4), L(6, 5) }, walkable,
                "北路切线，南路推离，中线占位护柱", "侧锋公开以稳压器为优先目标；约束助教限制玩家拦截路线。 ");
            return new CombatTestArenaScenario("arena_s02_protect", "稳压器守护", "S02｜稳压器守护",
                "保护柱可承受两次侧锋重击；从上下两路拦截、束缚或推离进攻者。", level,
                RestraintSpells, new[] { "G-T09", "G-T03", "G-T07", "G-T06" }, false, true);
        }

        private static CombatTestArenaScenario CreateEfficiencyPressureTest()
        {
            HashSet<GridPosition> walkable = Cells("C2 D2 E2 F2 G2 H2 B3 C3 D3 E3 F3 G3 H3 B4 C4 D4 E4 F4 G4 H4 B5 C5 D5 E5 F5 G5 H5 C6 D6 E6 F6 G6");
            FirstRegionLevelDefinition level = Level("arena_s03_efficiency", "S03｜短程压制评级",
                "击倒盾术与重弩。3 次主角回合内完成可保留效率奖励；超出阈值仍可正常胜利并获得基础奖励。",
                new GridPosition(1, 3), new[] { E("shieldguard", 5, 3), E("rune_arbalist", 7, 1) },
                new[] { L(2, 3), L(3, 1), L(3, 5), H(4, 2), H(4, 4), L(6, 5) }, walkable,
                "中路拆盾，上路钻入重弩死区，下路切线后包夹", "公开的 3 回合效率阈值鼓励主动换线；错过只取消额外奖励，不造成失败。 ");
            return new CombatTestArenaScenario("arena_s03_efficiency", "效率评级", "S03｜短程压制评级",
                "效率奖励阈值固定为 3 次主角回合；用位移、拉扯、烟幕和行动资源缩短接敌，而非退到远处放风筝。", level,
                CrossfireSpells, new[] { "G-T09", "G-T17", "G-T18", "G-T12" }, false, true);
        }

        private static FirstRegionLevelDefinition Level(string id, string name, string objective, GridPosition hero,
            IEnumerable<LevelEnemyPlacement> enemies, IEnumerable<LevelTerrainPlacement> terrain, HashSet<GridPosition> walkable,
            string grammar, string risk, bool elite = false, bool boss = false, int width = 10, int height = 7)
        {
            List<GridPosition> blocked = new List<GridPosition>();
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    if (!walkable.Contains(new GridPosition(x, y))) blocked.Add(new GridPosition(x, y));
            LevelEnemyPlacement[] placedEnemies = enemies.ToArray();
            LevelTerrainPlacement[] authoredTerrain = terrain.ToArray();
            LevelTerrainPlacement[] permanentWalls = PermanentWalls(id, walkable, width, height);
            return new FirstRegionLevelDefinition(id, name, objective, CombatObjectiveType.Elimination, 1, hero,
                FirstRegionFloorTheme.Courtyard, elite, boss, Array.Empty<string>(), placedEnemies, authoredTerrain.Concat(permanentWalls),
                new LevelSpaceContract(grammar, walkable.OrderBy(p => p.X).ThenBy(p => p.Y).Take(3).ToArray(), risk, "始终保留一条不依赖场地效果的推进路线。"),
                width, height, blocked);
        }

        private static LevelTerrainPlacement[] PermanentWalls(string id, HashSet<GridPosition> walkable, int width, int height)
        {
            // Boundary walls turn the already unusable perimeter into a readable room
            // without invalidating authored test routes or hiding their first-turn intent.
            int offset = Math.Abs(StableSeed(id)) % 3;
            GridPosition[] candidates =
            {
                new GridPosition(0, 0), new GridPosition(width - 1, 0),
                new GridPosition(0, 1 + offset), new GridPosition(width - 1, 1 + offset),
                new GridPosition(0, 4 + offset % 2), new GridPosition(width - 1, 4 + offset % 2),
                new GridPosition(0, height - 1), new GridPosition(width - 1, height - 1)
            };
            return candidates.Where(position => !walkable.Contains(position)).Distinct()
                .Select(position => new LevelTerrainPlacement(position.X, position.Y, LevelTerrainKind.PermanentWall)).ToArray();
        }

        private static int StableSeed(string value)
        {
            int seed = 17;
            foreach (char character in value ?? string.Empty) seed = unchecked(seed * 31 + character);
            return seed;
        }

        private static LevelEnemyPlacement E(string archetype, int x, int y) => new LevelEnemyPlacement(archetype, x, y);

        private static HashSet<GridPosition> Cells(string coordinates)
        {
            HashSet<GridPosition> result = new HashSet<GridPosition>();
            foreach (string token in coordinates.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int x = token[0] - 'A';
                int y = int.Parse(token.Substring(1)) - 1;
                result.Add(new GridPosition(x, y));
            }
            return result;
        }
    }
}
