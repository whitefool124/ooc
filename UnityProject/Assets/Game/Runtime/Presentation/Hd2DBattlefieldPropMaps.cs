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
            private readonly string albedoDirectory;
            private readonly string mapDirectory;
            public readonly float? UniformRoughness;
            public readonly float Metallic,ClearCoat;
            public bool HasRoughnessMap=>!UniformRoughness.HasValue;
            public string AlbedoResource=>albedoDirectory+"/"+Id;
            public bool CrystalRegions=>Id.StartsWith("academy_aether_crystal_")||Id.StartsWith("academy_pressure_crystal_");
            public bool HasRegionMap=>Id.StartsWith("academy_loot_chest_")||Id=="academy_decoy_lantern_active"||CrystalRegions;
            public string MapRoot=>mapDirectory+"/"+Id+"/";
            public float NormalStrength=>CrystalRegions?.55f:Id=="academy_decoy_lantern_active"?.2f:.28f;
            internal Entry(string id,string directory="Art/FormalAcademyCombat32",float? roughness=null,float metallic=0,float coat=0,string maps="HD2DMaterialData/Props")
            {Id=id;albedoDirectory=directory;mapDirectory=maps;UniformRoughness=roughness;Metallic=metallic;ClearCoat=coat;}
        }
        public static IReadOnlyList<Entry> Entries {get;}=new[]{
            new Entry("academy_heavy_archive_stack_intact"),new Entry("academy_heavy_archive_stack_damaged"),new Entry("academy_heavy_archive_stack_rubble"),
            new Entry("academy_loot_chest_closed"),new Entry("academy_loot_chest_open"),new Entry("academy_loot_chest_empty"),new Entry("academy_decoy_lantern_active"),
            new Entry("academy_heavy_training_barricade_intact","Art/AcademyCoverCandidates20260924"),
            new Entry("academy_light_book_crate_intact","Art/AcademyCoverCandidates20260924"),
            new Entry("academy_light_wood_bench_intact","Art/AcademyCoverCandidates20260924"),
            new Entry("academy_light_training_pad_intact","Art/AcademyCoverCandidates20260924"),
            new Entry("academy_light_planter_cover_intact","Art/AcademyCoverCandidates20260924"),
            new Entry("academy_light_stone_bench_damaged",roughness:.88f),
            new Entry("academy_light_stone_bench_rubble",roughness:.88f),
            new Entry("academy_aether_pillar_intact",roughness:.88f),
            new Entry("academy_aether_pillar_damaged",roughness:.88f),
            new Entry("academy_aether_pillar_rubble",roughness:.88f),
            new Entry("academy_light_planter_damaged",roughness:.9f),
            new Entry("academy_light_planter_rubble",roughness:.92f),
            new Entry("academy_heavy_masonry_screen_damaged",roughness:.88f),
            new Entry("academy_heavy_training_barricade_rubble","Art/AcademyCoverCandidates20260924",roughness:.9f),
            new Entry("lamp_vine","Art/FormalFirstBattle32",roughness:.92f,maps:"HD2DMaterialData/RemainingSprites"),
            new Entry("academy_scorched_lamp_vine",roughness:.98f,maps:"HD2DMaterialData/RemainingSprites"),
            new Entry("academy_aether_crystal_intact",roughness:.3f,maps:"HD2DMaterialData/RemainingSprites"),
            new Entry("academy_aether_crystal_damaged",roughness:.4f,maps:"HD2DMaterialData/RemainingSprites"),
            new Entry("academy_pressure_crystal_intact",roughness:.25f,maps:"HD2DMaterialData/RemainingSprites"),
            new Entry("academy_pressure_crystal_damaged",roughness:.35f,maps:"HD2DMaterialData/RemainingSprites"),
            new Entry("academy_aether_crystal_intact","Art/HD2DBattleCrystalCandidates",roughness:.25f,maps:"HD2DMaterialData/CrystalCandidates"),
            new Entry("academy_aether_crystal_damaged","Art/HD2DBattleCrystalCandidates",roughness:.4f,maps:"HD2DMaterialData/CrystalCandidates"),
            new Entry("academy_pressure_crystal_intact","Art/HD2DBattleCrystalCandidates",roughness:.2f,maps:"HD2DMaterialData/CrystalCandidates"),
            new Entry("academy_pressure_crystal_damaged","Art/HD2DBattleCrystalCandidates",roughness:.35f,maps:"HD2DMaterialData/CrystalCandidates")};
        public static bool Apply(Material material,Texture source)
        {
            if(source==null)return false;
            foreach(var entry in Entries)
            {
                if(source!=Resources.Load<Texture2D>(entry.AlbedoResource))continue;
                if(material==null||!material.HasProperty("_BumpMap")||!material.HasProperty("_RoughnessMap"))
                    throw new InvalidOperationException("Missing prop material inputs: "+entry.Id);
                foreach(var pair in new[]{("normal","_BumpMap"),("roughness","_RoughnessMap"),("regions","_MaterialMask")})
                {
                    if(pair.Item1=="roughness"&&!entry.HasRoughnessMap){material.SetTexture(pair.Item2,Texture2D.whiteTexture);continue;}
                    if(pair.Item1=="regions" && !entry.HasRegionMap){if(material.HasProperty(pair.Item2))material.SetTexture(pair.Item2,Texture2D.blackTexture);continue;}
                    if(!material.HasProperty(pair.Item2))throw new InvalidOperationException("Missing region input: "+entry.Id);
                    var map=Resources.Load<Texture2D>(entry.MapRoot+pair.Item1);
                    if(map==null||map.width!=source.width||map.height!=source.height||map.filterMode!=FilterMode.Point)
                        throw new InvalidOperationException("Invalid paired prop field: "+entry.Id+"/"+pair.Item1);
                    material.SetTexture(pair.Item2,map);
                }
                material.SetFloat("_BumpScale",entry.NormalStrength);material.SetFloat("_Metallic",entry.Metallic);
                if(entry.UniformRoughness.HasValue)material.SetFloat("_Smoothness",1f-entry.UniformRoughness.Value);
                if(entry.HasRoughnessMap)material.DisableKeyword("_OCC_UNIFORM_SURFACE");else material.EnableKeyword("_OCC_UNIFORM_SURFACE");
                if(entry.CrystalRegions)
                {material.EnableKeyword("_OCC_CRYSTAL_REGIONS");material.SetColor("_EmissionColor",new Color(.25f,.55f,.95f)*.45f);}
                if(material.HasProperty("_ClearCoatSmoothness"))material.SetFloat("_ClearCoatSmoothness",.94f);
                if(material.HasProperty("_ClearCoatMask"))material.SetFloat("_ClearCoatMask",entry.ClearCoat);
                material.EnableKeyword("_NORMALMAP");
                if(material.HasProperty("_MaterialMask"))material.EnableKeyword("_OCC_PROP_DATA");
                else if(entry.HasRoughnessMap)material.EnableKeyword("_OCC_ROUGHNESS_MAP");
                return true;
            }
            return false;
        }
    }
}
