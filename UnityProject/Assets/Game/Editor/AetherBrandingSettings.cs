using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace OCC.Combat.EditorTools
{
    public static class AetherBrandingSettings
    {
        public static void Apply()
        {
            const string path = "Assets/Game/Art/Branding/AetherAppIcon.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Missing prepared application icon: " + path);
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 512;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (icon == null || icon.width != 512 || icon.height != 512)
                throw new InvalidOperationException("Invalid 以太 application icon delivery.");
            PlayerSettings.productName = "以太";
            // Preserve company identity: only the product namespace is migrated.
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { icon }, IconKind.Any);
            int[] sizes = PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.Standalone, IconKind.Any);
            if (sizes.Length > 0)
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Standalone, sizes.Select(_ => icon).ToArray(), IconKind.Any);
            if (PlayerSettings.productName != "以太") throw new InvalidOperationException("Branding readback failed.");
            Debug.Log("AETHER_BRANDING_APPLIED 以太; icon=" + AssetDatabase.AssetPathToGUID(path));
        }
    }
}
