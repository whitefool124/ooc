using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    // Hard offset contact shadow for UI documents; no blurred raster or baked light.
    internal static class AcademyPaperDepth
    {
        internal static void Apply(Image paper, bool requireDesk = true)
        {
            if(paper==null || paper.color.a<.9f || paper.rectTransform.rect.width<480 ||
                paper.rectTransform.rect.height<140 || requireDesk && paper.GetComponentInParent<AcademyDeskBackdrop>()==null)return;
            var shadow=paper.GetComponent<Shadow>();
            if(shadow==null)shadow=paper.gameObject.AddComponent<Shadow>();
            shadow.effectColor=new Color(.08f,.055f,.035f,.32f);
            shadow.effectDistance=new Vector2(6,-6);shadow.useGraphicAlpha=true;
        }
    }
}
