using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    // One cache per label. No global dictionary retains destroyed pages.
    internal sealed class Hd2DReadingProjectionCache
    {
        private bool valid;
        private Matrix4x4 sourceMatrix, viewProjection;
        private Rect sourceRect, outputRect, viewportRect;
        private float canvasScale, spacing;
        private string text;
        private Color color;
        private Font font;
        private int fontSize;
        private TextAnchor alignment;
        private HorizontalWrapMode wrap;
        private bool richText;
        private Matrix4x4[] maskMatrices;
        private Rect[] maskRects;
        private bool[] maskEnabled;

        internal bool NeedsRefresh(Text source, Matrix4x4 projection, Rect output, Rect viewport, float scale, RectMask2D[] masks = null)
        {
            var matrix = source.rectTransform.localToWorldMatrix;
            var rect = source.rectTransform.rect;
            bool changed = !valid || sourceMatrix != matrix || viewProjection != projection || sourceRect != rect ||
                outputRect != output || viewportRect != viewport || canvasScale != scale || text != source.text ||
                color != source.color || font != source.font || fontSize != source.fontSize || spacing != source.lineSpacing ||
                alignment != source.alignment || wrap != source.horizontalOverflow || richText != source.supportRichText;
            if (masks != null)
            {
                if (maskMatrices == null || maskMatrices.Length != masks.Length)
                {
                    maskMatrices = new Matrix4x4[masks.Length]; maskRects = new Rect[masks.Length]; maskEnabled = new bool[masks.Length]; changed = true;
                }
                for (int i = 0; i < masks.Length; i++)
                {
                    var mask = masks[i]; bool enabled = mask != null && mask.isActiveAndEnabled;
                    var maskMatrix = enabled ? mask.rectTransform.localToWorldMatrix : Matrix4x4.identity;
                    var maskRect = enabled ? mask.rectTransform.rect : default;
                    changed |= maskEnabled[i] != enabled || maskMatrices[i] != maskMatrix || maskRects[i] != maskRect;
                    maskEnabled[i] = enabled; maskMatrices[i] = maskMatrix; maskRects[i] = maskRect;
                }
            }
            if (!changed) return false;
            valid = true; sourceMatrix = matrix; viewProjection = projection; sourceRect = rect;
            outputRect = output; viewportRect = viewport; canvasScale = scale; text = source.text;
            color = source.color; font = source.font; fontSize = source.fontSize; spacing = source.lineSpacing;
            alignment = source.alignment; wrap = source.horizontalOverflow; richText = source.supportRichText;
            return true;
        }
    }
}
