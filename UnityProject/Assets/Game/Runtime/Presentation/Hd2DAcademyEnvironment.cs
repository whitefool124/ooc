using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace OCC.Combat.Presentation
{
    // Noninteractive courtyard layers. No collider or logical tile is created here.
    public sealed class Hd2DAcademyEnvironment:MonoBehaviour
    {
#if UNITY_EDITOR
        // Decoded archive candidate supplied only by the Editor review runner.
        // This is not a formal asset replacement and is absent from Player builds.
        public static Texture2D GreenhouseReviewFacade { get; set; }
#endif
        private readonly List<Material> materials=new List<Material>();
        private readonly List<Mesh> meshes=new List<Mesh>();
        private int layer;
        private Transform scenery;
        private Material stone,trim,earth,roof,water,stoneFace,trimFace;
        private Texture2D masonryTexture;
        private Texture2D masonrySourceTexture;
        private Texture2D earthTexture;
        private Texture2D wallFaceTexture;
        private Texture2D pavingTexture;
        private readonly List<Texture2D> pavingRegions=new List<Texture2D>();
        private readonly List<Material> gardenMaterials=new List<Material>();
        public int GardenSurfaceVariants=>gardenMaterials.Count;
        private readonly HashSet<Texture> wetPavingRegions=new HashSet<Texture>();
        private readonly List<MeshFilter> architecture=new List<MeshFilter>();
        private readonly List<Bounds> occluders=new List<Bounds>();
        private readonly List<(Rect bounds,float eaves,float ridge)> rainRoofs=new List<(Rect,float,float)>();
        private Rect rainWater;
        internal Hd2DRainSurface RainSurface(Vector2 point)
        {
            float top=-.135f;bool wet=false;
            if(rainWater.width>0&&rainWater.Contains(point)){top=-.0405f;wet=true;}
            foreach(var box in occluders)
                if(point.x>=box.min.x&&point.x<=box.max.x&&point.y>=box.min.z&&point.y<=box.max.z&&box.max.y>top)
                {top=box.max.y;wet=false;}
            foreach(var roof in rainRoofs)
                if(roof.bounds.Contains(point))
                {
                    float y=Mathf.Lerp(roof.eaves,roof.ridge,1-Mathf.Abs(point.y-roof.bounds.center.y)/(roof.bounds.height*.5f));
                    if(y>top){top=y;wet=false;}
                }
            return new Hd2DRainSurface(top,wet);
        }
        public int CombinedArchitectureCount{get;private set;}
        public double BakeMilliseconds{get;private set;}
        internal void SetRainWetness(float amount)
        {
            foreach(var material in materials)
            {
                if(material.shader.name!="OCC/HD2D/BakedArchitecture"||
                    (material.mainTexture!=masonryTexture&&!wetPavingRegions.Contains(material.mainTexture)&&material.mainTexture!=wallFaceTexture))continue;
                material.SetFloat("_ClearCoatMask",Mathf.Clamp01(amount)*(material.mainTexture==wallFaceTexture?.18f:.42f));
                if(amount>.001f)material.EnableKeyword("_OCC_WEATHER_WET");else material.DisableKeyword("_OCC_WEATHER_WET");
            }
        }
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
            // The original grid stands on this continuous courtyard, rather than a floating slab.
            Box("连续庭院基座",new Vector3(width*.5f,-.36f,height*.5f),new Vector3(width+24,.44f,height+30),stone);
            var host=Object.FindAnyObjectByType<CombatPrototypeBootstrap>();
            var profile=Hd2DBattlefieldProfile.Resolve(host?.CurrentLevelId);
            scenery.name="学院环境组合_"+profile.Environment;
            var ground=host!=null&&host.CurrentState!=null?host.PresentBattlefieldCell(new GridPosition(1,1)):null;
            // Reuse the admitted native stone tile for exposed masonry. The runtime copy
            // owns repeat sampling; the source texture and its importer remain unchanged.
            var masonrySource=Resources.Load<Texture2D>(profile.MasonryResource);
            masonrySourceTexture=masonrySource;
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
                roof.mainTexture=masonryTexture;
            }
            else throw new System.InvalidOperationException("Missing native32 environment masonry: "+profile.MasonryResource);
            var wallSource=Resources.Load<Texture2D>("Art/AcademyPermanentWallCandidates20260926/academy_permanent_wall_nesw_a");
            if(wallSource==null||wallSource.width!=32||wallSource.height!=64)
                throw new System.InvalidOperationException("Missing native Academy masonry face");
            // Reuse only the authored masonry body, excluding the card's top/foot/alpha.
            // Native rows 38..59 (top origin) form a 32x22 repeatable surface, with no resize.
            wallFaceTexture=new Texture2D(32,22,wallSource.format,false,
                !UnityEngine.Experimental.Rendering.GraphicsFormatUtility.IsSRGBFormat(wallSource.graphicsFormat))
                {name="建筑石砌原生侧面32x22",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Repeat};
            Graphics.CopyTexture(wallSource,0,0,0,4,32,22,wallFaceTexture,0,0,0,0);
            stoneFace=Material("建筑暖灰石砌立面",stone.color);stoneFace.mainTexture=wallFaceTexture;
            trimFace=Material("建筑浅色石砌立面",trim.color);trimFace.mainTexture=wallFaceTexture;
            stoneFace.shader=Shader.Find("OCC/HD2D/BakedArchitecture");trimFace.shader=stoneFace.shader;
            var masonryFields=Hd2DMasonrySurfaceFields.For(gameObject);masonryFields.Apply(stoneFace,false);masonryFields.Apply(trimFace,false);
            stoneFace.SetFloat("_ClearCoatSmoothness",.88f);trimFace.SetFloat("_ClearCoatSmoothness",.88f);
            roof.shader=Shader.Find("OCC/HD2D/BakedArchitecture");
            Hd2DBattlefieldMaterialMaps.Apply(roof,masonrySource);
            var earthSource=Resources.Load<Texture2D>("Art/FormalAcademyIndependentFloors32/academy_block_earth_b");
            if(earthSource==null || earthSource.width!=32 || earthSource.height!=32)
                throw new System.InvalidOperationException("Missing native32 soil texture");
            earthTexture=new Texture2D(32,32,earthSource.format,false,
                !UnityEngine.Experimental.Rendering.GraphicsFormatUtility.IsSRGBFormat(earthSource.graphicsFormat));
            Graphics.CopyTexture(earthSource,earthTexture);earthTexture.filterMode=FilterMode.Point;earthTexture.wrapMode=TextureWrapMode.Repeat;
            earth.mainTexture=earthTexture;earth.color=Color.white;
            // Surrounding architecture selects a material role, not whichever
            // logical tile happened to be at (1,1), which may be a planting bed.
            if(ground?.FloorTexture!=null)Paving(masonrySource,new Rect(0,0,1,1),width,height,profile);
            float rear=height+.8f;
            bool gardenEntrance=profile.Environment==Hd2DEnvironmentKind.AcademyCourt||profile.Environment==Hd2DEnvironmentKind.StoneRoad;
            float entranceX=width*.5f+(gardenEntrance?.75f:0),terraceWidth=width+(gardenEntrance?6:17);
            Box("后庭台地",new Vector3(entranceX,.08f,rear+1.8f),new Vector3(terraceWidth,.45f,3.5f),stone);
            if(gardenEntrance)
            {
                // Two wings leave an actual three-metre entrance instead of a bar across the stairs.
                float wing=(terraceWidth-3)*.5f;
                foreach(float side in new[]{-1f,1f})
                    Box("后庭入口压边_"+side,new Vector3(entranceX+side*(1.5f+wing*.5f),.36f,rear),new Vector3(wing,.16f,.3f),trim);
            }
            else Box("后庭压边",new Vector3(entranceX,.36f,rear),new Vector3(terraceWidth,.16f,.3f),trim);
            for(int i=0;i<4;i++)Box("入院台阶_"+i,new Vector3(entranceX,-.05f+i*.09f,
                (gardenEntrance?height+.4f:rear-.9f)+i*.3f),new Vector3(3,.16f,.55f),trim);
            if(profile.Trees)
            {
            Box("左侧花圃",new Vector3(-3.4f,-.04f,height*.5f+.8f),new Vector3(2.5f,.28f,height+3),trim);
            Box("左侧种植土",new Vector3(-3.4f,.115f,height*.5f+.8f),new Vector3(2.2f,.04f,height+2.7f),earth);
            SoilBedSurface(new Rect(-4.5f,-.55f,2.2f,height+2.7f),.135f);
            }
            if(profile.WaterGarden)
            {
            Box("右侧浅水庭",new Vector3(width+3.8f,-.13f,height*.5f+1),new Vector3(3.8f,.13f,height+5),trim);
            BuildWaterSurface(width,height,masonrySource);
            for(int i=0;i<3;i++)Box("水庭踏石_"+i,new Vector3(width+3.8f,.06f,height*.5f-.8f+i*.95f),new Vector3(1.5f,.19f,.64f),trim);
            }
            if(gardenEntrance)
            {
                var cloister=Resources.Load<Texture2D>("Art/FormalAcademyStructures32/academy_cloister_wall_4x1");
                if(cloister==null||cloister.width!=128||cloister.height!=32)
                    throw new System.InvalidOperationException("Missing native Academy cloister boundary");
                foreach(float side in new[]{-1f,1f})
                {
                    float x=entranceX+side*3.5f,z=rear+.18f;
                    // Source alpha bounds are (2,10)..(126,30); its two transparent
                    // bottom pixels are excluded from the physical contact height.
                    SpritePlane("后庭像素低墙_"+side,cloister,new Vector3(x,.305f-2f/32f,z));
                    Box("后庭低墙厚度_"+side,new Vector3(x,.305f+19f/64f,z+.14f),new Vector3(124f/32f,19f/32f,.24f),stone);
                }
            }
            var facade=Resources.Load<Texture2D>("Art/HD2DAcademyReview/academy_facade");
            if(profile.Environment==Hd2DEnvironmentKind.Conservatory)
            {
                facade=Resources.Load<Texture2D>("Art/HD2DAcademyCandidates/greenhouse_facade");
#if UNITY_EDITOR
                if(GreenhouseReviewFacade!=null)facade=GreenhouseReviewFacade;
#endif
                if(facade==null)throw new System.InvalidOperationException("Missing decoded greenhouse candidate");
                SpritePlane("温室独立立面_候选",facade,new Vector3(width*.5f,.31f-1f/32f,rear+.2f));
            }
            else if(profile.Environment==Hd2DEnvironmentKind.RuinSite)
            {
                Box("遗址矮墙",new Vector3(width*.5f,.72f,rear+1.4f),new Vector3(width+6,.8f,.65f),stone);
                foreach(float x in new[]{-2f,width+2f})Box("遗址承重墩",new Vector3(x,1.4f,rear+1.4f),new Vector3(.8f,2.2f,.8f),trim);
            }
            else if(profile.Environment==Hd2DEnvironmentKind.InteriorHall || profile.Environment==Hd2DEnvironmentKind.AetherTower)
            {
                // Background only: no roof over gameplay or walls that alter sight lines.
                Box("厅堂后墙",new Vector3(width*.5f,1.7f,rear+1.4f),new Vector3(width+8,2.8f,.55f),stone);
                foreach(float x in new[]{-3f,-1.5f,width+1.5f,width+3f})
                    Box("厅堂承重柱",new Vector3(x,1.9f,rear+1f),new Vector3(.55f,3.2f,.55f),trim);
                if(facade!=null)SpritePlane("学院厅堂窗墙模块",facade,new Vector3(width*.5f,.31f,rear+.65f));
            }
            else if(facade!=null)
            {
                float facadeWidth=facade.width/32f,facadeHeight=facade.height/32f;
                // Genuine depth behind the flat front elevation; native texels stay at 32 PPU.
                float front=rear+(gardenEntrance?.4f:1.26f),bodyDepth=front+1.32f;
                Box("学院楼体实体",new Vector3(entranceX,facadeHeight*.38f+.3f,bodyDepth),new Vector3(facadeWidth,facadeHeight*.75f,2.6f),stone);
                Roof(entranceX,bodyDepth,facadeWidth+.3f,2.55f,facadeHeight*.64f+.31f,facadeHeight*.92f+.31f);
                SpritePlane("学院独立立面",facade,new Vector3(entranceX,.31f,front));
                // Side wings use the same independent raw module with preserved source pixel density.
                SpritePlane("左翼远景",facade,new Vector3(entranceX-facadeWidth-.7f,-.12f,rear+(gardenEntrance?4.5f:5.8f)));
                SpritePlane("右翼远景",facade,new Vector3(entranceX+facadeWidth+.5f,.31f,rear+(gardenEntrance?5.7f:3.2f)));
            }
            var tree=Resources.Load<Texture2D>("Art/HD2DAcademyReview/academy_tree");
            if(tree!=null && profile.Trees)
            {
                foreach(var p in new[]{new Vector3(-3.4f,.13f,.4f),new Vector3(-3.4f,.13f,height+1.3f),new Vector3(width+5.8f,0,height*.5f),new Vector3(-6.5f,.31f,rear+4),new Vector3(width+7,.31f,rear+4),
                    new Vector3(-4.4f,-.12f,-2.5f),new Vector3(width+(gardenEntrance?2.2f:4.8f),-.12f,gardenEntrance?-3.2f:-3.5f)})
                    SpritePlane("独立庭院树",tree,p,true);
            }
            FrameOutskirts(profile,width,height);
            FurnishOutskirts(profile,width,height);
            if(host?.CurrentState!=null)
                for(int y=0;y<height;y++)for(int x=0;x<width;x++)if(host.CurrentState.Map.GetTile(new GridPosition(x,y)).IsPermanentWall)
                    occluders.Add(new Bounds(new Vector3(x+.5f,.325f,y+.5f),new Vector3(1,.65f,1)));
            var timer=System.Diagnostics.Stopwatch.StartNew();BakeAndCombineArchitecture();BakePavingVisibility();timer.Stop();BakeMilliseconds=timer.Elapsed.TotalMilliseconds;
        }
        public float GroundVisibility(Vector3 p)=>Visibility(p,Vector3.up);
        internal float ArchitecturalVisibility(Vector3 p,Vector3 normal)=>Visibility(p,normal);
        private void BuildWaterSurface(int width,int height,Texture2D basin)
        {
            var pattern=Resources.Load<Texture2D>("Art/FormalFirstBattle32/rain_court_water");
            float x=width+3.8f-3.45f*.5f,z=height*.5f+1-(height+4.65f)*.5f;
            rainWater=new Rect(x,z,3.45f,height+4.65f);
            var origin=transform.position;
            water=Hd2DCourtyardWaterMaterial.Create(basin,pattern,new Vector4(origin.x+x,origin.z+z,3.45f,height+4.65f),.5f);
            materials.Add(water);
            for(int i=0;i<3;i++)
                water.SetVector("_Obstacle"+i,new Vector4(origin.x+width+3.8f-.75f,origin.z+height*.5f-.8f+i*.95f-.32f,1.5f,.64f));
            // A textured horizontal card, rather than a colour-only water cube.
            var mesh=new Mesh{name="32PPU庭院水面",vertices=new[]{new Vector3(x,-.0405f,z),new Vector3(x+3.45f,-.0405f,z),new Vector3(x+3.45f,-.0405f,z+height+4.65f),new Vector3(x,-.0405f,z+height+4.65f)},triangles=new[]{0,2,1,0,3,2}};
            mesh.RecalculateNormals();mesh.RecalculateBounds();meshes.Add(mesh);
            var go=new GameObject("浅水面",typeof(MeshFilter),typeof(MeshRenderer)){layer=layer};go.transform.SetParent(scenery,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=water;renderer.shadowCastingMode=ShadowCastingMode.Off;
        }
        private void BakePavingVisibility()
        {
            foreach(var filter in scenery.GetComponentsInChildren<MeshFilter>())
            {
                if(filter.sharedMesh.name!="外围连续铺地"&&filter.sharedMesh.name!="原生连续种植土面")continue;
                var mesh=filter.sharedMesh;var vertices=mesh.vertices;var mask=new Vector2[vertices.Length];
                for(int i=0;i<vertices.Length;i++)mask[i]=new Vector2(GroundVisibility(vertices[i]),0);
                mesh.uv2=mask;
                var host=Object.FindAnyObjectByType<CombatPrototypeBootstrap>();
                var profile=Hd2DBattlefieldProfile.Resolve(host.CurrentLevelId);
                bool rain=profile.WetStone;
                foreach(var material in filter.GetComponent<MeshRenderer>().sharedMaterials)
                {
                    if(!wetPavingRegions.Contains(material.mainTexture))continue;
                    material.shader=Shader.Find(profile.GroundShader);
                    if(rain&&wetPavingRegions.Contains(material.mainTexture))
                    {material.SetFloat("_Smoothness",.08f);material.SetFloat("_ClearCoatMask",.42f);material.SetFloat("_ClearCoatSmoothness",.88f);}
                    // Cropped albedo and its paired fields always retain the same UV region.
                    if(!pavingRegions.Contains(material.mainTexture as Texture2D))Hd2DBattlefieldMaterialMaps.Apply(material,material.mainTexture);
                }
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
        private Mesh BakedBox(MeshFilter source,bool splitMasonry)
        {
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var uv=new List<Vector2>();var texcoords=new List<Vector2>();var triangles=new List<int>();
            var sideTriangles=new List<int>();
            var size=source.transform.localScale;var center=source.transform.localPosition;
            foreach(var n in new[]{Vector3.up,Vector3.down,Vector3.right,Vector3.left,Vector3.forward,Vector3.back})
            {
                // The bed's horizontal cap uses native frameless variants below;
                // retain the original thin volume and its contact/occlusion bounds.
                if(n==Vector3.up&&source.GetComponent<MeshRenderer>().sharedMaterial==earth)continue;
                var u=Mathf.Abs(n.y)>.8f?Vector3.right:Vector3.up;var v=Vector3.Cross(n,u);
                float a=Vector3.Scale(u,size).magnitude,b=Vector3.Scale(v,size).magnitude,c=Vector3.Scale(n,size).magnitude;
                int nx=Mathf.Clamp(Mathf.CeilToInt(a/.7f),1,32),ny=Mathf.Clamp(Mathf.CeilToInt(b/.7f),1,32),start=vertices.Count;
                for(int y=0;y<=ny;y++)for(int x=0;x<=nx;x++)
                {
                    var p=center+n*c*.5f+u*((x/(float)nx-.5f)*a)+v*((y/(float)ny-.5f)*b);
                    vertices.Add(p);normals.Add(n);uv.Add(new Vector2(Visibility(p,n),0));
                    // One 32-texel tile per world unit on every plane; never stretch
                    // a single tile across the full platform or a tall pillar.
                    bool masonrySide=splitMasonry&&Mathf.Abs(n.y)<.8f;
                    texcoords.Add(masonrySide?new Vector2(Mathf.Abs(n.x)>.5f?p.z:p.x,p.y*32f/22f):
                        new Vector2(Vector3.Dot(p,u),Vector3.Dot(p,v)));
                }
                var faces=splitMasonry&&Mathf.Abs(n.y)<.8f?sideTriangles:triangles;
                for(int y=0;y<ny;y++)for(int x=0;x<nx;x++){int i=start+y*(nx+1)+x;faces.AddRange(new[]{i,i+1,i+nx+2,i,i+nx+2,i+nx+1});}
            }
            var mesh=new Mesh{name="静态建筑遮蔽烘焙",vertices=vertices.ToArray(),normals=normals.ToArray(),uv=texcoords.ToArray(),uv2=uv.ToArray(),subMeshCount=splitMasonry?2:1};
            mesh.SetTriangles(triangles,0);if(splitMasonry)mesh.SetTriangles(sideTriangles,1);
            mesh.RecalculateTangents();mesh.RecalculateBounds();meshes.Add(mesh);return mesh;
        }
        private void BakeAndCombineArchitecture()
        {
            var shader=Shader.Find("OCC/HD2D/BakedArchitecture");
            if(shader==null)throw new System.InvalidOperationException("Missing architecture visibility shader");
            var groups=new Dictionary<Material,List<CombineInstance>>();
            var temporaryMeshes=new List<Mesh>();
            foreach(var source in architecture)
            {
                var renderer=source.GetComponent<MeshRenderer>();var material=renderer.sharedMaterial;
                bool split=material==stone||material==trim;
                var box=BakedBox(source,split);temporaryMeshes.Add(box);Add(material,box,0);
                if(split)Add(material==stone?stoneFace:trimFace,box,1);
                renderer.enabled=false;
            }
            foreach(var group in groups)
            {
                var mesh=new Mesh{name="合批_"+group.Key.name,indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};mesh.CombineMeshes(group.Value.ToArray(),true,false);meshes.Add(mesh);
                var material=new Material(group.Key){shader=shader};materials.Add(material);
                if(group.Key.mainTexture==masonryTexture)Hd2DBattlefieldMaterialMaps.Apply(material,masonrySourceTexture);
                else if(group.Key.mainTexture==earthTexture)Hd2DBattlefieldMaterialMaps.Apply(material,Resources.Load<Texture2D>("Art/FormalAcademyIndependentFloors32/academy_block_earth_b"));
                var go=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer)){layer=layer};go.transform.SetParent(scenery,false);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=material;CombinedArchitectureCount++;
            }
            // Both top and side groups consume the same temporary box. Release
            // it only after every material group has copied its submesh.
            foreach(var part in temporaryMeshes){meshes.Remove(part);Destroy(part);}
            void Add(Material material,Mesh mesh,int submesh)
            {
                if(!groups.TryGetValue(material,out var parts)){parts=new List<CombineInstance>();groups.Add(material,parts);}
                parts.Add(new CombineInstance{mesh=mesh,subMeshIndex=submesh,transform=Matrix4x4.identity});
            }
        }
        private void Roof(float x,float z,float width,float depth,float eaves,float ridge)
        {
            rainRoofs.Add((new Rect(x-width*.5f,z-depth*.5f,width,depth),eaves,ridge));
            var mesh=new Mesh{name="学院真实双坡屋顶",vertices=new[]{new Vector3(-width*.5f,eaves,-depth*.5f),new Vector3(width*.5f,eaves,-depth*.5f),new Vector3(width*.5f,ridge,0),new Vector3(-width*.5f,ridge,0),new Vector3(-width*.5f,eaves,depth*.5f),new Vector3(width*.5f,eaves,depth*.5f)},triangles=new[]{0,3,2,0,2,1,3,4,5,3,5,2}};
            // Preserve the same texel/world density on sloped roof planes.
            var roofUv=new Vector2[mesh.vertexCount];var roofVertices=mesh.vertices;
            float run=Mathf.Sqrt(depth*depth*.25f+(ridge-eaves)*(ridge-eaves));
            for(int i=0;i<roofUv.Length;i++)roofUv[i]=new Vector2(roofVertices[i].x,(i==2||i==3)?run:((i==4||i==5)?2*run:0));
            mesh.uv=roofUv;var visibility=new Vector2[mesh.vertexCount];for(int i=0;i<visibility.Length;i++)visibility[i]=Vector2.right;mesh.uv2=visibility;mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();meshes.Add(mesh);
            var go=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer)){layer=layer};go.transform.SetParent(scenery,false);go.transform.localPosition=new Vector3(x,0,z);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterial=roof;
            Box("屋脊压条",new Vector3(x,ridge+.015f,z),new Vector3(width+.08f,.06f,.08f),roof);
        }
        private void FrameOutskirts(Hd2DBattlefieldProfile profile,int width,int height)
        {
            // Peripheral enclosure belongs to the scenery, outside the logical
            // board and its protected apron. Openings follow the existing crosswalk.
            bool interior=profile.Environment==Hd2DEnvironmentKind.InteriorHall||profile.Environment==Hd2DEnvironmentKind.AetherTower;
            bool ruined=profile.Environment==Hd2DEnvironmentKind.RuinSite;
            float leftOffset=profile.Trees?5.2f:4.25f;
            float rightOffset=profile.WaterGarden?6.2f:4.25f;
            float wallHeight=interior?1.5f:.65f;
            float crossing=height*.5f;
            foreach(float x in new[]{-leftOffset,width+rightOffset})
            {
                for(int section=0;section<2;section++)
                {
                    float start=section==0?-3f:crossing+1.25f;
                    float end=section==0?crossing-1.25f:height+3f;
                    float length=end-start;
                    if(ruined)
                    {
                        // Three surviving lengths with actual gaps, not a continuous
                        // pristine wall recoloured to represent a ruin.
                        for(int fragment=0;fragment<3;fragment++)
                        {
                            float span=length/3f-.45f,h=fragment==1?.35f:.75f;
                            Box("外围遗址断墙",new Vector3(x,-.135f+h*.5f,start+(fragment+.5f)*length/3f),new Vector3(.5f,h,span),stone);
                        }
                    }
                    else
                    {
                        Box("外围空间侧墙",new Vector3(x,-.135f+wallHeight*.5f,(start+end)*.5f),new Vector3(.45f,wallHeight,length),stone);
                        Box("外围侧墙石压顶",new Vector3(x,-.135f+wallHeight+.045f,(start+end)*.5f),new Vector3(.58f,.09f,length+.12f),trim);
                    }
                    foreach(float z in new[]{start,end})
                        Box("外围边界承重墩",new Vector3(x,-.135f+(wallHeight+.2f)*.5f,z),new Vector3(.7f,wallHeight+.2f,.7f),trim);
                }
            }
        }
        private void FurnishOutskirts(Hd2DBattlefieldProfile profile,int width,int height)
        {
            // Existing native cards frame the board. Their feet and entire width
            // stay outside the logical cells; no cover, loot or collision is added.
            if(profile.Trees)
            {
                var bench=Resources.Load<Texture2D>("Art/AcademyCoverCandidates20260924/academy_light_wood_bench_intact");
                var planter=Resources.Load<Texture2D>("Art/AcademyCoverCandidates20260924/academy_light_planter_cover_intact");
                if(bench!=null)
                {
                    SpritePlane("外围庭院长椅",bench,new Vector3(-2.1f,-.12f,height*.38f),true);
                    SpritePlane("后庭休息长椅",bench,new Vector3(width+2.2f,.31f,height+1.4f),true);
                }
                if(planter!=null)
                {
                    SpritePlane("左侧庭院花箱",planter,new Vector3(-3.7f,.14f,height*.55f),true);
                    SpritePlane("右侧庭院花箱",planter,new Vector3(width+1.9f,-.12f,profile.WaterGarden?-2f:1.1f),true);
                    SpritePlane("后庭花箱",planter,new Vector3(width+1.8f,.31f,height+2.2f),true);
                }
            }
            else
            {
                string id=profile.Environment==Hd2DEnvironmentKind.InteriorHall?"academy_heavy_archive_stack_intact":
                    profile.Environment==Hd2DEnvironmentKind.RuinSite?"academy_heavy_masonry_screen_damaged":"academy_aether_pillar_intact";
                var prop=Resources.Load<Texture2D>("Art/FormalAcademyCombat32/"+id);
                var fixtures=AcademyBattlefieldLayoutCatalog.PeripheralFixtureIds(profile.LevelId);
                var left=fixtures.Length>0?RequireFixture(fixtures[0]):prop;
                var rear=fixtures.Length>1?RequireFixture(fixtures[1]):prop;
                if(left!=null)
                {
                    SpritePlane("左侧外围陈设_"+(fixtures.Length>0?fixtures[0]:id),left,new Vector3(-2.7f,-.12f,height*.45f),true);
                }
                if(rear!=null)SpritePlane("后庭外围陈设_"+(fixtures.Length>1?fixtures[1]:id),rear,new Vector3(width+2.4f,.31f,height+1.5f),true);
            }
        }
        private static Texture2D RequireFixture(string id)
        {
            var texture=Resources.Load<Texture2D>("Art/FormalAcademyStructures32/"+id);
            if(texture==null)throw new System.InvalidOperationException("Missing declared peripheral fixture: "+id);
            return texture;
        }
        private static bool GardenCell(int x,int z,int width,int height,Hd2DBattlefieldProfile profile)
        {
            if(!profile.Trees)return false;
            bool left=x>=-8&&x<=-3&&z>=-5&&z<=height+4;
            int rightStart=profile.WaterGarden?width+7:width+3;
            bool right=x>=rightStart&&x<rightStart+5&&z>=-4&&z<=height+3;
            int forecourtMargin=profile.Environment==Hd2DEnvironmentKind.AcademyCourt||profile.Environment==Hd2DEnvironmentKind.StoneRoad?1:2;
            bool forecourt=z>=-6&&z<=-3&&(x<-2||x>width+forecourtMargin);
            return left||right||forecourt;
        }
        private void Paving(Texture2D texture,Rect textureUv,int width,int height,Hd2DBattlefieldProfile profile)
        {
            var m=Material("外围原生石路",new Color(.83f,.85f,.82f));m.mainTexture=texture;
            bool native=texture.width==32&&texture.height==32&&textureUv==new Rect(0,0,1,1);
            var gardenSource=Resources.Load<Texture2D>("Art/FormalAcademyIndependentFloors32/academy_block_earth_b");
            var gardenVariants=Hd2DBattlefieldMaterialMaps.NativeVariants(gardenSource,out _);
            if(gardenVariants.Length==0)throw new System.InvalidOperationException("Registered native soil variants required");
            foreach(var variant in gardenVariants)
            {
                var garden=Material("外围连续种植土_"+variant.name,Color.white);garden.shader=Shader.Find("OCC/HD2D/BakedArchitecture");
                garden.mainTexture=NativePavingRegion(variant);
                Hd2DBattlefieldMaterialMaps.ApplyRegion(garden,variant,NativePavingRegion);gardenMaterials.Add(garden);
            }
            if(native)
            {
                pavingTexture=NativePavingRegion(texture);m.mainTexture=pavingTexture;m.shader=Shader.Find(profile.GroundShader);
                Hd2DBattlefieldMaterialMaps.ApplyRegion(m,texture,NativePavingRegion);
            }
            var pavingMaterials=new List<Material>{m};
            var siblings=native?Hd2DBattlefieldMaterialMaps.NativeVariants(texture,out _):System.Array.Empty<Texture2D>();
            bool stoneSurface=false;
            if(siblings.Length>0)
            {
                Hd2DBattlefieldMaterialMaps.NativeVariants(texture,out stoneSurface);pavingMaterials.Clear();
                foreach(var variant in siblings)
                {
                    var surface=Material("外围同族原生变体_"+variant.name,m.color);surface.shader=Shader.Find(profile.GroundShader);
                    var crop=NativePavingRegion(variant);surface.mainTexture=crop;
                    if(stoneSurface)wetPavingRegions.Add(crop);
                    Hd2DBattlefieldMaterialMaps.ApplyRegion(surface,variant,NativePavingRegion);pavingMaterials.Add(surface);
                }
            }
#if UNITY_EDITOR
            var review=Hd2DGroundMaterialReview.Current;
            if(native&&review!=null&&review.Replaces(texture))
            {
                pavingMaterials.Clear();
                foreach(var variant in review.Variants)
                {
                    var candidate=Material("外围独立石材变体_"+variant.name,m.color);candidate.shader=Shader.Find(profile.GroundShader);
                    var crop=NativePavingRegion(variant);candidate.mainTexture=crop;wetPavingRegions.Add(crop);
                    Hd2DBattlefieldMaterialMaps.ApplyRegion(candidate,variant,NativePavingRegion);pavingMaterials.Add(candidate);
                }
            }
#endif
            int apronMaterialCount=pavingMaterials.Count;
            var outerSource=Resources.Load<Texture2D>(profile.SurroundOuterResource);
            var outerVariants=Hd2DBattlefieldMaterialMaps.NativeVariants(outerSource,out bool outerStone);
            if(outerVariants.Length==0)throw new System.InvalidOperationException("Registered surrounding material role required: "+profile.SurroundOuterResource);
            foreach(var variant in outerVariants)
            {
                var surface=Material("外围远场原生变体_"+variant.name,m.color);surface.shader=Shader.Find(profile.GroundShader);
                var crop=NativePavingRegion(variant);surface.mainTexture=crop;
                if(outerStone)wetPavingRegions.Add(crop);
                Hd2DBattlefieldMaterialMaps.ApplyRegion(surface,variant,NativePavingRegion);pavingMaterials.Add(surface);
            }
            var verts=new List<Vector3>();var uv=new List<Vector2>();
            var soilTriangles=new List<List<int>>();foreach(var unused in gardenMaterials)soilTriangles.Add(new List<int>());
            var pavingTriangles=new List<List<int>>();foreach(var unused in pavingMaterials)pavingTriangles.Add(new List<int>());
            var xEdges=PavingEdges(-14,width+14);var zEdges=PavingEdges(-9,height+18);
            for(int zi=0;zi<zEdges.Count-1;zi++)for(int xi=0;xi<xEdges.Count-1;xi++)
            {
                float x0=xEdges[xi],x1=xEdges[xi+1],z0=zEdges[zi],z1=zEdges[zi+1];
                int x=Mathf.FloorToInt((x0+x1)*.5f),y=Mathf.FloorToInt((z0+z1)*.5f);
                if(x>=0&&x<width&&y>=0&&y<height)continue;
                int n=verts.Count;verts.AddRange(new[]{new Vector3(x0,-.135f,z0),new Vector3(x1,-.135f,z0),new Vector3(x1,-.135f,z1),new Vector3(x0,-.135f,z1)});
                bool soil=GardenCell(x,y,width,height,profile);
                // Decorative surrounds have no logical grid. Remove only the source's
                // cell frame, preserving one native texel per 1/32 metre with world UVs.
                uv.AddRange(soil||native?new[]{new Vector2(x0,z0),new Vector2(x1,z0),new Vector2(x1,z1),new Vector2(x0,z1)}:
                    new[]{new Vector2(textureUv.xMin,textureUv.yMin),new Vector2(textureUv.xMax,textureUv.yMin),new Vector2(textureUv.xMax,textureUv.yMax),new Vector2(textureUv.xMin,textureUv.yMax)});
                if(soil||native)for(int i=n;i<n+4;i++)uv[i]*=32f/28f;
                bool outer=OuterSurroundCell(x,y,width,height,profile);
                int periodX=Mathf.FloorToInt((x0+x1)*.5f*32f/28f),periodZ=Mathf.FloorToInt((z0+z1)*.5f*32f/28f);
                int variantIndex=outer?apronMaterialCount+SurfaceVariant(periodX,periodZ,outerVariants.Length):SurfaceVariant(periodX,periodZ,apronMaterialCount);
#if UNITY_EDITOR
                if(!outer&&apronMaterialCount==3)variantIndex=review.VariantIndex(periodX,periodZ);
#endif
                (soil?soilTriangles[SurfaceVariant(periodX,periodZ,gardenMaterials.Count)]:pavingTriangles[variantIndex]).AddRange(new[]{n,n+2,n+1,n,n+3,n+2});
            }
            var mesh=new Mesh{name="外围连续铺地",vertices=verts.ToArray(),uv=uv.ToArray(),subMeshCount=pavingMaterials.Count+gardenMaterials.Count};
            for(int i=0;i<pavingMaterials.Count;i++)mesh.SetTriangles(pavingTriangles[i],i);
            for(int i=0;i<gardenMaterials.Count;i++)mesh.SetTriangles(soilTriangles[i],pavingMaterials.Count+i);
            mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();meshes.Add(mesh);
            var go=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer)){layer=layer};go.transform.SetParent(scenery,false);
            earth.shader=Shader.Find("OCC/HD2D/BakedArchitecture");
            Hd2DBattlefieldMaterialMaps.Apply(earth,Resources.Load<Texture2D>("Art/FormalAcademyIndependentFloors32/academy_block_earth_b"));
            pavingMaterials.AddRange(gardenMaterials);go.GetComponent<MeshFilter>().sharedMesh=mesh;go.GetComponent<MeshRenderer>().sharedMaterials=pavingMaterials.ToArray();
        }
        private void SoilBedSurface(Rect bounds,float y)
        {
            var vertices=new List<Vector3>();var uv=new List<Vector2>();
            var triangles=new List<List<int>>();foreach(var unused in gardenMaterials)triangles.Add(new List<int>());
            List<float> Edges(float min,float max)
            {
                var result=new List<float>{min};
                // Match the native 28-texel period of the surrounding soil. Each
                // whole period keeps one variant, including negative world UVs.
                for(int k=Mathf.CeilToInt(min*8f/7f);k*7f/8f<max;k++)
                {float edge=k*7f/8f;if(edge>min+.0001f)result.Add(edge);}
                result.Add(max);return result;
            }
            var xs=Edges(bounds.xMin,bounds.xMax);var zs=Edges(bounds.yMin,bounds.yMax);
            for(int zi=0;zi<zs.Count-1;zi++)for(int xi=0;xi<xs.Count-1;xi++)
            {
                float x0=xs[xi],x1=xs[xi+1],z0=zs[zi],z1=zs[zi+1];int n=vertices.Count;
                vertices.AddRange(new[]{new Vector3(x0,y,z0),new Vector3(x1,y,z0),new Vector3(x1,y,z1),new Vector3(x0,y,z1)});
                uv.AddRange(new[]{new Vector2(x0,z0)*32f/28f,new Vector2(x1,z0)*32f/28f,new Vector2(x1,z1)*32f/28f,new Vector2(x0,z1)*32f/28f});
                int px=Mathf.FloorToInt((x0+x1)*.5f*32f/28f),pz=Mathf.FloorToInt((z0+z1)*.5f*32f/28f);
                triangles[SurfaceVariant(px,pz,gardenMaterials.Count)].AddRange(new[]{n,n+2,n+1,n,n+3,n+2});
            }
            var mesh=new Mesh{name="原生连续种植土面",vertices=vertices.ToArray(),uv=uv.ToArray(),subMeshCount=gardenMaterials.Count};
            mesh.uv2=System.Array.ConvertAll(mesh.vertices,p=>new Vector2(GroundVisibility(p),0));
            for(int i=0;i<triangles.Count;i++)mesh.SetTriangles(triangles[i],i);
            mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();meshes.Add(mesh);
            var go=new GameObject(mesh.name,typeof(MeshFilter),typeof(MeshRenderer)){layer=layer};go.transform.SetParent(scenery,false);
            go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterials=gardenMaterials.ToArray();renderer.shadowCastingMode=ShadowCastingMode.Off;
        }
        private static List<float> PavingEdges(int min,int max)
        {
            // Native crop repeats after 28/32 metres. Split at its boundaries
            // and at existing world-cell boundaries so the protected board and
            // garden regions stay exact, while one period retains one variant.
            var edges=new List<float>();
            for(int eighth=min*8;eighth<=max*8;eighth++)
                if(eighth%7==0||eighth%8==0)edges.Add(eighth/8f);
            return edges;
        }
        private static bool OuterSurroundCell(int x,int z,int width,int height,Hd2DBattlefieldProfile profile)
        {
            float centreX=x+.5f,centreZ=z+.5f;
            float dx=Mathf.Max(-centreX,centreX-width,0),dz=Mathf.Max(-centreZ,centreZ-height,0);
            if(Mathf.Max(dx,dz)<=profile.SurroundApronWidth)return false;
            // The broad arrival strip links board, foreground and actual entry
            // stairs. Only decorative material changes; no path is added to the map.
            float entry=width*.5f+(profile.Environment==Hd2DEnvironmentKind.AcademyCourt||profile.Environment==Hd2DEnvironmentKind.StoneRoad?.75f:0);
            bool arrival=Mathf.Abs(centreX-entry)<=1.5f;
            bool crossWalk=Mathf.Abs(centreZ-(height*.5f))<=1;
            if(arrival||crossWalk)return false;
            return true;
        }
        private static int SurfaceVariant(int x,int y,int count)
        {
            unchecked{uint h=(uint)x*0x9e3779b1u^(uint)y*0x85ebca6bu^0x4f434347u;h^=h>>16;h*=0x7feb352du;h^=h>>15;return(int)(h%(uint)count);}
        }
        private Texture2D NativePavingRegion(Texture2D source)
        {
            if(source==null||source.width!=32||source.height!=32)throw new System.InvalidOperationException("Native32 paving region required");
            var texture=new Texture2D(28,28,source.format,false,
                !UnityEngine.Experimental.Rendering.GraphicsFormatUtility.IsSRGBFormat(source.graphicsFormat))
                {name=source.name+"_外围原生内区",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Repeat};
            Graphics.CopyTexture(source,0,0,2,2,28,28,texture,0,0,0,0);pavingRegions.Add(texture);return texture;
        }
        private void SpritePlane(string name,Texture2D texture,Vector3 p,bool screenFacing=false)
        {
            var m=Material(name+"原生像素材质",Color.white);m.mainTexture=texture;m.SetFloat("_AlphaClip",1);m.SetFloat("_Cutoff",.5f);m.SetFloat("_Cull",0);m.EnableKeyword("_ALPHATEST_ON");
            Hd2DEnvironmentMaterialMaps.Apply(m,texture);
            float w=texture.width/32f,h=texture.height/32f;
            var mesh=new Mesh{name=name,vertices=new[]{new Vector3(-w*.5f,0,0),new Vector3(w*.5f,0,0),new Vector3(w*.5f,h,0),new Vector3(-w*.5f,h,0)},uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up},triangles=new[]{0,2,1,0,3,2}};
            mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();meshes.Add(mesh);
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
                Hd2DEnvironmentMaterialMaps.Apply(visible,texture);
                Hd2DBattlefieldPropMaps.Apply(visible,texture);
                Hd2DVegetationSurfaceFields.ApplyRegistered(gameObject,visible,texture);
                var caster=new GameObject(name+"_直立投影",typeof(MeshFilter),typeof(MeshRenderer)){layer=layer};caster.transform.SetParent(scenery,false);caster.transform.localPosition=p;
                caster.GetComponent<MeshFilter>().sharedMesh=mesh;var shadow=caster.GetComponent<MeshRenderer>();shadow.sharedMaterial=m;shadow.shadowCastingMode=ShadowCastingMode.ShadowsOnly;shadow.receiveShadows=false;
            }
        }
        private void OnDestroy(){foreach(var m in materials)Destroy(m);foreach(var mesh in meshes)Destroy(mesh);if(masonryTexture!=null)Destroy(masonryTexture);if(earthTexture!=null)Destroy(earthTexture);if(wallFaceTexture!=null)Destroy(wallFaceTexture);foreach(var texture in pavingRegions)Destroy(texture);}
    }
}


