using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

namespace OCC.Combat.Presentation
{
    // Desk-only opt-in. Scene lighting and source pixels remain independent.
    public sealed class Hd2DDeskReadability : ScriptableObject
    {
        [SerializeField] private Material surfaceTemplate;
        [SerializeField, Range(0,.6f)] private float paperEmission = .24f;
        [SerializeField, Range(0,.6f)] private float controlEmission = .30f;
        [SerializeField, ColorUsage(false,true)] private Color edgeColour = new Color(1f,.78f,.44f);
        [SerializeField, Range(0,6)] private float edgeIntensity = 2.8f;
        [SerializeField, Range(.5f,5)] private float edgeWidth = 2f;
        [SerializeField, Range(0,1)] private float bloomIntensity = .24f;
        [SerializeField] private Color paperAlbedo = new Color(.94f,.89f,.76f,1);
        [SerializeField] private Color frameBrass = new Color(.46f,.28f,.095f,1);
        [SerializeField] private Color frameInk = new Color(.075f,.045f,.022f,1);
        private static Hd2DDeskReadability current;
        internal static Hd2DDeskReadability Current
        {
            get
            {
                if(current==null)current=Resources.Load<Hd2DDeskReadability>(Hd2DPageStructure.ResourceRoot+"Readability");
                if(current==null)throw new System.InvalidOperationException("Missing desk Readability profile");
                return current;
            }
        }
        internal void Apply(Material material, Image image)
        {
            if(surfaceTemplate==null)throw new System.InvalidOperationException("Missing serialized desk shader variant");
            material.shader=surfaceTemplate.shader;
            var control=image.GetComponent<Selectable>();
            bool panel=image.GetComponent<Hd2DContainerStyle>()!=null;
            // Illustrations keep their painted interiors; paper and controls gain a
            // little albedo emission. Every glow follows the existing silhouette.
            material.EnableKeyword("OCC_DESK_READABILITY");
            material.SetFloat("_SoftEmission",control!=null?controlEmission:panel?paperEmission:.10f);
            float weight=control!=null?(control.IsInteractable()?1f:.25f):panel?.65f:0;
            material.SetColor("_DeskEdgeColour",edgeColour);
            material.SetFloat("_DeskEdgeStrength",edgeIntensity*weight);
            material.SetFloat("_DeskEdgeWidth",edgeWidth);
            var uv=image.sprite==null?new Vector4(0,0,1,1):UnityEngine.Sprites.DataUtility.GetOuterUV(image.sprite);
            material.SetVector("_DeskUvBounds",uv);
            var rect=image.rectTransform.rect;
            material.SetVector("_DeskRect",new Vector4(rect.xMin,rect.yMin,rect.xMax,rect.yMax));
            material.SetFloat("_DeskPlain",image.sprite==null?1:0);
            material.SetFloat("_DeskPaperSurface",panel||control!=null?1:0);
            material.SetColor("_DeskPaperAlbedo",paperAlbedo);
            material.SetColor("_DeskFrameBrass",frameBrass);material.SetColor("_DeskFrameInk",frameInk);
            bool compact=image.name.StartsWith("背包格_");
            material.SetFloat("_DeskFrame",image.sprite==null&&(panel||control!=null)?(compact?2:1):0);
            var scale=image.transform.lossyScale;
            material.SetVector("_DeskPixelPitch",new Vector4(1/(32*Mathf.Max(.00001f,Mathf.Abs(scale.x))),1/(32*Mathf.Max(.00001f,Mathf.Abs(scale.y))),compact?.10f:control!=null?.22f:.32f,0));
            if(control!=null)
            {
                var feedback=image.GetComponent<Hd2DSceneControl>();
                if(feedback==null)feedback=image.gameObject.AddComponent<Hd2DSceneControl>();
                feedback.BindGlow(material,edgeIntensity,controlEmission);
            }
        }
        internal void ApplyBloom(MainMenuHd2DStage stage)
        {
            var profile=stage.sceneVolume.profile;
            if(!profile.TryGet<Bloom>(out var bloom))bloom=profile.Add<Bloom>(true);
            bloom.active=true;bloom.intensity.Override(bloomIntensity);
            bloom.threshold.Override(1.3f);bloom.scatter.Override(.7f);
        }
    }
}
