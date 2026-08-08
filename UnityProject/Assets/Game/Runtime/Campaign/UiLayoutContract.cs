namespace OCC.Combat
{
    public static class UiLayoutContract
    {
        public const int ReferenceWidth = 1920;
        public const int ReferenceHeight = 1080;
        public const float MatchWidthOrHeight = .5f;
        public const int SafeAreaPadding = 24;
        public const int CompactHeightThreshold = 600;
        public const int RogueliteSortingOrder = 40;
        public const int CombatSortingOrder = 45;
        public const int SettlementSortingOrder = 80;
        public const int InteractionSortingOrder = 100;

        public static bool HasValidLayerOrder => RogueliteSortingOrder < CombatSortingOrder && CombatSortingOrder < SettlementSortingOrder && SettlementSortingOrder < InteractionSortingOrder;
    }
}
