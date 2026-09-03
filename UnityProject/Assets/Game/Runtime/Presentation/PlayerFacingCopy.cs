namespace OCC.Combat.Presentation
{
    public static class PlayerFacingCopy
    {
        public const string ReturnToMapFree = "先回地图看看，不会花掉任何东西。";

        public static string AcademyTimeCost(int cost, int projectedTime)
            => cost <= 0 ? "不花时间" : "用时 " + cost + " · 归来后 " + projectedTime;

        public static string AcademyTimeOutcome(bool entersFinale, bool warnsFinale, bool entersConsolidation)
        {
            if (entersFinale) return "回来后就是终考";
            if (warnsFinale) return "终考已经很近";
            if (entersConsolidation) return "回来后，学期将尽";
            return "日程还宽裕";
        }

        public static string ResourceShortage(string resource, int required, int current)
            => resource + "不足：需要 " + required + "，当前 " + current;

        public static string ActionPointCost(int cost) => "消耗 " + cost + " 行动点";
    }
}
