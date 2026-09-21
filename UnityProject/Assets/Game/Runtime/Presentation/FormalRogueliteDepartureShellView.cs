using System;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    [DisallowMultipleComponent]
    public sealed class FormalRogueliteDepartureShellView : MonoBehaviour
    {
        public const string ResourcePath = "UI/Prefabs/FormalRogueliteDepartureShell";

        [SerializeField] private RectTransform body;
        [SerializeField] private RectTransform footer;
        [SerializeField] private RectTransform dossier;
        [SerializeField] private RectTransform topBar;

        public RectTransform Body => body;
        public RectTransform Footer => footer;
        public RectTransform Dossier => dossier;
        public RectTransform TopBar => topBar;

        public static FormalRogueliteDepartureShellView Create(Transform parent)
        {
            FormalRogueliteDepartureShellView prefab = Resources.Load<FormalRogueliteDepartureShellView>(ResourcePath);
            if (prefab == null)
                throw new InvalidOperationException("Missing formal roguelite departure shell prefab at Resources/" + ResourcePath + ".prefab");
            FormalRogueliteDepartureShellView view = Instantiate(prefab, parent, false);
            view.name = "战斗出发稳定骨架";
            view.ValidateReferences();
            return view;
        }

        public void Configure(RectTransform bodyReference, RectTransform footerReference,
            RectTransform dossierReference, RectTransform topBarReference)
        {
            body = bodyReference;
            footer = footerReference;
            dossier = dossierReference;
            topBar = topBarReference;
        }

        public void ValidateReferences()
        {
            if (body == null || footer == null || dossier == null || topBar == null)
                throw new InvalidOperationException("Formal roguelite departure shell prefab has incomplete serialized references.");
        }
    }
}
