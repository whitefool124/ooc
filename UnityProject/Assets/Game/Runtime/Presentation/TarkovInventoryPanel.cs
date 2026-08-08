using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    public sealed class TarkovInventoryPanel : MonoBehaviour
    {
        private CombatPrototypeBootstrap bootstrap;
        private bool open;
        private string selectedId;
        private string searchText = string.Empty;
        private ItemCategory? category;
        private Vector2 resultScroll;
        private readonly Dictionary<string, Texture2D> icons = new Dictionary<string, Texture2D>(StringComparer.Ordinal);
        private readonly Dictionary<int, Texture2D> swatches = new Dictionary<int, Texture2D>();
        private GUISkin formalSkin;

        private static Color Ink => FormalUiTheme.Ink;
        private static Color Panel => FormalUiTheme.Panel;
        private static Color Surface => FormalUiTheme.Surface;
        private static Color Cyan => FormalUiTheme.Cyan;
        private static Color Text => FormalUiTheme.Text;
        private static Color Muted => FormalUiTheme.Muted;

        public void Initialize(CombatPrototypeBootstrap source) { bootstrap = source; }
        public bool IsOpen => open;

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
                if (GUI.Button(new Rect(1170, 944, 236, 54), "背包 / 搜索 [B]")) open = true;
                GUI.skin = previousSkin; GUI.matrix = previous; return;
            }
            DrawPanel(); GUI.skin = previousSkin; GUI.matrix = previous;
        }

        private void HandleHotkey()
        {
            Event current = Event.current; if (current == null || current.type != EventType.KeyDown) return;
            if (current.keyCode == KeyCode.B) { open = !open; current.Use(); }
            else if (open && current.keyCode == KeyCode.R && !string.IsNullOrEmpty(selectedId)) { bootstrap.CurrentState.ItemInventory.Rotate(selectedId); bootstrap.NotifyInventoryChanged(); current.Use(); }
        }

        private void DrawPanel()
        {
            CombatState state = bootstrap.CurrentState; if (state == null) return;
            Fill(new Rect(0, 0, 1920, 1080), new Color(Ink.r, Ink.g, Ink.b, .82f));
            Rect panel = new Rect(32, 24, 1856, 1024); Fill(panel, new Color(Ink.r, Ink.g, Ink.b, .985f)); Outline(panel, new Color(Cyan.r, Cyan.g, Cyan.b, .72f));
            Fill(new Rect(34, 26, 1852, 112), new Color(Panel.r, Panel.g, Panel.b, .98f));
            Fill(new Rect(34, 136, 1852, 1), new Color(Cyan.r, Cyan.g, Cyan.b, .55f));
            GUI.Label(new Rect(100, 72, 880, 42), "OCC // 战术背包与容器搜索  6×10");
            GUI.color = Muted; GUI.Label(new Rect(100, 112, 1180, 30), "B 关闭 · 物品可旋转 · 容器逐项揭示 · 战斗内搜索/换入各 1 AP · 无倒计时"); GUI.color = Color.white;
            if (GUI.Button(new Rect(1630, 72, 180, 52), "返回战斗 [B]")) open = false;

            DrawInventory(state, new Rect(100, 160, 600, 720));
            DrawDetailsAndSearch(state, new Rect(730, 160, 500, 720));
            DrawLoot(state, new Rect(1260, 160, 550, 720));
            DrawQuickbar(state, new Rect(100, 900, 1710, 96));
        }

        private void DrawInventory(CombatState state, Rect rect)
        {
            DrawIcon(new Rect(rect.x + rect.width - 78, rect.y + 7, 28, 28), "Art/FormalItemIcons32/category_container");
            DrawIcon(new Rect(rect.x + rect.width - 44, rect.y + 7, 28, 28), "Art/FormalItemIcons32/inventory_weight");
            Box(rect, $"基础背包 // 6×10 // {state.ItemInventory.Items.Count} 件 // 负重 {state.ItemInventory.CurrentWeight}");
            const float cell = 52f; float gx = rect.x + 26; float gy = rect.y + 68;
            for (int y = 0; y < 10; y++) for (int x = 0; x < 6; x++)
            {
                ItemInstance occupied = state.ItemInventory.GetAt(x, y);
                Rect slotRect = new Rect(gx + x * cell, gy + y * cell, cell - 3, cell - 3);
                string slotSkin = occupied != null && occupied.InstanceId == selectedId ? "slot_selected" : "slot";
                DrawIcon(slotRect, "Art/FormalUI32/" + slotSkin, false);
                if (GUI.Button(slotRect, GUIContent.none, GUIStyle.none))
                {
                    if (occupied != null) selectedId = occupied.InstanceId;
                    else if (!string.IsNullOrEmpty(selectedId)) { state.ItemInventory.Move(selectedId, x, y); bootstrap.NotifyInventoryChanged(); }
                }
            }
            foreach (InventoryPlacement placement in state.ItemInventory.Placements)
            {
                ItemInstance item = state.ItemInventory.Get(placement.InstanceId); ItemDefinition definition = ItemCatalog.Get(item.DefinitionId);
                int width = placement.Rotated ? definition.Height : definition.Width; int height = placement.Rotated ? definition.Width : definition.Height;
                Rect itemRect = new Rect(gx + placement.X * cell + 3, gy + placement.Y * cell + 3, width * cell - 9, height * cell - 9);
                DrawIcon(itemRect, "Art/FormalUI32/" + (item.InstanceId == selectedId ? "slot_selected" : "slot"), false);
                Texture2D icon = Icon(definition.IconPath); if (icon != null) GUI.DrawTexture(new Rect(itemRect.x + 4, itemRect.y + 4, Math.Min(38, itemRect.width - 8), Math.Min(38, itemRect.height - 8)), icon, ScaleMode.ScaleToFit, true);
                GUI.Label(new Rect(itemRect.x + 44, itemRect.y + 4, itemRect.width - 48, 24), definition.DisplayName);
                if (definition.MaximumUses > 0) GUI.Label(new Rect(itemRect.x + 4, itemRect.yMax - 24, itemRect.width - 8, 22), item.RemainingUses + "/" + definition.MaximumUses + " 次");
            }
            GUI.enabled = !string.IsNullOrEmpty(selectedId);
            if (IconButton(new Rect(gx + 330, gy, 220, 46), "Art/FormalItemIcons32/inventory_rotate", "旋转选中物品 [R]")) { state.ItemInventory.Rotate(selectedId); bootstrap.NotifyInventoryChanged(); }
            GUI.enabled = true;
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
                else GUI.Label(new Rect(x, y + 92, 450, 158),
                    "来源：" + artifact.Provenance + "\n" +
                    "代价：" + artifact.PublicCost + "  次数 " + selected.RemainingUses + "/" + artifact.MaximumUses + "\n" +
                    "目标：" + artifact.TargetSummary + "\n" +
                    "效果：" + artifact.EffectSummary + "\n" +
                    "风险/反制：" + artifact.RiskSummary + "\n" +
                    "构筑用途：" + artifact.BuildUse);
            }
            y += artifactDetails ? 270 : 180; DrawIcon(new Rect(x, y - 2, 30, 30), "Art/FormalItemIcons32/inventory_search"); GUI.Label(new Rect(x + 38, y, 82, 28), "名称"); searchText = GUI.TextField(new Rect(x + 120, y, 320, 34), searchText ?? string.Empty);
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
                GUI.Label(new Rect(48, i * 48 + 7, 348, 28), d.DisplayName + "  //  " + d.Width + "×" + d.Height);
                if (GUI.Button(resultRect, GUIContent.none, GUIStyle.none)) selectedId = results[i].InstanceId;
            }
            GUI.EndScrollView();
        }

        private void DrawLoot(CombatState state, Rect rect)
        {
            LootSourceState visualLoot = state.LootSource;
            string visualLootIcon = visualLoot == null || visualLoot.IsComplete ? "loot_empty" : visualLoot.State == LootSearchState.Unsearched ? "loot_unknown" : "loot_searching";
            DrawIcon(new Rect(rect.x + rect.width - 44, rect.y + 7, 28, 28), "Art/FormalItemIcons32/" + visualLootIcon);
            Box(rect, "战利品容器 // 类塔科夫逐项搜索"); LootSourceState loot = state.LootSource; float x = rect.x + 22; float y = rect.y + 58;
            if (loot == null) { DrawIcon(new Rect(x, y, 64, 64), "Art/FormalItemIcons32/loot_empty"); GUI.Label(new Rect(x + 82, y + 18, 400, 40), "当前战场没有可搜索容器。"); return; }
            GUI.Label(new Rect(x, y, 500, 62), "状态：" + StateName(loot.State) + "\n未知物品：" + loot.HiddenCount + "  /  已揭示可取：" + loot.RevealedItems.Count);
            UnitState hero = state.GetUnit("hero"); bool adjacent = hero != null && Math.Abs(hero.Position.X - loot.Position.X) + Math.Abs(hero.Position.Y - loot.Position.Y) == 1;
            y += 78; GUI.enabled = adjacent && !loot.IsComplete && hero.ActionPoints >= 1;
            if (IconButton(new Rect(x, y, 500, 54), "Art/FormalItemIcons32/inventory_search", "继续搜索 // 1 AP")) bootstrap.SearchCurrentLoot(); GUI.enabled = true;
            if (!adjacent) GUI.Label(new Rect(x, y + 60, 500, 28), "需要移动到容器相邻格。");
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
                if (IconButton(new Rect(x + 340, y + row * 66 + 4, 150, 48), "Art/FormalItemIcons32/inventory_autoplace", "拿取")) bootstrap.TakeCurrentLoot(item.InstanceId); row++;
            }
        }

        private void DrawQuickbar(CombatState state, Rect rect)
        {
            DrawIcon(new Rect(rect.x + rect.width - 44, rect.y + 7, 28, 28), "Art/FormalItemIcons32/inventory_quickbar");
            Box(rect, "8 格快捷栏 // 卷轴与法宝合计最多 4 件 // 战斗内换入 1 AP"); float x = rect.x + 320;
            if (IconButton(new Rect(rect.x + 20, rect.y + 42, 280, 40), string.IsNullOrEmpty(selectedId) ? "Art/FormalItemIcons32/inventory_use" : "Art/FormalItemIcons32/inventory_clear", string.IsNullOrEmpty(selectedId) ? "未选物品 // 点击槽位使用" : "清除选中 // 改为使用槽位")) selectedId = null;
            for (int i = 0; i < 8; i++)
            {
                int slot = i; string instanceId = state.ItemQuickbar[i]; ItemInstance item = state.ItemInventory.Get(instanceId); ItemDefinition definition = item == null ? null : ItemCatalog.Get(item.DefinitionId); string label = item == null ? (i + 1) + " 空" : (i + 1) + " " + definition.DisplayName;
                Rect slotRect = new Rect(x + i * 166, rect.y + 42, 156, 40); DrawIcon(slotRect, "Art/FormalUI32/slot", false);
                if (item != null) DrawIcon(new Rect(slotRect.x + 5, slotRect.y + 4, 32, 32), definition.IconPath);
                GUI.Label(new Rect(slotRect.x + 42, slotRect.y + 7, 108, 28), label);
                if (GUI.Button(slotRect, GUIContent.none, GUIStyle.none))
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
            Texture2D texture = Icon(path);
            if (texture != null) GUI.DrawTexture(rect, texture, preserveAspect ? ScaleMode.ScaleToFit : ScaleMode.StretchToFill, true);
        }
        private bool IconButton(Rect rect, string path, string label)
        {
            bool clicked = GUI.Button(rect, "       " + label);
            DrawIcon(new Rect(rect.x + 8, rect.y + (rect.height - 28) * .5f, 28, 28), path);
            return clicked;
        }
        private static string CategoryIcon(ItemCategory? value) => "Art/FormalItemIcons32/category_" + (value?.ToString().ToLowerInvariant() ?? "container");
        private static ItemCategory? NextCategory(ItemCategory? value) { if (!value.HasValue) return ItemCategory.Consumable; int next = (int)value.Value + 1; return next > (int)ItemCategory.Container ? (ItemCategory?)null : (ItemCategory)next; }
        private static string StateName(LootSearchState state) => state == LootSearchState.Unsearched ? "未搜索" : state == LootSearchState.Searching ? "搜索中" : state == LootSearchState.Searched ? "已搜索" : "已清空";
        private static string RarityName(ItemRarity rarity) => rarity == ItemRarity.Common ? "普通" : rarity == ItemRarity.Uncommon ? "少见" : rarity == ItemRarity.Rare ? "稀有" : "珍奇";
        private static string CategoryName(ItemCategory category) => category == ItemCategory.Consumable ? "消耗品" : category == ItemCategory.Weapon ? "武器" : category == ItemCategory.Armor ? "护具" : category == ItemCategory.Scroll ? "卷轴" : category == ItemCategory.Artifact ? "法宝" : "容器";
        private static void Fill(Rect rect, Color color) { Color old = GUI.color; GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = old; }
        private static void Outline(Rect rect, Color color) { Fill(new Rect(rect.x, rect.y, rect.width, 1), color); Fill(new Rect(rect.x, rect.yMax - 1, rect.width, 1), color); Fill(new Rect(rect.x, rect.y, 1, rect.height), color); Fill(new Rect(rect.xMax - 1, rect.y, 1, rect.height), color); }
        private static void Box(Rect rect, string title) { Fill(rect, new Color(Surface.r, Surface.g, Surface.b, .98f)); Outline(rect, new Color(Cyan.r, Cyan.g, Cyan.b, .34f)); Fill(new Rect(rect.x + 1, rect.y + 42, rect.width - 2, 1), new Color(Cyan.r, Cyan.g, Cyan.b, .22f)); GUI.Label(new Rect(rect.x + 16, rect.y + 10, rect.width - 32, 30), title); }

        private void ConfigureFormalSkin(GUISkin source)
        {
            if (formalSkin == null) { formalSkin = Instantiate(source); formalSkin.hideFlags = HideFlags.HideAndDontSave; }
            GUI.skin = formalSkin;
            GUI.skin.font = FormalUiKit.Font;
            GUI.skin.label.fontSize = FormalUiTheme.BodyFontSize;
            GUI.skin.label.normal.textColor = Text;
            GUI.skin.button.fontSize = 15;
            GUI.skin.button.normal.textColor = Text;
            GUI.skin.button.hover.textColor = Text;
            GUI.skin.button.active.textColor = Text;
            GUI.skin.button.normal.background = Swatch(Panel);
            GUI.skin.button.hover.background = Swatch(Color.Lerp(Panel, Cyan, .18f));
            GUI.skin.button.active.background = Swatch(Color.Lerp(Panel, Cyan, .30f));
            GUI.skin.textField.fontSize = 16;
            GUI.skin.textField.normal.textColor = Text;
            GUI.skin.textField.focused.textColor = Text;
            GUI.skin.textField.normal.background = Swatch(Surface);
            GUI.skin.textField.focused.background = Swatch(Color.Lerp(Surface, Cyan, .10f));
        }

        private Texture2D Swatch(Color color)
        {
            int key = ColorUtility.ToHtmlStringRGBA(color).GetHashCode();
            if (swatches.TryGetValue(key, out Texture2D texture)) return texture;
            texture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixel(0, 0, color); texture.Apply(false, true); swatches[key] = texture; return texture;
        }

        private void OnDestroy()
        {
            foreach (Texture2D texture in swatches.Values) if (texture != null) Destroy(texture);
            swatches.Clear();
            if (formalSkin != null) Destroy(formalSkin);
        }
    }
}
