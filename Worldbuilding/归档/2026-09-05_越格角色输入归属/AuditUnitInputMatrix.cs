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

            var body=((IDictionary)Get(view,"cells")).Values.Cast<object>().First(c=>(string)c.GetType().GetField("PresentedUnitId").GetValue(c)=="hero");
            var graphic=(RawImage)body.GetType().GetField("Unit").GetValue(body);
            var rect=graphic.rectTransform;var source=(Texture2D)graphic.texture;Color32[] alpha=ReadPixels(source);
            var board=(RectTransform)Get(view,"boardRect");var heroPosition=bootstrap.CurrentState.GetUnit("hero").Position;
            var raycaster=graphic.GetComponentInParent<GraphicRaycaster>();
            Func<Vector2,MonoBehaviour> receiver=point=>{
                var data=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=point};
                var hits=new System.Collections.Generic.List<UnityEngine.EventSystems.RaycastResult>();raycaster.Raycast(data,hits);
                foreach(var hit in hits){var pointer=hit.gameObject.GetComponents<MonoBehaviour>().FirstOrDefault(c=>c.GetType().Name=="BattlefieldCellPointer");if(pointer!=null)return pointer;}return null;
            };
            Func<int,int,Vector3> worldAt=(x,y)=>rect.TransformPoint(new Vector3(rect.rect.xMin+(x+.5f)/source.width*rect.rect.width,rect.rect.yMin+(y+.5f)/source.height*rect.rect.height,0));
            Vector2 sample=Vector2.zero;GridPosition underlying=default;bool found=false;
            for(int y=source.height-1;y>=0&&!found;y--)for(int x=0;x<source.width;x++){
                if(alpha[y*source.width+x].a<128)continue;
                Vector3 world=worldAt(x,y);Vector3 p=board.InverseTransformPoint(world);float size=bootstrap.BattlefieldViewport.CellSize;
                var cell=new GridPosition(Mathf.FloorToInt(p.x/size),8-Mathf.FloorToInt(-p.y/size));
                if(cell==heroPosition)continue;sample=camera.WorldToScreenPoint(world);underlying=cell;found=true;break;
            }
            var hitBody=receiver(sample);var hitCorner=receiver(camera.WorldToScreenPoint(worldAt(0,source.height-1)));
            Func<MonoBehaviour,string> owner=p=>p==null?"none":p.GetType().GetField("position",Flags).GetValue(p).ToString();

            var maskCache=(CombatTextureAlphaMaskCache)Get(view,"unitHitMasks");
            int initialCaptures=maskCache.CaptureCount;
            for(int n=0;n<10;n++){
                receiver(sample);receiver(camera.WorldToScreenPoint(worldAt(0,source.height-1)));
                foreach(DictionaryEntry entry in (IDictionary)Get(view,"cells"))Call(view,"RefreshCell",entry.Value,
                    bootstrap.PresentBattlefieldCell((GridPosition)entry.Key),bootstrap.BattlefieldViewport,false);
            }
            bool reused=maskCache.CaptureCount==initialCaptures;
            Vector3 originalPosition=rect.position;
            Vector3 oldWorld=camera.ScreenToWorldPoint(new Vector3(sample.x,sample.y,10));
            Vector2 clippedPoint=new Vector2(5,height*.5f);
            rect.position+=camera.ScreenToWorldPoint(new Vector3(clippedPoint.x,clippedPoint.y,10))-oldWorld;
            Canvas.ForceUpdateCanvases();camera.Render();
            var clipped=receiver(clippedPoint);
            bool clipPassed=clipped==null||clipped.gameObject!=graphic.gameObject;
            rect.position=originalPosition;Canvas.ForceUpdateCanvases();camera.Render();
            string dragOwner=UnityEngine.EventSystems.ExecuteEvents.GetEventHandler<UnityEngine.EventSystems.IBeginDragHandler>(graphic.gameObject)?.name;
            GridPosition clicked=new GridPosition(-1,-1),hovered=clicked,inspected=clicked;
            var eventData=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){position=sample,button=UnityEngine.EventSystems.PointerEventData.InputButton.Left,clickCount=1};
            if(hitBody!=null){
                var clickField=hitBody.GetType().GetField("primaryClick",Flags);var click=(Action<GridPosition,int>)clickField.GetValue(hitBody);
                clickField.SetValue(hitBody,new Action<GridPosition,int>((p,n)=>{clicked=p;click(p,n);}));
                var hoverField=hitBody.GetType().GetField("enter",Flags);var hover=(Action<GridPosition>)hoverField.GetValue(hitBody);
                hoverField.SetValue(hitBody,new Action<GridPosition>(p=>{hovered=p;hover(p);}));
                var contextField=hitBody.GetType().GetField("contextClick",Flags);var context=(Action<GridPosition>)contextField.GetValue(hitBody);
                contextField.SetValue(hitBody,new Action<GridPosition>(p=>{inspected=p;context(p);}));
                UnityEngine.EventSystems.ExecuteEvents.Execute(hitBody.gameObject,eventData,UnityEngine.EventSystems.ExecuteEvents.pointerEnterHandler);
                UnityEngine.EventSystems.ExecuteEvents.Execute(hitBody.gameObject,eventData,UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
                eventData.button=UnityEngine.EventSystems.PointerEventData.InputButton.Right;
                UnityEngine.EventSystems.ExecuteEvents.Execute(hitBody.gameObject,eventData,UnityEngine.EventSystems.ExecuteEvents.pointerDownHandler);
            }
            return "resolution="+width+"x"+height+" cellSize="+bootstrap.BattlefieldViewport.CellSize+" expected="+heroPosition+
                " bodyReceiver="+owner(hitBody)+" bodyObject="+(hitBody?.gameObject.name??"none")+" underlying="+underlying+
                " cornerReceiver="+owner(hitCorner)+" cornerIsBody="+ReferenceEquals(hitBody,hitCorner)+
                " clicked="+clicked+" hovered="+hovered+" inspected="+inspected+
                " cacheCaptures="+maskCache.CaptureCount+" cacheFailures="+maskCache.FailedCaptureCount+" captureMs="+maskCache.CaptureMilliseconds+" reused="+reused+" clipped="+clipPassed+" dragOwner="+dragOwner+" pixelComponents="+temporary.GetRootGameObjects().Sum(r=>r.GetComponentsInChildren<CombatUnitPixelRaycast>(true).Length)+" heroPosition="+bootstrap.CurrentState.GetUnit("hero").Position+" heroAP="+bootstrap.CurrentState.GetUnit("hero").ActionPoints;

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
