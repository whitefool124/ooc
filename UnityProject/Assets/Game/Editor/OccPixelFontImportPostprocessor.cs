using System;
using UnityEditor;
using UnityEngine;

namespace OCC.Combat.Presentation.Editor
{
    public sealed class OccPixelFontImportPostprocessor : AssetPostprocessor
    {
        public const string FontPath = "Assets/Game/Resources/Fonts/FusionPixel12ProportionalZhHans.ttf";

        private void OnPreprocessAsset()
        {
            if (!string.Equals(assetPath, FontPath, StringComparison.Ordinal)) return;
            if (!(assetImporter is TrueTypeFontImporter importer)) return;
            importer.fontSize = 12;
            importer.characterSpacing = 0;
            importer.characterPadding = 1;
            importer.includeFontData = true;
            importer.fontRenderingMode = FontRenderingMode.HintedRaster;
        }
    }
}
