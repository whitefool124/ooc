using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace OCC.Combat.EditorTools
{
    // Isolated real-engine evidence. These fixtures do not substitute for a normal-entry full run.
    public static class FullCatalogBalanceAudit
    {
        private static readonly Queue<Tuple<int,string,string>> pending = new Queue<Tuple<int,string,string>>();
        private static readonly List<AcademyCombatAudit.Record> records = new List<AcademyCombatAudit.Record>();
        public static string Status { get; private set; } = "idle";
        private static string folder => Path.GetFullPath(Path.Combine(Application.dataPath,"../ReviewEvidence/20261005_full_balance"));

        public static string Bench(string label)
        {
            Guard();
            var entries = new List<object>();
            foreach (var provider in new ITrainingRangeCaseProvider[] {new FireSpellTrainingRangeProvider(),new ArtifactTrainingRangeProvider()})
            foreach (var ability in provider.Abilities)
            {
                var prepared = provider.Prepare(ability.Id);
                var before = prepared.Combat.Clone();
                var preview = prepared.Preview();
                string error = null, result = null;
                try { result = prepared.Execute().Signature(); } catch(Exception e) {error=e.Message;}
                var state = prepared.Combat;
                var hero = state.GetUnit("hero"); var oldHero=before.GetUnit("hero");
                entries.Add(new {id=ability.Id, legal=preview.CanCommit,error,result,
                    enemyDamage=before.Units.Values.Where(u=>!u.IsHero).Sum(u=>u.Health+u.Shield)-state.Units.Values.Where(u=>!u.IsHero).Sum(u=>u.Health+u.Shield),
                    mana=hero.Mana-oldHero.Mana,ap=hero.ActionPoints-oldHero.ActionPoints,
                    shield=state.Units.Values.Sum(u=>u.Shield)-before.Units.Values.Sum(u=>u.Shield),
                    moved=hero.Position.ManhattanDistance(oldHero.Position)});
            }
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder,label+"-bench.json"),Newtonsoft.Json.JsonConvert.SerializeObject(entries,Newtonsoft.Json.Formatting.Indented));
            File.WriteAllText(Path.Combine(folder,label+"-catalog.json"),Newtonsoft.Json.JsonConvert.SerializeObject(new {spells=FireSpellCatalog.All,artifacts=ArtifactCatalog.All,enemies=EnemyArchetypes.All},Newtonsoft.Json.Formatting.Indented,new Newtonsoft.Json.Converters.StringEnumConverter()));
            return "bench:"+entries.Count;
        }
        public static string Begin()
        {
            Guard(); if(pending.Count>0)throw new InvalidOperationException("Already running.");
            records.Clear();
            string saved=Path.Combine(folder,"combat-matrix.json");
            if(File.Exists(saved))records.AddRange(Newtonsoft.Json.JsonConvert.DeserializeObject<List<AcademyCombatAudit.Record>>(File.ReadAllText(saved)).Where(r=>r.Outcome=="victory"&&r.Errors.Count==0));
            foreach(string node in RogueliteAcademyLayerCatalog.EncounterNodeIds)
                foreach(string profile in new[]{"output","mobility","shield"})pending.Enqueue(Tuple.Create(701,node,profile));
            foreach(var spell in FireSpellCatalog.All)
            {
                pending.Enqueue(Tuple.Create(701,"tutorial_hall","spell:"+spell.Id));
                pending.Enqueue(Tuple.Create(701,"core_finale","spell:"+spell.Id));
            }
            foreach(var artifact in ArtifactCatalog.All)
                foreach(string node in new[]{"tutorial_hall","core_finale"})pending.Enqueue(Tuple.Create(701,node,"artifact:"+artifact.Id));
            var remaining=pending.Where(p=>!records.Any(r=>r.Seed==p.Item1&&r.Node==p.Item2&&r.Profile==p.Item3)).ToArray();
            pending.Clear();foreach(var item in remaining)pending.Enqueue(item);
            EditorApplication.update-=Step;EditorApplication.update+=Step;
            Status="running:"+records.Count+" remaining:"+pending.Count;return Status;
        }
        private static void Guard()
        {
            if(Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets" || Application.isPlaying)
                throw new InvalidOperationException("Use main project in Edit mode.");
        }
        private static void Step()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating)return;
            if(pending.Count==0){Status="complete:"+records.Count;EditorApplication.update-=Step;return;}
            var entry=pending.Dequeue();
            try{records.Add(AcademyCombatAudit.Fight(entry.Item1,entry.Item2,entry.Item3));}
            catch(Exception e){records.Add(new AcademyCombatAudit.Record{Seed=entry.Item1,Node=entry.Item2,Profile=entry.Item3,Outcome="error",Errors=new List<string>{e.ToString()}});}
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder,"combat-matrix.json"),Newtonsoft.Json.JsonConvert.SerializeObject(records,Newtonsoft.Json.Formatting.Indented));
            Status="running:"+records.Count+" remaining:"+pending.Count;
        }
    }
}
