from pathlib import Path
import json,csv,re
root=Path('E:/数据库/OCC_Codex'); task=root/'Worldbuilding/归档/2026-09-05_战斗演出真实性修复'
def edit(relative, transform):
 p=root/relative; backup=task/'before'/relative
 assert not backup.exists(),str(backup)
 backup.parent.mkdir(parents=True,exist_ok=True); backup.write_bytes(p.read_bytes())
 before=p.read_text(encoding='utf-8-sig'); after=transform(before); assert after!=before,relative
 p.write_text(after,encoding='utf-8')
def replace(s,old,new):
 assert s.count(old)==1,(old[:90],s.count(old))
 return s.replace(old,new)
doc=json.loads((task/'master_before_code.json').read_text(encoding='utf-8-sig'))['data']['document']
assert '12.6 结算来源与延迟触发表现' in doc['content']
edit('Worldbuilding/策划案/OCC_项目总策划案_v1.0.md',lambda s:doc['content'].rstrip()+'\n')
def runtime(s):
 s=replace(s,'        public FireSpellExecution(FireSpellPreview preview, IEnumerable<FireSpellResultStep> steps) { Preview = preview; Steps = steps.ToArray(); }','''        public string SourceUnitId { get; }
        public GridPosition SourcePosition { get; }
        public bool IsTriggered { get; }

        public FireSpellExecution(FireSpellPreview preview, IEnumerable<FireSpellResultStep> steps,
            string sourceUnitId, GridPosition sourcePosition, bool isTriggered = false)
        {
            if (string.IsNullOrEmpty(sourceUnitId)) throw new ArgumentException("A fire execution requires its actual source.", nameof(sourceUnitId));
            Preview = preview; Steps = steps.ToArray();
            SourceUnitId = sourceUnitId; SourcePosition = sourcePosition; IsTriggered = isTriggered;
        }''')
 s=replace(s,'return new FireSpellExecution(preview, steps);','return new FireSpellExecution(preview, steps, source.Id, sourceOrigin);')
 methods=[('TriggerWeaponAttack','source','target.Position, target.Id'),('TriggerWeaponAttackAt','source','targetCell, null'),('TriggerIncomingAdjacentAttack','target','attacker.Position, attacker.Id'),('TriggerMarkedTargetMove','source','destination, moving.Id'),('TriggerEnemyEntry','source','entering.Position, entering.Id')]
 for name,unit,target in methods:
  start=s.index('        public static IReadOnlyList<FireSpellExecution> '+name+'(')
  end=s.find('        public static ',start+10)
  section=s[start:end]
  section=replace(section,'                List<FireSpellResultStep> steps = new List<FireSpellResultStep>();',f'                GridPosition sourceOrigin = {unit}.Position;\n                List<FireSpellResultStep> steps = new List<FireSpellResultStep>();')
  # Pursuit changes the source before building its result; the snapshot is captured above.
  section=replace(section,f'new FireSpellExecution(TriggerPreview(effect.Spell, {target}), steps)',f'new FireSpellExecution(TriggerPreview(effect.Spell, {target}), steps, {unit}.Id, sourceOrigin, true)')
  s=s[:start]+section+s[end:]
 return s
edit('UnityProject/Assets/Game/Runtime/Combat/FireSpellRuntime.cs',runtime)
def publisher(s):
 s=replace(s,'IReadOnlyList<GridPosition> targetCells);','IReadOnlyList<GridPosition> targetCells, bool isTriggered = false);')
 old='''                FireSpellResultStep firstStep = execution.Steps.FirstOrDefault();
                UnitState stepTarget = string.IsNullOrEmpty(firstStep.TargetId)
                    ? null : state.GetUnit(firstStep.TargetId);
                if (spell != null)
                    sink?.NotifyFireSpell(spell, stepTarget?.Position ??
                        execution.Preview.Cells.FirstOrDefault(), execution.Preview.Cells);'''
 new='''                if (spell != null)
                    sink?.NotifyFireSpell(spell, execution.SourcePosition,
                        execution.Preview.Cells, execution.IsTriggered);'''
 s=replace(s,old,new)
 return s.replace('using System.Linq;\n','')
edit('UnityProject/Assets/Game/Runtime/Presentation/CombatFeedbackPublisher.cs',publisher)
def visual(s):
 s=replace(s,'public void NotifyFireSpell(FireSpellDefinition spell, GridPosition source, IReadOnlyList<GridPosition> targetCells)','public void NotifyFireSpell(FireSpellDefinition spell, GridPosition source, IReadOnlyList<GridPosition> targetCells, bool isTriggered = false)')
 s=replace(s,'''            if (sourceUnit != null) PlayUnitMotion(sourceUnit, UnitMotionKind.Cast, .34f, GridDirection(source, primary), Vector2.zero);
            PlayFormalVfx(source, "fire_cast");''','''            // Delayed effects already belong to a weapon hit, reaction or move. Recasting here
            // would replace that action and can falsely make a victim look like the caster.
            if (!isTriggered)
            {
                if (sourceUnit != null) PlayUnitMotion(sourceUnit, UnitMotionKind.Cast, .34f, GridDirection(source, primary), Vector2.zero);
                PlayFormalVfx(source, "fire_cast");
            }''')
 return s
edit('UnityProject/Assets/Game/Runtime/Presentation/CombatVisualFeedback.cs',visual)
def tests(s):
 s=replace(s,'PublishFireExecutions_UsesTargetPositionAndPreservesLogText','PublishFireExecutions_UsesSourceSnapshotAfterUnitsMoveAndPreservesLogText')
 s=replace(s,'CombatState state = State(out _, out UnitState enemy);','CombatState state = State(out UnitState hero, out UnitState enemy);\n            GridPosition sourceAtResolution = hero.Position, targetAtResolution = enemy.Position;')
 s=replace(s,'            });\n            RecordingSink sink = new RecordingSink();','''            }, hero.Id, sourceAtResolution, true);
            hero.MoveTo(new GridPosition(0, 1), Facing.East);
            enemy.MoveTo(new GridPosition(3, 0), Facing.West);
            int health = hero.Health, mana = hero.Mana, ap = hero.ActionPoints;
            RecordingSink sink = new RecordingSink();''')
 s=replace(s,'Assert.That(sink.FireSources, Is.EqualTo(new[] { enemy.Position }));','''Assert.That(sink.FireSources, Is.EqualTo(new[] { sourceAtResolution }));
            Assert.That(sink.FireTargets[0], Is.EqualTo(new[] { targetAtResolution }));
            Assert.That(sink.FireTriggers, Is.EqualTo(new[] { true }));
            Assert.That(hero.Position, Is.EqualTo(new GridPosition(0, 1)));
            Assert.That(enemy.Position, Is.EqualTo(new GridPosition(3, 0)));
            Assert.That(new[] { hero.Health, hero.Mana, hero.ActionPoints }, Is.EqualTo(new[] { health, mana, ap }));''')
 s=replace(s,'public List<GridPosition> FireSources { get; } = new List<GridPosition>();','''public List<GridPosition> FireSources { get; } = new List<GridPosition>();
            public List<IReadOnlyList<GridPosition>> FireTargets { get; } = new List<IReadOnlyList<GridPosition>>();
            public List<bool> FireTriggers { get; } = new List<bool>();''')
 s=replace(s,'IReadOnlyList<GridPosition> targetCells) => FireSources.Add(source);','''IReadOnlyList<GridPosition> targetCells, bool isTriggered = false)
            { FireSources.Add(source); FireTargets.Add(targetCells); FireTriggers.Add(isTriggered); }''')
 return s
edit('UnityProject/Assets/Game/Tests/EditMode/CombatFeedbackPublisherTests.cs',tests)
def catalog_tests(s):
 s=replace(s,'                UnitState enemy = battle.Combat.GetUnit("range_normal");\n                if (spell.TriggerWindow','                UnitState enemy = battle.Combat.GetUnit("range_normal");\n                GridPosition origin = hero.Position;\n                if (spell.TriggerWindow')
 s,n=re.subn(r'Assert.That\((FireSpellEngine.Trigger[^;\n]+), Is.Not.Empty, spell.Id\);',r'AssertTriggerOrigin(\1, hero.Id, origin, spell.Id);',s)
 assert n==5,n
 extra='''        private static void AssertTriggerOrigin(IReadOnlyList<FireSpellExecution> executions,
            string sourceId, GridPosition origin, string spellId)
        {
            Assert.That(executions, Is.Not.Empty, spellId);
            foreach (FireSpellExecution execution in executions)
            {
                Assert.That(execution.SourceUnitId, Is.EqualTo(sourceId), spellId);
                Assert.That(execution.SourcePosition, Is.EqualTo(origin), spellId);
                Assert.That(execution.IsTriggered, Is.True, spellId);
            }
        }

        [Test]
        public void EverySpell_RecordsCastOriginBeforeAnyMovement()
        {
            FireSpellTrainingRangeProvider provider = new FireSpellTrainingRangeProvider();
            var targetField = typeof(FireSpellTrainingRangeCase).GetField("target",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(targetField, Is.Not.Null);
            foreach (FireSpellDefinition spell in FireSpellCatalog.All)
            {
                var prepared = (FireSpellTrainingRangeCase)provider.Prepare(spell.Id);
                UnitState source = prepared.Battle.Combat.GetUnit("hero");
                GridPosition origin = source.Position;
                var target = (FireSpellTarget)targetField.GetValue(prepared);
                FireSpellExecution execution = FireSpellEngine.Execute(prepared.Battle, source.Id, spell, target);
                Assert.That(execution.SourceUnitId, Is.EqualTo(source.Id), spell.Id);
                Assert.That(execution.SourcePosition, Is.EqualTo(origin), spell.Id);
                Assert.That(execution.IsTriggered, Is.False, spell.Id);
            }
        }

'''
 return replace(s,'        [Test]\n        public void FiregroundUtility_',extra+'        [Test]\n        public void FiregroundUtility_') if '        public void FiregroundUtility_' in s else replace(s,'        [Test]\n        public void WeaponAttackPipeline_GrantsPreHitShieldWithoutFixedReduction()',extra+'        [Test]\n        public void WeaponAttackPipeline_GrantsPreHitShieldWithoutFixedReduction()')
edit('UnityProject/Assets/Game/Tests/EditMode/FireSpellCatalogTests.cs',catalog_tests)
def csvupdate(s):
 import io
 rows=list(csv.DictReader(io.StringIO(s))); fields=list(rows[0]); assert not any(r['ID']=='ART-FEEDBACK-SOURCE' for r in rows)
 rows.append(dict(zip(fields,['ART-FEEDBACK-SOURCE','表现','火术结算来源','来源ID与结算前格快照；主动/延迟标记','读取真实结算','延迟触发不重播施法起手；不能将受影响目标当来源；发布不改状态'])))
 out=io.StringIO(); w=csv.DictWriter(out,fieldnames=fields,lineterminator='\n');w.writeheader();w.writerows(rows);return out.getvalue()
edit('Worldbuilding/数据表/OCC_美术与界面规格表_v1.0.csv',csvupdate)
print('Applied fire feedback origin correction; source revision',doc.get('revision_id'))
