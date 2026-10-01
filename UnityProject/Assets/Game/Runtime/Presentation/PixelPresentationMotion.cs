using System.Collections;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    // Move existing artwork on a pixel grid; never stretch its native texels.
    public sealed class PixelPresentationMotion : MonoBehaviour
    {
        public static Vector2 Snap(Vector2 value, float step = 2f)
        {
            step = Mathf.Max(1f, step);
            return new Vector2(Mathf.Round(value.x / step) * step, Mathf.Round(value.y / step) * step);
        }

        public static void EnterPage(Transform page, float intensity)
        {
            if (intensity <= .01f || !Application.isPlaying) return;
            int index = 0;
            foreach (Transform child in page)
            {
                if (child.name == "学院档案装饰层") continue;
                RectTransform rect = child as RectTransform;
                if (rect == null) continue;
                child.gameObject.AddComponent<PixelPresentationMotion>().StartCoroutine(
                    Reveal(rect, Mathf.Min(index++ * .035f, .105f), intensity));
            }
        }

        private static IEnumerator Reveal(RectTransform rect, float delay, float intensity)
        {
            Vector2 rest = rect.anchoredPosition;
            CanvasGroup group = rect.GetComponent<CanvasGroup>();
            if (group == null) group = rect.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            float duration = Mathf.Lerp(.12f, .24f, Mathf.Clamp01(intensity));
            float started = Time.unscaledTime + delay;
            while (rect != null && Time.unscaledTime < started + duration)
            {
                float t = Mathf.Clamp01((Time.unscaledTime - started) / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                rect.anchoredPosition = rest + Snap(Vector2.down * (12f * (1f - eased)));
                group.alpha = Mathf.Round(t * 8f) / 8f;
                yield return null;
            }
            if (rect != null) { rect.anchoredPosition = rest; group.alpha = 1f; }
        }
    }
}
