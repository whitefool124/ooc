using System;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    [DisallowMultipleComponent]
    public sealed class FormalRogueliteServiceShellView : MonoBehaviour
    {
        public const string ResourcePath = "UI/Prefabs/FormalRogueliteServiceShell";

        [SerializeField] private RectTransform body;
        [SerializeField] private RectTransform footer;
        [SerializeField] private RectTransform topBar;

        public RectTransform Body => body;
        public RectTransform Footer => footer;
        public RectTransform TopBar => topBar;

        public static FormalRogueliteServiceShellView Create(Transform parent)
        {
            FormalRogueliteServiceShellView prefab = Resources.Load<FormalRogueliteServiceShellView>(ResourcePath);
            if (prefab == null)
                throw new InvalidOperationException("Missing formal roguelite service shell prefab at Resources/" + ResourcePath + ".prefab");
            FormalRogueliteServiceShellView view = Instantiate(prefab, parent, false);
            view.name = "服务节点稳定骨架";
            view.ValidateReferences();
            return view;
        }

        public void Configure(RectTransform bodyReference, RectTransform footerReference, RectTransform topBarReference)
        {
            body = bodyReference;
            footer = footerReference;
            topBar = topBarReference;
        }

        public void ValidateReferences()
        {
            if (body == null || footer == null || topBar == null)
                throw new InvalidOperationException("Formal roguelite service shell prefab has incomplete serialized references.");
        }
    }
}
