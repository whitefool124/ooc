using System;
using System.Collections.Generic;
using System.Linq;

namespace OCC.Combat
{
    public sealed class TrainingRangeAbilityEntry
    {
        public string ProviderId { get; }
        public string Id { get; }
        public string DisplayName { get; }
        public string Family { get; }
        public string Group { get; }
        public string Cost { get; }
        public string Targeting { get; }
        public string Summary { get; }
        public string IconPath { get; }
        public object RuntimeDefinition { get; }

        public TrainingRangeAbilityEntry(string providerId, string id, string displayName, string family, string group,
            string cost, string targeting, string summary, string iconPath, object runtimeDefinition)
        {
            ProviderId = providerId; Id = id; DisplayName = displayName; Family = family; Group = group;
            Cost = cost; Targeting = targeting; Summary = summary; IconPath = iconPath; RuntimeDefinition = runtimeDefinition;
        }
    }

    public sealed class TrainingRangePreviewReport
    {
        public bool CanCommit { get; }
        public IReadOnlyList<string> Failures { get; }
        public IReadOnlyList<GridPosition> Cells { get; }
        public IReadOnlyList<string> Targets { get; }
        public bool FriendlyFireRisk { get; }
        public object NativeResult { get; }
        public string Summary => CanCommit
            ? $"合法 // {Cells.Count} 格 / {Targets.Count} 单位" + (FriendlyFireRisk ? " / 友军风险" : string.Empty)
            : "不可提交 // " + string.Join("；", Failures);

        public TrainingRangePreviewReport(bool canCommit, IEnumerable<string> failures, IEnumerable<GridPosition> cells,
            IEnumerable<string> targets, bool friendlyFireRisk, object nativeResult)
        {
            CanCommit = canCommit; Failures = (failures ?? Array.Empty<string>()).ToArray();
            Cells = (cells ?? Array.Empty<GridPosition>()).ToArray(); Targets = (targets ?? Array.Empty<string>()).ToArray();
            FriendlyFireRisk = friendlyFireRisk; NativeResult = nativeResult;
        }

        public string Signature() => CanCommit + "|" + FriendlyFireRisk + "|" + string.Join(",", Failures) + "|" +
            string.Join(",", Cells) + "|" + string.Join(",", Targets);
    }

    public sealed class TrainingRangeExecutionReport
    {
        public IReadOnlyList<string> Steps { get; }
        public object NativeResult { get; }
        public string Summary => $"已执行 // {Steps.Count} 项确定性结果";
        public TrainingRangeExecutionReport(IEnumerable<string> steps, object nativeResult)
        { Steps = (steps ?? Array.Empty<string>()).ToArray(); NativeResult = nativeResult; }
        public string Signature() => string.Join("|", Steps);
    }

    public interface ITrainingRangeCase
    {
        TrainingRangeAbilityEntry Ability { get; }
        CombatState Combat { get; }
        GridPosition RecommendedCell { get; }
        string RecommendedUnitId { get; }
        TrainingRangePreviewReport Preview();
        TrainingRangeExecutionReport Execute();
    }

    public interface ITrainingRangeCaseProvider
    {
        string Id { get; }
        IReadOnlyList<TrainingRangeAbilityEntry> Abilities { get; }
        ITrainingRangeCase Prepare(string abilityId);
    }

    public sealed class TrainingRangeAuditReport
    {
        public int Total { get; }
        public int Passed { get; }
        public IReadOnlyList<string> Failures { get; }
        public bool IsSuccess => Total > 0 && Passed == Total && Failures.Count == 0;
        public int IllegalPreviewPassed { get; }
        public string Summary => $"全量巡检 {Passed}/{Total}" + (IsSuccess ? " // 确定性一致" : $" // 失败 {Failures.Count}");
        public TrainingRangeAuditReport(int total, int passed, IEnumerable<string> failures, int illegalPreviewPassed = 0)
        { Total = total; Passed = passed; Failures = (failures ?? Array.Empty<string>()).ToArray(); IllegalPreviewPassed = illegalPreviewPassed; }
    }

    public sealed class TrainingRangeSession
    {
        public const int PageSize = 10;
        private readonly IReadOnlyList<ITrainingRangeCaseProvider> providers;
        private readonly Dictionary<string, ITrainingRangeCaseProvider> providerByAbility;
        private int selectedIndex;

        public IReadOnlyList<TrainingRangeAbilityEntry> Abilities { get; }
        public TrainingRangeAbilityEntry CurrentAbility => Abilities[selectedIndex];
        public ITrainingRangeCase CurrentCase { get; private set; }
        public TrainingRangePreviewReport LastPreview { get; private set; }
        public TrainingRangeExecutionReport LastExecution { get; private set; }
        public TrainingRangeAuditReport LastAudit { get; private set; }
        public int CurrentPage => selectedIndex / PageSize;
        public int PageCount => Math.Max(1, (Abilities.Count + PageSize - 1) / PageSize);
        public ArtifactDefinition CurrentArtifact => CurrentAbility.RuntimeDefinition as ArtifactDefinition;
        public FireSpellDefinition CurrentFireSpell => CurrentAbility.RuntimeDefinition as FireSpellDefinition ?? CurrentArtifact?.Spell;
        public SkillDefinition CurrentSkill => CurrentAbility.RuntimeDefinition as SkillDefinition;
        public FireBattleState CurrentFireBattle => (CurrentCase as FireSpellTrainingRangeCase)?.Battle;

        public TrainingRangeSession(IEnumerable<ITrainingRangeCaseProvider> caseProviders = null)
        {
            providers = (caseProviders ?? new ITrainingRangeCaseProvider[] { new FireSpellTrainingRangeProvider(), new SkillTrainingRangeProvider(), new ArtifactTrainingRangeProvider() }).ToArray();
            Abilities = providers.SelectMany(provider => provider.Abilities).ToArray();
            if (Abilities.Count == 0) throw new InvalidOperationException("Training range requires at least one registered ability.");
            providerByAbility = providers.SelectMany(provider => provider.Abilities.Select(ability => new { ability.Id, Provider = provider }))
                .ToDictionary(pair => pair.Id, pair => pair.Provider, StringComparer.Ordinal);
        }

        public IReadOnlyList<TrainingRangeAbilityEntry> AbilitiesOnCurrentPage() =>
            Abilities.Skip(CurrentPage * PageSize).Take(PageSize).ToArray();

        public void Select(string abilityId)
        {
            int index = Abilities.ToList().FindIndex(ability => string.Equals(ability.Id, abilityId, StringComparison.Ordinal));
            if (index < 0) throw new InvalidOperationException("Unknown training range ability: " + abilityId);
            selectedIndex = index; CurrentCase = null; LastPreview = null; LastExecution = null;
        }

        public void ShiftPage(int delta)
        {
            int page = (CurrentPage + delta + PageCount) % PageCount;
            selectedIndex = Math.Min(page * PageSize, Abilities.Count - 1);
            CurrentCase = null; LastPreview = null; LastExecution = null;
        }

        public ITrainingRangeCase PrepareCurrent()
        {
            CurrentCase = providerByAbility[CurrentAbility.Id].Prepare(CurrentAbility.Id);
            LastPreview = CurrentCase.Preview(); LastExecution = null;
            return CurrentCase;
        }

        public TrainingRangePreviewReport PreviewCurrent()
        {
            if (CurrentCase == null) PrepareCurrent();
            LastPreview = CurrentCase.Preview(); return LastPreview;
        }

        public TrainingRangeExecutionReport ExecuteCurrent()
        {
            TrainingRangePreviewReport preview = PreviewCurrent();
            if (!preview.CanCommit) throw new InvalidOperationException(preview.Summary);
            LastExecution = CurrentCase.Execute(); return LastExecution;
        }

        public void RecordExternal(FireSpellPreview preview, FireSpellExecution execution)
        {
            LastPreview = FireSpellTrainingRangeCase.ToReport(preview);
            LastExecution = FireSpellTrainingRangeCase.ToReport(execution);
        }

        public void RecordExternal(CombatEffectExecution execution)
        {
            LastExecution = SkillTrainingRangeCase.ToReport(execution);
        }

        public TrainingRangeAuditReport RunFullAudit()
        {
            List<string> failures = new List<string>(); int passed = 0, illegalPassed = 0;
            foreach (ITrainingRangeCaseProvider provider in providers)
            foreach (TrainingRangeAbilityEntry ability in provider.Abilities)
            {
                try
                {
                    ITrainingRangeCase first = provider.Prepare(ability.Id), second = provider.Prepare(ability.Id);
                    TrainingRangePreviewReport previewA = first.Preview(), previewB = second.Preview();
                    if (!previewA.CanCommit) throw new InvalidOperationException(previewA.Summary);
                    if (previewA.Signature() != previewB.Signature()) throw new InvalidOperationException("预览不确定");
                    TrainingRangeExecutionReport resultA = first.Execute(), resultB = second.Execute();
                    if (resultA.Signature() != resultB.Signature()) throw new InvalidOperationException("结算不确定");
                    if (provider is FireSpellTrainingRangeProvider fireProvider)
                    {
                        TrainingRangePreviewReport illegalA = fireProvider.PrepareIllegal(ability.Id).Preview();
                        TrainingRangePreviewReport illegalB = fireProvider.PrepareIllegal(ability.Id).Preview();
                        if (illegalA.CanCommit) throw new InvalidOperationException("非法预览被错误接受");
                        if (illegalA.Signature() != illegalB.Signature()) throw new InvalidOperationException("非法预览不确定");
                        illegalPassed++;
                    }
                    passed++;
                }
                catch (Exception error) { failures.Add(ability.Id + " // " + error.Message); }
            }
            LastAudit = new TrainingRangeAuditReport(Abilities.Count, passed, failures, illegalPassed); return LastAudit;
        }
    }

    public sealed class SkillTrainingRangeProvider : ITrainingRangeCaseProvider
    {
        public string Id => "combat-skill";
        public IReadOnlyList<TrainingRangeAbilityEntry> Abilities { get; }

        public SkillTrainingRangeProvider()
        {
            Abilities = RogueliteSkillCatalog.All.Select(skill => new TrainingRangeAbilityEntry(Id, skill.Id, skill.DisplayName,
                "通用战斗技能", "技能池", $"1 AP + {skill.ManaCost} 以太 / CD {skill.Cooldown}",
                $"{skill.TargetRule} · {skill.Delivery} · 范围 {skill.Range}",
                string.Join(" → ", skill.Effects.Select(effect => effect.Type + (effect.Amount > 0 ? " " + effect.Amount : string.Empty))),
                FormalArtRegistry.RuntimeSkillPath(skill.Id), skill)).ToArray();
        }

        public ITrainingRangeCase Prepare(string abilityId)
        {
            SkillDefinition skill = RogueliteSkillCatalog.Get(abilityId);
            CombatState combat = TrainingRangeScenarioFactory.CreateStandard();
            UnitState hero = combat.GetUnit("hero"), ally = combat.GetUnit("range_ally"), enemy = combat.GetUnit("range_normal");
            hero.Equip(hero.MainHand, hero.OffHand, skill, CombatCatalog.FrostBind);
            hero.ConfigureMana(99, 80); hero.TakeDamage(12); hero.ApplyStatus(StatusType.Burning, 2, 8);
            ally.TakeDamage(12);
            CombatResolver.BeginTurn(combat, "hero");
            GridPosition cell; string unitId; CombatCommand command;
            switch (skill.TargetRule)
            {
                case SkillTargetRule.Self:
                    cell = hero.Position; unitId = hero.Id; command = CombatCommand.UseSkill(hero.Id, 0, null); break;
                case SkillTargetRule.AllyUnit:
                    cell = ally.Position; unitId = ally.Id; command = CombatCommand.UseSkill(hero.Id, 0, ally.Id); break;
                case SkillTargetRule.GridCell:
                    cell = new GridPosition(5, 5); unitId = null; command = CombatCommand.UseSkillAt(hero.Id, 0, cell, Facing.North); break;
                case SkillTargetRule.Destructible:
                    cell = new GridPosition(5, 4); unitId = null; combat.Map.SetTile(cell, new TileState { Cover = CoverType.Light, Durability = 24 });
                    command = CombatCommand.UseSkillAt(hero.Id, 0, cell, Facing.East); break;
                default:
                    cell = enemy.Position; unitId = enemy.Id; command = CombatCommand.UseSkill(hero.Id, 0, enemy.Id); break;
            }
            TrainingRangeAbilityEntry entry = Abilities.Single(ability => ability.Id == abilityId);
            return new SkillTrainingRangeCase(entry, combat, command, cell, unitId);
        }
    }

    public sealed class SkillTrainingRangeCase : ITrainingRangeCase
    {
        private readonly CombatCommand command;
        public TrainingRangeAbilityEntry Ability { get; }
        public CombatState Combat { get; }
        public GridPosition RecommendedCell { get; }
        public string RecommendedUnitId { get; }
        public SkillDefinition Skill => (SkillDefinition)Ability.RuntimeDefinition;

        public SkillTrainingRangeCase(TrainingRangeAbilityEntry ability, CombatState combat, CombatCommand command,
            GridPosition recommendedCell, string recommendedUnitId)
        { Ability = ability; Combat = combat; this.command = command; RecommendedCell = recommendedCell; RecommendedUnitId = recommendedUnitId; }

        public TrainingRangePreviewReport Preview()
        {
            try
            {
                CombatEffectExecution execution = CombatResolver.Resolve(Combat.Clone(), command);
                return new TrainingRangePreviewReport(true, Array.Empty<string>(), new[] { RecommendedCell },
                    string.IsNullOrEmpty(RecommendedUnitId) ? Array.Empty<string>() : new[] { RecommendedUnitId }, false, execution);
            }
            catch (InvalidOperationException error)
            {
                return new TrainingRangePreviewReport(false, new[] { error.Message }, new[] { RecommendedCell },
                    Array.Empty<string>(), false, null);
            }
        }

        public TrainingRangeExecutionReport Execute() => ToReport(CombatResolver.Resolve(Combat, command));

        public static TrainingRangeExecutionReport ToReport(CombatEffectExecution execution) => new TrainingRangeExecutionReport(
            execution.Results.Select(result => $"{result.Sequence:00} {result.Kind} // {(string.IsNullOrEmpty(result.TargetUnitId) ? result.PositionAfter.ToString() : result.TargetUnitId)} // {result.AppliedAmount} // {result.ValueBefore}>{result.ValueAfter}"), execution);
    }

    public static class TrainingRangeScenarioFactory
    {
        public static readonly GridPosition HeroCell = new GridPosition(3, 4);
        public static readonly GridPosition AllyCell = new GridPosition(3, 5);
        public static readonly GridPosition PrimaryEnemyCell = new GridPosition(4, 4);
        public static readonly GridPosition ObjectTargetCell = new GridPosition(6, 4);
        public static readonly GridPosition WaterCell = new GridPosition(5, 7);
        public static readonly GridPosition DeviceCell = new GridPosition(8, 6);
        public static readonly GridPosition ObjectiveCell = new GridPosition(10, 4);

        public static CombatState CreateStandard()
        {
            GridMap map = new GridMap(12, 9);
            map.SetTile(new GridPosition(8, 1), new TileState { Cover = CoverType.Light, Durability = 24 });
            map.SetTile(new GridPosition(9, 1), new TileState { Cover = CoverType.Heavy, Durability = 48 });
            map.SetTile(DeviceCell, new TileState { IsDevice = true, Durability = 20 });
            map.SetTile(WaterCell, new TileState { IsWater = true });
            map.SetTile(ObjectiveCell, new TileState { IsObjective = true, Durability = 120 });
            UnitState hero = new UnitState("hero", true, HeroCell, Facing.East) { DisplayName = "靶场施术者", Armor = 1, Speed = 11 };
            hero.ConfigureVitality(99); hero.ConfigureMana(99);
            UnitState ally = new UnitState("range_ally", true, AllyCell, Facing.East) { DisplayName = "友军校验员", Armor = 1 };
            ally.ConfigureVitality(60);
            UnitState normal = Target("range_normal", "步枪靶兵", PrimaryEnemyCell, 0, 0);
            UnitState shield = Target("range_shield", "盾卫靶兵", new GridPosition(5, 2), 1, 16);
            UnitState armored = Target("range_armored", "精英重甲靶", new GridPosition(7, 4), 8, 8);
            UnitState caster = Target("range_caster", "火术靶兵", new GridPosition(9, 4), 2, 4);
            UnitState mobile = Target("range_mobile", "突袭机动靶", new GridPosition(8, 7), 1, 2);
            return new CombatState(map, new[] { hero, ally, normal, shield, armored, caster, mobile }, Array.Empty<CombatObjective>());
        }

        private static UnitState Target(string id, string name, GridPosition cell, int armor, int shield)
        {
            UnitState unit = new UnitState(id, false, cell, Facing.West) { DisplayName = name, Armor = armor };
            unit.ConfigureVitality(99); unit.GrantShield(shield); return unit;
        }
    }

    public sealed class FireSpellTrainingRangeProvider : ITrainingRangeCaseProvider
    {
        public string Id => "fire-personal";
        public IReadOnlyList<TrainingRangeAbilityEntry> Abilities { get; }

        public FireSpellTrainingRangeProvider()
        {
            Abilities = FireSpellCatalog.All.Select(spell => new TrainingRangeAbilityEntry(Id, spell.Id, spell.DisplayName,
                "个人术式", GroupName(spell.Group), $"{spell.ActionPointCost} AP + {spell.ManaCost} 以太 / CD {spell.Cooldown}",
                $"{spell.CombatAffinity} · {spell.DeliveryMode} · {spell.WeaponRequirement} · {spell.TargetKind}/{spell.Shape} · 范围 {spell.Range}",
                $"{spell.TriggerWindow} · {spell.ConsumptionRule} // " + string.Join(" → ", spell.Rules.Select(rule => rule.Kind + (rule.Amount > 0 ? " " + rule.Amount : string.Empty))),
                spell.IconPath, spell)).ToArray();
        }

        public ITrainingRangeCase Prepare(string abilityId)
        {
            FireSpellDefinition spell = FireSpellCatalog.Get(abilityId);
            CombatState combat = TrainingRangeScenarioFactory.CreateStandard();
            combat.ConfigureRuleset(CombatRuleset.Roguelite);
            CombatResolver.BeginTurn(combat, "hero");
            FireBattleState battle = new FireBattleState(combat);
            UnitState hero = combat.GetUnit("hero"), enemy = combat.GetUnit("range_normal"), ally = combat.GetUnit("range_ally");
            hero.Equip(spell.CombatAffinity == FireCombatAffinity.MeleeOnly ? CombatCatalog.Hammer : CombatCatalog.Rifle,
                hero.OffHand, hero.SkillOne, hero.SkillTwo);

            foreach (FireSpellRule rule in spell.Rules)
            {
                if (rule.Condition == FireCondition.SourceBurning) hero.ApplyStatus(StatusType.Burning, 2, 8);
                if (rule.Condition == FireCondition.SourceBound) hero.ApplyStatus(StatusType.Bound, 2);
                if (rule.Condition == FireCondition.SourceSlowed) hero.ApplyStatus(StatusType.Slow, 2);
                if (rule.Kind == FireRuleKind.ClearOneSelfStatus) hero.ApplyStatus(StatusType.Burning, 2, 8);
            }

            GridPosition cell;
            string unitId;
            FireSpellTarget target;
            switch (spell.TargetKind)
            {
                case FireTargetKind.Self: cell = hero.Position; unitId = hero.Id; target = FireSpellTarget.Unit(hero.Id); break;
                case FireTargetKind.AllyOrSelf: cell = ally.Position; unitId = ally.Id; target = FireSpellTarget.Unit(ally.Id, Facing.East); break;
                case FireTargetKind.EmptyCell:
                    if (spell.CombatAffinity == FireCombatAffinity.MeleeOnly)
                    {
                        enemy.MoveTo(new GridPosition(5, 4), Facing.West); cell = new GridPosition(4, 4);
                    }
                    else cell = spell.Range <= 1 ? new GridPosition(3, 3) : spell.Range == 2 ? new GridPosition(3, 2) : new GridPosition(5, 5);
                    unitId = null; target = FireSpellTarget.At(cell, FacingToward(hero.Position, cell)); break;
                case FireTargetKind.BurningCell:
                    cell = spell.Shape == FireSelectionShape.Path ? new GridPosition(3, 1) : new GridPosition(5, 5);
                    unitId = null; target = FireSpellTarget.At(cell, FacingToward(hero.Position, cell)); break;
                case FireTargetKind.Destructible:
                    cell = spell.Range <= 2 ? new GridPosition(5, 4) : TrainingRangeScenarioFactory.ObjectTargetCell; unitId = null;
                    combat.Map.SetTile(cell, ObjectTileFor(spell)); target = FireSpellTarget.At(cell, Facing.East); break;
                default:
                    cell = enemy.Position; unitId = enemy.Id; target = FireSpellTarget.Unit(enemy.Id, Facing.East); break;
            }

            bool burning = spell.TargetKind == FireTargetKind.BurningUnit || spell.TargetKind == FireTargetKind.AdjacentBurningEnemy ||
                spell.TargetKind == FireTargetKind.BurningOrArmorBrokenEnemy || spell.Rules.Any(rule =>
                rule.Condition == FireCondition.TargetBurning || rule.Condition == FireCondition.TargetBurningAndOnFireground ||
                rule.Consumption == FireSourceConsumption.BurningOnly || rule.Consumption == FireSourceConsumption.BurningAndGround ||
                rule.Consumption == FireSourceConsumption.BurningFirstThenGround);
            bool ground = spell.TargetKind == FireTargetKind.BurningCell || spell.Rules.Any(rule =>
                rule.Condition == FireCondition.TargetOnFireground || rule.Condition == FireCondition.TargetBurningAndOnFireground ||
                rule.Consumption == FireSourceConsumption.GroundOnly || rule.Consumption == FireSourceConsumption.BurningAndGround);
            if (burning && unitId != null) combat.GetUnit(unitId).ApplyStatus(StatusType.Burning, 3, 8);
            if (ground)
            {
                if (spell.Shape == FireSelectionShape.Path)
                    for (int y = hero.Position.Y - 1; y >= cell.Y; y--) battle.CreateOrRefreshFireground(new GridPosition(hero.Position.X, y), 8, 8, "training-fixture");
                else battle.CreateOrRefreshFireground(cell, 8, 8, "training-fixture");
            }
            if (spell.Rules.Any(rule => rule.Kind == FireRuleKind.DamageDurability && rule.Scope == FireRuleScope.Selection) && spell.TargetKind != FireTargetKind.Destructible)
                combat.Map.SetTile(TrainingRangeScenarioFactory.ObjectTargetCell, new TileState { Cover = CoverType.Light, Durability = 24 });

            TrainingRangeAbilityEntry entry = Abilities.Single(ability => ability.Id == abilityId);
            return new FireSpellTrainingRangeCase(entry, battle, target, cell, unitId);
        }

        public ITrainingRangeCase PrepareIllegal(string abilityId)
        {
            FireSpellTrainingRangeCase prepared = (FireSpellTrainingRangeCase)Prepare(abilityId);
            prepared.Combat.GetUnit("hero").BeginTurn(0);
            return prepared;
        }

        private static TileState ObjectTileFor(FireSpellDefinition spell)
        {
            FireSpellRule[] objectRules = spell.Rules.Where(rule => rule.Kind == FireRuleKind.DamageDurability ||
                rule.Kind == FireRuleKind.DestroyLightCover || rule.Kind == FireRuleKind.OverloadDevice).ToArray();
            bool deviceOnly = objectRules.Length > 0 && objectRules.All(rule => rule.DestructibleMask == FireDestructibleMask.Device);
            return deviceOnly ? new TileState { IsDevice = true, Durability = 20 } : new TileState { Cover = CoverType.Light, Durability = 24 };
        }

        private static Facing FacingToward(GridPosition source, GridPosition target)
        {
            int dx = target.X - source.X, dy = target.Y - source.Y;
            if (Math.Abs(dx) >= Math.Abs(dy)) return dx >= 0 ? Facing.East : Facing.West;
            return dy >= 0 ? Facing.North : Facing.South;
        }

        private static string GroupName(FireSpellGroup group) => group == FireSpellGroup.Melee ? "近战专用" :
            group == FireSpellGroup.Universal ? "武器通用" : group == FireSpellGroup.Ranged ? "远程施法" :
            group == FireSpellGroup.Precision ? "精确" : group == FireSpellGroup.Fireground ? "火场" :
            group == FireSpellGroup.Detonation ? "引爆" : group == FireSpellGroup.Breach ? "攻坚" : "战术";
    }

    public sealed class ArtifactTrainingRangeProvider : ITrainingRangeCaseProvider
    {
        public string Id => "artifact";
        public IReadOnlyList<TrainingRangeAbilityEntry> Abilities { get; }

        public ArtifactTrainingRangeProvider()
        {
            Abilities = ArtifactCatalog.All.Select(artifact => new TrainingRangeAbilityEntry(Id, artifact.Id, artifact.DisplayName,
                "法宝", artifact.Provenance, $"{artifact.ActionPointCost} AP / {artifact.MaximumUses} 次封装",
                $"{artifact.TargetRule} · {artifact.Shape} · 范围 {artifact.Range}",
                artifact.EffectSummary + " // 风险：" + artifact.RiskSummary,
                artifact.IconPath, artifact)).ToArray();
        }

        public ITrainingRangeCase Prepare(string abilityId)
        {
            ArtifactDefinition artifact = ArtifactCatalog.All.Single(candidate => candidate.Id == abilityId);
            CombatState combat = TrainingRangeScenarioFactory.CreateStandard();
            CombatResolver.BeginTurn(combat, "hero");
            ArtifactBattleState battle = new ArtifactBattleState(combat);
            UnitState hero = combat.GetUnit("hero"), ally = combat.GetUnit("range_ally"), enemy = combat.GetUnit("range_normal");
            GridPosition cell; ArtifactTarget target; string unitId = null;
            switch (artifact.TargetRule)
            {
                case ArtifactTargetRule.Self:
                    cell = hero.Position; unitId = hero.Id; target = ArtifactTarget.Unit(hero.Id, cell); break;
                case ArtifactTargetRule.Enemy:
                case ArtifactTargetRule.AnyUnit:
                    cell = enemy.Position; unitId = enemy.Id; target = ArtifactTarget.Unit(enemy.Id, cell); break;
                case ArtifactTargetRule.AllyOrSelf:
                    ally.TakeDamage(20); ally.GrantShield(20); cell = ally.Position; unitId = ally.Id; target = ArtifactTarget.Unit(ally.Id, cell); break;
                case ArtifactTargetRule.TwoAllies:
                    cell = ally.Position; unitId = ally.Id; target = ArtifactTarget.Pair(ally.Id, hero.Id, cell); break;
                case ArtifactTargetRule.Destructible:
                case ArtifactTargetRule.Device:
                    cell = new GridPosition(3, 3); combat.Map.SetTile(cell, artifact.TargetRule == ArtifactTargetRule.Device
                        ? new TileState { IsDevice = true, Durability = 24 }
                        : new TileState { Cover = CoverType.Light, Durability = 24 }); target = ArtifactTarget.At(cell); break;
                default:
                    cell = artifact.Range <= 2 ? new GridPosition(3, 2) : new GridPosition(5, 5);
                    target = ArtifactTarget.At(cell); break;
            }
            if (artifact.Id == "G-T17")
            {
                cell = enemy.Position;
                unitId = enemy.Id;
                target = ArtifactTarget.At(cell);
            }
            if (artifact.Id == "G-T11")
            {
                battle.Firegrounds[cell] = 4;
                TileState smoke = combat.Map.GetTile(cell).Clone(); smoke.SmokeExpiresAt = 4; combat.Map.SetTile(cell, smoke);
            }
            if (artifact.Id == "G-T05") hero.SpendMana(4);
            if (artifact.Id == "G-T18") ally.ApplyStatus(StatusType.Slow, 2);
            TrainingRangeAbilityEntry entry = Abilities.Single(ability => ability.Id == abilityId);
            return new ArtifactTrainingRangeCase(entry, battle, target, cell, unitId);
        }
    }

    public sealed class ArtifactTrainingRangeCase : ITrainingRangeCase
    {
        private readonly ArtifactTarget target;
        public TrainingRangeAbilityEntry Ability { get; }
        public ArtifactBattleState Battle { get; }
        public CombatState Combat => Battle.Combat;
        public GridPosition RecommendedCell { get; }
        public string RecommendedUnitId { get; }
        public ArtifactDefinition Artifact => (ArtifactDefinition)Ability.RuntimeDefinition;

        public ArtifactTrainingRangeCase(TrainingRangeAbilityEntry ability, ArtifactBattleState battle,
            ArtifactTarget target, GridPosition recommendedCell, string recommendedUnitId)
        { Ability = ability; Battle = battle; this.target = target; RecommendedCell = recommendedCell; RecommendedUnitId = recommendedUnitId; }

        public TrainingRangePreviewReport Preview()
        {
            ArtifactPreview preview = ArtifactEngine.Preview(Battle, "hero", Artifact, target, Artifact.MaximumUses);
            return new TrainingRangePreviewReport(preview.CanCommit, preview.Failures, preview.Cells, preview.UnitIds,
                preview.FriendlyFireRisk, preview);
        }

        public TrainingRangeExecutionReport Execute()
        {
            ArtifactExecution execution = ArtifactEngine.Execute(Battle, "hero", Artifact, target, Artifact.MaximumUses);
            return new TrainingRangeExecutionReport(execution.Steps.Select(step => step.ToString()), execution);
        }
    }

    public sealed class FireSpellTrainingRangeCase : ITrainingRangeCase
    {
        private readonly FireSpellTarget target;
        public TrainingRangeAbilityEntry Ability { get; }
        public FireBattleState Battle { get; }
        public CombatState Combat => Battle.Combat;
        public GridPosition RecommendedCell { get; }
        public string RecommendedUnitId { get; }
        public FireSpellDefinition Spell => Ability.RuntimeDefinition as FireSpellDefinition ?? ((ArtifactDefinition)Ability.RuntimeDefinition).Spell;

        public FireSpellTrainingRangeCase(TrainingRangeAbilityEntry ability, FireBattleState battle, FireSpellTarget target,
            GridPosition recommendedCell, string recommendedUnitId)
        { Ability = ability; Battle = battle; this.target = target; RecommendedCell = recommendedCell; RecommendedUnitId = recommendedUnitId; }

        public TrainingRangePreviewReport Preview() => ToReport(FireSpellEngine.Preview(Battle, "hero", Spell, target));
        public TrainingRangeExecutionReport Execute() => ToReport(FireSpellEngine.Execute(Battle, "hero", Spell, target));

        public static TrainingRangePreviewReport ToReport(FireSpellPreview preview) => new TrainingRangePreviewReport(preview.CanCommit,
            preview.Failures, preview.Cells, preview.UnitIds, preview.FriendlyFireRisk, preview);

        public static TrainingRangeExecutionReport ToReport(FireSpellExecution execution) => new TrainingRangeExecutionReport(
            execution.Steps.Select(step => $"{step.Sequence:00} {step.Kind} // {(string.IsNullOrEmpty(step.TargetId) ? step.Cell.ToString() : step.TargetId)} // {step.Applied} // {step.Detail}"), execution);
    }
}
