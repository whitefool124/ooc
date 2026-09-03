using System;

namespace OCC.Combat
{
    public enum UiScreen
    {
        Landing,
        Map,
        Briefing,
        Combat,
        Settlement
    }

    public enum UiOverlay
    {
        None,
        Settings,
        Archive,
        Loadout,
        NodeRoom,
        Confirmation
    }

    public enum UiBackAction
    {
        None,
        CloseOverlay,
        NavigateLanding,
        NavigateMap,
        RequestLeaveCombat
    }

    public enum UiConfirmationKind
    {
        ReplaceExistingRun,
        TacticalRestart,
        LeaveCombat
    }

    public enum UiFeedbackKind
    {
        Information,
        Success,
        Rejected,
        Saved
    }

    public sealed class UiNavigationState
    {
        public UiScreen Screen { get; private set; }
        public UiOverlay Overlay { get; private set; }
        public string DefaultFocusKey { get; private set; }
        public string RestoreFocusKey { get; private set; }

        public UiNavigationState(UiScreen screen, string defaultFocusKey)
        {
            Navigate(screen, defaultFocusKey);
        }

        public void Navigate(UiScreen screen, string defaultFocusKey)
        {
            Screen = screen;
            Overlay = UiOverlay.None;
            DefaultFocusKey = defaultFocusKey ?? string.Empty;
            RestoreFocusKey = string.Empty;
        }

        public void OpenOverlay(UiOverlay overlay, string currentFocusKey)
        {
            if (overlay == UiOverlay.None) throw new ArgumentException("An overlay is required.", nameof(overlay));
            if (Overlay == UiOverlay.None) RestoreFocusKey = currentFocusKey ?? string.Empty;
            Overlay = overlay;
        }

        public string CloseOverlay()
        {
            string restore = RestoreFocusKey;
            Overlay = UiOverlay.None;
            RestoreFocusKey = string.Empty;
            return restore;
        }

        public UiBackAction ResolveBack()
        {
            if (Overlay != UiOverlay.None) return UiBackAction.CloseOverlay;
            if (Screen == UiScreen.Briefing) return UiBackAction.NavigateMap;
            if (Screen == UiScreen.Map) return UiBackAction.NavigateLanding;
            if (Screen == UiScreen.Combat) return UiBackAction.RequestLeaveCombat;
            return UiBackAction.None;
        }
    }

    public sealed class UiConfirmationRequest
    {
        public UiConfirmationKind Kind { get; }
        public string Title { get; }
        public string Message { get; }
        public string ConfirmLabel { get; }
        public string CancelLabel { get; }

        public UiConfirmationRequest(UiConfirmationKind kind, string title, string message, string confirmLabel, string cancelLabel = "取消")
        {
            if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("A title is required.", nameof(title));
            if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("A message is required.", nameof(message));
            if (string.IsNullOrWhiteSpace(confirmLabel)) throw new ArgumentException("A confirm label is required.", nameof(confirmLabel));
            if (string.IsNullOrWhiteSpace(cancelLabel)) throw new ArgumentException("A cancel label is required.", nameof(cancelLabel));
            Kind = kind;
            Title = title;
            Message = message;
            ConfirmLabel = confirmLabel;
            CancelLabel = cancelLabel;
        }
    }

    public sealed class UiActionFeedback
    {
        public UiFeedbackKind Kind { get; }
        public string Message { get; }

        public UiActionFeedback(UiFeedbackKind kind, string message)
        {
            if (string.IsNullOrWhiteSpace(message)) throw new ArgumentException("A feedback message is required.", nameof(message));
            Kind = kind;
            Message = message;
        }
    }

    public readonly struct MapSaveUiPresentation
    {
        public bool CanContinue { get; }
        public string ContinueDetail { get; }
        public string ReturnDetail { get; }
        public string ReplacementMessage { get; }

        private MapSaveUiPresentation(bool canContinue, string continueDetail, string returnDetail, string replacementMessage)
        {
            CanContinue = canContinue;
            ContinueDetail = continueDetail;
            ReturnDetail = returnDetail;
            ReplacementMessage = replacementMessage;
        }

        public static MapSaveUiPresentation From(bool hasSave, RogueliteSaveLoadStatus loadStatus, bool lastWriteSucceeded)
        {
            bool protectedSlot = loadStatus == RogueliteSaveLoadStatus.CorruptData || loadStatus == RogueliteSaveLoadStatus.InvalidSemantics;
            bool storeUnavailable = loadStatus == RogueliteSaveLoadStatus.StoreError;
            string continueDetail = !hasSave ? "暂无存档" :
                protectedSlot ? "存档损坏 · 可开始新游戏" :
                storeUnavailable ? "暂时无法读取 · 请重试" : "从上次位置继续";
            string replacementMessage = protectedSlot
                ? "当前存档无法读取。继续后会保留一份损坏备份，再用新游戏覆盖主存档。"
                : "开始新游戏会覆盖当前存档；这局已经完成的进度将无法恢复。";
            return new MapSaveUiPresentation(hasSave && !protectedSlot && !storeUnavailable, continueDetail,
                lastWriteSucceeded ? "当前进度已保存" : "保存失败 · 请留在当前页重试", replacementMessage);
        }
    }

    public readonly struct UiMotionProfile
    {
        public float Intensity { get; }
        public float QuickDuration { get; }
        public float StandardDuration { get; }
        public float ToastDuration { get; }
        public float PressOffset { get; }
        public float PageOffset { get; }
        public float ModalScaleOffset { get; }
        public bool IsImmediate => Intensity <= 0f;

        private UiMotionProfile(float intensity)
        {
            Intensity = intensity;
            QuickDuration = .12f * intensity;
            StandardDuration = .22f * intensity;
            ToastDuration = .28f * intensity;
            PressOffset = 2f * intensity;
            // Page changes should read as hierarchy feedback, not as lateral travel.
            PageOffset = 6f * intensity;
            ModalScaleOffset = .04f * intensity;
        }

        public static UiMotionProfile FromIntensity(float intensity)
        {
            return new UiMotionProfile(Math.Max(0f, Math.Min(1f, intensity)));
        }
    }
}
