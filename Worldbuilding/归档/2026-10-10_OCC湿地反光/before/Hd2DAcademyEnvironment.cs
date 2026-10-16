using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace OCC.Combat.Presentation
{
    // Noninteractive courtyard layers. No collider or logical tile is created here.
    public sealed class Hd2DAcademyEnvironment:MonoBehaviour
    {
        private readonly List<Material> materials=new List<Material>();
        private readonly List<Mesh> meshes=new List<Mesh>();
        private int layer;
        private Transform scenery;
        private Material stone,trim,earth,roof,water;
        private Texture2D masonryTexture;
        private readonly List<MeshFilter> architecture=new List<MeshFilter>();
        private readonly List<Bounds> occluders=new List<Bounds>();
        public int CombinedArchitectureCount{get;private set;}
        public double BakeMilliseconds{get;private set;}
        private Material Material(string name,Color color)
        {
            var m=new Material(Shader.Find("Universal Render Pipeline/Lit")){name=name,color=color};
            m.SetFloat("_Smoothness",0);materials.Add(m);return m;
        }
        private GameObject Box(string name,Vector3 p,Vector3 size,Material material)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.layer=layer;go.transform.SetParent(scenery,false);
            go.transform.localPosition=p;go.transform.localScale=size;go.GetComponent<MeshRenderer>().sharedMaterial=material;
            architecture.Add(go.GetComponent<MeshFilter>());occluders.Add(new Bounds(p,size));
            Destroy(go.GetComponent<Collider>());return go;
        }
        public void Build(int width,int height,int isolatedLayer)
        {
            layer=isolatedLayer;scenery=new GameObject("学院庭院环境组合").transform;scenery.SetParent(transform,false);
            stone=Material("庭院暖灰石",new Color(.47f,.45f,.38f));trim=Material("学院象牙石",new Color(.68f,.65f,.53f));
            earth=Material("花圃泥土",new Color(.23f,.24f,.16f));roof=Material("远景青灰屋面",new Color(.17f,.25f,.24f));
            water=Material("远景浅水",new Color(.19f,.34f,.36f));water.SetFloat("_Smoothness",.72f);
            // The original grid stands on this continuous courtyard, rather than a floating slab.
            Box("连续庭院基座",new Vector3(width*.5f,-.36f,height*.5f),new Vector3(width+24,.44f,height+30),stone);
            var host=Object.FindAnyObjectByType<CombatPrototypeBootstrap>();
            var ground=host!=null&&host.CurrentState!=null?host.PresentBattlefieldCell(new GridPosition(1,1)):null;
            // Reuse the admitted native stone tile for exposed masonry. The runtime copy
            // owns repeat sampling; the source texture and its importer remain unchanged.
            var masonrySource=Resources.Load<Texture2D>("Art/FormalAcademyIndependentFloors32/academy_block_court_b");
            if(masonrySource!=null && masonrySource.width==32 && masonrySource.height==32)
            {
                masonryTexture=new Texture2D(masonrySource.width,masonrySource.height,masonrySource.format,masonrySource.mipmapCount>1,
                    !UnityEngine.Experimental.Rendering.GraphicsFormatUtility.IsSRGBFormat(masonrySource.graphicsFormat));
                Graphics.CopyTexture(masonrySource,masonryTexture);
                masonryTexture.name="庭院石材32PPU运行时采样";
                masonryTexture.wrapMode=TextureWrapMode.Repeat;
                masonryTexture.filterMode=FilterMode.Point;
                stone.mainTexture=masonryTexture;stone.color=new Color(.82f,.84f,.86f);
                trim.mainTexture=masonryTexture;trim.color=new Color(1f,.98f,.92f);
            }
            if(ground?.FloorTexture!=null)Paving(ground.FloorTexture,ground.FloorUv,width,height);
            float rear=height+.8f;
            Box("后庭台地",new Vector3(width*.5f,.08f,rear+1.8f),new Vector3(width+17,.45f,3.5f),stone);
            Box("后庭压边",new Vector3(width*.5f,.36f,rear),new Vector3(width+17,.16f,.3f),trim);
            for(int i=0;i<4;i++)Box("入院台阶_"+i,new Vector3(width*.5f,-.05f+i*.09f,rear-.9f+i*.3f),new Vector3(3,.16f,.55f),trim);
            Box("左侧花圃",new Vector3(-3.4f,-.04f,height*.5f+.8f),new Vector3(2.5f,.28f,height+3),trim);
            Box("左侧种植土",new Vector3(-3.4f,.115f,height*.5f+.8f),new Vector3(2.2f,.04f,height+2.7f),earth);
            Box("右侧浅水庭",new Vector3(width+3.8f,-.13f,height*.5f+1),new Vector3(3.8f,.13f,height+5),trim);
            Box("浅水面",new Vector3(width+3.8f,-.053f,height*.5f+1),new Vector3(3.45f,.025f,height+4.65f),water);
            for(int i=0;i<3;i++)Box("水庭踏石_"+i,new Vector3(width+3.8f,.06f,height*.5f-.8f+i*.95f),new Vector3(1.5f,.19f,.64f),trim);
            for(int i=0;i<13;i++)
            {
                float x=-6+i*1.5f;
                if(Mathf.Abs(x-width*.5f)<2)continue;
                Box("后庭栏柱_"+i,new Vector3(x,.85f,rear+.5f),new Vector3(.2f,1.1f,.2f),trim);
                Box("栏柱柱帽_"+i,new Vector3(x,1.43f,rear+.5f),new Vector3(.33f,.12f,.33f),trim);
            }
            Box("后庭栏杆左",new Vector3(-2,.91f,rear+.5f),new Vector3(7.5f,.14f,.14f),trim);
            Box("后庭栏杆右",new Vector3(width+2,.91f,rear+.5f),new Vector3(7.5f,.14f,.14f),trim);
            var facade=Resources.Load<Texture2D>("Art/HD2DAcademyReview/academy_facade");
            if(facade!=null)
            {
                float facadeWidth=facade.width/32f,facadeHeight=facade.height/32f;
                // Genuine depth behind the flat front elevation; native texels stay at 32 PPU.
                Box("学院楼体实体",new Vector3(width*.5f,facadeHeight*.38f+.3f,rear+2.6f),new Vector3(facadeWidth,facadeHeight*.75f,2.6f),stone);
                Roof(width*.5f,rear+2.6f,facadeWidth+.3f,2.55f,facadeHeight*.64f+.31f,facadeHeight*.92f+.31f);
                SpritePlane("学院独立立面",facade,new Vector3(width*.5f,.31f,rear+1.26f));
                // Side wings use the same independent raw module with preserved source pixel density.
                SpritePlane("左翼远景",facade,new Vector3(width*.5f-facadeWidth,.2f,rear+4.3f));
                SpritePlane("右翼远景",facade,new Vector3(width*.5f+facadeWidth,.2f,rear+4.3f));
            }
            var tree=Resources.Load<Texture2D>("Art/HD2DAcademyReview/academy_tree");
            if(tree!=null)
            {
                foreach(var p in new[]{new Vector3(-3.4f,.13f,.4f),new Vector3(-3.4f,.13f,height+1.3f),new Vector3(width+5.8f,0,height*.5f),new Vector3(-6.5f,.31f,rear+4),new Vector3(width+7,.31f,rear+4)})
                    SpritePlane("独立庭院树",tree,p,true);
            }
            if(host?.CurrentState!=null)
                for(int y=0;y<height;y++)for(int x=0;x<width;x++)if(host.CurrentState.Map.GetTile(new GridPosition(x,y)).IsPermanentWall)
                    occluders.Add(new Bounds(new Vector3(x+.5f,.325f,y+.5f),new Vector3(1,.65f,1)));
            var timer=System.Diagnostics.Stopwatch.StartNew();BakeAndCombineArchitecture();BakePavingVisibility();timer.Stop();BakeMilliseconds=timer.Elapsed.TotalMilliseconds;
        }
        public float GroundVisibility(Vector3 p)=>Visibility(p,Vector3.up);
        private void BakePavingVisibility()
        {
            foreach(var filter in scenery.GetComponentsInChildren<MeshFilter>())
            {
                if(filter.sharedMesh.name!="外围连续铺地")continue;
                var mesh=filter.sharedMesh;var vertices=mesh.vertices;var mask=new Vector2[vertices.Length];
                for(int i=0;i<vertices.Length;i++)mask[i]=new Vector2(GroundVisibility(vertices[i]),0);
                mesh.uv2=mask;filter.GetComponent<MeshRenderer>().sharedMaterial.shader=Shader.Find("OCC/HD2D/BakedArchitecture");
            }
        }
        private float Visibility(Vector3 position,Vector3 normal)
        {
            var axis=Mathf.Abs(normal.y)>.8f?Vector3.right:Vector3.up;
            var u=Vector3.Cross(normal,axis).normalized;var v=Vector3.Cross(normal,u);
            float covered=0;
            for(int rayIndex=0;rayIndex<8;rayIndex++)
            {
                float angle=rayIndex*2.399963f;float radius=Mathf.Sqrt((rayIndex+.5f)/8f);
                var direction=(u*Mathf.Cos(angle)*radius+v*Mathf.Sin(angle)*radius+normal*Mathf.Sqrt(1-radius*radius)).normalized;
                var ray=new Ray(position+normal*.025f,direction);float nearest=1.4f;
                foreach(var box in occluders)if(box.IntersectRay(ray,out float distance)&&distance>=0&&distance<nearest)nearest=distance;
                covered+=1-nearest/1.4f;
            }
            return 1-covered/8;
        }
        private Mesh BakedBox(MeshFilter source)
        {
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var texcoords=new List<Vector2>();var triangles=new List<int>();
            var size=source.transform.localScale;var center=source.transform.localPosition;
            foreach(var n in new[]{Vector3.up,Vector3.down,Vector3.right,Vector3.left,Vector3.forward,Vector3.back})
            {
                var u=Mathf.Abs(n.y)>.8f?Vector3.right:Vector3.up;var v=Vector3.Cross(n,u);
                float a=Vector3.Scale(u,size).magnitude,b=Vector3.Scale(v,size).magnitude,c=Vector3.Scale(n,size).magnitude;
                int nx=Mathf.Clamp(Mathf.CeilToInt(a/.7f),1,32),ny=Mathf.Clamp(Mathf.CeilToInt(b/.7f),1,32),start=vertices.Count;
                for(int y=0;y<=ny;y++)for(int x=0;x<=nx;x++)
                {
                    var p=center+n*c*.5f+u*((x/(float)nx-.5f)*a)+v*((y/(float)ny-.5f)*b);
                    vertices.Add(p);normals.Add(n);uv.Add(new Vector2(Visibility(p,n),0));
                    // One 32-texel tile per world unit on every plane; never stretch
                    // a single tile across the full platform or a tall pillar.
                    texcoords.Add(new Vector2(Vector3.Dot(p,u),Vector3.Dot(p,v)));
                }
                for(int y=0;y<ny;y++)for(int x=0;x<nx;x++){int i=start+y*(nx+1)+x;triangles.AddRange(new[]{i,i+1,i+nx+2,i,i+nx+2,i+nx+1});}
            }
            var mesh=new Mesh{name="静态建筑遮蔽烘焙",vertices=vertices.ToArray(),normals=normals.ToArray(),uv=texcoords.ToArray(),uv2=uv.ToArray(),triangles=triangles.ToArray()};mesh.RecalculateBounds();meshes.Add(mesh);return mesh;
        }
        private void BakeAndCombineArchitecture()
        {
            var shader=Shader.Find("OCC/HD2D/BakedArchitecture");
            if(shader==null)throw new System.InvalidOperationException("Missing architecture visibility shader");
            var groups=new Dictionary<Material,List<CombineInstance>>();
            foreach(var source in architecture)
            {
                var renderer=source.GetComponent<MeshRenderer>();var material=renderer.sharedMaterial;
                if(!groups.TryGetValue(material,out var parts)){parts=new List<CombineInstance>();groups.Add(material,parts);}
                parts.Add(new CombineInstance{mesh=BakedBox(source),transform=Matrix4x4.identity});renderer.enabled=false;
            }
            foreach(var group in groups)
            {
                var mesh=new Mesh{name="合批_"+group.Key.name,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.CombineMeshes(group.Value.ToArray(),true,false);meshes.Add(mesh);
                var material=new Material(group.Key){shader=shader};materials.Add(material);
                var go=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer)){layer=layer};go.transform.SetParent(scenery,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;CombinedArchitectureCount++;
                // The combined mesh owns the copied data; release the temporary subdivision meshes.
                foreach(var part in group.Value){meshes.Remove(part.mesh);Destroy(part.mesh);}
            }
        }
        private void Roof(float x,float z,float width,float depth,float eaves,float ridge)
        {
            var mesh=new Mesh{name="学院真实双坡屋顶",vertices=new[]{new Vector3(-width*.5f,eaves,-depth*.5f),new Vector3(width*.5f,eaves,-depth*.5f),new Vector3(width*.5f,ridge,0),new Vector3(-width*.5f,ridge,0),new Vector3(-width*.5f,eaves,depth*.5f),new Vector3(width*.5f,eaves,depth*.5f)},triangles=new[]{0,3,2,0,2,1,3,4,5,3,5,2}};
            mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
            var go=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer)){layer=layer};go.transform.SetParent(scenery,false);go.transform.localPosition=new Vector3(x,0,z);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=roof;
            Box("屋脊压条",new Vector3(x,ridge+.015f,z),new Vector3(width+.08f,.06f,.08f),roof);
        }
        private void Paving(Texture2D texture,Rect textureUv,int width,int height)
        {
            var m=Material("外围原生石路",new Color(.83f,.85f,.82f));m.mainTexture=texture;
            var verts=new List<Vector3>();var uv=new List<Vector2>();var tris=new List<int>();
            for(int y=-9;y<height+18;y++)for(int x=-14;x<width+14;x++)
            {
                if(x>=0&&x<width&&y>=0&&y<height)continue;
                int n=verts.Count;verts.AddRange(new[]{new Vector3(x,-.135f,y),new Vector3(x+1,-.135f,y),new Vector3(x+1,-.135f,y+1),new Vector3(x,-.135f,y+1)});
                uv.AddRange(new[]{new Vector2(textureUv.xMin,textureUv.yMin),new Vector2(textureUv.xMax,textureUv.yMin),new Vector2(textureUv.xMax,textureUv.yMax),new Vector2(textureUv.xMin,textureUv.yMax)});tris.AddRange(new[]{n,n+2,n+1,n,n+3,n+2});
            }
            var mesh=new Mesh{name="外围连续铺地",vertices=verts.ToArray(),uv=uv.ToArray(),triangles=tris.ToArray()};mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
            var go=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer)){layer=layer};go.transform.SetParent(scenery,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=m;
        }
        private void SpritePlane(string name,Texture2D texture,Vector3 p,bool screenFacing=false)
        {
            var m=Material(name+"原生像素材质",Color.white);m.mainTexture=texture;m.SetFloat("_AlphaClip",1);m.SetFloat("_Cutoff",.5f);m.SetFloat("_Cull",0);m.EnableKeyword("_ALPHATEST_ON");
            float w=texture.width/32f,h=texture.height/32f;
            var mesh=new Mesh{name=name,vertices=new[]{new Vector3(-w*.5f,0,0),new Vector3(w*.5f,0,0),new Vector3(w*.5f,h,0),new Vector3(-w*.5f,h,0)},uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up},triangles=new[]{0,2,1,0,3,2}};
            mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
            var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer)){layer=layer};go.transform.SetParent(scenery,false);go.transform.localPosition=p;
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=m;
            if(screenFacing)
            {
                go.transform.localRotation=Hd2DViewProfile.BattlefieldUnitRotation;
                m.SetFloat("_ReceiveShadows",0);m.EnableKeyword("_RECEIVE_SHADOWS_OFF");
                var renderer=go.GetComponent<MeshRenderer>();renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
                var shader=Shader.Find("OCC/HD2D/GroundedSprite");
                if(shader==null)throw new System.InvalidOperationException("Missing grounded sprite shader");
                var visible=new Material(m){shader=shader};visible.EnableKeyword("_ALPHATEST_ON");visible.EnableKeyword("_RECEIVE_SHADOWS_OFF");materials.Add(visible);renderer.sharedMaterial=visible;
                var caster=new GameObject(name+"_直立投影",typeof(MeshFilter),typeof(MeshRenderer)){layer=layer};caster.transform.SetParent(scenery,false);caster.transform.localPosition=p;
                caster.GetComponent<MeshFilter>().sharedMesh=mesh;var shadow=caster.GetComponent<MeshRenderer>();shadow.sharedMaterial=m;shadow.shadowCastingMode=ShadowCastingMode.ShadowsOnly;shadow.receiveShadows=false;
            }
        }
        private void OnDestroy(){foreach(var m in materials)Destroy(m);foreach(var mesh in meshes)Destroy(mesh);if(masonryTexture!=null)Destroy(masonryTexture);}
    }
}


