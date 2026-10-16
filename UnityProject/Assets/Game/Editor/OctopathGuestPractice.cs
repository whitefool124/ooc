#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace OCC.Combat.EditorTools
{
    // Local practice only. Independently generated art stays outside shipping
    // Resources; this menu never saves a scene or modifies formal art.
    public static class OctopathGuestPractice
    {
        private const string Folder="../../Worldbuilding/归档/2026-10-10_城镇像素角色练习";
        [MenuItem("OCC/Research/Build town with laughing pixel guest (Play Mode)")]
        public static void Build()
        {
            if(!Application.isPlaying||Application.dataPath.Replace('\\','/')!="E:/数据库/OCC_Codex/UnityProject/Assets")
                throw new InvalidOperationException("Main project Play Mode required");
            if(OctopathTownStudy.Root==null)OctopathTownStudy.Build();
            var root=OctopathTownStudy.Root;
            if(root.transform.Find("Laughing yellow pixel guest")!=null)return;
            float ground=GroundHeight(root,new Vector3(-17,12,0));
            var texture=new Texture2D(2,2,TextureFormat.RGBA32,false,false)
            {name="Independent yellow guest native pixels",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            texture.LoadImage(File.ReadAllBytes(Path.GetFullPath(Path.Combine(Application.dataPath,Folder,"sprite_r1.png"))));
            var material=new Material(Shader.Find("OCC/Research/OctopathSurface"))
            {name="QA_PENDING yellow guest lit cutout",mainTexture=texture,color=new Color(.72f,.72f,.72f,1)};
            material.SetFloat("_AlphaClip",1);material.SetFloat("_Cutoff",.5f);material.EnableKeyword("_ALPHATEST_ON");material.renderQueue=2450;
            material.SetFloat("_Smoothness",.06f);material.SetFloat("_Metallic",0);material.SetFloat("_Cull",0);material.SetFloat("_SoftEmission",0);
            material.SetColor("_StudyAmbient",new Color(.22f,.24f,.25f));
            float w=texture.width/32f,h=texture.height/32f;
            var mesh=new Mesh{name="Guest 32PPU bottom anchored card"};
            mesh.vertices=new[]{new Vector3(-w/2,-1/32f,0),new Vector3(w/2,-1/32f,0),new Vector3(w/2,h-1/32f,0),new Vector3(-w/2,h-1/32f,0)};
            mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};mesh.triangles=new[]{0,2,1,0,3,2};mesh.RecalculateNormals();mesh.RecalculateBounds();
            MeshRenderer Card(string name)
            {
                var go=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer)){layer=30};
                go.transform.SetParent(root.transform,false);go.transform.position=new Vector3(-17,ground+.008f,0);go.transform.rotation=Quaternion.Euler(0,90,0);
                go.GetComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.GetComponent<MeshRenderer>();renderer.sharedMaterial=material;return renderer;
            }
            var face=Card("Laughing yellow pixel guest");face.shadowCastingMode=ShadowCastingMode.Off;face.receiveShadows=true;
            var shadow=Card("Guest upright shadow proxy");shadow.shadowCastingMode=ShadowCastingMode.ShadowsOnly;
            var control=root.GetComponent<OctopathTownStudyControls>();control.Owned.AddRange(new UnityEngine.Object[]{texture,material,mesh});
            control.Target=new Vector3(-16.5f,2.6f,0);control.Distance=21;control.ApplyCamera();
        }
        private static float GroundHeight(GameObject root,Vector3 origin)
        {
            // MeshCollider.Raycast avoids leaving physics components on research
            // geometry. Only authored walkable terrain/floors participate.
            float highest=float.NegativeInfinity;
            foreach(var renderer in root.GetComponentsInChildren<MeshRenderer>())
            {
                if(!renderer.name.Contains("T_Ground")&&!renderer.name.Contains("Floor"))continue;
                var bounds=renderer.bounds;
                if(origin.x<bounds.min.x||origin.x>bounds.max.x||origin.z<bounds.min.z||origin.z>bounds.max.z)continue;
                var filter=renderer.GetComponent<MeshFilter>();if(filter==null)continue;
                var probe=new GameObject("Temporary guest ground query");probe.transform.SetPositionAndRotation(renderer.transform.position,renderer.transform.rotation);probe.transform.localScale=renderer.transform.lossyScale;
                var collider=probe.AddComponent<MeshCollider>();collider.sharedMesh=filter.sharedMesh;
                if(collider.Raycast(new Ray(origin,Vector3.down),out var hit,30))highest=Mathf.Max(highest,hit.point.y);
                UnityEngine.Object.DestroyImmediate(probe);
            }
            if(float.IsNegativeInfinity(highest))throw new InvalidOperationException("No walkable surface at practice centre");
            return highest;
        }
    }
}
#endif
