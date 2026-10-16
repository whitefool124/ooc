using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    // Transient UI geometry, never an asset or an owner of gameplay state.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PeripheralPixelVfx : MaskableGraphic
    {
        private string motif;
        private float startedAt;
        private float duration;
        private Vector2[] route;
        public float Progress => Mathf.Clamp01((Time.unscaledTime - startedAt) / Mathf.Max(.01f, duration));

        public static PeripheralPixelVfx Spawn(Transform parent, string motif, float intensity,
            Vector2 size, Vector2 position, Vector2 anchor)
        {
            if (parent == null || !parent.gameObject.activeInHierarchy || intensity <= 0f) return null;
            var node = new GameObject("像素细节_" + motif, typeof(RectTransform), typeof(CanvasRenderer));
            node.transform.SetParent(parent, false);
            var rect = (RectTransform)node.transform;
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = PixelPresentationMotion.Snap(position);
            var effect = node.AddComponent<PeripheralPixelVfx>();
            effect.motif = motif;
            effect.duration = UiMotionProfile.FromIntensity(intensity).StandardDuration * (motif == "focus" || motif == "page" ? 1f : 2f);
            effect.startedAt = Time.unscaledTime;
            effect.color = motif == "forge" || motif == "meal" || motif == "purchase" || motif == "page"
                ? FormalUiTheme.Amber : motif == "blocked" ? FormalUiTheme.Danger :
                motif == "heal" || motif == "check" ? FormalUiTheme.Safe : FormalUiTheme.Cyan;
            effect.raycastTarget = false;
            return effect;
        }

        public static PeripheralPixelVfx SpawnRoute(Transform parent, Vector2[] points, float intensity)
        {
            if (points == null || points.Length < 2) return null;
            Vector2 bounds = parent is RectTransform rect ? rect.rect.size : new Vector2(1920f, 1080f);
            var effect = Spawn(parent, "route", intensity, bounds, Vector2.zero, new Vector2(.5f, .5f));
            if (effect != null) effect.route = (Vector2[])points.Clone();
            return effect;
        }

        private void Update()
        {
            if (Progress >= 1f) { Destroy(gameObject); return; }
            SetVerticesDirty();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            // Closing a page cancels feedback; reopening never resumes an old receipt.
            if (Application.isPlaying) Destroy(gameObject);
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            float step = Mathf.Floor(Progress * 12f) / 12f;
            Color tint = color;
            tint.a *= 1f - Mathf.Max(0f, (step - .5f) * 2f);
            Rect r = rectTransform.rect;
            if (route != null)
            {
                float length = 0f;
                for (int i = 1; i < route.Length; i++) length += Vector2.Distance(route[i - 1], route[i]);
                for (int trail = 0; trail < 3; trail++)
                {
                    float remaining = Mathf.Max(0f, step - trail * .08f) * length;
                    for (int i = 1; i < route.Length; i++)
                    {
                        float segment = Vector2.Distance(route[i - 1], route[i]);
                        if (remaining <= segment || i == route.Length - 1)
                        {
                            Color c = tint; c.a *= 1f - trail * .25f;
                            Quad(vh, Vector2.Lerp(route[i - 1], route[i], segment > 0f ? remaining / segment : 1f) - Vector2.one * 3f,
                                Vector2.one * 6f, c);
                            break;
                        }
                        remaining -= segment;
                    }
                }
                return;
            }
            if (motif == "focus" || motif == "page")
            {
                // A little travelling ruler tick on the border, leaving text untouched.
                float x = Mathf.Round(Mathf.Lerp(r.xMin + 4f, r.xMax - 12f, step) / 2f) * 2f;
                Quad(vh, new Vector2(x, r.yMin + 2f), new Vector2(12f, 2f), tint);
                Quad(vh, new Vector2(x, r.yMin + 4f), new Vector2(2f, 4f), tint);
                return;
            }
            if (motif == "blocked")
            {
                Quad(vh, new Vector2(-12f, -6f), new Vector2(24f, 4f), tint);
                Quad(vh, new Vector2(-12f, 2f), new Vector2(24f, 4f), tint);
            }
            else if (motif == "forge")
            {
                for (int i = 0; i < 8; i++)
                {
                    float angle = i * Mathf.PI / 4f;
                    Vector2 p = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (12f + step * 22f);
                    p.y -= step * step * 14f;
                    Quad(vh, p, new Vector2(i % 2 == 0 ? 4f : 2f, 2f), tint);
                }
            }
            else if (motif == "specialize")
            {
                float radius = 24f * (1f - step) + 6f;
                for (int i = 0; i < 4; i++)
                {
                    float angle = i * Mathf.PI / 2f;
                    Quad(vh, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, new Vector2(4f, 4f), tint);
                }
            }
            else if (motif == "meal")
            {
                for (int i = -1; i <= 1; i++)
                    Quad(vh, new Vector2(i * 10f + (step < .5f ? 2f : -2f), 6f + step * 20f), new Vector2(2f, 8f), tint);
            }
            else if (motif == "heal")
            {
                Quad(vh, new Vector2(-12f, -2f), new Vector2(24f, 4f), tint);
                Quad(vh, new Vector2(-2f, -12f), new Vector2(4f, 24f), tint);
                Quad(vh, new Vector2(-22f, -20f + step * 40f), new Vector2(44f, 2f), tint);
            }
            else
            {
                // A small receipt checkmark: confirmation, never rarity confetti.
                for (int i = 0; i < 3; i++) Quad(vh, new Vector2(-14f + i * 4f, 2f - i * 4f), new Vector2(4f, 4f), tint);
                for (int i = 0; i < 5; i++) Quad(vh, new Vector2(-2f + i * 4f, -6f + i * 4f), new Vector2(4f, 4f), tint);
            }
        }

        private static void Quad(VertexHelper vh, Vector2 position, Vector2 size, Color tint)
        {
            position = PixelPresentationMotion.Snap(position);
            int start = vh.currentVertCount;
            vh.AddVert(position, tint, Vector2.zero);
            vh.AddVert(position + new Vector2(0f, size.y), tint, Vector2.zero);
            vh.AddVert(position + size, tint, Vector2.zero);
            vh.AddVert(position + new Vector2(size.x, 0f), tint, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
