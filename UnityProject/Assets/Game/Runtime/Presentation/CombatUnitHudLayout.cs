using System;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    public static class CombatUnitHudLayout
    {
        public static Rect EnemyHoverCardRect(Vector2 pointer)
        {
            const float width = 456f, height = 242f, margin = 16f, battlefieldRight = 1440f, commandsTop = 864f;
            float x = pointer.x + 20f;
            if (x + width > battlefieldRight - margin) x = pointer.x - width - 20f;
            x = Mathf.Clamp(x, margin, battlefieldRight - margin - width);
            float y = Mathf.Clamp(pointer.y + 20f, 64f, commandsTop - margin - height);
            return new Rect(x, y, width, height);
        }

        public static Rect EnemyIntentBadgeRect(BattlefieldRect cell, int expectedDamage)
        {
            float width = expectedDamage > 0 ? 92f : 56f;
            Rect unit = UnitPresentationRect(cell);
            // This is the same visible-head boundary used by the annotation obstacle pass. Leave
            // its two-reference-pixel padding here so the placement solver keeps this close pose
            // instead of moving the badge one whole candidate step upward.
            float unitScale = unit.width / 128f;
            float visibleHead = unit.y + 24f * unitScale;
            return new Rect(cell.X + (cell.Width - width) * .5f,
                visibleHead - 56f - 2f * unitScale, width, 56f);
        }

        public static Rect EnemyIntentIconLocalRect()
        {
            return new Rect(4f, 4f, 48f, 48f);
        }

        public static Rect EnemyIntentDamageLocalRect(float badgeWidth)
        {
            return new Rect(56f, 0f, Mathf.Max(0f, badgeWidth - 60f), 56f);
        }

        public static Rect UnitPresentationRect(BattlefieldRect cell)
        {
            // One world pixel has one scale: 64px units use twice the canvas of 32px ground.
            // Keep the whole texture and the foot anchor; visual overhang never changes occupancy.
            float size = Mathf.Max(128f, Mathf.Ceil(cell.Width / 32f) * 64f);
            return new Rect(cell.X + (cell.Width - size) * .5f, cell.Y + cell.Height - size, size, size);
        }

        public static Rect UnitVisibleContentRect(BattlefieldRect cell, string textureName)
        {
            return UnitPresentationRect(cell);
        }

        /// <summary>
        /// Keeps a unit's authored canvas ratio at 32 PPU.  Unit files are not all square:
        /// for example, the fire trainee is 32x64 and must occupy a 64x128 UI rect at the
        /// 64px overview instead of being stretched into a 128x128 square.
        /// </summary>
        public static Rect UnitVisibleContentRect(BattlefieldRect cell, Texture2D texture)
        {
            if (texture == null || texture.width <= 0 || texture.height <= 0)
                return UnitPresentationRect(cell);

            float scale = cell.Width / 32f;
            float width = texture.width * scale;
            float height = texture.height * scale;
            return new Rect(cell.X + (cell.Width - width) * .5f, cell.Y + cell.Height - height, width, height);
        }

        // Formal unit textures own their transparent safety margin. Cropping by legacy body bounds
        // hides weapons, shields, tails and casting implements that legitimately extend sideways.
        public static Rect UnitTextureCropUv(string textureName) => new Rect(0f, 0f, 1f, 1f);

        public static Rect UnitHealthBarRect(BattlefieldRect cell, bool shieldVisible = false)
        {
            const float healthWidth = 128f;
            return new Rect(cell.X + (cell.Width - healthWidth) * .5f,
                cell.YMax - 8f, healthWidth, 16f);
        }

        public static Rect UnitShieldBadgeRect(BattlefieldRect cell)
        {
            Rect health = UnitHealthBarRect(cell, true);
            return new Rect(health.xMin - 34f, health.yMin, 32f, 16f);
        }

        public static Rect UnitManaBarRect(BattlefieldRect cell)
        {
            const float manaWidth = 128f;
            return new Rect(cell.X + (cell.Width - manaWidth) * .5f,
                cell.YMax + 10f, manaWidth, 8f);
        }

        public static float UnitBarBorder(BattlefieldRect cell) => 1f;

        public static string VitalText(CombatUnitVitalPresentation vital, float cellSize)
        {
            return vital == null ? string.Empty : vital.Remaining + "/" + vital.Maximum;
        }

        public static int VitalFontSize(float cellSize, bool health)
        {
            return 12;
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
            float size = 32f * scale;
            float x = column == 0 ? cell.X : cell.XMax - size;
            return new Rect(x, cell.Y + (16f + row * 32f) * scale, size, size);
        }

        public static Rect StatusHoverCardRect(Vector2 pointer)
        {
            const float width = 310f, height = 86f, margin = 16f, battlefieldRight = 1440f, commandsTop = 864f;
            float x = pointer.x + 18f;
            if (x + width > battlefieldRight - margin) x = pointer.x - width - 18f;
            x = Mathf.Clamp(x, margin, battlefieldRight - margin - width);
            float y = Mathf.Clamp(pointer.y + 18f, 64f, commandsTop - margin - height);
            return new Rect(x, y, width, height);
        }

        private static float ElementScale(BattlefieldRect cell) =>
            // Vital/status tracks are authored against the 128px readable unit scale;
            // keep their proportions stable when the board is shown at 64px overview.
            cell.Width / 128f;
    }
}
