using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace OCC.Combat.Presentation
{
    internal readonly struct Hd2DRainSurface
    {
        internal readonly float Height;
        internal readonly bool Water;
        internal Hd2DRainSurface(float height,bool water){Height=height;Water=water;}
    }
    // Scene-owned technical rain geometry shared by runtime atmosphere and preview.
    public sealed class Hd2DWeatherRainPreview : MonoBehaviour
    {
        private Material rainMaterial;
        private Mesh rainMesh;
        private Material impactMaterial;
        private Mesh impactMesh;
        private Func<Vector2,Hd2DRainSurface> surfaceAt;
        private Vector2[] columns;
        private float[] seeds;
        private Vector4[] rainContacts,impactContacts;
        private const int Count=440,ImpactStride=4,ImpactVertices=28;
        private const float NativePitch=1f/32f,Floor=-.5f,Roof=5.5f;
        private float start;
        public void BuildRain(int width,int height,Camera camera)=>BuildRain(width,height,camera,p=>new Hd2DRainSurface(0,false));
        internal void BuildRain(int width,int height,Camera camera,Func<Vector2,Hd2DRainSurface> sampleSurface)
        {
            if(rainMesh!=null)throw new InvalidOperationException("Rain geometry already initialized");
            if(camera==null||sampleSurface==null)throw new ArgumentException("Rain camera and contact surfaces required");
            surfaceAt=sampleSurface;
            start=Time.unscaledTime;
            var shader=Shader.Find("OCC/HD2D/WeatherPreviewRain");if(shader==null)throw new InvalidOperationException("Weather rain shader missing");
            var impactShader=Shader.Find("OCC/HD2D/WeatherRainImpacts");if(impactShader==null)throw new InvalidOperationException("Rain impact shader missing");
            rainMaterial=new Material(shader);rainMaterial.SetFloat("_Roof",Roof);rainMaterial.SetFloat("_Floor",Floor);
            impactMaterial=new Material(impactShader);impactMaterial.SetFloat("_Roof",Roof);impactMaterial.SetFloat("_Floor",Floor);
            var rng=new System.Random(12102026);
            var vertices=new Vector3[Count*4];var uv=new Vector2[Count*4];var triangles=new int[Count*6];
            columns=new Vector2[Count];seeds=new float[Count];rainContacts=new Vector4[vertices.Length];
            Vector3 side=transform.InverseTransformDirection(camera.transform.right)*.006f;
            for(int i=0;i<Count;i++)
            {
                float x=(float)rng.NextDouble()*(width+10)-5,phase=(float)rng.NextDouble(),z=(float)rng.NextDouble()*(height+10)-5;
                x=(Mathf.Floor(x*32)+.5f)*NativePitch;z=(Mathf.Floor(z*32)+.5f)*NativePitch;
                columns[i]=new Vector2(x,z);seeds[i]=phase;
                var p=new Vector3(x,Floor+phase*(Roof-Floor),z);
                var streak=new Vector3(.08f,.28f,.03f);int v=i*4,t=i*6;
                vertices[v]=p-side;vertices[v+1]=p+side;vertices[v+2]=p+streak+side;vertices[v+3]=p+streak-side;
                uv[v]=new Vector2(0,0);uv[v+1]=new Vector2(1,0);uv[v+2]=new Vector2(1,1);uv[v+3]=new Vector2(0,1);
                triangles[t]=v;triangles[t+1]=v+2;triangles[t+2]=v+1;triangles[t+3]=v;triangles[t+4]=v+3;triangles[t+5]=v+2;
            }
            rainMesh=new Mesh{name="Seeded spatial rain geometry"};rainMesh.vertices=vertices;rainMesh.uv=uv;rainMesh.triangles=triangles;
            rainMesh.bounds=new Bounds(new Vector3(width*.5f,2.75f,height*.5f),new Vector3(width+16,12,height+16));
            gameObject.AddComponent<MeshFilter>().sharedMesh=rainMesh;
            var renderer=gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=rainMaterial;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            BuildImpacts(width,height,camera);RefreshSurfaces();
        }
        private void BuildImpacts(int width,int height,Camera camera)
        {
            var vertices=new List<Vector3>();var normals=new List<Vector3>();var pieces=new List<Vector2>();var corners=new List<Vector2>();var triangles=new List<int>();
            Vector3 right=transform.InverseTransformDirection(camera.transform.right),up=transform.InverseTransformDirection(camera.transform.up);
            var normal=-transform.InverseTransformDirection(camera.transform.forward);
            var quadCorners=new[]{new Vector2(-.5f,-.5f),new Vector2(.5f,-.5f),new Vector2(.5f,.5f),new Vector2(-.5f,.5f)};
            for(int i=0;i<Count;i+=ImpactStride)
            {
                var centre=new Vector3(columns[i].x,0,columns[i].y);
                for(int piece=0;piece<7;piece++)
                {
                    bool water=piece>=3;int n=vertices.Count;
                    foreach(var corner in quadCorners)
                    {
                        vertices.Add(water?centre:centre+(right*corner.x+up*corner.y)*NativePitch);
                        normals.Add(water?Vector3.up:normal);pieces.Add(new Vector2(water?piece-3:piece-1,water?1:0));corners.Add(corner);
                    }
                    triangles.AddRange(new[]{n,n+2,n+1,n,n+3,n+2});
                }
            }
            impactMesh=new Mesh{name="Native32 rain contact geometry"};impactMesh.SetVertices(vertices);impactMesh.SetNormals(normals);
            impactMesh.SetUVs(0,pieces);impactMesh.SetUVs(2,corners);impactMesh.SetTriangles(triangles,0);
            impactMesh.bounds=new Bounds(new Vector3(width*.5f,2.75f,height*.5f),new Vector3(width+16,12,height+16));
            impactContacts=new Vector4[vertices.Count];
            var go=new GameObject("雨滴接触溅点与水纹",typeof(MeshFilter),typeof(MeshRenderer)){layer=gameObject.layer};go.transform.SetParent(transform,false);
            go.GetComponent<MeshFilter>().sharedMesh=impactMesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=impactMaterial;
            renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
        }
        internal void RefreshSurfaces()
        {
            if(rainMesh==null||surfaceAt==null)return;
            bool changed=false;
            for(int i=0;i<Count;i++)
            {
                var contact=surfaceAt(columns[i]);
                var data=new Vector4(seeds[i],contact.Height,contact.Water?1:0,0);
                if(rainContacts[i*4]!=data)changed=true;
                for(int j=0;j<4;j++)rainContacts[i*4+j]=data;
                if(i%ImpactStride==0)
                    for(int j=0;j<ImpactVertices;j++)impactContacts[i/ImpactStride*ImpactVertices+j]=data;
            }
            if(!changed)return;
            rainMesh.SetUVs(1,rainContacts);impactMesh.SetUVs(1,impactContacts);
        }
        private void Update(){SetTime(Time.unscaledTime-start);}
        public void SetTime(float time)
        {if(rainMaterial!=null)rainMaterial.SetFloat("_WeatherTime",time);if(impactMaterial!=null)impactMaterial.SetFloat("_WeatherTime",time);}
        public void SetAmount(float amount)
        {
            amount=Mathf.Clamp01(amount);
            if(rainMaterial!=null)rainMaterial.SetFloat("_RainAmount",amount);
            if(impactMaterial!=null)impactMaterial.SetFloat("_RainAmount",amount);
            if(gameObject.activeSelf!=(amount>.001f))gameObject.SetActive(amount>.001f);
        }
        private void OnDestroy()
        {if(rainMaterial!=null)Destroy(rainMaterial);if(rainMesh!=null)Destroy(rainMesh);if(impactMaterial!=null)Destroy(impactMaterial);if(impactMesh!=null)Destroy(impactMesh);surfaceAt=null;}
    }
}
