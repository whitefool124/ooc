using System;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    [Serializable]
    public sealed class RogueLoadoutSlotView
    {
        [SerializeField] private RectTransform root;
        [SerializeField] private Image background;
        [SerializeField] private Image icon;
        [SerializeField] private Image topAccent;
        [SerializeField] private Image leftAccent;
        [SerializeField] private Image bottomRule;
        [SerializeField] private Image rightRule;
        [SerializeField] private Image dropOverlay;
        [SerializeField] private Button button;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private RogueLoadoutDragHandler dragHandler;
        [SerializeField] private Text slotNumber;
        [SerializeField] private Text quantity;

        public RectTransform Root => root;
        public Image Background => background;
        public Image Icon => icon;
        public Image TopAccent => topAccent;
        public Image LeftAccent => leftAccent;
        public Image BottomRule => bottomRule;
        public Image RightRule => rightRule;
        public Image DropOverlay => dropOverlay;
        public Button Button => button;
        public CanvasGroup CanvasGroup => canvasGroup;
        public RogueLoadoutDragHandler DragHandler => dragHandler;
        public Text SlotNumber => slotNumber;
        public Text Quantity => quantity;

        public void ConfigureForAuthoring(RectTransform slotRoot, Image slotBackground, Image slotIcon,
            Image slotTopAccent, Image slotLeftAccent, Image slotBottomRule, Image slotRightRule,
            Image slotDropOverlay, Button slotButton, CanvasGroup slotCanvasGroup,
            RogueLoadoutDragHandler slotDragHandler, Text numberLabel, Text quantityLabel)
        {
            root = slotRoot;
            background = slotBackground;
            icon = slotIcon;
            topAccent = slotTopAccent;
            leftAccent = slotLeftAccent;
            bottomRule = slotBottomRule;
            rightRule = slotRightRule;
            dropOverlay = slotDropOverlay;
            button = slotButton;
            canvasGroup = slotCanvasGroup;
            dragHandler = slotDragHandler;
            slotNumber = numberLabel;
            quantity = quantityLabel;
        }

        public bool IsValid(bool requireDropOverlay, bool requireLabels)
        {
            return root != null && background != null && icon != null && topAccent != null &&
                leftAccent != null && bottomRule != null && rightRule != null && button != null &&
                canvasGroup != null && (!requireDropOverlay || dropOverlay != null) &&
                (!requireLabels || slotNumber != null && quantity != null);
        }

        public RogueLoadoutDragHandler EnsureDragHandler()
        {
            if (dragHandler == null && root != null)
                dragHandler = root.GetComponent<RogueLoadoutDragHandler>() ?? root.gameObject.AddComponent<RogueLoadoutDragHandler>();
            return dragHandler;
        }
    }

    /// <summary>
    /// Stable, inspectable skeleton for the roguelite preparation inventory. The prefab owns the
    /// three columns, nine equipment slots, four tactical slots and sixty backpack cells; the
    /// controller only binds run data and transient interaction state.
    /// </summary>
    public sealed class RogueLoadoutEquipmentView : MonoBehaviour
    {
        public const string ResourcePath = "UI/Prefabs/RogueLoadoutEquipmentView";

        [SerializeField] private RectTransform characterContentRoot;
        [SerializeField] private RectTransform backpackPanel;
        [SerializeField] private RectTransform backpackGrid;
        [SerializeField] private RectTransform backpackItemsRoot;
        [SerializeField] private RectTransform[] backpackCells = Array.Empty<RectTransform>();
        [SerializeField] private RogueLoadoutSlotView[] equipmentSlots = Array.Empty<RogueLoadoutSlotView>();
        [SerializeField] private RogueLoadoutSlotView[] tacticalSlots = Array.Empty<RogueLoadoutSlotView>();
        [SerializeField] private Text tacticalStatus;
        [SerializeField] private Text backpackStatus;

        public RectTransform CharacterContentRoot => characterContentRoot;
        public RectTransform BackpackPanel => backpackPanel;
        public RectTransform BackpackGrid => backpackGrid;
        public RectTransform BackpackItemsRoot => backpackItemsRoot;
        public RectTransform[] BackpackCells => backpackCells;
        public RogueLoadoutSlotView[] EquipmentSlots => equipmentSlots;
        public RogueLoadoutSlotView[] TacticalSlots => tacticalSlots;
        public Text TacticalStatus => tacticalStatus;
        public Text BackpackStatus => backpackStatus;

        public static RogueLoadoutEquipmentView Create(Transform parent)
        {
            GameObject prefab = Resources.Load<GameObject>(ResourcePath);
            if (prefab == null)
                throw new InvalidOperationException("Missing loadout equipment prefab at Resources/" + ResourcePath + ".prefab");
            RogueLoadoutEquipmentView view = Instantiate(prefab, parent, false).GetComponent<RogueLoadoutEquipmentView>();
            if (view == null || !view.IsValid())
                throw new InvalidOperationException("Loadout equipment prefab references are incomplete: Resources/" + ResourcePath + ".prefab");
            return view;
        }

        public void ConfigureForAuthoring(RectTransform characterRoot, RectTransform backpackPanelRoot,
            RectTransform gridRoot, RectTransform itemsRoot, RectTransform[] cells,
            RogueLoadoutSlotView[] equipment, RogueLoadoutSlotView[] tactical,
            Text tacticalStatusLabel, Text backpackStatusLabel)
        {
            characterContentRoot = characterRoot;
            backpackPanel = backpackPanelRoot;
            backpackGrid = gridRoot;
            backpackItemsRoot = itemsRoot;
            backpackCells = cells ?? Array.Empty<RectTransform>();
            equipmentSlots = equipment ?? Array.Empty<RogueLoadoutSlotView>();
            tacticalSlots = tactical ?? Array.Empty<RogueLoadoutSlotView>();
            tacticalStatus = tacticalStatusLabel;
            backpackStatus = backpackStatusLabel;
        }

        public bool IsValid()
        {
            if (characterContentRoot == null || backpackPanel == null || backpackGrid == null ||
                backpackItemsRoot == null || tacticalStatus == null || backpackStatus == null ||
                backpackCells == null || backpackCells.Length != 60 ||
                equipmentSlots == null || equipmentSlots.Length != 9 ||
                tacticalSlots == null || tacticalSlots.Length != 4)
                return false;

            for (int i = 0; i < backpackCells.Length; i++)
                if (backpackCells[i] == null) return false;
            for (int i = 0; i < equipmentSlots.Length; i++)
                if (equipmentSlots[i] == null || !equipmentSlots[i].IsValid(true, false)) return false;
            for (int i = 0; i < tacticalSlots.Length; i++)
                if (tacticalSlots[i] == null || !tacticalSlots[i].IsValid(false, true)) return false;
            return true;
        }
    }
}
