using System;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    public static class CombatUnitHudLayout
    {
        public static Rect EnemyHoverCardRect(Vector2 pointer)
        {
            const float width = 456f, height = 242f, margin = 16f, battlefieldRight = 1440f, commandsTop = 900f;
            float x = pointer.x + 20f;
            if (x + width > battlefieldRight - margin) x = pointer.x - width - 20f;
            x = Mathf.Clamp(x, margin, battlefieldRight - margin - width);
            float y = Mathf.Clamp(pointer.y + 20f, 64f, commandsTop - margin - height);
            return new Rect(x, y, width, height);
        }

        public static Rect EnemyIntentBadgeRect(BattlefieldRect cell, int expectedDamage)
        {
            float width = expectedDamage > 0 ? 43f : 20f;
            return new Rect(cell.X + (cell.Width - width) * .5f, cell.Y + 2f, width, 20f);
        }

        public static Rect UnitPresentationRect(BattlefieldRect cell)
        {
            // Formal unit sprites own a 64x64 transparent safety canvas. Present that whole canvas
            // at a whole-number texture scale. Intermediate camera zooms round upward and may
            // overflow the logical cell; presentation overflow never changes the logical hit cell.
            float size = Mathf.Max(64f, Mathf.Ceil(cell.Width / 64f) * 64f);
            return new Rect(cell.X + (cell.Width - size) * .5f, cell.Y + cell.Height - size, size, size);
        }

        public static Rect UnitVisibleContentRect(BattlefieldRect cell, string textureName)
        {
            return UnitPresentationRect(cell);
        }

        // Formal unit textures own their transparent safety margin. Cropping by legacy body bounds
        // hides weapons, shields, tails and casting implements that legitimately extend sideways.
        public static Rect UnitTextureCropUv(string textureName) => new Rect(0f, 0f, 1f, 1f);

        public static Rect UnitHealthBarRect(BattlefieldRect cell)
        {
            float scale = ElementScale(cell);
            float width = 108f * scale;
            return new Rect(cell.X + (cell.Width - width) * .5f, cell.Y + 94f * scale, width, 17f * scale);
        }

        public static Rect UnitShieldBarRect(BattlefieldRect cell)
        {
            float scale = ElementScale(cell);
            float width = 108f * scale;
            return new Rect(cell.X + (cell.Width - width) * .5f, cell.Y + 112f * scale, width, 15f * scale);
        }

        public static string VitalText(CombatUnitVitalPresentation vital, float cellSize)
        {
            if (vital == null || cellSize < 90f) return string.Empty;
            if (cellSize < 112f)
                return vital.ForecastLoss > 0 ? "-" + vital.ForecastLoss + "→" + vital.Remaining : vital.Current + "/" + vital.Maximum;
            return vital.CompactText;
        }

        public static int VitalFontSize(float cellSize, bool health)
        {
            if (cellSize < 90f) return 0;
            if (cellSize < 112f) return 9;
            if (cellSize < 144f) return health ? 14 : 12;
            return health ? 16 : 14;
        }

        public static Color VitalTextColor() => FormalUiTheme.OnInk;

        public static Color HealthFillColor(bool isHero) =>
            isHero ? FormalUiTheme.Health : FormalUiTheme.Danger;

        public static Color HealthForecastColor(bool isHero) =>
            isHero ? FormalUiTheme.Danger : FormalUiTheme.Amber;

        public static Rect UnitStatusIconRect(BattlefieldRect cell, int index)
        {
            int column = Math.Min(1, Math.Max(0, index) / 3);
            int row = Math.Max(0, index) % 3;
            float scale = ElementScale(cell);
            float size = 14f * scale;
            float x = column == 0 ? cell.X : cell.XMax - size;
            return new Rect(x, cell.Y + (20f + row * 16f) * scale, size, size);
        }

        public static Rect StatusHoverCardRect(Vector2 pointer)
        {
            const float width = 310f, height = 86f, margin = 16f, battlefieldRight = 1440f, commandsTop = 900f;
            float x = pointer.x + 18f;
            if (x + width > battlefieldRight - margin) x = pointer.x - width - 18f;
            x = Mathf.Clamp(x, margin, battlefieldRight - margin - width);
            float y = Mathf.Clamp(pointer.y + 18f, 64f, commandsTop - margin - height);
            return new Rect(x, y, width, height);
        }

        private static float ElementScale(BattlefieldRect cell) =>
            cell.Width / BattlefieldPresentationAdapter.CellSize;
    }
}
