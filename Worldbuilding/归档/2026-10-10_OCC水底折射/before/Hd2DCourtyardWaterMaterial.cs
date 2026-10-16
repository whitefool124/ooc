using UnityEngine;

namespace OCC.Combat.Presentation
{
    // Own the returned material with its scene. This is an opaque optical-depth
    // approximation over existing native textures, not screen-space refraction.
    internal static class Hd2DCourtyardWaterMaterial
    {
        internal static Material Create(Texture2D basin,Texture2D pattern,Vector4 bounds,float depth)
        {
            var shader=Shader.Find("OCC/HD2D/CourtyardWater");
            if(shader==null||basin==null||pattern==null)throw new System.InvalidOperationException("Missing courtyard water shader/native textures");
            var material=new Material(shader){name="32PPU浅水吸收与波纹",mainTexture=basin};
            material.SetTexture("_WaterPattern",pattern);material.SetVector("_WaterBounds",bounds);
            material.SetFloat("_WaterDepth",depth);material.SetFloat("_WaveStrength",.028f);
            material.SetFloat("_WaveSpeed",.7f);material.SetFloat("_Smoothness",.9f);
            material.SetColor("_DeepColor",new Color(.07f,.23f,.27f));
            return material;
        }
    }
}
