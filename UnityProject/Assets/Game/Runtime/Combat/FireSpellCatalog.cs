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
    public enum FireTriggerWindow { Immediate, NextLegalWeaponAttack, CurrentAction, UntilNextAction, FirstAdjacentAttack, FirstMarkedTargetMove, FirstEnemyEntry, AfterNextWeaponAttack }
    public enum FireConsumptionRule { OnCast, OnLegalAttackCommitted, OnTrigger, OnWindowEnd }
    public enum FireRuleTiming { OnCast, OnTrigger }
    public enum FireTargetKind { Self, Enemy, AllyOrSelf, Unit, EmptyCell, BurningUnit, BurningCell, Destructible, AdjacentEnemy, AdjacentBurningEnemy, BurningOrArmorBrokenEnemy }
    public enum FireSelectionShape { Single, Line, ContinuousLine, Cone, Cross, OrthogonalRing, CenterAndOrthogonal, Square3, AroundUnit, Path }
    public enum FireRuleKind
    {
        Damage, WeaponDamage, ApplyBurning, ExtendBurning, SetBurningDuration, CreateFireground, ExtendFireground,
        ConsumeBurning, ConsumeFireground, DamageDurability, DestroyLightCover, ApplyArmorBreak, RestoreShield,
        ClearStatus, RestoreMovement, AddMovement, MoveSource, SwapUnits, Push, RestoreMana, LoseHealth,
        RepairWeapon, ReduceIncomingDamage, MoveAfterAttack, ExtendTriggerToAlly, SpendActionPoints, SpendMana,
        ArmTrigger, ConsumeTrigger, OverloadDevice
    }
    public enum FireRuleScope { Primary, Selection, EnemySelection, AllySelection, OrthogonalNeighbors, PathAdjacentEnemies, Source, SourceCell, Destination, CoveredCells }
    public enum FireCondition
    {
        Always, TargetBurning, TargetOnFireground, TargetBurningAndOnFireground, TargetArmorBroken,
        TargetBurningOrArmorBroken, SourceBurning, SourceNotBurning, SourceBound, SourceSlowed,
        SourceNotArmorBroken, LightCoverDestroyed, DurabilityDepleted
    }
    public enum FireSourceConsumption { None, BurningFirstThenGround, BurningOnly, GroundOnly, BurningAndGround }
    [Flags]
    public enum FireDestructibleMask { None = 0, LightCover = 1, HeavyCover = 2, Device = 4, All = LightCover | HeavyCover | Device }

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
        {
            Id = id; DisplayName = displayName; Rarity = rarity; Group = group; CombatAffinity = combatAffinity;
            DeliveryMode = deliveryMode; WeaponRequirement = weaponRequirement; TriggerWindow = triggerWindow;
            ConsumptionRule = consumptionRule; ActionPointCost = ap; ManaCost = mana; Cooldown = cooldown;
            InitiativeDelay = delay; Range = range; TargetKind = targetKind; Shape = shape; ShapeLength = shapeLength;
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
    }

    public static class FireSpellCatalog
    {
        public const string Version = "fire-personal-spells-v0.2";

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
            S("F-P-M03","焦痕跃进",FireSpellRarity.Uncommon,FireSpellGroup.Melee,M,FireDeliveryMode.ContactConduction,MW,Now,Cast,2,4,2,0,3,FireTargetKind.EmptyCell,FireSelectionShape.Path,3,false,true,new[]{R(FireRuleKind.CreateFireground,8,4,FireRuleScope.SourceCell),R(FireRuleKind.MoveSource,3,scope:FireRuleScope.Destination),R(FireRuleKind.WeaponDamage,8,scope:FireRuleScope.PathAdjacentEnemies)},"path","fire_projectile","fire_burning_ground"),
            S("F-P-M04","热流折步",FireSpellRarity.Common,FireSpellGroup.Melee,M,FireDeliveryMode.Movement,MW,Now,Cast,1,2,1,0,1,FireTargetKind.AdjacentEnemy,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.SwapUnits)},"path"),
            S("F-P-M05","爆燃追步",FireSpellRarity.Uncommon,FireSpellGroup.Melee,M,FireDeliveryMode.TargetMarking,MW,FireTriggerWindow.FirstMarkedTargetMove,FireConsumptionRule.OnTrigger,1,3,2,0,1,FireTargetKind.AdjacentEnemy,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.MoveSource,1,timing:FireRuleTiming.OnTrigger)},"path"),

            // M06-M10: contact armor breaking and melee attacks.
            S("F-P-M06","熔甲贯击",FireSpellRarity.Common,FireSpellGroup.Melee,M,FireDeliveryMode.WeaponAttachment,MW,FireTriggerWindow.NextLegalWeaponAttack,FireConsumptionRule.OnLegalAttackCommitted,1,3,1,0,0,FireTargetKind.Self,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.ApplyArmorBreak,4,1,timing:FireRuleTiming.OnTrigger)},"armor_break"),
            S("F-P-M07","炉心穿刺",FireSpellRarity.Rare,FireSpellGroup.Melee,M,FireDeliveryMode.ContactConduction,MW,Now,Cast,2,5,3,4,1,FireTargetKind.AdjacentEnemy,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.WeaponDamage,20),R(FireRuleKind.Damage,8),R(FireRuleKind.ApplyArmorBreak,8,1)},"heavy_hit","armor_break"),
            S("F-P-M08","炉压横扫",FireSpellRarity.Uncommon,FireSpellGroup.Melee,M,FireDeliveryMode.ContactConduction,MW,Now,Cast,2,4,2,0,3,FireTargetKind.Unit,FireSelectionShape.Cone,3,false,true,new[]{R(FireRuleKind.WeaponDamage,12,scope:FireRuleScope.Selection,allies:true),R(FireRuleKind.Damage,4,scope:FireRuleScope.Selection,allies:true)},"fire_spray","hit"),
            S("F-P-M09","热震重击",FireSpellRarity.Uncommon,FireSpellGroup.Melee,M,FireDeliveryMode.ContactConduction,MW,Now,Cast,2,4,2,4,1,FireTargetKind.AdjacentEnemy,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.WeaponDamage,16),R(FireRuleKind.Damage,8),R(FireRuleKind.Push,1)},"heavy_hit","path"),
            S("F-P-M10","熔隙刺击",FireSpellRarity.Common,FireSpellGroup.Melee,M,FireDeliveryMode.ContactConduction,MW,Now,Cast,1,3,1,0,1,FireTargetKind.AdjacentEnemy,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.WeaponDamage,12,condition:FireCondition.Always,alternate:20)},"hit","armor_break"),

            // M11-M15: ranged resistance, block, counter and disengage.
            S("F-P-M11","热障架势",FireSpellRarity.Common,FireSpellGroup.Melee,M,FireDeliveryMode.SelfStance,MW,FireTriggerWindow.UntilNextAction,FireConsumptionRule.OnWindowEnd,1,3,2,0,0,FireTargetKind.Self,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.ReduceIncomingDamage,8,timing:FireRuleTiming.OnTrigger)},"shield_restore"),
            S("F-P-M12","余烬格挡",FireSpellRarity.Uncommon,FireSpellGroup.Melee,M,FireDeliveryMode.SelfStance,MW,FireTriggerWindow.UntilNextAction,FireConsumptionRule.OnWindowEnd,1,3,2,0,0,FireTargetKind.Self,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.RestoreShield,12,scope:FireRuleScope.Source),R(FireRuleKind.ReduceIncomingDamage,0,timing:FireRuleTiming.OnTrigger)},"shield_restore"),
            S("F-P-M13","炉心反击",FireSpellRarity.Rare,FireSpellGroup.Melee,M,FireDeliveryMode.SelfStance,MW,FireTriggerWindow.FirstAdjacentAttack,FireConsumptionRule.OnTrigger,1,4,3,0,0,FireTargetKind.Self,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.WeaponDamage,12,timing:FireRuleTiming.OnTrigger),R(FireRuleKind.Damage,4,timing:FireRuleTiming.OnTrigger)},"heavy_hit","fire_projectile"),
            S("F-P-M14","灼缚解离",FireSpellRarity.Uncommon,FireSpellGroup.Melee,M,FireDeliveryMode.ContactConduction,MW,Now,Cast,1,3,2,0,1,FireTargetKind.AdjacentEnemy,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.ClearStatus,scope:FireRuleScope.Source,condition:FireCondition.SourceBound,status:StatusType.Bound),R(FireRuleKind.Damage,8)},"cleanse","hit"),
            S("F-P-M15","焰压退击",FireSpellRarity.Common,FireSpellGroup.Melee,M,FireDeliveryMode.ContactConduction,MW,Now,Cast,1,3,1,0,1,FireTargetKind.AdjacentEnemy,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.Damage,8),R(FireRuleKind.Push,1)},"fire_spray","path"),

            // M16-M20: burning conversion and finishers.
            S("F-P-M16","燃势收割",FireSpellRarity.Common,FireSpellGroup.Melee,M,FireDeliveryMode.ContactConduction,MW,Now,Cast,1,3,1,0,1,FireTargetKind.AdjacentBurningEnemy,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.WeaponDamage,16),R(FireRuleKind.Damage,8)},"heavy_hit","burning"),
            S("F-P-M17","焦甲吸热",FireSpellRarity.Uncommon,FireSpellGroup.Melee,M,FireDeliveryMode.ContactConduction,MW,Now,Cast,1,3,2,0,1,FireTargetKind.AdjacentBurningEnemy,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.Damage,12),R(FireRuleKind.ConsumeBurning,consume:FireSourceConsumption.BurningOnly),R(FireRuleKind.RestoreShield,12,scope:FireRuleScope.Source)},"fire_detonate","shield_restore"),
            S("F-P-M18","火场踏行",FireSpellRarity.Common,FireSpellGroup.Melee,M,FireDeliveryMode.FiregroundManipulation,MW,Now,Cast,1,2,1,0,3,FireTargetKind.BurningCell,FireSelectionShape.Path,3,false,true,new[]{R(FireRuleKind.MoveSource,3,scope:FireRuleScope.Destination)},"path","fire_burning_ground"),
            S("F-P-M19","炉心超限",FireSpellRarity.Rare,FireSpellGroup.Melee,M,FireDeliveryMode.ContactConduction,MW,Now,Cast,2,5,4,8,3,FireTargetKind.Enemy,FireSelectionShape.Path,3,false,true,new[]{R(FireRuleKind.MoveSource,3,scope:FireRuleScope.Destination),R(FireRuleKind.WeaponDamage,24),R(FireRuleKind.Damage,12),R(FireRuleKind.LoseHealth,12,scope:FireRuleScope.Source)},"path","heavy_hit","fire_detonate"),
            S("F-P-M20","终炉断击",FireSpellRarity.Rare,FireSpellGroup.Melee,M,FireDeliveryMode.ContactConduction,MW,Now,Cast,3,5,4,8,1,FireTargetKind.BurningOrArmorBrokenEnemy,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.WeaponDamage,28),R(FireRuleKind.Damage,12),R(FireRuleKind.ConsumeBurning,condition:FireCondition.TargetBurning,consume:FireSourceConsumption.BurningOnly)},"heavy_hit","fire_detonate"),

            // U01-U05: one legal weapon attack attachments.
            S("F-P-U01","武器热载",FireSpellRarity.Common,FireSpellGroup.Universal,U,FireDeliveryMode.WeaponAttachment,AW,FireTriggerWindow.NextLegalWeaponAttack,FireConsumptionRule.OnLegalAttackCommitted,1,2,0,0,0,FireTargetKind.Self,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.Damage,8,timing:FireRuleTiming.OnTrigger)},"fire_projectile"),
            S("F-P-U02","烙痕传递",FireSpellRarity.Common,FireSpellGroup.Universal,U,FireDeliveryMode.WeaponAttachment,AW,FireTriggerWindow.NextLegalWeaponAttack,FireConsumptionRule.OnLegalAttackCommitted,1,3,1,0,0,FireTargetKind.Self,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.ApplyBurning,8,1,timing:FireRuleTiming.OnTrigger)},"burning"),
            S("F-P-U03","灼蚀校准",FireSpellRarity.Uncommon,FireSpellGroup.Universal,U,FireDeliveryMode.WeaponAttachment,AW,FireTriggerWindow.NextLegalWeaponAttack,FireConsumptionRule.OnLegalAttackCommitted,1,4,2,0,0,FireTargetKind.Self,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.ApplyArmorBreak,4,1,timing:FireRuleTiming.OnTrigger)},"armor_break"),
            S("F-P-U04","熔障校准",FireSpellRarity.Common,FireSpellGroup.Universal,U,FireDeliveryMode.WeaponAttachment,AW,FireTriggerWindow.NextLegalWeaponAttack,FireConsumptionRule.OnLegalAttackCommitted,1,2,0,0,0,FireTargetKind.Self,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.DamageDurability,16,objects:FireDestructibleMask.All,timing:FireRuleTiming.OnTrigger)},"object_damage"),
            S("F-P-U05","爆燃弹芯",FireSpellRarity.Uncommon,FireSpellGroup.Universal,U,FireDeliveryMode.WeaponAttachment,AW,FireTriggerWindow.NextLegalWeaponAttack,FireConsumptionRule.OnLegalAttackCommitted,2,4,2,0,0,FireTargetKind.Self,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.Damage,4,scope:FireRuleScope.OrthogonalNeighbors,allies:true,timing:FireRuleTiming.OnTrigger)},"fire_cross_blast"),

            // U06-U10: action, facing and defense.
            S("F-P-U06","热压续步",FireSpellRarity.Common,FireSpellGroup.Universal,U,FireDeliveryMode.WeaponAttachment,AW,FireTriggerWindow.AfterNextWeaponAttack,FireConsumptionRule.OnTrigger,1,2,1,0,0,FireTargetKind.Self,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.MoveAfterAttack,1,timing:FireRuleTiming.OnTrigger)},"path"),
            S("F-P-U07","炉温护持",FireSpellRarity.Common,FireSpellGroup.Universal,U,FireDeliveryMode.DetachedProjection,NW,Now,Cast,1,3,2,0,3,FireTargetKind.AllyOrSelf,FireSelectionShape.Single,1,true,false,new[]{R(FireRuleKind.RestoreShield,12)},"fire_projectile","shield_restore"),
            S("F-P-U08","余烬护甲",FireSpellRarity.Uncommon,FireSpellGroup.Universal,U,FireDeliveryMode.SelfStance,NW,Now,Cast,2,4,2,0,0,FireTargetKind.Self,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.ClearStatus,scope:FireRuleScope.Source,condition:FireCondition.SourceBurning,status:StatusType.Burning),R(FireRuleKind.RestoreShield,20,scope:FireRuleScope.Source,condition:FireCondition.SourceBurning),R(FireRuleKind.RestoreShield,12,scope:FireRuleScope.Source,condition:FireCondition.SourceNotBurning)},"cleanse","shield_restore"),
            S("F-P-U09","温血苏醒",FireSpellRarity.Common,FireSpellGroup.Universal,U,FireDeliveryMode.BodyEnhancement,NW,Now,Cast,1,2,1,0,0,FireTargetKind.Self,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.ClearStatus,scope:FireRuleScope.Source,condition:FireCondition.SourceSlowed,status:StatusType.Slow),R(FireRuleKind.RestoreMovement,3,scope:FireRuleScope.Source)},"cleanse","path"),
            S("F-P-U10","热障偏流",FireSpellRarity.Uncommon,FireSpellGroup.Universal,U,FireDeliveryMode.SelfStance,NW,FireTriggerWindow.UntilNextAction,FireConsumptionRule.OnWindowEnd,1,3,2,0,0,FireTargetKind.Self,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.ReduceIncomingDamage,4,timing:FireRuleTiming.OnTrigger)},"shield_restore"),

            // U11-U15: burning resource and attack conversion.
            S("F-P-U11","热源回收",FireSpellRarity.Common,FireSpellGroup.Universal,U,FireDeliveryMode.FiregroundManipulation,NW,Now,Cast,1,2,2,0,3,FireTargetKind.BurningCell,FireSelectionShape.Single,1,true,false,new[]{R(FireRuleKind.ConsumeFireground,consume:FireSourceConsumption.GroundOnly),R(FireRuleKind.RestoreMana,2,scope:FireRuleScope.Source)},"fire_burning_ground","mana_restore"),
            S("F-P-U12","余热转护",FireSpellRarity.Uncommon,FireSpellGroup.Universal,U,FireDeliveryMode.WeaponAttachment,AW,FireTriggerWindow.NextLegalWeaponAttack,FireConsumptionRule.OnLegalAttackCommitted,1,3,2,0,0,FireTargetKind.Self,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.ConsumeBurning,condition:FireCondition.TargetBurning,consume:FireSourceConsumption.BurningOnly,timing:FireRuleTiming.OnTrigger),R(FireRuleKind.RestoreShield,12,scope:FireRuleScope.Source,condition:FireCondition.TargetBurning,timing:FireRuleTiming.OnTrigger)},"fire_detonate","shield_restore"),
            S("F-P-U13","燃势回收",FireSpellRarity.Uncommon,FireSpellGroup.Universal,U,FireDeliveryMode.WeaponAttachment,AW,FireTriggerWindow.NextLegalWeaponAttack,FireConsumptionRule.OnLegalAttackCommitted,1,3,2,0,0,FireTargetKind.Self,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.RestoreMana,2,scope:FireRuleScope.Source,condition:FireCondition.TargetBurning,timing:FireRuleTiming.OnTrigger)},"mana_restore","burning"),
            S("F-P-U14","追火校准",FireSpellRarity.Common,FireSpellGroup.Universal,U,FireDeliveryMode.WeaponAttachment,AW,FireTriggerWindow.NextLegalWeaponAttack,FireConsumptionRule.OnLegalAttackCommitted,1,3,1,0,0,FireTargetKind.Self,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.Damage,8,condition:FireCondition.TargetBurning,timing:FireRuleTiming.OnTrigger)},"fire_projectile","burning"),
            S("F-P-U15","灰烬复燃",FireSpellRarity.Common,FireSpellGroup.Universal,U,FireDeliveryMode.WeaponAttachment,AW,FireTriggerWindow.NextLegalWeaponAttack,FireConsumptionRule.OnLegalAttackCommitted,1,2,1,0,0,FireTargetKind.Self,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.SetBurningDuration,duration:2,condition:FireCondition.TargetBurning,timing:FireRuleTiming.OnTrigger)},"burning"),

            // U16-U20: heavy attack, reaction, maintenance, cooperation and finisher.
            S("F-P-U16","炉压蓄势",FireSpellRarity.Uncommon,FireSpellGroup.Universal,U,FireDeliveryMode.WeaponAttachment,AW,FireTriggerWindow.NextLegalWeaponAttack,FireConsumptionRule.OnLegalAttackCommitted,1,4,2,4,0,FireTargetKind.Self,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.WeaponDamage,12,timing:FireRuleTiming.OnTrigger)},"heavy_hit"),
            S("F-P-U17","焦土警戒",FireSpellRarity.Rare,FireSpellGroup.Universal,U,FireDeliveryMode.TargetMarking,AW,FireTriggerWindow.FirstEnemyEntry,FireConsumptionRule.OnTrigger,2,4,3,0,3,FireTargetKind.EmptyCell,FireSelectionShape.Single,1,true,false,new[]{R(FireRuleKind.WeaponDamage,8,timing:FireRuleTiming.OnTrigger),R(FireRuleKind.CreateFireground,8,4,timing:FireRuleTiming.OnTrigger)},"fire_projectile","fire_burning_ground"),
            S("F-P-U18","兵装退火",FireSpellRarity.Uncommon,FireSpellGroup.Universal,U,FireDeliveryMode.WeaponAttachment,AW,Now,Cast,1,3,2,0,0,FireTargetKind.Self,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.ClearStatus,scope:FireRuleScope.Source,condition:FireCondition.TargetArmorBroken,status:StatusType.ArmorBreak),R(FireRuleKind.RepairWeapon,8,scope:FireRuleScope.Source,condition:FireCondition.SourceNotArmorBroken)},"cleanse","object_damage"),
            S("F-P-U19","火线协同",FireSpellRarity.Rare,FireSpellGroup.Universal,U,FireDeliveryMode.TargetMarking,AW,FireTriggerWindow.AfterNextWeaponAttack,FireConsumptionRule.OnTrigger,2,4,2,0,4,FireTargetKind.BurningUnit,FireSelectionShape.Single,1,true,false,new[]{R(FireRuleKind.ExtendTriggerToAlly,8,timing:FireRuleTiming.OnTrigger)},"burning","fire_projectile"),
            S("F-P-U20","炉心共振",FireSpellRarity.Rare,FireSpellGroup.Universal,U,FireDeliveryMode.WeaponAttachment,AW,FireTriggerWindow.NextLegalWeaponAttack,FireConsumptionRule.OnLegalAttackCommitted,3,5,4,8,0,FireTargetKind.Self,FireSelectionShape.Single,1,false,false,new[]{R(FireRuleKind.Damage,20,condition:FireCondition.TargetBurningOrArmorBroken,alternate:28,timing:FireRuleTiming.OnTrigger),R(FireRuleKind.ConsumeBurning,condition:FireCondition.TargetBurningAndOnFireground,consume:FireSourceConsumption.BurningOnly,timing:FireRuleTiming.OnTrigger)},"heavy_hit","fire_detonate"),

            // R01-R10: direct ranged casting.
            S("F-P-R01","火弹",FireSpellRarity.Common,FireSpellGroup.Ranged,X,FireDeliveryMode.DetachedProjection,NW,Now,Cast,1,2,0,0,3,FireTargetKind.Enemy,FireSelectionShape.Single,1,true,false,new[]{R(FireRuleKind.Damage,12)},"fire_projectile","hit"),
            S("F-P-R02","火矢",FireSpellRarity.Common,FireSpellGroup.Ranged,X,FireDeliveryMode.DetachedProjection,NW,Now,Cast,1,2,0,0,5,FireTargetKind.Enemy,FireSelectionShape.Single,1,true,false,new[]{R(FireRuleKind.Damage,8)},"fire_projectile","hit"),
            S("F-P-R03","烙印",FireSpellRarity.Common,FireSpellGroup.Ranged,X,FireDeliveryMode.DetachedProjection,NW,Now,Cast,1,3,0,0,3,FireTargetKind.Enemy,FireSelectionShape.Single,1,true,false,new[]{R(FireRuleKind.Damage,4),R(FireRuleKind.ApplyBurning,8,1)},"fire_projectile","burning"),
            S("F-P-R04","火种",FireSpellRarity.Uncommon,FireSpellGroup.Ranged,X,FireDeliveryMode.DetachedProjection,NW,Now,Cast,1,2,1,0,4,FireTargetKind.Enemy,FireSelectionShape.Single,1,true,false,new[]{R(FireRuleKind.ApplyBurning,8,2)},"fire_projectile","burning"),
            S("F-P-R05","余烬火弹",FireSpellRarity.Uncommon,FireSpellGroup.Ranged,X,FireDeliveryMode.DetachedProjection,NW,Now,Cast,2,4,1,0,4,FireTargetKind.Enemy,FireSelectionShape.Single,1,true,false,new[]{R(FireRuleKind.Damage,16),R(FireRuleKind.ExtendBurning,duration:2,condition:FireCondition.TargetBurning)},"fire_projectile","burning"),
            S("F-P-R06","焰线",FireSpellRarity.Uncommon,FireSpellGroup.Ranged,X,FireDeliveryMode.DetachedProjection,NW,Now,Cast,2,4,2,0,4,FireTargetKind.Unit,FireSelectionShape.Line,4,true,true,new[]{R(FireRuleKind.Damage,8,scope:FireRuleScope.Selection,allies:true)},"fire_projectile","fire_cross_blast","hit"),
            S("F-P-R07","火焰喷射",FireSpellRarity.Common,FireSpellGroup.Ranged,X,FireDeliveryMode.DetachedProjection,NW,Now,Cast,1,3,0,0,3,FireTargetKind.Unit,FireSelectionShape.Cone,3,true,true,new[]{R(FireRuleKind.Damage,8,scope:FireRuleScope.Selection,allies:true)},"fire_spray","hit"),
            S("F-P-R08","点燃喷射",FireSpellRarity.Rare,FireSpellGroup.Ranged,X,FireDeliveryMode.DetachedProjection,NW,Now,Cast,2,4,2,0,3,FireTargetKind.Unit,FireSelectionShape.Cone,3,true,true,new[]{R(FireRuleKind.Damage,4,scope:FireRuleScope.Selection,allies:true),R(FireRuleKind.ApplyBurning,8,1,FireRuleScope.EnemySelection)},"fire_spray","burning"),
            S("F-P-R09","焰击术",FireSpellRarity.Uncommon,FireSpellGroup.Ranged,X,FireDeliveryMode.DetachedProjection,NW,Now,Cast,2,3,1,4,3,FireTargetKind.Enemy,FireSelectionShape.Single,1,true,false,new[]{R(FireRuleKind.Damage,20)},"fire_projectile","heavy_hit"),
            S("F-P-R10","熔甲射流",FireSpellRarity.Rare,FireSpellGroup.Ranged,X,FireDeliveryMode.DetachedProjection,NW,Now,Cast,2,5,3,4,3,FireTargetKind.Enemy,FireSelectionShape.Single,1,true,false,new[]{R(FireRuleKind.Damage,12),R(FireRuleKind.ApplyArmorBreak,4,1)},"fire_projectile","armor_break"),

            // R11-R15: fireground control.
            S("F-P-R11","火带",FireSpellRarity.Uncommon,FireSpellGroup.Ranged,X,FireDeliveryMode.FiregroundManipulation,NW,Now,Cast,2,5,2,0,4,FireTargetKind.EmptyCell,FireSelectionShape.ContinuousLine,3,true,false,new[]{R(FireRuleKind.CreateFireground,8,6,FireRuleScope.Selection,allies:true)},"fire_projectile","fire_burning_ground"),
            S("F-P-R12","火路",FireSpellRarity.Common,FireSpellGroup.Ranged,X,FireDeliveryMode.FiregroundManipulation,NW,Now,Cast,1,3,1,0,4,FireTargetKind.EmptyCell,FireSelectionShape.Line,4,false,true,new[]{R(FireRuleKind.CreateFireground,8,4,FireRuleScope.Selection,allies:true)},"path","fire_burning_ground"),
            S("F-P-R13","灼域火钉",FireSpellRarity.Uncommon,FireSpellGroup.Ranged,X,FireDeliveryMode.FiregroundManipulation,NW,Now,Cast,1,3,1,0,3,FireTargetKind.EmptyCell,FireSelectionShape.Single,1,true,false,new[]{R(FireRuleKind.CreateFireground,12,8)},"fire_projectile","fire_burning_ground"),
            S("F-P-R14","炽焰墙",FireSpellRarity.Rare,FireSpellGroup.Ranged,X,FireDeliveryMode.FiregroundManipulation,NW,Now,Cast,3,5,3,4,5,FireTargetKind.EmptyCell,FireSelectionShape.ContinuousLine,5,true,false,new[]{R(FireRuleKind.CreateFireground,8,8,FireRuleScope.Selection,allies:true)},"fire_cross_blast","fire_burning_ground"),
            S("F-P-R15","熔火领域",FireSpellRarity.Rare,FireSpellGroup.Ranged,X,FireDeliveryMode.FiregroundManipulation,NW,Now,Cast,3,5,4,8,4,FireTargetKind.EmptyCell,FireSelectionShape.Square3,1,true,true,new[]{R(FireRuleKind.CreateFireground,8,6,FireRuleScope.CoveredCells,allies:true)},"fire_cross_blast","fire_burning_ground"),

            // R16-R20: detonation, breach and ranged finisher.
            S("F-P-R16","引爆",FireSpellRarity.Rare,FireSpellGroup.Ranged,X,FireDeliveryMode.DetachedProjection,NW,Now,Cast,2,5,2,0,3,FireTargetKind.BurningUnit,FireSelectionShape.Single,1,true,false,new[]{R(FireRuleKind.Damage,20),R(FireRuleKind.ConsumeBurning,consume:FireSourceConsumption.BurningOnly)},"fire_detonate","hit"),
            S("F-P-R17","地火抽爆",FireSpellRarity.Common,FireSpellGroup.Ranged,X,FireDeliveryMode.FiregroundManipulation,NW,Now,Cast,1,3,1,0,4,FireTargetKind.Unit,FireSelectionShape.Single,1,true,false,new[]{R(FireRuleKind.Damage,12,condition:FireCondition.TargetOnFireground),R(FireRuleKind.ConsumeFireground,condition:FireCondition.TargetOnFireground,consume:FireSourceConsumption.GroundOnly)},"fire_burning_ground","fire_detonate"),
            S("F-P-R18","爆燃横扫",FireSpellRarity.Uncommon,FireSpellGroup.Ranged,X,FireDeliveryMode.DetachedProjection,NW,Now,Cast,2,4,2,0,3,FireTargetKind.Unit,FireSelectionShape.Cone,3,true,true,new[]{R(FireRuleKind.Damage,12,scope:FireRuleScope.Selection,condition:FireCondition.TargetBurning,allies:true),R(FireRuleKind.ConsumeBurning,scope:FireRuleScope.Selection,condition:FireCondition.TargetBurning,allies:true,consume:FireSourceConsumption.BurningOnly)},"fire_spray","fire_detonate"),
            S("F-P-R19","熔障爆点",FireSpellRarity.Uncommon,FireSpellGroup.Ranged,X,FireDeliveryMode.DetachedProjection,NW,Now,Cast,2,4,2,0,4,FireTargetKind.Destructible,FireSelectionShape.Single,1,true,false,new[]{R(FireRuleKind.DamageDurability,24,objects:FireDestructibleMask.All),R(FireRuleKind.Damage,8,scope:FireRuleScope.OrthogonalNeighbors,allies:true)},"object_damage","fire_cross_blast"),
            S("F-P-R20","焚城界限",FireSpellRarity.Rare,FireSpellGroup.Ranged,X,FireDeliveryMode.DetachedProjection,NW,Now,Cast,3,5,4,8,4,FireTargetKind.Unit,FireSelectionShape.CenterAndOrthogonal,1,true,true,new[]{R(FireRuleKind.Damage,20,scope:FireRuleScope.Selection,allies:true),R(FireRuleKind.CreateFireground,12,6,FireRuleScope.CoveredCells,allies:true)},"fire_cross_blast","fire_detonate","fire_burning_ground")
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
                case "F-P-U01": return "f-p01"; case "F-P-U02": return "f-p03"; case "F-P-U03": return "f-p31";
                case "F-P-U04": return "f-p33"; case "F-P-U05": return "f-p25"; case "F-P-U06": return "f-p41";
                case "F-P-U07": return "f-p46"; case "F-P-U08": return "f-p43"; case "F-P-U09": return "f-p45";
                case "F-P-U10": return "f-p42"; case "F-P-U11": return "f-p49"; case "F-P-U12": return "f-p43";
                case "F-P-U13": return "f-p49"; case "F-P-U14": return "f-p12"; case "F-P-U15": return "f-p19";
                case "F-P-U16": return "f-p09"; case "F-P-U17": return "f-p15"; case "F-P-U18": return "f-p34";
                case "F-P-U19": return "f-p29"; case "F-P-U20": return "f-p30";
                case "F-P-R01": return "f-p01"; case "F-P-R02": return "f-p05"; case "F-P-R03": return "f-p03";
                case "F-P-R04": return "f-p06"; case "F-P-R05": return "f-p08"; case "F-P-R06": return "f-p07";
                case "F-P-R07": return "f-p10"; case "F-P-R08": return "f-p11"; case "F-P-R09": return "f-p09";
                case "F-P-R10": return "f-p31"; case "F-P-R11": return "f-p02"; case "F-P-R12": return "f-p13";
                case "F-P-R13": return "f-p15"; case "F-P-R14": return "f-p18"; case "F-P-R15": return "f-p21";
                case "F-P-R16": return "f-p04"; case "F-P-R17": return "f-p23"; case "F-P-R18": return "f-p24";
                case "F-P-R19": return "f-p37"; case "F-P-R20": return "f-p20";
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
