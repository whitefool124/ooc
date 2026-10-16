using System;
using System.Collections.Generic;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    // Alpha and silhouette come exclusively from the admitted albedo. The
    // opaque numeric fields use the exact same canvas/UV and never infer type
    // from a colour, tint or name on an unrelated asset.
    public static class Hd2DBattlefieldPropMaps
    {
        public sealed class Entry
        {
            public readonly string Id;
            public string AlbedoResource=>"Art/FormalAcademyCombat32/"+Id;
            public string MapRoot=>"HD2DMaterialData/Props/"+Id+"/";
            public float NormalStrength=>Id=="academy_decoy_lantern_active"?.2f:.28f;
            internal Entry(string id){Id=id;}
        }
        public static IReadOnlyList<Entry> Entries {get;}=new[]{
            new Entry("academy_heavy_archive_stack_intact"),new Entry("academy_heavy_archive_stack_damaged"),new Entry("academy_heavy_archive_stack_rubble"),
            new Entry("academy_loot_chest_closed"),new Entry("academy_loot_chest_open"),new Entry("academy_loot_chest_empty"),new Entry("academy_decoy_lantern_active")};
        public static bool Apply(Material material,Texture source)
        {
            if(source==null)return false;
            foreach(var entry in Entries)
            {
                if(source!=Resources.Load<Texture2D>(entry.AlbedoResource))continue;
                if(material==null||!material.HasProperty("_BumpMap")||!material.HasProperty("_RoughnessMap")||!material.HasProperty("_MaterialMask"))
                    throw new InvalidOperationException("Missing prop material inputs: "+entry.Id);
                foreach(var pair in new[]{("normal","_BumpMap"),("roughness","_RoughnessMap"),("regions","_MaterialMask")})
                {
                    if(pair.Item1=="regions" && entry.Id.StartsWith("academy_heavy_archive_stack_")){material.SetTexture(pair.Item2,Texture2D.blackTexture);continue;}
                    var map=Resources.Load<Texture2D>(entry.MapRoot+pair.Item1);
                    if(map==null||map.width!=source.width||map.height!=source.height||map.filterMode!=FilterMode.Point)
                        throw new InvalidOperationException("Invalid paired prop field: "+entry.Id+"/"+pair.Item1);
                    material.SetTexture(pair.Item2,map);
                }
                material.SetFloat("_BumpScale",entry.NormalStrength);material.SetFloat("_ClearCoatSmoothness",.94f);
                material.EnableKeyword("_NORMALMAP");material.EnableKeyword("_OCC_PROP_DATA");return true;
            }
            return false;
        }
    }
}
