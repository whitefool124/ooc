using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    // A culled Graphic may never have supplied CanvasRenderer.GetMesh(). Build
    // the same uGUI geometry explicitly so its first visible frame is already lit.
    // Disabled builder: no drawing, input, or source material changes.
    public sealed class Hd2DImageMeshBuilder : Image
    {
        internal Mesh Build(Image source)
        {
            enabled=false;raycastTarget=false;
            rectTransform.pivot=source.rectTransform.pivot;
            rectTransform.sizeDelta=source.rectTransform.rect.size;
            sprite=source.overrideSprite;type=source.type;preserveAspect=source.preserveAspect;
            pixelsPerUnitMultiplier=source.pixelsPerUnitMultiplier;
            fillCenter=source.fillCenter;fillMethod=source.fillMethod;
            fillAmount=source.fillAmount;fillOrigin=source.fillOrigin;fillClockwise=source.fillClockwise;
            useSpriteMesh=source.useSpriteMesh;color=Color.white;
            var mesh=new Mesh{name=source.name+" uncullable native geometry"};
            using(var vertices=new VertexHelper()){OnPopulateMesh(vertices);vertices.FillMesh(mesh);}
            return mesh;
        }
    }
}
