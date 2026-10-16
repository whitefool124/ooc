using System;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    // Candidate numeric fields registered only to this exact albedo; never infer
    // normal/roughness from colour or apply them to a different stone variant.
    public static class Hd2DCourtyardStoneMaps
    {
        public const string AlbedoResource="Art/FormalAcademyIndependentFloors32/academy_block_court_b";
        public const string NormalResource="HD2DMaterialData/CourtyardStone/normal";
        public const string RoughnessResource="HD2DMaterialData/CourtyardStone/roughness";
        public const float NormalStrength=.35f;
        public static bool Apply(Material material,Texture sourceAlbedo)
        {
            if(sourceAlbedo!=Resources.Load<Texture2D>(AlbedoResource))return false;
            if(!material.HasProperty("_BumpMap") || !material.HasProperty("_RoughnessMap"))
                throw new InvalidOperationException("Stone shader lacks paired material inputs");
            var normal=Resources.Load<Texture2D>(NormalResource);
            var roughness=Resources.Load<Texture2D>(RoughnessResource);
            foreach(var map in new[]{normal,roughness})
                if(map==null || map.width!=32 || map.height!=32 || map.filterMode!=FilterMode.Point)
                    throw new InvalidOperationException("Invalid registered native32 stone data");
            material.SetTexture("_BumpMap",normal);material.SetFloat("_BumpScale",NormalStrength);
            material.SetTexture("_RoughnessMap",roughness);
            material.EnableKeyword("_NORMALMAP");material.EnableKeyword("_OCC_ROUGHNESS_MAP");
            return true;
        }
    }
}
