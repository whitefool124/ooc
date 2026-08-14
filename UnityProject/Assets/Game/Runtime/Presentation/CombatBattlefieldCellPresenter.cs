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
            Texture2D unitTexture = assets.Unit(unit);
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
            CombatUnitVitalsPresentation vitals = unit == null ? null : CombatUnitVitalsPresentation.From(unit, forecast);
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
                string key = tile.IsDestroyed ? "relay_rubble" : tile.Durability < 6 ? "relay_damaged" : "relay_intact";
                objectTexture = assets.Relay(key);
                if (!tile.IsDestroyed) objectLabel = "导能柱";
            }
            else if (tile.Cover == CoverType.Light)
            {
                string key = tile.IsDestroyed ? "light_cover_rubble" : tile.Durability < 4 ? "light_cover_damaged" : "light_cover_intact";
                objectTexture = assets.Relay(key);
            }
            else if (tile.Cover == CoverType.Heavy)
            {
                string key = tile.IsDestroyed ? "heavy_cover_rubble" : tile.Durability < 7 ? "heavy_cover_damaged" : "heavy_cover_intact";
                objectTexture = assets.Relay(key);
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
                ? state.Loot.IsLooted ? assets.Relay("loot_crate_empty") : assets.LootClosed : null;
            bool selected = selection.IsKeyboardTargeting && selection.KeyboardPosition == position ||
                unit != null && unit.Id == selection.TargetId;
            Texture2D selectionOverlay = selected ? assets.Overlay("selected") : null;
            string hover = unit == null ? string.Empty : unit.IsHero
                ? CombatInformationPresenter.BuildHeroDetails(unit)
                : CombatInformationPresenter.BuildEnemyHoverDetails(state, unit, intent) +
                  (forecast == null ? string.Empty : "\n伤害预览：" + forecast.PlayerSummary);
            Texture2D floor = assets.Relay(FloorKey(level, state.Map.Height, position.X, position.Y));
            Rect uv = unitTexture == null ? new Rect(0f, 0f, 1f, 1f) :
                CombatUnitHudLayout.UnitTextureCropUv(unitTexture.name);
            return new BattlefieldCellPresentation(position, floor, environment, move,
                selection.Action == "移动" ? 1f : .45f, attack, selection.Action == "攻击" ? 1f : .65f,
                skill, selectionOverlay, unitTexture, uv, unitTint, unitOffset, objectTexture, objectLabel,
                objectLabelColor, loot, unit, vitals, statuses, intent, intentTexture, hover);
        }

        public static string FloorKey(FirstRegionLevelDefinition level, int mapHeight, int x, int y)
        {
            if (level == null)
                return y == 0 || y == mapHeight - 1 ? "rail_horizontal" :
                    (x == 5 || x == 6) && y >= 3 && y <= 5 ? "floor_warning" : "floor_industrial";
            switch (level.FloorTheme)
            {
                case FirstRegionFloorTheme.StoneRoad: return y == 4 ? "floor_industrial" : "floor_plain";
                case FirstRegionFloorTheme.Courtyard: return (x + y) % 5 == 0 ? "floor_industrial" : "floor_plain";
                case FirstRegionFloorTheme.Ruins: return (x * 3 + y * 5) % 11 == 0 ? "floor_plain" : "floor_industrial";
                case FirstRegionFloorTheme.AetherMarked: return x == 6 || y == 4 ? "floor_industrial" : "floor_plain";
                default: return "floor_plain";
            }
        }
    }
}
