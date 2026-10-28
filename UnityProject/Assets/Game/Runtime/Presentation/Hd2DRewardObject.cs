using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    // Shared scene-paper geometry supplies lighting and thickness. Keep the image
    // enabled so it is not hidden behind a second renderer's paper depth layers.
    internal sealed class Hd2DRewardObject : MonoBehaviour
    {
        private Image source;
        private bool sized;
        internal static void Attach(GameObject target, Image icon, bool animated)
        {
            if (!Application.isPlaying || icon == null || icon.sprite == null) return;
            target.AddComponent<Hd2DRewardObject>().source = icon;
        }
        private void LateUpdate()
        {
            if (sized || source == null || source.sprite == null) return;
            var canvas = source.GetComponentInParent<Canvas>();
            if (canvas == null || canvas.renderMode != RenderMode.WorldSpace) return;
            var rect = source.rectTransform;
            Vector3 scale = rect.lossyScale;
            if (Mathf.Abs(scale.x) < .00001f || Mathf.Abs(scale.y) < .00001f) return;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(0, -20);
            Hd2DUiIconSizing.Apply(source);
            // Refresh the canvas mesh before shared geometry observes the new rect.
            Canvas.ForceUpdateCanvases();
            sized = true;
        }
    }
}
