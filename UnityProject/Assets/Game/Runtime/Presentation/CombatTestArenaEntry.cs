using System.Collections;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    /// <summary>
    /// Owns the dedicated test-arena entry.  The arena deliberately asks for a
    /// scenario before it creates combat; this keeps a play-mode launch from
    /// silently testing whichever scene markers happened to be left behind.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatTestArenaEntry : MonoBehaviour
    {
        private const string QuickTestScenarioKey = "OCC.CombatUiQuickTest.PendingScenario";
        public static bool IsDedicatedTestArena { get; private set; }
        private static int playSessionGeneration;
        private int observedPlaySessionGeneration = -1;
        private bool selectionOpen;
        private bool arenaStarted;
        private bool quickStartPending;
        private int scenarioPage;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void BeginPlaySession()
        {
            // This hook is invoked even when Enter Play Mode Options disable
            // domain and scene reloads.
            playSessionGeneration++;
            IsDedicatedTestArena = false;
        }

        private void OnEnable()
        {
            // Enter Play Mode Options may keep managed instance fields alive
            // between sessions. The dedicated arena must always reopen its own
            // picker instead of remembering a previous test battle.
            selectionOpen = false;
            arenaStarted = false;
            quickStartPending = false;
            selectedScenarioId = CombatTestArenaScenarioCatalog.DefaultScenarioId;
            scenarioPage = 0;
        }

        public void CloseSelection()
        {
            selectionOpen = false;
            arenaStarted = true;
        }

        public void ShowSelection()
        {
            IsDedicatedTestArena = true;
            selectionOpen = true;
            arenaStarted = false;
        }
        private string selectedScenarioId = CombatTestArenaScenarioCatalog.DefaultScenarioId;

        private void OnDisable()
        {
            IsDedicatedTestArena = false;
        }

        private void Update()
        {
            if (!Application.isPlaying) return;
            if (observedPlaySessionGeneration != playSessionGeneration)
            {
                observedPlaySessionGeneration = playSessionGeneration;
                selectionOpen = false;
                arenaStarted = false;
                selectedScenarioId = CombatTestArenaScenarioCatalog.DefaultScenarioId;
                scenarioPage = 0;
            }
#if UNITY_EDITOR
            string quickTestScenario = PlayerPrefs.GetString(QuickTestScenarioKey, string.Empty);
            if (!string.IsNullOrEmpty(quickTestScenario) && !quickStartPending)
            {
                PlayerPrefs.DeleteKey(QuickTestScenarioKey);
                PlayerPrefs.Save();
                quickStartPending = true;
                StartCoroutine(StartQuickTestAfterSceneStart(quickTestScenario));
                return;
            }
#endif
            if (quickStartPending) return;
            if (arenaStarted) return;
            IsDedicatedTestArena = true;
            CombatPrototypeBootstrap bootstrap = GetComponent<CombatPrototypeBootstrap>();
            if (bootstrap == null) bootstrap = FindAnyObjectByType<CombatPrototypeBootstrap>();
            if (bootstrap != null) bootstrap.OpenDedicatedTestArenaSelection();
            selectionOpen = true;
        }

        private IEnumerator StartQuickTestAfterSceneStart(string scenarioId)
        {
            // Allow every scene component to complete Start before the direct
            // activation builds and binds the formal presentation stack.
            yield return null;
            yield return null;
            quickStartPending = false;
            StartArena(scenarioId, playEntrySequence: false);
        }

        private void StartArena(string scenarioId, bool playEntrySequence = true)
        {
            IsDedicatedTestArena = true;
            CombatPrototypeBootstrap bootstrap = GetComponent<CombatPrototypeBootstrap>();
            if (bootstrap == null) bootstrap = FindAnyObjectByType<CombatPrototypeBootstrap>();
            if (bootstrap == null)
            {
                Debug.LogError("CombatTestArenaEntry requires a CombatPrototypeBootstrap.");
                return;
            }
            // The bootstrap may have been enabled before this entry's first
            // frame. Explicitly suppress any shared front-end presentation
            // before building the test combat.
            bootstrap.OpenDedicatedTestArenaSelection();
            bootstrap.StartDedicatedTestArena(scenarioId, playEntrySequence);
            selectionOpen = false;
            arenaStarted = true;
        }

        private void OnGUI()
        {
            if (!Application.isPlaying || !selectionOpen) return;
            CombatTestArenaScenario[] scenarios = CombatTestArenaScenarioCatalog.All;
            CombatTestArenaScenario[] visibleScenarios = System.Array.FindAll(scenarios,
                scenario => scenarioPage == 0 ? !scenario.IsHighPressure && !scenario.IsSystemTest && !scenario.IsSkillTest :
                    scenarioPage == 1 ? scenario.IsHighPressure : scenarioPage == 2 ? scenario.IsSystemTest : scenario.IsSkillTest);
            GUI.depth = -1000;
            float width = Mathf.Min(960f, Screen.width - 72f);
            float height = Mathf.Min(580f, Screen.height - 72f);
            Rect panel = new Rect((Screen.width - width) * .5f, (Screen.height - height) * .5f, width, height);
            Color prior = GUI.color;
            GUI.color = new Color(.035f, .05f, .07f, .98f);
            GUI.Box(panel, GUIContent.none);
            GUI.color = prior;

            GUIStyle title = WhiteText(new GUIStyle(GUI.skin.label) { fontSize = 32, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter });
            GUIStyle subtitle = WhiteText(new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleCenter, wordWrap = true });
            GUIStyle button = WhiteText(new GUIStyle(GUI.skin.button));
            GUI.Label(new Rect(panel.x + 24f, panel.y + 24f, panel.width - 48f, 48f), "战斗测试场", title);
            GUI.Label(new Rect(panel.x + 60f, panel.y + 76f, panel.width - 120f, 42f), "单击项目直接进入战斗。每次随机生成偏向一种流派的 8 个术式与 4 件法宝。", subtitle);

            int normalCount = System.Array.FindAll(scenarios, scenario => !scenario.IsHighPressure && !scenario.IsSystemTest && !scenario.IsSkillTest).Length;
            int highPressureCount = System.Array.FindAll(scenarios, scenario => scenario.IsHighPressure).Length;
            int systemTestCount = System.Array.FindAll(scenarios, scenario => scenario.IsSystemTest).Length;
            int skillTestCount = System.Array.FindAll(scenarios, scenario => scenario.IsSkillTest).Length;
            if (GUI.Button(new Rect(panel.x + 42f, panel.y + 120f, 124f, 30f), "普通战 " + normalCount, button))
            {
                scenarioPage = 0;
                selectedScenarioId = CombatTestArenaScenarioCatalog.DefaultScenarioId;
            }
            if (GUI.Button(new Rect(panel.x + 174f, panel.y + 120f, 140f, 30f), "精英／首领 " + highPressureCount, button))
            {
                scenarioPage = 1;
                selectedScenarioId = System.Array.Find(scenarios, scenario => scenario.IsHighPressure).Id;
            }
            if (GUI.Button(new Rect(panel.x + 322f, panel.y + 120f, 154f, 30f), "系统压力测试 " + systemTestCount, button))
            {
                scenarioPage = 2;
                selectedScenarioId = System.Array.Find(scenarios, scenario => scenario.IsSystemTest).Id;
            }
            if (GUI.Button(new Rect(panel.x + 484f, panel.y + 120f, 140f, 30f), "术式实验 " + skillTestCount, button))
            {
                scenarioPage = 3;
                selectedScenarioId = System.Array.Find(scenarios, scenario => scenario.IsSkillTest).Id;
            }

            const int columns = 3;
            float gap = 12f;
            float cardWidth = (panel.width - 84f - gap * (columns - 1)) / columns;
            float cardHeight = 100f;
            for (int index = 0; index < visibleScenarios.Length; index++)
            {
                CombatTestArenaScenario scenario = visibleScenarios[index];
                int column = index % columns;
                int row = index / columns;
                Rect card = new Rect(panel.x + 42f + column * (cardWidth + gap), panel.y + 158f + row * (cardHeight + gap), cardWidth, cardHeight);
                bool chosen = selectedScenarioId == scenario.Id;
                Color cardColor = chosen ? new Color(.18f, .34f, .42f, 1f) : new Color(.10f, .14f, .18f, 1f);
                GUI.color = cardColor;
                GUI.Box(card, GUIContent.none);
                GUI.color = prior;
                if (GUI.Button(card, GUIContent.none, GUIStyle.none))
                {
                    selectedScenarioId = scenario.Id;
                    StartArena(scenario.Id);
                    return;
                }
                GUIStyle name = WhiteText(new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold });
                GUIStyle detail = WhiteText(new GUIStyle(GUI.skin.label) { fontSize = 12, wordWrap = true });
                GUI.Label(new Rect(card.x + 12f, card.y + 9f, card.width - 24f, 25f), scenario.DisplayName, name);
                GUI.Label(new Rect(card.x + 12f, card.y + 36f, card.width - 24f, 57f), scenario.SelectionSummary, detail);
            }

            CombatTestArenaScenario selected = CombatTestArenaScenarioCatalog.Get(selectedScenarioId);
            if (GUI.Button(new Rect(panel.x + panel.width - 286f, panel.y + panel.height - 70f, 244f, 42f), "随机满配进入「" + selected.ShortName + "」", button))
                StartArena(selected.Id);
        }

        private static GUIStyle WhiteText(GUIStyle style)
        {
            style.normal.textColor = Color.white;
            style.hover.textColor = Color.white;
            style.active.textColor = Color.white;
            style.focused.textColor = Color.white;
            style.onNormal.textColor = Color.white;
            style.onHover.textColor = Color.white;
            style.onActive.textColor = Color.white;
            style.onFocused.textColor = Color.white;
            return style;
        }
    }
}
