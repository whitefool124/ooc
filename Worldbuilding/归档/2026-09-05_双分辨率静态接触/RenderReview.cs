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
            ((CombatFormalVisualAssets)Get(bootstrap, "formalAssets")).LoadRuntime();
            bootstrap.BattlefieldViewport.ResetOverview();
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
            const int width = 1920, height = 1080;
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
            return Convert.ToBase64String(pixels.EncodeToPNG());
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
