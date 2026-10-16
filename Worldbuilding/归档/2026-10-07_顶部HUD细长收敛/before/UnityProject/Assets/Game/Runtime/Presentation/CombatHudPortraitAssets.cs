using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    public static class CombatHudPortraitAssets
    {
        private static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<Texture2D, Rect> headRegions = new Dictionary<Texture2D, Rect>();
        public static void Bind(RawImage image, UnitState unit, bool headWindow = false)
        {
            if (image == null) return;
            string id = unit == null ? null : unit.IsHero ? "hero" : unit.EnemyArchetypeId;
            if (string.IsNullOrEmpty(id)) { image.texture = null; image.enabled = false; return; }
            if (!cache.TryGetValue(id, out Texture2D texture))
            {
                texture = Resources.Load<Texture2D>("Art/CombatHudPortraits64/" + id);
                cache[id] = texture;
            }
            image.texture = texture;
            image.uvRect = new Rect(0, 0, 1, 1);
            if (texture == null)
            {
                texture = CombatFormalVisualAssets.DisplayPortrait(unit);
                image.texture = texture;
                if (texture != null) image.uvRect = HeadRegion(texture, id);
            }
            image.enabled = image.texture != null;
            if (headWindow && image.texture != null)
            {
                Texture2D source = (Texture2D)image.texture;
                Rect region = image.uvRect;
                float sizeX = 48f / source.width, sizeY = 48f / source.height;
                float topInset = 8f / source.height;
                image.uvRect = new Rect(region.x + (region.width - sizeX) * .5f, region.yMax - topInset - sizeY, sizeX, sizeY);
            }
            image.color = Color.white;
        }

        // Existing display illustrations are UI art; tactical sprites are never a fallback.
        private static Rect HeadRegion(Texture2D texture, string id)
        {
            if (headRegions.TryGetValue(texture, out Rect region)) return region;
            int left = texture.width, right = 0, bottom = texture.height, top = 0;
            Color32[] pixels = texture.GetPixels32();
            for (int y = 0; y < texture.height; y++)
            for (int x = 0; x < texture.width; x++)
                if (pixels[y * texture.width + x].a != 0)
                { left = Mathf.Min(left, x); right = Mathf.Max(right, x); bottom = Mathf.Min(bottom, y); top = Mathf.Max(top, y); }
            if (left > right) return new Rect(0, 0, 1, 1);
            int size = Mathf.Min(64, Mathf.Min(texture.width, texture.height));
            bool hound = id.Contains("hound");
            int x0 = Mathf.Clamp(hound ? right - size + 1 : (left + right - size) / 2, 0, texture.width - size);
            int y0 = Mathf.Clamp(hound ? bottom : top - size + 1, 0, texture.height - size);
            region = new Rect(x0 / (float)texture.width, y0 / (float)texture.height, size / (float)texture.width, size / (float)texture.height);
            headRegions[texture] = region;
            return region;
        }
    }
}
