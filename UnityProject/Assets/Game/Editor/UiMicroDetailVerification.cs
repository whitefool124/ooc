using System.Collections;
using System.IO;
using System.Linq;
using OCC.Combat.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OCC.Combat.EditorTools
{
    public static class UiMicroDetailVerification
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Worldbuilding/归档/2026-10-02_界面小细节第二轮"));
        private static bool background, armed;

        [MenuItem("OCC/VFX UI/Micro Details/Begin")]
        public static void Begin()
        {
            if (!Application.isPlaying) throw new System.InvalidOperationException("Play Mode required");
            if (!armed) { background = Application.runInBackground; armed = true; }
            Application.runInBackground = true;
            EditorApplication.isPaused = false;
            EditorApplication.playModeStateChanged -= Restore;
            EditorApplication.playModeStateChanged += Restore;
            EditorWindow.GetWindow(System.Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
            Inspect();
        }
        private static void Restore(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.ExitingPlayMode || !armed) return;
            Application.runInBackground = background; armed = false;
            EditorApplication.playModeStateChanged -= Restore;
        }
        [MenuItem("OCC/VFX UI/Micro Details/Inspect")]
        public static void Inspect()
        {
            Directory.CreateDirectory(Folder);
            File.WriteAllLines(Path.Combine(Folder, "buttons.txt"), Object.FindObjectsByType<Button>()
                .Where(b => b.isActiveAndEnabled).Select(b => b.name + " enabled=" + b.IsInteractable() + " | " + string.Join(" / ", b.GetComponentsInChildren<Text>().Select(t => t.text.Replace("\n", " ")))));
            File.WriteAllLines(Path.Combine(Folder, "markers.txt"), Object.FindObjectsByType<UiSelectionBookmark>()
                .Select(m => m.transform.parent.name + " rect=" + m.rectTransform.rect + " raycast=" + m.raycastTarget));
        }
        [MenuItem("OCC/VFX UI/Micro Details/Click")]
        public static void Click()
        {
            string label = File.ReadAllText(Path.Combine(Folder, "click.txt")).Trim();
            Button b = Object.FindObjectsByType<Button>().Single(button => button.isActiveAndEnabled && button.IsInteractable()
                && (button.name == label || button.GetComponentsInChildren<Text>().Any(t => t.text == label)));
            ExecuteEvents.Execute(b.gameObject, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
            Capture();
        }
        [MenuItem("OCC/VFX UI/Micro Details/Capture")]
        public static void Capture()
            => Object.FindAnyObjectByType<CombatPrototypeBootstrap>().StartCoroutine(CaptureRoutine());
        private static IEnumerator CaptureRoutine()
        {
            yield return new WaitForSecondsRealtime(.3f);
            yield return new WaitForEndOfFrame();
            string id = File.Exists(Path.Combine(Folder, "capture-id.txt")) ? File.ReadAllText(Path.Combine(Folder, "capture-id.txt")).Trim() : "interface";
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(Folder, id + ".png"), texture.EncodeToPNG());
            Object.Destroy(texture);
            Inspect();
        }
        [MenuItem("OCC/VFX UI/Micro Details/Verify Runtime Cleanup")]
        public static void VerifyCleanup()
            => Object.FindAnyObjectByType<CombatPrototypeBootstrap>().StartCoroutine(CleanupRoutine());
        private static IEnumerator CleanupRoutine()
        {
            var button = Object.FindObjectsByType<Button>().First(b => b.isActiveAndEnabled && b.IsInteractable() && b.GetComponent<UiButtonFeedback>() != null);
            var feedback = button.GetComponent<UiButtonFeedback>();
            feedback.OnPointerEnter(new PointerEventData(EventSystem.current));
            feedback.OnSelect(new BaseEventData(EventSystem.current));
            button.gameObject.SetActive(false);
            yield return null;
            button.gameObject.SetActive(true);
            int remaining = button.GetComponentsInChildren<PeripheralPixelVfx>(true).Length;
            bool focusVisible = button.transform.Find("像素焦点框")?.gameObject.activeSelf == true;
            File.WriteAllText(Path.Combine(Folder, "runtime-cleanup.txt"), "button=" + button.name + " remaining=" + remaining + " focusVisible=" + focusVisible);
            if (remaining != 0 || focusVisible) throw new System.InvalidOperationException("Old focus resumed on reopened button");
        }
    }
}
