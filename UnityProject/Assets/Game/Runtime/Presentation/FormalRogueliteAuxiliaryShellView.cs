using System;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    [DisallowMultipleComponent]
    public sealed class FormalRogueliteAuxiliaryShellView : MonoBehaviour
    {
        public const string SettingsResourcePath = "UI/Prefabs/FormalRogueliteSettingsShell";
        public const string ArchiveResourcePath = "UI/Prefabs/FormalRogueliteArchiveShell";

        [SerializeField] private RectTransform card;
        [SerializeField] private RectTransform body;
        [SerializeField] private RectTransform footer;

        public RectTransform Card => card;
        public RectTransform Body => body;
        public RectTransform Footer => footer;

        public static FormalRogueliteAuxiliaryShellView CreateSettings(Transform parent) =>
            Create(parent, SettingsResourcePath, "设置页稳定骨架");

        public static FormalRogueliteAuxiliaryShellView CreateArchive(Transform parent) =>
            Create(parent, ArchiveResourcePath, "档案页稳定骨架");

        private static FormalRogueliteAuxiliaryShellView Create(Transform parent, string resourcePath, string runtimeName)
        {
            FormalRogueliteAuxiliaryShellView prefab = Resources.Load<FormalRogueliteAuxiliaryShellView>(resourcePath);
            if (prefab == null)
                throw new InvalidOperationException("Missing formal roguelite auxiliary shell prefab at Resources/" + resourcePath + ".prefab");
            FormalRogueliteAuxiliaryShellView view = Instantiate(prefab, parent, false);
            view.name = runtimeName;
            view.ValidateReferences();
            return view;
        }

        public void Configure(RectTransform cardReference, RectTransform bodyReference, RectTransform footerReference)
        {
            card = cardReference;
            body = bodyReference;
            footer = footerReference;
        }

        public void ValidateReferences()
        {
            if (card == null || body == null || footer == null)
                throw new InvalidOperationException("Formal roguelite auxiliary shell prefab has incomplete serialized references.");
            if (body.parent != card || footer.parent != card)
                throw new InvalidOperationException("Formal roguelite auxiliary shell regions must be direct children of the card.");
        }
    }
}
