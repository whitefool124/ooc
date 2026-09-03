using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    public static class FormalUiEffects
    {
        private static readonly Dictionary<string, Sprite[]> FrameCache = new Dictionary<string, Sprite[]>();

        public static void ApplyBackdrop(Image image, string id)
        {
            Sprite sprite = Resources.Load<Sprite>(FormalUiEffectsConfig.BackdropPath(id));
            if (sprite == null) throw new KeyNotFoundException("Missing formal UI backdrop: " + id);
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
            image.color = Color.white;
        }

        public static void AddPageDecorations(Transform parent, string pageId, float intensity)
        {
            GameObject result = FormalUiKit.Create("学院档案装饰层", parent);
            RectTransform rect = result.AddComponent<RectTransform>();
            FormalUiKit.Stretch(rect);
            Image image = result.AddComponent<Image>(); image.sprite = null; image.color = Color.clear; image.raycastTarget = false;
            result.transform.SetSiblingIndex(Mathf.Min(1, parent.childCount - 1));
            if (pageId == "archive" || pageId == "landing" || pageId == "startup")
            {
                Sprite spine = Decoration("binding_spine");
                for (int i = 0; i < 5; i++)
                    ArchiveSprite(result.transform, "布面页脊_" + i, spine, new Vector2(0f, -i * 230f), new Vector2(128f, 256f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            }
            if (pageId == "landing" || pageId == "startup" || pageId == "settlement" || pageId == "settings")
                ArchiveSprite(result.transform, "档案角扣", Decoration("corner_clasp"), new Vector2(128f, -32f), new Vector2(128f, 128f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            if (pageId == "archive" || pageId == "inventory" || pageId == "settings")
                ArchiveSprite(result.transform, "索引页签", Decoration("index_tab"), new Vector2(-24f, -300f), new Vector2(256f, 128f), Vector2.one, Vector2.one);
            if (pageId == "archive" || pageId == "settlement")
                ArchiveSprite(result.transform, "折页", Decoration("folded_corner"), new Vector2(-8f, -8f), new Vector2(256f, 256f), Vector2.one, Vector2.one);
            if (pageId == "map" || pageId == "briefing" || pageId == "inventory")
                ArchiveSprite(result.transform, "测量尺", Decoration("measure_ruler"), new Vector2(160f, 20f), new Vector2(256f, 128f), Vector2.zero, Vector2.zero);
            if (pageId == "map" || pageId == "briefing" || pageId == "inventory" || pageId == "settlement")
                ArchiveSprite(result.transform, "状态纸夹", Decoration("status_clip"), new Vector2(-72f, 20f), new Vector2(128f, 128f), new Vector2(1f, 0f), new Vector2(1f, 0f));
        }

        public static void AddEmptyIllustration(Transform parent, string id, Vector2 position, float size)
        {
            Sprite sprite = Resources.Load<Sprite>(FormalUiEffectsConfig.IllustrationPath(id));
            if (sprite == null) throw new KeyNotFoundException("Missing formal UI empty illustration: " + id);
            GameObject result = FormalUiKit.Create("空状态插图_" + id, parent);
            int alignedSize = FormalUiKit.IntegerSpriteSize(sprite, size);
            RectTransform rect = result.AddComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = position; rect.sizeDelta = new Vector2(alignedSize, alignedSize);
            Image image = result.AddComponent<Image>(); image.sprite = sprite; image.type = Image.Type.Simple; image.preserveAspect = true; image.color = Color.white; image.raycastTarget = false;
        }

        public static void AddChapterDivider(Transform parent, string id, Vector2 position, float scale)
        {
            Sprite sprite = Resources.Load<Sprite>(FormalUiEffectsConfig.ChapterDividerPath(id));
            if (sprite == null) throw new KeyNotFoundException("Missing formal UI chapter divider: " + id);
            int integerScale = Mathf.Max(1, Mathf.RoundToInt(scale));
            GameObject result = FormalUiKit.Create("章节分隔横幅_" + id, parent);
            RectTransform rect = result.AddComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, .5f); rect.anchoredPosition = position; rect.sizeDelta = new Vector2(128f * integerScale, 32f * integerScale);
            Image image = result.AddComponent<Image>(); image.sprite = sprite; image.type = Image.Type.Simple; image.preserveAspect = true; image.color = Color.white; image.raycastTarget = false;
        }

        public static void AddChapterMarker(Transform parent, string id, Vector2 position, float scale)
        {
            Sprite sprite = Resources.Load<Sprite>(FormalUiEffectsConfig.ChapterMarkerPath(id));
            if (sprite == null) throw new KeyNotFoundException("Missing formal UI chapter marker: " + id);
            int integerScale = Mathf.Max(1, Mathf.RoundToInt(scale));
            GameObject result = FormalUiKit.Create("章节角标_" + id, parent);
            RectTransform rect = result.AddComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = position; rect.sizeDelta = new Vector2(32f * integerScale, 32f * integerScale);
            Image image = result.AddComponent<Image>(); image.sprite = sprite; image.type = Image.Type.Simple; image.preserveAspect = true; image.color = Color.white; image.raycastTarget = false;
        }

        private static Sprite Decoration(string id)
        {
            Sprite sprite = Resources.Load<Sprite>(FormalUiEffectsConfig.DecorationPath(id));
            if (sprite == null) throw new KeyNotFoundException("Missing formal UI decoration: " + id);
            return sprite;
        }

        private static void ArchiveSprite(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 size, Vector2 anchor, Vector2 pivot)
        {
            GameObject mark = FormalUiKit.Create(name, parent);
            float nativeWidth = Mathf.Max(1f, sprite.rect.width);
            float nativeHeight = Mathf.Max(1f, sprite.rect.height);
            int multiplier = Mathf.Max(1, Mathf.RoundToInt(Mathf.Min(size.x / nativeWidth, size.y / nativeHeight)));
            RectTransform rect = mark.AddComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot; rect.anchoredPosition = position; rect.sizeDelta = new Vector2(nativeWidth * multiplier, nativeHeight * multiplier);
            Image image = mark.AddComponent<Image>(); image.sprite = sprite; image.preserveAspect = true; image.color = Color.white; image.raycastTarget = false;
        }

        public static void PlayPageWipe(Transform parent, float intensity)
        {
            if (intensity <= 0f) return;
            Sprite sprite = Resources.Load<Sprite>(FormalUiEffectsConfig.Data.transitionSprite);
            if (sprite == null) throw new KeyNotFoundException("Missing formal transition sprite");
            GameObject result = FormalUiKit.Create("像素擦除转场", parent);
            RectTransform rect = result.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.pivot = new Vector2(.5f, .5f); rect.sizeDelta = new Vector2(2300f, 1080f);
            Image image = result.AddComponent<Image>(); image.sprite = sprite; image.type = Image.Type.Simple; image.raycastTarget = false;
            result.transform.SetAsLastSibling();
            float duration = FormalUiEffectsConfig.Data.transitionSeconds * intensity;
            rect.anchoredPosition = new Vector2(-2300f, 0f);
            DOTween.Sequence().SetUpdate(true).SetTarget(result)
                .Append(DOTween.To(() => rect.anchoredPosition.x, value => rect.anchoredPosition = new Vector2(value, 0f), 0f, duration * .48f).SetEase(Ease.InQuad))
                .Append(DOTween.To(() => rect.anchoredPosition.x, value => rect.anchoredPosition = new Vector2(value, 0f), 2300f, duration * .52f).SetEase(Ease.OutQuad))
                .OnComplete(() => Object.Destroy(result));
        }

        public static void SpawnLocalFeedback(Transform anchor, string id, float intensity, Vector2? offset = null)
        {
            if (anchor == null || intensity <= 0f) return;
            OccPeripheralFeedbackEntry entry = FormalUiEffectsConfig.Feedback(id);
            Sprite[] frames = Frames(entry);
            GameObject result = FormalUiKit.Create("像素反馈_" + id, anchor);
            RectTransform rect = result.AddComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = offset ?? Vector2.zero; rect.sizeDelta = new Vector2(64f, 64f);
            Image image = result.AddComponent<Image>(); image.sprite = frames[0]; image.preserveAspect = true; image.raycastTarget = false;
            result.transform.SetAsLastSibling();
            PixelUiFrameAnimator animator = result.AddComponent<PixelUiFrameAnimator>();
            animator.Play(frames, entry.framesPerSecond / Mathf.Max(.35f, intensity));
        }

        private static Sprite[] Frames(OccPeripheralFeedbackEntry entry)
        {
            if (FrameCache.TryGetValue(entry.id, out Sprite[] cached)) return cached;
            var frames = new Sprite[entry.frameCount];
            for (int i = 0; i < frames.Length; i++)
            {
                frames[i] = Resources.Load<Sprite>(entry.resourcePath + "/frame_" + i.ToString("00"));
                if (frames[i] == null) throw new KeyNotFoundException("Missing formal UI feedback frame: " + entry.id + "/" + i);
            }
            FrameCache.Add(entry.id, frames);
            return frames;
        }
    }

    public static class FormalUiAssetPlacement
    {
        public static string ChapterDivider(RogueliteMapNode node)
        {
            if (node == null) return "teaching_record";
            if (node.Type == RogueliteMapNodeType.Elite || node.Type == RogueliteMapNodeType.Finale || node.Type == RogueliteMapNodeType.Treasure) return "sealed_dossier";
            if (node.Type == RogueliteMapNodeType.Workshop || node.Type == RogueliteMapNodeType.Shop || ContainsAny(node, "workshop", "foundry", "refinery", "工坊", "校准")) return "workshop_record";
            if (node.Type == RogueliteMapNodeType.Rest || ContainsAny(node, "clinic", "infirmary", "med_", "医务", "诊疗")) return "infirmary_record";
            if (node.Type == RogueliteMapNodeType.Event || ContainsAny(node, "wild", "field", "courtyard", "path", "郊野", "石庭")) return "field_survey";
            return "teaching_record";
        }

        public static string ChapterMarker(RogueliteMapNode node)
        {
            switch (ChapterDivider(node))
            {
                case "sealed_dossier": return "sealed_red_clip";
                case "workshop_record": return "workshop_caliper_clip";
                case "infirmary_record": return "infirmary_bandage_clip";
                case "field_survey": return "field_leaf_clip";
                default: return "teaching_chalk_clip";
            }
        }

        private static bool ContainsAny(RogueliteMapNode node, params string[] fragments)
        {
            string source = (node.Id + " " + node.DisplayName).ToLowerInvariant();
            for (int i = 0; i < fragments.Length; i++)
                if (source.Contains(fragments[i].ToLowerInvariant())) return true;
            return false;
        }
    }

    public sealed class PixelUiFrameAnimator : MonoBehaviour
    {
        private Coroutine routine;

        public void Play(Sprite[] frames, float framesPerSecond)
        {
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(Animate(frames, Mathf.Max(1f, framesPerSecond)));
        }

        private IEnumerator Animate(Sprite[] frames, float framesPerSecond)
        {
            Image image = GetComponent<Image>();
            float delay = 1f / framesPerSecond;
            for (int i = 0; i < frames.Length; i++)
            {
                image.sprite = frames[i];
                float until = Time.unscaledTime + delay;
                while (Time.unscaledTime < until) yield return null;
            }
            Destroy(gameObject);
        }
    }
}
