using System.IO;
using System.Collections;
using System.Linq;
using OCC.Combat.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OCC.Combat.EditorTools
{
    public static class PeripheralPixelVfxVerification
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Worldbuilding/归档/2026-10-02_战斗外特效迭代"));
        private static bool background;
        private static bool backgroundSet;
        private static int frame;

        [MenuItem("OCC/VFX UI/Prepare Disposable Workshop")]
        public static void Prepare()
        {
            if (!Application.isPlaying) throw new System.InvalidOperationException("Play Mode required");
            EditorApplication.isPaused = false;
            Directory.CreateDirectory(Folder);
            if (!backgroundSet) { background = Application.runInBackground; backgroundSet = true; }
            Application.runInBackground = true;
            EditorApplication.playModeStateChanged -= Restore;
            EditorApplication.playModeStateChanged += Restore;
            var host = Object.FindAnyObjectByType<CombatPrototypeBootstrap>();
            string error = host.PreviewUiCaptureRun(false, "B1");
            if (error != null) throw new System.InvalidOperationException(error);
            // Isolated review fixture: settle battles through model APIs and follow the
            // normal event route. This is not evidence of playing the battles end to end.
            var run = host.CurrentMapRun;
            run.CompleteCurrentCombat();
            run.ClaimReward("F-P-R19");
            run.SelectNode("EV1");
            run.ChooseCurrentNodeContent("FIRST-EV1-ACCEPT-DELIVERY");
            run.SelectNode("EV2");
            run.ChooseCurrentNodeContent(run.CurrentContentChoices[0].Id);
            run.SelectNode("B2");
            run.CompleteCurrentCombat();
            run.ClaimReward(run.CurrentFirstRunRewardIds[0]);
            run.SelectNode("EV3");
            run.ChooseCurrentNodeContent(run.CurrentContentChoices[1].Id);
            host.SelectMapNode("W");
            frame = Time.frameCount;
            EditorApplication.update += OpenWorkshop;
        }
        private static void OpenWorkshop()
        {
            if (!Application.isPlaying || Time.frameCount < frame + 3) return;
            EditorApplication.update -= OpenWorkshop;
            Object.FindAnyObjectByType<CombatPrototypeBootstrap>().PreviewUiCapturePage("room", "W");
        }
        private static void Restore(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.ExitingPlayMode) return;
            if (backgroundSet) Application.runInBackground = background;
            backgroundSet = false;
            EditorApplication.update -= OpenWorkshop;
            EditorApplication.update -= PauseOnEffect;
            EditorApplication.playModeStateChanged -= Restore;
        }
        [MenuItem("OCC/VFX UI/Inspect")]
        public static void Inspect()
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllLines(Path.Combine(Folder, "buttons.txt"), Object.FindObjectsByType<Button>(FindObjectsSortMode.None)
                .Where(b => b.isActiveAndEnabled).Select(b => b.name + " | enabled=" + b.IsInteractable() + " | " + string.Join(" / ", b.GetComponentsInChildren<Text>().Select(t => t.text.Replace("\n", " ")))));
            File.WriteAllLines(Path.Combine(Folder, "effects.txt"), Object.FindObjectsByType<PeripheralPixelVfx>(FindObjectsSortMode.None)
                .Select(e => e.name + " active=" + e.isActiveAndEnabled + " raycast=" + e.raycastTarget + " age=" + e.Progress));
            File.WriteAllLines(Path.Combine(Folder, "text-layout.txt"), Object.FindObjectsByType<Text>(FindObjectsSortMode.None)
                .Where(t => t.name == "提示文字").Select(t => t.text + " rect=" + t.rectTransform.rect + " preferred=" + t.preferredHeight
                    + " vertices=" + t.cachedTextGenerator.vertexCount + " color=" + t.color + " cull=" + t.canvasRenderer.cull));
        }
        [MenuItem("OCC/VFX UI/Capture Forge Frames")]
        public static void CaptureFrames()
        {
            Prepare();
            EditorApplication.update -= PauseOnEffect;
            EditorWindow.GetWindow(System.Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
            Object.FindAnyObjectByType<CombatPrototypeBootstrap>().StartCoroutine(CaptureRoutine());
        }
        private static IEnumerator CaptureRoutine()
        {
            for (int i = 0; i < 8; i++) yield return null;
            File.WriteAllText(Path.Combine(Folder, "click.txt"), "工坊卡_ACA-EQ-CH01");
            Click();
            yield return null; yield return null;
            File.WriteAllText(Path.Combine(Folder, "click.txt"), "按钮_确认安装");
            Click();
            for (int i = 0; i < 10; i++)
            {
                yield return new WaitForEndOfFrame();
                var texture = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(Folder, "forge-frame-" + i.ToString("00") + ".png"), texture.EncodeToPNG());
                Object.Destroy(texture);
                if (i == 4) Inspect();
                yield return new WaitForSecondsRealtime(.06f);
            }
            File.WriteAllText(Path.Combine(Folder, "click.txt"), "按钮_返回地图");
            Click();
            for (int i = 0; i < 10; i++)
            {
                yield return new WaitForEndOfFrame();
                var texture = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(Folder, "route-frame-" + i.ToString("00") + ".png"), texture.EncodeToPNG());
                Object.Destroy(texture);
                yield return new WaitForSecondsRealtime(.06f);
            }
            Inspect();
            File.WriteAllText(Path.Combine(Folder, "capture-complete.txt"), "20 actual rendered frames; isolated in-memory preview; no player-slot write; forge feedback followed by committed route feedback on returning to map.");
        }
        [MenuItem("OCC/VFX UI/Click")]
        public static void Click()
        {
            string label = File.ReadAllText(Path.Combine(Folder, "click.txt")).Trim();
            var button = Object.FindObjectsByType<Button>(FindObjectsSortMode.None).Single(b => b.isActiveAndEnabled && b.IsInteractable()
                && (b.name == label || b.GetComponentsInChildren<Text>().Any(t => t.text == label)));
            ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
        }
        [MenuItem("OCC/VFX UI/Arm Capture")]
        public static void ArmCapture()
        {
            EditorApplication.update -= PauseOnEffect;
            EditorApplication.update += PauseOnEffect;
        }
        private static void PauseOnEffect()
        {
            if (!Object.FindObjectsByType<PeripheralPixelVfx>(FindObjectsSortMode.None)
                .Any(e => e.isActiveAndEnabled && e.name != "像素细节_page" && e.name != "像素细节_focus" && e.Progress >= .4f && e.Progress < .8f)) return;
            EditorApplication.update -= PauseOnEffect;
            Inspect();
            EditorApplication.isPaused = true;
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }
    }
}
