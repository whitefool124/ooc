using UnityEngine;

namespace OCC.Combat.Presentation
{
    /// <summary>
    /// One semantic label per battlefield cell marker. Callers choose the meaning of a cell;
    /// they never choose the texture or the opacity.
    /// </summary>
    public enum BattlefieldCellMarker
    {
        None,
        MovementRange,
        MovementRisk,
        AttackEnvelope,
        AttackTarget,
        SkillSelectable,
        SkillCommitted,
        SkillRisk,
        SkillRangeEnvelope,
        Undeliverable,
        Focus
    }

    /// <summary>
    /// Single source of truth for how a battlefield cell marker is drawn. Every marker resolves to
    /// exactly one texture and one intensity tier here, so the same meaning always looks the same
    /// and no call site writes its own alpha.
    ///
    /// The ladder exists because a marked cell answers one of six different player questions:
    ///   Focus              当前焦点
    ///   Risky              会生效，且会伤及友军
    ///   Committed          会生效
    ///   Selectable         可以选择
    ///   RangeEnvelope      在术式射程内，但当前没有可作用的对象
    ///   Unavailable        在动作的宣称射程内，但结构性不可投递
    /// </summary>
    public static class BattlefieldMarkerLadder
    {
        /// <summary>Path of the centre mark drawn inside the destination frame. PLACEHOLDER: this 32x32
        /// diamond was drawn programmatically on the user's explicit instruction, which the art contract
        /// normally forbids; it must be replaced by a generated source before the asset becomes formal.</summary>
        public const string EnemyIntentMarkerPath = "Art/FormalTacticalOverlays32V2/enemy_intent_diamond";

        /// <summary>The frame around the enemy plan. It uses the neutral white frame texture (the one the
        /// player's selection outline uses) because it is the only frame in the project that can actually
        /// be tinted: the move-range corner brackets are cyan (R≈0), so multiplying them by any warm tint
        /// renders muddy - that is where the old "pale grey" came from, and they are identical in form to
        /// the player's own range.</summary>
        public static Color EnemyIntentDestinationTint => new Color(1f, .54f, .12f);

        /// <summary>Near-opaque on purpose: the marker is the only thing that can still be seen when the
        /// destination sits inside the player's own reachable cells.</summary>
        public const float EnemyIntentDestinationAlpha = .9f;

        /// <summary>Centre mark edge as a fraction of one cell, so the marker scales with the board.</summary>
        public const float EnemyIntentDotFraction = .34f;

        public static string TextureId(BattlefieldCellMarker marker)
        {
            switch (marker)
            {
                case BattlefieldCellMarker.MovementRange: return "move_range";
                case BattlefieldCellMarker.MovementRisk: return "high_risk";
                case BattlefieldCellMarker.AttackEnvelope:
                case BattlefieldCellMarker.AttackTarget:
                case BattlefieldCellMarker.SkillSelectable:
                case BattlefieldCellMarker.SkillCommitted:
                case BattlefieldCellMarker.SkillRangeEnvelope: return "attack_range";
                case BattlefieldCellMarker.SkillRisk: return "high_risk";
                case BattlefieldCellMarker.Undeliverable: return "unreachable";
                case BattlefieldCellMarker.Focus: return "selected";
                default: return null;
            }
        }

        public static float Alpha(BattlefieldCellMarker marker)
        {
            // The marker texture is a thin corner bracket, so a low alpha reads as barely-there on
            // light floors. Every tier therefore sits high enough to stay legible; the ordering, not a
            // wide dynamic range, carries the distinction between neighbouring tiers.
            switch (marker)
            {
                case BattlefieldCellMarker.Focus: return 1f;
                case BattlefieldCellMarker.MovementRisk:
                case BattlefieldCellMarker.SkillRisk: return .96f;
                case BattlefieldCellMarker.AttackTarget:
                case BattlefieldCellMarker.SkillCommitted: return .88f;
                case BattlefieldCellMarker.MovementRange:
                case BattlefieldCellMarker.SkillSelectable: return .72f;
                case BattlefieldCellMarker.AttackEnvelope: return .72f;
                case BattlefieldCellMarker.SkillRangeEnvelope: return .58f;
                case BattlefieldCellMarker.Undeliverable: return .42f;
                default: return 0f;
            }
        }
    }
}
