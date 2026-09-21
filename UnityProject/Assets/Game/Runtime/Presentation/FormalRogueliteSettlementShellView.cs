using System;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    [DisallowMultipleComponent]
    public sealed class FormalRogueliteSettlementShellView : MonoBehaviour
    {
        public const string ResourcePath = "UI/Prefabs/FormalRogueliteSettlementShell";

        [SerializeField] private RectTransform card;
        [SerializeField] private RectTransform header;
        [SerializeField] private RectTransform rewards;
        [SerializeField] private RectTransform footer;

        public RectTransform Card => card;
        public RectTransform Header => header;
        public RectTransform Rewards => rewards;
        public RectTransform Footer => footer;

        public static FormalRogueliteSettlementShellView Create(Transform parent)
        {
            FormalRogueliteSettlementShellView prefab = Resources.Load<FormalRogueliteSettlementShellView>(ResourcePath);
            if (prefab == null)
                throw new InvalidOperationException("Missing formal roguelite settlement shell prefab at Resources/" + ResourcePath + ".prefab");
            FormalRogueliteSettlementShellView view = Instantiate(prefab, parent, false);
            view.name = "战后奖励稳定骨架";
            view.ValidateReferences();
            return view;
        }

        public void Configure(RectTransform cardReference, RectTransform headerReference,
            RectTransform rewardsReference, RectTransform footerReference)
        {
            card = cardReference;
            header = headerReference;
            rewards = rewardsReference;
            footer = footerReference;
        }

        public void ValidateReferences()
        {
            if (card == null || header == null || rewards == null || footer == null)
                throw new InvalidOperationException("Formal roguelite settlement shell prefab has incomplete serialized references.");
            if (header.parent != card || rewards.parent != card || footer.parent != card)
                throw new InvalidOperationException("Formal roguelite settlement shell regions must be direct children of the settlement card.");
        }
    }
}
