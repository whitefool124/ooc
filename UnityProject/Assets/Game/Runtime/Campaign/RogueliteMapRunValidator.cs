using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OCC.Combat
{
    public sealed class RogueliteMapRunValidationResult
    {
        private readonly List<string> errors = new List<string>();
        public bool IsValid => errors.Count == 0;
        public IReadOnlyList<string> Errors => errors;
        public string Summary => string.Join("; ", errors);
        internal void Add(string code) { if (!errors.Contains(code)) errors.Add(code); }
        internal void AddRange(IEnumerable<string> values) { foreach (string value in values) Add(value); }
    }

    public sealed class RogueliteSaveSemanticException : InvalidOperationException
    {
        public RogueliteMapRunValidationResult Validation { get; }
        public RogueliteSaveSemanticException(RogueliteMapRunValidationResult validation)
            : base("Invalid map run semantics: " + (validation?.Summary ?? "unknown"))
        {
            Validation = validation;
        }
    }

    public static class RogueliteMapRunValidator
    {
        private static readonly HashSet<string> NodeIds = new HashSet<string>(RogueliteMapCatalog.Nodes.Select(node => node.Id), StringComparer.Ordinal);
        private static readonly HashSet<string> RewardIds = new HashSet<string>(RogueliteMapCatalog.Rewards.Select(reward => reward.Id).Concat(ItemCatalog.All.Select(item => item.Id)), StringComparer.Ordinal);
        private static readonly HashSet<string> FireSpellIds = new HashSet<string>(FireSpellCatalog.All.Select(spell => spell.Id), StringComparer.Ordinal);
        private static readonly HashSet<string> StarterIds = new HashSet<string>(FireRogueliteStarterCatalog.All, StringComparer.Ordinal);

        public static RogueliteMapRunValidationResult Validate(RogueliteMapRun run, bool verifyRoundTrip = true)
        {
            RogueliteMapRunValidationResult result = ValidateState(run);
            if (!result.IsValid || !verifyRoundTrip) return result;

            try
            {
                string first = run.ToJson();
                RogueliteMapRun reparsed = RogueliteMapRun.FromJson(first);
                RogueliteMapRunValidationResult reparsedResult = ValidateState(reparsed);
                result.AddRange(reparsedResult.Errors);
                if (reparsed.ToJson() != first) result.Add("serialization.nondeterministic");
            }
            catch (Exception)
            {
                result.Add("serialization.roundtrip_failed");
            }
            return result;
        }

        public static void ValidateOrThrow(RogueliteMapRun run, bool verifyRoundTrip = true)
        {
            RogueliteMapRunValidationResult result = Validate(run, verifyRoundTrip);
            if (!result.IsValid) throw new RogueliteSaveSemanticException(result);
        }

        // Validates semantic values that the compatibility parser intentionally normalizes
        // (unknown/duplicate ids, quickbar references and consumable counters).
        public static RogueliteMapRunValidationResult ValidateSerializedCurrent(string raw)
        {
            RogueliteMapRunValidationResult result = new RogueliteMapRunValidationResult();
            string[] parts = raw?.Split('|');
            if (parts == null || parts.Length != 36 || (parts[0] != "map10" && parts[0] != "map9")) return result;

            ValidateIdList(parts[16], NodeIds, "nodes.visited", true, result);
            ValidateIdList(parts[17], NodeIds, "nodes.completed", false, result);
            ValidateIdList(parts[18], RewardIds, "rewards.claimed", false, result);
            ValidateIdList(parts[20], FireSpellIds, "build.owned_spells", false, result);
            if (!NodeIds.Contains(parts[3])) result.Add("nodes.current_unknown");
            if (!EnemyArchetypes.All.Any(enemy => enemy.Id == parts[2])) result.Add("build.region_boss_unknown");
            if (!string.IsNullOrEmpty(parts[31]) && !StarterIds.Contains(parts[31])) result.Add("build.starter_unknown");
            ValidateNonNegative(parts, new[] { 5, 6, 7, 8, 9, 10 }, "resources", result);
            if (TryInt(parts[4], out int level) && level < 1) result.Add("resources.level_invalid");
            if (TryInt(parts[33], out int health) && (health < 0 || health > 18)) result.Add("combat.health_out_of_range");
            if (TryInt(parts[34], out int shield) && (shield < 0 || shield > 6)) result.Add("combat.shield_out_of_range");
            if (TryInt(parts[35], out int mana) && (mana < 0 || mana > 12)) result.Add("combat.mana_out_of_range");

            foreach (int index in new[] { 13, 19, 30, 32 })
                if (parts[index] != "0" && parts[index] != "1") result.Add("serialization.boolean_invalid");

            string[] equipped = parts[21].Split(',');
            if (equipped.Length != 2) result.Add("build.equipped_spell_slots");
            HashSet<string> owned = new HashSet<string>(parts[20].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal);
            HashSet<string> equippedSeen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in equipped.Where(id => !string.IsNullOrEmpty(id)))
            {
                if (!FireSpellIds.Contains(id)) result.Add("build.equipped_spell_unknown");
                if (!owned.Contains(id)) result.Add("build.equipped_spell_not_owned");
                if (!equippedSeen.Add(id)) result.Add("build.equipped_spell_duplicate");
            }

            ValidateSerializedInventory(parts, result);
            return result;
        }

        private static RogueliteMapRunValidationResult ValidateState(RogueliteMapRun run)
        {
            RogueliteMapRunValidationResult result = new RogueliteMapRunValidationResult();
            if (run == null) { result.Add("run.null"); return result; }

            if (!NodeIds.Contains(run.CurrentNodeId)) result.Add("nodes.current_unknown");
            if (!run.VisitedNodes.Contains("start") || !run.VisitedNodes.Contains(run.CurrentNodeId)) result.Add("nodes.visited_inconsistent");
            if (run.VisitedNodes.Any(id => !NodeIds.Contains(id))) result.Add("nodes.visited_unknown");
            if (run.CompletedNodes.Any(id => !NodeIds.Contains(id))) result.Add("nodes.completed_unknown");
            if (run.CompletedNodes.Any(id => !run.VisitedNodes.Contains(id))) result.Add("nodes.completed_not_visited");

            if (run.Level < 1 || run.Experience < 0 || run.AccessCards < 0 || run.Supplies < 0 || run.ScoutingBeacons < 0 || run.Parts < 0 || run.Aether < 0)
                result.Add("resources.out_of_range");
            if (run.CurrentHealth < 0 || run.CurrentHealth > 18) result.Add("combat.health_out_of_range");
            if (run.CurrentShield < 0 || run.CurrentShield > 6) result.Add("combat.shield_out_of_range");
            if (run.CurrentMana < 0 || run.CurrentMana > 12) result.Add("combat.mana_out_of_range");
            if (!run.HasCombatSnapshot && (run.CurrentHealth != 18 || run.CurrentShield != 2 || run.CurrentMana != 12)) result.Add("combat.snapshot_inconsistent");
            if (run.HasCombatSnapshot && run.CurrentHealth <= 0) result.Add("combat.defeated_snapshot");

            bool weaponValid = string.IsNullOrEmpty(run.EquippedWeaponId) || RogueliteMapCatalog.Rewards.Any(reward => reward.Id == run.EquippedWeaponId && reward.Kind == RogueliteRewardKind.Weapon);
            if (!weaponValid) result.Add("build.weapon_unknown");
            if (!string.IsNullOrEmpty(run.EquippedSpellId) && !RogueliteMapCatalog.Rewards.Any(reward => reward.Id == run.EquippedSpellId && reward.Kind == RogueliteRewardKind.Spell)) result.Add("build.spell_unknown");
            if (!string.IsNullOrEmpty(run.StarterId) && !StarterIds.Contains(run.StarterId)) result.Add("build.starter_unknown");
            if (!EnemyArchetypes.All.Any(enemy => enemy.Id == run.RegionBossId)) result.Add("build.region_boss_unknown");
            if (run.OwnedFireSpellIds.Count != run.OwnedFireSpellIds.Distinct(StringComparer.Ordinal).Count() || run.OwnedFireSpellIds.Any(id => !FireSpellIds.Contains(id))) result.Add("build.owned_spells_invalid");
            if (run.EquippedFireSpellIds.Count != 2) result.Add("build.equipped_spell_slots");
            foreach (string id in run.EquippedFireSpellIds.Where(id => !string.IsNullOrEmpty(id)))
            {
                if (!run.OwnedFireSpellIds.Contains(id)) result.Add("build.equipped_spell_not_owned");
                else if (weaponValid && !FireSpellCatalog.IsWeaponCompatible(FireSpellCatalog.Get(id), run.EquippedWeapon)) result.Add("build.equipped_spell_incompatible");
            }

            if (run.ClaimedRewards.Count != run.ClaimedRewards.Distinct(StringComparer.Ordinal).Count() || run.ClaimedRewards.Any(id => !RewardIds.Contains(id))) result.Add("rewards.claimed_invalid");
            if (!string.IsNullOrEmpty(run.EquippedWeaponId) && !run.ClaimedRewards.Contains(run.EquippedWeaponId)) result.Add("rewards.weapon_not_claimed");
            if (!string.IsNullOrEmpty(run.EquippedSpellId) && !run.ClaimedRewards.Contains(run.EquippedSpellId)) result.Add("rewards.spell_not_claimed");
            ValidatePendingState(run, result);
            ValidateInventory(run, result);
            return result;
        }

        private static void ValidatePendingState(RogueliteMapRun run, RogueliteMapRunValidationResult result)
        {
            if (!NodeIds.Contains(run.CurrentNodeId)) return;
            RogueliteMapNode node = RogueliteMapCatalog.Node(run.CurrentNodeId);
            if (run.AwaitingReward && run.PendingFireSpellReselections.Count == 0 && (!run.CompletedNodes.Contains(node.Id) || (!node.IsCombat && node.Type != RogueliteMapNodeType.Treasure)))
                result.Add("rewards.awaiting_inconsistent");
            if (run.HasDeferredNodeReward && run.PendingFireSpellReselections.Count == 0) result.Add("rewards.deferred_inconsistent");
            if (run.HasPendingContentCombat)
            {
                RogueliteNodeContentChoice choice = RogueliteNodeContentCatalog.ChoicesFor(node).FirstOrDefault(value => value.Id == run.PendingContentChoiceId);
                if (choice == null || !choice.RequiresCombat || choice.CombatMissionId != run.PendingContentCombatMissionId) result.Add("choice.pending_combat_inconsistent");
            }
            else if (!string.IsNullOrEmpty(run.PendingContentChoiceId)) result.Add("choice.pending_without_combat");
        }

        private static void ValidateInventory(RogueliteMapRun run, RogueliteMapRunValidationResult result)
        {
            if (run.Inventory == null) { result.Add("inventory.null"); return; }
            ItemInstance[] items = run.Inventory.Items.ToArray();
            if (items.Select(item => item.InstanceId).Distinct(StringComparer.Ordinal).Count() != items.Length) result.Add("inventory.instance_duplicate");
            if (items.Select(item => item.AcquiredOrder).Distinct().Count() != items.Length || items.Any(item => item.AcquiredOrder < 0 || item.AcquiredOrder >= run.NextItemSequence)) result.Add("inventory.sequence_invalid");
            foreach (ItemInstance item in items)
            {
                ItemDefinition definition = ItemCatalog.All.FirstOrDefault(value => value.Id == item.DefinitionId);
                if (definition == null) { result.Add("inventory.definition_unknown"); continue; }
                if (item.RemainingUses < 0 || item.RemainingUses > definition.MaximumUses) result.Add("inventory.remaining_uses_invalid");
                InventoryPlacement? placement = run.Inventory.PlacementOf(item.InstanceId);
                if (!placement.HasValue || !run.Inventory.CanPlace(item, placement.Value.X, placement.Value.Y, item.InstanceId, placement.Value.Rotated).Success) result.Add("inventory.placement_invalid");
            }

            if (run.ItemQuickbar == null || run.ItemQuickbar.Length != 8) { result.Add("quickbar.slot_count"); return; }
            string[] equipped = run.ItemQuickbar.Where(id => !string.IsNullOrEmpty(id)).ToArray();
            if (equipped.Distinct(StringComparer.Ordinal).Count() != equipped.Length) result.Add("quickbar.duplicate_reference");
            int special = 0;
            foreach (string id in equipped)
            {
                ItemInstance item = run.Inventory.Get(id);
                if (item == null) { result.Add("quickbar.missing_reference"); continue; }
                ItemDefinition definition = ItemCatalog.Get(item.DefinitionId);
                if (!definition.CanQuickEquip) result.Add("quickbar.restricted_item");
                if (definition.Category == ItemCategory.Scroll || definition.Category == ItemCategory.Artifact) special++;
            }
            if (special > 4) result.Add("quickbar.special_limit");
        }

        private static void ValidateSerializedInventory(string[] parts, RogueliteMapRunValidationResult result)
        {
            try
            {
                string data = Encoding.UTF8.GetString(Convert.FromBase64String(parts[22]));
                HashSet<string> instanceIds = new HashSet<string>(StringComparer.Ordinal);
                HashSet<int> orders = new HashSet<int>();
                int maxOrder = -1;
                foreach (string row in data.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string[] fields = row.Split(',');
                    if (fields.Length != 8) return; // Structural failure is classified as CorruptData by the parser.
                    string instanceId = Encoding.UTF8.GetString(Convert.FromBase64String(fields[0]));
                    string definitionId = Encoding.UTF8.GetString(Convert.FromBase64String(fields[1]));
                    if (!instanceIds.Add(instanceId)) result.Add("inventory.instance_duplicate");
                    ItemDefinition definition = ItemCatalog.All.FirstOrDefault(value => value.Id == definitionId);
                    if (definition == null) result.Add("inventory.definition_unknown");
                    if (TryInt(fields[2], out int order))
                    {
                        if (order < 0 || !orders.Add(order)) result.Add("inventory.sequence_invalid");
                        maxOrder = Math.Max(maxOrder, order);
                    }
                    if (definition != null && TryInt(fields[3], out int uses) && (uses < 0 || uses > definition.MaximumUses)) result.Add("inventory.remaining_uses_invalid");
                }
                if (TryInt(parts[24], out int next) && (next < 0 || next <= maxOrder)) result.Add("inventory.sequence_invalid");

                string[] slots = parts[23].Split(',');
                if (slots.Length != 8) result.Add("quickbar.slot_count");
                string[] references = slots.Where(id => !string.IsNullOrEmpty(id)).ToArray();
                if (references.Distinct(StringComparer.Ordinal).Count() != references.Length) result.Add("quickbar.duplicate_reference");
                if (references.Any(id => !instanceIds.Contains(id))) result.Add("quickbar.missing_reference");
            }
            catch (FormatException) { }
        }

        private static void ValidateIdList(string raw, HashSet<string> known, string prefix, bool requireStart, RogueliteMapRunValidationResult result)
        {
            string[] values = raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            if (values.Distinct(StringComparer.Ordinal).Count() != values.Length) result.Add(prefix + "_duplicate");
            if (values.Any(id => !known.Contains(id))) result.Add(prefix + "_unknown");
            if (requireStart && !values.Contains("start")) result.Add(prefix + "_missing_start");
        }

        private static void ValidateNonNegative(string[] parts, IEnumerable<int> indexes, string prefix, RogueliteMapRunValidationResult result)
        {
            foreach (int index in indexes) if (TryInt(parts[index], out int value) && value < 0) result.Add(prefix + ".negative");
        }

        private static bool TryInt(string value, out int parsed) => int.TryParse(value, out parsed);
    }
}
