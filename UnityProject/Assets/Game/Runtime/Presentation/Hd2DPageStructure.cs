using System;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    // Stable world carriers are authored assets. Runtime code binds live controls
    // and instantiates the carrier template only for data-dependent repetitions.
    public sealed class Hd2DPageStructure : MonoBehaviour
    {
        public const string ResourceRoot = "UI/HD2D/";
        [SerializeField] private RectTransform[] carriers = Array.Empty<RectTransform>();
        [SerializeField] private GameObject carrierTemplate;
        private Hd2DDeskLightingRig lighting;
        [SerializeField] private float supportWidth=7.8f;
        [SerializeField] private float framingOffset=.15f;
        [SerializeField] private float framingFocus=.28f;
        public int AuthoredCarrierCount => carriers.Length;
        public Hd2DDeskLightingRig Lighting => lighting;
        public int DynamicCarrierCount { get; private set; }
        internal void ApplyBackdrop(AcademyDeskBackdrop backdrop,MainMenuHd2DStage stage)
        {
            backdrop.SetDocumentFraming(framingOffset,framingFocus);
            foreach(var item in stage.GetComponentsInChildren<Transform>(true))
                if(item.name=="桌面实体"||item.name.StartsWith("桌面木板"))
                    item.localScale=new Vector3(item.name=="桌面实体"?supportWidth:supportWidth-.05f,item.localScale.y,item.localScale.z);
            stage.GetComponent<Hd2DDeskArtCandidate>()?.RefreshModelUvs();
        }

        internal static Hd2DPageStructure Create(string id, MainMenuHd2DStage stage)
        {
            var source = Resources.Load<Hd2DPageStructure>(ResourceRoot + id);
            if (source == null) throw new InvalidOperationException("Missing HD2D page structure: " + id);
            var result = Instantiate(source, stage.transform, false);
            foreach (var item in result.GetComponentsInChildren<Transform>(true)) item.gameObject.layer = stage.gameObject.layer;
            result.lighting=stage.Lighting;
            return result;
        }

        internal RectTransform AcquireCarrier(string key, out bool authored)
        {
            foreach (var carrier in carriers)
                if (carrier != null && carrier.name == key)
                {
                    authored = true;
                    carrier.gameObject.SetActive(true);
                    return carrier;
                }
            authored = false;
            DynamicCarrierCount++;
            if (carrierTemplate == null) throw new InvalidOperationException("Missing dynamic carrier template: " + name);
            var instance = Instantiate(carrierTemplate, transform, false);
            instance.name = key;
            return instance.GetComponent<RectTransform>();
        }

        internal static GameObject CreateSurface(Transform parent, string name)
            => CreateContent("LitSurface",parent,name);
        internal static GameObject CreateContent(string id,Transform parent,string name)
        {
            var template = Resources.Load<GameObject>(ResourceRoot + id);
            if (template == null) throw new InvalidOperationException("Missing HD2D content template: "+id);
            var surface = Instantiate(template, parent, false);
            surface.name = name;
            surface.layer = parent.gameObject.layer;
            return surface;
        }
    }
}
