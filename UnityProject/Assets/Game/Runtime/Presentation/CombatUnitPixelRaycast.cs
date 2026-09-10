using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    /// <summary>Transparent sprite pixels leave the existing board interaction reachable.</summary>
    [RequireComponent(typeof(RawImage))]
    public sealed class CombatUnitPixelRaycast : MonoBehaviour, ICanvasRaycastFilter
    {
        private RawImage image;
        private CombatTextureAlphaMask mask;

        public void Bind(RawImage graphic, CombatTextureAlphaMask alpha)
        {
            image = graphic; mask = alpha;
            image.raycastTarget = mask != null;
        }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {
            if (image == null || mask == null || !isActiveAndEnabled || image.color.a <= .01f) return false;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(image.rectTransform, screenPoint, eventCamera, out Vector2 local)) return false;
            Rect rect = image.rectTransform.rect;
            if (rect.width <= 0 || rect.height <= 0) return false;
            float x = (local.x - rect.xMin) / rect.width;
            float y = (local.y - rect.yMin) / rect.height;
            if (x < 0 || x >= 1 || y < 0 || y >= 1) return false;
            Rect uv = image.uvRect;
            return mask.Sample(uv.x + x * uv.width, uv.y + y * uv.height);
        }
    }
}
