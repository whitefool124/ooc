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
        public Texture2D SelectionOverlayTexture { get; }
        public Texture2D UnitTexture { get; }
        public Rect UnitUv { get; }
        public Color UnitTint { get; }
        public Vector2 UnitOffset { get; }
        public Texture2D ObjectTexture { get; }
        public string ObjectLabel { get; }
        public Color ObjectLabelColor { get; }
        public Texture2D LootTexture { get; }
        public UnitState Unit { get; }
        public CombatUnitVitalsPresentation Vitals { get; }
        public IReadOnlyList<BattlefieldStatusVisual> Statuses { get; }
        public EnemyIntentPresentation Intent { get; }
        public Texture2D IntentTexture { get; }
        public string HoverText { get; }

        public BattlefieldCellPresentation(GridPosition position, Texture2D floorTexture, Rect floorUv,
            float floorRotationDegrees,
            Texture2D terrainBoundaryTexture, float terrainBoundaryRotationDegrees,
            Texture2D environmentTexture,
            Texture2D moveOverlayTexture, float moveOverlayAlpha, Texture2D attackOverlayTexture, float attackOverlayAlpha,
            Texture2D skillOverlayTexture, Texture2D selectionOverlayTexture, Texture2D unitTexture, Rect unitUv,
            Color unitTint, Vector2 unitOffset, Texture2D objectTexture, string objectLabel, Color objectLabelColor,
            Texture2D lootTexture, UnitState unit, CombatUnitVitalsPresentation vitals,
            IReadOnlyList<BattlefieldStatusVisual> statuses, EnemyIntentPresentation intent, Texture2D intentTexture,
            string hoverText)
        {
            Position = position;
            FloorTexture = floorTexture;
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
            SelectionOverlayTexture = selectionOverlayTexture;
            UnitTexture = unitTexture;
            UnitUv = unitUv;
            UnitTint = unitTint;
            UnitOffset = unitOffset;
            ObjectTexture = objectTexture;
            ObjectLabel = objectLabel ?? string.Empty;
            ObjectLabelColor = objectLabelColor;
            LootTexture = lootTexture;
            Unit = unit;
            Vitals = vitals;
            Statuses = statuses ?? Array.Empty<BattlefieldStatusVisual>();
            Intent = intent;
            IntentTexture = intentTexture;
            HoverText = hoverText ?? string.Empty;
        }
    }
}
