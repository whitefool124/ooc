using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OCC.Combat.Tests
{
    public sealed class CombatMovementPlaybackTests
    {
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        private static GridPosition P(int x, int y) => new GridPosition(x, y);

        [Test]
        public void ResolvedDetour_VisitsEveryActualSegmentAndNeverCrossesTheObstacle()
        {
            var hero = new UnitState("hero", true, P(0, 0));
            var state = new CombatState(new GridMap(4, 3), new[] { hero, new UnitState("enemy", false, P(3, 2)) });
            state.Map.SetTile(P(1, 0), new TileState { Cover = CoverType.Heavy, Durability = 10 });
            CombatResolver.BeginTurn(state, hero.Id);
            var result = new CombatCommandExecutionService().Execute(state, null, CombatCommand.Move(hero.Id, P(2, 0)));
            Assert.That(result.Accepted, Is.True);
            Assert.That(result.MovementPath.Count, Is.EqualTo(5));
            int ap = hero.ActionPoints, health = hero.Health;
            var playback = new CombatMovementPlayback();
            Assert.That(playback.Play(hero.Id, P(0, 0), hero.Position, result.MovementPath, 0f), Is.True);
            for (int i = 0; i < 4; i++)
            {
                var sample = playback.Sample(hero.Id, hero.Position, (i + .5f) * .08f);
                Vector2 expected = new Vector2(result.MovementPath[i].X + result.MovementPath[i + 1].X,
                    result.MovementPath[i].Y + result.MovementPath[i + 1].Y) * .5f;
                Assert.That(Vector2.Distance(sample.Position, expected), Is.LessThan(.001f));
                Assert.That(sample.Position, Is.Not.EqualTo(new Vector2(1f, 0f)));
            }
            Assert.That(playback.Sample(hero.Id, hero.Position, .4f).Position, Is.EqualTo(new Vector2(2, 0)));
            Assert.That(hero.Position, Is.EqualTo(P(2, 0))); Assert.That(hero.ActionPoints, Is.EqualTo(ap));
            Assert.That(hero.Health, Is.EqualTo(health)); Assert.That(playback.Count, Is.Zero);
        }

        [Test]
        public void ConsecutiveMoves_PreserveCurrentPoseAndBothOldAndNewCorners()
        {
            var playback = new CombatMovementPlayback();
            playback.Play("hero", P(0, 0), P(1, 1), new[] { P(0, 0), P(1, 0), P(1, 1) }, 0f);
            Vector2 before = playback.Sample("hero", P(1, 1), .04f).Position;
            playback.Play("hero", P(1, 1), P(2, 1), new[] { P(1, 1), P(2, 1) }, .04f);
            Assert.That(playback.Sample("hero", P(2, 1), .04f).Position, Is.EqualTo(before));
            Assert.That(Vector2.Distance(playback.Sample("hero", P(2, 1), .08f).Position, new Vector2(1, 0)), Is.LessThan(.001f));
            Assert.That(Vector2.Distance(playback.Sample("hero", P(2, 1), .16f).Position, new Vector2(1, 1)), Is.LessThan(.001f));
            Assert.That(playback.Sample("hero", P(2, 1), .4f).Position, Is.EqualTo(new Vector2(2, 1)));
        }

        [Test]
        public void InvalidOrStaleRoutes_AlignToFinalStateWithoutInventingAStraightLine()
        {
            var playback = new CombatMovementPlayback();
            Assert.That(playback.Play("hero", P(0, 0), P(2, 2), new[] { P(0, 0), P(2, 2) }, 0), Is.False);
            var path = new[] { P(0, 0), P(1, 0), P(1, 1) };
            playback.Play("hero", P(0, 0), P(1, 1), path, 0);
            path[1] = P(99, 99); // A caller cannot alter the captured route afterwards.
            Assert.That(playback.Sample("hero", P(1, 1), .04f).Position, Is.EqualTo(new Vector2(.5f, 0)));
            Assert.That(playback.Sample("hero", P(0, 1), .06f).Position, Is.EqualTo(new Vector2(0, 1)));
            Assert.That(playback.Count, Is.Zero);
        }

        [TestCase(192f)]
        [TestCase(384f)]
        public void BodyAndVitals_UseTheSameTravelAtBothZoomTiers(float size)
        {
            GameObject root = new GameObject("movement-render-test", typeof(RectTransform), typeof(FormalBattlefieldView));
            try
            {
                var view = root.GetComponent<FormalBattlefieldView>();
                typeof(FormalBattlefieldView).GetField("boardRect", Private).SetValue(view, root.GetComponent<RectTransform>());
                object cell = typeof(FormalBattlefieldView).GetMethod("CreateCell", Private).Invoke(view, new object[] { P(2, 1) });
                T Field<T>(string name) => (T)cell.GetType().GetField(name).GetValue(cell);
                var viewport = new BattlefieldPresentationAdapter().CreateViewport(12, 9);
                // 缩放到目标档位，而不是假设一步就到（步长是 32px 整数档）。
                while (viewport.CellSize < size) viewport.ZoomAt(300, 300, 1);
                Assert.That(viewport.CellSize, Is.EqualTo(size));
                var unit = new UnitState("hero", true, P(2, 1));
                Texture2D texture = Texture2D.whiteTexture;
                BattlefieldCellPresentation Model(Vector2 travel) => new BattlefieldCellPresentation(P(2, 1), null, default,
                    0, null, 0, null, null, 0, null, 0, null, null, texture, new Rect(0, 0, 1, 1), Color.white,
                    Vector2.zero, null, "", Color.white, null, unit, CombatUnitVitalsPresentation.From(unit, null, false),
                    null, null, null, "", travel);
                var refresh = typeof(FormalBattlefieldView).GetMethod("RefreshCell", Private);
                refresh.Invoke(view, new object[] { cell, Model(Vector2.zero), viewport, false });
                Vector2 bodyBefore = Field<RawImage>("Unit").rectTransform.anchoredPosition;
                Vector2 hudBefore = Field<RectTransform>("OverlayRect").anchoredPosition;
                refresh.Invoke(view, new object[] { cell, Model(new Vector2(-32, 16)), viewport, false });
                Vector2 expected = new Vector2(-32, -16) * (size / 64f);
                Assert.That(Field<RawImage>("Unit").rectTransform.anchoredPosition - bodyBefore, Is.EqualTo(expected));
                Assert.That(Field<RectTransform>("OverlayRect").anchoredPosition - hudBefore, Is.EqualTo(expected));
            }
            finally { Object.DestroyImmediate(root); }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PublishingThenDisableOrReset_ClearsTheRouteWithoutChangingTheResolvedActor(bool disable)
        {
            GameObject root = new GameObject("movement-feedback-test", typeof(CombatVisualFeedback));
            try
            {
                var feedback = root.GetComponent<CombatVisualFeedback>(); var host = new Host();
                typeof(CombatVisualFeedback).GetField("bootstrap", Private).SetValue(feedback, host);
                CombatResolver.BeginTurn(host.CurrentState, "hero");
                var result = new CombatCommandExecutionService().Execute(host.CurrentState, null, CombatCommand.Move("hero", P(1, 1)));
                Assert.That(result.Accepted, Is.True);
                UnitState hero = host.CurrentState.GetUnit("hero"); int ap = hero.ActionPoints;
                new CombatFeedbackPublisher().PublishCombatEffects(host.CurrentState, feedback, result.Execution, result.MovementPath);
                var playback = (CombatMovementPlayback)typeof(CombatVisualFeedback).GetField("movementPlayback", Private).GetValue(feedback);
                Assert.That(playback.Count, Is.EqualTo(1));
                if (disable) { host.UiPreferences.Configure(1f, 0f, false, true, false, false, true); feedback.UnitTravelPose(hero); }
                else feedback.ResetBattleFeedback();
                Assert.That(playback.Count, Is.Zero);
                Assert.That(feedback.UnitTravelPose(hero).Position, Is.EqualTo(new Vector2(1, 1)));
                Assert.That(hero.ActionPoints, Is.EqualTo(ap));
            }
            finally { Object.DestroyImmediate(root); }
        }

        private sealed class Host : ICombatFeedbackHost
        {
            public RogueliteUiPreferences UiPreferences { get; } = new RogueliteUiPreferences();
            public CombatState CurrentState { get; } = new CombatState(new GridMap(4, 3),
                new[] { new UnitState("hero", true, P(0, 0)), new UnitState("enemy", false, P(3, 2)) });
            public BattlefieldRect CurrentBattlefieldViewport => new BattlefieldPresentationAdapter().ViewportRect;
            public bool IsDeveloperCombatActive => true;
            public Vector2 GridToFeedbackPosition(GridPosition position) => new Vector2(position.X * 64, -position.Y * 64);
        }
    }
}
