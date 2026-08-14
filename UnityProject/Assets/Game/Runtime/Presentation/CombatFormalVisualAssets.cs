using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    /// <summary>
    /// Owns formal battlefield asset lookup, caching, and editor-preview construction.
    /// </summary>
    public sealed class CombatFormalVisualAssets
    {
        private readonly Dictionary<string, Texture2D> units = new Dictionary<string, Texture2D>();
        private readonly Dictionary<string, Texture2D> relay = new Dictionary<string, Texture2D>();
        private readonly Dictionary<string, Texture2D> overlays = new Dictionary<string, Texture2D>();
        private readonly Dictionary<string, Texture2D> intents = new Dictionary<string, Texture2D>();
        private readonly Dictionary<StatusType, Texture2D> statuses = new Dictionary<StatusType, Texture2D>();
        private readonly Texture2D[] firegroundFrames = new Texture2D[6];
        private readonly Texture2D[] smokeFrames = new Texture2D[6];

        public int EnvironmentFrameCount => firegroundFrames.Length;
        public Texture2D LootClosed => Relay("loot_crate_closed");
        public Texture2D Relay(string id) => relay[id];
        public Texture2D Overlay(string id) => overlays[id];
        public Texture2D Intent(string id) => !string.IsNullOrEmpty(id) && intents.TryGetValue(id, out Texture2D value) ? value : null;
        public Texture2D Status(StatusType status) => statuses.TryGetValue(status, out Texture2D value) ? value : null;
        public Texture2D FiregroundFrame(int frame) => firegroundFrames[frame];
        public Texture2D SmokeFrame(int frame) => smokeFrames[frame];

        public Texture2D Unit(UnitState unit)
        {
            if (unit == null) return null;
            if (unit.IsHero) return TextureFor("hero");
            if (string.IsNullOrEmpty(unit.EnemyArchetypeId)) return null;
            Texture2D texture = TextureFor(unit.EnemyArchetypeId);
            return texture != null ? texture : TextureFor(EnemyArchetypes.Get(unit.EnemyArchetypeId).ArtId);
        }

        public void LoadRuntime()
        {
            LoadUnits();
            string[] relayIds = { "floor_plain", "floor_industrial", "floor_warning", "floor_hazard", "rail_horizontal", "rail_vertical",
                "light_cover_intact", "light_cover_damaged", "light_cover_rubble", "heavy_cover_intact", "heavy_cover_damaged", "heavy_cover_rubble",
                "relay_intact", "relay_damaged", "relay_rubble", "loot_crate_closed", "loot_crate_open", "loot_crate_empty" };
            foreach (string id in relayIds) relay[id] = RequiredTexture("Art/FormalRelayV01/" + id);
            foreach (string id in new[] { "selected", "move_range", "attack_range", "objective", "high_risk", "unreachable", "line_of_sight" })
                overlays[id] = RequiredTexture("Art/FormalTacticalOverlays32/" + id);
            foreach (FormalArtEntry entry in FormalArtRegistry.Intents)
                intents[entry.RuntimeId] = RequiredTexture(entry.ResourcePath);
            statuses[StatusType.Burning] = RequiredTexture(FormalArtRegistry.StatusPath("burning"));
            statuses[StatusType.Slow] = RequiredTexture(FormalArtRegistry.StatusPath("slow"));
            statuses[StatusType.Bound] = RequiredTexture(FormalArtRegistry.StatusPath("bound"));
            statuses[StatusType.ArmorBreak] = RequiredTexture(FormalArtRegistry.StatusPath("armor_break"));
            statuses[StatusType.Dazzled] = RequiredTexture(FormalArtRegistry.StatusPath("dazzled"));
            statuses[StatusType.Revealed] = RequiredTexture(FormalArtRegistry.StatusPath("revealed"));
            for (int frame = 0; frame < firegroundFrames.Length; frame++)
            {
                firegroundFrames[frame] = RequiredTexture($"Art/FormalVfx32/fire_burning_ground/frame_{frame:00}");
                smokeFrames[frame] = RequiredTexture($"Art/FormalVfx32/fire_smoke/frame_{frame:00}");
            }
        }

        public void EnsureEditorVisuals(Transform host)
        {
            EnsureEditorMapVisuals(host);
            EnsureEditorUiVisuals(host);
        }

        public void EnsureEditorMapVisuals(Transform host)
        {
            if (host.Find("地图可视化") != null) return;
            GameObject root = new GameObject("地图可视化");
            root.transform.SetParent(host, false);
            Sprite floorSprite = LoadFormalSprite("floor") ?? CreateEditorSprite();
            for (int y = 0; y < 9; y++)
            for (int x = 0; x < 12; x++)
            {
                GameObject tile = new GameObject("格_" + x + "_" + y);
                tile.transform.SetParent(root.transform, false);
                tile.transform.position = new Vector3(x, y, 2f);
                tile.transform.localScale = Vector3.one * .96f;
                SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>();
                renderer.sprite = floorSprite;
                renderer.color = Color.white;
                renderer.sortingOrder = -10;
            }
            AddEditorMarker(root, "轻掩体_A", new Vector3(4, 2, 1), "light_cover");
            AddEditorMarker(root, "轻掩体_B", new Vector3(6, 5, 1), "light_cover");
            AddEditorMarker(root, "重掩体_A", new Vector3(7, 3, 1), "heavy_cover");
            AddEditorMarker(root, "重掩体_B", new Vector3(8, 6, 1), "heavy_cover");
            AddEditorMarker(root, "目标_中继器", new Vector3(10, 4, 1), "relay");
        }

        public void EnsureEditorUiVisuals(Transform host)
        {
            if (host.Find("场景UI") != null) return;
            GameObject canvasObject = new GameObject("场景UI");
            canvasObject.transform.SetParent(host, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasObject.AddComponent<GraphicRaycaster>();
            AddUiPanel(canvasObject, "标题栏", new Vector2(16, -16), new Vector2(640, 44), "OCC 战斗原型", 18);
            AddUiPanel(canvasObject, "战斗UI面板占位", new Vector2(658, -74), new Vector2(310, 560), "战斗信息由战斗管理器更新", 14);
        }

        public void ApplySceneSprites(Transform host)
        {
            Transform root = host.Find("地图可视化");
            if (root == null) return;
            Sprite floor = LoadFormalSprite("floor_industrial") ?? LoadFormalSprite("floor");
            Sprite railFloor = LoadFormalSprite("floor_rail") ?? floor;
            Sprite warningFloor = LoadFormalSprite("floor_warning") ?? floor;
            Sprite light = LoadFormalSprite("light_cover");
            Sprite heavy = LoadFormalSprite("heavy_cover");
            Sprite relaySprite = LoadFormalSprite("relay");
            foreach (SpriteRenderer renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                string objectName = renderer.gameObject.name;
                Sprite replacement = objectName.StartsWith("格_") ? RelayFloorSprite(objectName, floor, railFloor, warningFloor) :
                    objectName.StartsWith("轻掩体") ? light :
                    objectName.StartsWith("重掩体") ? heavy :
                    objectName.StartsWith("目标_中继器") ? relaySprite : null;
                if (replacement == null) continue;
                renderer.sprite = replacement;
                renderer.color = Color.white;
            }
        }

        private void LoadUnits()
        {
            foreach (FormalArtEntry entry in FormalArtRegistry.Units)
            {
                Texture2D texture = LoadOptionalTexture(entry.ResourcePath);
                if (texture != null) units[entry.RuntimeId] = texture;
            }
            foreach (string artId in EnemyArchetypes.All.Select(archetype => archetype.ArtId).Distinct(StringComparer.Ordinal))
            {
                Texture2D texture = LoadOptionalTexture("Art/FormalUnits64/" + artId);
                if (texture != null) units[artId] = texture;
            }
        }

        private Texture2D TextureFor(string id) => units.TryGetValue(id, out Texture2D value) ? value : null;

        private static Texture2D LoadOptionalTexture(string path)
        {
            Texture2D texture = Resources.Load<Texture2D>(path);
            if (texture == null) return null;
            Configure(texture);
            return texture;
        }

        private static Texture2D RequiredTexture(string path) =>
            LoadOptionalTexture(path) ?? throw new KeyNotFoundException("Missing formal texture: " + path);

        private static void Configure(Texture2D texture)
        {
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
        }

        private static void AddUiPanel(GameObject parent, string name, Vector2 position, Vector2 size,
            string text, int fontSize)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent.transform, false);
            RectTransform rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            Image image = panel.AddComponent<Image>();
            image.color = new Color(.07f, .13f, .22f, .72f);
            GameObject textObject = new GameObject(name + "文字");
            textObject.transform.SetParent(panel.transform, false);
            RectTransform textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            Text label = textObject.AddComponent<Text>();
            label.text = text;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.fontSize = fontSize;
            label.font = FormalUiKit.Font;
        }

        private static void AddEditorMarker(GameObject root, string name, Vector3 position, string formalAsset)
        {
            GameObject marker = new GameObject(name);
            marker.transform.SetParent(root.transform, false);
            marker.transform.position = position;
            marker.transform.localScale = Vector3.one * .96f;
            SpriteRenderer renderer = marker.AddComponent<SpriteRenderer>();
            renderer.sprite = LoadFormalSprite(formalAsset) ?? CreateEditorSprite();
            renderer.color = Color.white;
            renderer.sortingOrder = -5;
        }

        private static Sprite LoadFormalSprite(string name)
        {
            Texture2D texture = LoadOptionalTexture("Art/FormalRelay32/" + name);
            return texture == null ? null : Sprite.Create(texture,
                new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 32f);
        }

        private static Sprite RelayFloorSprite(string objectName, Sprite floor, Sprite railFloor, Sprite warningFloor)
        {
            string[] parts = objectName.Split('_');
            if (parts.Length != 3 || !int.TryParse(parts[1], out int x) ||
                !int.TryParse(parts[2], out int y)) return floor;
            if (y == 0 || y == 8) return railFloor;
            if ((x == 5 || x == 6) && y >= 3 && y <= 5) return warningFloor;
            return floor;
        }

        private static Sprite CreateEditorSprite()
        {
            Texture2D texture = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1f);
        }
    }
}
