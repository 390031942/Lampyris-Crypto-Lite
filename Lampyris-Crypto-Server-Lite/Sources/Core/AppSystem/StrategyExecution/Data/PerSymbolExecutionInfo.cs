namespace Lampyris.Crypto.Server.Lite;

/// <summary>
/// 单日 每个symbol的执行情况(不一定有对应的执行槽位，但是拥有执行的参数)，记录了执行的行为+日志
/// </summary>
public class PerSymbolExecutionInfo
{
    public List<ExecutionActionInfo> ExecutionActionInfos = new List<ExecutionActionInfo>();

    public StrategyExecutionParamInfo ParamInfo = new StrategyExecutionParamInfo();

    public List<string> LogList = new List<string>();
}
