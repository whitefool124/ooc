from pathlib import Path
p=Path('UnityProject/Assets/Game/Tests/EditMode/CombatBattlefieldCellPresenterTests.cs')
s=p.read_text(encoding='utf-8-sig');idx=s.rfind('\n}')
s=s[:idx]+'''
    public sealed class CombatRangeTransitionTests
    {
        private UnityEngine.GameObject root;
        private CombatBattlefieldRangeGraphic graphic;
        private const System.Reflection.BindingFlags Hidden = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;

        [SetUp] public void SetUp()
        {
            root = new UnityEngine.GameObject("range test", typeof(UnityEngine.RectTransform), typeof(UnityEngine.CanvasRenderer));
            graphic = root.AddComponent<CombatBattlefieldRangeGraphic>();
        }
        [TearDown] public void TearDown() => UnityEngine.Object.DestroyImmediate(root);
        private void SetAge(float age) => typeof(CombatBattlefieldRangeGraphic).GetField("appearedAt", Hidden)
            .SetValue(graphic, UnityEngine.Time.unscaledTime - age);
        private void Refresh(bool shown, float intensity = 1f) => graphic.Refresh(
            shown ? BattlefieldCellMarker.MovementReach : BattlefieldCellMarker.None,
            BattlefieldCellMarker.None, BattlefieldCellMarker.None, false, false, 0, false, 0, false, false,
            false, intensity);
        private int MaxAlpha()
        {
            using (var mesh = new UnityEngine.UI.VertexHelper())
            {
                typeof(CombatBattlefieldRangeGraphic).GetMethod("OnPopulateMesh", Hidden).Invoke(graphic, new object[] { mesh });
                int alpha = 0; var vertex = new UnityEngine.UIVertex();
                for (int i = 0; i < mesh.currentVertCount; i++) { mesh.PopulateUIVertex(ref vertex, i); alpha = System.Math.Max(alpha, vertex.color.a); }
                return alpha;
            }
        }
        [Test] public void PreviewFadesInThenRetainsGeometryDuringExitAndFinallyClears()
        {
            Refresh(true); SetAge(.08f); Assert.That(MaxAlpha(), Is.InRange(115, 125));
            SetAge(1f); Assert.That(MaxAlpha(), Is.EqualTo(240));
            Refresh(false); SetAge(.06f); Assert.That(MaxAlpha(), Is.InRange(115, 125));
            SetAge(.20f); Assert.That(MaxAlpha(), Is.Zero);
            typeof(CombatBattlefieldRangeGraphic).GetMethod("Update", Hidden).Invoke(graphic, null);
            Assert.That(typeof(CombatBattlefieldRangeGraphic).GetField("transitionPending", Hidden).GetValue(graphic), Is.EqualTo(false));
        }
        [Test] public void AnimationDisabledRemovesImmediatelyIncludingAnInProgressExit()
        {
            Refresh(true); SetAge(1f); Refresh(false); SetAge(.06f); Assert.That(MaxAlpha(), Is.GreaterThan(0));
            Refresh(false, 0f); Assert.That(MaxAlpha(), Is.Zero);
            Refresh(true, 0f); Assert.That(MaxAlpha(), Is.EqualTo(240));
        }
        [Test] public void RejectionFeedbackRemainsVisibleAtStartOfPreviewAndDisableClearsDepartingGeometry()
        {
            Refresh(true); graphic.ShowResult(false); Assert.That(MaxAlpha(), Is.GreaterThan(240));
            SetAge(1f); Refresh(false); root.SetActive(false); root.SetActive(true);
            Assert.That(MaxAlpha(), Is.Zero);
            Assert.That(graphic.raycastTarget, Is.False);
        }
    }
''' +s[idx:]
s=s.replace('BattlefieldCellMarker.MovementReach :','BattlefieldCellMarker.MovementReachable :')
p.write_text(s,encoding='utf-8')
