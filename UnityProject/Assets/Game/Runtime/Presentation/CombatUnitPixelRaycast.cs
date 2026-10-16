using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    /// <summary>Legacy component: unit art always passes input to the logical grid.</summary>
    [RequireComponent(typeof(RawImage))]
    public sealed class CombatUnitPixelRaycast : MonoBehaviour, ICanvasRaycastFilter
    {
        private void OnEnable() => GetComponent<RawImage>().raycastTarget = false;

        // Retained for existing component references; alpha masks are now only
        // used by visual contours and must never re-enable sprite targeting.
        public void Bind(RawImage graphic, CombatTextureAlphaMask alpha)
        {
            if (graphic != null) graphic.raycastTarget = false;
        }

        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera) => false;
    }
}
