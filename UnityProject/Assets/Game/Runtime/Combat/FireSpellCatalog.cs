using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    public enum FireSpellRarity { Common, Uncommon, Rare }
    public enum FireSpellGroup { Precision, Fireground, Detonation, Breach, Tactics, Melee, Universal, Ranged }
    public enum FireCombatAffinity { MeleeOnly, WeaponUniversal, RangedSpell }
    public enum FireDeliveryMode { BodyEnhancement, WeaponAttachment, ContactConduction, DetachedProjection, SelfStance, TargetMarking, Movement, FiregroundManipulation }
    public enum FireWeaponRequirement { None, MeleeWeapon, RangedWeapon, AnyWeapon }
    public enum FireTriggerWindow { Immediate, NextLegalWeaponAttack, CurrentAction, UntilNextAction, FirstAdjacentAttack, FirstMarkedTargetMove, FirstEnemyEntry, AfterNextWeaponAttack, CurrentTurnEnd, AfterNextCharge, AfterNextObjectDestruction }
    public enum FireConsumptionRule { OnCast, OnLegalAttackCommitted, OnTrigger, OnWindowEnd }
    public enum FireRuleTiming { OnCast, OnTrigger }
    public enum FireTargetKind { Self, Enemy, AllyOrSelf, Unit, EmptyCell, BurningUnit, BurningEnemy, BurningCell, Destructible, Hittable, AdjacentEnemy, AdjacentBurningEnemy, BurningOrArmorBrokenEnemy, Cell }
    public enum FireSelectionShape { Single, Line, ContinuousLine, Cone, Cross, OrthogonalRing, CenterAndOrthogonal, Square3, AroundUnit, Path, FirstHitLine, FoldedPath, ReflectionRay }
    public enum FireRuleKind
    {
        Damage, WeaponDamage, ApplyBurning, ExtendBurning, SetBurningDuration, CreateFireground, ExtendFireground,
        ConsumeBurning, ConsumeFireground, DamageDurability, DestroyLightCover, ApplyArmorBreak, RestoreShield,
        ClearStatus, RestoreMovement, AddMovement, MoveSource, SwapUnits, Push, RestoreMana, LoseHealth,
        RepairWeapon, ReduceIncomingDamage, ApplyBreakStance, GrantShieldBeforeRanged, ClearOneSelfStatus,
        MoveAfterAttack, SpendActionPoints, SpendMana, ReduceForcedMove,
        ArmTrigger, ConsumeTrigger, OverloadDevice, ApplyMeltBarrierMark, ApplyFracture, ArmFractureShield, ReserveNextTurnAction, PushAllUnits, PushFromDestroyedObjects,
        ApplyFiregroundBoost, ApplyFiregroundVulnerability,
        // 破障：只作为术式标记，不单独产生结算；带此标记的伤害使作用几何内的物块耐久伤害翻倍（总案 3.5.6.1／3.5.6.2）。
        BreakBarrier, CreateLightCover, AdvanceIntoBreach, ClearBoundOrSlow, OfferRetreat, OfferBreachMove
    }
    public enum FireRuleScope { Primary, Selection, EnemySelection, AllySelection, OrthogonalNeighbors, PathAdjacentEnemies, Source, SourceCell, Destination, CoveredCells, ReflectedFirstHit }
    public enum FireCondition
    {
        Always, TargetBurning, TargetOnFireground, TargetBurningAndOnFireground, TargetArmorBroken,
        TargetBurningOrArmorBroken, TargetBreakStance, TargetBurningOrBreakStance, SourceBreakStance,
        SourceBurning, SourceNotBurning, SourceBound, SourceSlowed,
        SourceNotArmorBroken, TargetAtWeaponMaxRange, LightCoverDestroyed, DurabilityDepleted
    }
    public enum FireSourceConsumption { None, BurningFirstThenGround, BurningOnly, GroundOnly, BurningAndGround }
    [Flags]
    public enum FireDestructibleMask { None = 0, LightCover = 1, HeavyCover = 2, Device = 4, Plant = 8, All = LightCover | HeavyCover | Device | Plant }

    public readonly struct FireSpellRule
    {
        public FireRuleKind Kind { get; }
        public FireRuleScope Scope { get; }
        public FireCondition Condition { get; }
        public FireRuleTiming Timing { get; }
        public int Amount { get; }
        public int Duration { get; }
        public int AlternateAmount { get; }
        public bool AffectAllies { get; }
        public StatusType Status { get; }
        public FireSourceConsumption Consumption { get; }
        public FireDestructibleMask DestructibleMask { get; }

        public FireSpellRule(FireRuleKind kind, int amount = 0, int duration = 0, FireRuleScope scope = FireRuleScope.Primary,
            FireCondition condition = FireCondition.Always, int alternateAmount = 0, bool affectAllies = false,
            StatusType status = default, FireSourceConsumption consumption = FireSourceConsumption.None,
            FireDestructibleMask destructibleMask = FireDestructibleMask.All, FireRuleTiming timing = FireRuleTiming.OnCast)
        {
            Kind = kind; Amount = amount; Duration = duration; Scope = scope; Condition = condition;
            AlternateAmount = alternateAmount; AffectAllies = affectAllies; Status = status; Consumption = consumption;
            DestructibleMask = destructibleMask; Timing = timing;
        }
    }

    public sealed class FireSpellDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public FireSpellRarity Rarity { get; }
        public FireSpellGroup Group { get; }
        public FireCombatAffinity CombatAffinity { get; }
        public FireDeliveryMode DeliveryMode { get; }
        public FireWeaponRequirement WeaponRequirement { get; }
        public FireTriggerWindow TriggerWindow { get; }
        public FireConsumptionRule ConsumptionRule { get; }
        public int ActionPointCost { get; }
        public int ManaCost { get; }
        public int Cooldown { get; }
        public int InitiativeDelay { get; }
        public int MinimumRange { get; }
        public int Range { get; }
        public FireTargetKind TargetKind { get; }
        public FireSelectionShape Shape { get; }
        public int ShapeLength { get; }
        public bool RequiresLineOfSight { get; }
        public bool HeavyCoverTruncates { get; }
        public string IconPath { get; }
        public IReadOnlyList<string> PresentationModules { get; }
        public IReadOnlyList<FireSpellRule> Rules { get; }

        public FireSpellDefinition(string id, string displayName, FireSpellRarity rarity, FireSpellGroup group,
            FireCombatAffinity combatAffinity, FireDeliveryMode deliveryMode, FireWeaponRequirement weaponRequirement,
            FireTriggerWindow triggerWindow, FireConsumptionRule consumptionRule, int ap, int mana, int cooldown,
            int delay, int range, FireTargetKind targetKind, FireSelectionShape shape, int shapeLength,
            bool lineOfSight, bool heavyCoverTruncates, IEnumerable<FireSpellRule> rules, params string[] presentationModules)
            : this(id, displayName, rarity, group, combatAffinity, deliveryMode, weaponRequirement, triggerWindow,
                consumptionRule, ap, mana, cooldown, delay, 0, range, targetKind, shape, shapeLength, lineOfSight,
                heavyCoverTruncates, rules, presentationModules) { }

        public FireSpellDefinition(string id, string displayName, FireSpellRarity rarity, FireSpellGroup group,
            FireCombatAffinity combatAffinity, FireDeliveryMode deliveryMode, FireWeaponRequirement weaponRequirement,
            FireTriggerWindow triggerWindow, FireConsumptionRule consumptionRule, int ap, int mana, int cooldown,
            int delay, int minimumRange, int range, FireTargetKind targetKind, FireSelectionShape shape, int shapeLength,
            bool lineOfSight, bool heavyCoverTruncates, IEnumerable<FireSpellRule> rules, params string[] presentationModules)
        {
            if (minimumRange < 0) throw new ArgumentOutOfRangeException(nameof(minimumRange));
            if (range < minimumRange) throw new ArgumentException("Maximum range cannot be below minimum range.", nameof(range));
            FireSpellRarity resolvedRarity = FireSpellCatalog.RarityFor(id, rarity);
            Id = id; DisplayName = displayName; Rarity = resolvedRarity; Group = group; CombatAffinity = combatAffinity;
            DeliveryMode = deliveryMode; WeaponRequirement = weaponRequirement; TriggerWindow = triggerWindow;
            ConsumptionRule = consumptionRule; ActionPointCost = FireSpellCatalog.BalancedActionPointCost(id, resolvedRarity, ap);
            ManaCost = FireSpellCatalog.BalancedManaCost(id, resolvedRarity, mana);
            Cooldown = FireSpellCatalog.BalancedCooldown(id, cooldown);
            InitiativeDelay = delay; MinimumRange = minimumRange; Range = range; TargetKind = targetKind; Shape = shape; ShapeLength = shapeLength;
            RequiresLineOfSight = lineOfSight; HeavyCoverTruncates = heavyCoverTruncates;
            IconPath = "Art/FormalSkillIcons32/Fire/" + FireSpellCatalog.IconIdFor(id);
            Rules = (rules ?? throw new ArgumentNullException(nameof(rules))).ToArray();
            PresentationModules = (presentationModules ?? Array.Empty<string>()).ToArray();
        }

        // Compatibility for scroll/artifact definitions that use the v0.1 mechanical shape but are not
        // members of the personal-spell M/U/R progression contract.
        public FireSpellDefinition(string id, string displayName, FireSpellRarity rarity, FireSpellGroup group,
            int ap, int mana, int cooldown, int delay, int range, FireTargetKind targetKind,
            FireSelectionShape shape, int shapeLength, bool lineOfSight, bool heavyCoverTruncates,
            IEnumerable<FireSpellRule> rules, params string[] presentationModules)
            : this(id, displayName, rarity, group, FireCombatAffinity.RangedSpell,
                FireDeliveryMode.DetachedProjection, FireWeaponRequirement.None, FireTriggerWindow.Immediate,
                FireConsumptionRule.OnCast, ap, mana, cooldown, delay, range, targetKind, shape, shapeLength,
                lineOfSight, heavyCoverTruncates, rules, presentationModules) { }

        // 运行时变体（术式专精）从既有定义重建：其行动点与魔力已经过平衡，必须原样保留，
        // 不能再走一次平衡流程（否则专精后会被二次减费，与卡面和预览不一致）。
        internal FireSpellDefinition(FireSpellDefinition source, IEnumerable<FireSpellRule> rules,
            int minimumRange, int range, int shapeLength, int manaCost = -1, int cooldown = -1)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            Id = source.Id; DisplayName = source.DisplayName; Rarity = source.Rarity; Group = source.Group;
            CombatAffinity = source.CombatAffinity; DeliveryMode = source.DeliveryMode;
            WeaponRequirement = source.WeaponRequirement; TriggerWindow = source.TriggerWindow;
            ConsumptionRule = source.ConsumptionRule; ActionPointCost = source.ActionPointCost;
            ManaCost = manaCost >= 0 ? manaCost : source.ManaCost;
            Cooldown = cooldown >= 0 ? cooldown : source.Cooldown;
            InitiativeDelay = source.InitiativeDelay;
            MinimumRange = minimumRange; Range = range; TargetKind = source.TargetKind; Shape = source.Shape;
            ShapeLength = shapeLength; RequiresLineOfSight = source.RequiresLineOfSight;
            HeavyCoverTruncates = source.HeavyCoverTruncates; IconPath = source.IconPath;
            Rules = (rules ?? throw new ArgumentNullException(nameof(rules))).ToArray();
            PresentationModules = source.PresentationModules.ToArray();
        }
    }

    public static class FireSpellCatalog
    {
        public const string Version = "fire-personal-spells-v0.4-reviewed-migration";

        // IDs stay stable for saves. 总案 3.5.6.5：稀有度按三条主构筑分配（地块破坏回流／突进穿刺／燃烧火场
        // 各 10 普通／7 罕见／3 稀有，合计 30/21/9），因此不再按 M／U／R 分线均分。
        // PersonalRarities 是个人术式稀有度的唯一来源；S(...) 调用点的稀有度字面量只是缺失 ID 的回退值，
        // 但必须与下表保持一致，避免被误读为另一套口径。
        private static readonly IReadOnlyDictionary<string, FireSpellRarity> PersonalRarities =
            new Dictionary<string, FireSpellRarity>(StringComparer.Ordinal)
            {
                { "F-P-M01", FireSpellRarity.Common }, { "F-P-M02", FireSpellRarity.Common }, { "F-P-M03", FireSpellRarity.Uncommon }, { "F-P-M04", FireSpellRarity.Common }, { "F-P-M05", FireSpellRarity.Uncommon },
                { "F-P-M06", FireSpellRarity.Common }, { "F-P-M07", FireSpellRarity.Rare }, { "F-P-M08", FireSpellRarity.Uncommon }, { "F-P-M09", FireSpellRarity.Rare }, { "F-P-M10", FireSpellRarity.Common },
                { "F-P-M11", FireSpellRarity.Common }, { "F-P-M12", FireSpellRarity.Uncommon }, { "F-P-M13", FireSpellRarity.Uncommon }, { "F-P-M14", FireSpellRarity.Common }, { "F-P-M15", FireSpellRarity.Common },
                { "F-P-M16", FireSpellRarity.Common }, { "F-P-M17", FireSpellRarity.Uncommon }, { "F-P-M18", FireSpellRarity.Common }, { "F-P-M19", FireSpellRarity.Uncommon }, { "F-P-M20", FireSpellRarity.Rare },
                { "F-P-M21", FireSpellRarity.Common }, { "F-P-M23", FireSpellRarity.Uncommon }, { "F-P-M24", FireSpellRarity.Common }, { "F-P-M25", FireSpellRarity.Common }, { "F-P-M26", FireSpellRarity.Rare },
                { "F-P-M22", FireSpellRarity.Uncommon },
                { "F-P-U01", FireSpellRarity.Common }, { "F-P-U02", FireSpellRarity.Common }, { "F-P-U03", FireSpellRarity.Uncommon }, { "F-P-U04", FireSpellRarity.Common }, { "F-P-U05", FireSpellRarity.Uncommon },
                { "F-P-U06", FireSpellRarity.Common }, { "F-P-U07", FireSpellRarity.Common }, { "F-P-U08", FireSpellRarity.Uncommon }, { "F-P-U09", FireSpellRarity.Common }, { "F-P-U10", FireSpellRarity.Uncommon },
                { "F-P-U11", FireSpellRarity.Common }, { "F-P-U12", FireSpellRarity.Uncommon }, { "F-P-U13", FireSpellRarity.Uncommon }, { "F-P-U14", FireSpellRarity.Common }, { "F-P-U15", FireSpellRarity.Common },
                { "F-P-U16", FireSpellRarity.Uncommon }, { "F-P-U17", FireSpellRarity.Common }, { "F-P-U18", FireSpellRarity.Common }, { "F-P-U19", FireSpellRarity.Uncommon }, { "F-P-U20", FireSpellRarity.Rare },
                { "F-P-U21", FireSpellRarity.Common }, { "F-P-U22", FireSpellRarity.Uncommon }, { "F-P-U23", FireSpellRarity.Common }, { "F-P-U24", FireSpellRarity.Uncommon }, { "F-P-U25", FireSpellRarity.Rare },
                { "F-P-U26", FireSpellRarity.Common }, { "F-P-U27", FireSpellRarity.Common }, { "F-P-U28", FireSpellRarity.Common },
                { "F-P-R01", FireSpellRarity.Common }, { "F-P-R02", FireSpellRarity.Common }, { "F-P-R03", FireSpellRarity.Common }, { "F-P-R04", FireSpellRarity.Common }, { "F-P-R05", FireSpellRarity.Uncommon },
                { "F-P-R06", FireSpellRarity.Uncommon }, { "F-P-R07", FireSpellRarity.Common }, { "F-P-R08", FireSpellRarity.Uncommon }, { "F-P-R09", FireSpellRarity.Rare }, { "F-P-R10", FireSpellRarity.Uncommon },
                { "F-P-R11", FireSpellRarity.Uncommon }, { "F-P-R12", FireSpellRarity.Common }, { "F-P-R13", FireSpellRarity.Common }, { "F-P-R14", FireSpellRarity.Rare }, { "F-P-R15", FireSpellRarity.Uncommon },
                { "F-P-R16", FireSpellRarity.Rare }, { "F-P-R17", FireSpellRarity.Common }, { "F-P-R18", FireSpellRarity.Common }, { "F-P-R19", FireSpellRarity.Rare }, { "F-P-R20", FireSpellRarity.Rare },
                { "F-P-R21", FireSpellRarity.Common }, { "F-P-R22", FireSpellRarity.Uncommon }, { "F-P-R23", FireSpellRarity.Uncommon }, { "F-P-R24", FireSpellRarity.Common }, { "F-P-R25", FireSpellRarity.Uncommon }, { "F-P-R26", FireSpellRarity.Rare }
            };

        public static FireSpellRarity RarityFor(string id, FireSpellRarity fallback)
            => id != null && PersonalRarities.TryGetValue(id, out FireSpellRarity rarity) ? rarity : fallback;

        public static int PersonalSpellCount(FireSpellRarity rarity)
            => All.Count(spell => spell.Rarity == rarity);

        // 总案 3.5.6.5：允许 0 行动点／0 魔力／0 冷却的术式，但必须消耗已存在的燃烧或火场，
        // 不能自行建立资源、恢复行动点或反复产生无条件收益。首批为「焦甲吸热」「热源回收」「地火抽爆」。
        private static readonly HashSet<string> ZeroTempoIds = new HashSet<string>(StringComparer.Ordinal)
        {
            "F-P-M17", "F-P-U11", "F-P-R17"
        };

        public static int BalancedActionPointCost(string id, FireSpellRarity rarity, int authoredCost)
        {
            if (ZeroTempoIds.Contains(id)) return 0;
            // Terminal spells remain a commitment, but no longer consume a whole three-action turn.
            return rarity == FireSpellRarity.Rare && authoredCost >= 3 ? 2 : authoredCost;
        }

        // 总案 3.5.6.5：普通与罕见术式的魔力成本统一下调 1 点，最低分别为 0 与 1；稀有不下调。
        public static int BalancedManaCost(string id, FireSpellRarity rarity, int authoredCost)
        {
            if (ZeroTempoIds.Contains(id)) return 0;
            if (authoredCost <= 0) return 0;
            return rarity == FireSpellRarity.Common ? Math.Max(0, authoredCost - 1) :
                rarity == FireSpellRarity.Uncommon ? Math.Max(1, authoredCost - 1) : authoredCost;
        }

        public static int BalancedCooldown(string id, int authoredCooldown) => ZeroTempoIds.Contains(id) ? 0 : authoredCooldown;

        private static FireSpellRule R(FireRuleKind kind, int amount = 0, int duration = 0,
            FireRuleScope scope = FireRuleScope.Primary, FireCondition condition = FireCondition.Always,
            int alternate = 0, bool allies = false, StatusType status = default,
            FireSourceConsumption consume = FireSourceConsumption.None,
            FireDestructibleMask objects = FireDestructibleMask.All, FireRuleTiming timing = FireRuleTiming.OnCast) =>
            new FireSpellRule(kind, amount, duration, scope, condition, alternate, allies, status, consume, objects, timing);

        private static FireSpellDefinition S(string id, string name, FireSpellRarity rarity, FireSpellGroup group,
            FireCombatAffinity affinity, FireDeliveryMode delivery, FireWeaponRequirement weapon,
            FireTriggerWindow window, FireConsumptionRule consumption, int ap, int mana, int cooldown, int delay,
            int range, FireTargetKind target, FireSelectionShape shape, int length, bool los, bool truncate,
            FireSpellRule[] rules, params string[] vfx) => new FireSpellDefinition(id, name, rarity, group, affinity,
                delivery, weapon, window, consumption, ap, mana, cooldown, delay, range, target, shape, length, los,
                truncate, rules, vfx);

        private static FireSpellDefinition RangeBand(string id, string name, FireSpellRarity rarity, FireSpellGroup group,
            FireCombatAffinity affinity, FireDeliveryMode delivery, FireWeaponRequirement weapon,
            FireTriggerWindow window, FireConsumptionRule consumption, int ap, int mana, int cooldown, int delay,
            int minimumRange, int range, FireTargetKind target, FireSelectionShape shape, int length, bool los, bool truncate,
            FireSpellRule[] rules, params string[] vfx) => new FireSpellDefinition(id, name, rarity, group, affinity,
                delivery, weapon, window, consumption, ap, mana, cooldown, delay, minimumRange, range, target, shape,
                length, los, truncate, rules, vfx);

        private const FireCombatAffinity M = FireCombatAffinity.MeleeOnly;
        private const FireCombatAffinity U = FireCombatAffinity.WeaponUniversal;
        private const FireCombatAffinity X = FireCombatAffinity.RangedSpell;
        private const FireWeaponRequirement MW = FireWeaponRequirement.MeleeWeapon;
        private const FireWeaponRequirement AW = FireWeaponRequirement.AnyWeapon;
        private const FireWeaponRequirement NW = FireWeaponRequirement.None;
        private const FireTriggerWindow Now = FireTriggerWindow.Immediate;
        private const FireConsumptionRule Cast = FireConsumptionRule.OnCast;

        public static readonly IReadOnlyList<FireSpellDefinition> All = new[]
        {
            // M01-M05: body reinforcement, engagement and pursuit.
            S("F-P-M01","热脉增压",FireSpellRarity.Common,FireSpellGroup.Melee,M,FireDeliveryMode.BodyEnhancement,MW,FireTriggerWindow.NextLegalWeaponAttack,FireConsumptionRule.OnLegalAttackCommitted,1,3,1,0,0,FireTargetKind.Self,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.AddMovement,2,scope:FireRuleScope.Source),R(FireRuleKind.Damage,8,timing:FireRuleTiming.OnTrigger)},"path","fire_projectile"),
            S("F-P-M02","炉压突步",FireSpellRarity.Common,FireSpellGroup.Melee,M,FireDeliveryMode.Movement,MW,Now,Cast,1,3,1,0,2,FireTargetKind.EmptyCell,FireSelectionShape.Path,2,false,true,new[]{R(FireRuleKind.MoveSource,2,scope:FireRuleScope.Destination)},"path"),
            S("F-P-M03","焦痕跃进",FireSpellRarity.Uncommon,FireSpellGroup.Melee,M,FireDeliveryMode.ContactConduction,MW,Now,Cast,2,4,2,0,3,FireTargetKind.EmptyCell,FireSelectionShape.Path,3,false,true,new[]{R(FireRuleKind.CreateFireground,8,2,FireRuleScope.SourceCell),R(FireRuleKind.MoveSource,3,scope:FireRuleScope.Destination),R(FireRuleKind.WeaponDamage,8,scope:FireRuleScope.PathAdjacentEnemies)},"path","fire_projectile","fire_burning_ground"),
            S("F-P-M04","热流折步",FireSpellRarity.Common,FireSpellGroup.Melee,M,FireDeliveryMode.Movement,MW,Now,Cast,1,2,1,0,1,FireTargetKind.AdjacentEnemy,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.SwapUnits)},"path"),
            S("F-P-M05","爆燃追步",FireSpellRarity.Uncommon,FireSpellGroup.Melee,M,FireDeliveryMode.TargetMarking,MW,FireTriggerWindow.FirstMarkedTargetMove,FireConsumptionRule.OnTrigger,1,3,2,0,1,FireTargetKind.AdjacentEnemy,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.MoveSource,1,timing:FireRuleTiming.OnTrigger)},"path"),

            // M06-M10: contact armor breaking and melee attacks.
            S("F-P-M06","熔势贯击",FireSpellRarity.Common,FireSpellGroup.Melee,M,FireDeliveryMode.WeaponAttachment,MW,FireTriggerWindow.NextLegalWeaponAttack,FireConsumptionRule.OnLegalAttackCommitted,1,3,1,0,0,FireTargetKind.Self,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.ApplyBreakStance,timing:FireRuleTiming.OnTrigger)},"armor_break"),
            S("F-P-M07","炉心穿刺",FireSpellRarity.Rare,FireSpellGroup.Melee,M,FireDeliveryMode.ContactConduction,MW,Now,Cast,2,5,3,4,1,FireTargetKind.AdjacentEnemy,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.WeaponDamage,20),R(FireRuleKind.Damage,8),R(FireRuleKind.ApplyBreakStance)},"heavy_hit","armor_break"),
            S("F-P-M08","炉压横扫",FireSpellRarity.Uncommon,FireSpellGroup.Melee,M,FireDeliveryMode.ContactConduction,MW,Now,Cast,2,4,2,0,3,FireTargetKind.Unit,FireSelectionShape.Cone,3,false,true,new[]{R(FireRuleKind.BreakBarrier),R(FireRuleKind.WeaponDamage,12,scope:FireRuleScope.Selection,allies:true),R(FireRuleKind.Damage,4,scope:FireRuleScope.Selection,allies:true)},"fire_spray","hit"),
            S("F-P-M09","热震重击",FireSpellRarity.Rare,FireSpellGroup.Melee,M,FireDeliveryMode.ContactConduction,MW,Now,Cast,2,4,2,4,1,FireTargetKind.AdjacentEnemy,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.BreakBarrier),R(FireRuleKind.WeaponDamage,16),R(FireRuleKind.Damage,8),R(FireRuleKind.Push,1)},"heavy_hit","path"),
            S("F-P-M10","熔隙刺击",FireSpellRarity.Common,FireSpellGroup.Melee,M,FireDeliveryMode.ContactConduction,MW,Now,Cast,1,3,1,0,1,FireTargetKind.AdjacentEnemy,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.WeaponDamage,12,condition:FireCondition.Always,alternate:20)},"hit","armor_break"),

            // M11-M15: ranged resistance, block, counter and disengage.
            S("F-P-M11","热障架势",FireSpellRarity.Common,FireSpellGroup.Melee,M,FireDeliveryMode.SelfStance,MW,FireTriggerWindow.UntilNextAction,FireConsumptionRule.OnWindowEnd,1,3,2,0,0,FireTargetKind.Self,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.GrantShieldBeforeRanged,8,timing:FireRuleTiming.OnTrigger)},"shield_restore"),
            S("F-P-M12","余烬格挡",FireSpellRarity.Uncommon,FireSpellGroup.Melee,M,FireDeliveryMode.SelfStance,MW,FireTriggerWindow.FirstAdjacentAttack,FireConsumptionRule.OnTrigger,1,3,2,0,0,FireTargetKind.Self,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.RestoreShield,12,scope:FireRuleScope.Source),R(FireRuleKind.RestoreShield,4,scope:FireRuleScope.Source,timing:FireRuleTiming.OnTrigger)},"shield_restore"),
            S("F-P-M13","炉心反击",FireSpellRarity.Uncommon,FireSpellGroup.Melee,M,FireDeliveryMode.SelfStance,MW,FireTriggerWindow.FirstAdjacentAttack,FireConsumptionRule.OnTrigger,1,4,3,0,0,FireTargetKind.Self,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.WeaponDamage,12,timing:FireRuleTiming.OnTrigger),R(FireRuleKind.Damage,4,timing:FireRuleTiming.OnTrigger)},"heavy_hit","fire_projectile"),
            S("F-P-M14","灼缚解离",FireSpellRarity.Common,FireSpellGroup.Melee,M,FireDeliveryMode.ContactConduction,MW,Now,Cast,1,3,2,0,1,FireTargetKind.AdjacentEnemy,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.ClearStatus,scope:FireRuleScope.Source,condition:FireCondition.SourceBound,status:StatusType.Bound),R(FireRuleKind.Damage,8)},"cleanse","hit"),
            S("F-P-M15","焰压退击",FireSpellRarity.Common,FireSpellGroup.Melee,M,FireDeliveryMode.ContactConduction,MW,Now,Cast,1,3,1,0,1,FireTargetKind.AdjacentEnemy,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.BreakBarrier),R(FireRuleKind.Damage,8),R(FireRuleKind.Push,1)},"fire_spray","path"),

            // M16-M20: burning conversion and finishers.
            S("F-P-M16","燃势收割",FireSpellRarity.Common,FireSpellGroup.Melee,M,FireDeliveryMode.ContactConduction,MW,Now,Cast,1,3,1,0,1,FireTargetKind.AdjacentBurningEnemy,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.WeaponDamage,16),R(FireRuleKind.Damage,8)},"heavy_hit","burning"),
            S("F-P-M17","焦甲吸热",FireSpellRarity.Uncommon,FireSpellGroup.Melee,M,FireDeliveryMode.ContactConduction,MW,Now,Cast,1,3,2,0,1,FireTargetKind.AdjacentBurningEnemy,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.Damage,12),R(FireRuleKind.ConsumeBurning,consume:FireSourceConsumption.BurningOnly),R(FireRuleKind.RestoreShield,12,scope:FireRuleScope.Source)},"fire_detonate","shield_restore"),
            S("F-P-M18","火场踏行",FireSpellRarity.Common,FireSpellGroup.Melee,M,FireDeliveryMode.FiregroundManipulation,MW,Now,Cast,1,2,1,0,3,FireTargetKind.BurningCell,FireSelectionShape.Path,3,false,true,new[]{R(FireRuleKind.MoveSource,3,scope:FireRuleScope.Destination)},"path","fire_burning_ground"),
            S("F-P-M19","炉心超限",FireSpellRarity.Uncommon,FireSpellGroup.Melee,M,FireDeliveryMode.ContactConduction,MW,Now,Cast,2,5,4,8,3,FireTargetKind.Enemy,FireSelectionShape.Path,3,false,true,new[]{R(FireRuleKind.MoveSource,3,scope:FireRuleScope.Destination),R(FireRuleKind.WeaponDamage,24),R(FireRuleKind.Damage,12),R(FireRuleKind.LoseHealth,8,scope:FireRuleScope.Source)},"path","heavy_hit","fire_detonate"),
            S("F-P-M20","终炉断击",FireSpellRarity.Rare,FireSpellGroup.Melee,M,FireDeliveryMode.ContactConduction,MW,Now,Cast,3,5,4,8,1,FireTargetKind.BurningOrArmorBrokenEnemy,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.WeaponDamage,28),R(FireRuleKind.Damage,12),R(FireRuleKind.ConsumeBurning,condition:FireCondition.TargetBurning,consume:FireSourceConsumption.BurningOnly)},"heavy_hit","fire_detonate"),

            // 已审核的新近战术式：折线路径、路径邻敌擦伤与落点推位。
            S("F-P-M21","折线突步",FireSpellRarity.Common,FireSpellGroup.Melee,M,FireDeliveryMode.Movement,MW,Now,Cast,1,3,2,0,3,FireTargetKind.EmptyCell,FireSelectionShape.FoldedPath,3,false,true,new[]{R(FireRuleKind.MoveSource,3,scope:FireRuleScope.Destination)},"path"),
            S("F-P-M22","越障跃步",FireSpellRarity.Uncommon,FireSpellGroup.Melee,M,FireDeliveryMode.Movement,MW,Now,Cast,1,5,2,0,2,FireTargetKind.EmptyCell,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.MoveSource,2,scope:FireRuleScope.Destination)},"path"),
            S("F-P-M23","擦锋穿行",FireSpellRarity.Uncommon,FireSpellGroup.Melee,M,FireDeliveryMode.Movement,MW,Now,Cast,2,5,2,0,3,FireTargetKind.EmptyCell,FireSelectionShape.Path,3,false,true,new[]{R(FireRuleKind.MoveSource,3,scope:FireRuleScope.Destination),R(FireRuleKind.WeaponDamage,8,scope:FireRuleScope.PathAdjacentEnemies,objects:FireDestructibleMask.None)},"path","hit"),
            S("F-P-M24","余势回身",FireSpellRarity.Common,FireSpellGroup.Melee,M,FireDeliveryMode.SelfStance,MW,FireTriggerWindow.AfterNextCharge,FireConsumptionRule.OnTrigger,1,2,1,0,0,FireTargetKind.Self,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.OfferRetreat,1,timing:FireRuleTiming.OnTrigger)},"path"),
            S("F-P-M25","侧压落步",FireSpellRarity.Common,FireSpellGroup.Melee,M,FireDeliveryMode.Movement,MW,Now,Cast,1,4,2,0,2,FireTargetKind.EmptyCell,FireSelectionShape.Path,2,false,true,new[]{R(FireRuleKind.MoveSource,2,scope:FireRuleScope.Destination),R(FireRuleKind.PushAllUnits,1,scope:FireRuleScope.OrthogonalNeighbors,allies:true)},"path","hit"),
            S("F-P-M26","迎锋架势",FireSpellRarity.Rare,FireSpellGroup.Melee,M,FireDeliveryMode.SelfStance,MW,FireTriggerWindow.AfterNextCharge,FireConsumptionRule.OnTrigger,1,3,3,0,0,FireTargetKind.Self,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.WeaponDamage,12,timing:FireRuleTiming.OnTrigger)},"shield_restore","heavy_hit"),

            // U01-U05: one legal weapon attack attachments.
            S("F-P-U01","脱线疾行",FireSpellRarity.Common,FireSpellGroup.Universal,U,FireDeliveryMode.Movement,NW,Now,Cast,1,3,2,0,3,FireTargetKind.EmptyCell,FireSelectionShape.Path,3,false,true,new[]{R(FireRuleKind.ReserveNextTurnAction,1,scope:FireRuleScope.Destination),R(FireRuleKind.MoveSource,3,scope:FireRuleScope.Destination)},"path"),
            S("F-P-U02","烙痕传递",FireSpellRarity.Common,FireSpellGroup.Universal,U,FireDeliveryMode.WeaponAttachment,AW,FireTriggerWindow.NextLegalWeaponAttack,FireConsumptionRule.OnLegalAttackCommitted,1,3,1,0,0,FireTargetKind.Self,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.ApplyBurning,8,1,timing:FireRuleTiming.OnTrigger)},"burning"),
            S("F-P-U03","灼蚀校准",FireSpellRarity.Uncommon,FireSpellGroup.Universal,U,FireDeliveryMode.WeaponAttachment,AW,FireTriggerWindow.NextLegalWeaponAttack,FireConsumptionRule.OnLegalAttackCommitted,1,4,2,0,0,FireTargetKind.Self,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.ApplyBreakStance,timing:FireRuleTiming.OnTrigger)},"armor_break"),
            S("F-P-U04","熔障校准",FireSpellRarity.Common,FireSpellGroup.Universal,U,FireDeliveryMode.DetachedProjection,NW,Now,Cast,1,2,2,0,4,FireTargetKind.Hittable,FireSelectionShape.Single,1,true,false,new[]{R(FireRuleKind.ApplyMeltBarrierMark,duration:4),R(FireRuleKind.Damage,8)},"fire_projectile","object_damage"),
            S("F-P-U05","爆燃弹芯",FireSpellRarity.Uncommon,FireSpellGroup.Universal,U,FireDeliveryMode.WeaponAttachment,AW,FireTriggerWindow.NextLegalWeaponAttack,FireConsumptionRule.OnLegalAttackCommitted,2,4,2,0,0,FireTargetKind.Self,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.BreakBarrier),R(FireRuleKind.Damage,4,scope:FireRuleScope.OrthogonalNeighbors,allies:true,timing:FireRuleTiming.OnTrigger)},"fire_cross_blast"),

            // U06-U10: action, displacement and defense.
            S("F-P-U06","热压续步",FireSpellRarity.Common,FireSpellGroup.Universal,U,FireDeliveryMode.WeaponAttachment,AW,FireTriggerWindow.AfterNextWeaponAttack,FireConsumptionRule.OnTrigger,1,2,1,0,0,FireTargetKind.Self,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.MoveAfterAttack,1,timing:FireRuleTiming.OnTrigger),R(FireRuleKind.Damage,4,timing:FireRuleTiming.OnTrigger)},"path"),
            S("F-P-U07","炉温护持",FireSpellRarity.Common,FireSpellGroup.Universal,U,FireDeliveryMode.DetachedProjection,NW,Now,Cast,1,3,2,0,3,FireTargetKind.AllyOrSelf,FireSelectionShape.Single,1,true,false,new[]{R(FireRuleKind.RestoreShield,12)},"fire_projectile","shield_restore"),
            S("F-P-U08","余烬护甲",FireSpellRarity.Uncommon,FireSpellGroup.Universal,U,FireDeliveryMode.SelfStance,NW,Now,Cast,2,4,2,0,0,FireTargetKind.Self,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.RestoreShield,12,scope:FireRuleScope.Source,condition:FireCondition.SourceNotBurning),R(FireRuleKind.RestoreShield,20,scope:FireRuleScope.Source,condition:FireCondition.SourceBurning),R(FireRuleKind.ClearStatus,scope:FireRuleScope.Source,condition:FireCondition.SourceBurning,status:StatusType.Burning)},"cleanse","shield_restore"),
            S("F-P-U09","温血苏醒",FireSpellRarity.Common,FireSpellGroup.Universal,U,FireDeliveryMode.BodyEnhancement,NW,Now,Cast,1,2,1,0,0,FireTargetKind.Self,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.ClearStatus,scope:FireRuleScope.Source,condition:FireCondition.SourceSlowed,status:StatusType.Agility),R(FireRuleKind.RestoreMovement,UnitState.HeroBaseMovementRange,scope:FireRuleScope.Source)},"cleanse","path"),
            S("F-P-U10","热障偏流",FireSpellRarity.Uncommon,FireSpellGroup.Universal,U,FireDeliveryMode.SelfStance,NW,FireTriggerWindow.UntilNextAction,FireConsumptionRule.OnWindowEnd,1,3,2,0,0,FireTargetKind.Self,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.GrantShieldBeforeRanged,8,timing:FireRuleTiming.OnTrigger)},"shield_restore"),

            // U11-U15: burning resource and attack conversion.
            S("F-P-U11","热源回收",FireSpellRarity.Common,FireSpellGroup.Universal,U,FireDeliveryMode.FiregroundManipulation,NW,Now,Cast,1,0,2,0,3,FireTargetKind.BurningCell,FireSelectionShape.Single,1,true,false,new[]{R(FireRuleKind.ConsumeFireground,consume:FireSourceConsumption.GroundOnly),R(FireRuleKind.RestoreMana,2,scope:FireRuleScope.Source)},"fire_burning_ground","mana_restore"),
            S("F-P-U12","余热转护",FireSpellRarity.Uncommon,FireSpellGroup.Universal,U,FireDeliveryMode.WeaponAttachment,AW,FireTriggerWindow.NextLegalWeaponAttack,FireConsumptionRule.OnLegalAttackCommitted,1,3,2,0,0,FireTargetKind.Self,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.RestoreShield,12,scope:FireRuleScope.Source,condition:FireCondition.TargetBurning,timing:FireRuleTiming.OnTrigger),R(FireRuleKind.ConsumeBurning,condition:FireCondition.TargetBurning,consume:FireSourceConsumption.BurningOnly,timing:FireRuleTiming.OnTrigger)},"fire_detonate","shield_restore"),
            S("F-P-U13","燃势回收",FireSpellRarity.Uncommon,FireSpellGroup.Universal,U,FireDeliveryMode.WeaponAttachment,AW,FireTriggerWindow.NextLegalWeaponAttack,FireConsumptionRule.OnLegalAttackCommitted,1,1,2,0,0,FireTargetKind.Self,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.RestoreMana,3,scope:FireRuleScope.Source,condition:FireCondition.TargetBurning,timing:FireRuleTiming.OnTrigger)},"mana_restore","burning"),
            S("F-P-U14","追火校准",FireSpellRarity.Common,FireSpellGroup.Universal,U,FireDeliveryMode.WeaponAttachment,AW,FireTriggerWindow.NextLegalWeaponAttack,FireConsumptionRule.OnLegalAttackCommitted,1,2,1,0,0,FireTargetKind.Self,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.Damage,12,condition:FireCondition.TargetBurning,timing:FireRuleTiming.OnTrigger)},"fire_projectile","burning"),
            S("F-P-U15","灰烬复燃",FireSpellRarity.Common,FireSpellGroup.Universal,U,FireDeliveryMode.WeaponAttachment,AW,FireTriggerWindow.NextLegalWeaponAttack,FireConsumptionRule.OnLegalAttackCommitted,1,2,1,0,0,FireTargetKind.Self,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.Damage,4,condition:FireCondition.TargetBurning,timing:FireRuleTiming.OnTrigger),R(FireRuleKind.SetBurningDuration,duration:2,condition:FireCondition.TargetBurning,timing:FireRuleTiming.OnTrigger)},"burning"),

            // U16-U20: heavy attack, reaction, maintenance, cooperation and finisher.
            S("F-P-U16","炉压蓄势",FireSpellRarity.Uncommon,FireSpellGroup.Universal,U,FireDeliveryMode.WeaponAttachment,AW,FireTriggerWindow.NextLegalWeaponAttack,FireConsumptionRule.OnLegalAttackCommitted,1,4,2,4,0,FireTargetKind.Self,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.WeaponDamage,12,timing:FireRuleTiming.OnTrigger)},"heavy_hit"),
            S("F-P-U17","焦土警戒",FireSpellRarity.Common,FireSpellGroup.Universal,U,FireDeliveryMode.TargetMarking,AW,FireTriggerWindow.FirstEnemyEntry,FireConsumptionRule.OnTrigger,1,4,3,0,3,FireTargetKind.EmptyCell,FireSelectionShape.Single,1,true,false,new[]{R(FireRuleKind.CreateFireground,8,2,timing:FireRuleTiming.OnTrigger)},"fire_projectile","fire_burning_ground"),
            S("F-P-U18","炉压震步",FireSpellRarity.Common,FireSpellGroup.Universal,U,FireDeliveryMode.Movement,NW,Now,Cast,1,3,2,0,2,FireTargetKind.EmptyCell,FireSelectionShape.Path,2,false,true,new[]{R(FireRuleKind.MoveSource,2,scope:FireRuleScope.Destination),R(FireRuleKind.Damage,8,scope:FireRuleScope.OrthogonalNeighbors,allies:true),R(FireRuleKind.PushAllUnits,1,scope:FireRuleScope.OrthogonalNeighbors,allies:true)},"path","fire_cross_blast","object_damage"),
            S("F-P-U19","火线协同",FireSpellRarity.Uncommon,FireSpellGroup.Universal,U,FireDeliveryMode.TargetMarking,AW,FireTriggerWindow.AfterNextWeaponAttack,FireConsumptionRule.OnTrigger,2,4,2,0,4,FireTargetKind.BurningEnemy,FireSelectionShape.Single,1,true,false,new[]{R(FireRuleKind.Push,1,timing:FireRuleTiming.OnTrigger),R(FireRuleKind.Damage,8,timing:FireRuleTiming.OnTrigger),R(FireRuleKind.CreateFireground,8,2,timing:FireRuleTiming.OnTrigger)},"burning","fire_projectile","fire_burning_ground"),
            S("F-P-U20","炉心共振",FireSpellRarity.Rare,FireSpellGroup.Universal,U,FireDeliveryMode.WeaponAttachment,AW,FireTriggerWindow.NextLegalWeaponAttack,FireConsumptionRule.OnLegalAttackCommitted,2,5,4,8,0,FireTargetKind.Self,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.Damage,20,condition:FireCondition.TargetBurningOrBreakStance,alternate:28,timing:FireRuleTiming.OnTrigger),R(FireRuleKind.ConsumeBurning,condition:FireCondition.TargetBurningAndOnFireground,consume:FireSourceConsumption.BurningOnly,timing:FireRuleTiming.OnTrigger)},"heavy_hit","fire_detonate"),

            // 已审核的新通用术式：首个命中目标测绘、突进、破障、落点推位。
            S("F-P-U21","结构测绘",FireSpellRarity.Common,FireSpellGroup.Universal,U,FireDeliveryMode.DetachedProjection,NW,Now,Cast,1,3,2,0,4,FireTargetKind.Cell,FireSelectionShape.FirstHitLine,4,false,true,new[]{R(FireRuleKind.ApplyFracture,scope:FireRuleScope.Selection),R(FireRuleKind.ApplyBreakStance,scope:FireRuleScope.Selection,allies:true)},"fire_projectile","object_damage"),
            S("F-P-U22","余料筑障",FireSpellRarity.Uncommon,FireSpellGroup.Universal,U,FireDeliveryMode.FiregroundManipulation,NW,Now,Cast,1,3,2,0,3,FireTargetKind.EmptyCell,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.CreateLightCover,8,scope:FireRuleScope.Destination)},"object_damage"),
            S("F-P-U23","破口突入",FireSpellRarity.Common,FireSpellGroup.Universal,U,FireDeliveryMode.Movement,AW,Now,Cast,2,4,2,0,3,FireTargetKind.Cell,FireSelectionShape.FirstHitLine,3,false,true,new[]{R(FireRuleKind.MoveSource,3,scope:FireRuleScope.Destination),R(FireRuleKind.WeaponDamage,12,scope:FireRuleScope.Selection),R(FireRuleKind.AdvanceIntoBreach,1,scope:FireRuleScope.Destination)},"path","hit","object_damage"),
            S("F-P-U24","裂路穿刺",FireSpellRarity.Uncommon,FireSpellGroup.Universal,U,FireDeliveryMode.Movement,AW,Now,Cast,1,3,2,0,3,FireTargetKind.EmptyCell,FireSelectionShape.Path,3,false,true,new[]{R(FireRuleKind.MoveSource,3,scope:FireRuleScope.Destination),R(FireRuleKind.WeaponDamage,6,scope:FireRuleScope.PathAdjacentEnemies,objects:FireDestructibleMask.None)},"path","hit"),
            S("F-P-U25","震楔落步",FireSpellRarity.Rare,FireSpellGroup.Universal,U,FireDeliveryMode.Movement,NW,Now,Cast,2,4,3,0,2,FireTargetKind.EmptyCell,FireSelectionShape.Path,2,false,true,new[]{R(FireRuleKind.MoveSource,2,scope:FireRuleScope.Destination),R(FireRuleKind.BreakBarrier),R(FireRuleKind.Damage,8,scope:FireRuleScope.OrthogonalNeighbors,allies:true),R(FireRuleKind.PushAllUnits,1,scope:FireRuleScope.OrthogonalNeighbors,allies:true)},"path","fire_cross_blast","object_damage"),
            S("F-P-U26","碎障回身",FireSpellRarity.Common,FireSpellGroup.Universal,U,FireDeliveryMode.SelfStance,NW,FireTriggerWindow.AfterNextObjectDestruction,FireConsumptionRule.OnTrigger,1,3,2,0,0,FireTargetKind.Self,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.OfferBreachMove,3,timing:FireRuleTiming.OnTrigger)},"path","object_damage"),
            S("F-P-U27","缓冲护幕",FireSpellRarity.Common,FireSpellGroup.Universal,U,FireDeliveryMode.SelfStance,NW,Now,Cast,1,4,2,0,0,FireTargetKind.Self,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.RestoreShield,8,scope:FireRuleScope.Source),R(FireRuleKind.ReduceForcedMove,1,scope:FireRuleScope.Source)},"shield_restore"),
            S("F-P-U28","断势调息",FireSpellRarity.Common,FireSpellGroup.Universal,U,FireDeliveryMode.SelfStance,NW,Now,Cast,1,2,2,0,0,FireTargetKind.Self,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.ClearBoundOrSlow,scope:FireRuleScope.Source)},"shield_restore"),

            // R01-R10: direct ranged casting.
            S("F-P-R01","火弹",FireSpellRarity.Common,FireSpellGroup.Ranged,X,FireDeliveryMode.DetachedProjection,NW,Now,Cast,1,2,0,0,3,FireTargetKind.Enemy,FireSelectionShape.Single,1,true,false,new[]{R(FireRuleKind.Damage,12)},"fire_projectile","hit"),
            S("F-P-R02","火矢",FireSpellRarity.Common,FireSpellGroup.Ranged,X,FireDeliveryMode.DetachedProjection,NW,Now,Cast,1,2,0,0,5,FireTargetKind.Enemy,FireSelectionShape.Single,1,true,false,new[]{R(FireRuleKind.Damage,8)},"fire_projectile","hit"),
            S("F-P-R03","烙印",FireSpellRarity.Common,FireSpellGroup.Ranged,X,FireDeliveryMode.DetachedProjection,NW,Now,Cast,1,3,0,0,3,FireTargetKind.Enemy,FireSelectionShape.Single,1,true,false,new[]{R(FireRuleKind.ApplyBurning,8,1)},"fire_projectile","burning"),
            S("F-P-R04","火种",FireSpellRarity.Common,FireSpellGroup.Ranged,X,FireDeliveryMode.DetachedProjection,NW,Now,Cast,1,2,1,0,4,FireTargetKind.Enemy,FireSelectionShape.Single,1,true,false,new[]{R(FireRuleKind.ApplyBurning,8,2)},"fire_projectile","burning"),
            S("F-P-R05","余烬火弹",FireSpellRarity.Uncommon,FireSpellGroup.Ranged,X,FireDeliveryMode.DetachedProjection,NW,Now,Cast,2,4,1,0,4,FireTargetKind.Enemy,FireSelectionShape.Single,1,true,false,new[]{R(FireRuleKind.Damage,16),R(FireRuleKind.ExtendBurning,duration:2,condition:FireCondition.TargetBurning)},"fire_projectile","burning"),
            S("F-P-R06","焰线",FireSpellRarity.Uncommon,FireSpellGroup.Ranged,X,FireDeliveryMode.DetachedProjection,NW,Now,Cast,2,4,2,0,4,FireTargetKind.Unit,FireSelectionShape.Line,4,true,true,new[]{R(FireRuleKind.Damage,8,scope:FireRuleScope.Selection,allies:true)},"fire_projectile","fire_cross_blast","hit"),
            S("F-P-R07","火焰喷射",FireSpellRarity.Common,FireSpellGroup.Ranged,X,FireDeliveryMode.DetachedProjection,NW,Now,Cast,1,3,0,0,3,FireTargetKind.Unit,FireSelectionShape.Cone,3,true,true,new[]{R(FireRuleKind.BreakBarrier),R(FireRuleKind.Damage,8,scope:FireRuleScope.Selection,allies:true)},"fire_spray","hit"),
            S("F-P-R08","点燃喷射",FireSpellRarity.Uncommon,FireSpellGroup.Ranged,X,FireDeliveryMode.DetachedProjection,NW,Now,Cast,2,4,2,0,3,FireTargetKind.Unit,FireSelectionShape.Cone,3,true,true,new[]{R(FireRuleKind.ApplyBurning,8,1,FireRuleScope.EnemySelection)},"fire_spray","burning"),
            S("F-P-R09","焰击术",FireSpellRarity.Rare,FireSpellGroup.Ranged,X,FireDeliveryMode.DetachedProjection,NW,Now,Cast,2,3,1,4,3,FireTargetKind.Enemy,FireSelectionShape.Single,1,true,false,new[]{R(FireRuleKind.Damage,20)},"fire_projectile","heavy_hit"),
            S("F-P-R10","熔势射流",FireSpellRarity.Uncommon,FireSpellGroup.Ranged,X,FireDeliveryMode.DetachedProjection,NW,Now,Cast,2,5,3,4,3,FireTargetKind.Enemy,FireSelectionShape.Single,1,true,false,new[]{R(FireRuleKind.Damage,12),R(FireRuleKind.ApplyBreakStance)},"fire_projectile","armor_break"),

            // R11-R15: fireground control.
            S("F-P-R11","火带",FireSpellRarity.Uncommon,FireSpellGroup.Ranged,X,FireDeliveryMode.FiregroundManipulation,NW,Now,Cast,2,5,2,0,4,FireTargetKind.EmptyCell,FireSelectionShape.ContinuousLine,3,true,true,new[]{R(FireRuleKind.CreateFireground,8,3,FireRuleScope.Selection,allies:true)},"fire_projectile","fire_burning_ground"),
            S("F-P-R12","火路",FireSpellRarity.Common,FireSpellGroup.Ranged,X,FireDeliveryMode.FiregroundManipulation,NW,Now,Cast,1,3,1,0,4,FireTargetKind.EmptyCell,FireSelectionShape.Line,4,false,true,new[]{R(FireRuleKind.CreateFireground,8,2,FireRuleScope.Selection,allies:true)},"path","fire_burning_ground"),
            S("F-P-R13","灼域火钉",FireSpellRarity.Common,FireSpellGroup.Ranged,X,FireDeliveryMode.FiregroundManipulation,NW,Now,Cast,1,5,3,0,3,FireTargetKind.EmptyCell,FireSelectionShape.Single,1,true,false,new[]{R(FireRuleKind.CreateFireground,8,4)},"fire_projectile","fire_burning_ground"),
            S("F-P-R14","炽焰墙",FireSpellRarity.Rare,FireSpellGroup.Ranged,X,FireDeliveryMode.FiregroundManipulation,NW,Now,Cast,3,6,4,4,5,FireTargetKind.EmptyCell,FireSelectionShape.ContinuousLine,5,true,true,new[]{R(FireRuleKind.CreateFireground,8,4,FireRuleScope.Selection,allies:true)},"fire_cross_blast","fire_burning_ground"),
            S("F-P-R15","熔火领域",FireSpellRarity.Uncommon,FireSpellGroup.Ranged,X,FireDeliveryMode.FiregroundManipulation,NW,Now,Cast,3,5,4,8,4,FireTargetKind.EmptyCell,FireSelectionShape.Square3,1,true,true,new[]{R(FireRuleKind.CreateFireground,8,3,FireRuleScope.Selection,allies:true)},"fire_cross_blast","fire_burning_ground"),

            // R16-R20: detonation, breach and ranged finisher.
            S("F-P-R16","引爆",FireSpellRarity.Rare,FireSpellGroup.Ranged,X,FireDeliveryMode.DetachedProjection,NW,Now,Cast,2,5,2,0,3,FireTargetKind.BurningEnemy,FireSelectionShape.Single,1,true,false,new[]{R(FireRuleKind.Damage,20),R(FireRuleKind.ConsumeBurning,consume:FireSourceConsumption.BurningOnly)},"fire_detonate","hit"),
            S("F-P-R17","地火抽爆",FireSpellRarity.Common,FireSpellGroup.Ranged,X,FireDeliveryMode.FiregroundManipulation,NW,Now,Cast,1,3,1,0,4,FireTargetKind.Unit,FireSelectionShape.Single,1,true,false,new[]{R(FireRuleKind.Damage,12,condition:FireCondition.TargetOnFireground),R(FireRuleKind.ConsumeFireground,condition:FireCondition.TargetOnFireground,consume:FireSourceConsumption.GroundOnly)},"fire_burning_ground","fire_detonate"),
            S("F-P-R18","爆燃横扫",FireSpellRarity.Common,FireSpellGroup.Ranged,X,FireDeliveryMode.DetachedProjection,NW,Now,Cast,2,4,2,0,3,FireTargetKind.Unit,FireSelectionShape.Cone,3,true,true,new[]{R(FireRuleKind.Damage,12,scope:FireRuleScope.Selection,condition:FireCondition.TargetBurning,allies:true),R(FireRuleKind.ConsumeBurning,scope:FireRuleScope.Selection,condition:FireCondition.TargetBurning,allies:true,consume:FireSourceConsumption.BurningOnly)},"fire_spray","fire_detonate"),
            S("F-P-R19","熔障爆点",FireSpellRarity.Rare,FireSpellGroup.Ranged,X,FireDeliveryMode.DetachedProjection,NW,Now,Cast,2,4,2,0,4,FireTargetKind.Hittable,FireSelectionShape.Single,1,true,false,new[]{R(FireRuleKind.BreakBarrier),R(FireRuleKind.Damage,16),R(FireRuleKind.Damage,8,scope:FireRuleScope.OrthogonalNeighbors,allies:true)},"object_damage","fire_cross_blast"),
            S("F-P-R20","焚城界限",FireSpellRarity.Rare,FireSpellGroup.Ranged,X,FireDeliveryMode.DetachedProjection,NW,Now,Cast,3,6,4,8,4,FireTargetKind.Unit,FireSelectionShape.CenterAndOrthogonal,1,true,true,new[]{R(FireRuleKind.Damage,20,scope:FireRuleScope.Selection,allies:true),R(FireRuleKind.CreateFireground,8,3,FireRuleScope.Selection,allies:true)},"fire_cross_blast","fire_detonate","fire_burning_ground"),
            S("F-P-R21","裂痕投射",FireSpellRarity.Common,FireSpellGroup.Ranged,X,FireDeliveryMode.DetachedProjection,NW,Now,Cast,1,4,1,0,4,FireTargetKind.Cell,FireSelectionShape.FirstHitLine,4,false,true,new[]{R(FireRuleKind.Damage,6,scope:FireRuleScope.Selection,allies:true),R(FireRuleKind.ApplyFracture,scope:FireRuleScope.Selection),R(FireRuleKind.Push,1,scope:FireRuleScope.Selection)},"fire_projectile","object_damage"),
            S("F-P-R22","楔形震裂",FireSpellRarity.Uncommon,FireSpellGroup.Ranged,X,FireDeliveryMode.DetachedProjection,NW,Now,Cast,2,5,2,0,2,FireTargetKind.Unit,FireSelectionShape.Cone,2,false,true,new[]{R(FireRuleKind.BreakBarrier),R(FireRuleKind.Damage,8,scope:FireRuleScope.Selection,allies:true)},"fire_spray","object_damage"),
            S("F-P-R23","崩面震波",FireSpellRarity.Uncommon,FireSpellGroup.Ranged,X,FireDeliveryMode.DetachedProjection,NW,Now,Cast,2,5,2,0,3,FireTargetKind.Cell,FireSelectionShape.CenterAndOrthogonal,1,true,false,new[]{R(FireRuleKind.Damage,6,scope:FireRuleScope.Selection,allies:true),R(FireRuleKind.PushFromDestroyedObjects,1,scope:FireRuleScope.Selection,allies:true)},"fire_cross_blast","object_damage"),
            S("F-P-R24","回流护持",FireSpellRarity.Common,FireSpellGroup.Ranged,X,FireDeliveryMode.SelfStance,NW,Now,Cast,1,4,2,0,0,FireTargetKind.Self,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.ArmFractureShield,4,scope:FireRuleScope.Source)},"shield_restore"),
            S("F-P-R25","借障折射",FireSpellRarity.Uncommon,FireSpellGroup.Ranged,X,FireDeliveryMode.DetachedProjection,NW,Now,Cast,1,3,1,0,3,FireTargetKind.EmptyCell,FireSelectionShape.ReflectionRay,2,true,true,new[]{R(FireRuleKind.Damage,10,scope:FireRuleScope.ReflectedFirstHit)},"fire_projectile","object_damage"),
            S("F-P-R26","断线爆破",FireSpellRarity.Rare,FireSpellGroup.Ranged,X,FireDeliveryMode.TargetMarking,NW,FireTriggerWindow.CurrentTurnEnd,FireConsumptionRule.OnTrigger,2,5,3,0,4,FireTargetKind.Cell,FireSelectionShape.CenterAndOrthogonal,1,true,false,new[]{R(FireRuleKind.BreakBarrier),R(FireRuleKind.Damage,12,scope:FireRuleScope.Selection,allies:true,timing:FireRuleTiming.OnTrigger)},"fire_cross_blast","object_damage")
        };

        public static FireSpellDefinition Get(string id) => All.FirstOrDefault(spell => string.Equals(spell.Id, id, StringComparison.Ordinal))
            ?? throw new InvalidOperationException("Unknown fire spell: " + id);

        // Explicit semantic reuse of the already normalized v0.1 icon set. This is intentionally not an ID fallback:
        // every v0.2 entry is reviewed against the legacy icon whose action silhouette it reuses.
        public static string LegacyIconIdFor(string id)
        {
            switch (id)
            {
                case "F-P-M01": return "f-p41"; case "F-P-M02": return "f-p41"; case "F-P-M03": return "f-p48";
                case "F-P-M04": return "f-p48"; case "F-P-M05": return "f-p12"; case "F-P-M06": return "f-p31";
                case "F-P-M07": return "f-p39"; case "F-P-M08": return "f-p35"; case "F-P-M09": return "f-p47";
                case "F-P-M10": return "f-p31"; case "F-P-M11": return "f-p42"; case "F-P-M12": return "f-p43";
                case "F-P-M13": return "f-p47"; case "F-P-M14": return "f-p44"; case "F-P-M15": return "f-p47";
                case "F-P-M16": return "f-p12"; case "F-P-M17": return "f-p22"; case "F-P-M18": return "f-p13";
                case "F-P-M19": return "f-p48"; case "F-P-M20": return "f-p30";
                case "F-P-M21": return "f-p48"; case "F-P-M23": return "f-p48"; case "F-P-M24": return "f-p48"; case "F-P-M25": return "f-p34"; case "F-P-M26": return "f-p34";
                case "F-P-M22": return "f-p48";
                case "F-P-U01": return "f-p01"; case "F-P-U02": return "f-p03"; case "F-P-U03": return "f-p31";
                case "F-P-U04": return "f-p33"; case "F-P-U05": return "f-p25"; case "F-P-U06": return "f-p41";
                case "F-P-U07": return "f-p46"; case "F-P-U08": return "f-p43"; case "F-P-U09": return "f-p45";
                case "F-P-U10": return "f-p42"; case "F-P-U11": return "f-p49"; case "F-P-U12": return "f-p43";
                case "F-P-U13": return "f-p49"; case "F-P-U14": return "f-p12"; case "F-P-U15": return "f-p19";
                case "F-P-U16": return "f-p09"; case "F-P-U17": return "f-p15"; case "F-P-U18": return "f-p34";
                case "F-P-U19": return "f-p29"; case "F-P-U20": return "f-p30";
                case "F-P-U21": return "f-p37"; case "F-P-U22": return "f-p37"; case "F-P-U23": return "f-p48"; case "F-P-U24": return "f-p48"; case "F-P-U25": return "f-p34";
                case "F-P-U26": return "f-p34"; case "F-P-U27": return "f-p43"; case "F-P-U28": return "f-p43";
                case "F-P-R01": return "f-p01"; case "F-P-R02": return "f-p05"; case "F-P-R03": return "f-p03";
                case "F-P-R04": return "f-p06"; case "F-P-R05": return "f-p08"; case "F-P-R06": return "f-p07";
                case "F-P-R07": return "f-p10"; case "F-P-R08": return "f-p11"; case "F-P-R09": return "f-p09";
                case "F-P-R10": return "f-p31"; case "F-P-R11": return "f-p02"; case "F-P-R12": return "f-p13";
                case "F-P-R13": return "f-p15"; case "F-P-R14": return "f-p18"; case "F-P-R15": return "f-p21";
                case "F-P-R16": return "f-p04"; case "F-P-R17": return "f-p23"; case "F-P-R18": return "f-p24";
                case "F-P-R19": return "f-p37"; case "F-P-R20": return "f-p20";
                case "F-P-R21": return "f-p37"; case "F-P-R22": return "f-p37"; case "F-P-R23": return "f-p37"; case "F-P-R24": return "f-p37"; case "F-P-R25": return "f-p37"; case "F-P-R26": return "f-p37";
                default: throw new InvalidOperationException("No reviewed legacy icon mapping for fire spell: " + id);
            }
        }

        internal static string IconIdFor(string id)
        {
            bool personalV02 = id != null && (id.StartsWith("F-P-M", StringComparison.Ordinal) ||
                id.StartsWith("F-P-U", StringComparison.Ordinal) || id.StartsWith("F-P-R", StringComparison.Ordinal));
            return personalV02 ? LegacyIconIdFor(id) : (id ?? string.Empty).ToLowerInvariant();
        }

        public static bool IsWeaponCompatible(FireSpellDefinition spell, WeaponDefinition weapon)
        {
            if (spell == null) return false;
            if (spell.WeaponRequirement == FireWeaponRequirement.None) return true;
            if (weapon == null) return false;
            bool melee = weapon.Range <= 1;
            return spell.WeaponRequirement == FireWeaponRequirement.AnyWeapon ||
                spell.WeaponRequirement == FireWeaponRequirement.MeleeWeapon && melee ||
                spell.WeaponRequirement == FireWeaponRequirement.RangedWeapon && !melee;
        }
    }
}
