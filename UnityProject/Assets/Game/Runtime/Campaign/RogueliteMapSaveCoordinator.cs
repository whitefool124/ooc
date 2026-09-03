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
        public const string NewRunSaveFailure = "新游戏保存失败，因此没有开始。请检查存储空间后重试。";
        public const string ActiveRunSaveFailure = "保存失败。请不要退出游戏，并在稍后再次操作以重试保存。";

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
                case RogueliteSaveLoadStatus.Missing: return "没有可以继续的存档。请开始新游戏。";
                case RogueliteSaveLoadStatus.CorruptData: return "这份记录已经损坏，无法继续。旧记录已经另外留存；请删除后开始新游戏。";
                case RogueliteSaveLoadStatus.InvalidSemantics: return "这份记录无法继续使用。旧记录已经另外留存；请删除后开始新游戏。";
                case RogueliteSaveLoadStatus.StoreError: return "暂时无法读取记录。它没有被改动，请稍后重试。";
                default: return "这份记录暂时读不开。请稍后重试，或删除后开始新游戏。";
            }
        }
    }
}
