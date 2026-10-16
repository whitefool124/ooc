#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    // Explicit Editor review only. QA artwork never replaces the release lookup.
    public static class EnemyUnitArtCandidates
    {
        public static bool ReviewEnabled {get;set;}
        private static readonly HashSet<string> UnitIds=new HashSet<string>
        {"signal_keeper","elder_tracker_hound","wind_librarian","legacy_storekeeper"};
        public static Texture2D Resolve(string unitId)
        {
            if(!ReviewEnabled||unitId==null||!UnitIds.Contains(unitId))return null;
            // Folder name predates the adaptive four-legged canvas; every image
            // retains its own dimensions at the same native 32 PPU.
            var texture=Resources.Load<Texture2D>("Art/FormalUnits64/"+unitId);
            if(texture==null)throw new System.InvalidOperationException("Missing QA battle sprite: "+unitId);
            return texture;
        }
    }
}
#endif
