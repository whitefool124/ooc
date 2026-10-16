using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace OCC.Combat.Presentation
{
    // Runtime surface reuse only: no source image, importer or logical tile is changed.
    public sealed class Hd2DBattlefieldMasonry:MonoBehaviour
    {
        private const float Pixel=1f/32f;
        private readonly List<Texture2D> textures=new List<Texture2D>();
        private readonly List<Material> materials=new List<Material>();
        private readonly List<Vector3> vertices=new List<Vector3>(),normals=new List<Vector3>();
        private readonly List<Vector2> uv=new List<Vector2>(),visibility=new List<Vector2>();
        private readonly List<int>[] triangles={new List<int>(),new List<int>(),new List<int>()};
        private Mesh mesh;
        private MeshRenderer surface;
        private bool[] layout;
        private int width,height;
        private Hd2DAcademyEnvironment environment;

        internal void Initialize(int mapWidth,int mapHeight,int layer,Hd2DAcademyEnvironment surroundings)
        {
            width=mapWidth;height=mapHeight;environment=surroundings;layout=new bool[width*height];
            var shader=Shader.Find("OCC/HD2D/BakedArchitecture");
            if(shader==null)throw new InvalidOperationException("Missing masonry lighting shader");
            var stone=Required("Art/FormalAcademyIndependentFloors32/academy_block_court_b",32,32);
            // Keep the admitted cell frame on wall tops for tactical grid reading.
            // One complete native32 tile repeats per metre; the masonry sides stay joined.
            var cap=Copy(stone,0,0,32,32,"墙顶原生石材逐格边缘32x32");
            var capMaterial=MakeMaterial(shader,cap,"逐格石墙压顶");
            Hd2DBattlefieldMaterialMaps.ApplyRegion(capMaterial,stone,
                source=>Copy(source,0,0,32,32,"墙顶配对数据_"+source.name));
            foreach(string variant in new[]{"a","b"})
            {
                // NESW has continuous material edges instead of a card's outside outline.
                // Only image rows 39..59 are projected onto the real wall face at 32 PPU.
                var source=Required("Art/AcademyPermanentWallCandidates20260926/academy_permanent_wall_nesw_"+variant,32,64);
                var faceMaterial=MakeMaterial(shader,Copy(source,0,0,32,64,"墙身原生石砌_"+variant),"石砌墙身_"+variant);
                Hd2DMasonrySurfaceFields.For(gameObject).Apply(faceMaterial,true);
            }
            var go=new GameObject("邻接石砌墙体",typeof(MeshFilter),typeof(MeshRenderer)){layer=layer};
            go.transform.SetParent(transform,false);
            mesh=new Mesh{name="32PPU逐格墙顶与连续石砌侧面",indexFormat=IndexFormat.UInt32};
            go.GetComponent<MeshFilter>().sharedMesh=mesh;surface=go.GetComponent<MeshRenderer>();
            surface.sharedMaterials=materials.ToArray();surface.enabled=false;
        }
        private static Texture2D Required(string path,int w,int h)
        {
            var texture=Resources.Load<Texture2D>(path);
            if(texture==null||texture.width!=w||texture.height!=h)
                throw new InvalidOperationException("Missing native wall surface: "+path);
            return texture;
        }
        private Texture2D Copy(Texture2D source,int x,int y,int w,int h,string name)
        {
            var texture=new Texture2D(w,h,source.format,false,
                !UnityEngine.Experimental.Rendering.GraphicsFormatUtility.IsSRGBFormat(source.graphicsFormat))
                {name=name,filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Repeat};
            Graphics.CopyTexture(source,0,0,x,y,w,h,texture,0,0,0,0);textures.Add(texture);return texture;
        }
        private Material MakeMaterial(Shader shader,Texture2D texture,string name)
        {
            var material=new Material(shader){name=name,mainTexture=texture,color=Color.white};
            material.SetFloat("_Smoothness",.02f);material.SetFloat("_ClearCoatSmoothness",.88f);materials.Add(material);return material;
        }
        internal void SetRainWetness(float amount)
        {
            for(int i=0;i<materials.Count;i++)
            {
                var material=materials[i];material.SetFloat("_ClearCoatMask",Mathf.Clamp01(amount)*(i==0?.32f:.18f));
                if(amount>.001f)material.EnableKeyword("_OCC_WEATHER_WET");else material.DisableKeyword("_OCC_WEATHER_WET");
            }
        }
        internal void Refresh(CombatState state)
        {
            bool changed=false;
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)
            {
                int i=y*width+x;bool wall=state.Map.GetTile(new GridPosition(x,y)).IsPermanentWall;
                if(layout[i]!=wall){layout[i]=wall;changed=true;}
            }
            if(!changed)return;
            vertices.Clear();normals.Clear();uv.Clear();visibility.Clear();foreach(var faces in triangles)faces.Clear();
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)if(Wall(x,y))Cell(x,y);
            mesh.Clear();mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.SetUVs(1,visibility);mesh.subMeshCount=3;
            for(int i=0;i<3;i++)mesh.SetTriangles(triangles[i],i);
            mesh.RecalculateTangents();mesh.RecalculateBounds();surface.enabled=vertices.Count>0;
        }
        private bool Wall(int x,int y)=>x>=0&&y>=0&&x<width&&y<height&&layout[y*width+x];
        private void Cell(int x,int z)
        {
            float top=Hd2DViewProfile.BattlefieldWallHeight,shoulder=top-Pixel;
            bool n=!Wall(x,z-1),e=!Wall(x+1,z),s=!Wall(x,z+1),w=!Wall(x-1,z);
            var a=new Vector3(x+(w?Pixel:0),top,z+(n?Pixel:0));
            var b=new Vector3(x+1-(e?Pixel:0),top,z+(n?Pixel:0));
            var c=new Vector3(x+1-(e?Pixel:0),top,z+1-(s?Pixel:0));
            var d=new Vector3(x+(w?Pixel:0),top,z+1-(s?Pixel:0));
            Quad(a,b,c,d,Vector3.up,0,true);
            int variant=1+((x+z)&1);
            if(n)Edge(new Vector3(x,0,z),new Vector3(x+1,0,z),a,b,Vector3.back,shoulder,variant);
            if(e)Edge(new Vector3(x+1,0,z),new Vector3(x+1,0,z+1),b,c,Vector3.right,shoulder,variant);
            if(s)Edge(new Vector3(x+1,0,z+1),new Vector3(x,0,z+1),c,d,Vector3.forward,shoulder,variant);
            if(w)Edge(new Vector3(x,0,z+1),new Vector3(x,0,z),d,a,Vector3.left,shoulder,variant);
        }
        private void Edge(Vector3 a,Vector3 b,Vector3 capA,Vector3 capB,Vector3 normal,float shoulder,int variant)
        {
            Vector3 upperA=a+Vector3.up*shoulder,upperB=b+Vector3.up*shoulder;
            Quad(a,b,upperB,upperA,normal,variant,false);
            Quad(upperA,upperB,capB,capA,(normal+Vector3.up).normalized,variant,false,true);
        }
        private void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,Vector3 normal,int material,bool cap,bool bevel=false)
        {
            int start=vertices.Count;
            foreach(var p in new[]{a,b,c,d})
            {
                vertices.Add(p);normals.Add(normal);
                // Unfold the 45-degree shoulder by its true surface length, too.
                float v=bevel?a.y+(p.y-a.y)*Mathf.Sqrt(2):p.y;
                uv.Add(cap?new Vector2(p.x,p.z):
                    new Vector2(Mathf.Abs(normal.x)>.5f?p.z:p.x,(4+v*32)/64f));
                visibility.Add(new Vector2(environment.ArchitecturalVisibility(p,normal),0));
            }
            bool forward=Vector3.Dot(Vector3.Cross(b-a,c-a),normal)>0;
            triangles[material].AddRange(forward?new[]{start,start+1,start+2,start,start+2,start+3}:
                new[]{start,start+2,start+1,start,start+3,start+2});
        }
        private void OnDestroy()
        {
            if(mesh!=null)Destroy(mesh);foreach(var material in materials)Destroy(material);foreach(var texture in textures)Destroy(texture);
        }
    }
}
