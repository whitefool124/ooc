using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    public enum Hd2DEnvironmentKind { AcademyCourt, Conservatory, StoneRoad, RuinSite, InteriorHall, AetherTower }
    // One candidate rendering contract for every catalog battlefield. Scene art
    // remains a separate review: a resolved profile is not aesthetic approval.
    public sealed class Hd2DBattlefieldProfile
    {
        public FirstRegionFloorTheme FloorTheme { get; }
        public Hd2DEnvironmentKind Environment { get; }
        public bool WaterGarden => Environment==Hd2DEnvironmentKind.AcademyCourt;
        public bool Trees => Environment==Hd2DEnvironmentKind.AcademyCourt || Environment==Hd2DEnvironmentKind.Conservatory || Environment==Hd2DEnvironmentKind.StoneRoad;
        public string MasonryResource => "Art/FormalAcademyIndependentFloors32/academy_block_"+
            (Environment==Hd2DEnvironmentKind.RuinSite?"ruin":Environment==Hd2DEnvironmentKind.StoneRoad?"road":"court")+"_b";
        public bool WetStone { get; }
        public bool LocalReflection => true; // Materials may use the scene-owned reflection capture in every family.
        public string GroundShader => WetStone ? "OCC/HD2D/WetCourtyard" : "OCC/HD2D/BakedArchitecture";
        public float MainIntensity => Environment==Hd2DEnvironmentKind.InteriorHall?1.9f:
            Environment==Hd2DEnvironmentKind.AetherTower?2.35f:
            Environment==Hd2DEnvironmentKind.Conservatory?2.7f:3.1f;
        public float FillIntensity => Environment==Hd2DEnvironmentKind.Conservatory?.5f:
            Environment==Hd2DEnvironmentKind.InteriorHall?.3f:.35f;
        public Color MainColour => Environment==Hd2DEnvironmentKind.AetherTower?new Color(.84f,.91f,1f):
            Environment==Hd2DEnvironmentKind.InteriorHall?new Color(1f,.87f,.7f):new Color(1f,.94f,.84f);
        public Color FillColour => Environment==Hd2DEnvironmentKind.InteriorHall?new Color(.8f,.84f,.92f):new Color(.7f,.82f,1f);
        public Vector3 MainEuler=>new Vector3(55,65,0);
        public float Exposure => .08f;
        public float CrystalIntensity => 1.8f;
        public float CrystalRange => 2.3f;
        public Color CrystalColour => new Color(.36f,.67f,1f);
        private Hd2DBattlefieldProfile(FirstRegionLevelDefinition level)
        {
            FloorTheme=level?.FloorTheme ?? FirstRegionFloorTheme.Courtyard;
            if(level==null)Environment=Hd2DEnvironmentKind.AcademyCourt;
            else if(level.Id==FirstRegionLevelCatalog.GreenhouseCollectionRoom.Id)Environment=Hd2DEnvironmentKind.Conservatory;
            else if(level.Id=="library_discipline" || level.Id=="calibration_lockdown" || level.Id=="sealed_vault_certification")Environment=Hd2DEnvironmentKind.InteriorHall;
            else if(level.IsBoss || level.Id=="transmission_tower" || level.Id=="outer_ring_clearance")Environment=Hd2DEnvironmentKind.AetherTower;
            else if(FloorTheme==FirstRegionFloorTheme.StoneRoad)Environment=Hd2DEnvironmentKind.StoneRoad;
            else if(FloorTheme==FirstRegionFloorTheme.Ruins)Environment=Hd2DEnvironmentKind.RuinSite;
            else Environment=Hd2DEnvironmentKind.AcademyCourt;
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
