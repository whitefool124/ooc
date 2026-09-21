using System;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    [DisallowMultipleComponent]
    public sealed class FormalRogueliteLoadoutShellView : MonoBehaviour
    {
        public const string ResourcePath = "UI/Prefabs/FormalRogueliteLoadoutShell";

        [SerializeField] private RectTransform card;
        [SerializeField] private RectTransform navigation;
        [SerializeField] private RectTransform body;
        [SerializeField] private RectTransform footer;

        public RectTransform Card => card;
        public RectTransform Navigation => navigation;
        public RectTransform Body => body;
        public RectTransform Footer => footer;

        public static FormalRogueliteLoadoutShellView Create(Transform parent)
        {
            FormalRogueliteLoadoutShellView prefab = Resources.Load<FormalRogueliteLoadoutShellView>(ResourcePath);
            if (prefab == null)
                throw new InvalidOperationException("Missing formal roguelite loadout shell prefab at Resources/" + ResourcePath + ".prefab");
            FormalRogueliteLoadoutShellView view = Instantiate(prefab, parent, false);
            view.name = "整备页稳定骨架";
            view.ValidateReferences();
            return view;
        }

        public void Configure(RectTransform cardReference, RectTransform navigationReference,
            RectTransform bodyReference, RectTransform footerReference)
        {
            card = cardReference;
            navigation = navigationReference;
            body = bodyReference;
            footer = footerReference;
        }

        public void ValidateReferences()
        {
            if (card == null || navigation == null || body == null || footer == null)
                throw new InvalidOperationException("Formal roguelite loadout shell prefab has incomplete serialized references.");
            if (navigation.parent != card || body.parent != card || footer.parent != card)
                throw new InvalidOperationException("Formal roguelite loadout shell regions must be direct children of the loadout card.");
        }
    }
}
