using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using OCC.Combat.Presentation;

namespace OCC.EditorTools
{
    public static class MainMenuEAuthoring
    {
        public static string Build()
        {
            const string folder = "Assets/Game/Resources/Art/MainMenuE/";
            foreach (string name in new[] { "scene_closed", "scene_open" })
            {
                string path = folder + name + ".png";
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) throw new InvalidOperationException("Missing scene texture: " + path);
                importer.textureType = TextureImporterType.Default;
                importer.filterMode = FilterMode.Point; importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed; importer.mipmapEnabled = false;
                importer.npotScale = TextureImporterNPOTScale.None; importer.maxTextureSize = 512;
                importer.alphaSource = TextureImporterAlphaSource.None; importer.isReadable = false;
                importer.SaveAndReimport();
            }
            Shader shader = Shader.Find("OCC/UI/MainMenuPixelScene");
            if (shader == null || !shader.isSupported) throw new InvalidOperationException("Scene shader unavailable or unsupported.");
            string matPath = folder + "scene_atmosphere.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, matPath); }
            else Undo.RecordObject(material, "Update E scene atmosphere");
            material.shader = shader;
            Texture2D closed = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "scene_closed.png");
            Texture2D open = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "scene_open.png");
            if (closed == null || open == null) throw new InvalidOperationException("Processed scene frames missing.");
            material.SetTexture("_MainTex", closed); material.SetTexture("_OpenTex", open);
            material.SetFloat("_Zoom", 1); material.SetFloat("_Open", 0); material.SetFloat("_Focus", 0); material.SetFloat("_Motion", 1);
            EditorUtility.SetDirty(material); AssetDatabase.SaveAssetIfDirty(material);
            const string prefabPath = "Assets/Game/Resources/UI/MainMenuEDesk.prefab";
            Directory.CreateDirectory(Path.GetDirectoryName(prefabPath));
            MainMenuDeskScene scene = MainMenuDeskScene.CreateTemplate(closed, material);
            try
            {
                scene.Configure(false, null, null, null, null, null);
                PrefabUtility.SaveAsPrefabAsset(scene.gameObject, prefabPath);
            }
            finally { UnityEngine.Object.DestroyImmediate(scene.gameObject); }
            var saved = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (saved == null || saved.GetComponent<MainMenuDeskScene>() == null) throw new InvalidOperationException("Prefab readback failed.");
            return "Saved E main menu prefab; 320x180 assembled native scene, intact 64px object canvases; " + saved.GetComponentsInChildren<UnityEngine.UI.Button>(true).Length + " buttons.";
        }
    }
}
