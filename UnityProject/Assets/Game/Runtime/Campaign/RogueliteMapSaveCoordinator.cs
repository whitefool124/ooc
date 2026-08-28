using System;

namespace OCC.Combat
{
    public readonly struct RogueliteMapStartResult
    {
        public bool Success { get; }
        public RogueliteMapRun Run { get; }
        public string FailureMessage { get; }

        public RogueliteMapStartResult(bool success, RogueliteMapRun run, string failureMessage)
        {
            Success = success;
            Run = run;
            FailureMessage = failureMessage ?? string.Empty;
        }
    }

    /// <summary>
    /// Owns map-run persistence policy while delegating serialization and corruption protection to the gateway.
    /// </summary>
    public sealed class RogueliteMapSaveCoordinator
    {
        public const string NewRunSaveFailure = "新推进未能写入存档；仍停留在入口，未启动未保存的行动";
        public const string ActiveRunSaveFailure = "地图进度未能写入；当前状态仍保留在内存中，请勿退出并稍后重试";

        private readonly RogueliteSaveGateway gateway;
        public bool LastSaveSucceeded { get; private set; } = true;
        public bool HasSave => gateway.HasMapRun;
        public MapSaveUiPresentation Presentation => MapSaveUiPresentation.From(
            HasSave, gateway.LastLoadStatus, LastSaveSucceeded);

        public RogueliteMapSaveCoordinator(RogueliteSaveGateway gateway)
        {
            this.gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        }

        public RogueliteMapStartResult TryStart(bool continueSave, string starterId, int seed)
        {
            RogueliteMapRun run;
            if (continueSave)
            {
                if (!gateway.TryLoadMapRun(out run))
                    return new RogueliteMapStartResult(false, null, DescribeLoadFailure(gateway.LastLoadStatus));
            }
            else
            {
                run = new RogueliteMapRun(seed, starterId);
                if (!gateway.SaveNewMapRun(run))
                {
                    LastSaveSucceeded = false;
                    return new RogueliteMapStartResult(false, null, NewRunSaveFailure);
                }
                LastSaveSucceeded = true;
            }
            return new RogueliteMapStartResult(true, run, string.Empty);
        }

        public bool PrepareSlotForReplacement()
        {
            if (gateway.TryLoadMapRun(out _)) return true;
            if (gateway.LastLoadStatus == RogueliteSaveLoadStatus.Missing) return true;
            if (gateway.LastLoadStatus == RogueliteSaveLoadStatus.CorruptData ||
                gateway.LastLoadStatus == RogueliteSaveLoadStatus.InvalidSemantics)
                return gateway.DeleteMapRun();
            return false;
        }

        public bool Save(RogueliteMapRun run)
        {
            LastSaveSucceeded = run != null && gateway.SaveMapRun(run);
            return LastSaveSucceeded;
        }

        public bool Delete() => gateway.DeleteMapRun();

        public static string DescribeLoadFailure(RogueliteSaveLoadStatus status)
        {
            switch (status)
            {
                case RogueliteSaveLoadStatus.Missing: return "没有可继续的地图存档；未创建或覆盖任何数据";
                case RogueliteSaveLoadStatus.CorruptData: return "地图存档文本损坏；主槽与首份备份已保护，明确删槽前不可覆盖";
                case RogueliteSaveLoadStatus.InvalidSemantics: return "地图存档状态不合法；主槽与首份备份已保护，明确删槽前不可覆盖";
                case RogueliteSaveLoadStatus.StoreError: return "存档存储暂时不可用；未把故障当作无存档，也未启动新推进";
                default: return "地图存档无法读取；未启动新推进";
            }
        }
    }
}
