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
            image.preserveAspect = false;
            image.color = Color.white;
        }

        public static void AddAmbientScanlines(Transform parent, float intensity)
        {
            GameObject result = FormalUiKit.Create("学院档案纸纹层", parent);
            RectTransform rect = result.AddComponent<RectTransform>();
            FormalUiKit.Stretch(rect);
            Image image = result.AddComponent<Image>(); image.sprite = null; image.color = Color.clear; image.raycastTarget = false;
            result.transform.SetSiblingIndex(Mathf.Min(1, parent.childCount - 1));
            Sprite spine = Decoration("binding_spine");
            for (int i = 0; i < 5; i++)
                ArchiveSprite(result.transform, "布面页脊_" + i, spine, new Vector2(0f, -i * 230f), new Vector2(128f, 256f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            ArchiveSprite(result.transform, "档案角扣", Decoration("corner_clasp"), new Vector2(128f, -32f), new Vector2(128f, 128f), new Vector2(0f, 1f), new Vector2(0f, 1f));
            ArchiveSprite(result.transform, "索引页签", Decoration("index_tab"), new Vector2(-24f, -300f), new Vector2(256f, 128f), Vector2.one, Vector2.one);
            ArchiveSprite(result.transform, "折页", Decoration("folded_corner"), new Vector2(-8f, -8f), new Vector2(256f, 256f), Vector2.one, Vector2.one);
            ArchiveSprite(result.transform, "测量尺", Decoration("measure_ruler"), new Vector2(160f, 20f), new Vector2(256f, 128f), Vector2.zero, Vector2.zero);
            ArchiveSprite(result.transform, "状态纸夹", Decoration("status_clip"), new Vector2(-72f, 20f), new Vector2(128f, 128f), new Vector2(1f, 0f), new Vector2(1f, 0f));
        }

        public static void AddEmptyIllustration(Transform parent, string id, Vector2 position, float size)
        {
            Sprite sprite = Resources.Load<Sprite>(FormalUiEffectsConfig.IllustrationPath(id));
            if (sprite == null) throw new KeyNotFoundException("Missing formal UI empty illustration: " + id);
            GameObject result = FormalUiKit.Create("空状态插图_" + id, parent);
            RectTransform rect = result.AddComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = position; rect.sizeDelta = new Vector2(size, size);
            Image image = result.AddComponent<Image>(); image.sprite = sprite; image.type = Image.Type.Simple; image.preserveAspect = true; image.color = Color.white; image.raycastTarget = false;
        }

        public static void AddChapterDivider(Transform parent, string id, Vector2 position, float scale)
        {
            Sprite sprite = Resources.Load<Sprite>(FormalUiEffectsConfig.ChapterDividerPath(id));
            if (sprite == null) throw new KeyNotFoundException("Missing formal UI chapter divider: " + id);
            GameObject result = FormalUiKit.Create("章节分隔横幅_" + id, parent);
            RectTransform rect = result.AddComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, .5f); rect.anchoredPosition = position; rect.sizeDelta = new Vector2(128f * scale, 32f * scale);
            Image image = result.AddComponent<Image>(); image.sprite = sprite; image.type = Image.Type.Simple; image.preserveAspect = true; image.color = Color.white; image.raycastTarget = false;
        }

        public static void AddChapterMarker(Transform parent, string id, Vector2 position, float scale)
        {
            Sprite sprite = Resources.Load<Sprite>(FormalUiEffectsConfig.ChapterMarkerPath(id));
            if (sprite == null) throw new KeyNotFoundException("Missing formal UI chapter marker: " + id);
            GameObject result = FormalUiKit.Create("章节角标_" + id, parent);
            RectTransform rect = result.AddComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = position; rect.sizeDelta = new Vector2(32f * scale, 32f * scale);
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
            RectTransform rect = mark.AddComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot; rect.anchoredPosition = position; rect.sizeDelta = size;
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
