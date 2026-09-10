using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class CombatFeedbackObstacleTests
    {
        [TestCase(1f)]
        [TestCase(.5f)]
        public void DisplayedObstacles_ConvertToReferenceCanvas_AndDisappearWithHiddenView(float scale)
        {
            var root = new GameObject("canvas",typeof(RectTransform),typeof(Canvas));
            try
            {
                var canvas = root.GetComponent<Canvas>();
                var rootRect = root.GetComponent<RectTransform>(); rootRect.sizeDelta = new Vector2(1920,1080);
                rootRect.localScale = Vector3.one * scale;
                var viewport = new GameObject("viewport",typeof(RectTransform)).GetComponent<RectTransform>();
                viewport.SetParent(root.transform,false); viewport.sizeDelta = new Vector2(800,600);
                viewport.anchorMin = viewport.anchorMax = new Vector2(.5f,.5f); viewport.anchoredPosition = new Vector2(-200,0);
                var board = new GameObject("board",typeof(RectTransform)).GetComponent<RectTransform>();
                board.SetParent(viewport,false); board.anchorMin = board.anchorMax = board.pivot = new Vector2(0,1);
                board.anchoredPosition = new Vector2(100,-50); board.sizeDelta = new Vector2(768,576);
                var view = root.AddComponent<FormalBattlefieldView>();
                const BindingFlags flags = BindingFlags.Instance|BindingFlags.NonPublic;
                void Set(string name,object value) => typeof(FormalBattlefieldView).GetField(name,flags).SetValue(view,value);
                Set("root",viewport.gameObject); Set("canvas",canvas); Set("boardRect",board); Set("viewportRect",viewport);
                var bodies = (List<Rect>)typeof(FormalBattlefieldView).GetField("annotationObstacles",flags).GetValue(view);
                bodies.Add(new Rect(20,30,80,60));
                bodies.Add(new Rect(-500,0,20,20)); // Entirely beyond the left viewport edge.
                bodies.Add(new Rect(-120,0,40,20)); // Partially visible; reserve only the visible portion.
                var intents = (List<Rect>)typeof(FormalBattlefieldView).GetField("placedIntents",flags).GetValue(view);
                intents.Add(new Rect(100,80,40,40));
                var result = new List<Rect>(); view.AppendFeedbackObstacles(result);
                Assert.That(result,Is.EqualTo(new[] {new Rect(-480,160,80,60),new Rect(-600,230,20,20),new Rect(-400,130,40,40)}));
                var information = (List<Rect>)typeof(FormalBattlefieldView).GetField("annotationInformation",flags).GetValue(view);
                information.Add(new Rect(20,30,80,10));
                var owners = (Dictionary<string,Rect>)typeof(FormalBattlefieldView).GetField("annotationBodies",flags).GetValue(view);
                owners.Add("hero",new Rect(20,30,80,60)); owners.Add("enemy",new Rect(80,100,40,40));
                result.Clear(); view.AppendFeedbackObstacles(result,false,"hero");
                Assert.That(result,Is.EqualTo(new[] {new Rect(-480,210,80,10),new Rect(-420,110,40,40),new Rect(-400,130,40,40)}));
                result.Clear(); viewport.gameObject.SetActive(false); view.AppendFeedbackObstacles(result);
                Assert.That(result,Is.Empty);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
