using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace OCC.Combat.Presentation
{
    // One inspectable desk profile for services and both loadout sections.
    // Lights, transforms and intensities live in the prefab, not page branches.
    public sealed class Hd2DDeskLightingRig : MonoBehaviour
    {
        [SerializeField] private float postExposure = .25f;
        [SerializeField] private float contrast = 4f;
        [SerializeField] private float saturation = -4f;
        [SerializeField] private ReflectionProbe environmentProbe;
        public ReflectionProbe EnvironmentProbe => environmentProbe;
        public float PostExposure => postExposure;
        private Hd2DDeskReflectionRefresh reflection;
        internal Hd2DDeskReflectionRefresh RefreshEnvironment(MainMenuHd2DStage stage)
        {
            if(environmentProbe==null)return null;
            if(reflection==null)
            {
                reflection=environmentProbe.gameObject.AddComponent<Hd2DDeskReflectionRefresh>();
                reflection.Initialize(environmentProbe,stage.transform);
            }
            else reflection.RequestRefresh();
            return reflection;
        }

        internal void Apply(MainMenuHd2DStage stage)
        {
            // The shared room keeps its architecture; this rig owns illumination.
            foreach (var light in stage.GetComponentsInChildren<Light>(true))
                if (light.transform.parent == stage.transform&&light!=stage.lampLight) light.enabled = false;
            foreach (var light in GetComponentsInChildren<Light>(true))
                light.cullingMask = 1 << stage.gameObject.layer;
            if (environmentProbe != null) environmentProbe.cullingMask = 1 << stage.gameObject.layer;
            var profile = stage.sceneVolume.profile;
            if (!profile.TryGet<ColorAdjustments>(out var grade)) grade = profile.Add<ColorAdjustments>(true);
            grade.postExposure.Override(postExposure); grade.contrast.Override(contrast);
            grade.saturation.Override(saturation); grade.colorFilter.Override(Color.white);
            if(profile.TryGet<WhiteBalance>(out var balance))
            {balance.temperature.Override(0);balance.tint.Override(0);}
            if(profile.TryGet<SplitToning>(out var split))split.active=false;
            Hd2DDeskReadability.Current.ApplyBloom(stage);
            // Every page keeps its reading and interaction surfaces in focus.
            if (profile.TryGet<DepthOfField>(out var focus)) focus.active = false;
            if (profile.TryGet<Tonemapping>(out var tone)) tone.mode.Override(TonemappingMode.ACES);
            var contact=stage.GetComponent<Hd2DDeskContactLighting>();
            if(contact==null)contact=stage.gameObject.AddComponent<Hd2DDeskContactLighting>();
            contact.Initialize();
        }
    }
}
