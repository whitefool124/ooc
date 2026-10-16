using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace OCC.Combat.EditorTools
{
    public static class AcademyBalanceAudit
    {
        private static readonly Queue<Tuple<int,string,string>> pending=new Queue<Tuple<int,string,string>>();
        private static readonly List<AcademyCombatAudit.Record> records=new List<AcademyCombatAudit.Record>();
        public static string Status { get; private set; }="idle";
        private static string outputName="real-combat-matrix.json";
        public static string BeginArtifacts()
        {
            if(pending.Count>0)throw new InvalidOperationException("Matrix already running.");
            records.Clear();outputName="artifact-risk-combat-matrix.json";
            for(int seed=701;seed<=706;seed++)
                foreach(string id in new[]{"G-T01","G-T04","G-T12","G-T13","G-T15","G-T19","F-T01"})
                    pending.Enqueue(Tuple.Create(seed,id=="G-T15"?"tutorial_hall":"core_finale","artifact:"+id));
            Status="artifacts";EditorApplication.update-=Step;EditorApplication.update+=Step;
            return Status+":"+pending.Count;
        }
        public static string RecheckLimits()
        {
            if(pending.Count>0)throw new InvalidOperationException("Matrix already running.");
            string path=Path.GetFullPath(Path.Combine(Application.dataPath,"../ReviewEvidence/20261005_four_tasks/real-combat-matrix.json"));
            records.Clear();
            outputName="real-combat-matrix.json";
            records.AddRange(Newtonsoft.Json.JsonConvert.DeserializeObject<List<AcademyCombatAudit.Record>>(File.ReadAllText(path)));
            foreach(var record in records.Where(r=>r.Outcome!="victory").ToArray())
                pending.Enqueue(Tuple.Create(record.Seed,record.Node,record.Profile));
            Status="rechecking";EditorApplication.update-=Step;EditorApplication.update+=Step;
            return Status+":"+pending.Count;
        }
        public static string Begin()
        {
            if(pending.Count>0)throw new InvalidOperationException("Matrix already running.");
            if(Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new InvalidOperationException("Wrong project.");
            records.Clear();
            for(int seed=701;seed<=706;seed++)
            {
                var run=RogueliteMapRun.CreateSubsequentAcademyRun(seed);
                var mappings=run.MapNodes.Select(n=>RogueliteAcademyLayerCatalog.Mapping(n.Id)).ToArray();
                string normal=mappings.First(m=>m.Type==RogueliteMapNodeType.Combat).NodeId;
                var nodes=new[]{normal}.Concat(mappings.Where(m=>m.Type==RogueliteMapNodeType.Elite).Select(m=>m.NodeId)).Concat(new[]{"core_finale"});
                foreach(var node in nodes)
                    foreach(var profile in new[]{"output","mobility","shield"})pending.Enqueue(Tuple.Create(seed,node,profile));
            }
            foreach(var node in RogueliteAcademyLayerCatalog.EncounterNodeIds)
                pending.Enqueue(Tuple.Create(701,node,"output"));
            pending.Enqueue(Tuple.Create(701,"tutorial_hall","output"));
            Status="running";EditorApplication.update-=Step;EditorApplication.update+=Step;
            return Status+":"+pending.Count;
        }
        private static void Step()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating)return;
            if(pending.Count==0){Status="complete:"+records.Count;EditorApplication.update-=Step;return;}
            var entry=pending.Dequeue();
            records.RemoveAll(r=>r.Seed==entry.Item1&&r.Node==entry.Item2&&r.Profile==entry.Item3);
            try { records.Add(AcademyCombatAudit.Fight(entry.Item1,entry.Item2,entry.Item3)); }
            catch(Exception error){records.Add(new AcademyCombatAudit.Record{Seed=entry.Item1,Node=entry.Item2,Profile=entry.Item3,Outcome="error",Errors=new List<string>{error.ToString()}});}
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../ReviewEvidence/20261005_four_tasks"));Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder,outputName),Newtonsoft.Json.JsonConvert.SerializeObject(records,Newtonsoft.Json.Formatting.Indented));
            Status="running:"+records.Count+" remaining:"+pending.Count;
        }
    }
}
