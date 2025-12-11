namespace Lampyris.Crypto.Server.Lite;

public enum StrategyExectionStatus 
{
    NONE, // 未判断过条件，也没执行
    TEST_FAILED, // 条件判断失败了，下次继续
    TEST_SUCCESS, // 条件判断成功
    EXECUTED, // 执行了, 但是要一直Tick等待执行结束
    EXCLUDED, // 条件判断失败后排除在外，不再执行
    ABORTED, // 中断执行
    FINISHED, // 执行完成
    WAIT_FOR_COOLDOWN, // 执行了，冷却中
}
