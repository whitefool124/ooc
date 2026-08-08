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
            if (sprite == null) throw new KeyNotFoundException("Missing formal UI backdrop sprite: " + id);
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = Color.white;
        }

        public static void AddAmbientScanlines(Transform parent, float intensity)
        {
            Sprite sprite = Resources.Load<Sprite>(FormalUiEffectsConfig.Data.scanlineSprite);
            if (sprite == null) throw new KeyNotFoundException("Missing formal scanline sprite");
            GameObject result = FormalUiKit.Create("环境扫描层", parent);
            RectTransform rect = result.AddComponent<RectTransform>();
            FormalUiKit.Stretch(rect);
            rect.offsetMin = new Vector2(0f, -12f); rect.offsetMax = new Vector2(0f, 12f);
            Image image = result.AddComponent<Image>(); image.sprite = sprite; image.type = Image.Type.Simple; image.color = new Color(1f, 1f, 1f, .42f); image.raycastTarget = false;
            result.transform.SetAsFirstSibling();
            if (intensity <= 0f) return;
            rect.anchoredPosition = new Vector2(0f, -8f);
            DOTween.To(() => rect.anchoredPosition.y, value => rect.anchoredPosition = new Vector2(0f, value), 8f,
                FormalUiEffectsConfig.Data.ambientScanSeconds / Mathf.Max(.25f, intensity)).SetEase(Ease.Linear).SetLoops(-1, LoopType.Yoyo).SetUpdate(true).SetTarget(result);
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
