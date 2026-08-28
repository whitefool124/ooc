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
            TileState tile = state.Map.GetTile(position);
            int environmentFrame = Mathf.FloorToInt(Time.unscaledTime * 8f) % assets.EnvironmentFrameCount;
            Texture2D environment = fireBattle?.HasFireground(position) == true
                ? assets.FiregroundFrame(environmentFrame)
                : tile.SmokeExpiresAt > state.CurrentTime ? assets.SmokeFrame(environmentFrame) : null;
            Texture2D move = battlefield.IsInMoveRange(state, position) ? assets.Overlay("move_range") : null;
            Texture2D attack = battlefield.IsInAttackRange(state, position) ? assets.Overlay("attack_range") : null;
            Texture2D skill = null;
            int fireSlot = selection.Action == "技能1" ? 0 : selection.Action == "技能2" ? 1 : -1;
            FireSpellDefinition fireSpell = fireSlot < 0 ? null : fireSpellInSlot(fireSlot);
            FireSpellPreview firePreview = fireSpell == null ? null : firePreviewAt(fireSpell, position);
            if (firePreview?.CanCommit == true)
                skill = assets.Overlay(firePreview.FriendlyFireRisk ? "high_risk" : "attack_range");

            UnitState unit = state.Units.Values.FirstOrDefault(candidate =>
                candidate.IsAlive && candidate.Position == position);
            Texture2D unitTexture = assets.Unit(unit, feedback?.EnemyAnimationFrame(unit) ?? -1);
            Vector2 unitOffset = Vector2.zero;
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
            if (tile.IsObjective)
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
            if (trainingRangeActive && tile.IsWater)
            {
                objectLabel = "水面";
                objectLabelColor = new Color(.38f, .82f, .94f, .92f);
            }

            Texture2D loot = state.Loot != null && state.Loot.Position == position
                ? assets.Academy(state.Loot.IsLooted ? "academy_loot_chest_empty" : "academy_loot_chest_closed") : null;
            bool selected = selection.IsKeyboardTargeting && selection.KeyboardPosition == position ||
                unit != null && unit.Id == selection.TargetId;
            Texture2D selectionOverlay = selected ? assets.Overlay("selected") : null;
            string hover = unit == null ? BuildTerrainHover(state, fireBattle, tile, position) : unit.IsHero
                ? CombatInformationPresenter.BuildHeroDetails(unit)
                : CombatInformationPresenter.BuildEnemyHoverDetails(state, unit, intent) +
                  (forecast == null ? string.Empty : "\n伤害预览：" + forecast.PlayerSummary);
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
                selection.Action == "移动" ? 1f : .45f, attack, selection.Action == "攻击" ? 1f : .65f,
                skill, selectionOverlay, unitTexture, uv, unitTint, unitOffset, objectTexture, objectLabel,
                objectLabelColor, loot, unit, vitals, statuses, intent, intentTexture, hover);
        }

        public static string BuildTerrainHover(CombatState state, FireBattleState fireBattle, TileState tile,
            GridPosition position)
        {
            if (state == null || tile == null) return string.Empty;
            if (state.Loot != null && state.Loot.Position == position)
                return state.Loot.IsLooted ? "空战利品箱\n已经搜刮，不再产出物品。" : "战利品箱\n相邻时可搜刮；内容将在打开后结算。";
            if (tile.IsObjective)
                return tile.IsDestroyed ? "损毁导能柱\n目标物已失效。" : "导能柱\n任务目标 · 耐久 " + tile.Durability + "；可被互动或指定术式影响。";
            if (tile.Cover == CoverType.Light)
                return tile.IsDestroyed ? "轻掩体残骸\n已失去防护效果，可正常通行。" : "轻掩体\n耐久 " + tile.Durability + "；剧情战斗中站立其上使物理伤害 -1，肉鸽战斗回合开始获得 2 护盾。";
            if (tile.Cover == CoverType.Heavy)
                return tile.IsDestroyed ? "重掩体残骸\n已失去阻挡和防护效果。" : "重掩体\n耐久 " + tile.Durability + "；阻挡移动与视线，肉鸽战斗中正前方相邻时回合开始获得 4 护盾。";
            if (tile.IsDevice)
                return tile.IsDestroyed ? "损毁设备\n设备已经失效。" : "战场设备\n耐久 " + tile.Durability + "；可被互动、破坏或指定术式影响。";
            if (fireBattle?.HasFireground(position) == true)
                return "燃烧地面\n进入或停留可能触发火焰伤害；剩余时间由施术效果决定。";
            if (tile.SmokeExpiresAt > state.CurrentTime)
                return "烟雾\n临时环境效果；会在时序 " + tile.SmokeExpiresAt + " 消散。";
            if (tile.IsWater)
                return "水面\n特殊地表；移动与术式交互以当前预览为准。";
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
    }
}
