using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    // Fit final-resolution glyphs to the carrier's actual projected writing box.
    // Keep the 12px glyph grid; never enlarge a label independently of its paper.
    internal static class Hd2DReadingLayout
    {
        internal static void Fit(Text display, Text source, Vector2 size, bool heading, float outputHeight, float canvasScale)
        {
            display.text=source.text;display.color=source.color;display.font=source.font;
            display.supportRichText=source.supportRichText;
            display.alignByGeometry=false;
            display.lineSpacing=source.lineSpacing;display.alignment=source.alignment;
            display.horizontalOverflow=source.horizontalOverflow;
            // uGUI's Truncate rejects a whole line by font ascent/leading,
            // even when the visible glyphs fit. The carrier's RectMask2D clips
            // the actual glyphs instead, including short metric/identity rows.
            display.verticalOverflow=VerticalWrapMode.Overflow;
            display.rectTransform.sizeDelta=new Vector2(Mathf.Max(4,Mathf.Round(size.x/4)*4),Mathf.Max(4,Mathf.Round(size.y/4)*4));
            float target=Mathf.Max(FormalUiTheme.PixelAlignedFontSize(source.fontSize,false),heading?48:24);
            target*=outputHeight/1080f;
            int minimum=outputHeight<=540.5f?12:24;
            display.fontSize=Mathf.Max(minimum,Mathf.RoundToInt(target/canvasScale/12)*12);
            display.resizeTextForBestFit=false;display.fontStyle=FontStyle.Normal;
        }
    }
}
