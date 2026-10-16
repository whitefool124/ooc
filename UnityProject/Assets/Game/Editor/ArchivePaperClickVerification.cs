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
namespace OCC.Combat.EditorTools
{
    public static class ArchivePaperClickVerification
    {
        private static string folderOverride;
        private static string Folder => folderOverride ?? Path.GetFullPath(Path.Combine(Application.dataPath,"../../Worldbuilding/归档/2026-10-03_夹纸位置与点击修复"));
        [MenuItem("OCC/VFX UI/Paper Click/Verify")]
        public static void Verify()
        {
            if (!Application.isPlaying || Application.dataPath.Replace('\\','/') != "E:/数据库/OCC_Codex/UnityProject/Assets") throw new System.InvalidOperationException("Main project Play required");
            Directory.CreateDirectory(Folder);
            Object.FindAnyObjectByType<CombatPrototypeBootstrap>().StartCoroutine(Run());
        }
        [MenuItem("OCC/VFX UI/Paper Click/Verify Restored Landing")]
        public static void VerifyRestoredLanding()
        {
            if (!Application.isPlaying || Application.dataPath.Replace('\\','/') != "E:/数据库/OCC_Codex/UnityProject/Assets") throw new System.InvalidOperationException("Main project Play required");
            folderOverride=Path.GetFullPath(Path.Combine(Application.dataPath,"../../Worldbuilding/归档/2026-10-03_撤下失当主界面装饰"));
            Directory.CreateDirectory(Folder);
            Object.FindAnyObjectByType<CombatPrototypeBootstrap>().StartCoroutine(RestoredRoutine());
        }
        private static IEnumerator RestoredRoutine()
        {
            bool background=Application.runInBackground;
            Vector2 previous=Mouse.current.position.ReadValue();
            Application.runInBackground=true;
            EditorWindow.GetWindow(System.Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
            try
            {
                var flow=Object.FindAnyObjectByType<FirstExperiencePrototypeController>();
                float deadline=Time.realtimeSinceStartup+15f;
                while(flow.CurrentStage != FirstExperiencePrototypeController.FlowStage.Landing && Time.realtimeSinceStartup<deadline) yield return null;
                yield return new WaitForSecondsRealtime(.6f);
                if(flow.CurrentStage != FirstExperiencePrototypeController.FlowStage.Landing) throw new System.InvalidOperationException("Normal landing not reached");
                if(Object.FindObjectsByType<ArchivePaperDetail>().Length!=0 || Object.FindObjectsByType<AcademyDeskLamp>().Length!=0) throw new System.InvalidOperationException("Rejected decoration still present");
                var begin=Object.FindObjectsByType<Button>().Single(b=>b.name=="按钮_开始新游戏" && b.isActiveAndEnabled);
                var label=begin.GetComponentInChildren<Text>().rectTransform;
                if(label.offsetMin != Vector2.zero || label.offsetMax != Vector2.zero) throw new System.InvalidOperationException("Entry label still displaced");
                InputSystem.QueueStateEvent(Mouse.current,new MouseState {position=Vector2.zero});
                yield return new WaitForSecondsRealtime(.5f);
                yield return Capture("restored-landing.png");
                var rect=(RectTransform)begin.transform;
                Vector2 target=RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center));
                var hits=new List<RaycastResult>();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=target},hits);
                if(hits.Count==0 || hits[0].gameObject.GetComponentInParent<Button>()!=begin) throw new System.InvalidOperationException("Entry raycast blocked");
                yield return Click(target);
                yield return new WaitForSecondsRealtime(.4f);
                if(flow.CurrentStage!=FirstExperiencePrototypeController.FlowStage.SaveSelection) throw new System.InvalidOperationException("Entry click failed");
                yield return Capture("entry.png");
                var back=Object.FindObjectsByType<Button>().Single(b=>b.name=="按钮_返回" && b.isActiveAndEnabled);
                rect=(RectTransform)back.transform;
                yield return Click(RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center)));
                yield return new WaitForSecondsRealtime(.4f);
                if(flow.CurrentStage!=FirstExperiencePrototypeController.FlowStage.Landing) throw new System.InvalidOperationException("Return click failed");
                File.WriteAllText(Path.Combine(Folder,"result.txt"),"PASS: normal Landing; no added paper or lamp; label offsets zero; actual raycast and Input System mouse press/release open SaveSelection and return Landing; no slot selected or saved.");
            }
            finally {Application.runInBackground=background; InputSystem.QueueStateEvent(Mouse.current,new MouseState {position=previous});}
        }
        private static IEnumerator Run()
        {
            bool background = Application.runInBackground;
            Vector2 previous = Mouse.current.position.ReadValue();
            Application.runInBackground = true;
            EditorWindow.GetWindow(System.Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
            try
            {
                float deadline = Time.realtimeSinceStartup + 15f;
                while (Object.FindAnyObjectByType<FirstExperiencePrototypeController>().CurrentStage != FirstExperiencePrototypeController.FlowStage.Landing && Time.realtimeSinceStartup < deadline) yield return null;
                yield return new WaitForSecondsRealtime(.6f);
                var paper = Object.FindObjectsByType<ArchivePaperDetail>().Single();
                var begin = paper.GetComponentInParent<Button>();
                if (begin == null || begin.name != "按钮_开始新游戏") throw new System.InvalidOperationException("Paper detached from entry");
                Vector2 target = RectTransformUtility.WorldToScreenPoint(null,paper.rectTransform.TransformPoint(Vector2.zero));
                File.WriteAllText(Path.Combine(Folder,"layout.txt"),"screen="+Screen.width+"x"+Screen.height+" target="+target+" paperRect="+paper.rectTransform.rect+" parentRect="+((RectTransform)begin.transform).rect+" filter="+paper.IsRaycastLocationValid(target,null)+" module="+EventSystem.current.currentInputModule+" raycasters="+Object.FindObjectsByType<GraphicRaycaster>().Length+" paperDepth="+paper.depth+" parent="+paper.transform.parent.name);
                yield return Capture("layout.png");
                var hits = new List<RaycastResult>();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=target},hits);
                File.WriteAllLines(Path.Combine(Folder,"raycast.txt"),hits.Select(h=>h.gameObject.name));
                if (hits.Count==0 || hits[0].gameObject.GetComponentInParent<Button>()!=begin) throw new System.InvalidOperationException("Paper hit did not resolve to new archive");
                InputSystem.QueueStateEvent(Mouse.current,new MouseState { position=target });
                yield return new WaitForSecondsRealtime(.4f);
                if (paper.Progress < .95f) throw new System.InvalidOperationException("Real hover failed");
                yield return Capture("landing.png");
                yield return Click(target);
                yield return new WaitForSecondsRealtime(.4f);
                var flow = Object.FindAnyObjectByType<FirstExperiencePrototypeController>();
                if (flow.CurrentStage != FirstExperiencePrototypeController.FlowStage.SaveSelection) throw new System.InvalidOperationException("Real click failed: "+flow.DebugStage);
                yield return Capture("clicked-entry.png");
                var back = Object.FindObjectsByType<Button>().Single(b=>b.name=="按钮_返回" && b.isActiveAndEnabled);
                var rect = (RectTransform)back.transform;
                yield return Click(RectTransformUtility.WorldToScreenPoint(null,rect.TransformPoint(rect.rect.center)));
                yield return new WaitForSecondsRealtime(.6f);
                if (flow.CurrentStage != FirstExperiencePrototypeController.FlowStage.Landing) throw new System.InvalidOperationException("Real return click failed");
                if (Object.FindObjectsByType<ArchivePaperDetail>().Length!=1) throw new System.InvalidOperationException("Duplicate paper");
                yield return Capture("returned.png");
                File.WriteAllText(Path.Combine(Folder,"result.txt"),"PASS: GraphicRaycaster hit paper -> parent new archive button; actual Input System hover, left press/release entered SaveSelection; actual return click returned Landing; no slot chosen or save written.");
            }
            finally { Application.runInBackground=background; InputSystem.QueueStateEvent(Mouse.current,new MouseState { position=previous }); }
        }
        private static IEnumerator Click(Vector2 point)
        {
            InputSystem.QueueStateEvent(Mouse.current,new MouseState { position=point }); yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState { position=point }.WithButton(MouseButton.Left)); yield return null; yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState { position=point }); yield return null;
        }
        private static IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            var texture=ScreenCapture.CaptureScreenshotAsTexture();
            File.WriteAllBytes(Path.Combine(Folder,name),texture.EncodeToPNG()); Object.Destroy(texture);
        }
    }
}