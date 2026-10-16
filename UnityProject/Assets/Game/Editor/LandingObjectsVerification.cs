using System.Collections;
using System.IO;
using System.Linq;
using OCC.Combat.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace OCC.Combat.EditorTools
{
    public static class LandingObjectsVerification
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Worldbuilding/归档/2026-10-02_无文字物件与悬停提示"));
        private static bool background, armed;

        [MenuItem("OCC/VFX UI/Landing Objects/Begin")]
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
        [MenuItem("OCC/VFX UI/Landing Objects/Inspect")]
        public static void Inspect()
        {
            if (Application.dataPath.Replace('\\', '/') != "E:/数据库/OCC_Codex/UnityProject/Assets") throw new System.InvalidOperationException("Wrong Unity project: " + Application.dataPath);
            Directory.CreateDirectory(Folder);
            File.WriteAllText(Path.Combine(Folder, "project-path.txt"), Application.dataPath);
            File.WriteAllLines(Path.Combine(Folder, "lamps.txt"), Object.FindObjectsByType<AcademyDeskLamp>().Select(l => l.name + " raycast=" + l.raycastTarget + " rect=" + l.rectTransform.rect + " lightStep=" + l.LightStep(Time.unscaledTime)));
            File.WriteAllLines(Path.Combine(Folder, "buttons.txt"), Object.FindObjectsByType<Button>()
                .Where(b => b.isActiveAndEnabled).Select(b => b.name + " enabled=" + b.IsInteractable() + " | " + string.Join(" / ", b.GetComponentsInChildren<Text>().Select(t => t.text.Replace("\n", " ")))));
            File.WriteAllLines(Path.Combine(Folder, "markers.txt"), Object.FindObjectsByType<UiSelectionBookmark>()
                .Select(m => m.transform.parent.name + " rect=" + m.rectTransform.rect + " raycast=" + m.raycastTarget));
            File.WriteAllLines(Path.Combine(Folder, "tooltips.txt"), Object.FindObjectsByType<FormalHoverTooltip>()
                .Select(t => t.name + " visible=" + t.IsVisible));
            File.WriteAllLines(Path.Combine(Folder, "visible-text.txt"), Object.FindObjectsByType<Text>()
                .Where(t => t.isActiveAndEnabled && !string.IsNullOrEmpty(t.text))
                .Select(t => t.name + " vertices=" + t.cachedTextGenerator.vertexCount + " | " + t.text.Replace("\n", " / ")));
        }
        [MenuItem("OCC/VFX UI/Landing Objects/Hover")]
        public static void Hover()
        {
            string name = File.ReadAllText(Path.Combine(Folder, "hover.txt")).Trim();
            var trigger = Object.FindObjectsByType<FormalHoverTooltipTrigger>().Single(t => t.name == name && t.isActiveAndEnabled);
            var rect = (RectTransform)trigger.transform;
            var data = new PointerEventData(EventSystem.current) { position = RectTransformUtility.WorldToScreenPoint(null, rect.TransformPoint(rect.rect.center)) };
            trigger.OnPointerEnter(data);
            Capture();
        }
        [MenuItem("OCC/VFX UI/Landing Objects/End Hover")]
        public static void EndHover()
        {
            string name = File.ReadAllText(Path.Combine(Folder, "hover.txt")).Trim();
            var trigger = Object.FindObjectsByType<FormalHoverTooltipTrigger>().Single(t => t.name == name && t.isActiveAndEnabled);
            trigger.OnPointerExit(new PointerEventData(EventSystem.current));
            Inspect();
        }
        [MenuItem("OCC/VFX UI/Landing Objects/Click")]
        public static void Click()
        {
            string label = File.ReadAllText(Path.Combine(Folder, "click.txt")).Trim();
            Button b = Object.FindObjectsByType<Button>().Single(button => button.isActiveAndEnabled && button.IsInteractable()
                && (button.name == label || button.GetComponentsInChildren<Text>().Any(t => t.text == label)));
            ExecuteEvents.Execute(b.gameObject, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
            Capture();
        }
        [MenuItem("OCC/VFX UI/Landing Objects/Capture")]
        public static void Capture()
            => Object.FindAnyObjectByType<CombatPrototypeBootstrap>().StartCoroutine(CaptureRoutine());
        [MenuItem("OCC/VFX UI/Landing Objects/Verify Lamp Hover")]
        public static void VerifyLampHover()
            => Object.FindAnyObjectByType<CombatPrototypeBootstrap>().StartCoroutine(LampHoverRoutine());
        private static IEnumerator LampHoverRoutine()
        {
            var lamp = Object.FindAnyObjectByType<AcademyDeskLamp>();
            var mouse = Mouse.current;
            if (lamp == null || mouse == null) throw new System.InvalidOperationException("Visible lamp and mouse required");
            Vector2 previous = mouse.position.ReadValue();
            Vector2 target = RectTransformUtility.WorldToScreenPoint(null, lamp.rectTransform.TransformPoint(lamp.rectTransform.rect.center));
            try
            {
                InputSystem.QueueDeltaStateEvent(mouse.position, target);
                yield return null;
                yield return new WaitForEndOfFrame();
                int step = lamp.LightStep(Time.unscaledTime);
                File.WriteAllText(Path.Combine(Folder, "lamp-hover-result.txt"), "Simulated mouse position via Input System; local lightStep=" + step + "; raycast=" + lamp.raycastTarget);
                if (step != 3) throw new System.InvalidOperationException("Lamp did not respond to nearby mouse");
                var texture = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(Folder, "lamp-hover.png"), texture.EncodeToPNG());
                Object.Destroy(texture);
            }
            finally { InputSystem.QueueDeltaStateEvent(mouse.position, previous); }
        }
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
    }
}


