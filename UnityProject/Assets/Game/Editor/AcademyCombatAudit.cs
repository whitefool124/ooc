using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OCC.Combat.Roguelite;
using OCC.Combat.Presentation;

namespace OCC.Combat.EditorTools
{
    // Test policy only: evaluates legal actions on clones; never modifies production stats or resolves debug outcomes.
    public static class AcademyCombatAudit
    {
        public sealed class Record
        {
            public int Seed;
            public string Node, Profile, Outcome;
            public int HeroTurns, HealthIn, HealthOut, ManaOut, Commands, EnemyMoves, EnemySkills, EnemyWaits;
            public List<string> Errors = new List<string>();
            public List<int> BossPhases = new List<int>();
            public string EndUnits;
            public string[] LastEvents;
            public int ArtifactUses;
            public Dictionary<string,int> SpellCasts = new Dictionary<string,int>();
        }
        public static CombatState LastState { get; private set; }
        private static ArtifactBattleState Battle(CombatState state)=>(ArtifactBattleState)
            typeof(CombatState).GetProperty("ArtifactBattle",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(state);
        private static bool TryUseAuditArtifact(CombatState state)
        {
            var item=state.ItemInventory?.Get("audit-artifact");
            if(item==null || item.RemainingUses<=0)return false;
            var hero=state.GetUnit("hero");
            var artifact=ArtifactCatalog.Get(item.DefinitionId);
            var cells=state.Units.Values.Where(u=>u.IsAlive).Select(u=>u.Position)
                .Concat(state.Map.PositionsWith(t=>t.Durability>0))
                .Concat(state.Map.PositionsWith(t=>!t.BlocksMovement).Where(p=>p.ManhattanDistance(hero.Position)<=3)).Distinct();
            ArtifactTarget best=default;double bestScore=3;
            foreach(var cell in cells)
            {
                var unit=state.Units.Values.FirstOrDefault(u=>u.IsAlive&&u.Position==cell);
                var target=unit==null?ArtifactTarget.At(cell):ArtifactTarget.Unit(unit.Id,cell);
                if(!ArtifactEngine.PreviewInventory(Battle(state),hero.Id,item.InstanceId,target).CanCommit)continue;
                var clone=state.Clone();var before=EnemyVitals(state);
                var execution=ArtifactEngine.ExecuteInventory(Battle(clone),hero.Id,item.InstanceId,target);
                var after=clone.GetUnit(hero.Id);
                double score=(before-EnemyVitals(clone))*10+(after.Health-hero.Health)*15+
                    (after.Mana-hero.Mana)*2+(after.ActionPoints-hero.ActionPoints)*8;
                if(hero.Shield<4)score+=Math.Min(8,Math.Max(0,after.Shield-hero.Shield));
                if(artifact.Id=="G-T15" && !state.Map.PositionsWith(t=>t.IsDecoy).Any())score+=5;
                score+=execution.Steps.Where(s=>s.Kind==ArtifactEffectKind.DamageObject).Sum(s=>s.Applied)*2;
                if(clone.IsVictory)score+=1000;
                if(score>bestScore){bestScore=score;best=target;}
            }
            if(bestScore<=3)return false;
            ArtifactEngine.ExecuteInventory(Battle(state),hero.Id,item.InstanceId,best);
            return true;
        }

        public static CombatCommand ChooseHero(CombatState state)
        {
            UnitState hero = state.GetUnit("hero");
            CombatCommand best = CombatCommand.EndTurn(hero.Id);
            double bestScore = .01;
            var executor = new CombatCommandExecutionService();
            int beforeEnemies = EnemyVitals(state);
            var enemies = state.Units.Values.Where(u => !u.IsHero && u.IsAlive).ToArray();
            var objectiveCells=state.Objectives.OfType<DestructionObjective>().SelectMany(o=>o.Positions)
                .Where(p=>!state.Map.GetTile(p).IsDestroyed).ToArray();
            int beforeObjects=objectiveCells.Sum(p=>state.Map.GetTile(p).Durability);
            var obstacles=state.Map.PositionsWith(t=>t.Durability>0 && (t.Cover!=CoverType.None || t.IsDevice)).ToArray();
            int beforeObstacles=obstacles.Sum(p=>state.Map.GetTile(p).Durability);
            var targets = enemies.Select(u => u.Position).Concat(new[] { hero.Position })
                .Concat(state.Map.PositionsWith(t => t.Cover != CoverType.None || t.IsDevice)
                    .Where(p => p.ManhattanDistance(hero.Position) <= 5)).Distinct().ToArray();
            var candidates = new List<CombatCommand>();
            foreach (var enemy in enemies) candidates.Add(CombatCommand.Attack(hero.Id, enemy.Id));
            foreach(var cell in objectiveCells.Concat(obstacles).Distinct())
                if(cell.ManhattanDistance(hero.Position)==1)candidates.Add(CombatCommand.Interact(hero.Id,cell));
            for (int slot = 0; slot < RogueRuntimeConstants.SpellSlotCount; slot++)
            {
                var spell = state.RogueSpells?.DefinitionAtSlot(slot);
                if (spell == null || spell.Role == "passive") continue;
                if(hero.ActionPoints<spell.ActionPointCost || hero.Mana<spell.ManaCost || state.RogueSpells.CooldownRemaining(spell.DefinitionId)>0)continue;
                foreach (var enemy in enemies) candidates.Add(CombatCommand.UseSkill(hero.Id, slot, enemy.Id));
                candidates.Add(CombatCommand.UseSkill(hero.Id, slot, hero.Id));
                foreach (var cell in targets)
                    candidates.Add(CombatCommand.UseSkillAt(hero.Id, slot, cell, Direction(hero.Position, cell)));
                if (state.AcademyCoreBoss != null)
                    foreach (var cell in state.Map.PositionsWith(t => t.IsDevice || t.Cover != CoverType.None).Where(p => p.ManhattanDistance(hero.Position) <= 5))
                        candidates.Add(CombatCommand.UseSkillAt(hero.Id, slot, cell, Direction(hero.Position, cell)));
            }
            foreach (var command in candidates)
            {
                // Reject invalid personal-spell targets on the real read-only preview before cloning.
                // This leaves the scoring policy unchanged and keeps exhaustive audits bounded.
                if(command.Type==CombatCommandType.UseSkill)
                {
                    var definition=state.RogueSpells?.DefinitionAtSlot(command.SlotIndex);
                    if(definition?.DefinitionId.StartsWith("F-P-",StringComparison.Ordinal)==true)
                    {
                        var target=string.IsNullOrEmpty(command.TargetUnitId)
                            ? FireSpellTarget.At(command.Destination,command.AimDirection)
                            : FireSpellTarget.Unit(command.TargetUnitId,command.AimDirection);
                        if(!FireSpellEngine.Preview(state.RogueSpells.FireBattle,hero.Id,
                            state.RogueSpells.EffectiveFireDefinition(definition.DefinitionId),target).CanCommit)continue;
                    }
                }
                var clone = state.Clone();
                var result = executor.Execute(clone, clone.RogueSpells?.FireBattle, command);
                if (!result.Accepted) continue;
                UnitState after = clone.GetUnit(hero.Id);
                double score = (beforeEnemies - EnemyVitals(clone)) * 10 +
                    (after.Health - hero.Health) * 5 - Math.Max(0, hero.Health - after.Health) * 15;
                score += (after.Mana - hero.Mana) * (hero.Mana < 5 ? 2.2 : .08);
                score += Math.Min(8, Math.Max(0, after.Shield - hero.Shield)) * (hero.Shield < 4 ? 1.1 : .03);
                score -= Math.Max(0, hero.ActionPoints - after.ActionPoints) * .6;
                score += (beforeObjects-objectiveCells.Sum(p=>clone.Map.GetTile(p).Durability))*12;
                score += (beforeObstacles-obstacles.Sum(p=>clone.Map.GetTile(p).Durability))*2;
                if (clone.IsVictory) score += 1000;
                if (score > bestScore) { best = command; bestScore = score; }
            }
            if (bestScore > 5) return best;
            // A wall detour may initially increase Manhattan distance. Evaluate complete legal approach routes.
            if (hero.ActionPoints > 0 && !hero.HasStatus(StatusType.Bound))
            {
                IReadOnlyList<GridPosition> route = null;
                int cheapest = int.MaxValue;
                foreach (var target in enemies.Select(e=>e.Position).Concat(objectiveCells))
                    for (int y = 0; y < state.Map.Height; y++)
                        for (int x = 0; x < state.Map.Width; x++)
                        {
                            var cell = new GridPosition(x, y);
                            int range = hero.EffectiveRange(hero.BasicAttack.Range);
                            int distance = cell.ManhattanDistance(target);
                            if (distance < hero.BasicAttack.MinimumRange || distance > range || state.Map.IsBlocked(cell) ||
                                state.IsOccupied(cell, hero.Id) || range > 1 && !state.HasLineOfSight(cell, target)) continue;
                            var path = state.Map.FindLowestCostPath(hero.Position, cell, state.Map.Width * state.Map.Height * 4,
                                p => CombatMovementQuery.EntryCost(state, hero, p), p => state.IsOccupied(p, hero.Id));
                            if (path.Count < 2) continue;
                            int cost = path.Skip(1).Sum(p => CombatMovementQuery.EntryCost(state, hero, p));
                            if (cost < cheapest) { cheapest = cost; route = path; }
                        }
                if (route != null)
                {
                    int budget = CombatMovementQuery.Budget(state, hero), spent = 0;
                    GridPosition destination = hero.Position;
                    foreach (var cell in route.Skip(1))
                    {
                        spent += CombatMovementQuery.EntryCost(state, hero, cell);
                        if (spent > budget) break;
                        destination = cell;
                        if (state.Map.GetTile(cell).IsBindingMark) break;
                    }
                    if (destination != hero.Position && string.IsNullOrEmpty(CombatMovementQuery.PlayerTargetFailure(state, destination)))
                        return CombatCommand.Move(hero.Id, destination);
                }
            }
            // Approach the nearest opponent using actual path costs. No unreachable teleport candidates.
            double beforeDistance = Distance(state, hero, enemies);
            var moves = new List<Tuple<GridPosition, double>>();
            for (int y = 0; y < state.Map.Height; y++)
                for (int x = 0; x < state.Map.Width; x++)
                {
                    var cell = new GridPosition(x, y);
                    if (cell == hero.Position || state.Map.IsBlocked(cell) || state.IsOccupied(cell, hero.Id)) continue;
                    var path = CombatMovementQuery.FindPath(state, hero, cell);
                    if (path.Count < 2) continue;
                    double distance = enemies.Length == 0 ? 0 : enemies.Min(e => cell.ManhattanDistance(e.Position));
                    moves.Add(Tuple.Create(cell, distance));
                }
            foreach (var move in moves.OrderBy(v => v.Item2).ThenBy(v => v.Item1.Y).ThenBy(v => v.Item1.X).Take(6))
            {
                var clone = state.Clone();
                var result = executor.Execute(clone, clone.RogueSpells?.FireBattle, CombatCommand.Move(hero.Id, move.Item1));
                if (!result.Accepted) continue;
                var after = clone.GetUnit(hero.Id);
                double score = (beforeDistance - Distance(clone, after, enemies)) * 1.8 - (hero.Health - after.Health) * 15;
                score -= clone.RogueSpells?.FireBattle.HasFireground(after.Position) == true ? 8 : 0;
                if (score > bestScore) { best = CombatCommand.Move(hero.Id, move.Item1); bestScore = score; }
            }
            return best;
        }

        private static double Distance(CombatState state, UnitState hero, UnitState[] enemies) =>
            enemies.Length == 0 ? 0 : enemies.Min(e => hero.Position.ManhattanDistance(e.Position));
        private static int EnemyVitals(CombatState state) => state.Units.Values.Where(u => !u.IsHero).Sum(u => u.Health + u.Shield);
        public static CardinalDirection Direction(GridPosition a, GridPosition b) =>
            Math.Abs(b.X - a.X) >= Math.Abs(b.Y - a.Y) ? (b.X >= a.X ? CardinalDirection.East : CardinalDirection.West) :
                (b.Y >= a.Y ? CardinalDirection.North : CardinalDirection.South);

        public static Record Fight(int seed, string node, string profile)
        {
            // Explicit isolated encounter setup, not a claim of normal-entry journey acceptance.
            var run = node.StartsWith("B") && node != "B01" || node == "X"
                ? RogueliteMapRun.CreateFirstRunV1(seed) : RogueliteMapRun.CreateSubsequentAcademyRun(seed);
            if (run.IsInAcademyLayer)
                typeof(RogueliteMapRun).GetMethod("ReplaceEncounterAssignments", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(run, new object[] { RogueliteAcademyLayerCatalog.GenerateEncounterAssignments(seed) });
            typeof(RogueliteMapRun).GetProperty("CurrentNodeId").SetValue(run, node);
            var build = new CombatSceneSessionBuilder().Build(run, null, Array.Empty<CombatSceneMarker>());
            if (build == null) throw new InvalidOperationException("Missing encounter " + node);
            var state = build.State;
            var loadout = RogueSpellLoadout.CreateStarter();
            string[] extras = profile == "mobility" ? new[] { "F-P-M01", "F-P-U04", "F-P-M11", "PASSIVE-ELITE-02" } :
                profile == "shield" ? new[] { "F-P-U13", "F-P-M12", "F-P-U03", "PASSIVE-ELITE-03" } :
                new[] { "F-P-R19", "F-P-U03", "F-P-M12", "PASSIVE-ELITE-01" };
            if(profile.StartsWith("spell:"))
            {
                string spellId=profile.Substring(6);
                extras=new[]{spellId,"F-P-R04","F-P-R11","PASSIVE-ELITE-01"}.Distinct().ToArray();
            }
            if (profile != "starter")
                for (int i = 0; i < extras.Length; i++) { loadout.Learn(extras[i]); loadout.Equip(i + 4, extras[i]); }
            state.AttachRogueSpellRuntime(new RogueSpellCombatRuntime(state, loadout.CreateCombatSnapshot()));
            string auditArtifact=profile.StartsWith("artifact:")?profile.Substring(9):null;
            int initialUses=0;
            if(auditArtifact!=null)
            {
                var inventory=new InventoryContainerState();
                initialUses=ArtifactCatalog.Get(auditArtifact).MaximumUses;
                if(!inventory.AddFirstFit(new ItemInstance("audit-artifact",auditArtifact,0,initialUses)).Success)
                    throw new InvalidOperationException("Artifact fixture cannot fit");
                state.ConfigureItemInventory(inventory,new[]{"audit-artifact"});
                new ArtifactBattleState(state);
            }
            var record = new Record { Seed = seed, Node = node, Profile = profile, HealthIn = state.GetUnit("hero").Health };
            var executor = new CombatCommandExecutionService();
            var plans = new EnemyTurnPlanBook();
            CombatResolver.AdvanceToNextTurn(state);
            int observedHeroTurn = -1;
            for (int step = 0; step < 600 && !state.IsVictory && !state.IsDefeat; step++)
            {
                var active = state.GetUnit(state.ActiveUnitId);
                if (active == null) { record.Errors.Add("Missing active unit"); break; }
                CombatCommand command = CombatCommand.EndTurn(active.Id);
                if (active.IsHero)
                {
                    if (observedHeroTurn != state.TurnSequence) { record.HeroTurns++; observedHeroTurn = state.TurnSequence; }
                    if(auditArtifact!=null && TryUseAuditArtifact(state)){record.Commands++;continue;}
                    command = ChooseHero(state);
                }
                else
                {
                    plans.Invalidate();
                    var intent = plans.GetPublicIntent(state, active, state.GetUnit("hero"));
                    command = plans.GetExecutionCommand(state, active, state.GetUnit("hero"));
                    if (command.Type == CombatCommandType.Move) record.EnemyMoves++;
                    else if (command.Type == CombatCommandType.UseSkill) record.EnemySkills++;
                    else if (command.Type == CombatCommandType.EndTurn) record.EnemyWaits++;
                    if (intent == null) record.Errors.Add("Missing public intent " + active.Id);
                }
                if(state.AcademyCoreBoss != null)
                {
                    var core=state.Units.Values.FirstOrDefault(u=>u.EnemyArchetypeId=="core_overseer");
                    int phase=state.AcademyCoreBoss.PhaseFor(state,core);
                    if(!record.BossPhases.Contains(phase))record.BossPhases.Add(phase);
                }
                var result = executor.Execute(state, state.RogueSpells?.FireBattle, command, true);
                if (!result.Accepted) { record.Errors.Add(active.Id + ":" + result.RejectionReason); break; }
                if(active.IsHero && command.Type==CombatCommandType.UseSkill)
                {
                    string spellId=state.RogueSpells?.DefinitionAtSlot(command.SlotIndex)?.DefinitionId;
                    if(spellId!=null)record.SpellCasts[spellId]=record.SpellCasts.TryGetValue(spellId,out int count)?count+1:1;
                }
                record.Commands++;
                if (!active.IsHero && state.ActiveUnitId == active.Id && !state.IsVictory && !state.IsDefeat)
                    CombatResolver.EndTurn(state, active);
                if (record.HeroTurns >= 60) break;
            }
            record.Outcome = state.IsVictory ? "victory" : state.IsDefeat ? "defeat" : "limit";
            record.HealthOut = state.GetUnit("hero").Health;
            record.ManaOut = state.GetUnit("hero").Mana;
            if(auditArtifact!=null)record.ArtifactUses=initialUses-(state.ItemInventory.Get("audit-artifact")?.RemainingUses??0);
            record.EndUnits=string.Join(";",state.Units.Values.Select(u=>u.Id+"@"+u.Position+" hp="+u.Health+" shield="+u.Shield));
            record.LastEvents=state.EventLog.Take(12).ToArray();
            LastState=state;
            return record;
        }
    }
}
