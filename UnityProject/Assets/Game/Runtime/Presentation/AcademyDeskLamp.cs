using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace OCC.Combat.Presentation
{
    // A local daily-use ether lamp. Geometry is presentation only, not a saved device state.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class AcademyDeskLamp : MaskableGraphic
    {
        private Func<float> motion;
        private static readonly int[] LightLevels = { 1, 1, 2, 1, 1, 0, 1, 1 };
        private int lastStep = -1;
        private bool hovered;
        public static int EvaluateLightStep(float seconds, float intensity, bool near)
            => intensity <= 0f ? 1 : near ? 3 :
                LightLevels[Mathf.FloorToInt(Mathf.Max(0f, seconds) * 2f) % LightLevels.Length];
        public int LightStep(float seconds) => EvaluateLightStep(seconds, motion == null ? 0f : motion(), hovered);

        public static AcademyDeskLamp Create(Transform parent, Func<float> intensity)
        {
            var go = new GameObject("学院日用术灯", typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(1068f, -598f);
            rect.sizeDelta = new Vector2(112f, 88f);
            var lamp = go.AddComponent<AcademyDeskLamp>();
            lamp.motion = intensity;
            lamp.raycastTarget = false;
            return lamp;
        }

        private void Update()
        {
            Canvas host = canvas;
            hovered = Mouse.current != null && host != null && RectTransformUtility.RectangleContainsScreenPoint(
                rectTransform, Mouse.current.position.ReadValue(), host.renderMode == RenderMode.ScreenSpaceOverlay ? null : host.worldCamera);
            int step = LightStep(Time.unscaledTime);
            if (lastStep == step) return;
            lastStep = step;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            float brightness = .08f + LightStep(Time.unscaledTime) * .02f;
            Color warm = FormalUiTheme.Amber;
            // Three hard-edged steps put light on the desk, never over the text.
            Quad(vh, -48, -38, 96, 12, new Color(warm.r, warm.g, warm.b, brightness));
            Quad(vh, -34, -26, 68, 12, new Color(warm.r, warm.g, warm.b, brightness));
            Quad(vh, -22, -14, 44, 28, new Color(warm.r, warm.g, warm.b, brightness));
            Color brass = ArchiveUiStyle.Brass;
            Quad(vh, -18, -30, 36, 4, brass);
            Quad(vh, -12, -26, 24, 4, brass);
            Quad(vh, -2, -22, 4, 12, brass);
            Quad(vh, -12, -10, 2, 28, brass);
            Quad(vh, 10, -10, 2, 28, brass);
            Quad(vh, -14, 18, 28, 4, brass);
            Quad(vh, -8, 22, 16, 4, brass);
            Quad(vh, -8, -8, 16, 24, new Color(warm.r, warm.g, warm.b, .6f));
            Quad(vh, -4, -4, 8, 16, new Color(1f, .89f, .58f, .9f));
            if (LightStep(Time.unscaledTime) == 3)
            {
                Quad(vh, -20, 8, 4, 4, warm);
                Quad(vh, 18, 14, 4, 4, warm);
                Quad(vh, 4, 30, 4, 4, warm);
            }
        }

        protected override void OnDisable()
        {
            hovered = false;
            lastStep = -1;
            base.OnDisable();
        }

        private static void Quad(VertexHelper vh, float x, float y, float w, float h, Color c)
        {
            int i = vh.currentVertCount;
            vh.AddVert(new Vector3(x, y), c, Vector2.zero);
            vh.AddVert(new Vector3(x, y + h), c, Vector2.zero);
            vh.AddVert(new Vector3(x + w, y + h), c, Vector2.zero);
            vh.AddVert(new Vector3(x + w, y), c, Vector2.zero);
            vh.AddTriangle(i, i + 1, i + 2); vh.AddTriangle(i, i + 2, i + 3);
        }
    }
}
