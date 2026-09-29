using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    public sealed class BattlefieldEnemyStatusChipView : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private Text label;
        [SerializeField] private Text count;
        [SerializeField] private Image accent;

        public bool HasRequiredBindings => icon != null && label != null && count != null && accent != null;

        public void Bind(string displayName, string stackCount, string detail, string iconPath,
            Color tint, FormalHoverTooltip tooltip)
        {
            label.text = displayName;
            count.text = stackCount;
            accent.color = tint;
            icon.sprite = Resources.Load<Sprite>(iconPath) ??
                Resources.Load<Sprite>("Art/FormalResourceIcons32/notice");
            icon.color = icon.sprite == null ? Color.clear : Color.white;
            FormalHoverTooltipTrigger trigger = GetComponent<FormalHoverTooltipTrigger>() ??
                gameObject.AddComponent<FormalHoverTooltipTrigger>();
            trigger.Configure(tooltip, () => new FormalTooltipContent(displayName + " " + stackCount, detail, tint));
        }
    }
}
