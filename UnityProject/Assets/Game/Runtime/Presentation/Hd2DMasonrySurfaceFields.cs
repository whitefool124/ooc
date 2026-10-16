using UnityEngine;

namespace OCC.Combat.Presentation
{
    // Numeric surface data shared by all masonry in the owned battle scene.
    // Regions come from the native wall's authored brick rectangles, never from
    // diffuse brightness. Albedo, alpha, geometry, grid and PPU remain untouched.
    internal sealed class Hd2DMasonrySurfaceFields:MonoBehaviour
    {
        private Texture2D atlasNormal,atlasRoughness,bodyNormal,bodyRoughness;
        private static readonly RectInt[] Bricks={
            new RectInt(1,39,17,11),new RectInt(19,39,12,12),
            new RectInt(1,51,9,9),new RectInt(10,52,15,8),new RectInt(25,51,6,9)};
        internal static Hd2DMasonrySurfaceFields For(GameObject owner)
        {
            var fields=owner.GetComponent<Hd2DMasonrySurfaceFields>();
            if(fields==null){fields=owner.AddComponent<Hd2DMasonrySurfaceFields>();fields.Build();}
            return fields;
        }
        private static float Relief(int x,int y)
        {
            x=(x+32)%32;y=Mathf.Clamp(y,0,63);float topY=63-y;
            foreach(var brick in Bricks)
            {
                if(x<brick.xMin||x>=brick.xMax||topY<brick.yMin||topY>=brick.yMax)continue;
                float edge=Mathf.Min(Mathf.Min(x+.5f-brick.xMin,brick.xMax-x-.5f),
                    Mathf.Min(topY+.5f-brick.yMin,brick.yMax-topY-.5f));
                return .004f*Mathf.Clamp01(edge/1.5f); // Four millimetres over mortar; 1.5 native pixels of shoulder.
            }
            return 0;
        }
        private void Build()
        {
            var normals=new Color32[32*64];var roughness=new Color32[normals.Length];
            for(int y=0;y<64;y++)for(int x=0;x<32;x++)
            {
                // One native texel spans 1/32 metre in both tangent directions.
                float dx=(Relief(x+1,y)-Relief(x-1,y))*16f,dy=(Relief(x,y+1)-Relief(x,y-1))*16f;
                var n=new Vector3(-dx,-dy,1).normalized;int i=y*32+x;
                normals[i]=new Color32(Encode(n.x),Encode(n.y),Encode(n.z),255);
                byte r=(byte)Mathf.RoundToInt(Mathf.Lerp(.98f,.86f,Mathf.Clamp01(Relief(x,y)/.004f))*255);
                roughness[i]=new Color32(r,r,r,255);
            }
            atlasNormal=Texture("石砌共享结构法线32x64",normals,64,0);
            atlasRoughness=Texture("石砌共享粗糙度32x64",roughness,64,0);
            bodyNormal=Texture("石砌共享结构法线32x22",normals,22,4);
            bodyRoughness=Texture("石砌共享粗糙度32x22",roughness,22,4);
        }
        private static byte Encode(float value)=>(byte)Mathf.RoundToInt(Mathf.Clamp01(value*.5f+.5f)*255);
        private static Texture2D Texture(string name,Color32[] atlas,int height,int firstRow)
        {
            var pixels=new Color32[32*height];System.Array.Copy(atlas,firstRow*32,pixels,0,pixels.Length);
            var texture=new Texture2D(32,height,TextureFormat.RGBA32,false,true)
                {name=name,filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Repeat};
            texture.SetPixels32(pixels);texture.Apply(false,true);return texture;
        }
        internal void Apply(Material material,bool wholeAtlas)
        {
            material.SetTexture("_BumpMap",wholeAtlas?atlasNormal:bodyNormal);
            material.SetTexture("_RoughnessMap",wholeAtlas?atlasRoughness:bodyRoughness);
            material.SetFloat("_BumpScale",1);material.EnableKeyword("_NORMALMAP");material.EnableKeyword("_OCC_ROUGHNESS_MAP");
        }
        private void OnDestroy()
        {
            foreach(var texture in new[]{atlasNormal,atlasRoughness,bodyNormal,bodyRoughness})if(texture!=null)Destroy(texture);
        }
    }
}
