using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    // Pinned paper reacts to intent to open a new archive; it never represents a saved result.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ArchivePaperDetail : MaskableGraphic, ICanvasRaycastFilter
    {
        private Func<float> intensity;
        private bool pulling;
        private float progress;
        private int drawnStep = -1;
        public const string PaperArtPath = "Art/ArchiveUiFormal/archive_memo_note";
        private Sprite paperArt;
        private readonly Vector2[] polygon = new Vector2[8];
        private readonly Vector2[] clipped = new Vector2[8];
        private readonly float[] xs = { -86f, 0f, 0f, 76f };
        private readonly float[] ys = { -38f, 0f, 0f, 44f };
        private readonly float[] us = { 0f, 0f, 0f, 1f };
        private readonly float[] vs = { 0f, 0f, 0f, 1f };
        private Vector4 artUv;
        public float Progress => progress;
        public override Texture mainTexture => paperArt == null ? base.mainTexture : paperArt.texture;

        public static ArchivePaperDetail Create(Transform parent, Func<float> motion)
        {
            var go = new GameObject("夹住的空白档案纸", typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0f, .5f);
            rect.anchoredPosition = new Vector2(108f, 0f);
            rect.sizeDelta = new Vector2(224f, 136f);
            var detail = go.AddComponent<ArchivePaperDetail>();
            detail.intensity = motion;
            detail.paperArt = Resources.Load<Sprite>(PaperArtPath);
            if (detail.paperArt == null) throw new InvalidOperationException("Missing formal archive paper: " + PaperArtPath);
            detail.raycastTarget = true;
            return detail;
        }

        // Only the visible sheet inside its owning button participates in input.
        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            var owner = GetComponentInParent<Button>();
            if (owner == null || !RectTransformUtility.RectangleContainsScreenPoint((RectTransform)owner.transform, screenPoint, eventCamera)) return false;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out Vector2 local)) return false;
            return local.x >= -88f && local.x <= 84f && local.y >= -48f && local.y <= 44f;
        }

        public void SetPulling(bool value) => pulling = value;

        private void Update() => Advance(Time.unscaledDeltaTime);

        public void Advance(float seconds)
        {
            float motion = intensity == null ? 0f : intensity();
            progress = motion <= 0f ? 0f : Mathf.MoveTowards(progress, pulling ? 1f : 0f,
                Mathf.Max(0f, seconds) / ((pulling ? .28f : .42f) * Mathf.Max(.35f, motion)));
            int step = Mathf.RoundToInt(progress * 12f);
            if (step == drawnStep) return;
            drawnStep = step;
            SetVerticesDirty();
        }

        protected override void OnDisable()
        {
            pulling = false;
            progress = 0f;
            drawnStep = -1;
            base.OnDisable();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (paperArt == null) return;
            artUv = UnityEngine.Sprites.DataUtility.GetOuterUV(paperArt);
            Vector4 border = paperArt.border;
            float units = (canvas == null ? 100f : canvas.referencePixelsPerUnit) / paperArt.pixelsPerUnit;
            float horizontal = Mathf.Min(1f, (xs[3] - xs[0]) / Mathf.Max(1f, (border.x + border.z) * units));
            float vertical = Mathf.Min(1f, (ys[3] - ys[0]) / Mathf.Max(1f, (border.y + border.w) * units));
            xs[1] = xs[0] + border.x * units * horizontal; xs[2] = xs[3] - border.z * units * horizontal;
            ys[1] = ys[0] + border.y * units * vertical; ys[2] = ys[3] - border.w * units * vertical;
            us[1] = border.x / paperArt.rect.width; us[2] = 1f - border.z / paperArt.rect.width;
            vs[1] = border.y / paperArt.rect.height; vs[2] = 1f - border.w / paperArt.rect.height;
            float p = Mathf.Round(progress * 12f) / 12f;
            float t = p * p * (3f - 2f * p);
            float curl = Mathf.Round(t * 20f / 2f) * 2f;
            // The imported paper supplies fibres, worn edges and the stationary brass pin.
            // UV nine-slicing preserves its border instead of shrinking the whole artwork.
            Paper(vh, new Color(0f, 0f, 0f, .13f), new Vector2(4, -10), 0f, 0f);
            Paper(vh, new Color(.84f, .81f, .74f), new Vector2(6, -8), 0f, 0f);
            Paper(vh, new Color(.94f, .92f, .86f), new Vector2(2, -4), 0f, 0f);
            Paper(vh, new Color(.22f, .17f, .1f, .18f), new Vector2(2, -2), t, curl);
            Paper(vh, Color.white, Vector2.zero, t, curl);
            if (curl > 0)
            {
                Vector2 a = Bend(new Vector2(76 - curl, -38), t);
                Vector2 b = Bend(new Vector2(76, -38 + curl), t);
                Vector2 tip = PixelPresentationMotion.Snap(new Vector2(a.x + 2f, b.y - 2f));
                Vector2 uv = Uv(new Vector2(.5f, .5f));
                // A close contact shadow, then a cream reverse face and a thin fold highlight.
                Tri(vh, a + new Vector2(2, -2), b + new Vector2(2, -2), tip + new Vector2(2, -2),
                    uv, uv, uv, new Color(.24f, .18f, .1f, .3f));
                Tri(vh, a, b, tip, Uv(new Vector2(.45f, .45f)), Uv(new Vector2(.62f, .45f)),
                    Uv(new Vector2(.45f, .62f)), new Color(.95f, .92f, .84f));
                Tri(vh, a, b, a + new Vector2(0, 2), uv, uv, uv, new Color(1f, 1f, 1f, .65f));
            }
        }

        private Vector2 Uv(Vector2 unit)
        {
            return new Vector2(Mathf.Lerp(artUv.x, artUv.z, unit.x), Mathf.Lerp(artUv.y, artUv.w, unit.y));
        }

        private Vector2 Bend(Vector2 point, float t)
        {
            float free = Mathf.Clamp01((ys[2] - point.y) / Mathf.Max(1f, ys[2] - ys[0]));
            return PixelPresentationMotion.Snap(point - new Vector2(t * 12f * free, 0f));
        }

        private void Paper(VertexHelper vh, Color tint, Vector2 offset, float t, float curl)
        {
            for (int y = 0; y < 3; y++) for (int x = 0; x < 3; x++)
            {
                polygon[0] = new Vector2(xs[x], ys[y]); polygon[1] = new Vector2(xs[x], ys[y + 1]);
                polygon[2] = new Vector2(xs[x + 1], ys[y + 1]); polygon[3] = new Vector2(xs[x + 1], ys[y]);
                int count = ClipCorner(curl);
                for (int i = 1; i < count - 1; i++)
                {
                    Vector2 a = clipped[0], b = clipped[i], c = clipped[i + 1];
                    Tri(vh, Bend(a, t) + offset, Bend(b, t) + offset, Bend(c, t) + offset,
                        CellUv(a, x, y, xs, ys, us, vs), CellUv(b, x, y, xs, ys, us, vs),
                        CellUv(c, x, y, xs, ys, us, vs), tint);
                }
            }
        }

        private Vector2 CellUv(Vector2 point, int x, int y, float[] xs, float[] ys, float[] us, float[] vs)
            => Uv(new Vector2(Mathf.Lerp(us[x], us[x + 1], Mathf.InverseLerp(xs[x], xs[x + 1], point.x)),
                Mathf.Lerp(vs[y], vs[y + 1], Mathf.InverseLerp(ys[y], ys[y + 1], point.y))));

        private int ClipCorner(float curl)
        {
            int count = 0;
            for (int i = 0; i < 4; i++)
            {
                Vector2 a = polygon[i], b = polygon[(i + 1) % 4];
                float da = 114f - curl - (a.x - a.y), db = 114f - curl - (b.x - b.y);
                if (da >= 0) clipped[count++] = a;
                if ((da >= 0) != (db >= 0)) clipped[count++] = Vector2.Lerp(a, b, da / (da - db));
            }
            return count;
        }

        private static void Tri(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c,
            Vector2 ua, Vector2 ub, Vector2 uc, Color color)
        {
            int index = vh.currentVertCount;
            vh.AddVert(a, color, ua); vh.AddVert(b, color, ub); vh.AddVert(c, color, uc);
            vh.AddTriangle(index, index + 1, index + 2);
        }
    }

    public sealed class ArchivePaperPullTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private ArchivePaperDetail paper;
        public void Configure(ArchivePaperDetail target) => paper = target;
        public void OnPointerEnter(PointerEventData data) { if (paper != null) paper.SetPulling(true); }
        public void OnPointerExit(PointerEventData data) { if (paper != null) paper.SetPulling(false); }
        private void OnDisable() { if (paper != null) paper.SetPulling(false); }
    }
}
