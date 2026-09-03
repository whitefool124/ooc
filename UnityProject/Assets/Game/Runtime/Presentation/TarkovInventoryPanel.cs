using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using OCC.Combat.Roguelite;

namespace OCC.Combat.Presentation
{
    public sealed class TarkovInventoryPanel : MonoBehaviour
    {
        private IInventoryPresentationHost bootstrap;
        private bool open;
        private string selectedId;
        private InventoryDragState dragState;
        private string rogueDragId;
        private bool rogueDragRotated;
        private int rogueGrabX;
        private int rogueGrabY;
        private string inventoryHoverText;
        private Vector2 inventoryHoverPointer;
        private string semanticHoverText;
        private Vector2 semanticHoverPointer;
        private Rect inventoryPanelRect;
        private string inventoryInteractionMessage = "左键拖拽物品 · 拖拽中右键旋转";
        private string searchText = string.Empty;
        private ItemCategory? category;
        private Vector2 resultScroll;
        private readonly Dictionary<string, Texture2D> icons = new Dictionary<string, Texture2D>(StringComparer.Ordinal);
        private readonly Dictionary<int, Texture2D> swatches = new Dictionary<int, Texture2D>();
        private GUISkin formalSkin;
        private Texture2D inventoryBackdrop;
        private readonly Texture2D[] clickFeedbackFrames = new Texture2D[6];
        private Vector2 clickFeedbackPointer;
        private float clickFeedbackStarted = -1f;

        private static Color Ink => FormalUiTheme.Ink;
        private static Color Panel => FormalUiTheme.Panel;
        private static Color Surface => FormalUiTheme.Surface;
        private static Color Cyan => FormalUiTheme.Cyan;
        private static Color Text => FormalUiTheme.Text;
        private static Color Muted => FormalUiTheme.Muted;

        public void Initialize(IInventoryPresentationHost source)
        {
            bootstrap = source;
            inventoryBackdrop = Resources.Load<Texture2D>(FormalUiEffectsConfig.BackdropPath("inventory"));
            if (inventoryBackdrop == null) throw new KeyNotFoundException("Missing formal inventory backdrop");
        }
        public bool IsOpen => open;
        public static Rect LauncherRect => new Rect(1472f, 16f, 160f, 48f);

        private void OnGUI()
        {
            if (bootstrap == null || !Application.isPlaying || !bootstrap.IsDeveloperCombatActive) return;
            HandleHotkey();
            float scale = Mathf.Min(Screen.width / 1920f, Screen.height / 1080f); Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector2((Screen.width - 1920f * scale) * .5f, (Screen.height - 1080f * scale) * .5f), Quaternion.identity, Vector3.one * scale);
            GUISkin previousSkin = GUI.skin;
            GUI.depth = -1100; ConfigureFormalSkin(previousSkin);
            if (!open)
            {
                if (ClickButton(LauncherRect, "背包 [B]")) open = true;
                DrawClickFeedback();
                GUI.skin = previousSkin; GUI.matrix = previous; return;
            }
            DrawPanel(); DrawClickFeedback(); GUI.skin = previousSkin; GUI.matrix = previous;
        }

        private void HandleHotkey()
        {
            Event current = Event.current; if (current == null || current.type != EventType.KeyDown) return;
            if (GUI.GetNameOfFocusedControl() == "inventory.search" && current.keyCode != KeyCode.Escape) return;
            if (current.keyCode == KeyCode.B || open && current.keyCode == KeyCode.Escape)
            {
                open = !open; if (!open) dragState = null;
                else if (string.IsNullOrEmpty(selectedId)) selectedId = InventoryInteractionPresentation.NextSelection(bootstrap.CurrentState?.ItemInventory, null, 0, 0);
                current.Use(); return;
            }
            if (!open || dragState != null) return;
            bool rogue = bootstrap.CurrentState?.Ruleset == CombatRuleset.Roguelite && bootstrap.CurrentState.RogueEquipment != null;
            int dx = current.keyCode == KeyCode.LeftArrow ? -1 : current.keyCode == KeyCode.RightArrow ? 1 : 0;
            int dy = current.keyCode == KeyCode.UpArrow ? -1 : current.keyCode == KeyCode.DownArrow ? 1 : 0;
            if (dx != 0 || dy != 0)
            {
                selectedId = rogue ? NextRogueSelection(bootstrap.CurrentState.RogueEquipment, selectedId, dx, dy) : InventoryInteractionPresentation.NextSelection(bootstrap.CurrentState?.ItemInventory, selectedId, dx, dy);
                current.Use(); return;
            }
            if (current.keyCode == KeyCode.R && !string.IsNullOrEmpty(selectedId))
            {
                if (rogue && !string.IsNullOrEmpty(rogueDragId))
                { rogueDragRotated = !rogueDragRotated; inventoryInteractionMessage = rogueDragRotated ? "已经横过来了" : "已经竖回来了"; current.Use(); return; }
                if (rogue)
                {
                    bool rotated = bootstrap.RotateRogueBackpackItem(selectedId);
                    inventoryInteractionMessage = rotated ? "已旋转" : "空间不足 · 保持原朝向";
                    current.Use(); return;
                }
                InventoryResult result = bootstrap.CurrentState.ItemInventory.Rotate(selectedId);
                inventoryInteractionMessage = result.Success ? "已旋转物品" : InventoryInteractionPresentation.ErrorName(result.Error) + " · 已保持原朝向";
                if (result.Success) bootstrap.NotifyInventoryChanged(); current.Use(); return;
            }
            int slot = NumberSlot(current.keyCode);
            if (slot >= 0)
            {
                if (rogue) { if (slot < RogueRuntimeConstants.ItemQuickbarSize && !string.IsNullOrEmpty(selectedId)) bootstrap.AssignRogueQuickbar(selectedId, slot); current.Use(); return; }
                if (!string.IsNullOrEmpty(selectedId)) { bootstrap.EquipInventoryQuickbar(selectedId, slot); inventoryInteractionMessage = "已关联快捷栏 " + (slot + 1); }
                else { bootstrap.ActivateInventoryQuickbar(slot); open = false; }
                current.Use(); return;
            }
            if (current.keyCode == KeyCode.F) { HandleKeyboardLoot(); current.Use(); }
        }

        private void DrawPanel()
        {
            CombatState state = bootstrap.CurrentState; if (state == null) return;
            inventoryHoverText = null;
            semanticHoverText = null;
            GUI.DrawTexture(new Rect(0, 0, 1920, 1080), inventoryBackdrop, ScaleMode.StretchToFill, false);
            Fill(new Rect(0, 0, 1920, 1080), FormalUiTheme.WithAlpha(Ink, .82f));
            Rect panel = new Rect(32, 24, 1856, 1024); Fill(panel, FormalUiTheme.WithAlpha(Ink, .985f)); Outline(panel, FormalUiTheme.WithAlpha(FormalUiTheme.OnInk, .88f));
            Fill(new Rect(34, 26, 1852, 112), FormalUiTheme.WithAlpha(Panel, .98f));
            Fill(new Rect(36, 136, 1848, 2), FormalUiTheme.WithAlpha(FormalUiTheme.Rule, .72f));
            bool rogue = state.Ruleset == CombatRuleset.Roguelite && state.RogueEquipment != null;
            GUI.Label(new Rect(100, 72, 880, 42), rogue ? "学院整备" : "背包与搜索");
            Fill(new Rect(92, 106, 1460, 34), FormalUiTheme.WithAlpha(Panel, .98f));
            GUI.color = Muted; GUI.Label(new Rect(100, 112, 1440, 30), rogue ? "B/Esc 关闭   ←↑↓→ 选择   R 旋转   1–4 关联   拖拽整理" : "B/Esc 关闭 · 方向键选择 · R 旋转 · 1–8 关联快捷栏 · F 搜索/拿取 · 鼠标拖拽"); GUI.color = Color.white;
            if (ClickButton(new Rect(1630, 72, 180, 52), "返回战斗 [B]")) { open = false; dragState = null; }

            if (rogue) { DrawRogueInventory(state); DrawSemanticTooltip(); return; }

            DrawInventory(state, new Rect(100, 160, 600, 720));
            DrawDetailsAndSearch(state, new Rect(730, 160, 500, 720));
            DrawLoot(state, new Rect(1260, 160, 550, 720));
            DrawQuickbar(state, new Rect(100, 900, 1710, 96));
            DrawInventoryOverlay(state);
            DrawSemanticTooltip();
        }

        private void DrawRogueInventory(CombatState state)
        {
            RogueEquipmentRuntime runtime = state.RogueEquipment;
            IReadOnlyList<RogueInventoryItemPresentation> items = RogueInventoryPresentation.Build(runtime);
            if (string.IsNullOrEmpty(selectedId) || runtime.EquipmentItem(selectedId) == null && runtime.TacticalItem(selectedId) == null)
                selectedId = items.FirstOrDefault()?.InstanceId ?? runtime.Equipped.Values.FirstOrDefault(value => !string.IsNullOrEmpty(value));
            DrawRogueEquipmentSlots(runtime, new Rect(100, 160, 500, 610));
            DrawRogueBackpack(runtime, items, new Rect(630, 160, 430, 720));
            DrawRogueDetails(runtime, new Rect(1090, 160, 720, 500));
            DrawRogueQuickbar(runtime, new Rect(1090, 690, 720, 190));
        }

        private void DrawRogueEquipmentSlots(RogueEquipmentRuntime runtime, Rect rect)
        {
            Box(rect, "装备 11  ·  战斗中锁定");
            OCC.Combat.Roguelite.EquipmentSlot[] slots = Enum.GetValues(typeof(OCC.Combat.Roguelite.EquipmentSlot)).Cast<OCC.Combat.Roguelite.EquipmentSlot>().ToArray();
            for (int index = 0; index < slots.Length; index++)
            {
                OCC.Combat.Roguelite.EquipmentSlot slot = slots[index]; string id = runtime.Equipped[slot]; EquipmentDefinition definition = runtime.DefinitionFor(id);
                Rect slotRect = new Rect(rect.x + 18 + (index % 2) * 232, rect.y + 58 + (index / 2) * 78, 216, 64);
                DrawIcon(slotRect, "Art/FormalUI32/" + (id == selectedId ? "slot_selected" : "slot"), false);
                DrawIcon(new Rect(slotRect.x + 8, slotRect.y + 14, 32, 32), definition == null ? EquipmentIconPath(slot) : FormalArtRegistry.EquipmentIconPath(definition.DefinitionId));
                GUI.Label(new Rect(slotRect.x + 48, slotRect.y + 8, 156, 22), EquipmentSlotName(slot));
                GUI.color = definition == null ? Muted : Text; GUI.Label(new Rect(slotRect.x + 48, slotRect.y + 30, 156, 26), definition?.DisplayName ?? "空"); GUI.color = Color.white;
                if (ClickButton(slotRect, GUIContent.none, GUIStyle.none) && !string.IsNullOrEmpty(id)) { selectedId = id; inventoryInteractionMessage = "战斗中装备锁定"; }
            }
        }

        private void DrawRogueBackpack(RogueEquipmentRuntime runtime, IReadOnlyList<RogueInventoryItemPresentation> items, Rect rect)
        {
            Box(rect, "背包 6×10  ·  " + items.Count + " 件");
            const float cell = 52f; float gx = rect.x + 26, gy = rect.y + 60;
            for (int y = 0; y < 10; y++) for (int x = 0; x < 6; x++)
                DrawIcon(new Rect(gx + x * cell, gy + y * cell, cell - 3, cell - 3), "Art/FormalUI32/slot", false);
            Event current = Event.current; RogueInventoryItemPresentation hovered = null; Rect hoveredRect = default;
            foreach (RogueInventoryItemPresentation item in items)
            {
                Rect itemRect = new Rect(gx + item.X * cell, gy + item.Y * cell, item.Width * cell - 3, item.Height * cell - 3);
                bool dragging = rogueDragId == item.InstanceId;
                if (RogueInventoryPresentation.ShouldDrawSourceItem(rogueDragId, item.InstanceId))
                {
                    DrawIcon(itemRect, "Art/FormalUI32/" + (item.InstanceId == selectedId ? "slot_selected" : "slot"), false);
                    DrawInventoryArt(new Rect(itemRect.x + 5, itemRect.y + 5, itemRect.width - 10, itemRect.height - 10), RogueItemIconPath(item), item.Rotated);
                    if (!item.IsEquipment) GUI.Label(new Rect(itemRect.x + 6, itemRect.yMax - 24, itemRect.width - 12, 22), "×" + item.ChargesCurrent);
                }
                if (!dragging && current != null && itemRect.Contains(current.mousePosition))
                {
                    hovered = item; hoveredRect = itemRect;
                    semanticHoverText = item.DisplayName + "  ·  " + (item.IsEquipment ? EquipmentSlotName(item.Slot.Value) : item.ChargesCurrent + "/" + item.ChargesMaximum + " 次");
                    semanticHoverPointer = current.mousePosition;
                }
            }
            HandleRogueBackpackPointer(runtime, hovered, hoveredRect, gx, gy, cell);
            DrawRogueDragPreview(runtime, gx, gy, cell);
            GUI.color = Muted; GUI.Label(new Rect(rect.x + 24, rect.yMax - 48, rect.width - 48, 28), inventoryInteractionMessage); GUI.color = Color.white;
        }

        private void HandleRogueBackpackPointer(RogueEquipmentRuntime runtime, RogueInventoryItemPresentation hovered, Rect hoveredRect, float gx, float gy, float cell)
        {
            Event current = Event.current; if (current == null) return;
            if (current.type == EventType.MouseDown && current.button == 1 && !string.IsNullOrEmpty(rogueDragId))
                { rogueDragRotated = !rogueDragRotated; inventoryInteractionMessage = rogueDragRotated ? "已经横过来了" : "已经竖回来了"; current.Use(); return; }
            if (current.type == EventType.MouseDown && current.button == 0 && hovered != null && string.IsNullOrEmpty(rogueDragId))
            {
                selectedId = hovered.InstanceId; rogueDragId = hovered.InstanceId; rogueDragRotated = hovered.Rotated;
                rogueGrabX = Mathf.FloorToInt((current.mousePosition.x - hoveredRect.x) / cell); rogueGrabY = Mathf.FloorToInt((current.mousePosition.y - hoveredRect.y) / cell);
                inventoryInteractionMessage = "拖拽中  ·  右键旋转"; current.Use(); return;
            }
            if (string.IsNullOrEmpty(rogueDragId) || current.type != EventType.MouseUp || current.button != 0) return;
            int x = Mathf.FloorToInt((current.mousePosition.x - gx) / cell) - rogueGrabX;
            int y = Mathf.FloorToInt((current.mousePosition.y - gy) / cell) - rogueGrabY;
            bool moved = bootstrap.MoveRogueBackpackItem(rogueDragId, x, y, rogueDragRotated);
            inventoryInteractionMessage = moved ? "已放置" : "不可放置  ·  保持原位"; rogueDragId = null; current.Use();
        }

        private void DrawRogueDragPreview(RogueEquipmentRuntime runtime, float gx, float gy, float cell)
        {
            if (string.IsNullOrEmpty(rogueDragId) || Event.current == null) return;
            EquipmentDefinition equipment = runtime.DefinitionFor(rogueDragId); TacticalItemDefinition tactical = runtime.TacticalDefinitionFor(rogueDragId);
            int baseWidth = equipment?.Width ?? tactical.Width, baseHeight = equipment?.Height ?? tactical.Height;
            int width = rogueDragRotated ? baseHeight : baseWidth, height = rogueDragRotated ? baseWidth : baseHeight;
            int x = Mathf.FloorToInt((Event.current.mousePosition.x - gx) / cell) - rogueGrabX;
            int y = Mathf.FloorToInt((Event.current.mousePosition.y - gy) / cell) - rogueGrabY;
            bool legal = runtime.CanMoveBackpack(rogueDragId, x, y, rogueDragRotated);
            Rect ghost = new Rect(gx + x * cell, gy + y * cell, width * cell - 3, height * cell - 3);
            Fill(ghost, FormalUiTheme.WithAlpha(legal ? Cyan : FormalUiTheme.Danger, .34f)); Outline(ghost, legal ? Cyan : FormalUiTheme.Danger);
        }

        private void DrawRogueDetails(RogueEquipmentRuntime runtime, Rect rect)
        {
            Box(rect, "物品详情"); RogueEquipmentInstance equipment = runtime.EquipmentItem(selectedId); RogueTacticalItemInstance tactical = runtime.TacticalItem(selectedId);
            if (equipment == null && tactical == null) { GUI.Label(new Rect(rect.x + 24, rect.y + 70, 640, 40), "选择一个物品"); return; }
            string name, icon, type, metrics, effects;
            if (equipment != null)
            {
                EquipmentDefinition definition = runtime.DefinitionFor(selectedId); name = definition.DisplayName; icon = FormalArtRegistry.EquipmentIconPath(definition.DefinitionId);
                type = EquipmentSlotName(definition.Slot) + "  ·  " + equipment.Rarity; metrics = definition.Width + "×" + definition.Height + "   ⚖ " + definition.BaseWeight + "   ◆ " + definition.BaseAetherLoad;
                effects = string.Join("\n", definition.FixedEffectIds.Concat(equipment.MutableAffixIds).Concat(equipment.UpgradeBranchIds).Take(7));
            }
            else
            {
                TacticalItemDefinition definition = runtime.TacticalDefinitionFor(selectedId); name = definition.DisplayName; icon = FormalArtRegistry.ItemPath(tactical.DefinitionId);
                type = "战术道具"; metrics = definition.Width + "×" + definition.Height + "   行动点 " + definition.ActionPointCost + "   剩余 " + tactical.ChargesCurrent + "/" + tactical.ChargesMaximum;
                effects = "可关联至下方 4 格战术栏";
            }
            DrawIcon(new Rect(rect.x + 24, rect.y + 62, 72, 72), icon); GUI.Label(new Rect(rect.x + 116, rect.y + 62, 560, 34), name);
            GUI.color = equipment != null ? Cyan : FormalUiTheme.Safe; GUI.Label(new Rect(rect.x + 116, rect.y + 98, 560, 28), type); GUI.color = Color.white;
            GUI.Label(new Rect(rect.x + 24, rect.y + 154, 650, 34), metrics);
            GUI.color = FormalUiTheme.Amber; GUI.Label(new Rect(rect.x + 24, rect.y + 212, 120, 28), "效果"); GUI.color = Color.white;
            GUI.Label(new Rect(rect.x + 24, rect.y + 246, 650, 126), string.IsNullOrEmpty(effects) ? "—" : effects);
            GUI.color = Muted; GUI.Label(new Rect(rect.x + 24, rect.yMax - 56, 650, 30), equipment != null ? "战斗中装备锁定" : "点击战术栏或按 1–4 关联"); GUI.color = Color.white;
        }

        private void DrawRogueQuickbar(RogueEquipmentRuntime runtime, Rect rect)
        {
            Box(rect, "战术栏 4"); string[] quickbar = runtime.ItemQuickbarInstanceIds;
            for (int i = 0; i < RogueRuntimeConstants.ItemQuickbarSize; i++)
            {
                int slot = i; string id = quickbar[i]; RogueTacticalItemInstance item = runtime.TacticalItem(id); Rect slotRect = new Rect(rect.x + 20 + i * 170, rect.y + 62, 156, 84);
                DrawIcon(slotRect, "Art/FormalUI32/slot", false); if (item != null) DrawIcon(new Rect(slotRect.x + 8, slotRect.y + 12, 40, 40), FormalArtRegistry.ItemPath(item.DefinitionId));
                GUI.Label(new Rect(slotRect.x + 56, slotRect.y + 10, 92, 26), (i + 1) + "  " + (item == null ? "空" : runtime.TacticalDefinitionFor(id).DisplayName));
                if (item != null) GUI.Label(new Rect(slotRect.x + 56, slotRect.y + 42, 92, 24), item.ChargesCurrent + "/" + item.ChargesMaximum);
                if (ClickButton(slotRect, GUIContent.none, GUIStyle.none))
                { if (runtime.TacticalItem(selectedId) != null) bootstrap.AssignRogueQuickbar(selectedId, slot); else if (item != null) selectedId = item.InstanceId; }
            }
        }

        private static string NextRogueSelection(RogueEquipmentRuntime runtime, string currentId, int dx, int dy)
        {
            RogueInventoryItemPresentation[] items = RogueInventoryPresentation.Build(runtime).ToArray(); if (items.Length == 0) return null;
            RogueInventoryItemPresentation current = items.FirstOrDefault(value => value.InstanceId == currentId) ?? items[0];
            return items.Where(value => value.InstanceId != current.InstanceId).OrderBy(value =>
            {
                int deltaX = value.X - current.X, deltaY = value.Y - current.Y;
                bool direction = dx < 0 ? deltaX < 0 : dx > 0 ? deltaX > 0 : dy < 0 ? deltaY > 0 : deltaY < 0;
                return direction ? Math.Abs(deltaX) + Math.Abs(deltaY) : 1000 + Math.Abs(deltaX) + Math.Abs(deltaY);
            }).FirstOrDefault()?.InstanceId ?? current.InstanceId;
        }

        private static string RogueItemIconPath(RogueInventoryItemPresentation item) => item.IsEquipment ? FormalArtRegistry.EquipmentFootprintPath(item.DefinitionId) : FormalArtRegistry.ItemPath(item.DefinitionId);
        private static string EquipmentIconPath(OCC.Combat.Roguelite.EquipmentSlot slot) => FormalArtRegistry.EquipmentSlotPath(slot.ToString());
        private static string EquipmentSlotName(OCC.Combat.Roguelite.EquipmentSlot slot) => slot == OCC.Combat.Roguelite.EquipmentSlot.MainHand ? "主手" : slot == OCC.Combat.Roguelite.EquipmentSlot.OffHand ? "副手" : slot == OCC.Combat.Roguelite.EquipmentSlot.Head ? "头部" : slot == OCC.Combat.Roguelite.EquipmentSlot.Chest ? "胸甲" : slot == OCC.Combat.Roguelite.EquipmentSlot.Hands ? "手部" : slot == OCC.Combat.Roguelite.EquipmentSlot.Legs ? "腿部" : slot == OCC.Combat.Roguelite.EquipmentSlot.Backpack ? "背架" : slot == OCC.Combat.Roguelite.EquipmentSlot.AetherCore ? "以太核心" : slot == OCC.Combat.Roguelite.EquipmentSlot.Conduit ? "导器" : slot == OCC.Combat.Roguelite.EquipmentSlot.Accessory1 ? "饰品一" : "饰品二";

        private void DrawInventory(CombatState state, Rect rect)
        {
            inventoryPanelRect = rect;
            DrawIcon(new Rect(rect.x + rect.width - 78, rect.y + 7, 28, 28), "Art/FormalItemIcons32/category_container");
            DrawIcon(new Rect(rect.x + rect.width - 44, rect.y + 7, 28, 28), "Art/FormalItemIcons32/inventory_weight");
            Box(rect, $"基础背包 · 6×10 · {state.ItemInventory.Items.Count} 件 · 负重 {state.ItemInventory.CurrentWeight}");
            const float cell = 52f; float gx = rect.x + 26; float gy = rect.y + 68;
            for (int y = 0; y < 10; y++) for (int x = 0; x < 6; x++)
            {
                ItemInstance occupied = state.ItemInventory.GetAt(x, y);
                Rect slotRect = new Rect(gx + x * cell, gy + y * cell, cell - 3, cell - 3);
                string slotSkin = occupied != null && occupied.InstanceId == selectedId ? "slot_selected" : "slot";
                DrawIcon(slotRect, "Art/FormalUI32/" + slotSkin, false);
            }
            foreach (InventoryPlacement placement in state.ItemInventory.Placements)
            {
                ItemInstance item = state.ItemInventory.Get(placement.InstanceId); ItemDefinition definition = ItemCatalog.Get(item.DefinitionId);
                Rect itemRect = InventoryInteractionPresentation.PlacementRect(rect, placement, definition);
                Color previous = GUI.color; if (dragState != null && dragState.InstanceId == item.InstanceId) GUI.color = FormalUiTheme.WithAlpha(Color.white, .28f);
                DrawIcon(itemRect, "Art/FormalUI32/" + (item.InstanceId == selectedId ? "slot_selected" : "slot"), false);
                DrawInventoryArt(new Rect(itemRect.x + 4, itemRect.y + 4, itemRect.width - 8, itemRect.height - 8), definition.InventoryArtPath, placement.Rotated);
                GUI.Label(new Rect(itemRect.x + 44, itemRect.y + 4, itemRect.width - 48, 24), definition.DisplayName);
                if (definition.MaximumUses > 0) GUI.Label(new Rect(itemRect.x + 4, itemRect.yMax - 24, itemRect.width - 8, 22), item.RemainingUses + "/" + definition.MaximumUses + " 次");
                GUI.color = previous;
            }
            HandleInventoryPointer(state, rect);
            GUI.enabled = !string.IsNullOrEmpty(selectedId);
            if (IconButton(new Rect(gx + 330, gy, 220, 46), "Art/FormalItemIcons32/inventory_rotate", "旋转选中物品 [R]")) { state.ItemInventory.Rotate(selectedId); bootstrap.NotifyInventoryChanged(); }
            GUI.enabled = true;
            GUI.color = Muted; GUI.Label(new Rect(gx, gy + 536, 548, 32), inventoryInteractionMessage); GUI.color = Color.white;
        }

        private void HandleInventoryPointer(CombatState state, Rect rect)
        {
            Event current = Event.current;
            if (current == null) return;
            Vector2 pointer = current.mousePosition;
            Vector2Int pointerCell = InventoryInteractionPresentation.GridCellAt(rect, pointer);
            bool insideGrid = pointerCell != InventoryInteractionPresentation.OutsideGrid;
            ItemInstance hovered = insideGrid ? state.ItemInventory.GetAt(pointerCell.x, pointerCell.y) : null;

            if (dragState == null && hovered != null)
            {
                InventoryPlacement placement = state.ItemInventory.PlacementOf(hovered.InstanceId).Value;
                inventoryHoverText = InventoryInteractionPresentation.BuildHoverText(hovered, placement);
                inventoryHoverPointer = pointer;
            }

            if (current.type == EventType.MouseDown && current.button == 1 && dragState != null)
            {
                ItemInstance dragged = state.ItemInventory.Get(dragState.InstanceId);
                if (dragged != null)
                {
                    dragState.ToggleRotation(ItemCatalog.Get(dragged.DefinitionId));
                    inventoryInteractionMessage = dragState.Rotated ? "横放 · 松开左键放下" : "竖放 · 松开左键放下";
                }
                current.Use();
                return;
            }

            if (current.type == EventType.MouseDown && current.button == 0 && dragState == null && insideGrid)
            {
                if (hovered == null)
                {
                    selectedId = null;
                    inventoryInteractionMessage = "空格 · 左键拖拽物品到这里";
                }
                else
                {
                    InventoryPlacement placement = state.ItemInventory.PlacementOf(hovered.InstanceId).Value;
                    selectedId = hovered.InstanceId;
                    dragState = new InventoryDragState(hovered.InstanceId, placement.Rotated, pointerCell.x - placement.X, pointerCell.y - placement.Y);
                    inventoryHoverText = null;
                    inventoryInteractionMessage = "正在拖拽 · 右键旋转 · 松开左键放置";
                }
                current.Use();
                return;
            }

            if (dragState == null) return;
            if (current.type == EventType.MouseDrag && current.button == 0)
            {
                current.Use();
                return;
            }
            if (current.type != EventType.MouseUp || current.button != 0) return;

            InventoryResult result = dragState.Commit(state.ItemInventory, pointerCell);
            inventoryInteractionMessage = result.Success
                ? "已移动物品 · 左键继续拖拽 · 拖拽中右键旋转"
                : InventoryInteractionPresentation.ErrorName(result.Error) + " · 已保持原位置";
            if (result.Success) bootstrap.NotifyInventoryChanged();
            dragState = null;
            current.Use();
        }

        private void DrawInventoryOverlay(CombatState state)
        {
            if (dragState == null)
            {
                if (string.IsNullOrEmpty(inventoryHoverText)) return;
                Rect tooltip = InventoryInteractionPresentation.TooltipRect(inventoryHoverPointer);
                Fill(tooltip, FormalUiTheme.WithAlpha(FormalUiTheme.SurfaceRaised, .99f));
                Outline(tooltip, FormalUiTheme.WithAlpha(FormalUiTheme.Rule, .92f));
                GUI.Label(new Rect(tooltip.x + 18f, tooltip.y + 14f, tooltip.width - 36f, tooltip.height - 28f), inventoryHoverText);
                return;
            }

            ItemInstance item = state.ItemInventory.Get(dragState.InstanceId);
            if (item == null) { dragState = null; return; }
            ItemDefinition definition = ItemCatalog.Get(item.DefinitionId);
            Vector2 pointer = Event.current.mousePosition;
            Vector2Int pointerCell = InventoryInteractionPresentation.GridCellAt(inventoryPanelRect, pointer);
            InventoryResult preview = dragState.Preview(state.ItemInventory, pointerCell);
            Vector2Int anchor = dragState.AnchorFor(pointerCell);
            int width = dragState.Rotated ? definition.Height : definition.Width;
            int height = dragState.Rotated ? definition.Width : definition.Height;
            Color previewColor = preview.Success ? FormalUiTheme.WithAlpha(Cyan, .42f) : FormalUiTheme.WithAlpha(FormalUiTheme.Danger, .46f);

            if (pointerCell != InventoryInteractionPresentation.OutsideGrid)
            {
                Rect grid = InventoryInteractionPresentation.GridRect(inventoryPanelRect);
                for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
                {
                    int cellX = anchor.x + x; int cellY = anchor.y + y;
                    if (cellX < 0 || cellY < 0 || cellX >= state.ItemInventory.Width || cellY >= state.ItemInventory.Height) continue;
                    Fill(new Rect(grid.x + cellX * InventoryInteractionPresentation.CellSize, grid.y + cellY * InventoryInteractionPresentation.CellSize,
                        InventoryInteractionPresentation.CellSize - 3f, InventoryInteractionPresentation.CellSize - 3f), previewColor);
                }
            }

            Rect ghost;
            if (pointerCell == InventoryInteractionPresentation.OutsideGrid)
                ghost = new Rect(pointer.x + 18f, pointer.y + 18f, width * InventoryInteractionPresentation.CellSize - 9f, height * InventoryInteractionPresentation.CellSize - 9f);
            else
                ghost = InventoryInteractionPresentation.PlacementRect(inventoryPanelRect, new InventoryPlacement(item.InstanceId, anchor.x, anchor.y, dragState.Rotated), definition);
            Fill(ghost, FormalUiTheme.WithAlpha(Panel, .92f));
            Outline(ghost, preview.Success ? Cyan : FormalUiTheme.Danger);
            DrawInventoryArt(new Rect(ghost.x + 6f, ghost.y + 6f, ghost.width - 12f, ghost.height - 12f), definition.InventoryArtPath, dragState.Rotated);
            GUI.Label(new Rect(ghost.x + 52f, ghost.y + 6f, Math.Max(60f, ghost.width - 58f), 26f), definition.DisplayName);
            string status = preview.Success ? "可放置" : InventoryInteractionPresentation.ErrorName(preview.Error);
            GUI.Label(new Rect(ghost.x + 6f, ghost.yMax - 26f, Math.Max(80f, ghost.width - 12f), 22f), status + " · 右键旋转");
        }

        private void DrawDetailsAndSearch(CombatState state, Rect rect)
        {
            DrawIcon(new Rect(rect.x + rect.width - 44, rect.y + 7, 28, 28), "Art/FormalItemIcons32/inventory_search");
            Box(rect, "物品详情 / 管理筛选"); float x = rect.x + 22; float y = rect.y + 56;
            ItemInstance selected = state.ItemInventory.Get(selectedId); ItemDefinition definition = selected == null ? null : ItemCatalog.Get(selected.DefinitionId);
            bool artifactDetails = false;
            if (definition == null) GUI.Label(new Rect(x, y, 450, 80), "点击背包格或查询结果选择物品。\n空格点击可移动选中物品。");
            else
            {
                ArtifactDefinition artifact = ArtifactCatalog.All.FirstOrDefault(candidate => candidate.Id == definition.Id);
                Texture2D icon = Icon(definition.IconPath); if (icon != null) GUI.DrawTexture(new Rect(x, y, 64, 64), icon, ScaleMode.ScaleToFit, true);
                GUI.Label(new Rect(x + 80, y, 350, 32), definition.DisplayName);
                GUI.Label(new Rect(x + 80, y + 34, 350, 54), (artifact == null ? CategoryName(definition.Category) : "法宝 · " + RarityName(definition.Rarity)) + " · " + definition.Width + "×" + definition.Height + " · 重量 " + definition.Weight + "\n" + (definition.MaximumUses > 0 ? selected.RemainingUses + "/" + definition.MaximumUses + " 次 · " : string.Empty) + definition.Provenance);
                artifactDetails = artifact != null;
                if (artifact == null) GUI.Label(new Rect(x, y + 96, 450, 60), definition.Description);
                else
                {
                    GUI.Label(new Rect(x, y + 92, 450, 26), "来源：" + artifact.Provenance);
                    DrawSemanticIcon(new Rect(x, y + 120, 24, 24), "action", "行动");
                    GUI.Label(new Rect(x + 30, y + 120, 52, 24), artifact.ActionPointCost.ToString());
                    string perUseCost = artifact.PublicCost
                        .Replace(artifact.ActionPointCost + " 行动点，", string.Empty)
                        .Replace("消耗 ", string.Empty);
                    GUI.Label(new Rect(x + 82, y + 120, 300, 24), "每次 " + perUseCost + " · 剩余 " + selected.RemainingUses + "/" + artifact.MaximumUses);
                    GUI.Label(new Rect(x, y + 148, 450, 26), "目标：" + artifact.TargetSummary);
                    GUI.Label(new Rect(x, y + 176, 450, 36), artifact.EffectSummary);
                    DrawSemanticIcon(new Rect(x, y + 214, 24, 24), "notice", "注意");
                    GUI.color = FormalUiTheme.Amber; GUI.Label(new Rect(x + 30, y + 212, 420, 36), artifact.RiskSummary); GUI.color = Color.white;
                    GUI.Label(new Rect(x, y + 248, 450, 24), "适合：" + artifact.BuildUse);
                }
            }
            y += artifactDetails ? 270 : 180; DrawIcon(new Rect(x, y - 2, 30, 30), "Art/FormalItemIcons32/inventory_search"); GUI.Label(new Rect(x + 38, y, 82, 28), "名称"); GUI.SetNextControlName("inventory.search"); searchText = GUI.TextField(new Rect(x + 120, y, 320, 34), searchText ?? string.Empty);
            y += 48; if (IconButton(new Rect(x, y, 210, 40), CategoryIcon(category), "类别：" + (category?.ToString() ?? "全部"))) category = NextCategory(category);
            if (IconButton(new Rect(x + 230, y, 210, 40), "Art/FormalItemIcons32/inventory_clear", "清空条件")) { searchText = string.Empty; category = null; }
            ItemQuery query = new ItemQuery { Text = searchText, Category = category, Sort = ItemSort.Acquired };
            IReadOnlyList<ItemInstance> results = ItemSearchService.Search(state.ItemInventory, query, state.ItemQuickbar);
            y += 54; GUI.Label(new Rect(x, y, 440, 28), "查询结果 " + results.Count + "（不改变背包布局）");
            float resultHeight = artifactDetails ? 200 : 300;
            resultScroll = GUI.BeginScrollView(new Rect(x, y + 34, 440, resultHeight), resultScroll, new Rect(0, 0, 416, Math.Max(resultHeight, results.Count * 48)));
            for (int i = 0; i < results.Count; i++)
            {
                ItemDefinition d = ItemCatalog.Get(results[i].DefinitionId); Rect resultRect = new Rect(0, i * 48, 410, 42);
                DrawIcon(resultRect, "Art/FormalUI32/" + (results[i].InstanceId == selectedId ? "slot_selected" : "slot"), false);
                DrawIcon(new Rect(7, i * 48 + 5, 32, 32), d.IconPath);
                GUI.Label(new Rect(48, i * 48 + 7, 348, 28), d.DisplayName + " · " + d.Width + "×" + d.Height);
                if (ClickButton(resultRect, GUIContent.none, GUIStyle.none)) selectedId = results[i].InstanceId;
            }
            GUI.EndScrollView();
        }

        private void DrawLoot(CombatState state, Rect rect)
        {
            LootSourceState visualLoot = state.LootSource;
            string visualLootIcon = visualLoot == null || visualLoot.IsComplete ? "loot_empty" : visualLoot.State == LootSearchState.Unsearched ? "loot_unknown" : "loot_searching";
            DrawIcon(new Rect(rect.x + rect.width - 44, rect.y + 7, 28, 28), "Art/FormalItemIcons32/" + visualLootIcon);
            Box(rect, "战利品"); LootSourceState loot = state.LootSource; float x = rect.x + 22; float y = rect.y + 58;
            if (loot == null) { DrawIcon(new Rect(x, y, 64, 64), "Art/FormalItemIcons32/loot_empty"); GUI.Label(new Rect(x + 82, y + 18, 400, 40), "当前战场没有可搜索容器。"); return; }
            GUI.Label(new Rect(x, y, 500, 62), "状态：" + StateName(loot.State) + "\n未知物品：" + loot.HiddenCount + "  /  已揭示可取：" + loot.RevealedItems.Count);
            UnitState hero = state.GetUnit("hero"); bool adjacent = hero != null && Math.Abs(hero.Position.X - loot.Position.X) + Math.Abs(hero.Position.Y - loot.Position.Y) == 1;
            string searchReason = InventoryInteractionPresentation.LootSearchReason(adjacent, hero == null ? 0 : hero.ActionPoints, loot.IsComplete);
            y += 78; GUI.enabled = adjacent && !loot.IsComplete && hero.ActionPoints >= 1;
            if (IconButton(new Rect(x, y, 500, 54), "Art/FormalItemIcons32/inventory_search", "继续搜索")) bootstrap.SearchCurrentLoot();
            DrawSemanticIcon(new Rect(x + 424, y + 15, 24, 24), "action", "行动"); GUI.Label(new Rect(x + 454, y + 15, 28, 24), "1"); GUI.enabled = true;
            if (!adjacent) GUI.Label(new Rect(x, y + 60, 500, 28), "需要移动到容器相邻格。");
            GUI.Label(new Rect(x, y + 84, 500, 24), searchReason);
            y += 110; int row = 0;
            for (int hidden = 0; hidden < Math.Min(loot.HiddenCount, 5); hidden++)
            {
                Rect unknown = new Rect(x + hidden * 62, y, 54, 54);
                DrawIcon(unknown, "Art/FormalUISkin16/slot_locked", false);
                DrawIcon(new Rect(unknown.x + 11, unknown.y + 11, 32, 32), "Art/FormalItemIcons32/loot_unknown");
            }
            if (loot.HiddenCount > 0) y += 66;
            foreach (ItemInstance item in loot.RevealedItems)
            {
                ItemDefinition d = ItemCatalog.Get(item.DefinitionId); Rect lootRow = new Rect(x, y + row * 66, 500, 56); DrawIcon(lootRow, "Art/FormalUI32/slot", false); Texture2D icon = Icon(d.IconPath); if (icon != null) GUI.DrawTexture(new Rect(x + 5, y + row * 66 + 4, 48, 48), icon, ScaleMode.ScaleToFit, true);
                GUI.Label(new Rect(x + 60, y + row * 66, 260, 48), d.DisplayName + "\n" + d.Width + "×" + d.Height + (item.MaximumUses > 0 ? " · " + item.RemainingUses + "次" : string.Empty));
                UiOperationAvailability availability = InventoryInteractionPresentation.LootTakeAvailability(state.ItemInventory, item);
                GUI.enabled = availability.CanExecute;
                if (IconButton(new Rect(x + 340, y + row * 66 + 4, 150, 48), "Art/FormalItemIcons32/inventory_autoplace", availability.Status)) bootstrap.TakeCurrentLoot(item.InstanceId);
                GUI.enabled = true;
                if (!availability.CanExecute) GUI.Label(new Rect(x + 60, y + row * 66 + 34, 270, 20), availability.Reason);
                row++;
            }
        }

        private void DrawQuickbar(CombatState state, Rect rect)
        {
            DrawIcon(new Rect(rect.x + rect.width - 44, rect.y + 7, 28, 28), "Art/FormalItemIcons32/inventory_quickbar");
            Box(rect, "快捷栏 · 卷轴与法宝最多 4 件"); DrawSemanticIcon(new Rect(rect.x + 258, rect.y + 8, 22, 22), "action", "行动"); GUI.Label(new Rect(rect.x + 284, rect.y + 8, 90, 22), "换入 1"); float x = rect.x + 380;
            if (IconButton(new Rect(rect.x + 20, rect.y + 42, 280, 40), string.IsNullOrEmpty(selectedId) ? "Art/FormalItemIcons32/inventory_use" : "Art/FormalItemIcons32/inventory_clear", string.IsNullOrEmpty(selectedId) ? "未选物品" : "清除选中")) selectedId = null;
            for (int i = 0; i < 8; i++)
            {
                int slot = i; string instanceId = state.ItemQuickbar[i]; ItemInstance item = state.ItemInventory.Get(instanceId); ItemDefinition definition = item == null ? null : ItemCatalog.Get(item.DefinitionId); string label = item == null ? (i + 1) + " 空" : (i + 1) + " " + definition.DisplayName;
                Rect slotRect = new Rect(x + i * 166, rect.y + 42, 156, 40); DrawIcon(slotRect, "Art/FormalUI32/slot", false);
                if (item != null) DrawIcon(new Rect(slotRect.x + 5, slotRect.y + 4, 32, 32), definition.IconPath);
                GUI.Label(new Rect(slotRect.x + 42, slotRect.y + 7, 108, 28), label);
                if (ClickButton(slotRect, GUIContent.none, GUIStyle.none))
                {
                    if (!string.IsNullOrEmpty(selectedId)) bootstrap.EquipInventoryQuickbar(selectedId, slot); else { bootstrap.ActivateInventoryQuickbar(slot); open = false; }
                }
            }
        }

        private Texture2D Icon(string path)
        {
            if (string.IsNullOrEmpty(path)) return null; if (!icons.TryGetValue(path, out Texture2D icon)) { icon = Resources.Load<Texture2D>(path) ?? Resources.Load<Texture2D>("Art/FormalIcons32/loot"); icons[path] = icon; } return icon;
        }
        private void DrawIcon(Rect rect, string path, bool preserveAspect = true)
        {
            if (DrawReadingSlot(rect, path)) return;
            Texture2D texture = Icon(path);
            if (texture != null) GUI.DrawTexture(rect, texture, preserveAspect ? ScaleMode.ScaleToFit : ScaleMode.StretchToFill, true);
        }

        private static bool DrawReadingSlot(Rect rect, string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            bool selected = path.EndsWith("/slot_selected", StringComparison.Ordinal);
            bool locked = path.EndsWith("/slot_locked", StringComparison.Ordinal);
            bool disabled = path.EndsWith("/slot_disabled", StringComparison.Ordinal);
            bool normal = path.EndsWith("/slot", StringComparison.Ordinal);
            if (!selected && !locked && !disabled && !normal) return false;

            Color surface = selected ? FormalUiTheme.InventorySlotSelected :
                locked || disabled ? FormalUiTheme.InventorySlotLocked : FormalUiTheme.InventorySlotSurface;
            Color border = selected ? FormalUiTheme.Cyan :
                locked || disabled ? FormalUiTheme.Muted : FormalUiTheme.Rule;
            Fill(rect, surface);
            bool repeatedEmptyCell = normal && rect.width <= 60f && rect.height <= 60f;
            if (repeatedEmptyCell)
            {
                const float divider = 3f;
                Fill(new Rect(rect.x, rect.y, rect.width, divider), FormalUiTheme.WithAlpha(FormalUiTheme.SurfaceRaised, .82f));
                Fill(new Rect(rect.xMax - divider, rect.y, divider, rect.height), FormalUiTheme.WithAlpha(border, .62f));
                Fill(new Rect(rect.x, rect.yMax - divider, rect.width, divider), FormalUiTheme.WithAlpha(border, .62f));
                return true;
            }
            Outline(rect, FormalUiTheme.WithAlpha(border, selected ? .96f : .78f));
            if (selected)
                Fill(new Rect(rect.x + FormalUiTheme.FrameThickness, rect.y + FormalUiTheme.FrameThickness,
                    FormalUiTheme.FrameThickness, Mathf.Max(0f, rect.height - FormalUiTheme.FrameThickness * 2)), FormalUiTheme.Cyan);
            return true;
        }
        private void DrawInventoryArt(Rect rect, string path, bool rotated)
        {
            Texture2D texture = Icon(path); if (texture == null) return;
            if (!rotated) { GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit, true); return; }
            Matrix4x4 previous = GUI.matrix;
            GUIUtility.RotateAroundPivot(90f, rect.center);
            Rect rotatedRect = new Rect(rect.center.x - rect.height * .5f, rect.center.y - rect.width * .5f, rect.height, rect.width);
            GUI.DrawTexture(rotatedRect, texture, ScaleMode.ScaleToFit, true);
            GUI.matrix = previous;
        }
        private bool IconButton(Rect rect, string path, string label)
        {
            bool clicked = ClickButton(rect, "       " + label);
            DrawIcon(new Rect(rect.x + 8, rect.y + (rect.height - 28) * .5f, 28, 28), path);
            return clicked;
        }

        private bool ClickButton(Rect rect, string label)
        {
            bool clicked = GUI.Button(rect, label);
            if (clicked) RegisterClickFeedback();
            return clicked;
        }

        private bool ClickButton(Rect rect, GUIContent content, GUIStyle style)
        {
            bool clicked = GUI.Button(rect, content, style);
            if (clicked) RegisterClickFeedback();
            return clicked;
        }

        private void RegisterClickFeedback()
        {
            clickFeedbackPointer = Event.current == null ? Vector2.zero : Event.current.mousePosition;
            clickFeedbackStarted = Time.unscaledTime;
        }

        private void DrawClickFeedback()
        {
            if (clickFeedbackStarted < 0f) return;
            OccPeripheralFeedbackEntry feedback = FormalUiEffectsConfig.Feedback("click");
            int frame = Mathf.FloorToInt((Time.unscaledTime - clickFeedbackStarted) * feedback.framesPerSecond);
            if (frame < 0 || frame >= feedback.frameCount) { clickFeedbackStarted = -1f; return; }
            if (clickFeedbackFrames[frame] == null)
                clickFeedbackFrames[frame] = Resources.Load<Texture2D>(feedback.resourcePath + "/frame_" + frame.ToString("00"));
            Texture2D texture = clickFeedbackFrames[frame];
            if (texture == null) throw new KeyNotFoundException("Missing formal inventory click feedback frame: " + frame);
            Color previous = GUI.color; GUI.color = Color.white;
            GUI.DrawTexture(new Rect(clickFeedbackPointer.x - 24f, clickFeedbackPointer.y - 24f, 48f, 48f), texture, ScaleMode.ScaleToFit, true);
            GUI.color = previous;
        }
        private static string CategoryIcon(ItemCategory? value) => "Art/FormalItemIcons32/category_" + (value?.ToString().ToLowerInvariant() ?? "container");
        private static ItemCategory? NextCategory(ItemCategory? value) { if (!value.HasValue) return ItemCategory.Consumable; int next = (int)value.Value + 1; return next > (int)ItemCategory.Container ? (ItemCategory?)null : (ItemCategory)next; }
        private static string StateName(LootSearchState state) => state == LootSearchState.Unsearched ? "未搜索" : state == LootSearchState.Searching ? "搜索中" : state == LootSearchState.Searched ? "已搜索" : "已清空";
        private static int NumberSlot(KeyCode key)
        {
            int value = (int)key;
            if (value >= (int)KeyCode.Alpha1 && value <= (int)KeyCode.Alpha8) return value - (int)KeyCode.Alpha1;
            if (value >= (int)KeyCode.Keypad1 && value <= (int)KeyCode.Keypad8) return value - (int)KeyCode.Keypad1;
            return -1;
        }
        private void HandleKeyboardLoot()
        {
            CombatState state = bootstrap.CurrentState; LootSourceState loot = state?.LootSource; UnitState hero = state?.GetUnit("hero");
            if (loot == null) { inventoryInteractionMessage = "当前战场没有可搜索容器"; return; }
            ItemInstance revealed = loot.RevealedItems.FirstOrDefault();
            if (revealed != null)
            {
                UiOperationAvailability availability = InventoryInteractionPresentation.LootTakeAvailability(state.ItemInventory, revealed);
                if (availability.CanExecute) bootstrap.TakeCurrentLoot(revealed.InstanceId); else inventoryInteractionMessage = availability.Reason;
                return;
            }
            bool adjacent = hero != null && Math.Abs(hero.Position.X - loot.Position.X) + Math.Abs(hero.Position.Y - loot.Position.Y) == 1;
            string reason = InventoryInteractionPresentation.LootSearchReason(adjacent, hero == null ? 0 : hero.ActionPoints, loot.IsComplete);
            if (adjacent && !loot.IsComplete && hero.ActionPoints >= 1) bootstrap.SearchCurrentLoot(); else inventoryInteractionMessage = reason;
        }
        private static string RarityName(ItemRarity rarity) => rarity == ItemRarity.Common ? "普通" : rarity == ItemRarity.Uncommon ? "少见" : rarity == ItemRarity.Rare ? "稀有" : "珍奇";
        private static string CategoryName(ItemCategory category) => category == ItemCategory.Consumable ? "消耗品" : category == ItemCategory.Weapon ? "武器" : category == ItemCategory.Armor ? "护具" : category == ItemCategory.Scroll ? "卷轴" : category == ItemCategory.Artifact ? "法宝" : "容器";
        private static void Fill(Rect rect, Color color) { Color old = GUI.color; GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = old; }
        private static void Outline(Rect rect, Color color)
        {
            float edge = FormalUiTheme.FrameThickness;
            float corner = FormalUiTheme.FrameCornerSize;
            Fill(new Rect(rect.x, rect.y, rect.width, edge), color);
            Fill(new Rect(rect.x, rect.yMax - edge, rect.width, edge), color);
            Fill(new Rect(rect.x, rect.y, edge, rect.height), color);
            Fill(new Rect(rect.xMax - edge, rect.y, edge, rect.height), color);
            Fill(new Rect(rect.x, rect.y, corner, corner), color);
            Fill(new Rect(rect.xMax - corner, rect.y, corner, corner), color);
            Fill(new Rect(rect.x, rect.yMax - corner, corner, corner), color);
            Fill(new Rect(rect.xMax - corner, rect.yMax - corner, corner, corner), color);
        }
        private static void Box(Rect rect, string title) { Fill(rect, FormalUiTheme.WithAlpha(Surface, .98f)); Outline(rect, FormalUiTheme.WithAlpha(FormalUiTheme.Rule, .72f)); Fill(new Rect(rect.x + FormalUiTheme.FrameThickness, rect.y + 42, rect.width - FormalUiTheme.FrameThickness * 2, 2), FormalUiTheme.WithAlpha(FormalUiTheme.Rule, .52f)); GUI.Label(new Rect(rect.x + 16, rect.y + 10, rect.width - 32, 30), title); }

        private void ConfigureFormalSkin(GUISkin source)
        {
            if (formalSkin == null) { formalSkin = Instantiate(source); formalSkin.hideFlags = HideFlags.HideAndDontSave; }
            GUI.skin = formalSkin;
            GUI.skin.font = FormalUiKit.Font;
            GUI.skin.label.fontSize = FormalUiTheme.ResponsiveFontSize(FormalUiTheme.BodyFontSize);
            GUI.skin.label.normal.textColor = Text;
            GUI.skin.label.wordWrap = true;
            GUI.skin.button.fontSize = FormalUiTheme.ResponsiveFontSize(FormalUiTheme.CaptionFontSize);
            GUI.skin.button.normal.textColor = Text;
            GUI.skin.button.hover.textColor = Text;
            GUI.skin.button.active.textColor = Text;
            GUI.skin.button.focused.textColor = Text;
            GUI.skin.button.border = new RectOffset(FormalUiTheme.FrameCornerSize, FormalUiTheme.FrameCornerSize, FormalUiTheme.FrameCornerSize, FormalUiTheme.FrameCornerSize);
            GUI.skin.button.normal.background = FramedSwatch(Panel, FormalUiTheme.Rule);
            GUI.skin.button.hover.background = FramedSwatch(Color.Lerp(Panel, Cyan, .18f), Cyan);
            GUI.skin.button.active.background = FramedSwatch(Color.Lerp(Panel, Cyan, .30f), FormalUiTheme.Ink);
            GUI.skin.textField.fontSize = FormalUiTheme.ResponsiveFontSize(FormalUiTheme.BodyFontSize);
            GUI.skin.textField.normal.textColor = Text;
            GUI.skin.textField.focused.textColor = Text;
            GUI.skin.textField.border = new RectOffset(FormalUiTheme.FrameCornerSize, FormalUiTheme.FrameCornerSize, FormalUiTheme.FrameCornerSize, FormalUiTheme.FrameCornerSize);
            GUI.skin.textField.normal.background = FramedSwatch(Surface, FormalUiTheme.Rule);
            GUI.skin.textField.focused.background = FramedSwatch(Color.Lerp(Surface, Cyan, .10f), Cyan);
            GUI.skin.box.normal.textColor = Text;
            GUI.skin.box.border = new RectOffset(FormalUiTheme.FrameCornerSize, FormalUiTheme.FrameCornerSize, FormalUiTheme.FrameCornerSize, FormalUiTheme.FrameCornerSize);
            GUI.skin.box.normal.background = FramedSwatch(FormalUiTheme.SurfaceRaised, FormalUiTheme.Rule);
        }

        private void DrawSemanticIcon(Rect rect, string semanticId, string word)
        {
            DrawIcon(rect, FormalArtRegistry.SemanticPath(semanticId));
            if (Event.current == null || !rect.Contains(Event.current.mousePosition)) return;
            semanticHoverText = word;
            semanticHoverPointer = Event.current.mousePosition;
        }

        private void DrawSemanticTooltip()
        {
            if (string.IsNullOrEmpty(semanticHoverText)) return;
            Vector2 size = GUI.skin.box.CalcSize(new GUIContent(semanticHoverText));
            Rect rect = new Rect(semanticHoverPointer.x + 14f, semanticHoverPointer.y + 14f, Mathf.Max(58f, size.x + 18f), 30f);
            GUI.Box(rect, semanticHoverText);
        }

        private Texture2D Swatch(Color color)
        {
            int key = ColorUtility.ToHtmlStringRGBA(color).GetHashCode();
            if (swatches.TryGetValue(key, out Texture2D texture)) return texture;
            texture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixel(0, 0, color); texture.Apply(false, true); swatches[key] = texture; return texture;
        }

        private Texture2D FramedSwatch(Color fill, Color border)
        {
            int key = (ColorUtility.ToHtmlStringRGBA(fill) + ColorUtility.ToHtmlStringRGBA(border) + ":frame").GetHashCode();
            if (swatches.TryGetValue(key, out Texture2D texture)) return texture;
            const int size = 32;
            texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                pixels[y * size + x] = x < FormalUiTheme.FrameThickness || x >= size - FormalUiTheme.FrameThickness || y < FormalUiTheme.FrameThickness || y >= size - FormalUiTheme.FrameThickness ? border : fill;
            texture.SetPixels(pixels); texture.Apply(false, true); swatches[key] = texture; return texture;
        }

        private void OnDestroy()
        {
            foreach (Texture2D texture in swatches.Values) if (texture != null) Destroy(texture);
            swatches.Clear();
            if (formalSkin != null) Destroy(formalSkin);
        }
    }
}
