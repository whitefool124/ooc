using System;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    // Reuses the registered academy_tree crown/wood regions. These numeric
    // shading proxies do not derive geometry or material from diffuse colours.
    internal sealed class Hd2DVegetationSurfaceFields : MonoBehaviour
    {
        private Texture2D normal,leafData;
        // Native top-down image coordinates; crown relief is in world metres.
        private static readonly Vector4[] Crowns={
            new Vector4(48,24,22,18),new Vector4(22,37,18,17),
            new Vector4(70,39,22,20),new Vector4(28,60,20,12),new Vector4(61,57,24,13)};
        private static readonly float[] Relief={.16f,.14f,.15f,.10f,.12f};
        internal static bool ApplyRegistered(GameObject owner,Material material,Texture source)
        {
            var tree=Resources.Load<Texture2D>("Art/HD2DAcademyReview/academy_tree");
            if(tree==null||source!=tree)return false;
            if(tree.width!=96||tree.height!=96||!material.HasProperty("_FoliageData"))
                throw new InvalidOperationException("Registered native96 foliage shader/data required");
            var fields=owner.GetComponent<Hd2DVegetationSurfaceFields>();
            if(fields==null){fields=owner.AddComponent<Hd2DVegetationSurfaceFields>();fields.Build();}
            material.SetTexture("_BumpMap",fields.normal);material.SetFloat("_BumpScale",.65f);
            material.SetTexture("_FoliageData",fields.leafData);
            material.EnableKeyword("_NORMALMAP");material.EnableKeyword("_OCC_FOLIAGE_DATA");return true;
        }
        private static bool Wood(int x,int topY)=>x>=42&&x<=56&&topY>=58&&topY<=94;
        private static float Height(int x,int y)
        {
            x=Mathf.Clamp(x,0,95);y=Mathf.Clamp(y,0,95);int topY=95-y;
            if(Wood(x,topY))return .016f*Mathf.Clamp01(1-Mathf.Abs(x-49)/6f);
            float height=0;
            for(int i=0;i<Crowns.Length;i++)
            {
                var p=Crowns[i];float dx=(x-p.x)/p.z,dy=(topY-p.y)/p.w,r=dx*dx+dy*dy;
                if(r<1)height=Mathf.Max(height,Relief[i]*Mathf.Sqrt(1-r));
            }
            return height;
        }
        private void Build()
        {
            var normals=new Color32[96*96];var fields=new Color32[normals.Length];
            for(int y=0;y<96;y++)for(int x=0;x<96;x++)
            {
                float dx=(Height(x+1,y)-Height(x-1,y))*16,dy=(Height(x,y+1)-Height(x,y-1))*16;
                var n=new Vector3(-dx,-dy,1).normalized;int i=y*96+x;
                normals[i]=new Color32(Encode(n.x*.5f+.5f),Encode(n.y*.5f+.5f),Encode(n.z*.5f+.5f),255);
                float thickness=Mathf.Clamp01(Height(x,y)/.16f);
                // R is light-dependent leaf scatter; G only attenuates indirect GI.
                fields[i]=Wood(x,95-y)?new Color32(0,255,0,255):
                    new Color32(Encode(Mathf.Lerp(.30f,.14f,thickness)),Encode(1-.18f*thickness),0,255);
            }
            normal=Field("登记树冠体积法线96x96",normals);leafData=Field("登记薄叶散射与冠内遮蔽96x96",fields);
        }
        private static byte Encode(float value)=>(byte)Mathf.RoundToInt(Mathf.Clamp01(value)*255);
        private static Texture2D Field(string name,Color32[] pixels)
        {
            var texture=new Texture2D(96,96,TextureFormat.RGBA32,false,true)
                {name=name,filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            texture.SetPixels32(pixels);texture.Apply(false,true);return texture;
        }
        private void OnDestroy(){if(normal!=null)Destroy(normal);if(leafData!=null)Destroy(leafData);}
    }
}
