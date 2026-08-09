using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OCC.Combat
{
    public enum ItemCategory { Weapon, Armor, Consumable, Scroll, Artifact, Material, Quest, Container }
    public enum ItemRarity { Common, Uncommon, Rare, Exceptional }
    public enum InventoryError { None, InvalidItem, DuplicateInstance, OutOfBounds, Occupied, MissingInstance, NoSpace, Overweight, Restricted, QuickbarFull, InsufficientActionPoints, Depleted }
    public enum ItemSort { Acquired, Name, Category, Size, Weight, RemainingUses }

    public sealed class ItemDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public ItemCategory Category { get; }
        public ItemRarity Rarity { get; }
        public string Element { get; }
        public string Provenance { get; }
        public int Width { get; }
        public int Height { get; }
        public int Weight { get; }
        public int MaximumUses { get; }
        public bool IsQuestItem { get; }
        public bool CanDiscard { get; }
        public bool CanQuickEquip { get; }
        public string IconPath { get; }
        public string InventoryArtPath { get; }

        public ItemDefinition(string id, string displayName, string description, ItemCategory category, ItemRarity rarity,
            int width = 1, int height = 1, int weight = 0, int maximumUses = 0, string element = "", string provenance = "",
            bool isQuestItem = false, bool canDiscard = true, bool canQuickEquip = false, string iconPath = "Art/FormalIcons32/loot",
            string inventoryArtPath = null)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(displayName)) throw new ArgumentException("Item identity is required.");
            if (width < 1 || height < 1 || weight < 0 || maximumUses < 0) throw new ArgumentOutOfRangeException(nameof(width));
            Id = id; DisplayName = displayName; Description = description ?? string.Empty; Category = category; Rarity = rarity;
            Width = width; Height = height; Weight = weight; MaximumUses = maximumUses; Element = element ?? string.Empty;
            Provenance = provenance ?? string.Empty; IsQuestItem = isQuestItem; CanDiscard = canDiscard; CanQuickEquip = canQuickEquip; IconPath = iconPath;
            InventoryArtPath = string.IsNullOrWhiteSpace(inventoryArtPath) ? iconPath : inventoryArtPath;
        }
    }

    public sealed class ItemInstance
    {
        public string InstanceId { get; }
        public string DefinitionId { get; }
        public int AcquiredOrder { get; }
        public int RemainingUses { get; private set; }
        public int Stability { get; private set; }
        public bool IsDepleted => MaximumUses > 0 && RemainingUses <= 0;
        public int MaximumUses => ItemCatalog.Get(DefinitionId).MaximumUses;

        public ItemInstance(string instanceId, string definitionId, int acquiredOrder, int remainingUses = -1, int stability = -1)
        {
            if (string.IsNullOrWhiteSpace(instanceId) || string.IsNullOrWhiteSpace(definitionId)) throw new ArgumentException("Item instance identity is required.");
            ItemDefinition definition = ItemCatalog.Get(definitionId);
            InstanceId = instanceId; DefinitionId = definition.Id; AcquiredOrder = acquiredOrder;
            RemainingUses = remainingUses < 0 ? definition.MaximumUses : Math.Min(remainingUses, definition.MaximumUses);
            Stability = stability;
        }

        public bool TryConsume(int amount = 1)
        {
            if (amount < 1) throw new ArgumentOutOfRangeException(nameof(amount));
            if (MaximumUses == 0 || RemainingUses < amount) return false;
            RemainingUses -= amount; return true;
        }
        public ItemInstance Clone() => new ItemInstance(InstanceId, DefinitionId, AcquiredOrder, RemainingUses, Stability);
    }

    public readonly struct InventoryPlacement
    {
        public string InstanceId { get; }
        public int X { get; }
        public int Y { get; }
        public bool Rotated { get; }
        public InventoryPlacement(string instanceId, int x, int y, bool rotated = false) { InstanceId = instanceId; X = x; Y = y; Rotated = rotated; }
    }

    public readonly struct ItemFootprintSize
    {
        public int Width { get; }
        public int Height { get; }
        public ItemFootprintSize(int width, int height) { Width = width; Height = height; }
    }

    public readonly struct InventoryResult
    {
        public bool Success => Error == InventoryError.None;
        public InventoryError Error { get; }
        public string InstanceId { get; }
        public int X { get; }
        public int Y { get; }
        public bool Rotated { get; }
        public InventoryResult(InventoryError error, string instanceId = null, int x = -1, int y = -1, bool rotated = false) { Error = error; InstanceId = instanceId; X = x; Y = y; Rotated = rotated; }
        public static InventoryResult Ok(string id, int x, int y, bool rotated = false) => new InventoryResult(InventoryError.None, id, x, y, rotated);
    }

    public sealed class InventoryContainerState
    {
        public const int BaseWidth = 6;
        public const int BaseHeight = 10;
        private readonly Dictionary<string, ItemInstance> instances = new Dictionary<string, ItemInstance>(StringComparer.Ordinal);
        private readonly Dictionary<string, InventoryPlacement> placements = new Dictionary<string, InventoryPlacement>(StringComparer.Ordinal);
        public string Id { get; }
        public int Width { get; }
        public int Height { get; }
        public int WeightLimit { get; }
        public IReadOnlyList<ItemInstance> Items => instances.Values.OrderBy(item => item.AcquiredOrder).ThenBy(item => item.InstanceId, StringComparer.Ordinal).ToArray();
        public IReadOnlyList<InventoryPlacement> Placements => placements.Values.OrderBy(p => p.Y).ThenBy(p => p.X).ThenBy(p => p.InstanceId, StringComparer.Ordinal).ToArray();
        public int CurrentWeight => instances.Values.Sum(item => ItemCatalog.Get(item.DefinitionId).Weight);

        public InventoryContainerState(string id = "backpack", int width = BaseWidth, int height = BaseHeight, int weightLimit = 0)
        {
            if (string.IsNullOrWhiteSpace(id) || width < 1 || height < 1 || weightLimit < 0) throw new ArgumentOutOfRangeException(nameof(width));
            Id = id; Width = width; Height = height; WeightLimit = weightLimit;
        }

        public ItemInstance Get(string instanceId) => instanceId != null && instances.TryGetValue(instanceId, out ItemInstance value) ? value : null;
        public InventoryPlacement? PlacementOf(string instanceId) => placements.TryGetValue(instanceId, out InventoryPlacement value) ? value : (InventoryPlacement?)null;
        public ItemInstance GetAt(int x, int y)
        {
            foreach (InventoryPlacement placement in placements.Values)
            {
                ItemDefinition definition = ItemCatalog.Get(instances[placement.InstanceId].DefinitionId);
                int width = placement.Rotated ? definition.Height : definition.Width;
                int height = placement.Rotated ? definition.Width : definition.Height;
                if (x >= placement.X && x < placement.X + width && y >= placement.Y && y < placement.Y + height) return instances[placement.InstanceId];
            }
            return null;
        }

        public InventoryResult CanPlace(ItemInstance item, int x, int y, string ignoredInstanceId = null, bool rotated = false)
        {
            if (item == null) return new InventoryResult(InventoryError.InvalidItem);
            ItemDefinition definition = ItemCatalog.Get(item.DefinitionId);
            int width = rotated ? definition.Height : definition.Width;
            int height = rotated ? definition.Width : definition.Height;
            if (x < 0 || y < 0 || x + width > Width || y + height > Height) return new InventoryResult(InventoryError.OutOfBounds, item.InstanceId, x, y);
            if (WeightLimit > 0 && CurrentWeight + (instances.ContainsKey(item.InstanceId) ? 0 : definition.Weight) > WeightLimit) return new InventoryResult(InventoryError.Overweight, item.InstanceId, x, y);
            for (int iy = y; iy < y + height; iy++) for (int ix = x; ix < x + width; ix++)
            {
                ItemInstance occupied = GetAt(ix, iy);
                if (occupied != null && occupied.InstanceId != ignoredInstanceId) return new InventoryResult(InventoryError.Occupied, item.InstanceId, x, y);
            }
            return InventoryResult.Ok(item.InstanceId, x, y, rotated);
        }

        public InventoryResult Place(ItemInstance item, int x, int y, bool rotated = false)
        {
            if (item == null) return new InventoryResult(InventoryError.InvalidItem);
            if (instances.ContainsKey(item.InstanceId)) return new InventoryResult(InventoryError.DuplicateInstance, item.InstanceId);
            InventoryResult result = CanPlace(item, x, y, null, rotated); if (!result.Success) return result;
            instances.Add(item.InstanceId, item); placements.Add(item.InstanceId, new InventoryPlacement(item.InstanceId, x, y, rotated)); return result;
        }

        public InventoryResult FindFirstFit(ItemInstance item)
        {
            if (item == null) return new InventoryResult(InventoryError.InvalidItem);
            for (int y = 0; y < Height; y++) for (int x = 0; x < Width; x++)
            {
                InventoryResult result = CanPlace(item, x, y); if (result.Success) return result;
                if (ItemCatalog.Get(item.DefinitionId).Width != ItemCatalog.Get(item.DefinitionId).Height) { result = CanPlace(item, x, y, null, true); if (result.Success) return new InventoryResult(InventoryError.None, item.InstanceId, x, y, true); }
            }
            return new InventoryResult(WeightLimit > 0 && CurrentWeight + ItemCatalog.Get(item.DefinitionId).Weight > WeightLimit ? InventoryError.Overweight : InventoryError.NoSpace, item.InstanceId);
        }
        public InventoryResult AddFirstFit(ItemInstance item) { InventoryResult fit = FindFirstFit(item); return fit.Success ? Place(item, fit.X, fit.Y, fit.Rotated) : fit; }

        public InventoryResult Move(string instanceId, int x, int y, bool? rotated = null)
        {
            if (!instances.TryGetValue(instanceId, out ItemInstance item)) return new InventoryResult(InventoryError.MissingInstance, instanceId);
            bool orientation = rotated ?? placements[instanceId].Rotated;
            InventoryResult result = CanPlace(item, x, y, instanceId, orientation); if (!result.Success) return result;
            placements[instanceId] = new InventoryPlacement(instanceId, x, y, orientation); return result;
        }

        public InventoryResult Rotate(string instanceId)
        {
            if (!placements.TryGetValue(instanceId, out InventoryPlacement placement) || !instances.TryGetValue(instanceId, out ItemInstance item)) return new InventoryResult(InventoryError.MissingInstance, instanceId);
            return Move(instanceId, placement.X, placement.Y, !placement.Rotated);
        }

        public InventoryResult Swap(string firstId, string secondId)
        {
            if (!placements.TryGetValue(firstId, out InventoryPlacement first) || !placements.TryGetValue(secondId, out InventoryPlacement second)) return new InventoryResult(InventoryError.MissingInstance);
            ItemInstance a = instances[firstId]; ItemInstance b = instances[secondId];
            placements.Remove(firstId); placements.Remove(secondId);
            InventoryResult placeA = CanPlace(a, second.X, second.Y, null, first.Rotated); InventoryResult placeB = CanPlace(b, first.X, first.Y, null, second.Rotated);
            if (!placeA.Success || !placeB.Success) { placements[firstId] = first; placements[secondId] = second; return new InventoryResult(InventoryError.Occupied); }
            placements[firstId] = new InventoryPlacement(firstId, second.X, second.Y, first.Rotated); placements[secondId] = new InventoryPlacement(secondId, first.X, first.Y, second.Rotated); return InventoryResult.Ok(firstId, second.X, second.Y);
        }

        public ItemInstance Remove(string instanceId)
        {
            if (!instances.TryGetValue(instanceId, out ItemInstance item)) return null;
            instances.Remove(instanceId); placements.Remove(instanceId); return item;
        }

        public InventoryContainerState Clone()
        {
            InventoryContainerState clone = new InventoryContainerState(Id, Width, Height, WeightLimit);
            foreach (InventoryPlacement placement in Placements) clone.Place(instances[placement.InstanceId].Clone(), placement.X, placement.Y, placement.Rotated);
            return clone;
        }

        public string ToDataString()
        {
            return string.Join(";", Placements.Select(p =>
            {
                ItemInstance item = instances[p.InstanceId];
                return string.Join(",", Encode(item.InstanceId), Encode(item.DefinitionId), item.AcquiredOrder, item.RemainingUses, item.Stability, p.X, p.Y, p.Rotated ? "1" : "0");
            }));
        }

        public static InventoryContainerState FromDataString(string data, string id = "backpack", int width = BaseWidth, int height = BaseHeight, int weightLimit = 0)
        {
            InventoryContainerState result = new InventoryContainerState(id, width, height, weightLimit);
            if (string.IsNullOrEmpty(data)) return result;
            foreach (string row in data.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] fields = row.Split(','); if (fields.Length != 8) throw new InvalidOperationException("Invalid inventory row.");
                ItemInstance item = new ItemInstance(Decode(fields[0]), Decode(fields[1]), int.Parse(fields[2]), int.Parse(fields[3]), int.Parse(fields[4]));
                InventoryResult placed = result.Place(item, int.Parse(fields[5]), int.Parse(fields[6]), fields[7] == "1"); if (!placed.Success) throw new InvalidOperationException("Invalid inventory placement: " + placed.Error);
            }
            return result;
        }

        public static InventoryContainerState FromLegacyMap9DataString(string data, string id = "backpack", int width = BaseWidth, int height = BaseHeight, int weightLimit = 0)
        {
            List<LegacyPlacement> rows = ParseLegacyRows(data);
            bool[,] occupied = new bool[width, height];
            foreach (LegacyPlacement row in rows)
            {
                ItemFootprintSize size = ItemCatalog.LegacyMap9Footprint(row.Item.DefinitionId);
                int itemWidth = row.Rotated ? size.Height : size.Width;
                int itemHeight = row.Rotated ? size.Width : size.Height;
                if (row.X < 0 || row.Y < 0 || row.X + itemWidth > width || row.Y + itemHeight > height)
                    throw new InvalidOperationException("Legacy inventory placement was out of bounds.");
                for (int y = row.Y; y < row.Y + itemHeight; y++) for (int x = row.X; x < row.X + itemWidth; x++)
                {
                    if (occupied[x, y]) throw new InvalidOperationException("Legacy inventory placement overlapped another item.");
                    occupied[x, y] = true;
                }
            }

            InventoryContainerState preserved = new InventoryContainerState(id, width, height, weightLimit);
            bool preservesCoordinates = true;
            foreach (LegacyPlacement row in rows)
                if (!preserved.Place(row.Item.Clone(), row.X, row.Y, row.Rotated).Success) { preservesCoordinates = false; break; }
            if (preservesCoordinates) return preserved;

            InventoryContainerState repacked = new InventoryContainerState(id, width, height, weightLimit);
            foreach (LegacyPlacement row in rows.OrderBy(value => value.Item.AcquiredOrder).ThenBy(value => value.Item.InstanceId, StringComparer.Ordinal))
            {
                InventoryResult fit = FindPreferredFit(repacked, row.Item, row.Rotated);
                if (!fit.Success) throw new InvalidOperationException("Legacy inventory does not fit the current footprint catalog.");
                InventoryResult placed = repacked.Place(row.Item.Clone(), fit.X, fit.Y, fit.Rotated);
                if (!placed.Success) throw new InvalidOperationException("Legacy inventory repack failed: " + placed.Error);
            }
            return repacked;
        }

        private static InventoryResult FindPreferredFit(InventoryContainerState inventory, ItemInstance item, bool preferredRotation)
        {
            for (int y = 0; y < inventory.Height; y++) for (int x = 0; x < inventory.Width; x++)
            {
                InventoryResult preferred = inventory.CanPlace(item, x, y, null, preferredRotation);
                if (preferred.Success) return preferred;
                ItemDefinition definition = ItemCatalog.Get(item.DefinitionId);
                if (definition.Width == definition.Height) continue;
                InventoryResult alternate = inventory.CanPlace(item, x, y, null, !preferredRotation);
                if (alternate.Success) return alternate;
            }
            return new InventoryResult(InventoryError.NoSpace, item.InstanceId);
        }

        private static List<LegacyPlacement> ParseLegacyRows(string data)
        {
            List<LegacyPlacement> rows = new List<LegacyPlacement>(); HashSet<string> ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (string row in (data ?? string.Empty).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] fields = row.Split(','); if (fields.Length != 8) throw new InvalidOperationException("Invalid legacy inventory row.");
                ItemInstance item = new ItemInstance(Decode(fields[0]), Decode(fields[1]), int.Parse(fields[2]), int.Parse(fields[3]), int.Parse(fields[4]));
                if (!ids.Add(item.InstanceId)) throw new InvalidOperationException("Duplicate legacy inventory instance.");
                rows.Add(new LegacyPlacement(item, int.Parse(fields[5]), int.Parse(fields[6]), fields[7] == "1"));
            }
            return rows;
        }

        private readonly struct LegacyPlacement
        {
            public ItemInstance Item { get; }
            public int X { get; }
            public int Y { get; }
            public bool Rotated { get; }
            public LegacyPlacement(ItemInstance item, int x, int y, bool rotated) { Item = item; X = x; Y = y; Rotated = rotated; }
        }
        private static string Encode(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        private static string Decode(string value) => Encoding.UTF8.GetString(Convert.FromBase64String(value));
    }

    public sealed class ItemQuery
    {
        public string Text { get; set; } = string.Empty;
        public ItemCategory? Category { get; set; }
        public ItemRarity? Rarity { get; set; }
        public string Element { get; set; } = string.Empty;
        public bool? Usable { get; set; }
        public bool? QuestItem { get; set; }
        public bool? QuickEquipped { get; set; }
        public ItemSort Sort { get; set; } = ItemSort.Acquired;
    }

    public static class ItemSearchService
    {
        public static IReadOnlyList<ItemInstance> Search(InventoryContainerState inventory, ItemQuery query, IEnumerable<string> quickbarIds = null)
        {
            if (inventory == null) throw new ArgumentNullException(nameof(inventory)); query = query ?? new ItemQuery();
            HashSet<string> quick = new HashSet<string>(quickbarIds ?? Array.Empty<string>(), StringComparer.Ordinal);
            IEnumerable<ItemInstance> values = inventory.Items.Where(item => Matches(item, query, quick));
            Func<ItemInstance, object> key = item => Key(item, query.Sort);
            return values.OrderBy(key).ThenBy(item => item.InstanceId, StringComparer.Ordinal).ToArray();
        }
        private static bool Matches(ItemInstance item, ItemQuery query, HashSet<string> quick)
        {
            ItemDefinition d = ItemCatalog.Get(item.DefinitionId); string text = query.Text?.Trim() ?? string.Empty;
            return (text.Length == 0 || d.DisplayName.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0 || d.Id.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                && (!query.Category.HasValue || d.Category == query.Category) && (!query.Rarity.HasValue || d.Rarity == query.Rarity)
                && (string.IsNullOrEmpty(query.Element) || string.Equals(d.Element, query.Element, StringComparison.OrdinalIgnoreCase))
                && (!query.Usable.HasValue || query.Usable.Value == (!item.IsDepleted && (d.MaximumUses > 0 || d.Category == ItemCategory.Consumable)))
                && (!query.QuestItem.HasValue || query.QuestItem.Value == d.IsQuestItem)
                && (!query.QuickEquipped.HasValue || query.QuickEquipped.Value == quick.Contains(item.InstanceId));
        }
        private static object Key(ItemInstance item, ItemSort sort)
        {
            ItemDefinition d = ItemCatalog.Get(item.DefinitionId);
            switch (sort) { case ItemSort.Name: return d.DisplayName; case ItemSort.Category: return (int)d.Category; case ItemSort.Size: return d.Width * d.Height; case ItemSort.Weight: return d.Weight; case ItemSort.RemainingUses: return item.RemainingUses; default: return item.AcquiredOrder; }
        }
    }

    public static class ItemCatalog
    {
        public static readonly ItemDefinition Medkit = new ItemDefinition("medkit", "医疗包", "恢复结构的战地耗材。", ItemCategory.Consumable, ItemRarity.Common, width: 2, height: 1, maximumUses: 1, canQuickEquip: true, iconPath: "Art/FormalItemIcons32/medkit", inventoryArtPath: "Art/FormalInventoryFootprints/medkit");
        public static readonly ItemDefinition ShieldCell = new ItemDefinition("shield_cell", "护盾电池", "恢复护盾并清除燃烧。", ItemCategory.Consumable, ItemRarity.Common, width: 1, height: 2, maximumUses: 1, canQuickEquip: true, iconPath: "Art/FormalItemIcons32/shield_cell", inventoryArtPath: "Art/FormalInventoryFootprints/shield_cell");
        public static readonly ItemDefinition FirelineScroll = new ItemDefinition("F-S01", "火线卷轴", "一次性封装火术式。", ItemCategory.Scroll, ItemRarity.Uncommon, width: 2, height: 1, maximumUses: 1, element: "火", provenance: "现代制作", canQuickEquip: true, iconPath: "Art/FormalItemIcons32/fire_scroll", inventoryArtPath: "Art/FormalInventoryFootprints/fire_scroll");
        public static readonly ItemDefinition DemolitionCanister = ArtifactCatalog.DemolitionCanister.ToItemDefinition();
        public static readonly ItemDefinition AetherCore = new ItemDefinition("aether_core", "以太核心", "任务回收用工业核心。", ItemCategory.Quest, ItemRarity.Rare, width: 2, height: 2, weight: 3, isQuestItem: true, canDiscard: false, iconPath: "Art/FormalResourceIcons32/operational_aether", inventoryArtPath: "Art/FormalInventoryFootprints/aether_core");
        public static readonly IReadOnlyList<ItemDefinition> All = new[] { Medkit, ShieldCell, FirelineScroll, AetherCore }
            .Concat(ArtifactCatalog.All.Select(artifact => artifact.ToItemDefinition())).ToArray();
        public static ItemDefinition Get(string id) => All.FirstOrDefault(item => item.Id == id) ?? throw new InvalidOperationException("Unknown item definition: " + id);

        public static ItemFootprintSize LegacyMap9Footprint(string id)
        {
            switch (id)
            {
                case "aether_core": return new ItemFootprintSize(2, 1);
                case "F-T01": case "G-T03": case "G-T05": case "G-T11": case "G-T13": case "G-T15": case "G-T16": case "G-T18": return new ItemFootprintSize(2, 1);
                case "G-T06": case "G-T07": return new ItemFootprintSize(2, 2);
                case "G-T08": case "G-T17": return new ItemFootprintSize(1, 2);
                default:
                    Get(id);
                    return new ItemFootprintSize(1, 1);
            }
        }
    }

    public static class ItemAbilityCatalog
    {
        public static readonly FireSpellDefinition FirelineScroll = new FireSpellDefinition(
            "F-S01", "火线卷轴", FireSpellRarity.Uncommon, FireSpellGroup.Fireground, 1, 0, 0, 0, 4,
            FireTargetKind.EmptyCell, FireSelectionShape.Line, 4, true, true, new[]
            {
                new FireSpellRule(FireRuleKind.Damage, 8, scope: FireRuleScope.Selection, affectAllies: true),
                new FireSpellRule(FireRuleKind.CreateFireground, 8, 4, FireRuleScope.Selection, affectAllies: true)
            }, "fire_projectile", "fire_cross_blast", "fire_burning_ground");
        public static FireSpellDefinition For(string definitionId)
        {
            if (definitionId == "F-S01") return FirelineScroll;
            return null;
        }
    }

    public sealed class DeterministicItemIdAllocator
    {
        private readonly string prefix; private int next;
        public DeterministicItemIdAllocator(int seed, int nextValue = 0) { prefix = Math.Abs(seed).ToString("D6"); next = Math.Max(0, nextValue); }
        public string Next(string definitionId) => prefix + "-" + (next++).ToString("D6") + "-" + definitionId;
        public int NextValue => next;
    }

    public enum LootSearchState { Unsearched, Searching, Searched, Emptied }

    public sealed class LootSourceState
    {
        private readonly List<ItemInstance> contents;
        private readonly HashSet<string> revealed = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> taken = new HashSet<string>(StringComparer.Ordinal);
        public string Id { get; }
        public GridPosition Position { get; }
        public LootSearchState State => taken.Count == contents.Count && contents.Count > 0 ? LootSearchState.Emptied : revealed.Count == contents.Count ? LootSearchState.Searched : revealed.Count == 0 ? LootSearchState.Unsearched : LootSearchState.Searching;
        public IReadOnlyList<ItemInstance> RevealedItems => contents.Where(item => revealed.Contains(item.InstanceId) && !taken.Contains(item.InstanceId)).Select(item => item.Clone()).ToArray();
        public int HiddenCount => contents.Count - revealed.Count;
        public int RevealedCount => revealed.Count;
        public IReadOnlyCollection<string> TakenInstanceIds => taken.OrderBy(id => id, StringComparer.Ordinal).ToArray();
        public bool IsComplete => revealed.Count == contents.Count;

        public LootSourceState(string id, GridPosition position, IEnumerable<ItemInstance> items)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Loot source identity is required.");
            Id = id; Position = position; contents = (items ?? throw new ArgumentNullException(nameof(items))).Select(item => item.Clone()).OrderBy(item => item.AcquiredOrder).ThenBy(item => item.InstanceId, StringComparer.Ordinal).ToList();
            if (contents.Select(item => item.InstanceId).Distinct(StringComparer.Ordinal).Count() != contents.Count) throw new ArgumentException("Duplicate loot instance.");
        }

        public ItemInstance RevealNext()
        {
            ItemInstance next = contents.FirstOrDefault(item => !revealed.Contains(item.InstanceId));
            if (next == null) return null; revealed.Add(next.InstanceId); return next.Clone();
        }

        public InventoryResult Take(string instanceId, InventoryContainerState destination)
        {
            if (!revealed.Contains(instanceId) || taken.Contains(instanceId)) return new InventoryResult(InventoryError.MissingInstance, instanceId);
            ItemInstance item = contents.First(value => value.InstanceId == instanceId).Clone(); InventoryResult result = destination.AddFirstFit(item);
            if (result.Success) taken.Add(instanceId); return result;
        }

        public IReadOnlyList<InventoryResult> TakeAllRevealed(InventoryContainerState destination)
        {
            return RevealedItems.Select(item => Take(item.InstanceId, destination)).ToArray();
        }

        public LootSourceState Clone()
        {
            LootSourceState clone = new LootSourceState(Id, Position, contents);
            foreach (string id in revealed) clone.revealed.Add(id); foreach (string id in taken) clone.taken.Add(id); return clone;
        }
        public string ToProgressString() => revealed.Count + ":" + string.Join(",", TakenInstanceIds);
        public void RestoreProgress(string data)
        {
            if (string.IsNullOrEmpty(data)) return; string[] parts = data.Split(':'); int count = Math.Min(contents.Count, int.Parse(parts[0]));
            for (int i = 0; i < count; i++) revealed.Add(contents[i].InstanceId);
            if (parts.Length > 1) foreach (string id in parts[1].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)) if (contents.Any(item => item.InstanceId == id)) { revealed.Add(id); taken.Add(id); }
        }
    }
}
