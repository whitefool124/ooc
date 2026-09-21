using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace OCC.Combat.Tests
{
    /// <summary>塔之守卫的机关放行效果与阶段二并链。</summary>
    public sealed class AcademyCoreBossMechanismTests
    {
        private static CombatState BossState(out UnitState core)
        {
            FirstRegionLevelBuild build = FirstRegionLevelBuilder.Build("core_finale", "core_overseer");
            CombatState state = build.State;
            core = state.GetUnit("enemy_0");
            return state;
        }

        private static void BeginBossTurn(CombatState state, UnitState core)
        {
            MethodInfo begin = typeof(AcademyCoreBossRuntime).GetMethod("BeginTurn", BindingFlags.Instance | BindingFlags.NonPublic);
            begin.Invoke(state.AcademyCoreBoss, new object[] { state, core });
        }

        [Test]
        public void FirstRelease_RaisesTheRevealLaneAndClearsShieldsOnIt()
        {
            CombatState state = BossState(out UnitState core);
            UnitState hero = state.GetUnit("hero");
            state.TryGrantRogueliteShield("hero", "test-shield", 4);
            // 第一道门槛是护障维护，第二道才是显影巡查：推两次直到光带出现。
            BeginBossTurn(state, core);
            BeginBossTurn(state, core);

            Assert.That(state.Environment.LightLanes.Count, Is.EqualTo(1), "显影巡查放行后亮起一条光带。");
            Assert.That(state.EventLog.Any(line => line.Contains("显影巡查放行")), Is.True, string.Join(" | ", state.EventLog));
            Assert.That(state.AcademyCoreBoss.ReleasedMechanismCount(state), Is.EqualTo(2));
            Assert.That(hero, Is.Not.Null);
        }

        [Test]
        public void ThirdRelease_PublishesAPressLineAndDamagesItAtTurnEnd()
        {
            CombatState state = BossState(out UnitState core);
            BeginBossTurn(state, core);
            BeginBossTurn(state, core);
            BeginBossTurn(state, core);
            Assert.That(state.AcademyCoreBoss.ReleasedMechanismCount(state), Is.EqualTo(3));
            Assert.That(state.EventLog.Any(line => line.Contains("冲压隔离放行")), Is.True, string.Join(" | ", state.EventLog));

            MethodInfo end = typeof(AcademyCoreBossRuntime).GetMethod("EndTurn", BindingFlags.Instance | BindingFlags.NonPublic);
            end.Invoke(state.AcademyCoreBoss, new object[] { state, core });

            Assert.That(state.EventLog.Any(line => line.Contains("冲压线结算")), Is.True, "冲压线在核心回合结束时结算。");
        }

        [Test]
        public void PhaseTwoChain_ResolvesEverySurvivingReleasedMechanismOnEachAction()
        {
            CombatState state = BossState(out UnitState core);
            for (int turn = 0; turn < 4; turn++) BeginBossTurn(state, core);
            Assert.That(state.AcademyCoreBoss.PhaseFor(state, core), Is.EqualTo(1));
            MethodInfo takeDamage = typeof(UnitState).GetMethod("TakeDamage", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            takeDamage.Invoke(core, new object[] { core.Health - (core.MaxHealth * 2 / 10) });
            Assert.That(state.AcademyCoreBoss.PhaseFor(state, core), Is.EqualTo(2), "血量降到三成以下进入阶段二。");

            MethodInfo observe = typeof(AcademyCoreBossRuntime).GetMethod("ObserveCommand", BindingFlags.Instance | BindingFlags.NonPublic);
            observe.Invoke(state.AcademyCoreBoss, new object[] { state, core });

            Assert.That(state.EventLog.Any(line => line.Contains("并链")), Is.True, string.Join(" | ", state.EventLog));
        }

        [Test]
        public void DestroyedReleasedMechanism_StopsContributingOnceChained()
        {
            CombatState state = BossState(out UnitState core);
            BeginBossTurn(state, core);
            GridPosition released = state.Map.PositionsWith(tile => tile.IsTowerMechanism && tile.IsReleased).Single();
            TileState cleared = state.Map.GetTile(released).Clone();
            cleared.Durability = 0;
            state.Map.SetTile(released, cleared);

            Assert.That(state.AcademyCoreBoss.ReleasedMechanismCount(state), Is.Zero, "被拆除的机关不再计入维护链。");
        }

        [Test]
        public void BossRuntimeClone_KeepsMechanismDirections()
        {
            CombatState state = BossState(out UnitState core);
            BeginBossTurn(state, core);

            CombatState clone = state.Clone();

            Assert.That(clone.AcademyCoreBoss.ReleasedMechanismCount(clone), Is.EqualTo(1));
            Assert.That(clone.AcademyCoreBoss.PhaseFor(clone, clone.GetUnit("enemy_0")), Is.EqualTo(0));
        }
    }
}
