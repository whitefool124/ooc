using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    // UI symbols are read beside 24px text. Their source canvases use the same
    // reference display scale; room props and character sprites stay physical.
    internal static class Hd2DUiIconSizing
    {
        internal static void Apply(Image image, int pixelScale = 4)
        {
            if(image==null||image.sprite==null)return;
            var rect=image.rectTransform;
            var centre=rect.TransformPoint(rect.rect.center);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,image.sprite.rect.width*pixelScale);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,image.sprite.rect.height*pixelScale);
            rect.position+=centre-rect.TransformPoint(rect.rect.center);
            image.preserveAspect=true;
        }
    }
}
