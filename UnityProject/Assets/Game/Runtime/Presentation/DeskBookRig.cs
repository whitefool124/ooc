using System.Collections.Generic;
using UnityEngine;
namespace OCC.Combat.Presentation
{
    // Independent materials retain one texel per physical native pixel.
    public sealed class DeskBookRig : MonoBehaviour
    {
        public const float Width=50,Depth=33;
        private readonly List<Mesh> meshes=new List<Mesh>();
        private readonly List<Material> materials=new List<Material>();
        private readonly List<MeshRenderer> front=new List<MeshRenderer>(),back=new List<MeshRenderer>();
        private Transform hinge;
        private Material materialOverride;
        public float Angle {get;private set;}
        public Vector3 Spine=>hinge.position;
        public void Initialize(Sprite cover,SpriteRenderer source,Material litMaterial=null)
        {
            materialOverride=litMaterial;
            var cloth=MaterialFor(cover,source);
            var navy=new Rect(25.5f/64,13.5f/64,0,0);
            var label=MaterialFor(Load("label"),source);
            var right=MaterialFor(Load("right"),source);
            var left=MaterialFor(Load("left"),source);
            var labelUv=new Rect(9f/64,2f/64,46f/64,28f/64);
            var rightUv=new Rect(9f/64,2f/64,46f/64,29f/64);
            var leftUv=new Rect(18f/64,2f/64,28f/64,46f/64);
            var paper=new Rect(12.5f/64,4.5f/64,0,0);
            Face("固定底封",transform,0,Width,0,Depth,0,navy,cloth,source,0);
            Face("固定右页",transform,2,48,2,31,2,rightUv,right,source,2);
            Surface("右页块前沿",transform,new Vector3(2,0,2),new Vector3(48,0,2),new Vector3(48,2,2),new Vector3(2,2,2),paper,right,source,3);
            Surface("右页块外沿",transform,new Vector3(48,0,2),new Vector3(48,0,31),new Vector3(48,2,31),new Vector3(48,2,2),paper,right,source,3);
            Surface("固定书脊厚度",transform,new Vector3(0,0,0),new Vector3(0,0,Depth),new Vector3(0,3,Depth),new Vector3(0,3,0),navy,cloth,source,4);
            var pivot=new GameObject("共用书脊铰链"){layer=source.gameObject.layer};
            pivot.transform.SetParent(transform,false);hinge=pivot.transform;
            hinge.localPosition=Project(new Vector3(0,1.5f,0));
            front.Add(Face("封面外侧",hinge,0,Width,0,Depth,1.5f,navy,cloth,source,5));
            front.Add(Face("独立封面纸签",hinge,2,48,2,30,1.54f,labelUv,label,source,6));
            back.Add(Face("内封皮边",hinge,0,Width,0,Depth,1.5f,navy,cloth,source,5));
            back.Add(Face("独立左页",hinge,2,48,2,30,-.5f,leftUv,left,source,6,true));
            back.Add(Surface("左页块前沿",hinge,new Vector3(2,1.5f,2),new Vector3(48,1.5f,2),new Vector3(48,-.5f,2),new Vector3(2,-.5f,2),paper,right,source,7));
            back.Add(Surface("左页块外沿",hinge,new Vector3(48,1.5f,2),new Vector3(48,1.5f,30),new Vector3(48,-.5f,30),new Vector3(48,-.5f,2),paper,right,source,7));
            SetProgress(0);
        }
        private static Sprite Load(string name)
        {
            var sprite=Resources.Load<Sprite>("Art/MainMenuE/BookRigReview/"+name);
            if(sprite==null)throw new System.InvalidOperationException("Missing processed book material: "+name);
            return sprite;
        }
        private Material MaterialFor(Sprite sprite,SpriteRenderer source)
        {
            var material=new Material(materialOverride!=null?materialOverride:source.sharedMaterial){mainTexture=sprite.texture};materials.Add(material);return material;
        }
        private static Vector3 Project(Vector3 p)=>new Vector3(p.x,.5f*p.z+.8660254f*p.y,.8660254f*p.z-.5f*p.y)/32;
        private MeshRenderer Face(string name,Transform parent,float x0,float x1,float d0,float d1,float height,Rect uv,Material material,SpriteRenderer source,int order,bool rotate=false)
            =>Surface(name,parent,new Vector3(x0,height,d0),new Vector3(x1,height,d0),new Vector3(x1,height,d1),new Vector3(x0,height,d1),uv,material,source,order,rotate);
        private MeshRenderer Surface(string name,Transform parent,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Rect uv,Material material,SpriteRenderer source,int order,bool rotate=false)
        {
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer)){layer=source.gameObject.layer};go.transform.SetParent(parent,false);
            var mesh=new Mesh{name=name};mesh.vertices=new[]{Project(a),Project(b),Project(c),Project(d)};
            mesh.uv=rotate?new[]{new Vector2(uv.xMin,uv.yMax),new Vector2(uv.xMin,uv.yMin),new Vector2(uv.xMax,uv.yMin),new Vector2(uv.xMax,uv.yMax)}:new[]{new Vector2(uv.xMin,uv.yMin),new Vector2(uv.xMax,uv.yMin),new Vector2(uv.xMax,uv.yMax),new Vector2(uv.xMin,uv.yMax)};
            bool underside=name=="内封皮边"||name=="独立左页";
            mesh.triangles=underside?new[]{0,1,2,0,2,3}:new[]{0,2,1,0,3,2};mesh.colors=new[]{Color.white,Color.white,Color.white,Color.white};mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.sortingLayerID=source.sortingLayerID;renderer.sortingOrder=source.sortingOrder+order;return renderer;
        }
        public void SetProgress(float progress)
        {
            Angle=Mathf.Clamp01(progress)*180;hinge.localRotation=Quaternion.AngleAxis(Angle,new Vector3(0,.5f,.8660254f));
            foreach(var renderer in front)renderer.enabled=Angle<=90;
            foreach(var renderer in back)renderer.enabled=Angle>90;
        }
        private void OnDestroy(){foreach(var mesh in meshes)Destroy(mesh);foreach(var material in materials)Destroy(material);}
    }
}
