using UnityEngine;

namespace OCC.Combat.Presentation
{
    /// <summary>
    /// Scene-authored canvas roots for stable presentation layers. Their dynamic children are
    /// still bound by the presentation controllers at runtime, but the Canvas, scaler and
    /// raycaster settings now remain inspectable and reusable in the scene/prefab.
    /// </summary>
    public sealed class PresentationSceneAnchors : MonoBehaviour
    {
        public static Canvas TryAcquireCanvas(string name)
        {
            PresentationSceneAnchors anchors = FindFirstObjectByType<PresentationSceneAnchors>(FindObjectsInactive.Include);
            if (anchors == null) return null;

            Transform target = anchors.transform.Find(name);
            if (target == null) return null;

            Canvas canvas = target.GetComponent<Canvas>();
            if (canvas == null) return null;
            target.gameObject.SetActive(true);
            return canvas;
        }

        public static Transform TryAcquireAcademyMapRoot()
        {
            PresentationSceneAnchors anchors = FindFirstObjectByType<PresentationSceneAnchors>(FindObjectsInactive.Include);
            if (anchors == null) return null;

            Transform target = anchors.transform.Find("学院地块地图_运行时");
            if (target == null) return null;
            target.gameObject.SetActive(true);
            return target;
        }
    }
}
