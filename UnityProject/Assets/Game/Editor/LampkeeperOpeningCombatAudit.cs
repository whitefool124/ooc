using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using OCC.Combat.Presentation;
using OCC.Combat.Roguelite;

namespace OCC.Combat.EditorTools
{
    public static class LampkeeperOpeningCombatAudit
    {
        public sealed class FightRecord
        {
            public string Node, Outcome;
            public int HealthIn, ManaIn, HealthOut, ManaOut, HeroTurns, Commands, GiftMana, GiftShield, GoldAfter;
            public string[] EquippedSpells;
        }
        public sealed class RunRecord
        {
            public int Seed; public string Gift, OpeningSpell;
            public List<FightRecord> Fights=new List<FightRecord>();
            public List<string> Traversal=new List<string>();
            public string Note;
        }
        public static void Begin()
        {
            if(Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new InvalidOperationException("Wrong project");
            var reports=new List<RunRecord>();
            foreach(int seed in new[]{303,912})
            {
                var choices=RogueliteMapRun.CreateSubsequentAcademyRun(seed).OpeningGift.CandidateIds;
                foreach(string gift in new[]{"control-none"}.Concat(choices))
                {
                    var record=Run(seed,gift,3); reports.Add(record);
                    Debug.Log("Opening continuity: "+seed+" "+gift+" "+string.Join(",",record.Fights.Select(f=>f.Node+":"+f.Outcome+" hp="+f.HealthOut+" mp="+f.ManaOut)));
                }
            }
            string root=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Worldbuilding/归档/2026-10-21_守灯人赠礼实现"));
            File.WriteAllText(Path.Combine(root,"continuous_combat.json"),Newtonsoft.Json.JsonConvert.SerializeObject(reports,Newtonsoft.Json.Formatting.Indented));
            EditorApplication.Exit(0);
        }
        public static RunRecord Run(int seed,string gift,int count)
        {
            var run=RogueliteMapRun.CreateSubsequentAcademyRun(seed,includeOpeningGift:gift!="control-none");
            var record=new RunRecord{Seed=seed,Gift=gift,Note="Local normal-round command simulation, heuristic policy. Control disables gifts for comparison only; not a player-facing option. No resource injection or full journey claim."};
            if(gift!="control-none")
            {
                run.ChooseOpeningGift(gift);
                if(gift=="LK-05")
                {
                    record.OpeningSpell=run.OpeningGift.SpellCandidateIds[0];run.ChooseOpeningGiftSpell(record.OpeningSpell);
                    if(!run.AssignRogueSpell(record.OpeningSpell,4))throw new InvalidOperationException("Opening spell cannot equip");
                }
            }
            run=RoundTrip(run);run.ConfirmAcademyDeparture();
            for(int battle=0;battle<count;battle++)
            {
                var path=FindNextCombat(run);
                if(path==null){record.Note+=" No further reachable combat without an uncleared combat traversal.";break;}
                foreach(string id in path){run.SelectNode(id);record.Traversal.Add(id);}
                run=RoundTrip(run);
                var state=new CombatSceneSessionBuilder().Build(run,null,Array.Empty<CombatSceneMarker>()).State;
                var fight=Fight(state);fight.Node=run.CurrentNodeId;record.Fights.Add(fight);
                if(!state.IsVictory)break;
                if(!RogueliteCombatSettlement.TrySettleVictory(run,state))throw new InvalidOperationException("Victory settlement failed");
                run=RoundTrip(run);
                Collect(run);
                for(int slot=4;slot<8;slot++)
                {
                    if(!string.IsNullOrEmpty(run.RogueRunState.EquippedSpellIds[slot]))continue;
                    string next=run.OwnedFireSpellIds.FirstOrDefault(id=>!run.RogueRunState.EquippedSpellIds.Contains(id));
                    if(next!=null)run.AssignRogueSpell(next,slot);
                }
                fight.GoldAfter=run.Gold;fight.EquippedSpells=run.RogueRunState.EquippedSpellIds.ToArray();
                run=RoundTrip(run);
            }
            return record;
        }
        private static FightRecord Fight(CombatState state)
        {
            var hero=state.GetUnit("hero");var record=new FightRecord{HealthIn=hero.Health,ManaIn=hero.Mana};
            var executor=new CombatCommandExecutionService();var plans=new EnemyTurnPlanBook();
            CombatResolver.AdvanceToNextTurn(state);int own=-1;
            for(int i=0;i<600&&!state.IsVictory&&!state.IsDefeat;i++)
            {
                var unit=state.GetUnit(state.ActiveUnitId);
                if(unit==null)throw new InvalidOperationException("No active unit");
                CombatCommand command;
                if(unit.IsHero)
                {
                    if(own!=state.TurnSequence){own=state.TurnSequence;record.HeroTurns++;}
                    command=AcademyCombatAudit.ChooseHero(state);
                }
                else { plans.Invalidate();command=plans.GetExecutionCommand(state,unit,hero); }
                var execution=executor.Execute(state,state.RogueSpells.FireBattle,command,true);
                if(!execution.Accepted)throw new InvalidOperationException("Rejected command "+execution.RejectionReason);
                record.Commands++;
                if(!unit.IsHero&&state.ActiveUnitId==unit.Id&&!state.IsVictory&&!state.IsDefeat)CombatResolver.EndTurn(state,unit);
                if(record.HeroTurns>=60)break;
            }
            record.Outcome=state.IsVictory?"victory":state.IsDefeat?"defeat":"limit";
            record.HealthOut=hero.Health;record.ManaOut=hero.Mana;
            record.GiftMana=state.Lampkeeper?.TotalManaGained??0;record.GiftShield=state.Lampkeeper?.TotalShieldGained??0;
            return record;
        }
        private static void Collect(RogueliteMapRun run)
        {
            if(run.RogueRunState.PendingRewardGold>0)run.ClaimFixedLoot("gold");
            if(run.RogueRunState.PendingRewardContribution>0)run.ClaimFixedLoot("contribution");
            if(run.RogueRunState.PendingFixedMaterialIds.Count>0)
            {
                try{run.ClaimFixedLoot("materials");}catch(InvalidOperationException){/* Leave blocked material unclaimed; never add capacity. */}
            }
            for(int i=0;i<4&&run.CurrentRewards.Count>0;i++)
            {
                run.OpenPendingRewardChoices();
                var reward=run.CurrentRewards.FirstOrDefault(r=>r.Kind==RogueliteRewardKind.Spell&&run.CanAcceptIndividualLootChoice(r))
                    ??run.CurrentRewards.FirstOrDefault(r=>run.CanAcceptIndividualLootChoice(r));
                if(reward==null)break;
                run.SelectPendingReward(reward.Id);run.ClaimLootChoice(reward.Id);
            }
            run.LeaveLoot();
        }
        private static string[] FindNextCombat(RogueliteMapRun run)
        {
            var seen=new HashSet<string>{run.CurrentNodeId};var queue=new Queue<string[]>();queue.Enqueue(new[]{run.CurrentNodeId});
            while(queue.Count>0)
            {
                var path=queue.Dequeue();var node=run.MapNode(path[path.Length-1]);
                if(path.Length>1&&node.IsCombat&&!run.CompletedNodes.Contains(node.Id))return path.Skip(1).ToArray();
                foreach(string id in node.NextIds.OrderBy(x=>x,StringComparer.Ordinal))
                {
                    if(!seen.Add(id)||run.MapNode(id).Type==RogueliteMapNodeType.Finale)continue;
                    queue.Enqueue(path.Concat(new[]{id}).ToArray());
                }
            }
            return null;
        }
        private static RogueliteMapRun RoundTrip(RogueliteMapRun run)
        {
            var gateway=new RogueliteSaveGateway(new AuditStore());
            if(!gateway.SaveMapRun(run)||!gateway.TryLoadMapRun(out var restored))
                throw new InvalidOperationException("Normal save gateway failed: "+gateway.LastError);
            return restored;
        }
        private sealed class AuditStore:IRogueliteSaveStore
        {
            private readonly Dictionary<string,string> data=new Dictionary<string,string>();
            public bool HasKey(string key)=>data.ContainsKey(key);
            public string GetString(string key,string fallback="")=>data.TryGetValue(key,out var value)?value:fallback;
            public void SetString(string key,string value)=>data[key]=value;
            public void DeleteKey(string key)=>data.Remove(key);public void Flush(){}
        }
    }
}
