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
        private void OnGUI() { if (!Application.isPlaying || developerFlow == null) return; float scale = Mathf.Min(Screen.width / UiWidth, Screen.height / UiHeight); Vector2 offset = new Vector2((Screen.width - UiWidth * scale) * .5f, (Screen.height - UiHeight * scale) * .5f); Matrix4x4 previous = GUI.matrix; GUI.matrix = Matrix4x4.TRS(offset, Quaternion.identity, Vector3.one * scale); ConfigureGuiSkin(); if (developerFlow.Phase == CombatFlowPhase.DeveloperMenu) { DrawDeveloperMenu(); GUI.matrix = previous; return; } if (developerFlow.Phase == CombatFlowPhase.Briefing) { DrawDeveloperBriefing(); GUI.matrix = previous; return; } DrawHeader(); DrawGrid(new Rect(24, 112, 12 * CellSize, 9 * CellSize)); DrawPanelStageTwo(new Rect(1470, 112, 420, 790)); DrawDeveloperFlowBar(); GUI.matrix = previous; }
        private void ConfigureGuiSkin()
        {
            GUI.skin.label.fontSize = 16; GUI.skin.button.fontSize = 16; GUI.skin.box.fontSize = 18;
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
            GUI.Box(rect, "");
            UnitState active = state.GetUnit(state.ActiveUnitId);
            UnitState hero = state.GetUnit("hero");
            DrawHudSectionTitle(rect, 12, "战斗控制台");
            DrawHudLabel(rect, 40, $"行动单位：{active.DisplayName}  AP {active.ActionPoints}");
            DrawHudLabel(rect, 62, $"生命 {hero.Health}/{hero.MaxHealth}  护盾 {hero.Shield}/{hero.MaxShield}");
            DrawHudLabel(rect, 84, $"以太 {hero.Mana}/{hero.MaxMana}  背包 {state.Backpack.Items.Count}/{state.Backpack.Width * state.Backpack.Height}");
            DrawHudLabel(rect, 106, $"主手：{hero.MainHand.DisplayName}");
            DrawHudLabel(rect, 128, $"技能：{hero.SkillOne.DisplayName} {hero.Cooldown(hero.SkillOne)}  /  {hero.SkillTwo.DisplayName} {hero.Cooldown(hero.SkillTwo)}");
            DrawHudLabel(rect, 150, GetRangeDescriptionStageTwo());
            DrawHudLabel(rect, 172, "状态：" + GetStatusText(hero));
            string[] actions = { "\u79fb\u52a8", "\u653b\u51fb", "\u6280\u80fd1", "\u6280\u80fd2", "\u641c\u522e", "\u4e92\u52a8" };
            for (int i = 0; i < actions.Length; i++) if (GUI.Toggle(new Rect(rect.x + 14 + (i % 2) * 136, rect.y + 204 + (i / 2) * 32, 128, 27), selectedAction == actions[i], actions[i], "Button")) selectedAction = actions[i];
            DrawHudSectionTitle(rect, 310, "快捷栏（使用消耗 1 AP）");
            for (int i = 0; i < state.Quickbar.Length; i++)
            {
                ConsumableDefinition item = state.Quickbar[i];
                string label = item == null ? $"{i + 1} \u7a7a" : $"{i + 1} {item.DisplayName}";
                if (GUI.Button(new Rect(rect.x + 14 + (i % 2) * 136, rect.y + 338 + (i / 2) * 28, 128, 25), label) && item != null) TryCommand(CombatCommand.UseQuickbar("hero", i));
            }
            DrawHudSectionTitle(rect, 462, "免费工坊（不消耗 AP）");
            if (GUI.Button(new Rect(rect.x + 14, rect.y + 490, 84, 26), "\u6b65\u67aa\u6784\u7b51")) ApplyBuild(0);
            if (GUI.Button(new Rect(rect.x + 104, rect.y + 490, 84, 26), "\u6218\u9524\u6784\u7b51")) ApplyBuild(1);
            if (GUI.Button(new Rect(rect.x + 194, rect.y + 490, 84, 26), "\u6cd5\u6756\u6784\u7b51")) ApplyBuild(2);
            if (GUI.Button(new Rect(rect.x + 14, rect.y + 526, 128, 28), "\u7ed3\u675f\u884c\u52a8")) TryCommand(CombatCommand.EndTurn("hero"));
            if (GUI.Button(new Rect(rect.x + 150, rect.y + 526, 128, 28), "\u6218\u672f\u91cd\u5f00")) TacticalRestartDeveloperCombat();
            DrawHudSectionTitle(rect, 570, "行动条（数值低者先行动）");
            int row = 0;
            foreach (UnitState unit in state.Units.Values.Take(5)) { DrawHudLabel(rect, 598 + row * 24, $"{unit.DisplayName}  HP {unit.Health}  盾 {unit.Shield}"); GUI.HorizontalScrollbar(new Rect(rect.x + 238, rect.y + 604 + row * 24, 154, 14), Math.Min(100, unit.InitiativeTime) / 100f, .12f, 0f, 1f); row++; }
            DrawHudSectionTitle(rect, 726, "战斗记录");
            for (int i = 0; i < Math.Min(3, state.EventLog.Count); i++) DrawHudLabel(rect, 752 + i * 18, state.EventLog[i]);
        }

        private static void DrawHudSectionTitle(Rect panel, float y, string text)
        {
            GUI.color = new Color(.35f, .9f, 1f);
            GUI.Label(new Rect(panel.x + 14, panel.y + y, panel.width - 28, 20), text);
            GUI.color = Color.white;
        }

        private static void DrawHudLabel(Rect panel, float y, string text) => GUI.Label(new Rect(panel.x + 14, panel.y + y, panel.width - 28, 20), text);

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
