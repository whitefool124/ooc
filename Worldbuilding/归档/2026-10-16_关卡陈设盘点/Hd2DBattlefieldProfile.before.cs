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
        public string SurroundOuterResource=>"Art/FormalAcademyIndependentFloors32/academy_block_"+
            (Environment==Hd2DEnvironmentKind.InteriorHall||Environment==Hd2DEnvironmentKind.AetherTower?"road":"earth")+"_b";
        public float SurroundApronWidth=>Environment==Hd2DEnvironmentKind.RuinSite?1.5f:2.5f;
        public bool WetStone { get; }
        public bool OutdoorClouds => Environment==Hd2DEnvironmentKind.AcademyCourt||
            Environment==Hd2DEnvironmentKind.StoneRoad||Environment==Hd2DEnvironmentKind.RuinSite;
        public float CloudOpacity => .12f;
        public Vector2 CloudVelocity => new Vector2(.035f,.018f); // Light-plane world metres per second.
        public bool LocalReflection => true; // Materials may use the scene-owned reflection capture in every family.
        public string GroundShader => WetStone ? "OCC/HD2D/WetCourtyard" : "OCC/HD2D/BakedArchitecture";
        public float MainIntensity => Environment==Hd2DEnvironmentKind.InteriorHall?1.75f:
            Environment==Hd2DEnvironmentKind.AetherTower?2.15f:
            Environment==Hd2DEnvironmentKind.Conservatory?2.20f:2.30f;
        // Broad sky fill stays subordinate to key and standing-card bounce.
        // Uniform fill made perimeter paving compete with the active board.
        public float FillIntensity => Environment==Hd2DEnvironmentKind.Conservatory?.28f:
            Environment==Hd2DEnvironmentKind.InteriorHall?.36f:.30f;
        public Color MainColour => Environment==Hd2DEnvironmentKind.AetherTower?new Color(.84f,.91f,1f):
            Environment==Hd2DEnvironmentKind.InteriorHall?new Color(1f,.87f,.7f):
            Environment==Hd2DEnvironmentKind.Conservatory?new Color(1f,.97f,.88f):
            Environment==Hd2DEnvironmentKind.RuinSite?new Color(1f,.90f,.76f):
            Environment==Hd2DEnvironmentKind.StoneRoad?new Color(.94f,.96f,1f):new Color(1f,.94f,.84f);
        public Color FillColour => Environment==Hd2DEnvironmentKind.InteriorHall?new Color(.86f,.85f,.90f):
            Environment==Hd2DEnvironmentKind.Conservatory?new Color(.80f,.89f,.87f):
            Environment==Hd2DEnvironmentKind.RuinSite?new Color(.79f,.83f,.91f):new Color(.78f,.85f,.97f);
        public Vector3 MainEuler=>new Vector3(42,65,0);
        // Shared times of day preserve each environment's illumination role.
        public Color NightMainColour=>Environment==Hd2DEnvironmentKind.AetherTower?new Color(.55f,.72f,1f):
            Environment==Hd2DEnvironmentKind.InteriorHall?new Color(.72f,.77f,.89f):
            Environment==Hd2DEnvironmentKind.Conservatory?new Color(.66f,.82f,.96f):
            Environment==Hd2DEnvironmentKind.RuinSite?new Color(.68f,.76f,.93f):
            Environment==Hd2DEnvironmentKind.StoneRoad?new Color(.60f,.73f,1f):new Color(.64f,.78f,1f);
        public Color SunsetMainColour=>Environment==Hd2DEnvironmentKind.Conservatory||Environment==Hd2DEnvironmentKind.InteriorHall?new Color(1f,.80f,.58f):
            Environment==Hd2DEnvironmentKind.RuinSite?new Color(1f,.69f,.43f):
            Environment==Hd2DEnvironmentKind.AetherTower?new Color(.94f,.78f,.66f):
            Environment==Hd2DEnvironmentKind.StoneRoad?new Color(1f,.77f,.57f):new Color(1f,.74f,.48f);
        public Color NightSkyRadiance=>NightMainColour.linear*.13f*(MainIntensity/2.3f);
        public Color NightGroundRadiance=>NightMainColour.linear*.052f*(MainIntensity/2.3f);
        // A shallow front fill reaches upright cards more than horizontal paving.
        // Its direction is independent of sunlight and never adds a second shadow.
        public Vector3 FillEuler=>new Vector3(20,-15,0);
        // Horizontal bounce illuminates standing cards while leaving flat paving dim.
        public Vector3 ReadabilityFillEuler=>new Vector3(0,20,0);
        public float ReadabilityFillIntensity=>Environment==Hd2DEnvironmentKind.InteriorHall?.45f:.55f;
        public Color ReadabilityFillColour=>new Color(.86f,.87f,.90f);
        // Linear scene-owned irradiance, independent of the host scene's skybox.
        // Lower hemisphere stays dim; key/fill lights retain the main value hierarchy.
        public Color AmbientSkyRadiance=>FillColour.linear*(Environment==Hd2DEnvironmentKind.InteriorHall?.14f:.18f);
        public Color AmbientGroundRadiance=>MainColour.linear*.05f;
        public Color AirColour => Environment==Hd2DEnvironmentKind.InteriorHall?new Color(.46f,.43f,.39f):
            Environment==Hd2DEnvironmentKind.AetherTower?new Color(.38f,.49f,.65f):
            Environment==Hd2DEnvironmentKind.Conservatory?new Color(.48f,.57f,.51f):new Color(.49f,.56f,.63f);
        public float AirDensity => Environment==Hd2DEnvironmentKind.InteriorHall?.035f:
            Environment==Hd2DEnvironmentKind.Conservatory?.055f:.075f;
        public float AirMaxOpacity => Environment==Hd2DEnvironmentKind.InteriorHall?.16f:.30f;
        public float PeripheralDimming => Environment==Hd2DEnvironmentKind.InteriorHall?.20f:.32f;
        // World-metre single-scattering candidate. Indoor/tower illumination
        // keeps its existing ambient haze without unmotivated sunlight shafts.
        public float AirScatterDensity => Environment==Hd2DEnvironmentKind.Conservatory?.009f:.014f;
        public float AirScatterStrength => OutdoorClouds?.28f:Environment==Hd2DEnvironmentKind.Conservatory?.16f:0f;
        public float AirHeightFalloff => .12f;
        public float AirRayLength => 18f;
        public float Exposure => 0f;
        public float CrystalIntensity => 1.8f;
        public float CrystalRange => 2.3f;
        public float CrystalNightIntensity => 2.7f;
        public float CrystalNightRange => 2.9f;
        public float CrystalLightHeight => .85f;
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
