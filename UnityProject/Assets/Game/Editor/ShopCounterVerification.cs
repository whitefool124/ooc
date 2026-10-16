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
    public static class ShopCounterVerification
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Worldbuilding/归档/2026-10-03_小细节第二批商店/验证"));
        private static bool background;
        private static float animation;
        private static CombatPrototypeBootstrap host;

        [MenuItem("OCC/Details60/Shop Counter/Record")]
        public static void Record()
        {
            if (!Application.isPlaying || Application.dataPath.Replace('\\', '/') != "E:/数据库/OCC_Codex/UnityProject/Assets")
                throw new InvalidOperationException("Main project Play required");
            DetailsInputFixture.End();
            background = Application.runInBackground;
            DetailsInputFixture.Begin(background);
            host = Object.FindAnyObjectByType<CombatPrototypeBootstrap>();
            animation = host.UiPreferences.AnimationIntensity;
            Directory.CreateDirectory(Folder);
            File.Delete(Path.Combine(Folder, "result.txt"));
            AudioExpansionVerification.StartLedger(Folder);
            EditorApplication.playModeStateChanged -= Restore;
            EditorApplication.playModeStateChanged += Restore;
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
            host.StartCoroutine(Guard(Routine()));
        }

        private static void Restore(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.ExitingPlayMode) return;
            Motion(animation);
            DetailsInputFixture.End();
            EditorApplication.playModeStateChanged -= Restore;
        }

        private static IEnumerator Guard(IEnumerator routine)
        {
            while (true)
            {
                object current;
                try { if (!routine.MoveNext()) break; current = routine.Current; }
                catch (Exception error)
                {
                    File.WriteAllText(Path.Combine(Folder, "result.txt"), "FAIL: " + error);
                    Motion(animation); DetailsInputFixture.End(); Debug.LogException(error); yield break;
                }
                yield return current;
            }
        }
        private static void Motion(float value)
        {
            if (host == null) return;
            var p = host.UiPreferences;
            p.Configure(p.MasterVolume, value, p.ScreenShake, p.FloatingText, p.HighContrast, p.LargeText, p.KeyHints);
        }
        private static void Pointer(Vector2 point, bool held) => InputSystem.QueueStateEvent(Mouse.current,
            held ? new MouseState { position = point }.WithButton(MouseButton.Left) : new MouseState { position = point });
        private static Button[] BuyButtons() => Object.FindObjectsByType<Button>().Where(x => x.isActiveAndEnabled &&
            (x.name == "按钮_购买" || x.name == "按钮_已售罄")).OrderBy(x => x.transform.position.x).ToArray();
        private static Vector2 Center(RectTransform rect) => RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center));
        private static IEnumerator Click(Button button, bool enabled)
        {
            Vector2 point = Center((RectTransform)button.transform);
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = point }, hits);
            if (enabled && (hits.Count == 0 || hits[0].gameObject.GetComponentInParent<Button>() != button))
                throw new InvalidOperationException("Top purchase raycast blocked: " + string.Join(",", hits.Select(x => x.gameObject.name)));
            Pointer(point, false); yield return null;
            Pointer(point, true); yield return null;
            Pointer(point, false); yield return null;
        }
        private static Image[] Trays() => Object.FindObjectsByType<Image>().Where(x => x.name.StartsWith("商品托盘_")).OrderBy(x => x.transform.position.x).ToArray();
        private static Image[] Products() => Object.FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(x => x.name.StartsWith("托盘商品_")).OrderBy(x => x.transform.position.x).ToArray();
        private static IEnumerator Routine()
        {
            string error = host.PreviewUiCaptureRun(false, "S");
            if (error != null) throw new InvalidOperationException(error);
            yield return new WaitForSecondsRealtime(.6f);
            host.PreviewUiCapturePage("room", "S");
            yield return new WaitForSecondsRealtime(.5f); yield return new WaitForEndOfFrame();
            if (Trays().Length != 3 || Products().Length != 3 || Products().Any(x => !x.gameObject.activeInHierarchy))
                throw new InvalidOperationException("Three supported products required");
            if (Trays().Concat(Products()).Any(x => x.raycastTarget)) throw new InvalidOperationException("Display blocks input");
            var counter = Object.FindAnyObjectByType<ShopCounterDetail>();
            if (counter == null || counter.CommittedPlays != 0) throw new InvalidOperationException("Opening replayed transaction");
            var table = Object.FindObjectsByType<Image>().Single(x => x.name == "市集零钱柜台");
            if (table.raycastTarget || table.sprite.rect.width != 64 || table.sprite.rect.height != 32)
                throw new InvalidOperationException("Missing formal native counter support");
            foreach (var image in counter.GetComponentsInChildren<Image>()) if (image.raycastTarget) throw new InvalidOperationException("Cash tray blocks input");
            yield return Capture("shop-before.png");
            int gold = host.CurrentMapRun.Gold;
            int coinBefore = AudioExpansionVerification.Count("coins");
            yield return Click(BuyButtons()[2], true);
            // Buffer consecutive rendered frames without PNG IO in the sampling window.
            var frames = new List<Texture2D>(); var trace = new List<string>();
            bool moved = false; int plays = 0;
            for (int i = 0; i < 24; i++)
            {
                yield return new WaitForEndOfFrame();
                counter = Object.FindAnyObjectByType<ShopCounterDetail>();
                moved |= counter != null && counter.IsAnimating;
                plays = Math.Max(plays, counter != null ? counter.CommittedPlays : 0);
                frames.Add(ScreenCapture.CaptureScreenshotAsTexture());
                trace.Add(Time.unscaledTime.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + " plays=" + plays + " active=" + (counter != null && counter.IsAnimating));
                yield return new WaitForSecondsRealtime(.015f);
            }
            for (int i = 0; i < frames.Count; i++)
            { File.WriteAllBytes(Path.Combine(Folder, "trade-" + i.ToString("00") + ".png"), frames[i].EncodeToPNG()); Object.Destroy(frames[i]); }
            File.WriteAllLines(Path.Combine(Folder, "trade-times.txt"), trace);
            if (!moved || plays != 1 || counter.IsAnimating) throw new InvalidOperationException("Trade animation did not play once and settle");
            if (host.CurrentMapRun.Gold != gold - 4 || !host.CurrentMapRun.CurrentShopService.Offers[2].Sold)
                throw new InvalidOperationException("Actual goggles purchase failed");
            if (Products()[2].gameObject.activeInHierarchy || Products().Take(2).Any(x => !x.gameObject.activeInHierarchy) || Trays().Length != 3)
                throw new InvalidOperationException("Sale changed unrelated product or lost empty tray");
            if (AudioExpansionVerification.Count("coins") != coinBefore + 1) throw new InvalidOperationException("Committed coin sound missing or duplicated");
            yield return Capture("shop-sold.png");
            int settledGold = host.CurrentMapRun.Gold;
            yield return Click(BuyButtons()[1], false); // six gold, four available
            yield return new WaitForSecondsRealtime(.15f);
            yield return Click(BuyButtons()[2], false); // already sold
            yield return new WaitForSecondsRealtime(.15f);
            if (host.CurrentMapRun.Gold != settledGold || counter.CommittedPlays != 1 || AudioExpansionVerification.Count("coins") != coinBefore + 1)
                throw new InvalidOperationException("Disabled or sold offer replayed transaction");
            host.PreviewUiCapturePage("map"); yield return new WaitForSecondsRealtime(.2f);
            if (Object.FindAnyObjectByType<ShopCounterDetail>() != null || Trays().Length != 0) throw new InvalidOperationException("Shop display leaked on exit");
            host.PreviewUiCapturePage("room", "S"); yield return new WaitForSecondsRealtime(.25f); yield return new WaitForEndOfFrame();
            counter = Object.FindAnyObjectByType<ShopCounterDetail>();
            if (counter.CommittedPlays != 0 || Products()[2].gameObject.activeInHierarchy) throw new InvalidOperationException("Reopen lost sold state or replayed");
            Motion(0);
            yield return Click(BuyButtons()[0], true); yield return new WaitForSecondsRealtime(.2f);
            counter = Object.FindAnyObjectByType<ShopCounterDetail>();
            if (host.CurrentMapRun.Gold != settledGold - 3 || counter.CommittedPlays != 1 || counter.IsAnimating || Products()[0].gameObject.activeInHierarchy)
                throw new InvalidOperationException("Animation zero changed purchase result or left motion");
            yield return Capture("shop-zero-motion.png");
            Motion(animation);
            host.PreviewUiCapturePage("map"); yield return new WaitForSecondsRealtime(.2f);
            if (Object.FindAnyObjectByType<ShopCounterDetail>() != null) throw new InvalidOperationException("Final cleanup failed");
            File.WriteAllText(Path.Combine(Folder, "result.txt"), "PASS: disposable in-memory S entry; actual top-raycast purchase of goggles and talisman; three formal supported displays, sold item alone removed; cash coins animate once after committed purchase and settle; insufficient/sold disabled clicks do not replay; animation zero preserves actual purchase with no motion; exit clears and reopen retains sold state without replay. Existing formal assets reused; no new prop promotion, no player save writes, no complete-journey or acoustic-review claim.");
            DetailsInputFixture.End();
        }
        private static IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame(); var frame = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(Folder, name), frame.EncodeToPNG()); Object.Destroy(frame);
        }
    }
}
