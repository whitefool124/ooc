using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using OCC.Combat;
using OCC.Combat.Presentation;

public static class RenderReview
{
    static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static object Get(object target, string field) => target.GetType().GetField(field, Flags).GetValue(target);
    static void Set(object target, string field, object value) => target.GetType().GetField(field, Flags).SetValue(target, value);
    static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, Flags).Invoke(target, args);

    static Color32[] ReadPixels(Texture2D source) {
        RenderTexture old=RenderTexture.active;
        RenderTexture rt=RenderTexture.GetTemporary(source.width,source.height,0,RenderTextureFormat.ARGB32);
        Texture2D cpu=null;
        try { Graphics.Blit(source,rt);RenderTexture.active=rt;cpu=new Texture2D(source.width,source.height,TextureFormat.RGBA32,false);
            cpu.ReadPixels(new Rect(0,0,source.width,source.height),0,0);cpu.Apply();return cpu.GetPixels32(); }
        finally { RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);if(cpu!=null)UnityEngine.Object.DestroyImmediate(cpu); }
    }

    public static string Run()
    {
        if (Application.dataPath != "E:/数据库/OCC_Codex/UnityProject/Assets" || Application.isPlaying)
            throw new InvalidOperationException("Wrong project or Play Mode active.");
        if (UnityEngine.Object.FindObjectsByType<UnityEngine.EventSystems.EventSystem>(FindObjectsInactive.Include).Length != 0)
            throw new InvalidOperationException("Existing event system requires separate isolation.");
        Scene original = SceneManager.GetActiveScene();
        Scene temporary = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        RenderTexture texture = null;
        Texture2D pixels = null;
        RenderTexture previous = RenderTexture.active;
        try
        {
            SceneManager.SetActiveScene(temporary);
            var go = new GameObject("OCC_Static_Review");
            var bootstrap = go.AddComponent<CombatPrototypeBootstrap>();
            bootstrap.enabled = false;
            var run = RogueliteMapRun.CreateFirstRunV1(9001);
            run.AcknowledgeFirstRunOrigin(); run.SelectNode("B1");
            typeof(CombatPrototypeBootstrap).GetProperty("mapRun", Flags).SetValue(bootstrap, run);
            Call(bootstrap, "BuildCombatFromSceneStageTwo");
            bootstrap.OpenDeveloperBriefing(); bootstrap.StartDeveloperCombat();
            var testUnits=bootstrap.CurrentState.Units.Values.Where(u=>!u.IsHero).OrderBy(u=>u.Id).ToArray();
            CombatEffectExecutor.Execute(bootstrap.CurrentState,"hero",CombatEffect.Move(new GridPosition(4,2),Facing.East));


            CombatEffectExecutor.Execute(bootstrap.CurrentState,testUnits[0].Id,CombatEffect.Move(new GridPosition(4,3),Facing.South));
            CombatEffectExecutor.Execute(bootstrap.CurrentState,testUnits[1].Id,CombatEffect.Move(new GridPosition(5,2),Facing.West));
            CombatEffectExecutor.Execute(bootstrap.CurrentState,"hero",CombatEffect.ApplyStatus("hero",StatusType.Bound,2),CombatEffect.ApplyStatus("hero",StatusType.ArmorBreak,2));
            ((CombatFormalVisualAssets)Get(bootstrap, "formalAssets")).LoadRuntime();
            bootstrap.BattlefieldViewport.ResetOverview(); bootstrap.BattlefieldViewport.ZoomAt(720,464,1); bootstrap.BattlefieldViewport.Focus(bootstrap.CurrentState.GetUnit("hero").Position);
            var view = go.AddComponent<FormalBattlefieldView>(); view.Initialize(bootstrap);
            Call(view, "EnsureUi");
            Call(view, "EnsureCells", bootstrap.CurrentState.Map.Width, bootstrap.CurrentState.Map.Height);
            Call(view, "RefreshGeometry", bootstrap.BattlefieldViewport);
            Call(view, "RefreshStructures", bootstrap.CurrentLevelId, bootstrap.BattlefieldViewport);
            var destinations = FormalBattlefieldView.CollectIntentDestinations(bootstrap.CurrentState.Units.Values
                .Where(unit => !unit.IsHero && unit.IsAlive).Select(bootstrap.EnemyIntent));
            foreach (DictionaryEntry entry in (IDictionary)Get(view, "cells"))
            {
                var position = (GridPosition)entry.Key;
                Call(view, "RefreshCell", entry.Value, bootstrap.PresentBattlefieldCell(position), bootstrap.BattlefieldViewport,
                    destinations.Contains(position));
            }
            Call(view, "RefreshUnitOrder");
            var hud = go.AddComponent<FormalCombatHud>(); hud.Initialize(bootstrap); Call(hud, "Refresh");
            const int width = 960, height = 540;
            texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            texture.antiAliasing = 1; texture.Create();
            var camera = new GameObject("ReviewCamera").AddComponent<Camera>();
            camera.enabled = false; camera.orthographic = true; camera.orthographicSize = 540;
            camera.transform.position = new Vector3(0, 0, -1000);
            camera.nearClipPlane = .1f; camera.farClipPlane = 2000;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = FormalUiTheme.Ink;
            camera.cullingMask = 1 << 31; camera.targetTexture = texture;
            foreach (var root in temporary.GetRootGameObjects())
                foreach (var transform in root.GetComponentsInChildren<Transform>(true)) transform.gameObject.layer = 31;
            foreach (var canvas in temporary.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Canvas>()))
            {
                canvas.GetComponent<CanvasScaler>().enabled = false;
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera;
                canvas.planeDistance = 10; canvas.scaleFactor = width / 1920f;
            }
            Canvas.ForceUpdateCanvases(); camera.Render();
            RenderTexture.active = texture;
            pixels = new Texture2D(width, height, TextureFormat.RGBA32, false);
            pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0); pixels.Apply();


            var units=((IDictionary)Get(view,"cells")).Values.Cast<object>()
                .Where(c=>(string)c.GetType().GetField("PresentedUnitId").GetValue(c)!=null)
                .Select(c=>new {Id=(string)c.GetType().GetField("PresentedUnitId").GetValue(c),
                    Image=(RawImage)c.GetType().GetField("Unit").GetValue(c)})
                .OrderByDescending(u=>u.Image.depth).ToArray();
            int overlaps=0,fallbacks=0,mismatches=0;string evidence="";
            for(int a=0;a<units.Length;a++)for(int b=a+1;b<units.Length;b++){
                var front=units[a];var back=units[b];bool testedOverlap=false,testedFallback=false;
                var rect=front.Image.rectTransform;
                for(int y=0;y<64&&(!testedOverlap||!testedFallback);y++)for(int x=0;x<64&&(!testedOverlap||!testedFallback);x++){
                    Vector3 world=rect.TransformPoint(new Vector3(rect.rect.xMin+(x+.5f)/64*rect.rect.width,rect.rect.yMin+(y+.5f)/64*rect.rect.height,0));
                    Vector2 point=camera.WorldToScreenPoint(world);
                    bool frontHit=front.Image.GetComponent<CombatUnitPixelRaycast>().IsRaycastLocationValid(point,camera);
                    if(!back.Image.GetComponent<CombatUnitPixelRaycast>().IsRaycastLocationValid(point,camera))continue;
                    var expected=frontHit?front:back;
                    if(units.Any(u=>u.Image.depth>expected.Image.depth&&u.Image.GetComponent<CombatUnitPixelRaycast>().IsRaycastLocationValid(point,camera)))continue;
                    if(frontHit&&testedOverlap||!frontHit&&testedFallback)continue;
                    var data=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=point};
                    var hits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();
                    front.Image.GetComponentInParent<GraphicRaycaster>().Raycast(data,hits);
                    if(hits.Count==0||hits[0].gameObject.GetComponent<CombatUnitPixelRaycast>()==null)continue;
                    bool correct=hits[0].gameObject==expected.Image.gameObject;
                    if(!correct)mismatches++;
                    evidence+=" "+(frontHit?"opaque":"transparent")+"("+front.Id+"/"+back.Id+")->"+expected.Id+":"+correct;
                    if(frontHit){testedOverlap=true;overlaps++;}else{testedFallback=true;fallbacks++;}
                }
            }
            return "overlapSamples="+overlaps+" transparentFallbackSamples="+fallbacks+" mismatches="+mismatches+evidence;
        }
        finally
        {
            RenderTexture.active = previous;
            if (pixels != null) UnityEngine.Object.DestroyImmediate(pixels);
            if (texture != null) { texture.Release(); UnityEngine.Object.DestroyImmediate(texture); }
            SceneManager.SetActiveScene(original);
            EditorSceneManager.CloseScene(temporary, true);
        }
    }
}
