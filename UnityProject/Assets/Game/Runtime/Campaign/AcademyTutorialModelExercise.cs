using System;
using System.Collections.Generic;
using System.Linq;
using OCC.Combat.Roguelite;

namespace OCC.Combat
{
    // Disposable transactions: identical domain methods, no PlayerPrefs and no production run references.
    public sealed partial class AcademyTutorialModelExercise
    {
        private readonly Func<Model> factory;
        private readonly Model model;
        private string expected;
        private int step;
        private bool ready;
        public string LessonId { get; }
        public string Prompt => model.Prompt;
        public string ActionLabel => model.Labels != null ? model.Labels[step] : model.ActionLabel;
        public string Before { get; private set; }
        public string Preview { get; private set; }
        public string Result { get; private set; }
        public bool Executed { get; private set; }
        public bool MatchedPreview { get; private set; }
        public string[] Proofs => model.Proofs;
        public CombatState Combat => model.ReadCombat?.Invoke();

        private AcademyTutorialModelExercise(string id, Func<Model> build)
        {
            LessonId = id; factory = build; model = build(); PrepareNext();
        }
        private static int Count(Model value) => value.Steps?.Length ?? 1;
        private static void ApplyStep(Model value, int index)
        { if (value.Steps == null) value.Apply(); else value.Steps[index](); }
        public void PrepareNext()
        {
            if (Executed || step >= Count(model)) throw new InvalidOperationException("Practice has no next step.");
            Before = model.Read(); var preview = factory();
            for (int i = 0; i <= step; i++) ApplyStep(preview, i);
            if (step == Count(model) - 1 && !preview.Verify()) throw new InvalidOperationException("Practice preflight failed: " + LessonId);
            expected = preview.Fingerprint(); Preview = preview.Read(); ready = true; MatchedPreview = false; Result = null;
        }
        public void Execute()
        {
            if (Executed || !ready) throw new InvalidOperationException("Practice already submitted.");
            ApplyStep(model, step); Result = model.Read(); ready = false;
            MatchedPreview = model.Fingerprint() == expected;
            step++;
            Executed = step == Count(model);
            if (Executed) MatchedPreview &= model.Verify();
            if (!MatchedPreview) throw new InvalidOperationException("Practice transaction did not match preview: " + LessonId);
        }
        public static bool CanPrepare(string id) => Factories.ContainsKey(id) || VariantKeys.ContainsKey(id);
        public static AcademyTutorialModelExercise Prepare(string id, IEnumerable<string> completed = null)
        {
            if (VariantKeys.TryGetValue(id, out string[] variants))
            {
                var done = new HashSet<string>(completed ?? Array.Empty<string>());
                string next = variants.FirstOrDefault(key => !done.Contains(key)) ?? variants[0];
                return new AcademyTutorialModelExercise(id, () => id == "C14" ? ShieldRefresh(next == "cover-heavy-shield") : id == "E08" ? CrystalBurst(next == "pressure-crystal") : id == "E13" ? DeviceVariant(next) : id == "E03" ? DeliveryVariant(next) : id == "E10" ? ContainerVariant(next) : id == "E12" ? EnvironmentVariant(next) : id == "R03" || id == "R05" ? RewardVariant(id, next) : StateVariant(id, next));
            }
            if (!Factories.TryGetValue(id, out var factory)) throw new InvalidOperationException("Missing transaction practice: " + id);
            return new AcademyTutorialModelExercise(id, factory);
        }
        private sealed class Model
        {
            public string Prompt, ActionLabel;
            public string[] Proofs;
            public Func<string> Read, Fingerprint;
            public Func<CombatState> ReadCombat;
            public Action Apply;
            public Action[] Steps;
            public string[] Labels;
            public Func<bool> Verify;
        }
        private static readonly Dictionary<string, Func<Model>> Factories = new Dictionary<string, Func<Model>>
        {
            ["S01"] = Forge,
            ["S02"] = Specialize,
            ["S03"] = Treatment,
            ["S04"] = Meal,
            ["S05"] = Shop,
            ["S06"] = ServiceQuota,
            ["I07"] = SpellLoadout,
            ["I04"] = Quickbar,
            ["G02"] = MasterAndEquip,
            ["I01"] = Equipment,
            ["I02"] = Placement,
            ["I03"] = Capacity,
            ["I05"] = ItemCharges,
            ["I06"] = CombatInventory,
            ["I08"] = PassiveSpell,
            ["I09"] = Search,
            ["I10"] = Discard,
            ["U01"] = StatusAndGround,
            ["X08"] = FollowUpMovement,
            ["X16"] = Interception,
            ["X17"] = DelayedResources,
            ["X18"] = PreHitShield,
            ["X19"] = DirectCounter,
            ["X20"] = Interception,
            ["A08"] = SafeSave,
            ["A09"] = PauseAndAbandon,
            ["G01"] = GiftChoice,
            ["M01"] = MapTravel,
            ["M03"] = EventCost,
            ["M04"] = EventCost,
            ["M05"] = EventRevisit,
            ["M06"] = TermClock,
            ["M07"] = FinaleGate,
            ["R01"] = RewardReadOnly,
            ["R02"] = RewardSelection,
            ["F02"] = FailureBoundaries,
            ["F03"] = SameRoundHandoff,
            ["F04"] = NewRound,
            ["F05"] = ProtectUnfinishedRound,
            ["A07"] = ConsequenceConfirmation,
            ["R04"] = RewardOverflow,
            ["R06"] = RewardDetails,
            ["F01"] = VictorySummary,
            ["E01"] = Passage,
            ["E02"] = CoverLines,
            ["E05"] = WaterCost,
            ["E07"] = DestructiblePassage,
            ["E08"] = CrystalBurst,
            ["E11"] = FieldOverwrite,
            ["C03"] = ActionPoints,
            ["C04"] = MoveControl,
            ["C05"] = WeaponPayment,
            ["C06"] = SpellPayment,
            ["C07"] = ManaCapacity,
            ["C12"] = RejectedAction,
            ["C13"] = DamageLayers,
            ["C14"] = ShieldRefresh,
            ["E09"] = SearchContainer,
            ["P04"] = LockedRoute,
            ["C02"] = UpdatedIntent,
            ["P01"] = ReactionLimit,
            ["P02"] = ReactionOrder,
            ["P05"] = BossPhases,
            ["U05"] = BattleBoundary,
            ["C08"] = CooldownAndUses,
            ["C15"] = PassiveSource,
            ["P03"] = TemporaryGrowth,
            ["E04"] = AreaAndAnchor,
            ["E06"] = VineSearch,
            ["C09"] = TurnWindows,
        };
        private static RogueliteMapRun Service(string node)
        {
            var source = RogueliteMapRun.CreateSubsequentAcademyRun(4312, false);
            var dto = source.ExportRogue11();
            dto.DeparturePending = false; dto.CurrentNodeId = node;
            if (!dto.VisitedNodeIds.Contains(node)) dto.VisitedNodeIds.Add(node);
            dto.ForgeMaterialCount = 1; dto.SpecializationMaterialCount = 1;
            dto.Gold = 20; dto.StageContribution = 3; dto.CurrentHealth = 40; dto.AcademyFoodCount = 2;
            return RogueliteMapRun.FromRogue11(dto);
        }
        private static string Save(RogueliteMapRun run) => Rogue11Serializer.Serialize(run.ExportRogue11());
        private static RogueliteMapRun Restore(RogueliteMapRun run) => RogueliteMapRun.FromRogue11(Rogue11Serializer.Deserialize(Save(run)));
        private static Model Forge()
        {
            var run = Service("layer_workshop"); string target = run.RogueRunState.EquipmentInstances.First().InstanceId;
            int material = run.AcademyMaterialCount(AcademyBattleRewardCatalog.ForgeLoad), gold = run.Gold;
            return new Model
            {
                Prompt = "示例装备使用一份锻造材料。查看部位、消耗和唯一结果。", ActionLabel = "确认加工示例装备",
                Read = () => "材料 " + run.AcademyMaterialCount(AcademyBattleRewardCatalog.ForgeLoad) + "　金币 " + run.Gold + "\n锻造 " + (run.CurrentWorkshopService.ForgeCompleted ? "已完成" : "未加工") + "\n本次额度 " + (run.CurrentWorkshopService.ForgeCompleted ? "0" : "1"),
                Apply = () => run.CompleteFirstRunForge(target, AcademyBattleRewardCatalog.ForgeLoad),
                Verify = () => run.CurrentWorkshopService.ForgeCompleted && run.AcademyMaterialCount(AcademyBattleRewardCatalog.ForgeLoad) == material - 1 && run.Gold == gold && Restore(run).CurrentWorkshopService.ForgeCompleted,
                Fingerprint = () => Save(run), Proofs = new[] { "forge-preview", "forge-cost", "forge-commit", "forge-result" }
            };
        }
        private static Model Specialize()
        {
            var run = Service("layer_workshop"); int material = run.AcademyMaterialCount(AcademyBattleRewardCatalog.SpecAmplify);
            return new Model
            {
                Prompt = "示例术式消耗一份专精材料。结果固定，本轮不能更换。", ActionLabel = "确认专精示例术式",
                Read = () => "专精材料 " + run.AcademyMaterialCount(AcademyBattleRewardCatalog.SpecAmplify) + "\n专精 " + (run.CurrentWorkshopService.SpecializationCompleted ? "已完成" : "未专精") + "\n本次额度 " + (run.CurrentWorkshopService.SpecializationCompleted ? "0" : "1"),
                Apply = () => run.CompleteFirstRunSpecialization("BASE-FIRE-MELEE", AcademyBattleRewardCatalog.SpecAmplify),
                Verify = () => run.CurrentWorkshopService.SpecializationCompleted && run.AcademyMaterialCount(AcademyBattleRewardCatalog.SpecAmplify) == material - 1 && Restore(run).CurrentWorkshopService.SpecializationCompleted,
                Fingerprint = () => Save(run), Proofs = new[] { "specialize-preview", "specialize-commit", "specialize-result" }
            };
        }
        private static Model Treatment()
        {
            var run = Service("layer_medical"); int contribution = run.StageContribution;
            var entering = new UnitState("hero", true, new GridPosition(1, 1)); run.ApplyBuild(entering);
            int retainedHealth = entering.Health;
            return new Model
            {
                Prompt = "演练档案生命为40。治疗需1点贡献，恢复不超过生命上限。", ActionLabel = "治疗演练档案",
                Read = () => "生命 " + run.RogueRunState.CurrentHealth + "　上限 " + run.RogueHealthCapacity + "\n贡献 " + run.StageContribution + "\n治疗额度 " + (run.CurrentMedicalService.HealUsed ? "0" : "1"),
                Apply = run.UseFirstRunHeal,
                Verify = () => retainedHealth == 40 && run.RogueRunState.CurrentHealth == 50 && run.StageContribution == contribution - 1 && Restore(run).CurrentMedicalService.HealUsed,
                Fingerprint = () => Save(run), Proofs = new[] { "health-retained", "heal-preview", "heal-commit", "heal-cap" }
            };
        }
        private static Model Meal()
        {
            var run = Service("layer_medical"); string meal = run.CurrentMedicalService.MealCandidateIds.First();
            bool rejectedSecond = false; int afterPurchase = 0;
            return new Model
            {
                Prompt = "餐食不占术式槽，只能保留一种；每结算一场战斗减少一场。", ActionLabel = "试用餐食并结算演练战",
                Read = () => "餐食 " + (string.IsNullOrEmpty(run.ActiveMealId) ? "无" : AcademyMealCatalog.Get(run.ActiveMealId).Name) + "\n剩余战数 " + run.MealBattlesRemaining + "\n金币 " + run.Gold + "　贡献 " + run.StageContribution,
                Apply = () =>
                {
                    run.ChooseFirstRunMeal(meal); afterPurchase = run.MealBattlesRemaining;
                    try { run.ChooseFirstRunMeal(meal); } catch (InvalidOperationException) { rejectedSecond = true; }
                    var dto = run.RogueRunState; dto.CurrentNodeId = run.MapNodes.First(n => n.IsCombat).Id;
                    run = RogueliteMapRun.FromRogue11(dto); run.CompleteCurrentCombat();
                },
                Verify = () => rejectedSecond && afterPurchase == 3 && run.MealBattlesRemaining == 2 && Restore(run).MealBattlesRemaining == 2,
                Fingerprint = () => Save(run), Proofs = new[] { "meal-preview", "meal-commit", "meal-battle-count", "single-meal" }
            };
        }
        private static Model Shop()
        {
            var run = Service("layer_shop"); var offer = run.CurrentShopService.Offers.First(); int gold = run.Gold; bool soldOut = false;
            return new Model
            {
                Prompt = "商品显示完整价格。点击可购买的商品会直接购买；售罄不会因复访刷新。", ActionLabel = "购买演练商品",
                Read = () => "商品价格 " + offer.Price + " 金币\n金币 " + run.Gold + "\n库存 " + (run.CurrentShopService.Offers.First().Sold ? "售罄" : "1"),
                Apply = () =>
                {
                    run.PurchaseFirstRunOffer(offer.OfferId);
                    try { run.PurchaseFirstRunOffer(offer.OfferId); } catch (InvalidOperationException) { soldOut = true; }
                    run = Restore(run);
                },
                Verify = () => soldOut && run.Gold == gold - offer.Price && run.CurrentShopService.Offers.First().Sold,
                Fingerprint = () => Save(run), Proofs = new[] { "price-view", "purchase", "sold-out", "revisit-stock" }
            };
        }
        private static Model ServiceQuota()
        {
            var run = Service("layer_workshop"); string target = run.RogueRunState.EquipmentInstances.First().InstanceId;
            int gold = run.Gold; bool rejected = false;
            return new Model
            {
                Prompt = "免费只指不花金币，仍消耗材料和本次额度。复访不会补回。", ActionLabel = "加工并查看复访额度",
                Read = () => "金币 " + run.Gold + "\n锻造材料 " + run.AcademyMaterialCount(AcademyBattleRewardCatalog.ForgeLoad) + "\n可用额度 " + (run.CurrentWorkshopService.ForgeCompleted ? "0" : "1"),
                Apply = () =>
                {
                    run.CompleteFirstRunForge(target); run = Restore(run);
                    string before = Save(run);
                    try { run.CompleteFirstRunForge(target); } catch (InvalidOperationException) { rejected = Save(run) == before; }
                },
                Verify = () => rejected && run.Gold == gold && run.AcademyMaterialCount(AcademyBattleRewardCatalog.ForgeLoad) == 0 && run.CurrentWorkshopService.ForgeCompleted,
                Fingerprint = () => Save(run), Proofs = new[] { "quota-view", "material-cost", "revisit-quota" }
            };
        }
        private static Model SpellLoadout()
        {
            var loadout = RogueSpellLoadout.CreateStarter(); const string learned = "F-P-M04"; int stage = 0;
            return new Model
            {
                Prompt = "八个槽位保存配置。掌握、装备、替换和移除是不同操作。", ActionLabel = "试着装入、替换并取回",
                Read = () => "已掌握 " + loadout.MasteredSpellIds.Count + "\n八槽已装入 " + loadout.EquippedSpellIds.Count(s => !string.IsNullOrEmpty(s)) + "\n示例术式 " + (loadout.EquippedSpellIds.Contains(learned) ? "已装入" : loadout.MasteredSpellIds.Contains(learned) ? "已掌握，未装入" : "未掌握"),
                Steps = new Action[]
                {
                    () => loadout.Learn(learned),
                    () => { loadout.Equip(4, learned); if (loadout.EquippedSpellIds[4] == learned) stage++; },
                    () => { loadout.Equip(4, ""); if (string.IsNullOrEmpty(loadout.EquippedSpellIds[4])) stage++; },
                    () => { loadout.Equip(0, learned); if (loadout.EquippedSpellIds[0] == learned) stage++; }
                },
                Labels = new[] { "掌握示例术式", "装入第五个槽", "从第五槽移除", "替换第一槽的术式" },
                Verify = () => stage == 3 && loadout.MasteredSpellIds.Contains(learned) && loadout.EquippedSpellIds.Count() == 8,
                Fingerprint = () => string.Join("|", loadout.EquippedSpellIds), Proofs = new[] { "learn", "equip", "replace", "remove" }
            };
        }
        private static Model MasterAndEquip()
        {
            var loadout = RogueSpellLoadout.CreateStarter(); const string learned = "F-P-M04"; loadout.Learn(learned);
            return new Model
            {
                Prompt = "这条术式已掌握。把它装入空槽，战斗中才有对应入口。", ActionLabel = "装入演练空槽",
                Read = () => "已掌握 " + (loadout.MasteredSpellIds.Contains(learned) ? "是" : "否") + "\n已装备 " + (loadout.EquippedSpellIds.Contains(learned) ? "是" : "否"),
                Apply = () => loadout.Equip(4, learned), Verify = () => loadout.MasteredSpellIds.Contains(learned) && loadout.EquippedSpellIds[4] == learned,
                Fingerprint = () => string.Join("|", loadout.EquippedSpellIds), Proofs = new[] { "mastered", "equipped" }
            };
        }
        private static Model Quickbar()
        {
            var items = RogueEquipmentRuntime.CreateStarter(4312);
            var item = items.CreateTacticalItem("teaching-brace", "G-T13", 9, "teaching"); items.AddTacticalToBackpack(item); item.Consume();
            int initialUses = item.ChargesCurrent; bool linked = false, unlinked = false;
            return new Model
            {
                Prompt = "四格战术栏关联背包里的物品。取回或重新装入不恢复次数。", ActionLabel = "装入、取回并重新装入",
                Read = () => "四格战术栏 " + string.Join("　", items.ItemQuickbarInstanceIds.Select(s => string.IsNullOrEmpty(s) ? "空" : "定锚支架")) + "\n支架剩余次数 " + item.ChargesCurrent + "　总计 " + item.ChargesMaximum,
                Steps = new Action[] { () => linked = items.AssignQuickbar(0, item.InstanceId), () => unlinked = items.AssignQuickbar(0, ""), () => items.AssignQuickbar(0, item.InstanceId) },
                Labels = new[] { "装入第一战术格", "从战术格取回", "重新装入第一格" },
                Verify = () => linked && unlinked && items.ItemQuickbarInstanceIds[0] == item.InstanceId && item.ChargesCurrent == initialUses && item.ChargesMaximum == 3,
                Fingerprint = () => string.Join("|", items.ItemQuickbarInstanceIds) + "|" + item.ChargesCurrent,
                Proofs = new[] { "link", "unlink", "relink", "charges-retained" }
            };
        }
    }
}
