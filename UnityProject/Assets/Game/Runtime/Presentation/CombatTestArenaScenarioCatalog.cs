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
        private static LevelTerrainPlacement M(int mechanismKind, int x, int y) => new LevelTerrainPlacement(x, y, LevelTerrainKind.TowerMechanism, mechanismKind);

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
            HashSet<GridPosition> walkable = Cells("C2 D2 E2 F2 G2 B3 C3 D3 E3 F3 G3 B4 C4 D4 E4 F4 G4 B5 C5 D5 E5 F5 G5 C6 D6 E6 F6 G6");
            string id = "arena_l_" + suffix;
            FirstRegionLevelDefinition level = Level(id, "L｜" + name,
                "在同一紧凑场地验证整组术式的射程、条件、位移、场地覆盖与友伤预览。",
                new GridPosition(2, 3), new[] { E("shieldguard", 4, 3), E("pyromancer", 6, 1), E("barrier_mender", 6, 4) },
                new[] { L(3, 2), L(3, 4), H(5, 2), H(5, 4), W(4, 1), W(4, 5), C(6, 3) }, walkable,
                "近身目标、远距目标与支援目标同时公开", "掩体、水区和晶簇提供通用条件，不绑定单关机关");
            return new CombatTestArenaScenario(id, name, "L｜" + name,
                "25 格紧凑场地；近、中、远目标及通用场地同时可用。", level, spells,
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
            HashSet<GridPosition> walkable = Cells("D2 E2 F2 G2 C3 D3 E3 G3 B4 C4 D4 E4 F4 G4 C5 D5 E5 G5 D6 E6 F6 G6");
            FirstRegionLevelDefinition level = Level("arena_n01_flank", "N01｜中庭侧锋对练",
                "击倒盾术生与侧锋生。重掩墙收窄了中线，迫使更快接敌。",
                new GridPosition(1, 3), new[] { E("shieldguard", 5, 3), E("raider", 6, 5) },
                new[] { L(2, 3), L(4, 5), L(4, 1), L(6, 1), H(5, 2), H(5, 4) }, walkable,
                "中线压盾，北截侧锋，南线绕侧", "盾术生守中，侧锋从北线逼近；收窄后两侧接敌更快。 ");
            return new CombatTestArenaScenario("arena_n01_flank", "侧锋对练", "N01｜中庭侧锋对练",
                "22 格可走区；可试罗盘拉近→火种＋协同标记→武器命中推位留火。", level, FlankSpells,
                new[] { "G-T09", "G-T07", "G-T01", "G-T13" });
        }

        private static CombatTestArenaScenario CreateTrackerDrill()
        {
            HashSet<GridPosition> walkable = Cells("C2 D2 E2 F2 G2 B3 C3 D3 E3 G3 B4 C4 D4 E4 G4 B5 C5 D5 E5 F5 G5 C6 D6 E6 F6 G6");
            FirstRegionLevelDefinition level = Level("arena_n02_tracker", "N02｜宿舍外寻迹测试",
                "击倒盾术生与寻迹兽。收窄后灯藤遮断占比更高，走位更紧凑。",
                new GridPosition(1, 3), new[] { E("shieldguard", 6, 5), E("tether_hound", 6, 1) },
                new[] { L(2, 3), L(2, 5), L(2, 1), L(5, 5), L(5, 1), V(4, 2), V(4, 3), V(4, 4), H(5, 2), H(5, 3) }, walkable,
                "藤带拆压，北截盾术生，南诱寻迹兽", "灯藤延迟视线与追击；北、南干路依旧可走。 ");
            return new CombatTestArenaScenario("arena_n02_tracker", "寻迹测试", "N02｜宿舍外寻迹测试",
                "26 格可走区；可试诱导灯只引开寻迹兽，再用灯藤高移动成本与缚位框维持拆分。", level, ControlSpells,
                new[] { "G-T15", "G-T10", "G-T03", "G-T09" });
        }

        private static CombatTestArenaScenario CreateBarrierDrill()
        {
            HashSet<GridPosition> walkable = Cells("C2 D2 E2 F2 G2 B3 C3 D3 F3 G3 B4 C4 D4 F4 G4 B5 C5 D5 E5 F5 G5 C6 D6 E6 F6 G6");
            FirstRegionLevelDefinition level = Level("arena_n03_barrier", "N03｜护障课程示范",
                "击倒补盾助教与盾术生。收窄后排布更密，维护线更短。",
                new GridPosition(1, 3), new[] { E("barrier_mender", 6, 5), E("shieldguard", 5, 3) },
                new[] { L(2, 3), L(3, 5), L(3, 1), L(6, 2), H(4, 2), H(4, 3) }, walkable,
                "北拆支援，中继切线，南侧压盾", "护障先于盾术生行动；紧凑排布下维护线更易被切断。 ");
            return new CombatTestArenaScenario("arena_n03_barrier", "护障示范", "N03｜护障课程示范",
                "26 格可走区；可试移至 E5→在 G5 建墙，花满 3 AP 换一次公开续盾断档。", level, ControlSpells,
                new[] { "G-T08", "G-T07", "G-T09", "G-T04" });
        }

        private static CombatTestArenaScenario CreateFireDrill()
        {
            HashSet<GridPosition> walkable = Cells("C2 D2 E2 F2 G2 B3 C3 D3 E3 F3 B4 C4 D4 E4 F4 G4 B5 C5 D5 E5 G5 C6 D6 E6 F6 G6");
            FirstRegionLevelDefinition level = Level("arena_n04_fire", "N04｜锅炉房火线演练",
                "击倒火矢生与侧锋生。收窄后火线更快覆盖全场，浅水区选择更关键。",
                new GridPosition(1, 3), new[] { E("pyromancer", 6, 1), E("raider", 6, 4) },
                new[] { L(2, 2), L(2, 4), H(4, 2), H(4, 4), W(3, 3), W(4, 3), W(5, 3) }, walkable,
                "水带越线，掩体逼近，侧廊追火矢", "浅水与火场互相覆盖；紧凑场地中水带安全区更宝贵。 ");
            return new CombatTestArenaScenario("arena_n04_fire", "火线演练", "N04｜锅炉房火线演练",
                "26 格可走区；可试火路覆盖水带→冷凝器清通道→封装筒续火→导位器拉回侧锋。", level, FireSpells,
                new[] { "G-T11", "G-T07", "G-T09", "F-T01" });
        }

        private static CombatTestArenaScenario CreateArbalistDrill()
        {
            HashSet<GridPosition> walkable = Cells("D2 E2 F2 G2 B3 C3 D3 E3 F3 B4 C4 D4 F4 G4 B5 C5 D5 E5 F5 G5 C6 D6 E6 F6 G6");
            FirstRegionLevelDefinition level = Level("arena_n05_arbalist", "N05｜回廊背弩生校准",
                "击倒背弩生与侧锋生。收窄后重柜迫近，远程死区更易进入。",
                new GridPosition(1, 3), new[] { E("rune_arbalist", 6, 1), E("raider", 6, 4) },
                new[] { L(2, 2), L(2, 4), L(5, 2), H(4, 3), H(6, 3), H(6, 5) }, walkable,
                "柜后换线，近身压弩，侧锋封退路", "紧凑回廊中背弩生死区距离更短，柜后换线收益更高。 ");
            return new CombatTestArenaScenario("arena_n05_arbalist", "背弩生校准", "N05｜回廊背弩生校准",
                "25 格可走区；可试 D4 拆 E4 重柜后越线，再贴入背弩生 1 格死区迫其公开退距。", level, ArbalistSpells,
                new[] { "G-T07", "G-T08", "G-T14", "G-T15" });
        }

        private static CombatTestArenaScenario CreateRestraintDrill()
        {
            HashSet<GridPosition> walkable = Cells("C2 D2 E2 F2 G2 B3 C3 D3 E3 F3 G3 B4 C4 D4 E4 F4 G4 B5 C5 D5 F5 G5 C6 D6 E6 F6 G6");
            FirstRegionLevelDefinition level = Level("arena_n06_restraint", "N06｜石索约束考核",
                "击倒拴索助教与侧锋生。收窄后晶簇争夺更激烈，通行线更少。",
                new GridPosition(1, 3), new[] { E("stone_snare", 5, 2), E("raider", 6, 4) },
                new[] { L(2, 3), L(3, 1), L(4, 4), L(3, 2), H(5, 4), C(4, 3) }, walkable,
                "晶体争位，窄口抗缚，侧路换线", "石索控制通路、侧锋贴身；紧凑场地中晶体争夺更关键。 ");
            return new CombatTestArenaScenario("arena_n06_restraint", "约束考核", "N06｜石索约束考核",
                "27 格可走区；可试 D4 承受石索→罗盘拉拴索助教到 D3→灼缚解离后立即换线。", level, RestraintSpells,
                new[] { "G-T03", "G-T09", "G-T13", "G-T06" });
        }

        private static CombatTestArenaScenario CreateMaintenanceDrill()
        {
            HashSet<GridPosition> walkable = Cells("C2 D2 E2 F2 G2 B3 C3 D3 F3 G3 B4 C4 D4 E4 F4 G4 B5 C5 D5 E5 G5 C6 D6 E6 F6 G6");
            FirstRegionLevelDefinition level = Level("arena_n07_maintenance", "N07｜档案廊维护链",
                "击倒补盾助教、提灯巡查与盾术生。收窄后三敌排布更密，优先序更紧迫。",
                new GridPosition(1, 3), new[] { E("shieldguard", 5, 3), E("barrier_mender", 6, 5), E("lantern_revealer", 6, 1) },
                new[] { L(2, 2), L(2, 4), L(6, 2), H(4, 2), H(4, 4), V(5, 1), V(5, 5) }, walkable,
                "中路断援，北压显影，南追维护", "三敌分处不同攻击线；紧凑排布下优先序更紧迫。 ");
            return new CombatTestArenaScenario("arena_n07_maintenance", "维护链", "N07｜档案廊维护链",
                "26 格可走区；可试罗盘将盾术生拉出 4 格维护范围，再以烙印＋贴身占位拆开三种意图。", level, ControlSpells,
                new[] { "G-T08", "G-T09", "G-T14", "G-T15" });
        }

        private static CombatTestArenaScenario CreateContainmentDrill()
        {
            HashSet<GridPosition> walkable = Cells("C2 D2 E2 F2 G2 B3 C3 D3 E3 F3 G3 B4 C4 D4 E4 F4 B5 C5 D5 E5 F5 G5 C6 D6 E6 F6 G6");
            FirstRegionLevelDefinition level = Level("arena_n08_containment", "N08｜器材库收束演练",
                "击倒寻迹兽、替身偶与拴索助教。收窄后三线更密，闭合更快。",
                new GridPosition(1, 3), new[] { E("tether_hound", 6, 1), E("sigil_mauler", 6, 2), E("stone_snare", 6, 5) },
                new[] { L(2, 2), L(2, 4), L(4, 1), L(4, 5), H(4, 3), H(6, 3), C(3, 3) }, walkable,
                "中轴卡偶，上下拆缚，晶体换资源", "上下两路施加束缚，中轴重偶负责惩罚停留；紧凑场地中更易形成交叉。 ");
            return new CombatTestArenaScenario("arena_n08_containment", "收束演练", "N08｜器材库收束演练",
                "27 格可走区；北路进位后用缚位框截住寻迹兽一轮，再以震测铅锤推开中轴重偶并撤出夹击线。", level, RestraintSpells,
                new[] { "G-T03", "G-T07", "G-T09", "G-T17" });
        }

        private static CombatTestArenaScenario CreateCrossfireDrill()
        {
            HashSet<GridPosition> walkable = Cells("C2 D2 E2 F2 G2 B3 C3 D3 E3 F3 B4 C4 D4 E4 F4 G4 B5 C5 E5 F5 G5 C6 D6 E6 F6 G6");
            FirstRegionLevelDefinition level = Level("arena_n09_crossfire", "N09｜终段交叉火线",
                "击倒盾术生、火矢与侧锋生。收窄后火线覆盖密度更高，掩体破坏影响更大。",
                new GridPosition(1, 3), new[] { E("shieldguard", 5, 3), E("pyromancer", 6, 1), E("raider", 6, 5) },
                new[] { L(2, 2), L(2, 4), L(5, 1), H(4, 2), H(4, 4), H(6, 3), W(3, 3), W(4, 3) }, walkable,
                "水带抢中，拆柜开线，侧路分敌", "盾术生占中、火矢控远、侧锋封退；紧凑场地下每次破坏都改变全场态势。 ");
            return new CombatTestArenaScenario("arena_n09_crossfire", "交叉火线", "N09｜终段交叉火线",
                "26 格可走区；火路覆盖水带并压住中轴盾术生，冷凝出入口后可追击抢效率，或以熔障爆点削柜后择一路解构推进。", level, CrossfireSpells,
                new[] { "G-T11", "G-T08", "G-T09", "G-T01" });
        }

        private static CombatTestArenaScenario CreateEliteMaintenance()
        {
            HashSet<GridPosition> walkable = Cells("C2 D2 E2 F2 G2 B3 C3 D3 E3 F3 G3 B4 C4 D4 E4 F4 G4 B5 C5 D5 E5 F5 G5 C6 D6 E6 F6 G6");
            FirstRegionLevelDefinition level = Level("arena_e01_maintenance", "E01｜刻阵工坊断供",
                "击倒划线教官、补盾助教与替身偶。收窄后维护线更短，切线窗口更紧。",
                new GridPosition(1, 3), new[] { E("elite_vanguard", 5, 3), E("barrier_mender", 6, 1), E("sigil_mauler", 6, 5) },
                new[] { L(2, 2), L(2, 4), L(6, 2), H(4, 2), H(4, 4), C(5, 1) }, walkable,
                "断供换线，双侧破势，晶体抢窗", "划线教官占中、补盾助教续盾、替身偶封侧；紧凑场地中三角维护更易被切断。 ",
                true);
            return new CombatTestArenaScenario("arena_e01_maintenance", "工坊断供", "E01｜刻阵工坊断供",
                "27 格可走区；罗盘可把划线教官拉出维护距离，再以 U03 附着主手攻击、M09 重击完成断供；也可破晶或削柜换线。", level, PressureSpells,
                new[] { "G-T08", "G-T09", "G-T14", "G-T16" }, true);
        }

        private static CombatTestArenaScenario CreateEliteCrosslock()
        {
            HashSet<GridPosition> walkable = Cells("C2 D2 E2 F2 G2 B3 C3 D3 E3 F3 G3 B4 C4 D4 F4 G4 B5 C5 D5 E5 F5 G5 C6 D6 E6 F6 G6");
            FirstRegionLevelDefinition level = Level("arena_e02_crosslock", "E02｜塔前对角封锁",
                "击倒划线教官、背弩生与拴索助教。收窄后封锁线交叉更快，外廊更短。",
                new GridPosition(1, 3), new[] { E("elite_vanguard", 5, 3), E("rune_arbalist", 6, 1), E("stone_snare", 5, 4) },
                new[] { L(2, 2), L(2, 4), L(5, 1), L(5, 5), H(4, 3), H(6, 2), H(6, 4) }, walkable,
                "双廊切角，中轴诱敌，拆柜反射线", "背弩生与拴索助教覆盖相反外廊；紧凑场地中划线教官覆盖范围更大。 ",
                true);
            return new CombatTestArenaScenario("arena_e02_crosslock", "对角封锁", "E02｜塔前对角封锁",
                "27 格可走区；北压背弩生、南拆石索或中轴诱划线教官后换线。", level, CrossfireSpells,
                new[] { "G-T07", "G-T08", "G-T18", "G-T15" }, true);
        }

        private static CombatTestArenaScenario CreateElitePressure()
        {
            HashSet<GridPosition> walkable = Cells("C2 D2 E2 F2 G2 B3 C3 D3 E3 F3 G3 B4 C4 D4 E4 F4 G4 B5 C5 D5 E5 F5 G5 C6 D6 E6 F6 G6");
            FirstRegionLevelDefinition level = Level("arena_e03_pressure", "E03｜楔角稳压测试",
                "击倒楔角与火矢生。收窄后冲压线覆盖范围更大，卸压窗口更短。",
                new GridPosition(1, 3), new[] { E("breach_ram", 6, 2), E("pyromancer", 6, 1) },
                new[] { W(3, 1), W(3, 2), W(3, 3), W(3, 4), W(3, 5), V(5, 1), V(5, 2), V(5, 4), V(5, 5), C(6, 3), L(2, 5) }, walkable,
                "水沟冷却，藤带藏线，撞晶开窗", "楔角锁定主角并按五点移动预算冲压；紧凑场地中卸压路线选择更关键。 ",
                true);
            return new CombatTestArenaScenario("arena_e03_pressure", "楔角稳压", "E03｜楔角稳压测试",
                "27 格可走区；诱撞晶簇、借水冷却或从南侧干路等卸压。", level, ControlSpells,
                new[] { "G-T09", "G-T10", "G-T01", "G-T13" }, true);
        }

        private static CombatTestArenaScenario CreateAcademyCoreBoss()
        {
            HashSet<GridPosition> walkable = Cells("C2 D2 E2 F2 G2 H2 B3 C3 D3 E3 F3 G3 H3 I3 J3 B4 C4 D4 E4 F4 G4 H4 B5 C5 D5 E5 F5 G5 H5 B6 C6 D6 E6 F6 G6 H6 C7 D7 E7 F7 G7 H7");
            FirstRegionLevelDefinition level = Level("arena_b01_core", "B01｜学院封存塔核心",
                "击倒拦在必经之路上的塔之守卫。三组塔内机关在阶段〇逐组放行，放行前不提供任何效果；拆掉已放行的机关即切断维护链。",
                new GridPosition(1, 3), new[] { E("core_overseer", 5, 3) },
                new[] { L(2, 3), L(3, 5), L(8, 1), L(8, 5), H(4, 2), H(6, 2), H(4, 4), H(6, 4), C(7, 3),
                    M(FirstRegionLevelCatalog.TowerMechanismWard, 1, 1),
                    M(FirstRegionLevelCatalog.TowerMechanismReveal, 9, 3),
                    M(FirstRegionLevelCatalog.TowerMechanismPress, 8, 6) }, walkable,
                "中心核心，三道机关门槛，双缺口换序", "收窄后两侧外廊更短；三组机关耐久公开，可先拆以切断维护链。 ",
                false, true, 11, 8);
            return new CombatTestArenaScenario("arena_b01_core", "封存塔核心", "B01｜学院封存塔核心",
                "42 格可走区；收窄后往返距离更短，拆链顺序窗口更紧。", level, FinisherSpells,
                new[] { "G-T08", "G-T09", "G-T12", "G-T16" }, true);
        }

        private static CombatTestArenaScenario CreateReactionPressureTest()
        {
            HashSet<GridPosition> walkable = Cells("C2 D2 E2 F2 G2 B3 C3 D3 E3 F3 G3 B4 C4 D4 E4 F4 G4 B5 C5 D5 E5 F5 G5 C6 D6 E6 F6 G6");
            FirstRegionLevelDefinition level = Level("arena_s01_reaction", "S01｜警戒火线反应",
                "击倒背弩生与侧锋。收窄后警戒射界覆盖比例更高，钻死区收益更显著。",
                new GridPosition(1, 3), new[] { E("rune_arbalist", 6, 2), E("raider", 5, 3) },
                new[] { L(2, 3), L(2, 1), L(2, 5), H(4, 2), H(4, 4), L(6, 1), L(6, 5) }, walkable,
                "中央诱导友伤，上下切线，相邻钻入死区", "高风险移动格直接显示警戒结果；紧凑场地下死区距离更短。 ");
            return new CombatTestArenaScenario("arena_s01_reaction", "警戒火线", "S01｜警戒火线反应",
                "25 格可走区；紧凑场地下可更快钻入背弩生死区诱导友伤。", level,
                FlankSpells, new[] { "G-T09", "G-T10", "G-T07", "G-T01" }, false, true);
        }

        private static CombatTestArenaScenario CreateProtectionPressureTest()
        {
            HashSet<GridPosition> walkable = Cells("C2 D2 E2 F2 G2 B3 C3 D3 E3 F3 G3 B4 C4 D4 E4 F4 G4 B5 C5 D5 E5 F5 G5 C6 D6 E6 F6 G6");
            FirstRegionLevelDefinition level = Level("arena_s02_protect", "S02｜稳压器守护",
                "击倒两名进攻者并保护 F4 稳压器。收窄后拦截路线更短，进攻者更接近目标。",
                new GridPosition(1, 3), new[] { E("raider", 6, 3), E("stone_snare", 6, 1) },
                new[] { new LevelTerrainPlacement(5, 3, LevelTerrainKind.AetherObjective),
                    L(2, 3), L(3, 1), L(3, 5), H(4, 2), H(4, 4), L(6, 5) }, walkable,
                "北路切线，南路推离，中线占位护柱", "侧锋公开以稳压器为优先目标；紧凑场地下进攻者更早到达威胁位置。 ");
            return new CombatTestArenaScenario("arena_s02_protect", "稳压器守护", "S02｜稳压器守护",
                "25 格可走区；拦截路线更短，保护柱承受压力的节奏更快。", level,
                RestraintSpells, new[] { "G-T09", "G-T03", "G-T07", "G-T06" }, false, true);
        }

        private static CombatTestArenaScenario CreateEfficiencyPressureTest()
        {
            HashSet<GridPosition> walkable = Cells("C2 D2 E2 F2 G2 B3 C3 D3 E3 F3 G3 B4 C4 D4 E4 F4 G4 B5 C5 D5 E5 F5 G5 C6 D6 E6 F6 G6");
            FirstRegionLevelDefinition level = Level("arena_s03_efficiency", "S03｜短程压制评级",
                "击倒盾术生与背弩生。收窄后接敌更直接，效率阈值下走位容错更小。",
                new GridPosition(1, 3), new[] { E("shieldguard", 5, 3), E("rune_arbalist", 6, 1) },
                new[] { L(2, 3), L(3, 1), L(3, 5), H(4, 2), H(4, 4), L(6, 5) }, walkable,
                "中路拆盾，上路钻入背弩生死区，下路切线后包夹", "收窄后 3 回合效率阈值压力更大；紧凑场地中位移收益更高。 ");
            return new CombatTestArenaScenario("arena_s03_efficiency", "效率评级", "S03｜短程压制评级",
                "25 格可走区；收窄场地迫使更快接敌，位移与换线收益更高。", level,
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
            // Place indestructible wall tiles on every blocked cell adjacent to the
            // walkable bounding box, creating a solid visual wall around the arena.
            if (walkable.Count == 0) return System.Array.Empty<LevelTerrainPlacement>();

            int minX = width, maxX = 0, minY = height, maxY = 0;
            foreach (GridPosition pos in walkable)
            {
                if (pos.X < minX) minX = pos.X;
                if (pos.X > maxX) maxX = pos.X;
                if (pos.Y < minY) minY = pos.Y;
                if (pos.Y > maxY) maxY = pos.Y;
            }

            HashSet<GridPosition> walls = new HashSet<GridPosition>();

            // Left column (x = minX - 1): wall every non-walkable cell
            if (minX > 0)
            {
                int x = minX - 1;
                for (int y = 0; y < height; y++)
                {
                    GridPosition p = new GridPosition(x, y);
                    if (!walkable.Contains(p)) walls.Add(p);
                }
            }

            // Right column (x = maxX + 1): wall every non-walkable cell
            if (maxX < width - 1)
            {
                int x = maxX + 1;
                for (int y = 0; y < height; y++)
                {
                    GridPosition p = new GridPosition(x, y);
                    if (!walkable.Contains(p)) walls.Add(p);
                }
            }

            // Top row (y = minY - 1): wall blocks spanning the full walkable width
            if (minY > 0)
            {
                int y = minY - 1;
                for (int x = minX; x <= maxX; x++)
                {
                    GridPosition p = new GridPosition(x, y);
                    if (!walkable.Contains(p)) walls.Add(p);
                }
            }

            // Bottom row (y = maxY + 1): wall blocks spanning the full walkable width
            if (maxY < height - 1)
            {
                int y = maxY + 1;
                for (int x = minX; x <= maxX; x++)
                {
                    GridPosition p = new GridPosition(x, y);
                    if (!walkable.Contains(p)) walls.Add(p);
                }
            }

            return walls.Select(p => new LevelTerrainPlacement(p.X, p.Y, LevelTerrainKind.PermanentWall)).ToArray();
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
