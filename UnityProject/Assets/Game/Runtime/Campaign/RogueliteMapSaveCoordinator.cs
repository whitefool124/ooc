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
        public bool IsWriteProtected => gateway.IsMapRunWriteProtected;
        public MapSaveUiPresentation Presentation => MapSaveUiPresentation.From(
            HasSave, gateway.LastLoadStatus, LastSaveSucceeded);

        public RogueliteMapSaveCoordinator(RogueliteSaveGateway gateway)
        {
            this.gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        }

        public RogueliteMapStartResult TryStart(bool continueSave, string starterId, int seed,
            bool acknowledgeOriginOnCreate = false)
        {
            RogueliteMapRun run;
            if (continueSave)
            {
                if (IsWriteProtected)
                    return new RogueliteMapStartResult(false, null,
                        "这份记录已启用写入保护，不能继续。旧档与安全副本已保留；请选择其他档案，或确认覆盖此档后开始新游戏。");
                if (!gateway.TryLoadMapRun(out run))
                    return new RogueliteMapStartResult(false, null, DescribeLoadFailure(gateway.LastLoadStatus));
                // A successful reload is a fresh persisted state, even if this coordinator
                // previously reported a failed attempt to replace or save another state.
                LastSaveSucceeded = true;
            }
            else
            {
                run = RogueliteMapRun.CreateFirstRunV1(seed);
                // The formal front end opens directly on the map. Include this transition
                // in the first verified write so a later write failure cannot report a
                // failed replacement after the previous slot was already overwritten.
                if (acknowledgeOriginOnCreate) run.AcknowledgeFirstRunOrigin();
                if (!gateway.SaveNewMapRun(run))
                {
                    LastSaveSucceeded = false;
                    return new RogueliteMapStartResult(false, null, NewRunSaveFailure);
                }
                LastSaveSucceeded = true;
            }
            return new RogueliteMapStartResult(true, run, string.Empty);
        }

        public RogueliteMapStartResult TryStartSubsequentAcademyRound(int seed)
        {
            if (IsWriteProtected)
                return new RogueliteMapStartResult(false, null, "此档案已启用写入保护，请先恢复旧档。");
            if (!gateway.TryLoadMapRun(out RogueliteMapRun previous))
                return new RogueliteMapStartResult(false, null, DescribeLoadFailure(gateway.LastLoadStatus));
            if (!previous.IsComplete || !previous.IsInAcademyLayer)
                return new RogueliteMapStartResult(false, null, "当前档案还有未结束的单轮，不能创建新一轮。");
            RogueliteMapRun next = RogueliteMapRun.CreateSubsequentAcademyRun(seed);
            if (!gateway.SaveNewMapRun(next))
            {
                LastSaveSucceeded = false;
                return new RogueliteMapStartResult(false, null, NewRunSaveFailure);
            }
            LastSaveSucceeded = true;
            return new RogueliteMapStartResult(true, next, string.Empty);
        }

        public bool PrepareSlotForReplacement()
        {
            if (IsWriteProtected)
            {
                // Preserve a valid old record until the replacement write is verified.
                if (gateway.TryRecoverProtectedMapRun(out _)) return true;
                if (gateway.LastLoadStatus != RogueliteSaveLoadStatus.CorruptData &&
                    gateway.LastLoadStatus != RogueliteSaveLoadStatus.InvalidSemantics) return false;
                return gateway.TryPrepareInvalidMapRunForReplacement();
            }
            if (gateway.TryLoadMapRun(out _)) return true;
            if (gateway.LastLoadStatus == RogueliteSaveLoadStatus.Missing) return true;
            if (gateway.LastLoadStatus == RogueliteSaveLoadStatus.CorruptData ||
                gateway.LastLoadStatus == RogueliteSaveLoadStatus.InvalidSemantics)
                return gateway.TryPrepareInvalidMapRunForReplacement();
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
