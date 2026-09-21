using System;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    [DisallowMultipleComponent]
    public sealed class FormalRogueliteMapShellView : MonoBehaviour
    {
        public const string ResourcePath = "UI/Prefabs/FormalRogueliteMapShell";

        [SerializeField] private RectTransform header;
        [SerializeField] private RectTransform status;
        [SerializeField] private RectTransform mapViewport;
        [SerializeField] private RectTransform mapCanvas;
        [SerializeField] private RogueMapViewportController viewportController;

        public RectTransform Header => header;
        public RectTransform Status => status;
        public RectTransform MapViewport => mapViewport;
        public RectTransform MapCanvas => mapCanvas;
        public RogueMapViewportController ViewportController => viewportController;

        public static FormalRogueliteMapShellView Create(Transform parent)
        {
            FormalRogueliteMapShellView prefab = Resources.Load<FormalRogueliteMapShellView>(ResourcePath);
            if (prefab == null)
                throw new InvalidOperationException("Missing formal roguelite map shell prefab at Resources/" + ResourcePath + ".prefab");
            FormalRogueliteMapShellView view = Instantiate(prefab, parent, false);
            view.name = "肉鸽地图稳定骨架";
            view.ValidateReferences();
            return view;
        }

        public void Configure(RectTransform headerReference, RectTransform statusReference,
            RectTransform viewportReference, RectTransform canvasReference,
            RogueMapViewportController controllerReference)
        {
            header = headerReference;
            status = statusReference;
            mapViewport = viewportReference;
            mapCanvas = canvasReference;
            viewportController = controllerReference;
        }

        public void ValidateReferences()
        {
            if (header == null || status == null || mapViewport == null || mapCanvas == null || viewportController == null)
                throw new InvalidOperationException("Formal roguelite map shell prefab has incomplete serialized references.");
            if (mapViewport.GetComponent<RectMask2D>() == null)
                throw new InvalidOperationException("Formal roguelite map shell viewport must contain RectMask2D.");
        }
    }
}
