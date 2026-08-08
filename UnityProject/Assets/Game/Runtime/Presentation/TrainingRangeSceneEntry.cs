using System.Collections;
using UnityEngine;

namespace OCC.Combat.Presentation
{
    [DisallowMultipleComponent]
    public sealed class TrainingRangeSceneEntry : MonoBehaviour
    {
        private IEnumerator Start()
        {
            yield return null;
            CombatPrototypeBootstrap bootstrap = GetComponent<CombatPrototypeBootstrap>();
            if (bootstrap == null) bootstrap = FindAnyObjectByType<CombatPrototypeBootstrap>();
            if (bootstrap == null)
            {
                Debug.LogError("TrainingRange scene requires a CombatPrototypeBootstrap.", this);
                yield break;
            }

            bootstrap.StartTrainingRange();
        }
    }
}
