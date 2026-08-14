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
            return new Rect(cell.X + (cell.Width - width) * .5f, cell.Y - 22f, width, 20f);
        }

        public static Rect UnitPresentationRect(BattlefieldRect cell)
        {
            float scale = ElementScale(cell);
            float size = 116f * scale;
            return new Rect(cell.X + (cell.Width - size) * .5f, cell.Y + 1f, size, size);
        }

        public static Rect UnitVisibleContentRect(BattlefieldRect cell, string textureName)
        {
            Rect frame = UnitPresentationRect(cell);
            Rect uv = UnitTextureCropUv(textureName);
            float aspect = uv.height > 0f ? uv.width / uv.height : 1f;
            float width = aspect <= 1f ? frame.height * aspect : frame.width;
            float height = aspect <= 1f ? frame.height : frame.width / aspect;
            return new Rect(frame.x + (frame.width - width) * .5f, frame.yMax - height, width, height);
        }

        public static Rect UnitTextureCropUv(string textureName)
        {
            switch (textureName)
            {
                case "hero": return UnitCropUv(21, 11, 22, 48);
                case "rifleman": return UnitCropUv(22, 11, 20, 48);
                case "shieldguard": return UnitCropUv(20, 11, 24, 48);
                case "pyromancer": return UnitCropUv(17, 11, 30, 48);
                case "raider": return UnitCropUv(15, 11, 35, 48);
                case "elite": return UnitCropUv(12, 11, 40, 48);
                case "barrier_mender": return UnitCropUv(19, 19, 27, 40);
                case "lantern_revealer": return UnitCropUv(19, 19, 27, 40);
                case "rune_arbalist": return UnitCropUv(17, 19, 29, 40);
                case "sigil_mauler": return UnitCropUv(13, 19, 37, 40);
                case "stone_snare": return UnitCropUv(17, 19, 31, 40);
                case "tether_hound": return UnitCropUv(3, 23, 57, 36);
                default: return new Rect(0f, 0f, 1f, 1f);
            }
        }

        public static Rect UnitHealthBarRect(BattlefieldRect cell)
        {
            float scale = ElementScale(cell);
            float width = 108f * scale;
            return new Rect(cell.X + (cell.Width - width) * .5f, cell.Y + 101f * scale, width, 13f * scale);
        }

        public static Rect UnitShieldBarRect(BattlefieldRect cell)
        {
            float scale = ElementScale(cell);
            float width = 108f * scale;
            return new Rect(cell.X + (cell.Width - width) * .5f, cell.Y + 115f * scale, width, 11f * scale);
        }

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

        private static Rect UnitCropUv(float x, float top, float width, float height) =>
            new Rect(x / 64f, (64f - top - height) / 64f, width / 64f, height / 64f);

        private static float ElementScale(BattlefieldRect cell) =>
            cell.Width / BattlefieldPresentationAdapter.CellSize;
    }
}
