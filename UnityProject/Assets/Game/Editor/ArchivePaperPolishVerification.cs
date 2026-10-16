using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OCC.Combat.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace OCC.Combat.EditorTools
{
    public static class ArchivePaperPolishVerification
    {
        private static string Folder => Path.GetFullPath(Path.Combine(Application.dataPath,
            "../../Worldbuilding/归档/2026-10-02_夹纸精修实装"));
        private static bool previousBackground, armed;
        private static readonly List<Texture2D> Captures = new List<Texture2D>();
        [MenuItem("OCC/VFX UI/Paper Polish/Record")]
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
            foreach (Texture2D capture in Captures) if (capture != null) Object.Destroy(capture);
            Captures.Clear();
            EditorApplication.playModeStateChanged -= Restore;
        }
        [MenuItem("OCC/VFX UI/Paper Polish/Verify Entry And Return")]
        public static void VerifyEntryAndReturn()
        {
            if (!Application.isPlaying || Application.dataPath.Replace('\\', '/') != "E:/数据库/OCC_Codex/UnityProject/Assets")
                throw new System.InvalidOperationException("Main project Play Mode required");
            if (!armed) { previousBackground = Application.runInBackground; armed = true; }
            Application.runInBackground = true;
            EditorApplication.isPaused = false;
            EditorApplication.playModeStateChanged -= Restore;
            EditorApplication.playModeStateChanged += Restore;
            EditorWindow.GetWindow(System.Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
            Object.FindAnyObjectByType<CombatPrototypeBootstrap>().StartCoroutine(EntryRoutine());
        }
        private static IEnumerator EntryRoutine()
        {
            var data = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            Button begin = Object.FindObjectsByType<Button>().Single(b => b.name == "按钮_开始新游戏" && b.isActiveAndEnabled);
            ExecuteEvents.Execute(begin.gameObject, data, ExecuteEvents.pointerEnterHandler);
            yield return new WaitForSecondsRealtime(.3f);
            ExecuteEvents.Execute(begin.gameObject, data, ExecuteEvents.pointerClickHandler);
            yield return new WaitForSecondsRealtime(.3f);
            if (Object.FindObjectsByType<ArchivePaperDetail>().Length != 0 ||
                Object.FindObjectsByType<FormalHoverTooltip>().Any(t => t.IsVisible))
                throw new System.InvalidOperationException("Paper or tooltip survived entry");
            yield return SaveFrame("new-archive-entry.png");
            Button back = Object.FindObjectsByType<Button>().Single(b => b.name == "按钮_返回" && b.isActiveAndEnabled);
            ExecuteEvents.Execute(back.gameObject, data, ExecuteEvents.pointerClickHandler);
            yield return new WaitForSecondsRealtime(.3f);
            var paper = Object.FindObjectsByType<ArchivePaperDetail>().Single();
            if (paper.Progress != 0f) throw new System.InvalidOperationException("Old pull resumed on return");
            paper.SetPulling(true); paper.Advance(.3f);
            paper.gameObject.SetActive(false);
            paper.gameObject.SetActive(true);
            if (paper.Progress != 0f) throw new System.InvalidOperationException("Disabled paper retained its curl");
            yield return SaveFrame("landing-return.png");
            File.WriteAllText(Path.Combine(Folder, "entry-cleanup.txt"),
                "New game button entered archive selection normally; no archive chosen. Active paper and tooltip during selection = 0. Return: one paper, progress=0. Disable/re-enable: progress=0.");
        }
        private static IEnumerator SaveFrame(string name)
        {
            yield return new WaitForEndOfFrame();
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(Folder, name), texture.EncodeToPNG());
            Object.Destroy(texture);
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
                yield return new WaitForSecondsRealtime(.025f);
                yield return Capture(i, paper);
            }
            yield return new WaitForSecondsRealtime(.28f);
            if (paper.Progress < .99f) throw new System.InvalidOperationException("Paper did not pull");
            ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerExitHandler);
            for (int i = 9; i <= 17; i++)
            {
                yield return new WaitForSecondsRealtime(.025f);
                yield return Capture(i, paper);
            }
            yield return new WaitForSecondsRealtime(.42f);
            yield return Capture(18, paper);
            for (int f = 0; f < Captures.Count; f++) { File.WriteAllBytes(Path.Combine(Folder, "frame-" + f.ToString("00") + ".png"), Captures[f].EncodeToPNG()); Object.Destroy(Captures[f]); }
            Captures.Clear();
            if (paper.Progress != 0f) throw new System.InvalidOperationException("Paper did not return");
            File.WriteAllText(Path.Combine(Folder, "runtime-result.txt"), "Application.dataPath=" + Application.dataPath
                + "\nPointer enter: fully pulled; pointer exit: progress=" + paper.Progress
                + "; instances=" + Object.FindObjectsByType<ArchivePaperDetail>().Length + "; raycast=" + paper.raycastTarget);
        }
        private static IEnumerator Capture(int index, ArchivePaperDetail paper)
        {
            yield return new WaitForEndOfFrame();
            var texture = ScreenCapture.CaptureScreenshotAsTexture();
            Captures.Add(texture);
            File.AppendAllText(Path.Combine(Folder, "frame-timing.txt"), index + " time=" + Time.unscaledTime + " progress=" + paper.Progress + "\n");
        }
    }
}

