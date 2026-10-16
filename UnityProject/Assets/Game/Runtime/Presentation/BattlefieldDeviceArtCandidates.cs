#if UNITY_EDITOR
using UnityEngine;

namespace OCC.Combat.Presentation
{
    // New artwork remains explicitly opt-in until formal asset admission.
    public static class BattlefieldDeviceArtCandidates
    {
        public static bool ReviewEnabled {get;set;}
        public static Texture2D Resolve(string id)
        {
            if(!ReviewEnabled||(id!="device_overload"&&id!="device_ward_generator"&&id!="device_tower_mechanism"&&id!="material_crystal_shards"))return null;
            var texture=Resources.Load<Texture2D>(BattlefieldArtResources.Devices+id);
            if(texture==null)throw new System.InvalidOperationException("Missing device/material QA source: "+id);
            return texture;
        }
    }
}
#endif
