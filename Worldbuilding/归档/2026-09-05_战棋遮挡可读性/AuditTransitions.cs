using System.Collections.Generic;
using UnityEngine.EventSystems;
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
    const bool USE_CANDIDATE = false;
    static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    static object Get(object target, string field) => target.GetType().GetField(field, Flags | BindingFlags.Public).GetValue(target);
    static void Set(object target, string field, object value) => target.GetType().GetField(field, Flags).SetValue(target, value);
    static object Call(object target, string method, params object[] args) => target.GetType().GetMethod(method, Flags).Invoke(target, args);

    static string Audit(FormalBattlefieldView view, Scene scene, Camera camera)
    {
        var contours=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<CombatUnitContourGraphic>(true)).ToArray();
        int boundaryBefore=contours.Sum(c=>c.BoundaryBuildCount), geometryBefore=contours.Sum(c=>c.GeometryChangeCount);
        var cache=(CombatTextureAlphaMaskCache)Get(view,"unitHitMasks");
        int reads=cache.CaptureCount;
        var timer=System.Diagnostics.Stopwatch.StartNew();
        for(int i=0;i<100;i++) Call(view,"RefreshUnitOrder");
        timer.Stop();
        Canvas.ForceUpdateCanvases(); camera.Render();
        if(contours.Sum(c=>c.BoundaryBuildCount)!=boundaryBefore || contours.Sum(c=>c.GeometryChangeCount)!=geometryBefore || cache.CaptureCount!=reads)
            throw new InvalidOperationException("Static refresh rebuilt contour data or read texture.");
        var es=UnityEngine.Object.FindFirstObjectByType<EventSystem>();
        if(es==null) es=new GameObject("AuditEventSystem").AddComponent<EventSystem>();
        var checks=new List<string>();
        foreach(DictionaryEntry entry in (IDictionary)Get(view,"cells"))
        {
            var cell=entry.Value; var unit=(RawImage)Get(cell,"Unit");
            if(!unit.gameObject.activeSelf) continue;
            foreach(string key in new[]{"Health","Shield"})
            {
                var bar=Get(cell,key); var barRoot=(GameObject)Get(bar,"Root");
                if(!barRoot.activeInHierarchy) continue;
                var rect=(RectTransform)Get(bar,"Rect");
                var point=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center));
                var pointer=new PointerEventData(es){position=point,button=PointerEventData.InputButton.Left,clickCount=1};
                var hits=new List<RaycastResult>(); rect.GetComponentInParent<GraphicRaycaster>().Raycast(pointer,hits);
                if(hits.Count==0) throw new InvalidOperationException("No vital raycast: "+entry.Key+"/"+key);
                var receiver=ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject);
                if(receiver!=barRoot) throw new InvalidOperationException("Wrong vital receiver: "+entry.Key+"/"+key+" -> "+hits[0].gameObject.name);
                var binding=barRoot.GetComponents<MonoBehaviour>().First(c=>c.GetType().FullName=="OCC.Combat.Presentation.BattlefieldCellPointer");
                GridPosition expected=(GridPosition)entry.Key;
                if(!expected.Equals((GridPosition)Get(binding,"position"))) throw new InvalidOperationException("Wrong bound cell.");
                string hover="",left="",right="";
                object oldHover=Get(binding,"enter"), oldLeft=Get(binding,"primaryClick"), oldRight=Get(binding,"contextClick");
                try
                {
                    Set(binding,"enter",(Action<GridPosition>)(p=>hover=p.ToString()));
                    Set(binding,"primaryClick",(Action<GridPosition,int>)((p,count)=>left=p.ToString()));
                    Set(binding,"contextClick",(Action<GridPosition>)(p=>right=p.ToString()));
                    ExecuteEvents.Execute(barRoot,pointer,ExecuteEvents.pointerEnterHandler);
                    ExecuteEvents.Execute(barRoot,pointer,ExecuteEvents.pointerClickHandler);
                    pointer.button=PointerEventData.InputButton.Right;
                    ExecuteEvents.Execute(barRoot,pointer,ExecuteEvents.pointerDownHandler);
                    if(hover!=expected.ToString()||left!=hover||right!=hover) throw new InvalidOperationException("Vital callback mismatch.");
                }
                finally {Set(binding,"enter",oldHover);Set(binding,"primaryClick",oldLeft);Set(binding,"contextClick",oldRight);}
                if(ExecuteEvents.GetEventHandler<IDragHandler>(barRoot)==null) throw new InvalidOperationException("Vital lost viewport drag.");
                checks.Add(expected+"/"+key);
            }
        }
        if(contours.Any(c=>c.raycastTarget)) throw new InvalidOperationException("Contour intercepted input.");
        int occluded=view.OccludedUnitCount,hidden=view.OcclusionPixelCount;
        var occupied=((IDictionary)Get(view,"cells")).Values.Cast<object>().First(c=>((RawImage)Get(c,"Unit")).gameObject.activeSelf);
        var disappearing=(RawImage)Get(occupied,"Unit"); disappearing.gameObject.SetActive(false);
        Call(view,"RefreshUnitOrder");
        var disappearingContour=(CombatUnitContourGraphic)Get(occupied,"Contour");
        if(disappearingContour!=null && disappearingContour.gameObject.activeSelf) throw new InvalidOperationException("Removed unit left contour.");
        disappearing.gameObject.SetActive(true); Call(view,"RefreshUnitOrder");

        // Sample production presentation transitions without advancing combat or saving a scene.
        var moving=((IDictionary)Get(view,"cells")).Values.Cast<object>().First(c=>((RawImage)Get(c,"Unit")).gameObject.activeSelf);
        var movingImage=(RawImage)Get(moving,"Unit");
        Vector2 savedPosition=movingImage.rectTransform.anchoredPosition;
        Rect savedUv=movingImage.uvRect; Color savedTint=movingImage.color;
        movingImage.rectTransform.anchoredPosition += new Vector2(64,0); Call(view,"RefreshUnitOrder");
        var movingContour=(CombatUnitContourGraphic)Get(moving,"Contour");
        if(movingContour!=null && movingContour.gameObject.activeSelf &&
            movingContour.rectTransform.anchoredPosition!=movingImage.rectTransform.anchoredPosition)
            throw new InvalidOperationException("Contour failed to follow attack/move display offset.");
        movingImage.rectTransform.anchoredPosition=savedPosition;
        movingImage.uvRect=new Rect(1,0,-1,1); Call(view,"RefreshUnitOrder");
        movingImage.color=Color.clear; Call(view,"RefreshUnitOrder");
        if(movingContour!=null && movingContour.gameObject.activeSelf) throw new InvalidOperationException("Invisible source left contour.");
        movingImage.color=savedTint; movingImage.uvRect=savedUv; Call(view,"RefreshUnitOrder");
        int withFront=view.OcclusionPixelCount;
        var fronts=((IDictionary)Get(view,"cells")).Values.Cast<object>().Select(c=>(RawImage)Get(c,"ObjectFront")).Where(i=>i!=null && i.gameObject.activeSelf).ToArray();
        foreach(var front in fronts) front.gameObject.SetActive(false);
        Call(view,"RefreshUnitOrder");
        if(view.OcclusionPixelCount>withFront) throw new InvalidOperationException("Removing terrain created more hidden contour.");
        int withoutFront=view.OcclusionPixelCount;
        foreach(var front in fronts) front.gameObject.SetActive(true);
        Call(view,"RefreshUnitOrder");

        return "lifecycle=PASS|withFront="+withFront+"|withoutFront="+withoutFront+"|occluded="+occluded+"|hidden="+hidden+"|maskCaptures="+reads+"|boundaryBuilds="+boundaryBefore+"|geometryChanges="+geometryBefore+
            "|warm100Ms="+timer.Elapsed.TotalMilliseconds.ToString("F3")+"|vitalTargets="+string.Join(",",checks)+"|hoverLeftRight=PASS|drag=PASS|remove=PASS|raycastTransparent=PASS";
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
        Texture2D auditCandidate = null;
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
            var auditAssets = (CombatFormalVisualAssets)Get(bootstrap, "formalAssets"); auditAssets.LoadRuntime();
            if (USE_CANDIDATE)
            {
                auditCandidate = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                auditCandidate.name = "tether_hound_compact_v1_QA_PENDING";
                auditCandidate.hideFlags = HideFlags.HideAndDontSave;
                if (!ImageConversion.LoadImage(auditCandidate, Convert.FromBase64String("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"), false))
                    throw new InvalidOperationException("Candidate decode failed.");
                auditCandidate.filterMode = FilterMode.Point; auditCandidate.wrapMode = TextureWrapMode.Clamp;
                if (auditCandidate.width != 64 || auditCandidate.height != 64) throw new InvalidOperationException("Invalid candidate size.");
                ((IDictionary)Get(auditAssets, "units"))["tether_hound"] = auditCandidate;
                ((IDictionary)Get(auditAssets, "enemyAnimations"))["tether_hound"] = new Texture2D[] { auditCandidate, auditCandidate };
            }
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
            var hound = bootstrap.CurrentState.Units.Values.First(u => u.EnemyArchetypeId == "tether_hound");
            var shown = auditAssets.Unit(hound);
            return Audit(view, temporary, camera) + "|" + width + "|" + height + "|" + CombatUnitHudLayout.UnitPresentationRect(bootstrap.BattlefieldViewport.CellRect(hound.Position)).width + "|" + shown.name + "|" + shown.width + "x" + shown.height + "|" + shown.filterMode + "|" + shown.wrapMode + "\n" + Convert.ToBase64String(pixels.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous;
            if (pixels != null) UnityEngine.Object.DestroyImmediate(pixels);
            if (auditCandidate != null) UnityEngine.Object.DestroyImmediate(auditCandidate);
            if (texture != null) { texture.Release(); UnityEngine.Object.DestroyImmediate(texture); }
            SceneManager.SetActiveScene(original);
            EditorSceneManager.CloseScene(temporary, true);
        }
    }
}






