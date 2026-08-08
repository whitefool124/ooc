using System;

namespace OCC.Combat
{
    public enum UiVisualEventKind
    {
        MapNodeSelected,
        MapLocationChanged,
        SafeRevisit,
        ResourceChanged,
        BriefingOpened,
        ConfirmationOpened,
        CombatActionSelected,
        CombatRangeRevealed,
        CombatTargetConfirmed,
        CombatCommandSubmitted,
        CombatCommandRejected,
        SettlementOpened,
        RewardClaimed
    }

    // Read-only presentation signal. Consumers may animate or annotate it, but never use it to mutate game state.
    public readonly struct UiVisualEvent
    {
        public UiVisualEventKind Kind { get; }
        public string Subject { get; }
        public int Delta { get; }
        public string Message { get; }

        public UiVisualEvent(UiVisualEventKind kind, string subject, int delta = 0, string message = "")
        {
            if (string.IsNullOrWhiteSpace(subject)) throw new ArgumentException("A visual event subject is required.", nameof(subject));
            Kind = kind;
            Subject = subject;
            Delta = delta;
            Message = message ?? string.Empty;
        }
    }

    public sealed class UiVisualEventStream
    {
        public event Action<UiVisualEvent> Published;

        public void Publish(UiVisualEvent visualEvent)
        {
            Published?.Invoke(visualEvent);
        }
    }
}
