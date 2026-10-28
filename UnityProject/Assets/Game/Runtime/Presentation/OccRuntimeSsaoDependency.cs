using UnityEngine.Rendering.Universal;

namespace OCC.Combat.Presentation
{
    // Declares runtime-created SSAO to URP's build stripper. The live desk/battle
    // owns its actual AO pass; this dependency never renders another one.
    public sealed class OccRuntimeSsaoDependency : ScreenSpaceAmbientOcclusion
    {
        public override void Create() { }
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) { }
    }
}
