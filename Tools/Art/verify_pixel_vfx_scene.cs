var lab = UnityEngine.Object.FindFirstObjectByType<OCC.Combat.Presentation.PixelVfxShowcase>();
if(lab==null || lab.Output==null) throw new System.Exception("Showcase is not running.");
var output = System.IO.Path.GetFullPath(System.IO.Path.Combine(UnityEngine.Application.dataPath,"../../Worldbuilding/归档/2026-10-02_Unity特效展示场"));
System.IO.Directory.CreateDirectory(output);
var receipts = new System.Collections.Generic.List<object>();
var hashes = new System.Collections.Generic.HashSet<string>();
int stage = 0, frames = 0;
double started = UnityEditor.EditorApplication.timeSinceStartup;
System.Action<UnityEngine.UI.Button> click = button => {
    var corners=new UnityEngine.Vector3[4];button.GetComponent<UnityEngine.RectTransform>().GetWorldCorners(corners);
    var position=(corners[0]+corners[2])*.5f;
    var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) { position=position,button=UnityEngine.EventSystems.PointerEventData.InputButton.Left };
    var hits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
    UnityEngine.EventSystems.EventSystem.current.RaycastAll(pointer,hits);
    if(hits.Count==0 || hits[0].gameObject!=button.gameObject) throw new System.Exception("Button is occluded: "+button.name);
    UnityEngine.EventSystems.ExecuteEvents.Execute(button.gameObject,pointer,UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
};
click(lab.modeButtons[0]);
click(lab.replayButton);
UnityEditor.EditorApplication.CallbackFunction tick=null;
tick=()=> {
    try {
        if(lab==null) throw new System.Exception("Showcase was stopped during verification.");
        if(++frames<5 || UnityEditor.EditorApplication.timeSinceStartup-started<.65) return;
        if(!lab.Paused) {click(lab.pauseButton); frames=0; started=UnityEditor.EditorApplication.timeSinceStartup;return;}
        var previous=UnityEngine.RenderTexture.active;UnityEngine.RenderTexture.active=lab.Output;
        var pixels=new UnityEngine.Texture2D(240,180,UnityEngine.TextureFormat.RGBA32,false);
        pixels.ReadPixels(new UnityEngine.Rect(0,0,240,180),0,0);pixels.Apply();UnityEngine.RenderTexture.active=previous;
        byte[] png=pixels.EncodeToPNG();UnityEngine.Object.DestroyImmediate(pixels);
        string hash=System.Convert.ToBase64String(System.Security.Cryptography.MD5.Create().ComputeHash(png));
        if(stage<7 && !hashes.Add(hash)) throw new System.Exception("Two effect modes produced identical output.");
        System.IO.File.WriteAllBytes(System.IO.Path.Combine(output,"mode-"+stage+".png"),png);
        receipts.Add(new {stage,lab.Mode,lab.Age,lab.Paused,lab.Original,hash,shaderErrors=UnityEditor.ShaderUtil.GetShaderMessages(lab.effectShader).Length});
        stage++;
        if(stage<7) click(lab.modeButtons[stage]);
        else if(stage==7) {click(lab.originalButton); if(!lab.Original) throw new System.Exception("Original comparison failed.");}
        else if(stage==8) {click(lab.originalButton);click(lab.modeButtons[5]);click(lab.slowButton);lab.lightStrength.value=1.5f;}
        else if(stage==9) {click(lab.modeButtons[6]);click(lab.slowButton);lab.lightStrength.value=1f;
            var corners=new UnityEngine.Vector3[4];lab.display.rectTransform.GetWorldCorners(corners);
            var point=UnityEngine.Vector3.Lerp(corners[0],corners[2],.55f);
            var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=point};
            UnityEngine.EventSystems.ExecuteEvents.Execute(lab.gameObject,pointer,UnityEngine.EventSystems.ExecuteEvents.pointerDownHandler);
        }
        else {
            UnityEditor.EditorApplication.update-=tick;
            var json=Newtonsoft.Json.JsonConvert.SerializeObject(new {success=true,scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,controls="All 7 mode buttons passed screen raycast and click; replay, pause, slow, original, intensity and stage pointer executed.",receipts},Newtonsoft.Json.Formatting.Indented);
            System.IO.File.WriteAllText(System.IO.Path.Combine(output,"verification.json"),json);
            UnityEngine.ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(output,"showcase-shield.png"));
            UnityEngine.Debug.Log("OCC_PIXEL_VFX_VERIFIED "+output);return;
        }
        frames=0;started=UnityEditor.EditorApplication.timeSinceStartup;
    } catch(System.Exception ex) {UnityEditor.EditorApplication.update-=tick;System.IO.File.WriteAllText(System.IO.Path.Combine(output,"verification-error.txt"),ex.ToString());UnityEngine.Debug.LogException(ex);}
};
UnityEditor.EditorApplication.update+=tick;
return new {scheduled=true,output};
