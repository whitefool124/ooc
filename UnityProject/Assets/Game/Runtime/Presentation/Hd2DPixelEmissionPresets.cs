using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace OCC.Combat.Presentation
{
    public static class Hd2DPixelEmissionPresets
    {
        internal static void ConfigureStructuralShadow(Light light)
        {
            light.shadows=LightShadows.Soft;light.shadowStrength=.85f;
            light.shadowBias=.035f;light.shadowNormalBias=.035f;light.shadowNearPlane=.05f;
            var data=light.GetUniversalAdditionalLightData();
            data.customShadowLayers=true;data.shadowRenderingLayers=2u;
            data.softShadowQuality=SoftShadowQuality.Low;
            light.shadowCustomResolution=256;
            data.additionalLightsShadowResolutionTier=UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierCustom;
        }
        private static Texture2D staffSource;
        private static Texture2D crystalSource;
        public static void ApplyCrystal(MeshRenderer body,Texture2D source)
        {
            if(crystalSource==null)crystalSource=Resources.Load<Texture2D>("Art/FormalAcademyCombat32/academy_aether_crystal_intact");
            if(source!=crystalSource||body.GetComponent<Hd2DPixelEmission>()!=null)return;
            var emitter=body.gameObject.AddComponent<Hd2DPixelEmission>();
            emitter.Initialize(body,source,CrystalFacets(source));
            // The tile already owns the crystal's environment light. A second
            // coincident point flattened nearby stone into a bright cyan pool.
        }
        public static Hd2DPixelEmission.Pixel[] CrystalFacets(Texture2D source)
        {
            if(source.width!=96||source.height!=64)throw new System.InvalidOperationException("Registered crystal source changed");
            // Manually traced native facet boundaries, using top-origin source
            // coordinates for audit. This is energy data, not diffuse recolouring.
            var pixels=new List<Hd2DPixelEmission.Pixel>();
            Facet(new[]{new Vector2(38,20),new Vector2(40,21),new Vector2(40,36),new Vector2(37,32),
                new Vector2(36,30),new Vector2(34,28),new Vector2(33,26),new Vector2(35,25),new Vector2(36,23)},Vector3.one*8f);
            Facet(new[]{new Vector2(21,31),new Vector2(22,31),new Vector2(23,33),new Vector2(24,35),
                new Vector2(24,39),new Vector2(25,40),new Vector2(25,44),new Vector2(23,43),
                new Vector2(22,40),new Vector2(20,37),new Vector2(20,34)},Vector3.one*5f);
            Facet(new[]{new Vector2(67,32),new Vector2(68,34),new Vector2(68,36),new Vector2(67,38),
                new Vector2(65,39),new Vector2(64,40),new Vector2(63,40),new Vector2(62,39),
                new Vector2(64,37),new Vector2(65,36),new Vector2(66,33)},Vector3.one*6.5f);
            return pixels.ToArray();
            void Facet(Vector2[] polygon,Vector3 radiance)
            {
                for(int y=0;y<source.height;y++)for(int x=0;x<source.width;x++)
                {
                    var p=new Vector2(x+.5f,y+.5f);bool inside=false;
                    for(int i=0,j=polygon.Length-1;i<polygon.Length;j=i++)
                    {
                        var a=polygon[i];var b=polygon[j];
                        if((a.y>p.y)!=(b.y>p.y)&&p.x<(b.x-a.x)*(p.y-a.y)/(b.y-a.y)+a.x)inside=!inside;
                    }
                    if(inside)pixels.Add(new Hd2DPixelEmission.Pixel(x,source.height-1-y,radiance));
                }
            }
        }
        // Explicit source rectangle for the already painted flame at the staff tip.
        // Original shader alpha clips the declared region; no CPU texture read
        // or importer change is needed, and diffuse colour selects no pixels.
        public static Hd2DPixelEmission.Pixel[] Cluster(Texture2D source,RectInt region,Vector3 radiance)
        {
            var pixels=new List<Hd2DPixelEmission.Pixel>();
            for(int y=region.yMin;y<region.yMax;y++)for(int x=region.xMin;x<region.xMax;x++)
                if(x>=0&&y>=0&&x<source.width&&y<source.height)
                    pixels.Add(new Hd2DPixelEmission.Pixel(x,y,radiance));
            return pixels.ToArray();
        }
        public static void ApplyStaff(MeshRenderer body,Texture2D source)
        {
            if(staffSource==null)staffSource=Resources.Load<Texture2D>("Art/FormalUnits64/pyromancer");
            if(source!=staffSource||body.GetComponent<Hd2DPixelEmission>()!=null)return;
            if(source.width!=32||source.height!=64)throw new System.InvalidOperationException("Registered staff source changed");
            var emitter=body.gameObject.AddComponent<Hd2DPixelEmission>();
            emitter.Initialize(body,source,Cluster(source,new RectInt(23,31,6,6),new Vector3(5f,1.4f,.035f)));
            var light=emitter.AddLightGroup(new Vector3((26f-16f)/32f,34f/32f,-.12f),new Color(1f,.48f,.08f),1.2f,1.9f);
            ConfigureStructuralShadow(light);
            var atmosphere=body.GetComponentInParent<Hd2DBattleAtmosphere>();
            if(atmosphere!=null)atmosphere.RegisterLocalLight(light,1.8f,2.4f);
        }
    }
}
