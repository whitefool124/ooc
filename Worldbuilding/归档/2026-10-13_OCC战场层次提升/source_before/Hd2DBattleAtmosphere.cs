using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OCC.Combat.Presentation
{
    public enum Hd2DBattleTimeOfDay { SceneDefault, Sunset, Night }
    public enum Hd2DBattleWeather { Clear, Rain }

    // Scene-owned presentation only. No simulation clock, combat state or save changes.
    public sealed class Hd2DBattleAtmosphere : MonoBehaviour
    {
        private Camera camera;
        private Light sun,fill;
        private ReflectionProbe[] probes;
        private ColorAdjustments grade;
        private SplitToning split;
        private Bloom bloom;
        private Hd2DWeatherRainPreview rain;
        private int width,height;
        private Action invalidateLighting;
        private Action<float> applyWetness;
        private State defaults,current,from,target;
        private float elapsed,duration;
        private bool initialized,transitioning;
        private readonly List<LocalLight> localLights=new List<LocalLight>();
        public Hd2DBattleTimeOfDay TimeOfDay {get;private set;}
        public Hd2DBattleWeather Weather {get;private set;}
        public bool IsTransitioning=>transitioning;
        public float RainAmount=>current.rain;

        private struct State
        {
            internal Color sunColour,fillColour,background,probeColour,shadows,highlights,bloomTint;
            internal Quaternion sunRotation;
            internal float sunIntensity,fillIntensity,shadowStrength,probeIntensity,exposure,contrast,saturation,bloomIntensity,bloomThreshold,night,rain;
            internal static State Blend(State a,State b,float t)=>new State{
                sunColour=Color.Lerp(a.sunColour,b.sunColour,t),fillColour=Color.Lerp(a.fillColour,b.fillColour,t),background=Color.Lerp(a.background,b.background,t),
                probeColour=Color.Lerp(a.probeColour,b.probeColour,t),shadows=Color.Lerp(a.shadows,b.shadows,t),highlights=Color.Lerp(a.highlights,b.highlights,t),bloomTint=Color.Lerp(a.bloomTint,b.bloomTint,t),
                sunRotation=Quaternion.Slerp(a.sunRotation,b.sunRotation,t),sunIntensity=Mathf.Lerp(a.sunIntensity,b.sunIntensity,t),fillIntensity=Mathf.Lerp(a.fillIntensity,b.fillIntensity,t),
                shadowStrength=Mathf.Lerp(a.shadowStrength,b.shadowStrength,t),probeIntensity=Mathf.Lerp(a.probeIntensity,b.probeIntensity,t),exposure=Mathf.Lerp(a.exposure,b.exposure,t),
                contrast=Mathf.Lerp(a.contrast,b.contrast,t),saturation=Mathf.Lerp(a.saturation,b.saturation,t),bloomIntensity=Mathf.Lerp(a.bloomIntensity,b.bloomIntensity,t),
                bloomThreshold=Mathf.Lerp(a.bloomThreshold,b.bloomThreshold,t),night=Mathf.Lerp(a.night,b.night,t),rain=Mathf.Lerp(a.rain,b.rain,t)};
        }
        private sealed class LocalLight
        {
            internal Light light;
            internal float intensity,range;
        }

        internal void Initialize(Camera view,int mapWidth,int mapHeight,Action invalidate,Action<float> wetness)
        {
            if(initialized)throw new InvalidOperationException("Atmosphere already initialized");
            camera=view;width=mapWidth;height=mapHeight;invalidateLighting=invalidate;applyWetness=wetness;
            foreach(var light in GetComponentsInChildren<Light>(true))
            {if(light.name=="战场统一日光")sun=light;else if(light.name=="战场柔和补光")fill=light;}
            if(camera==null||sun==null||fill==null)throw new InvalidOperationException("Atmosphere requires the owned battle camera/key/fill lights");
            probes=GetComponentsInChildren<ReflectionProbe>(true);
            var volume=GetComponentInChildren<Volume>();
            if(volume!=null&&volume.sharedProfile!=null)
            {volume.sharedProfile.TryGet(out grade);volume.sharedProfile.TryGet(out split);volume.sharedProfile.TryGet(out bloom);}
            defaults=new State{
                sunColour=sun.color,fillColour=fill.color,background=camera.backgroundColor,sunRotation=sun.transform.localRotation,
                sunIntensity=sun.intensity,fillIntensity=fill.intensity,shadowStrength=sun.shadowStrength,
                probeColour=probes.Length>0?probes[0].backgroundColor:Color.black,probeIntensity=probes.Length>0?probes[0].intensity:0,
                exposure=grade!=null?grade.postExposure.value:0,contrast=grade!=null?grade.contrast.value:0,saturation=grade!=null?grade.saturation.value:0,
                shadows=split!=null?split.shadows.value:Color.gray,highlights=split!=null?split.highlights.value:Color.gray,
                bloomTint=bloom!=null?bloom.tint.value:Color.white,bloomIntensity=bloom!=null?bloom.intensity.value:0,bloomThreshold=bloom!=null?bloom.threshold.value:1};
            current=defaults;initialized=true;
        }

        public void Set(Hd2DBattleTimeOfDay timeOfDay,Hd2DBattleWeather weather,float transitionSeconds=.8f)
        {
            if(!initialized)throw new InvalidOperationException("Battle atmosphere is not initialized");
            if(!Enum.IsDefined(typeof(Hd2DBattleTimeOfDay),timeOfDay)||!Enum.IsDefined(typeof(Hd2DBattleWeather),weather))throw new ArgumentOutOfRangeException("Unknown atmosphere preset");
            if(float.IsNaN(transitionSeconds)||float.IsInfinity(transitionSeconds)||transitionSeconds<0)throw new ArgumentOutOfRangeException(nameof(transitionSeconds));
            // A caller may publish its current setting every frame. Do not keep
            // restarting the same fade; an explicit instant request can finish it.
            if(timeOfDay==TimeOfDay&&weather==Weather&&(!transitioning||transitionSeconds>0))return;
            TimeOfDay=timeOfDay;Weather=weather;from=current;target=Preset(timeOfDay);target.rain=weather==Hd2DBattleWeather.Rain?1:0;
            if(target.rain>0&&rain==null)
            {
                var go=new GameObject("战场空间雨幕"){layer=gameObject.layer};go.transform.SetParent(transform,false);
                rain=go.AddComponent<Hd2DWeatherRainPreview>();rain.BuildRain(width,height,camera);rain.SetAmount(current.rain);
            }
            elapsed=0;duration=transitionSeconds;transitioning=duration>0;
            if(!transitioning){current=target;Apply(current);invalidateLighting?.Invoke();}
        }
        internal void RegisterLocalLight(Light light)
        {
            if(light==null)return;
            foreach(var registered in localLights)if(registered.light==light)return;
            var entry=new LocalLight{light=light,intensity=light.intensity,range=light.range};localLights.Add(entry);ApplyLocal(entry,current.night);
        }
        private State Preset(Hd2DBattleTimeOfDay time)
        {
            var result=defaults;if(time==Hd2DBattleTimeOfDay.SceneDefault)return result;
            bool night=time==Hd2DBattleTimeOfDay.Night;
            result.sunRotation=Quaternion.Euler(night?48:22,night?125:65,0);
            result.sunColour=night?new Color(.64f,.78f,1f):new Color(1f,.68f,.40f);result.sunIntensity=night?1.4f:3.4f;result.shadowStrength=night?.7f:.9f;
            result.fillColour=night?new Color(.29f,.43f,.72f):new Color(.56f,.67f,1f);result.fillIntensity=.48f;
            result.background=night?new Color(.018f,.033f,.075f):new Color(.20f,.105f,.07f);
            result.probeColour=night?new Color(.045f,.075f,.14f):new Color(.27f,.20f,.17f);result.probeIntensity=night?.65f:.8f;
            result.exposure=night?.18f:.12f;result.saturation=night?-4f:6f;result.contrast=night?10f:9f;
            result.shadows=night?new Color(.40f,.45f,.57f):new Color(.44f,.46f,.56f);result.highlights=night?new Color(.48f,.53f,.60f):new Color(.64f,.53f,.43f);
            result.bloomIntensity=night?.34f:.18f;result.bloomThreshold=night?1.05f:1.25f;result.bloomTint=night?new Color(.7f,.85f,1f):new Color(1f,.83f,.60f);result.night=night?1:0;
            return result;
        }
        private void Update()
        {
            if(!transitioning)return;
            elapsed+=Time.unscaledDeltaTime;float t=Mathf.Clamp01(elapsed/duration);current=State.Blend(from,target,t*t*(3-2*t));Apply(current);
            if(t>=1){transitioning=false;invalidateLighting?.Invoke();}
        }
        private static void ApplyLocal(LocalLight entry,float night)
        {if(entry.light!=null){entry.light.intensity=Mathf.Lerp(entry.intensity,3.8f,night);entry.light.range=Mathf.Lerp(entry.range,3.2f,night);}}
        private void Apply(State state)
        {
            sun.color=state.sunColour;sun.intensity=state.sunIntensity;sun.shadowStrength=state.shadowStrength;sun.transform.localRotation=state.sunRotation;
            fill.color=state.fillColour;fill.intensity=state.fillIntensity;camera.backgroundColor=state.background;
            foreach(var probe in probes)if(probe!=null){probe.backgroundColor=state.probeColour;probe.intensity=state.probeIntensity;}
            foreach(var light in localLights)ApplyLocal(light,state.night);
            if(grade!=null){grade.postExposure.value=state.exposure;grade.contrast.value=state.contrast;grade.saturation.value=state.saturation;}
            if(split!=null){split.shadows.value=state.shadows;split.highlights.value=state.highlights;}
            if(bloom!=null){bloom.intensity.value=state.bloomIntensity;bloom.threshold.value=state.bloomThreshold;bloom.tint.value=state.bloomTint;}
            if(rain!=null)rain.SetAmount(state.rain);
            applyWetness?.Invoke(state.rain);
        }
    }
}
