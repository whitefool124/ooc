using NUnit.Framework;
using OCC.Combat.Presentation;
using UnityEngine;

namespace OCC.Combat.Tests
{
    public sealed class ArchivePaperDetailTests
    {
        [Test] public void PaperClick_ResolvesToItsOwningEntry_AndRejectsDetachedDecoration()
        {
            var root = new GameObject("entry", typeof(RectTransform), typeof(UnityEngine.UI.Button));
            try
            {
                var paper = ArchivePaperDetail.Create(root.transform, () => 1f);
                Assert.That(UnityEngine.EventSystems.ExecuteEvents.GetEventHandler<UnityEngine.EventSystems.IPointerClickHandler>(paper.gameObject), Is.SameAs(root));
                paper.transform.SetParent(null);
                Assert.That(paper.IsRaycastLocationValid(Vector2.zero, null), Is.False);
                Object.DestroyImmediate(paper.gameObject);
            }
            finally { Object.DestroyImmediate(root); }
        }
        [Test] public void RenderedPaper_UsesFormalArt_AndARealTexturedMesh()
        {
            var root = new GameObject("desk", typeof(RectTransform), typeof(Canvas));
            try
            {
                var paper = ArchivePaperDetail.Create(root.transform, () => 1f);
                var sprite = Resources.Load<Sprite>(ArchivePaperDetail.PaperArtPath);
                Assert.That(paper.mainTexture, Is.SameAs(sprite.texture));
                Canvas.ForceUpdateCanvases();
                using (var vertices = new UnityEngine.UI.VertexHelper())
                {
                    typeof(ArchivePaperDetail).GetMethod("OnPopulateMesh", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.DeclaredOnly)
                        .Invoke(paper, new object[] { vertices });
                    Assert.That(vertices.currentVertCount, Is.GreaterThan(100));
                    var vertex = new UnityEngine.UIVertex();
                    vertices.PopulateUIVertex(ref vertex, 0);
                    Assert.That(float.IsNaN(vertex.uv0.x), Is.False);
                }
            }
            finally { Object.DestroyImmediate(root); }
        }
        [Test] public void EnterAndLeave_ReturnsToRest_WithoutAccumulatingObjects()
        {
            var root = new GameObject("desk", typeof(RectTransform));
            try
            {
                var paper = ArchivePaperDetail.Create(root.transform, () => 1f);
                var trigger = root.AddComponent<ArchivePaperPullTrigger>(); trigger.Configure(paper);
                for (int i = 0; i < 12; i++)
                {
                    trigger.OnPointerEnter(null); paper.Advance(.3f);
                    Assert.That(paper.Progress, Is.EqualTo(1f));
                    trigger.OnPointerExit(null); paper.Advance(.5f);
                    Assert.That(paper.Progress, Is.Zero);
                }
                Assert.That(root.GetComponentsInChildren<ArchivePaperDetail>().Length, Is.EqualTo(1));
                Assert.That(paper.raycastTarget, Is.True);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test] public void TurningMotionOff_ImmediatelyRestsThePulledPaper()
        {
            var root = new GameObject("desk", typeof(RectTransform)); float intensity = 1f;
            try
            {
                var paper = ArchivePaperDetail.Create(root.transform, () => intensity);
                paper.SetPulling(true); paper.Advance(.3f);
                Assert.That(paper.Progress, Is.EqualTo(1f));
                intensity = 0f; paper.Advance(.01f);
                Assert.That(paper.Progress, Is.Zero);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}


