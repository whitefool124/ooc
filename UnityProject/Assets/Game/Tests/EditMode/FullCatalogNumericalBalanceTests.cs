using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class FullCatalogNumericalBalanceTests
    {
        private static IEnumerable<Dictionary<string,string>> Rows(string file)
        {
            var lines=File.ReadAllLines(Path.GetFullPath(Path.Combine(Application.dataPath,"../../Worldbuilding/数据表",file)));
            string[] Parse(string line)=>Regex.Matches(line,"\"((?:[^\"]|\"\")*)\"(?=,|$)")
                .Cast<Match>().Select(m=>m.Groups[1].Value.Replace("\"\"","\"")).ToArray();
            var header=Parse(lines[0]);
            foreach(var line in lines.Skip(1).Where(value=>!string.IsNullOrWhiteSpace(value)))
            {
                var values=Parse(line);
                Assert.That(values.Length,Is.EqualTo(header.Length),file);
                yield return header.Select((name,i)=>new {name,value=values[i]}).ToDictionary(v=>v.name,v=>v.value);
            }
        }

        [Test]
        public void EveryPersonalSpellAndArtifactMatchesTheApprovedConfiguration()
        {
            var rows=Rows("OCC_技能配置表_v1.0.csv").ToDictionary(r=>r["技能ID"]);
            foreach(var spell in FireSpellCatalog.All)
            {
                var row=rows[spell.Id];
                Assert.That(spell.ActionPointCost,Is.EqualTo(int.Parse(row["AP"])),spell.Id);
                Assert.That(spell.ManaCost,Is.EqualTo(int.Parse(row["魔力"])),spell.Id);
                Assert.That(spell.Cooldown,Is.EqualTo(int.Parse(Regex.Match(row["冷却"],"^[0-9]+").Value)),spell.Id);
                Assert.That(FireSpellCardCopyCatalog.TryGet(spell.Id,out string copy),Is.True,spell.Id);
                Assert.That(copy,Is.EqualTo(row["卡面文案"]),spell.Id);
            }
            foreach(var artifact in ArtifactCatalog.All)
            {
                var row=rows["ARTIFACT-"+artifact.Id];
                Assert.That(artifact.MaximumUses,Is.EqualTo(int.Parse(row["次数"])),artifact.Id);
                Assert.That(artifact.ActionPointCost,Is.EqualTo(int.Parse(row["AP"])),artifact.Id);
                Assert.That(artifact.ManaCost,Is.EqualTo(int.Parse(row["魔力"])),artifact.Id);
            }
        }

        [Test]
        public void AllSeventeenEnemyHealthBaselinesMatchBothTablesAndActualRogueliteUnits()
        {
            var rows=Rows("OCC_学院敌人数据表_v1.0.csv").ToDictionary(r=>r["ID"]);
            var units=Rows("OCC_单位配置表_v1.0.csv").ToDictionary(r=>r["单位ID"]);
            var service=new OCC.Combat.Roguelite.RogueAcademyContentService();
            Assert.That(EnemyArchetypes.All.Count,Is.EqualTo(17));
            foreach(var enemy in EnemyArchetypes.All)
            {
                int hp=int.Parse(rows[enemy.Id]["生命基线"]);
                Assert.That(enemy.MaxHealth,Is.EqualTo(hp),enemy.Id);
                Assert.That(int.Parse(units[enemy.Id]["生命"]),Is.EqualTo(hp),enemy.Id);
                var hero=new UnitState("hero",true,new GridPosition(0,0));
                var unit=new UnitState("enemy",false,new GridPosition(2,2));enemy.Apply(unit);
                var state=new CombatState(new GridMap(4,4),new[]{hero,unit});state.ConfigureRuleset(CombatRuleset.Roguelite);
                service.ApplyEnemyBaseline(state,unit);
                Assert.That(unit.MaxHealth,Is.EqualTo(hp),enemy.Id);
                Assert.That(unit.Shield,Is.EqualTo(int.Parse(rows[enemy.Id]["初始护盾"])),enemy.Id);
            }
        }

        [Test]
        public void FreeDamageCannotSkipBossThresholdsBeforeItsFirstTurn()
        {
            var hero=new UnitState("hero",true,new GridPosition(0,1));
            var core=new UnitState("core",false,new GridPosition(3,1));EnemyArchetypes.Get("core_overseer").Apply(core);
            var state=new CombatState(new GridMap(5,3),new[]{hero,core});state.ConfigureRuleset(CombatRuleset.Roguelite);
            state.AttachAcademyCoreBoss(new AcademyCoreBossRuntime());
            CombatResolver.BeginTurn(state,hero.Id);
            var battle=new ArtifactBattleState(state);int health=core.Health;
            var target=ArtifactTarget.Unit(core.Id,core.Position);
            Assert.That(ArtifactEngine.Preview(battle,hero.Id,ArtifactCatalog.FortuneSeal,target,2).CanCommit,Is.True);
            ArtifactEngine.Execute(battle,hero.Id,ArtifactCatalog.FortuneSeal,target,2);
            ArtifactEngine.Execute(battle,hero.Id,ArtifactCatalog.FortuneSeal,target,1);
            Assert.That(core.Health,Is.EqualTo(health));
            Assert.That(state.AcademyCoreBoss.PhaseFor(state,core),Is.Zero);
            Assert.That(state.Clone().GetUnit(core.Id).IsSuperArmored,Is.True);
            for(int i=0;i<4;i++)CombatResolver.BeginTurn(state,core.Id);
            Assert.That(core.IsSuperArmored,Is.False);
            CombatResolver.BeginTurn(state,hero.Id);
            ArtifactEngine.Execute(battle,hero.Id,ArtifactCatalog.FortuneSeal,target,1);
            Assert.That(core.Health,Is.LessThan(health));
        }

        [Test]
        public void MenderDoesNotPublishAnIllegalHealAcrossTheSharedHeavyStructure()
        {
            var hero=new UnitState("hero",true,new GridPosition(4,2));
            var mender=new UnitState("mender",false,new GridPosition(1,1));EnemyArchetypes.Get("barrier_mender").Apply(mender);
            var ally=new UnitState("ally",false,new GridPosition(3,1));EnemyArchetypes.Get("shieldguard").Apply(ally);
            var map=new GridMap(5,3);map.SetTile(new GridPosition(2,1),new TileState{Cover=CoverType.Heavy,Durability=24});
            var state=new CombatState(map,new[]{hero,mender,ally});state.ConfigureRuleset(CombatRuleset.Roguelite);
            CombatResolver.BeginTurn(state,mender.Id);
            Assert.That(state.HasLineOfSight(mender.Position,ally.Position),Is.False);
            var plans=new EnemyTurnPlanBook();
            var command=plans.GetExecutionCommand(state,mender,hero);
            Assert.That(command.Type==CombatCommandType.UseSkill&&command.TargetUnitId==ally.Id,Is.False);
            Assert.DoesNotThrow(()=>CombatResolver.Resolve(state,command));
        }

        [Test]
        public void ZeroTempoCardsConsumeTheirFiniteSetupAndGrantNoActionPoints()
        {
            var provider=new FireSpellTrainingRangeProvider();
            foreach(string id in new[]{"F-P-M17","F-P-U11","F-P-R17"})
            {
                var prepared=(FireSpellTrainingRangeCase)provider.Prepare(id);
                int ap=prepared.Combat.GetUnit("hero").ActionPoints;
                prepared.Execute();
                Assert.That(prepared.Combat.GetUnit("hero").ActionPoints,Is.EqualTo(ap),id);
                Assert.That(prepared.Preview().CanCommit,Is.False,id+" cannot reuse consumed setup");
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void RangedReflectGrantsItsWholeShieldOnceAndRespectsBreakStance(bool broken)
        {
            var prepared=(ArtifactTrainingRangeCase)new ArtifactTrainingRangeProvider().Prepare("G-T14");
            var hero=prepared.Combat.GetUnit("hero");
            prepared.Combat.ConfigureRuleset(CombatRuleset.Roguelite);
            prepared.Execute();
            if(broken) prepared.Combat.ApplyRogueliteBreakStance(hero.Id);
            int shield=hero.Shield;
            var attacker=prepared.Combat.GetUnit("range_normal");
            int durability=attacker.Health+attacker.Shield;
            Assert.That(prepared.Battle.ResolveIncomingRangedHit(hero.Id,attacker.Id,0).Steps,Is.Empty);
            Assert.That(prepared.Battle.ResolveIncomingRangedHit(hero.Id,attacker.Id,3).Steps,Is.Not.Empty);
            Assert.That(hero.Shield-shield,Is.EqualTo(broken?0:8));
            Assert.That(durability-attacker.Health-attacker.Shield,Is.EqualTo(8));
            Assert.That(prepared.Battle.ResolveIncomingRangedHit(hero.Id,attacker.Id,3).Steps,Is.Empty);
        }
    }
}
