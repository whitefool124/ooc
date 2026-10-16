using UnityEngine;

namespace OCC.Combat.Presentation
{
    // Own the returned material with its scene. This is an opaque optical-depth
    // approximation with analytic basin refraction, not screen-space refraction.
    internal static class Hd2DCourtyardWaterMaterial
    {
        // The optical shore band is narrower than half a cell, so the eight
        // neighbours describe every bank that can affect this cell. Keep open
        // water seams out of the distance field, including concave corners.
        internal static void ConfigureShore(MaterialPropertyBlock properties,GridPosition cell,
            Vector3 origin,bool[] water,int width,int height)
        {
            bool Wet(int x,int y)=>x>=0&&y>=0&&x<width&&y<height&&water[y*width+x];
            int x=cell.X,y=cell.Y;
            properties.SetFloat("_UseCellShore",1);
            properties.SetVector("_WaterCell",new Vector4(origin.x+x,origin.z+y,1,1));
            properties.SetVector("_ShoreEdges",new Vector4(
                Wet(x-1,y)?0:1,Wet(x+1,y)?0:1,Wet(x,y-1)?0:1,Wet(x,y+1)?0:1));
            properties.SetVector("_ShoreCorners",new Vector4(
                Wet(x-1,y-1)?0:1,Wet(x+1,y-1)?0:1,Wet(x-1,y+1)?0:1,Wet(x+1,y+1)?0:1));
        }

        internal static Material Create(Texture2D basin,Texture2D pattern,Vector4 bounds,float depth)
        {
            var shader=Shader.Find("OCC/HD2D/CourtyardWater");
            if(shader==null||basin==null||pattern==null)throw new System.InvalidOperationException("Missing courtyard water shader/native textures");
            var material=new Material(shader){name="32PPU浅水吸收与波纹",mainTexture=basin};
            material.SetTexture("_WaterPattern",pattern);material.SetVector("_WaterBounds",bounds);
            material.SetFloat("_WaterDepth",depth);material.SetFloat("_WaveStrength",.028f);
            material.SetFloat("_WaveSpeed",.7f);material.SetFloat("_Smoothness",.82f);
            material.SetFloat("_RefractionStrength",.35f);
            material.SetColor("_DeepColor",new Color(.07f,.23f,.27f));
            return material;
        }
    }
}
