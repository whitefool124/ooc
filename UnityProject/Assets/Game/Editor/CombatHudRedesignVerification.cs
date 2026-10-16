using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OCC.Combat.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace OCC.Combat.EditorTools
{
    public static class CombatHudRedesignVerification
    {
        private static IDisposable saveScope;
        private static Dictionary<string, string> originals;
        private static CombatPrototypeBootstrap host;
        private static bool inputActive;
        public static string Status { get; private set; } = "idle";
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Worldbuilding/归档/2026-10-07_头像近景与连续行动轨道/verification"));

        [MenuItem("OCC/Combat HUD Redesign/Verify Normal Entry")]
        public static void Start()
        {
            if (!Application.isPlaying || Application.dataPath.Replace('\\', '/') != "E:/数据库/OCC_Codex/UnityProject/Assets")
                throw new InvalidOperationException("Main project Play Mode required");
            host = Object.FindAnyObjectByType<CombatPrototypeBootstrap>();
            if (host == null || saveScope != null) throw new InvalidOperationException("Missing host or verification already running");
            originals = new Dictionary<string, string>();
            for (int i = 0; i < 3; i++)
                foreach (string key in new[] { "OCC.FirstExperiencePrototype.v2.slot." + i, "occ.first_experience.map_run.slot." + i })
                    originals[key] = PlayerPrefs.GetString(key, "<missing>");
            saveScope = FirstExperienceSaveRouting.BeginVerificationScope("hud20261006_" + DateTime.UtcNow.Ticks);
            DetailsInputFixture.Begin(Application.runInBackground); inputActive = true;
            EditorApplication.playModeStateChanged += Restore;
            Directory.CreateDirectory(Folder); Status = "running";
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
            host.StartCoroutine(Guard(Routine()));
        }

        [MenuItem("OCC/Combat HUD Redesign/Verify Current Combat")]
        public static void VerifyCurrent()
        {
            host = Object.FindAnyObjectByType<CombatPrototypeBootstrap>();
            if (!Application.isPlaying || host == null || !host.IsDeveloperCombatActive) throw new InvalidOperationException("Active combat required");
            DetailsInputFixture.Begin(Application.runInBackground); inputActive = true;
            EditorApplication.playModeStateChanged -= Restore; EditorApplication.playModeStateChanged += Restore;
            Directory.CreateDirectory(Folder); Status = "running-current";
            host.StartCoroutine(Guard(CheckHud()));
        }

        private static void Restore(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingPlayMode) return;
            CheckPlayerSaves(); saveScope?.Dispose(); saveScope = null;
            if (inputActive) DetailsInputFixture.End(); inputActive = false;
            EditorApplication.playModeStateChanged -= Restore;
        }
        private static void CheckPlayerSaves()
        {
            foreach (var pair in originals ?? new Dictionary<string, string>())
                if (PlayerPrefs.GetString(pair.Key, "<missing>") != pair.Value) throw new InvalidOperationException("Player save changed: " + pair.Key);
        }
        private static IEnumerator Guard(IEnumerator routine)
        {
            var stack = new Stack<IEnumerator>(); stack.Push(routine);
            while (stack.Count > 0)
            {
                object current = null; bool moved = false; Exception error = null;
                try { moved = stack.Peek().MoveNext(); if (moved) current = stack.Peek().Current; } catch (Exception e) { error = e; }
                if (error != null)
                {
                    Status = "FAIL " + error; File.WriteAllText(Path.Combine(Folder, "result.txt"), Status);
                    if (inputActive) DetailsInputFixture.End(); inputActive = false;
                    Debug.LogException(error); yield break;
                }
                if (!moved) { stack.Pop(); continue; }
                if (current is IEnumerator child) { stack.Push(child); continue; }
                yield return current;
            }
            CheckPlayerSaves(); Status = "PASS"; File.WriteAllText(Path.Combine(Folder, "result.txt"), Status + "\n" + string.Join("\n", trace));
            if (inputActive) DetailsInputFixture.End(); inputActive = false;
        }
        private static readonly List<string> trace = new List<string>();
        private static Button Find(string name) => Object.FindObjectsByType<Button>().FirstOrDefault(b => b.isActiveAndEnabled && b.IsInteractable() && (b.name == name || b.name == "按钮_" + name));
        private static Vector2 Center(RectTransform r) => RectTransformUtility.WorldToScreenPoint(null, r.TransformPoint(r.rect.center));
        private static void Pointer(Vector2 p, bool down = false) => InputSystem.QueueStateEvent(Mouse.current, down ? new MouseState { position = p }.WithButton(MouseButton.Left) : new MouseState { position = p });
        private static IEnumerator Click(string name)
        {
            float until = Time.realtimeSinceStartup + 4; Button b;
            while ((b = Find(name)) == null && Time.realtimeSinceStartup < until) yield return null;
            if (b == null) throw new InvalidOperationException("Missing button " + name);
            Vector2 p = Center((RectTransform)b.transform);
            var hits = new List<RaycastResult>();
            do
            {
                hits.Clear(); EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = p }, hits);
                if (hits.Count > 0 && hits[0].gameObject.GetComponentInParent<Button>() == b) break;
                yield return null;
            } while (Time.realtimeSinceStartup < until);
            if (hits.Count == 0 || hits[0].gameObject.GetComponentInParent<Button>() != b) throw new InvalidOperationException("Blocked button " + name);
            Pointer(p); yield return null; Pointer(p, true); yield return null; Pointer(p); yield return new WaitForSecondsRealtime(.30f);
        }
        private static IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            var image = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(Folder, name + ".png"), image.EncodeToPNG()); Object.Destroy(image);
        }
        private static IEnumerator Routine()
        {
            trace.Clear(); yield return new WaitForSecondsRealtime(1.5f);
            if (Find("跳过动画") != null) yield return Click("跳过动画");
            yield return Click("开始新游戏"); yield return Click("选择档案2");
            yield return Click("确认"); yield return Click("以此角色开始"); yield return Click("进入首次地图");
            if (Find("不需要教程") != null) yield return Click("不需要教程");
            yield return Click("map.node.B1"); yield return Click("进入战斗");
            float until = Time.realtimeSinceStartup + 15;
            while (host.IsInteractionModalOpen && Time.realtimeSinceStartup < until) yield return null;
            if (!host.IsDeveloperCombatActive || host.IsInteractionModalOpen) throw new InvalidOperationException("Normal B1 entry blocked");
            trace.Add("Normal UI entry to B1; production disk save namespace; saved=" + host.IsMapRunSaved);
            yield return CheckHud();
        }
        private static IEnumerator CheckHud()
        {
            float settle = Time.realtimeSinceStartup + 15f;
            while (host.IsInteractionModalOpen && Time.realtimeSinceStartup < settle) yield return null;
            if (host.IsInteractionModalOpen) throw new InvalidOperationException("Combat entry did not settle");
            var drawer = Object.FindAnyObjectByType<CombatTimelineDrawer>();
            var shell = Object.FindAnyObjectByType<FormalCombatHudShellView>();
            if (drawer == null || shell == null) throw new InvalidOperationException("HUD missing");
            Pointer(new Vector2(Screen.width * .5f, Screen.height - 10f)); yield return new WaitForSecondsRealtime(.5f);
            if (drawer.IsOpen || drawer.IsPinned) throw new InvalidOperationException("Drawer not collapsed by default");
            var viewport = host.BattlefieldViewport; var before = viewport.BoardRect; var view = viewport.ViewportRect;
            if (view.Width != 1888) throw new InvalidOperationException("Old reserved sidebar still active");
            yield return Capture("collapsed-" + Screen.width + "x" + Screen.height);
            Button tab = Find("行动值页签"); if (tab == null) throw new InvalidOperationException("Tab missing");
            Pointer(Center((RectTransform)tab.transform)); yield return new WaitForSecondsRealtime(.12f);
            if (drawer.IsOpen) throw new InvalidOperationException("Opened before hover delay");
            yield return new WaitForSecondsRealtime(.16f);
            if (!drawer.IsOpen) throw new InvalidOperationException("Hover did not expand drawer");
            if (viewport.BoardRect.X != before.X || viewport.BoardRect.Y != before.Y || viewport.ViewportRect.Width != view.Width)
                throw new InvalidOperationException("Drawer changed camera or viewport");
            Pointer(Center(shell.RightConsole)); yield return new WaitForSecondsRealtime(.4f);
            if (!drawer.IsOpen) throw new InvalidOperationException("Closed between tab and panel");
            yield return Capture("expanded-" + Screen.width + "x" + Screen.height);
            yield return Click("固定行动值"); Pointer(new Vector2(Screen.width * .5f, Screen.height * .5f)); yield return new WaitForSecondsRealtime(.45f);
            if (!drawer.IsPinned || !drawer.IsOpen) throw new InvalidOperationException("Pin did not retain drawer");
            yield return Click("固定行动值"); Pointer(new Vector2(Screen.width * .5f, Screen.height * .5f)); yield return new WaitForSecondsRealtime(.12f);
            if (!drawer.IsOpen) throw new InvalidOperationException("Closed before exit grace");
            yield return new WaitForSecondsRealtime(.3f);
            if (drawer.IsOpen) throw new InvalidOperationException("Unpin and exit did not close");
            var hits = new List<RaycastResult>(); Vector2 formerPanel = Center(shell.RightConsole);
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = formerPanel }, hits);
            if (hits.Any(h => h.gameObject.transform.IsChildOf(shell.RightConsole))) throw new InvalidOperationException("Hidden drawer blocks battlefield");
            var order = CombatActionOrderPresentation.Build(host.CurrentState);
            if (order.Select(u => u.Id).Distinct().Count() != order.Count) throw new InvalidOperationException("Duplicate order actors");
            yield return Click("英雄概况翻面"); yield return Capture("hero-details-" + Screen.width + "x" + Screen.height); yield return Click("英雄概况翻面");
            EventSystem.current.SetSelectedGameObject(tab.gameObject); yield return null;
            if (!drawer.IsOpen) throw new InvalidOperationException("Keyboard focus did not open drawer");
            EventSystem.current.SetSelectedGameObject(null); Pointer(new Vector2(Screen.width * .5f, Screen.height * .5f)); yield return new WaitForSecondsRealtime(.45f);
            trace.Add("PASS hover delay, panel transit, pin/unpin, grace close, keyboard focus, no invisible raycast; no camera shift; status flip; size=" + Screen.width + "x" + Screen.height);
            yield return CheckPortraitCards();
        }

        private static IEnumerator CheckPortraitCards()
        {
            var card = Object.FindObjectsByType<CombatPortraitCardView>().First(c => c.UnitId != "hero" && c.Portrait.enabled);
            var battlefield = Object.FindAnyObjectByType<FormalBattlefieldView>();
            var field = typeof(FormalBattlefieldView).GetField("timelineHoveredUnitId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Vector2 p = Center((RectTransform)card.transform);
            Pointer(p); yield return new WaitForSecondsRealtime(.5f);
            if ((string)field.GetValue(battlefield) != card.UnitId) throw new InvalidOperationException("Portrait hover locator missing");
            yield return Capture("portrait-hover-" + Screen.width + "x" + Screen.height);
            int clicks = 0; UnityEngine.Events.UnityAction observer = () => clicks++;
            card.Button.onClick.AddListener(observer);
            InputSystem.QueueStateEvent(Mouse.current, new MouseState { position = p }.WithButton(MouseButton.Right)); yield return null;
            Pointer(p); yield return null;
            if (clicks != 0) throw new InvalidOperationException("Right click submitted portrait");
            Pointer(p, true); yield return null; Pointer(p); yield return new WaitForSecondsRealtime(.3f);
            if (clicks != 1) throw new InvalidOperationException("Portrait click unavailable");
            EventSystem.current.SetSelectedGameObject(card.gameObject); Pointer(new Vector2(Screen.width * .5f, Screen.height - 10)); yield return new WaitForSecondsRealtime(.3f);
            if ((string)field.GetValue(battlefield) != card.UnitId) throw new InvalidOperationException("Pointer exit cleared keyboard locator");
            ExecuteEvents.Execute(card.gameObject, new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler); yield return null;
            if (clicks != 2) throw new InvalidOperationException("Keyboard submit unavailable");
            EventSystem.current.SetSelectedGameObject(null); yield return null;
            if (!string.IsNullOrEmpty((string)field.GetValue(battlefield))) throw new InvalidOperationException("Deselect left stale locator");
            card.Button.onClick.RemoveListener(observer);
            trace.Add("PASS portrait real pointer hover, left click and right click suppression; independent keyboard selection and submit event; deselect clears locator; size=" + Screen.width + "x" + Screen.height);
        }
    }
}
