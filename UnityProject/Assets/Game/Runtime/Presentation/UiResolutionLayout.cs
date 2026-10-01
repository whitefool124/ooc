using UnityEngine;

namespace OCC.Combat.Presentation
{
    public static class UiResolutionLayout
    {
        // Quantize the source pixel multiplier, not the design canvas multiplier.
        // QHD is 8 source pixels (8/6 canvas scale); UHD is 12 (2 canvas scale).
        public static float ScreenScale(float width, float height)
        {
            float nativeScale = Mathf.Min(Mathf.Max(1f, width) / UiLayoutContract.NativeWidth,
                Mathf.Max(1f, height) / UiLayoutContract.NativeHeight);
            return (nativeScale >= 1f ? Mathf.Floor(nativeScale + .0001f) : nativeScale) /
                UiLayoutContract.NativePixelsAtReference;
        }

        // Screen pixels, bottom-left origin. Place the source grid on a physical pixel.
        public static Rect ScreenContentRect(float width, float height,
            float referenceWidth = UiLayoutContract.ReferenceWidth,
            float referenceHeight = UiLayoutContract.ReferenceHeight)
        {
            float scale = ScreenScale(width, height);
            float contentWidth = referenceWidth * scale;
            float contentHeight = referenceHeight * scale;
            return new Rect(Mathf.Floor((width - contentWidth) * .5f + .0001f),
                Mathf.Floor((height - contentHeight) * .5f + .0001f), contentWidth, contentHeight);
        }

        public static Matrix4x4 GuiMatrix(float width, float height)
        {
            Rect content = ScreenContentRect(width, height);
            return Matrix4x4.TRS(new Vector3(content.x, height - content.yMax, 0f), Quaternion.identity,
                Vector3.one * ScreenScale(width, height));
        }
    }
}
