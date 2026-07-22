using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    [ExecuteAlways]
    public sealed class CombatPrototypeBootstrap : MonoBehaviour
    {
        private const float CellSize = 78f;
        private const float UiWidth = 1920f;
        private const float UiHeight = 1080f;
        private CombatState state;
        // Legacy panel helpers still use this editor-only snapshot; active flow restarts use developerFlow.
        private CombatState snapshot;
        private string selectedAction = "\u79fb\u52a8";
        private Font chineseFont;
        private Texture2D barTexture;
        private bool initialized;
        private MissionPreparation developerPreparation;
        private CombatFlowController developerFlow;
        private readonly Dictionary<string, Texture2D> hudIcons = new Dictionary<string, Texture2D>();

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            if (initialized) return;
            initialized = true;
            chineseFont = Resources.Load<Font>("Fonts/SimHei");
            barTexture = Resources.Load<Texture2D>("UI/Bar");
            Transform sceneUi = transform.Find("场景UI");
            if (sceneUi != null) sceneUi.gameObject.SetActive(false);
            developerPreparation = new MissionPreparation().Configure("relay_test", "破坏任务目标并清理威胁", "步枪兵、盾卫、火术师、突袭者、精英先锋");
            BuildCombatFromSceneStageTwo();
        }

        private void Awake()
        {
            if (!Application.isPlaying) return;
            Application.targetFrameRate = 60;
        }

        private void OnDisable()
        {
            foreach (Texture2D icon in hudIcons.Values) Destroy(icon);
            hudIcons.Clear();
        }

        public void EnsureEditorVisuals()
        {
            EnsureEditorMapVisuals();
            EnsureEditorUiVisuals();
        }

        public void EnsureEditorMapVisuals()
        {
            if (transform.Find("地图可视化") != null) return;
            GameObject root = new GameObject("地图可视化"); root.transform.SetParent(transform, false);
            Sprite sprite = CreateEditorSprite();
            for (int y = 0; y < 9; y++) for (int x = 0; x < 12; x++)
            {
                GameObject tile = new GameObject("格_" + x + "_" + y); tile.transform.SetParent(root.transform, false); tile.transform.position = new Vector3(x, y, 2f); tile.transform.localScale = Vector3.one * .96f;
                SpriteRenderer renderer = tile.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.color = new Color(.12f, .19f, .29f, 1f); renderer.sortingOrder = -10;
            }
            AddEditorMarker(root, "轻掩体_A", new Vector3(4, 2, 1), new Color(.42f, .42f, .18f, 1f));
            AddEditorMarker(root, "轻掩体_B", new Vector3(6, 5, 1), new Color(.42f, .42f, .18f, 1f));
            AddEditorMarker(root, "重掩体_A", new Vector3(7, 3, 1), new Color(.34f, .28f, .48f, 1f));
            AddEditorMarker(root, "重掩体_B", new Vector3(8, 6, 1), new Color(.34f, .28f, .48f, 1f));
            AddEditorMarker(root, "目标_中继器", new Vector3(10, 4, 1), new Color(.7f, .25f, .16f, 1f));
        }

        public void EnsureEditorUiVisuals()
        {
            if (transform.Find("场景UI") != null) return;
            GameObject canvasObject = new GameObject("场景UI"); canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 20;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); canvasObject.AddComponent<GraphicRaycaster>();
            AddUiPanel(canvasObject, "标题栏", new Vector2(16, -16), new Vector2(640, 44), "OCC // \u786e\u5b9a\u6027\u6218\u6597\u539f\u578b", 18);
            AddUiPanel(canvasObject, "战斗UI面板占位", new Vector2(658, -74), new Vector2(310, 560), "\u6218\u6597\u4fe1\u606f\u7531\u6218\u6597\u7ba1\u7406\u5668\u66f4\u65b0", 14);
        }

        private static void AddUiPanel(GameObject parent, string name, Vector2 position, Vector2 size, string text, int fontSize)
        {
            GameObject panel = new GameObject(name); panel.transform.SetParent(parent.transform, false); RectTransform rect = panel.AddComponent<RectTransform>(); rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1); rect.anchoredPosition = position; rect.sizeDelta = size; Image image = panel.AddComponent<Image>(); image.color = new Color(.07f, .13f, .22f, .72f);
            GameObject textObject = new GameObject(name + "文字"); textObject.transform.SetParent(panel.transform, false); RectTransform textRect = textObject.AddComponent<RectTransform>(); textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one; textRect.offsetMin = Vector2.zero; textRect.offsetMax = Vector2.zero; Text label = textObject.AddComponent<Text>(); label.text = text; label.alignment = TextAnchor.MiddleCenter; label.color = Color.white; label.fontSize = fontSize; label.font = Resources.Load<Font>("Fonts/SimHei");
        }

        private static void AddEditorMarker(GameObject root, string name, Vector3 position, Color color)
        {
            GameObject marker = new GameObject(name); marker.transform.SetParent(root.transform, false); marker.transform.position = position; marker.transform.localScale = Vector3.one * .72f; SpriteRenderer renderer = marker.AddComponent<SpriteRenderer>(); renderer.sprite = CreateEditorSprite(); renderer.color = color; renderer.sortingOrder = -5;
        }

        private static Sprite CreateEditorSprite()
        {
            Texture2D texture = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave }; texture.SetPixel(0, 0, Color.white); texture.Apply(); return Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1f);
        }

        private void BuildCombatFromScene()
        {
            GridMap map = new GridMap(12, 9); CombatSceneMarker[] markers = FindObjectsByType<CombatSceneMarker>();
            foreach (CombatSceneMarker marker in markers) { GridPosition p = ScenePosition(marker); if (marker.MarkerType == CombatSceneMarkerType.LightCover) map.SetTile(p, new TileState { Cover = CoverType.Light, Durability = 4 }); if (marker.MarkerType == CombatSceneMarkerType.HeavyCover) map.SetTile(p, new TileState { Cover = CoverType.Heavy, Durability = 7 }); if (marker.MarkerType == CombatSceneMarkerType.Objective) map.SetTile(p, new TileState { IsObjective = true, Durability = 6 }); }
            List<UnitState> units = new List<UnitState>();
            foreach (CombatSceneMarker marker in markers.Where(m => m.MarkerType == CombatSceneMarkerType.Unit)) { GridPosition p = ScenePosition(marker); bool hero = marker.name.Contains("主角"); string id = hero ? "hero" : marker.name.Contains("步枪") ? "rifle" : marker.name.Contains("盾") ? "guard" : "caster"; string name = hero ? "\u963f\u65af\u7279\u62c9" : id == "rifle" ? "\u6b65\u67aa\u5175" : id == "guard" ? "\u76fe\u536b" : "\u706b\u672f\u5e08"; units.Add(new UnitState(id, hero, p, hero ? Facing.East : Facing.West) { DisplayName = name, Armor = hero ? 1 : id == "guard" ? 2 : 0, Block = id == "guard" ? 2 : hero ? 1 : 0, Speed = hero ? 11 : id == "guard" ? 7 : id == "caster" ? 9 : 8 }); }
            if (units.Count == 0) return;
            state = new CombatState(map, units);
            state.ConfigureQuickbar(CombatCatalog.Medkit, CombatCatalog.ShieldCell);
            state.SetLoot(new LootContainer(new GridPosition(2, 0), new InventoryItem("aether_core", "\u4ee5\u592a\u6838\u5fc3", 2, 1)));
            CombatResolver.BeginTurn(state, "hero");
        }

        private void BuildCombatFromSceneStageTwo()
        {
            GridMap map = new GridMap(12, 9);
            CombatSceneMarker[] markers = FindObjectsByType<CombatSceneMarker>();
            foreach (CombatSceneMarker marker in markers)
            {
                GridPosition p = ScenePosition(marker);
                if (marker.MarkerType == CombatSceneMarkerType.LightCover) map.SetTile(p, new TileState { Cover = CoverType.Light, Durability = 4 });
                if (marker.MarkerType == CombatSceneMarkerType.HeavyCover) map.SetTile(p, new TileState { Cover = CoverType.Heavy, Durability = 7 });
                if (marker.MarkerType == CombatSceneMarkerType.Objective) map.SetTile(p, new TileState { IsObjective = true, Durability = 6 });
            }
            List<UnitState> units = new List<UnitState>();
            int enemyIndex = 0;
            foreach (CombatSceneMarker marker in markers.Where(m => m.MarkerType == CombatSceneMarkerType.Unit).OrderBy(m => m.name, StringComparer.Ordinal))
            {
                bool hero = marker.name.Contains("\u4e3b\u89d2");
                UnitState unit = new UnitState(hero ? "hero" : "enemy_" + enemyIndex, hero, ScenePosition(marker), hero ? Facing.East : Facing.West);
                if (hero) { unit.DisplayName = "\u963f\u65af\u7279\u62c9"; unit.Speed = 11; }
                else { EnemyArchetypes.All[Math.Min(enemyIndex, EnemyArchetypes.All.Count - 1)].Apply(unit); enemyIndex++; }
                units.Add(unit);
            }
            if (units.Count == 0 || !units.Any(unit => unit.IsHero)) return;
            state = new CombatState(map, units);
            state.ConfigureQuickbar(CombatCatalog.Medkit, CombatCatalog.ShieldCell);
            state.SetLoot(new LootContainer(new GridPosition(2, 0), new InventoryItem("aether_core", "\u4ee5\u592a\u6838\u5fc3", 2, 1)));
            developerFlow = new CombatFlowController();
            developerFlow.Configure(developerPreparation, state);
        }

        private static GridPosition ScenePosition(CombatSceneMarker marker) => new GridPosition(Mathf.RoundToInt(marker.transform.position.x), Mathf.RoundToInt(marker.transform.position.y));
        public void OpenDeveloperBriefing() { developerFlow.OpenBriefing(); }
        public void StartDeveloperCombat() { developerFlow.BeginCombat(); state = developerFlow.State; CombatResolver.BeginTurn(state, "hero"); }
        public void TacticalRestartDeveloperCombat() { developerFlow.TacticalRestart(); state = developerFlow.State; CombatResolver.BeginTurn(state, "hero"); developerFlow.ResumeAfterRestart(); }
        public void ReturnToDeveloperMenu() { developerFlow.ReturnToDeveloperMenu(); state = developerFlow.State; selectedAction = "移动"; }
        private void Update() { if (!Application.isPlaying || developerFlow == null || developerFlow.Phase != CombatFlowPhase.Active || state == null || state.IsVictory || state.IsDefeat || state.ActiveUnitId == "hero") return; RunEnemyTurn(); developerFlow.RefreshOutcome(); }
        private void RunEnemyTurn() { UnitState enemy = state.GetUnit(state.ActiveUnitId); UnitState hero = state.GetUnit("hero"); if (enemy == null || !enemy.IsAlive) { if (enemy != null) CombatResolver.EndTurn(state, enemy); return; } try { if (Distance(enemy.Position, hero.Position) <= 4 && enemy.ActionPoints > 0) CombatResolver.Resolve(state, enemy.Id == "caster" ? CombatCommand.Cast(enemy.Id, hero.Id) : CombatCommand.Attack(enemy.Id, hero.Id)); else if (enemy.ActionPoints > 0) CombatResolver.Resolve(state, CombatCommand.Move(enemy.Id, StepToward(enemy.Position, hero.Position), FacingToward(enemy.Position, hero.Position))); if (state.ActiveUnitId == enemy.Id) CombatResolver.EndTurn(state, enemy); } catch (InvalidOperationException error) { state.AddLog(error.Message); CombatResolver.EndTurn(state, enemy); } }
        private void OnGUI() { if (!Application.isPlaying || developerFlow == null) return; float scale = Mathf.Min(Screen.width / UiWidth, Screen.height / UiHeight); Vector2 offset = new Vector2((Screen.width - UiWidth * scale) * .5f, (Screen.height - UiHeight * scale) * .5f); Matrix4x4 previous = GUI.matrix; GUI.matrix = Matrix4x4.TRS(offset, Quaternion.identity, Vector3.one * scale); ConfigureGuiSkin(); if (developerFlow.Phase == CombatFlowPhase.DeveloperMenu) { DrawDeveloperMenu(); GUI.matrix = previous; return; } if (developerFlow.Phase == CombatFlowPhase.Briefing) { DrawDeveloperBriefing(); GUI.matrix = previous; return; } DrawHeader(); DrawGrid(new Rect(24, 112, 12 * CellSize, 9 * CellSize)); DrawPanelStageTwo(new Rect(1390, 112, 500, 790)); DrawDeveloperFlowBar(); GUI.matrix = previous; }
        private void ConfigureGuiSkin()
        {
            GUI.skin.label.fontSize = 18; GUI.skin.button.fontSize = 18; GUI.skin.box.fontSize = 20;
            GUI.skin.button.padding = new RectOffset(12, 12, 8, 8);
            GUI.skin.box.normal.textColor = new Color(.88f, .94f, 1f);
        }
        private void DrawHeader()
        {
            GUI.color = new Color(.035f, .075f, .13f, .98f); GUI.Box(new Rect(24, 20, 1390, 68), ""); GUI.color = Color.white;
            GUI.Label(new Rect(48, 30, 520, 28), "OCC // 正式战斗流程"); GUI.color = new Color(.35f, .9f, 1f); GUI.Label(new Rect(1040, 30, 320, 28), "开发测试构建  ·  1920×1080"); GUI.color = Color.white;
        }
        private void DrawDeveloperMenu()
        {
            GUI.color = new Color(.035f, .075f, .13f, .98f); GUI.Box(new Rect(540, 250, 840, 470), ""); GUI.color = Color.white;
            GUI.Label(new Rect(590, 300, 740, 38), "OCC  开发测试菜单"); GUI.color = new Color(.68f, .78f, .88f); GUI.Label(new Rect(590, 352, 740, 28), "从战前简报进入一场完整战斗，并可随时战术重开。"); GUI.color = Color.white;
            GUI.Label(new Rect(590, 410, 740, 28), "任务：" + developerPreparation.MissionId + "  ·  破坏目标"); GUI.Label(new Rect(590, 448, 740, 28), "敌人：" + developerPreparation.EnemySummary);
            if (GUI.Button(new Rect(590, 510, 350, 54), "查看战前简报")) OpenDeveloperBriefing();
            if (GUI.Button(new Rect(980, 510, 350, 54), "重新加载场景数据")) { BuildCombatFromSceneStageTwo(); }
            GUI.color = new Color(.35f, .9f, 1f); GUI.Label(new Rect(590, 620, 740, 28), "开发菜单  →  战前简报  →  战斗  →  结算  →  战术重开"); GUI.color = Color.white;
        }
        private void DrawDeveloperBriefing()
        {
            GUI.color = new Color(.035f, .075f, .13f, .98f); GUI.Box(new Rect(500, 220, 920, 580), ""); GUI.color = Color.white;
            GUI.Label(new Rect(550, 270, 800, 38), "OCC  战前简报");
            GUI.color = new Color(.68f, .78f, .88f); GUI.Label(new Rect(550, 326, 800, 28), "任务编号：" + developerPreparation.MissionId); GUI.Label(new Rect(550, 364, 800, 28), "任务目标：" + developerPreparation.RulesSummary); GUI.Label(new Rect(550, 402, 800, 28), "敌方编成：" + developerPreparation.EnemySummary); GUI.color = Color.white;
            GUI.Label(new Rect(550, 470, 800, 28), "行动准则：破坏中继器；战术重开会恢复到本次战斗的初始状态。");
            if (GUI.Button(new Rect(550, 620, 350, 54), "开始正式战斗")) StartDeveloperCombat();
            if (GUI.Button(new Rect(960, 620, 350, 54), "返回开发菜单")) ReturnToDeveloperMenu();
        }
        private void DrawDeveloperFlowBar()
        {
            developerFlow.RefreshOutcome();
            GUI.color = new Color(.035f, .075f, .13f, .98f); GUI.Box(new Rect(24, 930, 1866, 72), ""); GUI.color = Color.white;
            GUI.Label(new Rect(52, 950, 600, 30), "当前流程：" + developerFlow.Phase);
            if ((developerFlow.Phase == CombatFlowPhase.Victory || developerFlow.Phase == CombatFlowPhase.Defeat) && GUI.Button(new Rect(1450, 944, 190, 42), "战术重开")) TacticalRestartDeveloperCombat();
            if (GUI.Button(new Rect(1660, 944, 190, 42), "返回开发菜单")) ReturnToDeveloperMenu();
        }
        private void DrawGrid(Rect board)
        {
            Event e = Event.current;
            for (int y = 0; y < state.Map.Height; y++) for (int x = 0; x < state.Map.Width; x++)
            {
                GridPosition p = new GridPosition(x, y);
                Rect cell = new Rect(board.x + x * CellSize, board.y + (state.Map.Height - 1 - y) * CellSize, CellSize - 2, CellSize - 2);
                TileState tile = state.Map.GetTile(p);
                GUI.color = tile.IsObjective ? new Color(.55f, .2f, .15f) : tile.Cover == CoverType.Light ? new Color(.3f, .32f, .18f) : tile.Cover == CoverType.Heavy ? new Color(.26f, .25f, .34f) : new Color(.13f, .16f, .18f);
                GUI.Box(cell, "");

                // Both core tactical ranges stay visible during the hero turn.
                if (IsInMoveRange(p))
                {
                    GUI.color = new Color(.1f, .8f, 1f, selectedAction == "\u79fb\u52a8" ? .75f : .35f);
                    GUI.DrawTexture(new Rect(cell.x + 2, cell.y + 2, cell.width - 4, cell.height - 4), Texture2D.whiteTexture);
                }
                if (IsInAttackRange(p)) DrawOutline(cell, new Color(.25f, .58f, 1f, selectedAction == "\u653b\u51fb" ? 1f : .7f));
                if ((selectedAction == "\u6280\u80fd1" || selectedAction == "\u6280\u80fd2") && IsInSelectedRange(p))
                {
                    GUI.color = new Color(1f, .45f, .1f, .75f);
                    GUI.DrawTexture(new Rect(cell.x + 5, cell.y + 5, cell.width - 10, cell.height - 10), Texture2D.whiteTexture);
                }

                GUI.color = Color.white;
                UnitState unit = state.Units.Values.FirstOrDefault(u => u.IsAlive && u.Position == p);
                if (unit != null)
                {
                    GUI.color = unit.IsHero ? new Color(.25f, .72f, 1f) : new Color(1f, .35f, .3f);
                    GUI.Box(new Rect(cell.x + 7, cell.y + 11, cell.width - 14, cell.height - 14), FacingGlyph(unit.Facing));
                    GUI.color = Color.white;
                    DrawUnitBars(unit, new Rect(cell.x + 5, cell.y + 3, cell.width - 10, 5));
                    if (!unit.IsHero) GUI.Label(new Rect(cell.x - 14, cell.y - 15, cell.width + 28, 16), GetEnemyIntent(unit));
                }
                if (tile.IsObjective && !tile.IsDestroyed) GUI.Label(new Rect(cell.x + 2, cell.y + 18, cell.width, 20), "\u4e2d\u7ee7\u5668");
                if (state.Loot != null && !state.Loot.IsLooted && state.Loot.Position == p)
                {
                    GUI.color = new Color(1f, .78f, .18f);
                    GUI.Box(new Rect(cell.x + 15, cell.y + 16, cell.width - 30, cell.height - 30), "\u7269");
                    GUI.color = Color.white;
                }
                if (e.type == EventType.MouseDown && e.button == 0 && cell.Contains(e.mousePosition)) { HandleCellClick(p); e.Use(); }
            }
        }

        private static void DrawOutline(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x + 1, rect.y + 1, rect.width - 2, 3), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x + 1, rect.yMax - 4, rect.width - 2, 3), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x + 1, rect.y + 1, 3, rect.height - 2), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - 4, rect.y + 1, 3, rect.height - 2), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
        private void DrawPanel(Rect rect) { GUI.Box(rect, "\u6218\u6597\u63a7\u5236\u53f0"); UnitState active = state.GetUnit(state.ActiveUnitId); UnitState hero = state.GetUnit("hero"); GUI.Label(new Rect(rect.x + 14, rect.y + 34, 280, 22), $"\u884c\u52a8\u5355\u4f4d：{active.DisplayName} | AP {active.ActionPoints}"); GUI.Label(new Rect(rect.x + 14, rect.y + 60, 280, 20), $"\u4e3b\u89d2\u8d44\u6e90：{hero.Health}/{hero.MaxHealth} HP  {hero.Shield} \u62a4\u76fe  {hero.Mana}/{hero.MaxMana} \u4ee5\u592a"); GUI.Label(new Rect(rect.x + 14, rect.y + 82, 280, 20), GetRangeDescription()); string[] actions = { "\u79fb\u52a8", "\u653b\u51fb", "\u65bd\u672f", "\u9053\u5177", "\u4e92\u52a8" }; for (int i = 0; i < actions.Length; i++) if (GUI.Toggle(new Rect(rect.x + 14 + (i % 2) * 136, rect.y + 108 + (i / 2) * 34, 128, 28), selectedAction == actions[i], actions[i], "Button")) selectedAction = actions[i]; if (GUI.Button(new Rect(rect.x + 14, rect.y + 216, 128, 30), "\u7ed3\u675f\u884c\u52a8")) TryCommand(CombatCommand.EndTurn("hero")); if (GUI.Button(new Rect(rect.x + 150, rect.y + 216, 128, 30), "\u6218\u672f\u91cd\u5f00")) { state = snapshot.Clone(); CombatResolver.BeginTurn(state, "hero"); } GUI.Label(new Rect(rect.x + 14, rect.y + 256, 280, 20), "\u884c\u52a8\u6761\uff1a\u6570\u503c\u8d8a\u4f4e\u8d8a\u5148\u884c\u52a8"); int row = 0; foreach (UnitState unit in state.Units.Values) { GUI.Label(new Rect(rect.x + 14, rect.y + 280 + row * 27, 125, 20), $"{unit.DisplayName} HP{unit.Health} \u62a4{unit.Shield}"); GUI.HorizontalScrollbar(new Rect(rect.x + 142, rect.y + 284 + row * 27, 130, 16), Math.Min(100, unit.InitiativeTime) / 100f, .12f, 0f, 1f); row++; } GUI.Label(new Rect(rect.x + 14, rect.y + 410, 280, 20), "\u654c\u4eba\u610f\u56fe\u548c\u6218\u6597\u8bb0\u5f55"); for (int i = 0; i < Math.Min(6, state.EventLog.Count); i++) GUI.Label(new Rect(rect.x + 14, rect.y + 434 + i * 18, 280, 18), state.EventLog[i]); }
        private void DrawPanelStageTwo(Rect rect)
        {
            UnitState active = state.GetUnit(state.ActiveUnitId);
            UnitState hero = state.GetUnit("hero");
            DrawHudFrame(rect);
            DrawHudHeader(rect, active, hero);
            DrawHudSection(rect, 160, "战术指令", "点击图标后选择战场格");
            string[] actions = { "移动", "攻击", "技能1", "技能2", "搜刮", "互动" };
            string[] iconIds = { "move", "attack", "skillOne", "skillTwo", "loot", "interact" };
            for (int i = 0; i < actions.Length; i++)
            {
                Rect button = new Rect(rect.x + 18 + (i % 2) * 238, rect.y + 198 + (i / 2) * 52, 226, 46);
                if (DrawHudActionButton(button, iconIds[i], actions[i], selectedAction == actions[i])) selectedAction = actions[i];
            }

            DrawHudSection(rect, 370, "快捷栏", "使用消耗 1 AP");
            for (int i = 0; i < state.Quickbar.Length; i++)
            {
                ConsumableDefinition item = state.Quickbar[i];
                string label = item == null ? (i + 1) + " 空" : (i + 1) + " " + item.DisplayName;
                Rect button = new Rect(rect.x + 18 + (i % 2) * 238, rect.y + 408 + (i / 2) * 28, 226, 24);
                if (DrawHudTextButton(button, label, item != null, false) && item != null) TryCommand(CombatCommand.UseQuickbar("hero", i));
            }

            DrawHudSection(rect, 530, "构筑与回合", "构筑切换不消耗 AP");
            if (DrawHudTextButton(new Rect(rect.x + 18, rect.y + 568, 148, 26), "步枪", true, false)) ApplyBuild(0);
            if (DrawHudTextButton(new Rect(rect.x + 176, rect.y + 568, 148, 26), "战锤", true, false)) ApplyBuild(1);
            if (DrawHudTextButton(new Rect(rect.x + 334, rect.y + 568, 148, 26), "法杖", true, false)) ApplyBuild(2);
            if (DrawHudTextButton(new Rect(rect.x + 18, rect.y + 600, 226, 28), "结束行动", true, true)) TryCommand(CombatCommand.EndTurn("hero"));
            if (DrawHudTextButton(new Rect(rect.x + 258, rect.y + 600, 224, 28), "战术重开", true, true)) TacticalRestartDeveloperCombat();

            DrawHudSection(rect, 648, "行动条", "数值低者先行动");
            int row = 0;
            foreach (UnitState unit in state.Units.Values.OrderBy(unit => unit.InitiativeTime).Take(4))
            {
                float y = 678 + row * 18;
                DrawHudLabel(rect, y, unit.DisplayName + "  " + unit.Health + " HP", 13);
                DrawHudMeter(new Rect(rect.x + 278, rect.y + y + 4, 204, 10), Math.Min(100, unit.InitiativeTime) / 100f, unit.IsHero ? HudCyan : HudRed);
                row++;
            }

            DrawHudSection(rect, 736, "记录", "最新信息");
            if (state.EventLog.Count > 0) DrawHudLabel(rect, 764, state.EventLog[0], 12);
        }

        private static readonly Color HudInk = new Color(.035f, .04f, .04f, .985f);
        private static readonly Color HudSurface = new Color(.07f, .075f, .07f, 1f);
        private static readonly Color HudLine = new Color(.82f, .8f, .72f, .62f);
        private static readonly Color HudText = new Color(.92f, .9f, .84f, 1f);
        private static readonly Color HudMuted = new Color(.58f, .57f, .52f, 1f);
        private static readonly Color HudCyan = new Color(.1f, .82f, .87f, 1f);
        private static readonly Color HudRed = new Color(.84f, .25f, .19f, 1f);
        private static readonly Color HudAmber = new Color(.95f, .62f, .14f, 1f);

        private static void DrawHudFrame(Rect rect)
        {
            GUI.color = HudInk;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            DrawHudLine(new Vector2(rect.x, rect.y), new Vector2(rect.xMax, rect.y));
            DrawHudLine(new Vector2(rect.x, rect.yMax), new Vector2(rect.xMax, rect.yMax));
            DrawHudLine(new Vector2(rect.x, rect.y), new Vector2(rect.x, rect.yMax));
            DrawHudLine(new Vector2(rect.xMax, rect.y), new Vector2(rect.xMax, rect.yMax));
            DrawHudCorner(rect.x + 8, rect.y + 8, 1, 1); DrawHudCorner(rect.xMax - 8, rect.y + 8, -1, 1);
            DrawHudCorner(rect.x + 8, rect.yMax - 8, 1, -1); DrawHudCorner(rect.xMax - 8, rect.yMax - 8, -1, -1);
            GUI.color = Color.white;
        }

        private static void DrawHudHeader(Rect panel, UnitState active, UnitState hero)
        {
            GUI.color = HudText;
            GUI.Label(new Rect(panel.x + 18, panel.y + 16, 260, 22), "战斗控制台");
            GUI.color = HudMuted;
            GUI.Label(new Rect(panel.x + 18, panel.y + 42, panel.width - 36, 18), "行动：" + active.DisplayName + "  /  AP " + active.ActionPoints);
            DrawHudMeter(new Rect(panel.x + 18, panel.y + 70, panel.width - 36, 8), hero.Health / (float)hero.MaxHealth, HudText);
            DrawHudMeter(new Rect(panel.x + 18, panel.y + 88, panel.width - 36, 8), hero.Shield / (float)Math.Max(1, hero.MaxShield), HudCyan);
            DrawHudMeter(new Rect(panel.x + 18, panel.y + 106, panel.width - 36, 8), hero.Mana / (float)hero.MaxMana, HudCyan);
            GUI.color = HudMuted;
            GUI.Label(new Rect(panel.x + 18, panel.y + 124, panel.width - 36, 18), "主手：" + hero.MainHand.DisplayName + "  状态：" + GetStatusText(hero));
            GUI.color = Color.white;
        }

        private static void DrawHudSection(Rect panel, float y, string title, string subtitle)
        {
            DrawHudLine(new Vector2(panel.x + 18, panel.y + y), new Vector2(panel.xMax - 18, panel.y + y));
            GUI.color = HudText;
            GUI.Label(new Rect(panel.x + 18, panel.y + y + 8, 150, 18), title);
            GUI.color = HudMuted;
            GUI.Label(new Rect(panel.x + 176, panel.y + y + 8, panel.width - 204, 18), subtitle);
            GUI.color = Color.white;
        }

        private static void DrawHudMeter(Rect rect, float value, Color fill)
        {
            GUI.color = new Color(.015f, .018f, .018f, 1f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = fill;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(value), rect.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private bool DrawHudActionButton(Rect rect, string iconId, string label, bool selected)
        {
            Color accent = iconId == "loot" ? HudAmber : selected ? HudCyan : HudText;
            GUI.color = selected ? new Color(HudCyan.r, HudCyan.g, HudCyan.b, .12f) : HudSurface;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            DrawHudLine(new Vector2(rect.x, rect.y), new Vector2(rect.xMax, rect.y), accent);
            DrawHudLine(new Vector2(rect.x, rect.yMax), new Vector2(rect.xMax, rect.yMax), accent);
            DrawHudLine(new Vector2(rect.x, rect.y), new Vector2(rect.x, rect.yMax), accent);
            DrawHudLine(new Vector2(rect.xMax, rect.y), new Vector2(rect.xMax, rect.yMax), accent);
            Texture2D icon = GetHudIcon(iconId, accent);
            GUI.DrawTexture(new Rect(rect.x + 8, rect.y + 7, 32, 32), icon, ScaleMode.StretchToFill, true);
            GUI.color = accent;
            GUI.Label(new Rect(rect.x + 50, rect.y + 13, rect.width - 56, 20), label);
            GUI.color = Color.white;
            return GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        private static bool DrawHudTextButton(Rect rect, string text, bool enabled, bool emphasis)
        {
            Color line = emphasis ? HudCyan : HudLine;
            GUI.color = enabled ? HudSurface : new Color(HudSurface.r, HudSurface.g, HudSurface.b, .35f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            DrawHudLine(new Vector2(rect.x, rect.y), new Vector2(rect.xMax, rect.y), line);
            DrawHudLine(new Vector2(rect.x, rect.yMax), new Vector2(rect.xMax, rect.yMax), line);
            GUI.color = enabled ? HudText : HudMuted;
            GUI.Label(rect, text, CenteredHudLabel());
            GUI.color = Color.white;
            return enabled && GUI.Button(rect, GUIContent.none, GUIStyle.none);
        }

        private static GUIStyle CenteredHudLabel()
        {
            GUIStyle style = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 14 };
            return style;
        }

        private static void DrawHudLine(Vector2 from, Vector2 to) => DrawHudLine(from, to, HudLine);
        private static void DrawHudLine(Vector2 from, Vector2 to, Color color)
        {
            GUI.color = color;
            if (Mathf.Abs(from.y - to.y) < .1f) GUI.DrawTexture(new Rect(Mathf.Min(from.x, to.x), from.y, Mathf.Abs(to.x - from.x), 1f), Texture2D.whiteTexture);
            else GUI.DrawTexture(new Rect(from.x, Mathf.Min(from.y, to.y), 1f, Mathf.Abs(to.y - from.y)), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private static void DrawHudCorner(float x, float y, int horizontal, int vertical)
        {
            DrawHudLine(new Vector2(x, y), new Vector2(x + horizontal * 10, y));
            DrawHudLine(new Vector2(x, y), new Vector2(x, y + vertical * 10));
        }

        private Texture2D GetHudIcon(string iconId, Color color)
        {
            string cacheKey = iconId + color.ToString();
            if (hudIcons.TryGetValue(cacheKey, out Texture2D icon)) return icon;
            icon = new Texture2D(32, 32, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
            Color transparent = new Color(0f, 0f, 0f, 0f);
            Color[] pixels = Enumerable.Repeat(transparent, 32 * 32).ToArray();
            Action<int, int> pixel = (x, y) => { if (x >= 0 && x < 32 && y >= 0 && y < 32) pixels[y * 32 + x] = color; };
            Action<int, int, int, int> line = (x0, y0, x1, y1) =>
            {
                int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1, dy = -Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1, error = dx + dy;
                while (true) { pixel(x0, y0); if (x0 == x1 && y0 == y1) break; int twice = 2 * error; if (twice >= dy) { error += dy; x0 += sx; } if (twice <= dx) { error += dx; y0 += sy; } }
            };
            if (iconId == "move") { line(4, 16, 25, 16); line(19, 9, 26, 16); line(19, 23, 26, 16); }
            else if (iconId == "attack") { for (int i = 4; i <= 28; i += 6) { pixel(i, 16); pixel(16, i); } line(8, 8, 24, 24); line(24, 8, 8, 24); }
            else if (iconId == "skillOne") { line(16, 3, 16, 29); line(3, 16, 29, 16); line(7, 7, 25, 25); line(25, 7, 7, 25); }
            else if (iconId == "skillTwo") { line(16, 4, 16, 28); line(6, 22, 16, 4); line(26, 22, 16, 4); line(6, 22, 26, 22); }
            else if (iconId == "loot") { line(5, 10, 16, 5); line(16, 5, 27, 10); line(5, 10, 5, 25); line(27, 10, 27, 25); line(5, 25, 27, 25); line(16, 5, 16, 25); line(5, 10, 16, 16); line(27, 10, 16, 16); }
            else { line(16, 4, 16, 27); line(6, 14, 16, 4); line(26, 14, 16, 4); line(9, 27, 23, 27); }
            icon.SetPixels(pixels); icon.Apply(false, true); hudIcons.Add(cacheKey, icon); return icon;
        }

        private static void DrawHudLabel(Rect panel, float y, string text, int size)
        {
            int previousSize = GUI.skin.label.fontSize;
            GUI.skin.label.fontSize = size;
            GUI.Label(new Rect(panel.x + 18, panel.y + y, panel.width - 36, 18), text);
            GUI.skin.label.fontSize = previousSize;
        }

        private void ApplyBuild(int build)
        {
            UnitState hero = state.GetUnit("hero");
            StageTwoBuilds.Apply(hero, build);
            state.AddLog($"\u5de5\u574a\u5df2\u5207\u6362\u4e3a{hero.MainHand.DisplayName}\u6784\u7b51\u3002");
        }

        private static string GetStatusText(UnitState unit)
        {
            if (unit.Statuses.Count == 0) return "\u65e0";
            return string.Join(" ", unit.Statuses.Select(entry => $"{StatusName(entry.Key)}{entry.Value}"));
        }

        private static string StatusName(StatusType status) => status == StatusType.Burning ? "\u71c3\u70e7" : status == StatusType.Slow ? "\u7f13\u6162" : status == StatusType.Bound ? "\u675f\u7f1a" : "\u7834\u7532";

        private string GetRangeDescriptionStageTwo()
        {
            int count = 0;
            for (int y = 0; y < state.Map.Height; y++) for (int x = 0; x < state.Map.Width; x++) if (IsInSelectedRange(new GridPosition(x, y))) count++;
            UnitState hero = state.GetUnit("hero");
            string rule = selectedAction == "\u79fb\u52a8" ? "\u79fb\u52a8 3 \u683c" : selectedAction == "\u653b\u51fb" ? $"{hero.MainHand.DisplayName} {hero.MainHand.Range} \u683c" : selectedAction == "\u6280\u80fd1" ? $"{hero.SkillOne.DisplayName} {hero.SkillOne.Range} \u683c" : selectedAction == "\u6280\u80fd2" ? $"{hero.SkillTwo.DisplayName} {hero.SkillTwo.Range} \u683c" : selectedAction == "\u641c\u522e" ? "\u641c\u522e\uff1a\u76f8\u90bb 1 \u683c" : "\u4ea4\u4e92\uff1a\u76f8\u90bb 1 \u683c";
            return $"\u5f53\u524d\uff1a{rule} | \u9ad8\u4eae {count} \u683c";
        }

        private void HandleCellClick(GridPosition p) { if (state.ActiveUnitId != "hero" || state.IsVictory || state.IsDefeat) return; UnitState target = state.Units.Values.FirstOrDefault(u => !u.IsHero && u.IsAlive && u.Position == p); if (selectedAction == "\u79fb\u52a8") TryCommand(CombatCommand.Move("hero", p, FacingToward(state.GetUnit("hero").Position, p))); else if (selectedAction == "\u653b\u51fb" && target != null) TryCommand(CombatCommand.Attack("hero", target.Id)); else if (selectedAction == "\u6280\u80fd1" && target != null) TryCommand(CombatCommand.UseSkill("hero", 0, target.Id)); else if (selectedAction == "\u6280\u80fd2" && target != null) TryCommand(CombatCommand.UseSkill("hero", 1, target.Id)); else if (selectedAction == "\u641c\u522e") TryCommand(CombatCommand.Loot("hero")); else if (selectedAction == "\u4e92\u52a8") TryCommand(CombatCommand.Interact("hero", p)); }
        private void TryCommand(CombatCommand command) { try { CombatResolver.Resolve(state, command); if (state.ActiveUnitId == "hero" && state.GetUnit("hero").ActionPoints == 0) CombatResolver.EndTurn(state, state.GetUnit("hero")); developerFlow.RefreshOutcome(); } catch (InvalidOperationException error) { state.AddLog(error.Message); } }
        private void DrawUnitBars(UnitState unit, Rect rect) { GUI.color = Color.black; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = unit.IsHero ? new Color(.2f, .85f, .45f) : new Color(.9f, .22f, .22f); GUI.DrawTexture(new Rect(rect.x + 1, rect.y + 1, (rect.width - 2) * unit.Health / unit.MaxHealth, rect.height - 2), Texture2D.whiteTexture); GUI.color = Color.white; }
        private string GetEnemyIntent(UnitState enemy) => Distance(enemy.Position, state.GetUnit("hero").Position) <= 4 ? (enemy.Id == "caster" ? "\u706b\u672f" : "\u653b\u51fb") : "\u9760\u8fd1";
        private string GetRangeDescription() { int count = 0; if (state != null) for (int y = 0; y < state.Map.Height; y++) for (int x = 0; x < state.Map.Width; x++) if (IsInSelectedRange(new GridPosition(x, y))) count++; string rule = selectedAction == "\u79fb\u52a8" ? "\u79fb\u52a8\u8303\u56f4：3 \u683c" : selectedAction == "\u653b\u51fb" ? "\u653b\u51fb\u8303\u56f4：4 \u683c" : selectedAction == "\u65bd\u672f" ? "\u706b\u672f\u8303\u56f4：5 \u683c" : selectedAction == "\u4e92\u52a8" ? "\u4e92\u52d5\u8303\u56f4：1 \u683c" : "\u9053\u5177：\u81ea\u8eab\u4f7f\u7528"; return rule + "  |  \u9ad8\u4eae " + count + " \u683c"; }
        private bool IsInSelectedRange(GridPosition p)
        {
            if (state == null || state.ActiveUnitId != "hero") return false;
            UnitState hero = state.GetUnit("hero");
            int distance = Distance(hero.Position, p);
            if (selectedAction == "\u79fb\u52a8") return IsInMoveRange(p);
            if (selectedAction == "\u653b\u51fb") return IsInAttackRange(p);
            if (selectedAction == "\u6280\u80fd1") return distance > 0 && distance <= hero.SkillOne.Range && state.Map.HasLineOfSight(hero.Position, p);
            if (selectedAction == "\u6280\u80fd2") return distance > 0 && distance <= hero.SkillTwo.Range && state.Map.HasLineOfSight(hero.Position, p);
            if (selectedAction == "\u641c\u522e") return state.Loot != null && !state.Loot.IsLooted && p == state.Loot.Position && distance == 1;
            if (selectedAction == "\u4e92\u52a8") return distance == 1;
            return false;
        }

        private bool IsInMoveRange(GridPosition p)
        {
            if (state == null || state.ActiveUnitId != "hero") return false;
            UnitState hero = state.GetUnit("hero");
            int distance = Distance(hero.Position, p);
            return distance > 0 && distance <= 3 && state.Map.IsInside(p) && !state.Map.IsBlocked(p) && !state.IsOccupied(p);
        }

        private bool IsInAttackRange(GridPosition p)
        {
            if (state == null || state.ActiveUnitId != "hero") return false;
            UnitState hero = state.GetUnit("hero");
            int distance = Distance(hero.Position, p);
            return distance > 0 && distance <= hero.MainHand.Range && state.Map.HasLineOfSight(hero.Position, p);
        }
        private static int Distance(GridPosition a, GridPosition b) => Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
        private static GridPosition StepToward(GridPosition a, GridPosition b) => Math.Abs(b.X - a.X) >= Math.Abs(b.Y - a.Y) ? new GridPosition(a.X + Math.Sign(b.X - a.X), a.Y) : new GridPosition(a.X, a.Y + Math.Sign(b.Y - a.Y));
        private static Facing FacingToward(GridPosition a, GridPosition b) => Math.Abs(b.X - a.X) >= Math.Abs(b.Y - a.Y) ? (b.X >= a.X ? Facing.East : Facing.West) : (b.Y >= a.Y ? Facing.North : Facing.South);
        private static string FacingGlyph(Facing facing) => facing == Facing.North ? "^" : facing == Facing.South ? "v" : facing == Facing.East ? ">" : "<";
    }
}
