using System;
using OCC.Combat.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace OCC.Combat.EditorTools
{
    public static class CombatPortraitHudAuthoring
    {
        [MenuItem("OCC/Combat HUD Portraits/Build Identity Prefab")]
        public static void Build()
        {
            if (Application.isPlaying || Application.dataPath.Replace('\\', '/') != "E:/数据库/OCC_Codex/UnityProject/Assets")
                throw new InvalidOperationException("Main project Edit Mode required");
            string path = "Assets/Game/Resources/UI/Prefabs/FormalCombatPortraitCard.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) throw new InvalidOperationException("Identity prefab already exists; inspect before rebuilding");
            Scene preview = EditorSceneManager.NewPreviewScene();
            GameObject root = null;
            try
            {
                root = new GameObject("FormalCombatPortraitCard", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CombatPortraitCardView));
                SceneManager.MoveGameObjectToScene(root, preview);
                RectTransform rect = (RectTransform)root.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); rect.sizeDelta = new Vector2(128, 152);
                Image surface = root.GetComponent<Image>(); surface.color = FormalUiTheme.Panel;
                Button button = root.GetComponent<Button>(); button.targetGraphic = surface; button.transition = Selectable.Transition.None;
                GameObject portraitObject = new GameObject("独立头肩头像", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
                portraitObject.transform.SetParent(root.transform, false);
                RectTransform p = (RectTransform)portraitObject.transform; p.anchorMin = p.anchorMax = p.pivot = new Vector2(0, 1); p.sizeDelta = new Vector2(128, 128);
                RawImage portrait = portraitObject.GetComponent<RawImage>(); portrait.raycastTarget = false;
                Image frame = ImageAt("象牙黄铜头像框", root.transform, Vector2.zero, new Vector2(128, 128), Color.clear);
                Sprite sprite = Resources.Load<Sprite>("Art/CombatHudChrome/portrait_frame");
                if (sprite != null) { frame.sprite = sprite; frame.color = Color.white; }
                FormalUiKit.ThinFrame(root.transform, new Vector2(128, 128), FormalUiTheme.Rule, "基础轮廓");
                Image focus = FormalUiKit.FocusFrame(root.transform); focus.rectTransform.offsetMin = new Vector2(-2, 24); focus.rectTransform.offsetMax = new Vector2(2, 2); focus.raycastTarget = false; focus.gameObject.SetActive(false);
                Image active = ImageAt("当前行动标记", root.transform, new Vector2(0, 2), new Vector2(128, 4), FormalUiTheme.Cyan);
                Image faction = ImageAt("敌我标记", root.transform, new Vector2(0, -124), new Vector2(128, 4), FormalUiTheme.Danger);
                Text caption = FormalUiKit.Label("顺位与姓名", "", root.transform, new Vector2(0, -128), new Vector2(128, 24), 18, FormalUiTheme.Ink, TextAnchor.MiddleCenter); caption.raycastTarget = false;
                root.GetComponent<CombatPortraitCardView>().Configure(button, portrait, frame, focus, active, faction, caption);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { if (root != null) UnityEngine.Object.DestroyImmediate(root); EditorSceneManager.ClosePreviewScene(preview); }
            string shellPath = "Assets/Game/Resources/UI/Prefabs/FormalCombatHudShell.prefab";
            GameObject shellRoot = PrefabUtility.LoadPrefabContents(shellPath);
            try
            {
                var shell = shellRoot.GetComponent<FormalCombatHudShellView>(); shell.ValidateReferences();
                shell.HeroStatus.sizeDelta = new Vector2(480, 288);
                shell.ActionOrder.sizeDelta = new Vector2(976, 192); shell.ActionOrder.anchoredPosition = new Vector2(928, -80);
                Image orderSurface = shell.ActionOrder.GetComponent<Image>(); orderSurface.color = Color.clear; orderSurface.raycastTarget = false;
                PrefabUtility.SaveAsPrefabAsset(shellRoot, shellPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(shellRoot); }
            Debug.Log("OCC portrait-card prefab authored; stable shell references preserved.");
        }
        private static Image ImageAt(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform; rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1); rect.anchoredPosition = position; rect.sizeDelta = size;
            var image = go.GetComponent<Image>(); image.color = color; image.raycastTarget = false; return image;
        }
    }
}
