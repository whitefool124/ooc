using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OCC.Combat.Presentation
{
    internal sealed partial class Hd2DLoadoutScene
    {
        private void ConfigureRealisticLighting(MainMenuHd2DStage stage)
        {
            // The stage owns this runtime profile; never alter the authored asset.
            var profile=stage.sceneVolume.profile;
            if(profile.TryGet<ColorAdjustments>(out var grade))
            {
                grade.postExposure.Override(.35f);
                grade.contrast.Override(4f);
                grade.saturation.Override(-4f);
            }
            // Keep the complete working surface sharp. Its tilt spans several depths.
            // Background separation comes from lighting, rather than blurring pixels.
            if(profile.TryGet<DepthOfField>(out var depth))depth.active=false;
            if(profile.TryGet<Bloom>(out var bloom))
            {bloom.intensity.Override(.06f);bloom.threshold.Override(1.5f);}

            var host=new GameObject("整备台局部环境反射",typeof(ReflectionProbe));
            host.layer=stage.gameObject.layer;
            host.transform.SetParent(sceneRoot.transform,false);
            host.transform.localPosition=new Vector3(0,2,0);
            var probe=host.GetComponent<ReflectionProbe>();
            probe.mode=ReflectionProbeMode.Realtime;
            probe.refreshMode=ReflectionProbeRefreshMode.ViaScripting;
            probe.timeSlicingMode=ReflectionProbeTimeSlicingMode.AllFacesAtOnce;
            probe.resolution=128;probe.hdr=true;
            probe.size=new Vector3(12,7,9);probe.center=Vector3.zero;
            probe.boxProjection=true;probe.blendDistance=1f;
            probe.cullingMask=1<<stage.gameObject.layer;
            probe.nearClipPlane=.1f;probe.farClipPlane=20;
            probe.clearFlags=ReflectionProbeClearFlags.SolidColor;
            probe.backgroundColor=new Color(.16f,.19f,.23f);
            probe.intensity=.7f;
            StartCoroutine(CaptureDeskReflection(probe));
        }

        private static IEnumerator CaptureDeskReflection(ReflectionProbe probe)
        {
            // Capture after paper meshes and camera framing have finished building.
            yield return null;
            if(probe!=null)probe.RenderProbe();
        }
    }
}
