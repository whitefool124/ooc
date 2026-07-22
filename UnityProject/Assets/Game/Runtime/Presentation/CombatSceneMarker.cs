using UnityEngine;

namespace OCC.Combat.Presentation
{
    public enum CombatSceneMarkerType
    {
        Unit,
        LightCover,
        HeavyCover,
        Objective
    }

    [DisallowMultipleComponent]
    public sealed class CombatSceneMarker : MonoBehaviour
    {
        [SerializeField] private CombatSceneMarkerType markerType;
        [SerializeField] private string prototypeId;

        public CombatSceneMarkerType MarkerType => markerType;
        public string PrototypeId => prototypeId;

        public void Configure(CombatSceneMarkerType type, string id)
        {
            markerType = type;
            prototypeId = id;
        }
    }
}
