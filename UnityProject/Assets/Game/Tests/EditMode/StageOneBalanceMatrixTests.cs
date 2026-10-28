using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class StageOneBalanceMatrixTests
    {
        [Test,Timeout(600000)]
        public void NormalOpening_ThreeSeedsAndAvailableGifts_ThreeContinuousFights()
        {
            var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("OCC.Combat.EditorTools.LampkeeperOpeningCombatAudit")).First(t=>t!=null);
            var records=new List<object>();
            foreach(int seed in new[]{303,912,1070729756})
            foreach(string gift in new[]{"control-none"}.Concat(RogueliteMapRun.CreateSubsequentAcademyRun(seed).OpeningGift.CandidateIds))
                records.Add(type.GetMethod("Run").Invoke(null,new object[]{seed,gift,3}));
            string folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Worldbuilding/归档/2026-10-22_第一阶段战斗测试与平衡"));
            string label=Environment.GetEnvironmentVariable("OCC_BALANCE_PASS")??"baseline";
            File.WriteAllText(Path.Combine(folder,label+"_continuous.json"),(string)AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null).GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{records}));
            Assert.That(records.Count,Is.EqualTo(12));
        }
    }
}
