using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    // One candidate rendering contract for every catalog battlefield. Scene art
    // remains a separate review: a resolved profile is not aesthetic approval.
    public sealed class Hd2DBattlefieldProfile
    {
        public FirstRegionFloorTheme FloorTheme { get; }
        public bool WetStone { get; }
        public bool LocalReflection => true; // Current common environment contains a water garden.
        public string GroundShader => WetStone ? "OCC/HD2D/WetCourtyard" : "OCC/HD2D/BakedArchitecture";
        public float MainIntensity => 2.6f;
        public float FillIntensity => .6f;
        public float Exposure => .08f;
        public float CrystalIntensity => 1.8f;
        public float CrystalRange => 2.3f;
        public Color CrystalColour => new Color(.36f,.67f,1f);
        private Hd2DBattlefieldProfile(FirstRegionLevelDefinition level)
        {
            FloorTheme=level?.FloorTheme ?? FirstRegionFloorTheme.Courtyard;
            // Explicit weather overrides, not inferred from water or blue artwork.
            WetStone=level!=null && (level.Id==FirstRegionLevelCatalog.RainLanternCourt.Id ||
                level.Id==FirstRegionLevelCatalog.RainPrismCourt.Id);
        }
        public static Hd2DBattlefieldProfile Resolve(string levelId)
        {
            // Marker-based developer/training scenes have no catalog level.
            if(levelId==null)return new Hd2DBattlefieldProfile(null);
            var level=FirstRegionLevelCatalog.For(levelId);
            if(!Enum.IsDefined(typeof(FirstRegionFloorTheme),level.FloorTheme))
                throw new InvalidOperationException("Unconfigured HD2D floor theme: "+level.FloorTheme);
            return new Hd2DBattlefieldProfile(level);
        }
        public static IReadOnlyList<FirstRegionLevelDefinition> Levels => FirstRegionLevelCatalog.All
            .Concat(new[]{FirstRegionLevelCatalog.RainLanternCourt,FirstRegionLevelCatalog.GreenhouseCollectionRoom,
                FirstRegionLevelCatalog.RainPrismCourt,FirstRegionLevelCatalog.ThreeMaterialPressure})
            .Concat(FirstRegionLevelCatalog.NewEliteLevels).GroupBy(l=>l.Id).Select(g=>g.First()).ToArray();
    }
}
