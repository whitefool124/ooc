using System;
using System.Collections.Generic;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    public sealed class BattlefieldStatusVisual
    {
        public CombatStatusPresentation Presentation { get; }
        public Texture2D Texture { get; }

        public BattlefieldStatusVisual(CombatStatusPresentation presentation, Texture2D texture)
        {
            Presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));
            Texture = texture;
        }
    }

    public sealed class BattlefieldCellPresentation
    {
        public GridPosition Position { get; }
        public Texture2D FloorTexture { get; }
        public Texture2D FloorTextureLow { get; }
        public Rect FloorUv { get; }
        public float FloorRotationDegrees { get; }
        public Texture2D TerrainBoundaryTexture { get; }
        public float TerrainBoundaryRotationDegrees { get; }
        public Texture2D EnvironmentTexture { get; }
        public Texture2D MoveOverlayTexture { get; }
        public float MoveOverlayAlpha { get; }
        public Texture2D AttackOverlayTexture { get; }
        public float AttackOverlayAlpha { get; }
        public Texture2D SkillOverlayTexture { get; }
        public float SkillOverlayAlpha { get; }
        public Texture2D SelectionOverlayTexture { get; }
        public Texture2D UnitTexture { get; }
        public Rect UnitUv { get; }
        public Color UnitTint { get; }
        public Vector2 UnitOffset { get; }
        public Vector2 UnitTravelOffset { get; }
        public Texture2D ObjectTexture { get; }
        public Texture2D ObjectTextureLow { get; }
        public int ObjectForegroundRows { get; }
        public string ObjectLabel { get; }
        public Color ObjectLabelColor { get; }
        public Texture2D LootTexture { get; }
        public UnitState Unit { get; }
        public CombatUnitVitalsPresentation Vitals { get; }
        public IReadOnlyList<BattlefieldStatusVisual> Statuses { get; }
        public EnemyIntentPresentation Intent { get; }
        public Texture2D IntentTexture { get; }
        public string HoverText { get; }
        public string SurfaceHoverText { get; }
        public string TerrainEffectHoverText { get; }
        public string ObjectHoverText { get; }

        public BattlefieldCellPresentation(GridPosition position, Texture2D floorTexture, Rect floorUv,
            float floorRotationDegrees,
            Texture2D terrainBoundaryTexture, float terrainBoundaryRotationDegrees,
            Texture2D environmentTexture,
            Texture2D moveOverlayTexture, float moveOverlayAlpha, Texture2D attackOverlayTexture, float attackOverlayAlpha,
            Texture2D skillOverlayTexture, Texture2D selectionOverlayTexture, Texture2D unitTexture, Rect unitUv,
            Color unitTint, Vector2 unitOffset, Texture2D objectTexture, string objectLabel, Color objectLabelColor,
            Texture2D lootTexture, UnitState unit, CombatUnitVitalsPresentation vitals,
            IReadOnlyList<BattlefieldStatusVisual> statuses, EnemyIntentPresentation intent, Texture2D intentTexture,
            string hoverText, Vector2 unitTravelOffset = default, int objectForegroundRows = 0,
            string surfaceHoverText = null, string terrainEffectHoverText = null, string objectHoverText = null,
            Texture2D floorTextureLow = null, Texture2D objectTextureLow = null, float skillOverlayAlpha = 1f)
        {
            Position = position;
            FloorTexture = floorTexture;
            FloorTextureLow = floorTextureLow;
            FloorUv = floorUv;
            FloorRotationDegrees = floorRotationDegrees;
            TerrainBoundaryTexture = terrainBoundaryTexture;
            TerrainBoundaryRotationDegrees = terrainBoundaryRotationDegrees;
            EnvironmentTexture = environmentTexture;
            MoveOverlayTexture = moveOverlayTexture;
            MoveOverlayAlpha = moveOverlayAlpha;
            AttackOverlayTexture = attackOverlayTexture;
            AttackOverlayAlpha = attackOverlayAlpha;
            SkillOverlayTexture = skillOverlayTexture;
            SkillOverlayAlpha = skillOverlayAlpha;
            SelectionOverlayTexture = selectionOverlayTexture;
            UnitTexture = unitTexture;
            UnitUv = unitUv;
            UnitTint = unitTint;
            UnitOffset = unitOffset;
            UnitTravelOffset = unitTravelOffset;
            ObjectTexture = objectTexture;
            ObjectTextureLow = objectTextureLow;
            ObjectForegroundRows = objectForegroundRows;
            ObjectLabel = objectLabel ?? string.Empty;
            ObjectLabelColor = objectLabelColor;
            LootTexture = lootTexture;
            Unit = unit;
            Vitals = vitals;
            Statuses = statuses ?? Array.Empty<BattlefieldStatusVisual>();
            Intent = intent;
            IntentTexture = intentTexture;
            HoverText = hoverText ?? string.Empty;
            SurfaceHoverText = surfaceHoverText ?? string.Empty;
            TerrainEffectHoverText = terrainEffectHoverText ?? string.Empty;
            ObjectHoverText = objectHoverText ?? string.Empty;
        }
    }
}
