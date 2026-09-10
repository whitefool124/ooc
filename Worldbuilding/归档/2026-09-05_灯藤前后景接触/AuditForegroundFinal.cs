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

    static object Field(object cell, string name) => cell.GetType().GetField(name).GetValue(cell);
    static BattlefieldCellPresentation Travel(BattlefieldCellPresentation m, Vector2 offset) =>
        new BattlefieldCellPresentation(m.Position,m.FloorTexture,m.FloorUv,m.FloorRotationDegrees,
        m.TerrainBoundaryTexture,m.TerrainBoundaryRotationDegrees,m.EnvironmentTexture,
        m.MoveOverlayTexture,m.MoveOverlayAlpha,m.AttackOverlayTexture,m.AttackOverlayAlpha,
        m.SkillOverlayTexture,m.SelectionOverlayTexture,m.UnitTexture,m.UnitUv,m.UnitTint,m.UnitOffset,
        m.ObjectTexture,m.ObjectLabel,m.ObjectLabelColor,m.LootTexture,m.Unit,m.Vitals,m.Statuses,
        m.Intent,m.IntentTexture,m.HoverText,offset,m.UnitVisualFacing,m.ObjectForegroundRows);

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

            var allCells=((IDictionary)Get(view,"cells")).Values.Cast<object>().ToArray();
            var frontCells=allCells.Where(c=>Field(c,"ObjectFront")!=null).ToArray();
            bool groundDepth=true;
            foreach (var placement in AcademyBattlefieldLayoutCatalog.VisualModules(bootstrap.CurrentLevelId).Where(p=>p.IsGroundAttachment)) {
                object c=((IDictionary)Get(view,"cells"))[new GridPosition(placement.X,placement.TopY)];
                var floor=(RawImage)Field(c,"Floor");var environment=(RawImage)Field(c,"Environment");
                var attachment=((RectTransform)Field(c,"Rect")).Find("结构_"+placement.AssetId);
                groundDepth &= attachment!=null&&floor.transform.GetSiblingIndex()<attachment.GetSiblingIndex()&&attachment.GetSiblingIndex()<environment.transform.GetSiblingIndex();
            }
            bool passThrough=frontCells.All(c=>!((RawImage)Field(c,"ObjectFront")).raycastTarget);
            var heroPosition=bootstrap.CurrentState.GetUnit("hero").Position;
            object heroCell=((IDictionary)Get(view,"cells"))[heroPosition];
            var heroImage=(RawImage)Field(heroCell,"Unit");
            var heroFront=(RawImage)Field(heroCell,"ObjectFront");
            Vector2 anchored=heroFront.rectTransform.anchoredPosition;
            var model=bootstrap.PresentBattlefieldCell(heroPosition);
            bool stationary=true, depthCorrect=true;
            foreach(float offset in new[]{-32f,0f,32f}) {
                Call(view,"RefreshCell",heroCell,Travel(model,new Vector2(0,offset)),bootstrap.BattlefieldViewport,false);
                Call(view,"RefreshUnitOrder");
                stationary &= heroFront.rectTransform.anchoredPosition==anchored;
                int order=heroFront.transform.GetSiblingIndex().CompareTo(heroImage.transform.GetSiblingIndex());
                depthCorrect &= offset<=0 ? order>0 : order<0;
            }
            Call(view,"RefreshCell",heroCell,model,bootstrap.BattlefieldViewport,false);
            Call(view,"RefreshUnitOrder");
            var tile=bootstrap.CurrentState.Map.GetTile(heroPosition);
            bool originalVine=tile.IsLampVine,originalScorch=tile.IsScorched;
            tile.IsLampVine=false;tile.IsScorched=true;
            Call(view,"RefreshCell",heroCell,bootstrap.PresentBattlefieldCell(heroPosition),bootstrap.BattlefieldViewport,false);
            bool removed=!heroFront.gameObject.activeSelf;
            tile.IsLampVine=originalVine;tile.IsScorched=originalScorch;
            Call(view,"RefreshCell",heroCell,bootstrap.PresentBattlefieldCell(heroPosition),bootstrap.BattlefieldViewport,false);
            Call(view,"RefreshUnitOrder");
            bool reused=ReferenceEquals(heroFront,Field(heroCell,"ObjectFront"))&&heroFront.gameObject.activeSelf;
            ((RectTransform)Get(view,"overlayLayerRect")).gameObject.SetActive(false);
            foreach(var cell in allCells)((RawImage)Field(cell,"Unit")).gameObject.SetActive(false);
            pixels=new Texture2D(width,height,TextureFormat.RGBA32,false);
            Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=texture;
            pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();
            Color32[] split=pixels.GetPixels32();
            foreach(var cell in frontCells) {
                ((RawImage)Field(cell,"ObjectFront")).gameObject.SetActive(false);
                var back=(RawImage)Field(cell,"Object");
                back.uvRect=new Rect(0,0,1,1);
                back.rectTransform.sizeDelta=Vector2.one*bootstrap.BattlefieldViewport.CellSize;
            }
            Canvas.ForceUpdateCanvases();camera.Render();
            pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();
            Color32[] whole=pixels.GetPixels32();int differences=0;
            for(int i=0;i<split.Length;i++)if(!split[i].Equals(whole[i]))differences++;
            return "frontImages="+frontCells.Length+" passThrough="+passThrough+" fixedTerrain="+stationary+
                " groundDepth="+groundDepth+" movingDepth="+depthCorrect+" burnedRemoved="+removed+" restoredReused="+reused+" emptyCompositionPixelDifferences="+differences;
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
