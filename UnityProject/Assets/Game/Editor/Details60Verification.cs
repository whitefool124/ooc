using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OCC.Combat.Presentation;
using OCC.Combat.Roguelite;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using Object=UnityEngine.Object;

namespace OCC.Combat.EditorTools
{
    public static class Details60Verification
    {
        private static string Folder=>Path.GetFullPath(Path.Combine(Application.dataPath,"../../Worldbuilding/归档/2026-10-03_六十项小细节实装/验证/map-settings-archive"));
        private static Vector2 mouse;
        private static bool background,hadVolume;
        private static float volume;
        private const string VolumeKey="OCC.FirstExperiencePrototype.volume.v1";
        [MenuItem("OCC/Details60/Import Ambience")]
        public static void Import()
        {
            if(Application.isPlaying || Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new InvalidOperationException("Main project Edit Mode required");
            AssetDatabase.Refresh();
            foreach(string guid in AssetDatabase.FindAssets("t:AudioClip",new[]{"Assets/Resources/Audio/OCC/Ambience"}))
            {
                var importer=(AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GUIDToAssetPath(guid));
                importer.forceToMono=true;importer.loadInBackground=true;
                var sample=importer.defaultSampleSettings;sample.loadType=AudioClipLoadType.Streaming;sample.compressionFormat=AudioCompressionFormat.Vorbis;sample.quality=.7f;sample.preloadAudioData=false;
                importer.defaultSampleSettings=sample;importer.SaveAndReimport();
            }
            var shader=Shader.Find("OCC/UI/ItemSurface");
            if(shader==null || ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("Item surface shader missing or invalid");
            Debug.Log("Details60 ambiance Streaming/Vorbis; item surface shader compiled");
        }
        [MenuItem("OCC/Details60/Map Settings Archive/Record")]
        public static void Record()
        {
            if(!Application.isPlaying || Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")throw new InvalidOperationException("Main project Play required");
            DetailsInputFixture.End();Directory.CreateDirectory(Folder);AudioExpansionVerification.StartLedger(Folder);
            background=Application.runInBackground;mouse=Mouse.current.position.ReadValue();Application.runInBackground=true;DetailsInputFixture.Begin(background);
            hadVolume=PlayerPrefs.HasKey(VolumeKey);volume=PlayerPrefs.GetFloat(VolumeKey);
            EditorApplication.playModeStateChanged-=Restore;EditorApplication.playModeStateChanged+=Restore;
            EditorWindow.GetWindow(Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
            Object.FindAnyObjectByType<CombatPrototypeBootstrap>().StartCoroutine(Routine());
        }
        private static void Restore(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.ExitingPlayMode)return;
            Application.runInBackground=background;Pointer(mouse,false);
            if(hadVolume)PlayerPrefs.SetFloat(VolumeKey,volume);else PlayerPrefs.DeleteKey(VolumeKey);PlayerPrefs.Save();
            DetailsInputFixture.End();EditorApplication.playModeStateChanged-=Restore;
        }
        private static IEnumerator Routine()
        {
            var host=Object.FindAnyObjectByType<CombatPrototypeBootstrap>();
            string error=host.PreviewUiCaptureRun(false,"B1");if(error!=null)throw new InvalidOperationException(error);
            host.CurrentMapRun.CompleteCurrentCombat();host.CurrentMapRun.ClaimReward("F-P-R19");
            host.PreviewUiCapturePage("map");yield return new WaitForSecondsRealtime(.65f);
            var map=Object.FindAnyObjectByType<RogueMapViewportController>();
            var current=Object.FindObjectsByType<OccMapNodeDetails>().First(x=>x.name=="map.node.B1");
            var artwork=current.GetComponentsInChildren<Image>().First(i=>i.name.StartsWith("节点类型图标_"));
            float width=artwork.rectTransform.rect.width*artwork.transform.lossyScale.x;
            map.ZoomIn();yield return new WaitForSecondsRealtime(.3f);
            if(Mathf.Abs(width-artwork.rectTransform.rect.width*artwork.transform.lossyScale.x)>.1f)throw new InvalidOperationException("Current marker enlarged with map");
            var available=Object.FindObjectsByType<OccMapNodeDetails>().FirstOrDefault(x=>host.CurrentMapRun.VisualStateFor(x.name.Substring("map.node.".Length))==RogueliteMapNodeVisualState.Available);
            if(available==null)throw new InvalidOperationException("No available node to inspect");
            map.ResetToOverview();yield return null;
            Pointer(Center(available.GetComponent<RectTransform>()),false);yield return new WaitForSecondsRealtime(.18f);
            var guide=map.MapContent.Find("可达路线预览");
            if(guide==null || guide.GetComponentsInChildren<Graphic>().Any(g=>g.raycastTarget))throw new InvalidOperationException("Route hover missing or blocks clicks");
            yield return Capture("route-hover.png");Pointer(new Vector2(1910,1070),false);yield return null;yield return null;
            if(map.MapContent.Find("可达路线预览")!=null)throw new InvalidOperationException("Route leaked after exit");
            host.PreviewUiCapturePage("settings");yield return new WaitForSecondsRealtime(.35f);
            Slider slider=Object.FindObjectsByType<Slider>().Single(s=>s.name.Contains("音量"));
            // Release the existing handle without changing or saving the player's setting.
            Vector2 thumb=Center(slider.handleRect);Pointer(thumb,false);yield return null;Pointer(thumb,true);yield return null;Pointer(thumb,false);yield return null;
            var audio=host.GetComponent<OccDetailAudio>();
            if(audio==null || !audio.AuditionPlaying)throw new InvalidOperationException("Volume release has no audition");
            if(host.GetComponentsInChildren<AudioSource>().Count(s=>s.name=="音量试听")!=1)throw new InvalidOperationException("Duplicate audition source");
            host.PreviewUiCapturePage("map");yield return new WaitForSecondsRealtime(.12f);
            if(audio.AuditionPlaying)throw new InvalidOperationException("Audition survived settings exit");
            host.PreviewUiCapturePage("encyclopedia");yield return new WaitForSecondsRealtime(.35f);yield return Capture("archive-entry.png");
            yield return Click("按钮_百科_装备");yield return new WaitForSecondsRealtime(.2f);
            ScrollRect gallery=Object.FindObjectsByType<ScrollRect>().Single(s=>s.name.Contains("百科"));gallery.verticalNormalizedPosition=.37f;
            yield return Click("按钮_百科_术式");yield return new WaitForSecondsRealtime(.15f);
            yield return Click("按钮_百科_装备");yield return new WaitForSecondsRealtime(.2f);
            gallery=Object.FindObjectsByType<ScrollRect>().Single(s=>s.name.Contains("百科"));
            if(Mathf.Abs(gallery.verticalNormalizedPosition-.37f)>.015f)throw new InvalidOperationException("Archive category forgot browse position");
            if(Object.FindObjectsByType<OccItemSurface>().Length==0)throw new InvalidOperationException("No item-local material in equipment archive");
            yield return Capture("archive-return.png");
            host.PreviewUiCapturePage("map");yield return null;yield return null;
            if(Object.FindObjectsByType<OccItemSurface>().Length!=0)throw new InvalidOperationException("Item materials survived page exit");
            File.WriteAllText(Path.Combine(Folder,"result.txt"),"PASS: disposable preview; constant-sized current marker at 1x/2x; actual pointer route hover/exit with no raycast targets; actual volume handle release audition, one source, stop on exit; actual equipment/spell category button clicks restore .37 scroll; item-local material and cleanup. Does not prove a complete journey or an acoustic listening review. Player volume not deliberately changed.");
        }
        private static Vector2 Center(RectTransform r)=>RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));
        private static void Pointer(Vector2 p,bool down)=>InputSystem.QueueStateEvent(Mouse.current,down?new MouseState{position=p}.WithButton(MouseButton.Left):new MouseState{position=p});
        private static IEnumerator Click(string name)
        {
            yield return new WaitForEndOfFrame();
            Button b=Object.FindObjectsByType<Button>().Single(x=>x.isActiveAndEnabled && x.name==name);Vector2 p=Center((RectTransform)b.transform);
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=p},hits);
            if(hits.Count==0 || hits[0].gameObject.GetComponentInParent<Button>()!=b){File.WriteAllText(Path.Combine(Folder,"blocked-button.txt"),name+" point="+p+" target="+b.targetGraphic+" raycast="+b.targetGraphic.raycastTarget+" depth="+b.targetGraphic.depth+" cull="+b.targetGraphic.canvasRenderer.cull+" canvas="+b.targetGraphic.canvas+" groups="+string.Join(",",b.GetComponentsInParent<CanvasGroup>().Select(g=>g.name+":"+g.blocksRaycasts+":"+g.alpha))+" hits="+string.Join(",",hits.Select(h=>h.gameObject.name+" parent="+h.gameObject.transform.parent?.name)));throw new InvalidOperationException("Blocked button "+name);}
            Pointer(p,false);yield return null;Pointer(p,true);yield return null;yield return null;Pointer(p,false);yield return null;
        }
        private static IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();var texture=ScreenCapture.CaptureScreenshotAsTexture();File.WriteAllBytes(Path.Combine(Folder,name),texture.EncodeToPNG());Object.Destroy(texture);
        }
    }
}
