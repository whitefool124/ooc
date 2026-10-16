using OCC.Combat.Presentation;
using UnityEditor;
using UnityEngine;

namespace OCC.Combat.EditorTools
{
    public static class BattlefieldDeviceCandidateReview
    {
        private const string Menu="OCC/Art/Review devices and crystal ground material";
        [MenuItem(Menu)] public static void Toggle()
        {BattlefieldDeviceArtCandidates.ReviewEnabled=!BattlefieldDeviceArtCandidates.ReviewEnabled;EditorApplication.QueuePlayerLoopUpdate();}
        [MenuItem(Menu,true)] private static bool Validate()
        {UnityEditor.Menu.SetChecked(Menu,BattlefieldDeviceArtCandidates.ReviewEnabled);return true;}
    }
    public sealed class BattlefieldDeviceCandidateImporter:AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith("Assets/Game/Resources/Art/DeviceMaterialCandidates32/",System.StringComparison.Ordinal))return;
            var importer=(TextureImporter)assetImporter;
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.spritePixelsPerUnit=32;importer.filterMode=FilterMode.Point;importer.wrapMode=TextureWrapMode.Clamp;
            importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.npotScale=TextureImporterNPOTScale.None;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;
            importer.GetSourceTextureWidthAndHeight(out int width,out int height);
            settings.spriteAlignment=(int)SpriteAlignment.Custom;
            settings.spritePivot=assetPath.Contains("material_crystal_shards")?new Vector2(.5f,.5f):new Vector2(.5f,2f/height);
            importer.SetTextureSettings(settings);
        }
    }
}
