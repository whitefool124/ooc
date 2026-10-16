using OCC.Combat.Presentation;
using UnityEditor;
using UnityEngine;

namespace OCC.Combat.EditorTools
{
    public static class EnemyUnitCandidateReview
    {
        private const string TogglePath="OCC/Art/Review independent enemy candidates";
        [MenuItem(TogglePath)]
        public static void Toggle()
        {
            EnemyUnitArtCandidates.ReviewEnabled=!EnemyUnitArtCandidates.ReviewEnabled;
            EditorApplication.QueuePlayerLoopUpdate();
            Debug.Log("Independent enemy QA review: "+EnemyUnitArtCandidates.ReviewEnabled);
        }
        [MenuItem(TogglePath,true)]
        private static bool ValidateToggle()
        {
            Menu.SetChecked(TogglePath,EnemyUnitArtCandidates.ReviewEnabled);
            return true;
        }
    }
}
