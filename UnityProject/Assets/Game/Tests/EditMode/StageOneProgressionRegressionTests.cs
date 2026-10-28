using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using OCC.Combat.Presentation;
using OCC.Combat.Roguelite;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class StageOneProgressionRegressionTests
    {
        [TestCase(1070729756)][TestCase(1)][TestCase(303)][TestCase(912)]
        public void GeneratedMap_AllSavedRowsHaveDistinctVisiblePositionsWithoutChangingTheGraph(int seed)
        {
            var run=RogueliteMapRun.CreateSubsequentAcademyRun(seed);
            string[] graph=RogueliteAcademyMapGenerator.Encode(run.MapNodes).ToArray();
            var positions=run.MapNodes.Select(n=>AcademyMapVisualLayout.SourcePositionFor(run,n)).ToArray();
            Assert.That(positions.Distinct().Count(),Is.EqualTo(run.MapNodes.Count));
            foreach(var point in positions)
            {
                Assert.That(point.x,Is.InRange(64f,AcademyMapVisualLayout.SourceSize.x-64f));
                Assert.That(point.y,Is.InRange(64f,AcademyMapVisualLayout.SourceSize.y-64f));
            }
            foreach(var node in run.MapNodes) Assert.That(FormalRogueliteUi.MapRegionId(run,node),Is.Not.Empty);
            var restored=RoundTrip(run);
            Assert.That(RogueliteAcademyMapGenerator.Encode(restored.MapNodes),Is.EqualTo(graph));
            Assert.That(restored.MapNodes.Select(n=>AcademyMapVisualLayout.SourcePositionFor(restored,n)),Is.EqualTo(positions));
            if(seed==1070729756)
            {
                Assert.That(restored.MapNode("supply_depot").GridY,Is.EqualTo(5));
                Assert.That(restored.MapNode("workshop_yard").GridY,Is.EqualTo(6));
            }
        }
        [TestCase("supply_depot")][TestCase("workshop_yard")]
        public void GeneratedMap_RevealOriginUsesSavedCurrentNodeBeyondLegacyRows(string nodeId)
        {
            var dto=RoundTrip(RogueliteMapRun.CreateSubsequentAcademyRun(1070729756)).RogueRunState;
            dto.CurrentNodeId=nodeId;
            var restored=RogueliteMapRun.FromRogue11(dto);
            var method=typeof(FormalRogueliteUi).GetMethod("MapRevealOrigin",BindingFlags.NonPublic|BindingFlags.Static);
            var actual=(Vector2)method.Invoke(null,new object[]{restored});
            var source=AcademyMapVisualLayout.SourcePositionFor(restored,restored.MapNode(nodeId));
            Assert.That(actual,Is.EqualTo(new Vector2(source.x/1536f*1760f-880f,397f-source.y/864f*794f)));
        }
        [Test]
        public void TutorialAndLegacyMap_KeepTheirExistingAnchors()
        {
            var tutorial=RogueliteMapRun.CreateFirstRunV1(1070729756);
            foreach(var node in tutorial.MapNodes)
                Assert.That(AcademyMapVisualLayout.SourcePositionFor(tutorial,node),Is.EqualTo(AcademyMapVisualLayout.SourcePositionFor(node,false)));
            var dto=RoundTrip(RogueliteMapRun.CreateSubsequentAcademyRun(1070729756)).RogueRunState;
            dto.GeneratedAcademyMapRows.Clear();
            var legacy=RogueliteMapRun.FromRogue11(dto);
            Assert.That(legacy.HasGeneratedAcademyMap,Is.False);
            foreach(var node in legacy.MapNodes)
                Assert.That(AcademyMapVisualLayout.SourcePositionFor(legacy,node),Is.EqualTo(AcademyMapVisualLayout.SourcePositionFor(node,false)));
        }
        [Test]
        public void EveryRuntimeFeedbackKind_ResolvesAnImportedIconInThePlaybackRenderer()
        {
            var root=new GameObject("All feedback semantic regression",typeof(CombatVisualFeedback));
            try
            {
                var method=typeof(CombatVisualFeedback).GetMethod("SemanticIcon",BindingFlags.NonPublic|BindingFlags.Instance);
                foreach(CombatFeedbackKind kind in Enum.GetValues(typeof(CombatFeedbackKind)))
                    Assert.That(method.Invoke(root.GetComponent<CombatVisualFeedback>(),new object[]{CombatFeedbackCatalog.For(kind).Key}),Is.Not.Null,kind.ToString());
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
        private static RogueliteMapRun RoundTrip(RogueliteMapRun run)
        {
            var gateway=new RogueliteSaveGateway(new Store());
            Assert.That(gateway.SaveMapRun(run),Is.True,gateway.LastError);
            Assert.That(gateway.TryLoadMapRun(out var restored),Is.True,gateway.LastError);return restored;
        }
        private sealed class Store:IRogueliteSaveStore
        {
            private readonly Dictionary<string,string> data=new Dictionary<string,string>();
            public bool HasKey(string key)=>data.ContainsKey(key);
            public string GetString(string key,string fallback="")=>data.TryGetValue(key,out var value)?value:fallback;
            public void SetString(string key,string value)=>data[key]=value;
            public void DeleteKey(string key)=>data.Remove(key);public void Flush(){}
        }
    }
}
