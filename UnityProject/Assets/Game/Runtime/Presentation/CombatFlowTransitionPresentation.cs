using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace OCC.Combat.Presentation
{
    /// <summary>Owns the short, input-blocking bridge from a briefing into live combat.</summary>
    public sealed class CombatFlowTransitionPresentation : MonoBehaviour
    {
        private IUiPreferenceHost bootstrap;
        private Canvas canvas;
        private CanvasGroup group;
        private bool running;

        public void Initialize(IUiPreferenceHost host)
        {
            bootstrap = host;
        }

        public bool Play(Action activateCombat)
        {
            if (running || activateCombat == null) return false;
            UiMotionProfile motion = UiMotionProfile.FromIntensity(bootstrap == null ? 1f : bootstrap.UiPreferences.AnimationIntensity);
            if (motion.IsImmediate)
            {
                activateCombat();
                return true;
            }

            EnsureCanvas();
            running = true;
            canvas.gameObject.SetActive(true);
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = true;
            DOTween.Kill(this);
            DOTween.Sequence().SetTarget(this).SetUpdate(true)
                .Append(DOTween.To(() => group.alpha, value => group.alpha = value, 1f, motion.QuickDuration)
                    .SetEase(FormalUiMotionTokens.FeedbackEase))
                .AppendCallback(() => activateCombat())
                .AppendInterval(motion.QuickDuration * .5f)
                .Append(DOTween.To(() => group.alpha, value => group.alpha = value, 0f, motion.StandardDuration)
                    .SetEase(FormalUiMotionTokens.StandardEase))
                .OnComplete(Finish);
            return true;
        }

        private void EnsureCanvas()
        {
            if (canvas != null) return;
            canvas = FormalUiKit.CanvasRoot("战斗流程过渡", UiLayoutContract.InteractionSortingOrder + 30);
            GameObject root = canvas.gameObject;
            group = root.AddComponent<CanvasGroup>();
            Image veil = root.AddComponent<Image>();
            veil.color = FormalUiTheme.WithAlpha(FormalUiTheme.Ink, .94f);
            veil.raycastTarget = true;

            GameObject title = FormalUiKit.AnchoredPanel("过渡提示", root.transform, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                Vector2.zero, new Vector2(560f, 96f), Color.clear);
            FormalUiKit.Label("文字", "正在进入场地", title.transform, new Vector2(0f, -4f), new Vector2(560f, 40f),
                FormalUiTheme.BodyFontSize, FormalUiTheme.OnInk, TextAnchor.MiddleCenter);
            FormalUiKit.Panel("进度线", title.transform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0f, -34f),
                new Vector2(256f, 4f), FormalUiTheme.Cyan);
            root.SetActive(false);
        }

        private void Finish()
        {
            running = false;
            if (canvas != null) canvas.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
            if (canvas != null) Destroy(canvas.gameObject);
        }
    }
}
