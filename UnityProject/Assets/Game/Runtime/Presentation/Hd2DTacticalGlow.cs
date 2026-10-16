using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    // Reuses the graphic's actual glyph/line/filled pixels; the centre stays sharp.
    public sealed class Hd2DTacticalGlow:BaseMeshEffect
    {
        public float Radius=1.5f;
        private readonly List<UIVertex> source=new List<UIVertex>(),output=new List<UIVertex>();
        public static void Attach(Graphic graphic,float radius=1.5f)
        {
            if(graphic==null||graphic.GetComponent<Hd2DTacticalGlow>()!=null)return;
            graphic.gameObject.AddComponent<Hd2DTacticalGlow>().Radius=radius;
        }
        public override void ModifyMesh(VertexHelper helper)
        {
            if(!IsActive())return;
            source.Clear();output.Clear();helper.GetUIVertexStream(source);
            foreach(var offset in new[]{new Vector2(-Radius,0),new Vector2(Radius,0),new Vector2(0,-Radius),new Vector2(0,Radius)})
                foreach(var original in source)
                {
                    var v=original;v.position+=new Vector3(offset.x,offset.y,0);
                    v.color.a=(byte)(v.color.a*.13f);output.Add(v);
                }
            output.AddRange(source);helper.Clear();helper.AddUIVertexTriangleStream(output);
        }
    }
}
