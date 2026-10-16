using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace OCC.Combat.Presentation
{
    /// <summary>Spatial effect model. All poses are baked on the same1/32m lattice; no per-frame meshing or scale animation.</summary>
    public sealed class SkyFurnaceVoxelStage
    {
        private readonly Action<UnityEngine.Object> register;
        private readonly Transform root;
        private readonly int layer;
        private readonly Material ringMaterial,fireMaterial,coreMaterial,brightMaterial;
        private readonly MaterialPropertyBlock block=new MaterialPropertyBlock();
        private readonly List<MeshRenderer> rings=new List<MeshRenderer>(),trims=new List<MeshRenderer>(),flows=new List<MeshRenderer>();
        private readonly List<Mesh[]> flowPoses=new List<Mesh[]>();
        private readonly MeshRenderer core,innerCore,wave,beam;
        private readonly Mesh[] wavePoses;
        public int ModelMeshCount {get;private set;}
        public int TotalModelVertices {get;private set;}
        private readonly List<Mesh> meshes=new List<Mesh>();
        public IReadOnlyList<Mesh> Meshes=>meshes;
        public float MaximumSurfacePitch=>VfxVoxelMesh.Pitch;
        private T Own<T>(T value) where T:UnityEngine.Object {register(value);return value;}
        public SkyFurnaceVoxelStage(Transform parent,int renderLayer,Vector3 centre,Shader shader,Texture2D band,Texture2D fire,Action<UnityEngine.Object> owner)
        {
            register=owner;layer=renderLayer;root=new GameObject("体素天炉 · 原生32空间模型").transform;root.SetParent(parent,false);root.position=centre;root.gameObject.layer=layer;
            ringMaterial=Material(shader,band,new Vector4(18,4,90,14),.28f,"体素约束环 · 像素符文材质");
            fireMaterial=Material(shader,fire,new Vector4(9,20,108,106),1.2f,"体素火流 · 独立像素能量材质");
            coreMaterial=Material(shader,fire,new Vector4(9,20,108,106),2.0f,"压缩炉芯外层");
            brightMaterial=Material(shader,fire,new Vector4(9,20,108,106),5,"压缩炉芯内核");
            for(int i=0;i<3;i++)
            {
                float radius=new[]{1.65f,1.12f,.55f}[i],height=new[]{3.15f,2.22f,1.28f}[i];
                var body=new VfxVoxelMesh();body.Ring(radius,.15625f,.25f);
                // Four small thickened junctions are integrated into the same mesh.
                for(int j=0;j<4;j++){float a=j*Mathf.PI*.5f;var p=new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);body.Shape(p-new Vector3(.125f,.1875f,.125f),p+new Vector3(.125f,.1875f,.125f),v=>true);}
                var r=Renderer("有厚度的第"+(i+1)+"层炉环",Bake(body,"voxel ring "+i),ringMaterial);r.transform.localPosition=Vector3.up*height;r.shadowCastingMode=ShadowCastingMode.On;rings.Add(r);
                var rim=new VfxVoxelMesh();rim.Ring(radius,.1875f,.03125f);var trim=Renderer("炉环上沿导能口 "+i,Bake(rim,"voxel ring conductor "+i),fireMaterial);trim.transform.localPosition=Vector3.up*(height+.140625f);trims.Add(trim);
            }
            for(int i=0;i<6;i++)
            {
                var poses=new Mesh[8];float seed=i*Mathf.PI/3;
                for(int pose=0;pose<poses.Length;pose++)
                {
                    float compression=pose/(float)(poses.Length-1);var path=new List<Vector3>();var radii=new List<float>();
                    for(int j=0;j<=36;j++)
                    {
                        float u=j/36f,angle=seed+u*1.7f;
                        float radius=Mathf.Lerp(.17f+u*1.2f,.14f+u*.2f,compression);
                        float y=Mathf.Lerp(1.20f+u*1.9f,2.19f+u*.32f,compression);
                        path.Add(new Vector3(Mathf.Cos(angle)*radius,y,Mathf.Sin(angle)*radius));
                        radii.Add(Mathf.Lerp(.045f+Mathf.Sin(u*Mathf.PI)*.085f,.04f,compression));
                    }
                    var volume=new VfxVoxelMesh();volume.Tube(path,radii);poses[pose]=Bake(volume,"native32 fire current "+i+" pose "+pose);
                }
                var renderer=Renderer("中空炉腔火流 "+i,poses[0],fireMaterial);flows.Add(renderer);flowPoses.Add(poses);
            }
            var outer=new VfxVoxelMesh();outer.Shape(-Vector3.one*.21875f,Vector3.one*.21875f,p=>p.magnitude<=.21875f&&p.magnitude>=.155f&&(Mathf.Abs(p.x)<.046875f||Mathf.Abs(p.y)<.046875f||Mathf.Abs(p.z)<.046875f));core=Renderer("体素压缩炉芯",Bake(outer,"voxel core shell"),coreMaterial);core.transform.localPosition=Vector3.up*2.35f;
            var inner=new VfxVoxelMesh();inner.Sphere(Vector3.zero,.125f);innerCore=Renderer("体素炉芯高热内核",Bake(inner,"voxel core centre"),brightMaterial);innerCore.transform.localPosition=core.transform.localPosition;
            var beamShape=new VfxVoxelMesh();beamShape.Shape(new Vector3(-.0625f,0,-.0625f),new Vector3(.0625f,2.35f,.0625f),p=>Mathf.Abs(p.x)+Mathf.Abs(p.z)<.09f);
            beam=Renderer("真实厚度竖直贯穿束",Bake(beamShape,"voxel piercing beam"),brightMaterial);
            wavePoses=new Mesh[25];
            for(int pose=0;pose<wavePoses.Length;pose++)
            {
                float u=pose/(float)(wavePoses.Length-1),radius=Mathf.Lerp(.28f,2.5f,u);
                var volume=new VfxVoxelMesh();
                volume.Shape(new Vector3(-radius-.5f,0,-radius-.5f),new Vector3(radius+.5f,1.3f,radius+.5f),p=>
                {
                    float angle=Mathf.Atan2(p.z,p.x),r=new Vector2(p.x,p.z).magnitude;
                    float crest=.5f+.5f*Mathf.Sin(angle*5+u*2);
                    float height=.25f+(.38f+.58f*crest)*Mathf.Sin(Mathf.Lerp(.35f,Mathf.PI*.75f,u));
                    float sweptRadius=radius+p.y*.35f-Mathf.Pow(p.y,2)*.12f;
                    return p.y<=height&&Mathf.Abs(r-sweptRadius)<.09375f+.03125f*crest;
                });
                wavePoses[pose]=Bake(volume,"continuous voxel fire crown "+pose);
            }
            wave=Renderer("连贯体素火冠 · 全周有厚度",wavePoses[0],fireMaterial);
        }
        private Material Material(Shader shader,Texture2D texture,Vector4 region,float energy,string name)
        {var m=Own(new Material(shader){name=name});m.SetTexture("_MainTex",texture);m.SetVector("_SourceRect",region);m.SetFloat("_Energy",energy);return m;}
        private Mesh Bake(VfxVoxelMesh model,string name)
        {var mesh=Own(model.Bake(name));meshes.Add(mesh);ModelMeshCount++;TotalModelVertices+=mesh.vertexCount;return mesh;}
        private MeshRenderer Renderer(string name,Mesh mesh,Material material)
        {
            var node=new GameObject(name){layer=layer};node.transform.SetParent(root,false);node.AddComponent<MeshFilter>().sharedMesh=mesh;
            var r=node.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;return r;
        }
        private static float Ramp(float t,float a,float b)=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(a,b,t));
        private void Surface(MeshRenderer renderer,float reveal,float energy,float phase,int mode=2)
        {
            renderer.gameObject.SetActive(reveal>.001f);block.Clear();block.SetFloat("_Reveal",Mathf.Clamp01(reveal));block.SetFloat("_Energy",energy);block.SetFloat("_Phase",phase);block.SetFloat("_RevealMode",mode);
            var bounds=renderer.GetComponent<MeshFilter>().sharedMesh.bounds;block.SetVector("_RevealBounds",new Vector4(bounds.min.y,bounds.size.y,0,0));renderer.SetPropertyBlock(block);
        }
        public void Evaluate(float time,float impact)
        {
            float compression=Ramp(time,5,7),fade=1-Ramp(time,impact,impact+.9f);
            for(int i=0;i<rings.Count;i++)
            {
                float reveal=Ramp(time,3+i*.2f,3.75f+i*.2f)*fade;
                // Geometry stays fixed; energy travels through the surface in native-pixel steps.
                Surface(rings[i],reveal,.30f*(1-compression*.65f),time*8+i*5,1);
                Surface(trims[i],reveal,1.15f*(1-compression*.7f),time*12,1);
            }
            for(int i=0;i<flows.Count;i++)
            {
                int pose=Mathf.Clamp(Mathf.FloorToInt(compression*(flowPoses[i].Length-.001f)),0,flowPoses[i].Length-1);flows[i].GetComponent<MeshFilter>().sharedMesh=flowPoses[i][pose];
                flows[i].transform.localRotation=Quaternion.Euler(0,(i%2==0?1:-1)*time*14,0);
                Surface(flows[i],Ramp(time,3.4f+i*.08f,4.4f+i*.08f)*(1-Ramp(time,6.75f,7.25f)),1.05f+compression*.8f,time*18+i*9);
            }
            float coreReveal=Ramp(time,5.1f,5.9f)*(1-Ramp(time,impact,impact+.12f));
            Surface(core,coreReveal,2.3f+compression*1.1f,time*5);
            // Inner light appears through the shell's native-cell coverage, not a pasted highlight.
            Surface(innerCore,coreReveal,5.2f,time*3);
            core.transform.localRotation=Quaternion.Euler(time*12,time*19,0);innerCore.transform.localRotation=core.transform.localRotation;
            float endHeight=Mathf.Lerp(2.35f,0,Ramp(time,7,impact));
            beam.transform.localPosition=Vector3.up*endHeight;
            // A floor-aligned clip plane reveals the growing beam without stretching its texels.
            Surface(beam,time>=7&&time<impact+.17f?1:0,4.4f,0);
            block.Clear();block.SetFloat("_Reveal",time>=7&&time<impact+.17f?1:0);block.SetFloat("_Energy",4.4f);block.SetFloat("_ClipHeight",2.35f-endHeight);beam.SetPropertyBlock(block);
            float after=time-impact,u=Ramp(after,0,1.1f);int wavePose=Mathf.Clamp(Mathf.RoundToInt(u*(wavePoses.Length-1)),0,wavePoses.Length-1);wave.GetComponent<MeshFilter>().sharedMesh=wavePoses[wavePose];
            Surface(wave,after>=0?(1-Ramp(time,8.7f,10.3f)):0,1.4f*(1-Ramp(time,8.7f,10.3f)),time*20);
        }
    }
}
