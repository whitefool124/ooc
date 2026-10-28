using System;
using System.Linq;
using OCC.Combat.Roguelite;
using RogueSlot = OCC.Combat.Roguelite.EquipmentSlot;

namespace OCC.Combat
{
    public sealed partial class AcademyTutorialModelExercise
    {
        private static string InventoryFingerprint(RogueEquipmentRuntime items)
        { var dto = RogueRunDto.CreateNew("teaching-inventory", 4312); items.WriteToDto(dto); return Rogue11Serializer.Serialize(dto); }
        private static Model Equipment()
        {
            var items = RogueEquipmentRuntime.CreateStarter(4312);
            var head = items.CreateInstance("teaching-head", "ACA-EQ-HD01", EquipmentRarity.Uncommon, 9, "teaching"); items.AddToBackpack(head);
            bool equipped = false, unequipped = false;
            return new Model
            {
                Prompt = "九个装备位只接受相应部位。这里比较的是示例护目镜与空头部栏。",
                Read = () => "头部 " + (string.IsNullOrEmpty(items.Equipped[RogueSlot.Head]) ? "空" : "测距护目镜") + "\n护目镜位置 " + (items.Backpack.ContainsKey(head.InstanceId) ? "背包" : "头部装备栏") + "\n效果：武器射程 +1；总装备位 " + EquipmentSlotRules.ActiveSlots.Count,
                Steps = new Action[] { () => equipped = items.Equip(head.InstanceId, RogueSlot.Head), () => unequipped = items.Unequip(RogueSlot.Head) },
                Labels = new[] { "穿戴示例护目镜", "卸下并放回背包" },
                Verify = () => equipped && unequipped && items.Backpack.ContainsKey(head.InstanceId) && string.IsNullOrEmpty(items.Equipped[RogueSlot.Head]),
                Fingerprint = () => InventoryFingerprint(items), Proofs = new[] { "equip", "unequip", "comparison" }
            };
        }
        private static Model Placement()
        {
            var items = RogueEquipmentRuntime.CreateStarter(4312);
            var head = items.CreateInstance("teaching-head", "ACA-EQ-HD01", EquipmentRarity.Uncommon, 9, "teaching"); items.AddToBackpack(head);
            bool moved = false, rotated = false, preserved = false;
            return new Model
            {
                Prompt = "物品按真实占格摆放。移动与旋转不会改变物品，越界不会提交。",
                Read = () => { var p = items.Backpack[head.InstanceId]; return "测距护目镜　" + (p.Rotated ? "1×2" : "2×1") + "\n背包格 " + p.X + ", " + p.Y + "\n旋转 " + (p.Rotated ? "已旋转" : "原方向"); },
                Steps = new Action[]
                {
                    () => moved = items.MoveBackpack(head.InstanceId, 1, 1, false),
                    () => rotated = items.RotateBackpack(head.InstanceId),
                    () => { string before = InventoryFingerprint(items); bool accepted = items.MoveBackpack(head.InstanceId, -1, 1, true); preserved = !accepted && before == InventoryFingerprint(items); }
                },
                Labels = new[] { "移动示例物品", "旋转示例物品", "查看越界为何不能放下" },
                Verify = () => moved && rotated && preserved,
                Fingerprint = () => InventoryFingerprint(items), Proofs = new[] { "move", "rotate", "illegal-unchanged" }
            };
        }
        private static Model Capacity()
        {
            var items = RogueEquipmentRuntime.CreateStarter(4312);
            var item = items.CreateTacticalItem("teaching-item", "G-T12", 9, "teaching"); items.AddTacticalToBackpack(item);
            if (!items.MoveBackpack(item.InstanceId, 0, items.BackpackRows - 1, false)) throw new InvalidOperationException("Capacity fixture is invalid.");
            bool preserved = false;
            return new Model
            {
                Prompt = "装备背架扩充容量。卸下后装不下时，原背架和摆放都会保留。",
                ActionLabel = "尝试卸下演练背架",
                Read = () => "当前容量 " + items.BackpackColumns + "×" + items.BackpackRows + "\n卸下后基础容量 6×4\n最后一行有示例物品\n背架 " + (string.IsNullOrEmpty(items.Equipped[RogueSlot.Backpack]) ? "已卸下" : "仍穿戴"),
                Apply = () => { string before = InventoryFingerprint(items); bool accepted = items.Unequip(RogueSlot.Backpack); preserved = !accepted && before == InventoryFingerprint(items); },
                Verify = () => preserved, Fingerprint = () => InventoryFingerprint(items), Proofs = new[] { "capacity-failure", "preserved" }
            };
        }
        private static Model ItemCharges()
        {
            var combat = TrainingRangeScenarioFactory.CreateStandard(); combat.ConfigureRuleset(CombatRuleset.Roguelite); CombatResolver.BeginTurn(combat, "hero");
            var bag = new InventoryContainerState(); var scroll = new ItemInstance("teaching-scroll", ItemCatalog.FirelineScroll.Id, 0);
            var artifact = new ItemInstance("teaching-artifact", "G-T01", 1); bag.AddFirstFit(scroll); bag.AddFirstFit(artifact);
            combat.ConfigureItemInventory(bag, Array.Empty<string>()); var battle = new ArtifactBattleState(combat);
            artifact = combat.ItemInventory.Get(artifact.InstanceId);
            int artifactBefore = artifact.RemainingUses, mana = combat.GetUnit("hero").Mana; bool scrollGone = false;
            return new Model
            {
                Prompt = "卷轴使用一次即消耗；法宝按物品实例扣总次数。先看所示费用和其他代价。",
                Read = () => "卷轴 " + (combat.ItemInventory.Get(scroll.InstanceId) == null ? "已消耗" : "1次") + "\n折盾匣剩余 " + artifact.RemainingUses + "　总计 " + ItemCatalog.Get(artifact.DefinitionId).MaximumUses + "\n魔力 " + combat.GetUnit("hero").Mana,
                Steps = new Action[]
                {
                    () => { var target = ArtifactTarget.At(new GridPosition(4, 3)); ArtifactEngine.ExecuteInventory(battle, "hero", scroll.InstanceId, target); scrollGone = combat.ItemInventory.Get(scroll.InstanceId) == null; },
                    () => ArtifactEngine.ExecuteInventory(battle, "hero", artifact.InstanceId, ArtifactTarget.Unit("hero", combat.GetUnit("hero").Position))
                },
                Labels = new[] { "使用演练卷轴", "使用演练法宝" },
                Verify = () => scrollGone && artifact.RemainingUses == artifactBefore - 1 && combat.GetUnit("hero").Mana == mana - ArtifactCatalog.FirelineScroll.ManaCost - ArtifactCatalog.AegisFold.ManaCost,
                Fingerprint = () => combat.ItemInventory.ToDataString() + "|" + combat.GetUnit("hero").Mana,
                Proofs = new[] { "scroll-consumed", "artifact-paid", "artifact-consumed" }
            };
        }
        private static Model CombatInventory()
        {
            var combat = TrainingRangeScenarioFactory.CreateStandard(); combat.ConfigureRuleset(CombatRuleset.Roguelite); CombatResolver.BeginTurn(combat, "hero");
            var locked = RogueSpellLoadout.CreateStarter().CreateCombatSnapshot(); int ap = combat.GetUnit("hero").ActionPoints; bool blocked = false;
            return new Model
            {
                Prompt = "战斗中打开背包按预览扣行动点。术式配置会锁定，关闭背包不返还费用。",
                Read = () => "行动点 " + combat.GetUnit("hero").ActionPoints + "\n开包次数 " + combat.InventoryOpenCount + "\n术式配置 " + (locked.IsCombatLocked ? "战斗中锁定" : "可编辑"),
                Steps = new Action[] { () => CombatResolver.Resolve(combat, CombatCommand.OpenInventory("hero")), () => { try { locked.Equip(0, ""); } catch (InvalidOperationException) { blocked = true; } } },
                Labels = new[] { "打开演练背包", "查看术式配置限制" },
                Verify = () => combat.GetUnit("hero").ActionPoints == ap - 1 && blocked,
                Fingerprint = () => combat.GetUnit("hero").ActionPoints + "|" + combat.InventoryOpenCount + "|" + string.Join("|", locked.EquippedSpellIds),
                Proofs = new[] { "inventory-ap", "spell-config-locked" }
            };
        }
        private static Model PassiveSpell()
        {
            var hero = new UnitState("hero", true, new GridPosition(1, 1)); hero.ConfigureMana(12, 8);
            var target = new UnitState("target", false, new GridPosition(2, 1)); target.ConfigureVitality(99);
            var combat = new CombatState(new GridMap(5, 5), new[] { hero, target }); combat.ConfigureRuleset(CombatRuleset.Roguelite); CombatResolver.BeginTurn(combat, "hero");
            var loadout = RogueSpellLoadout.CreateStarter(); loadout.Learn("PASSIVE-ELITE-01"); RogueSpellCombatRuntime runtime = null;
            int manaBefore = hero.Mana, cost = 0;
            return new Model
            {
                Prompt = "被动术式也占一个槽，按条件自动触发；它没有可主动施放的按钮。",
                Read = () => "槽位已用 " + loadout.EquippedSpellIds.Count(s => !string.IsNullOrEmpty(s)) + "　总计8\n个人魔力 " + hero.Mana + "\n回火导流 " + (loadout.EquippedSpellIds.Contains("PASSIVE-ELITE-01") ? "已装备（被动）" : "未装备"),
                Steps = new Action[]
                {
                    () => { loadout.Equip(4, "PASSIVE-ELITE-01"); runtime = new RogueSpellCombatRuntime(combat, loadout.CreateCombatSnapshot()); combat.AttachRogueSpellRuntime(runtime); },
                    () => { cost = runtime.EffectiveManaCost(runtime.DefinitionAtSlot(0)); runtime.ExecuteSlot(0, CombatCommand.UseSkill("hero", 0, "target")); }
                },
                Labels = new[] { "装入示例被动术式", "执行一次术式，观察自动触发" },
                Verify = () => runtime != null && runtime.DefinitionAtSlot(4).Role == "passive" && hero.Mana == manaBefore - cost + 1,
                Fingerprint = () => string.Join("|", loadout.EquippedSpellIds) + "|" + hero.Mana,
                Proofs = new[] { "passive-equip", "passive-trigger" }
            };
        }
        private static Model Search()
        {
            var bag = new InventoryContainerState(); bag.AddFirstFit(new ItemInstance("teaching-medkit", ItemCatalog.Medkit.Id, 0)); bag.AddFirstFit(new ItemInstance("teaching-scroll", ItemCatalog.FirelineScroll.Id, 1));
            var query = new ItemQuery(); int found = 0, filtered = 0; bool cleared = false;
            Func<string> read = () => "当前检索结果\n" + string.Join("\n", ItemSearchService.Search(bag, query).Select(i => ItemCatalog.Get(i.DefinitionId).DisplayName));
            return new Model
            {
                Prompt = "搜索名称或筛选类别可缩小列表。清除条件后恢复完整物品视图。",
                Read = read,
                Steps = new Action[] { () => { query.Text = ItemCatalog.Medkit.DisplayName; found = ItemSearchService.Search(bag, query).Count; }, () => { query.Text = ""; query.Category = ItemCatalog.FirelineScroll.Category; filtered = ItemSearchService.Search(bag, query).Count; }, () => { query = new ItemQuery(); cleared = ItemSearchService.Search(bag, query).Count == 2; } },
                Labels = new[] { "按示例名称搜索", "筛选卷轴类别", "清除检索条件" },
                Verify = () => found == 1 && filtered == 1 && cleared, Fingerprint = read,
                Proofs = new[] { "search", "filter", "reset-filter" }
            };
        }
        private static Model Discard()
        {
            var bag = new InventoryContainerState("teaching", 2, 1); var original = new ItemInstance("teaching-medkit", ItemCatalog.Medkit.Id, 0); bag.AddFirstFit(original);
            var incoming = new ItemInstance("teaching-shield", ItemCatalog.ShieldCell.Id, 1); bool cancelled = false, overflow = false, removed = false;
            var confirmation = new UiConfirmationSession();
            string originalData = bag.ToDataString();
            confirmation.Open(new UiConfirmationRequest(UiConfirmationKind.DiscardInventoryItem, "丢弃演练医疗包？", "确认后会失去此示例物品。", "确认丢弃"), () => bag.Remove(original.InstanceId));
            return new Model
            {
                Prompt = "丢弃后原物品会失去。放不下新物品时，可返回整理或放弃；演练不丢弃真实物品。",
                Read = () => "演练背包 2×1\n原物品 " + (bag.Get(original.InstanceId) == null ? "已丢弃" : "保留") + "\n待放入物品 " + (bag.Get(incoming.InstanceId) == null ? "尚未放入" : "已放入"),
                Steps = new Action[]
                {
                    () => { confirmation.Cancel(); cancelled = bag.Get(original.InstanceId) != null && originalData == bag.ToDataString(); },
                    () => { string before = bag.ToDataString(); overflow = !bag.AddFirstFit(incoming).Success && before == bag.ToDataString(); },
                    () => { removed = bag.Remove(original.InstanceId) != null; bag.AddFirstFit(incoming); }
                },
                Labels = new[] { "预览后取消丢弃", "查看容量不足时的保留结果", "确认丢弃示例物品，再放入新物品" },
                Verify = () => cancelled && overflow && removed && bag.Get(incoming.InstanceId) != null, Fingerprint = bag.ToDataString,
                Proofs = new[] { "discard-preview", "discard-cancel", "overflow-choice" }
            };
        }
    }
}
