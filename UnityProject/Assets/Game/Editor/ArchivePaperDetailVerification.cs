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
    public static class ArchivePaperDetailVerification
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath,
            "../../Worldbuilding/归档/2026-10-02_夹纸小巧思样板"));
        private static bool previousBackground, armed;
        [MenuItem("OCC/VFX UI/Paper Detail/Record")]
        public static void Record()
        {
            if (Application.dataPath.Replace('\\', '/') != "E:/数据库/OCC_Codex/UnityProject/Assets")
                throw new System.InvalidOperationException("Wrong project");
            if (!Application.isPlaying) throw new System.InvalidOperationException("Play Mode required");
            Directory.CreateDirectory(Folder);
            if (!armed) { previousBackground = Application.runInBackground; armed = true; }
            Application.runInBackground = true;
            EditorApplication.isPaused = false;
            EditorApplication.playModeStateChanged -= Restore;
            EditorApplication.playModeStateChanged += Restore;
            EditorWindow.GetWindow(System.Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
            Object.FindAnyObjectByType<CombatPrototypeBootstrap>().StartCoroutine(RecordRoutine());
        }
        private static void Restore(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.ExitingPlayMode || !armed) return;
            Application.runInBackground = previousBackground; armed = false;
            EditorApplication.playModeStateChanged -= Restore;
        }
        private static IEnumerator RecordRoutine()
        {
            var button = Object.FindObjectsByType<Button>().Single(b => b.name == "按钮_开始新游戏" && b.isActiveAndEnabled);
            var paper = Object.FindAnyObjectByType<ArchivePaperDetail>();
            var data = new PointerEventData(EventSystem.current)
            {
                position = RectTransformUtility.WorldToScreenPoint(null, button.transform.position)
            };
            ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerExitHandler);
            yield return new WaitForSecondsRealtime(.5f);
            yield return Capture(0, paper);
            ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerEnterHandler);
            for (int i = 1; i <= 8; i++)
            {
                yield return new WaitForSecondsRealtime(.05f);
                yield return Capture(i, paper);
            }
            if (paper.Progress < .99f) throw new System.InvalidOperationException("Paper did not pull");
            ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerExitHandler);
            for (int i = 9; i <= 17; i++)
            {
                yield return new WaitForSecondsRealtime(.05f);
                yield return Capture(i, paper);
            }
            if (paper.Progress != 0f) throw new System.InvalidOperationException("Paper did not return");
            File.WriteAllText(Path.Combine(Folder, "runtime-result.txt"), "Application.dataPath=" + Application.dataPath
                + "\nPointer enter: fully pulled; pointer exit: progress=" + paper.Progress
                + "; instances=" + Object.FindObjectsByType<ArchivePaperDetail>().Length + "; raycast=" + paper.raycastTarget);
        }
        private static IEnumerator Capture(int index, ArchivePaperDetail paper)
        {
            yield return new WaitForEndOfFrame();
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(Folder, "frame-" + index.ToString("00") + ".png"), texture.EncodeToPNG());
            Object.Destroy(texture);
            File.AppendAllText(Path.Combine(Folder, "frame-timing.txt"), index + " time=" + Time.unscaledTime + " progress=" + paper.Progress + "\n");
        }
    }
}
