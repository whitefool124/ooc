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
    public static class AcademyAtmosphereVerification
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath, "../../Worldbuilding/归档/2026-10-02_学院日常氛围第三轮"));
        private static bool background, armed;

        [MenuItem("OCC/VFX UI/Academy Atmosphere/Begin")]
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
        [MenuItem("OCC/VFX UI/Academy Atmosphere/Inspect")]
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
        }
        [MenuItem("OCC/VFX UI/Academy Atmosphere/Click")]
        public static void Click()
        {
            string label = File.ReadAllText(Path.Combine(Folder, "click.txt")).Trim();
            Button b = Object.FindObjectsByType<Button>().Single(button => button.isActiveAndEnabled && button.IsInteractable()
                && (button.name == label || button.GetComponentsInChildren<Text>().Any(t => t.text == label)));
            ExecuteEvents.Execute(b.gameObject, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
            Capture();
        }
        [MenuItem("OCC/VFX UI/Academy Atmosphere/Capture")]
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
    }
}

