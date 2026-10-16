using System;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    // Registered numeric geometry fields; never derive height from diffuse colour.
    public static class Hd2DEnvironmentMaterialMaps
    {
        private static readonly string[] sources={"Art/HD2DAcademyReview/academy_facade","Art/HD2DAcademyReview/academy_tree","Art/HD2DAcademyCandidates/greenhouse_facade"};
        public static bool Apply(Material material,Texture source)
        {
            if(source==null||material==null)return false;
            foreach(string path in sources)
            {
                if(source!=Resources.Load<Texture2D>(path))continue;
                string id=path.Substring(path.LastIndexOf('/')+1);
                string root="HD2DMaterialData/RemainingSprites/"+id+"/";
                var normal=Resources.Load<Texture2D>(root+"normal");
                var surface=Resources.Load<Texture2D>(root+"metallic_gloss");
                if(normal==null||surface==null||normal.width!=source.width||normal.height!=source.height||surface.width!=source.width||surface.height!=source.height)
                    throw new InvalidOperationException("Unregistered environment material fields: "+path);
                if(!material.HasProperty("_BumpMap"))throw new InvalidOperationException("Environment shader needs registered normal input");
                material.SetTexture("_BumpMap",normal);material.SetFloat("_BumpScale",.32f);material.EnableKeyword("_NORMALMAP");
                if(!material.HasProperty("_MetallicGlossMap"))
                    throw new InvalidOperationException("Environment shader needs registered surface input: "+id);
                // Registered environment R/A fields and prop R/G/B fields have
                // separate meanings, including after switching a card's shader.
                material.DisableKeyword("_OCC_PROP_DATA");material.DisableKeyword("_OCC_UNIFORM_SURFACE");
                material.DisableKeyword("_OCC_CRYSTAL_REGIONS");material.DisableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
                material.SetTexture("_MetallicGlossMap",surface);material.SetFloat("_Smoothness",1f);
                material.SetFloat("_Metallic",0);material.SetFloat("_SmoothnessTextureChannel",0);
                material.EnableKeyword("_METALLICSPECGLOSSMAP");
                return true;
            }
            return false;
        }
    }
}
