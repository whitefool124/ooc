using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    public sealed class CombatBattlefieldCellPresenter
    {
        private readonly BattlefieldPresentationAdapter battlefield;
        private readonly CombatFormalVisualAssets assets;

        public CombatBattlefieldCellPresenter(BattlefieldPresentationAdapter battlefield,
            CombatFormalVisualAssets assets)
        {
            this.battlefield = battlefield ?? throw new ArgumentNullException(nameof(battlefield));
            this.assets = assets ?? throw new ArgumentNullException(nameof(assets));
        }

        public BattlefieldCellPresentation Build(CombatState state, FirstRegionLevelDefinition level,
            FireBattleState fireBattle, CombatSelectionController selection, bool trainingRangeActive,
            CombatVisualFeedback feedback, GridPosition position,
            Func<int, FireSpellDefinition> fireSpellInSlot,
            Func<FireSpellDefinition, GridPosition, FireSpellPreview> firePreviewAt,
            Func<UnitState, CombatTargetDamageForecast> damageForecast,
            Func<UnitState, EnemyIntentPresentation> enemyIntent)
        {
            if (state == null || !state.Map.IsInside(position)) return null;
            TileState tile = feedback?.PresentedTile(position, state.Map.GetTile(position)) ?? state.Map.GetTile(position);
            int environmentFrame = Mathf.FloorToInt(Time.unscaledTime * 8f) % assets.EnvironmentFrameCount;
            bool hasFireground = fireBattle?.HasFireground(position) == true;
            hasFireground = feedback?.PresentedFireground(position, hasFireground) ?? hasFireground;
            Texture2D environment = hasFireground
                ? assets.FiregroundFrame(environmentFrame)
                : tile.SmokeExpiresAt > state.CurrentTime ? assets.SmokeFrame(environmentFrame)
                : tile.IsWater ? assets.Environment("rain_court_water") : null;
            Texture2D move = selection.Action == "移动" && battlefield.IsInMoveRange(state, position) ? assets.Overlay("move_range") : null;
            Texture2D attack = selection.Action == "攻击" && battlefield.IsInAttackRange(state, position) ? assets.Overlay("attack_range") : null;
            Texture2D skill = null;
            int fireSlot = selection.Action == "技能1" ? 0 : selection.Action == "技能2" ? 1 : -1;
            FireSpellDefinition fireSpell = fireSlot < 0 ? null : fireSpellInSlot(fireSlot);
            FireSpellPreview firePreview = fireSpell == null ? null : firePreviewAt(fireSpell, position);
            if (firePreview?.CanCommit == true)
                skill = assets.Overlay(firePreview.FriendlyFireRisk ? "high_risk" : "attack_range");

            UnitState unit = state.Units.Values.Select(candidate => feedback?.PresentedUnit(candidate) ?? candidate).FirstOrDefault(candidate =>
                candidate.IsAlive && candidate.Position == position);
            if (state.GreenhouseCollectionRoom?.IsRaiderHidden(state, unit) == true) unit = null;
            Texture2D unitTexture = assets.Unit(unit, feedback?.EnemyAnimationFrame(unit) ?? -1);
            Vector2 unitOffset = Vector2.zero;
            CombatMovementPose travel = feedback != null ? feedback.UnitTravelPose(unit) :
                new CombatMovementPose(new Vector2(position.X, position.Y));
            // Quantize at the overview scale; the View doubles it for the integer zoom tier.
            Vector2 travelOffset = unit == null ? Vector2.zero : travel.OffsetFrom(position, 64f);
            Color unitTint = Color.white;
            if (unit != null)
            {
                float phase = unit.IsHero ? 0f : unit.Position.X * .71f + unit.Position.Y * .37f;
                unitOffset.y = Mathf.RoundToInt(Mathf.Sin(Time.unscaledTime * 1.8f + phase));
                if (feedback != null)
                {
                    unitOffset += feedback.UnitPresentationOffset(unit);
                    unitOffset.x += feedback.UnitShakeOffset(unit);
                    unitTint = feedback.UnitPresentationTint(unit);
                }
            }

            CombatTargetDamageForecast forecast = unit != null && !unit.IsHero ? damageForecast(unit) : null;
            CombatUnitVitalsPresentation vitals = unit == null ? null : CombatUnitVitalsPresentation.From(unit, forecast,
                state.Ruleset == CombatRuleset.Roguelite);
            List<BattlefieldStatusVisual> statuses = unit == null ? new List<BattlefieldStatusVisual>() :
                unit.Statuses.OrderBy(entry => entry.Key).Take(6)
                    .Select(entry => new BattlefieldStatusVisual(
                        CombatStatusPresentation.From(unit, entry.Key), assets.Status(entry.Key))).ToList();
            EnemyIntentPresentation intent = unit != null && !unit.IsHero ? enemyIntent(unit) : null;
            Texture2D intentTexture = intent == null ? null : assets.Intent(intent.IconId);

            Texture2D objectTexture = null;
            string objectLabel = string.Empty;
            Color objectLabelColor = FormalUiTheme.Text;
            if (tile.IsLampVine)
            {
                objectTexture = assets.Environment("lamp_vine");
                objectLabel = state.GreenhouseCollectionRoom?.HasHiddenRaider(state) == true &&
                    state.GreenhouseCollectionRoom.RaiderLastKnownPosition == position ? "侧锋踪迹" : string.Empty;
                objectLabelColor = new Color(.94f, .69f, .28f, .95f);
            }
            else if (tile.IsAetherCrystal)
            {
                objectTexture = assets.Academy("academy_aether_pillar_intact");
                objectLabel = "晶簇";
                objectLabelColor = new Color(.42f, .88f, 1f, .96f);
            }
            else if (tile.IsCrystalShard)
            {
                objectTexture = assets.Academy("academy_light_planter_rubble");
                objectLabel = "碎晶";
                objectLabelColor = new Color(.52f, .9f, 1f, .9f);
            }
            else if (tile.IsScorched)
            {
                objectTexture = assets.Academy("academy_light_planter_rubble");
                objectLabel = "焦痕";
                objectLabelColor = new Color(.48f, .38f, .31f, .9f);
            }
            else if (tile.IsObjective)
            {
                string key = tile.IsDestroyed ? "academy_aether_pillar_rubble" : tile.Durability < 6 ? "academy_aether_pillar_damaged" : "academy_aether_pillar_intact";
                objectTexture = assets.Academy(key);
                if (!tile.IsDestroyed) objectLabel = "导能柱";
            }
            else if (tile.Cover == CoverType.Light)
            {
                string family = (position.X + position.Y) % 2 == 0 ? "academy_light_stone_bench_" : "academy_light_planter_";
                string stateKey = tile.IsDestroyed ? "rubble" : tile.Durability < 4 ? "damaged" : "intact";
                string variant = stateKey == "intact"
                    ? AcademyBattlefieldLayoutCatalog.CoverVariant(level?.Id, position, CoverType.Light)
                    : null;
                objectTexture = assets.Academy(variant ?? family + stateKey);
            }
            else if (tile.Cover == CoverType.Heavy)
            {
                string family = (position.X + position.Y) % 2 == 0 ? "academy_heavy_archive_stack_" : "academy_heavy_masonry_screen_";
                string stateKey = tile.IsDestroyed ? "rubble" : tile.Durability < 7 ? "damaged" : "intact";
                string variant = stateKey == "intact"
                    ? AcademyBattlefieldLayoutCatalog.CoverVariant(level?.Id, position, CoverType.Heavy)
                    : null;
                objectTexture = assets.Academy(variant ?? family + stateKey);
            }
            else if (trainingRangeActive && tile.IsDevice)
            {
                objectTexture = assets.Relay(tile.IsDestroyed ? "heavy_cover_rubble" : "heavy_cover_intact");
                objectLabel = "设备";
            }
            if (tile.IsWater)
            {
                objectLabel = string.Empty;
                objectLabelColor = new Color(.38f, .82f, .94f, .92f);
            }

            bool legacyLootHere = state.Loot != null && state.Loot.Position == position;
            bool searchableLootHere = state.LootSource != null && state.LootSource.Position == position;
            Texture2D loot = legacyLootHere
                ? assets.Academy(state.Loot.IsLooted ? "academy_loot_chest_empty" : "academy_loot_chest_closed")
                : searchableLootHere ? assets.Academy(state.LootSource.State == LootSearchState.Emptied
                    ? "academy_loot_chest_empty" : "academy_loot_chest_closed") : null;
            bool selected = selection.IsKeyboardTargeting && selection.KeyboardPosition == position ||
                unit != null && unit.Id == selection.TargetId;
            Texture2D selectionOverlay = selected ? assets.Overlay("selected") : null;
            string hover = unit == null ? BuildTerrainHover(state, fireBattle, tile, position) : unit.IsHero
                ? CombatInformationPresenter.BuildHeroDetails(unit)
                : CombatInformationPresenter.BuildEnemyHoverDetails(state, unit, intent) +
                  FixedEncounterEnemyRuleText(state, unit) +
                  (forecast == null ? string.Empty : "\n预计伤害：" + forecast.PlayerSummary);
            if (unit != null)
                hover = AppendTerrainHover(hover, BuildTerrainHover(state, fireBattle, tile, position));
            Texture2D floor = assets.Academy(FloorKey(level, state.Map.Height, position.X, position.Y));
            Rect floorUv = FloorUv(position.X, position.Y);
            float floorRotation = FloorRotationDegrees(level, position.X, position.Y);
            string boundaryId = AcademyBattlefieldLayoutCatalog.BoundaryOverlay(level, position.X, position.Y,
                out int boundaryTurns);
            Texture2D terrainBoundary = string.IsNullOrEmpty(boundaryId) ? null : assets.Academy(boundaryId);
            float terrainBoundaryRotation = -90f * boundaryTurns;
            Rect uv = unitTexture == null ? new Rect(0f, 0f, 1f, 1f) :
                CombatUnitHudLayout.UnitTextureCropUv(unitTexture.name);
            return new BattlefieldCellPresentation(position, floor, floorUv, floorRotation,
                terrainBoundary, terrainBoundaryRotation, environment, move,
                .64f, attack, .72f,
                skill, selectionOverlay, unitTexture, uv, unitTint, unitOffset, objectTexture, objectLabel,
                objectLabelColor, loot, unit, vitals, statuses, intent, intentTexture, hover, travelOffset,
                tile.IsLampVine ? CombatObjectLayerLayout.LampVineFrontRows : 0);
        }

        public static string AppendTerrainHover(string unitHover, string terrainHover) =>
            string.IsNullOrWhiteSpace(terrainHover) ? unitHover : unitHover + "\n\n脚下地形 · " + terrainHover;

        public static string BuildTerrainHover(CombatState state, FireBattleState fireBattle, TileState tile,
            GridPosition position)
        {
            if (state == null || tile == null) return string.Empty;
            if (tile.IsLampVine)
                return "灯藤\n可进入；进入消耗 2 移动距离并阻挡双方视线。藤内只能攻击相邻目标；可作用物块的火焰会烧去一格。";
            if (tile.IsAetherCrystal)
                return "蓄能晶簇（占位表现）\n不可进入、不阻挡攻击线；耐久 " + tile.Durability + "。摧毁后对正交四格造成 8 点以太伤害，并生成五格碎晶。";
            if (tile.IsCrystalShard)
                return "碎晶（占位表现）\n可进入；进入消耗 2 移动距离，不阻挡攻击线且不持续造成伤害。";
            if (tile.IsScorched)
                return "灯藤焦痕\n纯视觉痕迹；原地表已露出，不造成伤害、遮挡或状态。";
            if (tile.IsWater)
                return "浅水\n主角进入消耗 2 移动距离，寻迹兽进入消耗 1；进入时立即移除燃烧。";
            if (state.Loot != null && state.Loot.Position == position)
                return state.Loot.IsLooted ? "空战利品箱\n里面已经没有东西了。" : "战利品箱\n走到旁边就能打开，看看里面有什么。";
            if (state.LootSource != null && state.LootSource.Position == position)
                return state.LootSource.State == LootSearchState.Emptied ? "空检修备件箱\n苗床回流芯已经取走。" :
                    "中央检修备件箱\n站在正交邻格搜刮消耗 1 AP；固定藏有苗床回流芯。";
            if (tile.IsObjective)
                return tile.IsDestroyed ? "损毁导能柱\n目标物已失效。" : "导能柱\n任务目标 · 耐久 " + tile.Durability + "；可被互动或指定术式影响。";
            if (tile.Cover == CoverType.Light)
                return tile.IsDestroyed ? "轻掩体残骸\n已失去防护效果，可正常通行。" : "轻掩体\n耐久 " + tile.Durability + "；肉鸽战斗中站立其上，于自身回合结束获得 2 护盾。";
            if (tile.Cover == CoverType.Heavy)
                return tile.IsDestroyed ? "重掩体残骸\n已失去阻挡和防护效果。" : "重掩体\n耐久 " + tile.Durability + "；阻挡移动与视线，肉鸽战斗中与其正交相邻时于自身回合结束获得 4 护盾。";
            if (tile.IsDevice)
                return tile.IsDestroyed ? "损毁设备\n设备已经失效。" : "战场设备\n耐久 " + tile.Durability + "；可被互动、破坏或指定术式影响。";
            if (fireBattle?.HasFireground(position) == true)
                return "燃烧地面\n进入或停留可能触发火焰伤害；剩余时间由施术效果决定。";
            if (tile.SmokeExpiresAt > state.CurrentTime)
                return "烟雾\n临时环境效果；会在第 " + tile.SmokeExpiresAt + " 行动时消散。";
            return string.Empty;
        }

        public static string FloorKey(FirstRegionLevelDefinition level, int mapHeight, int x, int y)
        {
            return AcademyBattlefieldLayoutCatalog.FloorAsset(level, x, y, out _);
        }

        public static float FloorRotationDegrees(FirstRegionLevelDefinition level, int x, int y)
        {
            AcademyBattlefieldLayoutCatalog.FloorAsset(level, x, y, out int quarterTurns);
            return -90f * quarterTurns;
        }

        public static Rect FloorUv(int x, int y)
        {
            return new Rect(0f, 0f, 1f, 1f);
        }

        private static string FixedEncounterEnemyRuleText(CombatState state, UnitState unit)
        {
            if (state == null || unit == null) return string.Empty;
            if (state.RainLanternCourt != null && unit.EnemyArchetypeId == "tether_hound")
                return "\n搜索规则：看见主角时追击并优先扑咬；失去视线后会前往灯藤入口，完整嗅探一回合，再沿外围顺时针搜索。";
            if (state.RainLanternCourt != null && unit.EnemyArchetypeId == "pyromancer")
                return "\n固定规则：能合法使用火矢时优先攻击；否则每回合最多一次，依次烧去 F3、F4、F5 的灯藤。";
            if (state.GreenhouseCollectionRoom != null && unit.EnemyArchetypeId == "sigil_mauler")
                return "\n固定规则：无法攻击主角时接近最近完整晶簇并公开贴晶校准；所贴晶簇被毁后正面追击。借晶强化量未冻结，当前按基础重击结算。";
            if (state.GreenhouseCollectionRoom != null && unit.EnemyArchetypeId == "raider")
                return "\n固定规则：相邻时优先钩刃牵制，否则限位钩刃；不相邻时从灯藤侧翼接近，藤内隐藏，能出藤贴邻时才显形。";
            return string.Empty;
        }
    }
}
