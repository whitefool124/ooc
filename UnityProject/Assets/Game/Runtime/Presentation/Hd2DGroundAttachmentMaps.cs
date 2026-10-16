using System;
using System.Collections.Generic;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    // Exact albedo identity; no name/tint-based material inference at runtime.
    // Candidate numerical fields are baked from authored structure/region data.
    public static class Hd2DGroundAttachmentMaps
    {
        public sealed class Entry
        {
            public readonly string Id;
            public string AlbedoResource=>"Art/FormalAcademyStructures32/"+Id;
            public string MapRoot=>"HD2DMaterialData/GroundAttachments/"+Id+"/";
            internal Texture2D Albedo,Normal,Surface;
            internal Entry(string name){Id="academy_floor_"+name;}
        }
        public static IReadOnlyList<Entry> Entries {get;}=new[]{
            new Entry("drain_round"),new Entry("drain_slot"),new Entry("service_hatch_round"),new Entry("service_hatch_square"),
            new Entry("repair_stone"),new Entry("repair_iron"),new Entry("cable_cap"),new Entry("pipe_socket"),
            new Entry("anchor_plate"),new Entry("inspection_window"),new Entry("mortar_inlay"),new Entry("threshold_studs"),
            new Entry("safety_marker"),new Entry("herb_drain"),new Entry("rain_channel"),new Entry("conduit_blank"),
            new Entry("convergence_scribe"),new Entry("drain_grate"),new Entry("maintenance_hatch"),new Entry("repair_plate")};
        public static bool Apply(Material material,Texture source)
        {
            if(source==null)return false;
            foreach(var entry in Entries)
            {
                if(entry.Albedo==null)entry.Albedo=Resources.Load<Texture2D>(entry.AlbedoResource);
                if(source!=entry.Albedo)continue;
                if(material==null||!material.HasProperty("_BumpMap")||!material.HasProperty("_GroundSurfaceData"))
                    throw new InvalidOperationException("Ground attachment shader inputs missing: "+entry.Id);
                if(entry.Normal==null)entry.Normal=Resources.Load<Texture2D>(entry.MapRoot+"normal");
                if(entry.Surface==null)entry.Surface=Resources.Load<Texture2D>(entry.MapRoot+"surface");
                foreach(var field in new[]{entry.Normal,entry.Surface})
                    if(field==null||field.width!=32||field.height!=32||field.filterMode!=FilterMode.Point)
                        throw new InvalidOperationException("Ground attachment registered field invalid: "+entry.Id);
                material.SetTexture("_BumpMap",entry.Normal);material.SetFloat("_BumpScale",.85f);
                material.SetTexture("_GroundSurfaceData",entry.Surface);
                material.EnableKeyword("_NORMALMAP");material.EnableKeyword("_OCC_GROUND_DATA");
                material.DisableKeyword("_OCC_ROUGHNESS_MAP");
                return true;
            }
            return false;
        }
    }
}
