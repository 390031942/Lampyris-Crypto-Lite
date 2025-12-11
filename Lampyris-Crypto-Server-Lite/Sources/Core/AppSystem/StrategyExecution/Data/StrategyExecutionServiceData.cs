using DocumentFormat.OpenXml.Drawing.Charts;
using Lampyris.CSharp.Common;

namespace Lampyris.Crypto.Server.Lite;

public class StrategyExecutionServiceData
{
    #region 字段
    /// <summary>
    /// 是否开仓，用于控制
    /// </summary>
    public bool ShouldOpenSlotPosition = true;

    /// <summary>
    /// 做多槽位数量
    /// </summary>
    public int SlotLongCount = 7;

    /// <summary>
    /// 做空槽位数量
    /// </summary>
    public int SlotShortCount = 3;

    /// <summary>
    /// 每日最大资金量, 当日所有仓位的保证金占用不能超过这个数值
    /// </summary>
    public decimal PerDayInitialUSDT = 10000;

    /// <summary>
    /// 目标杠杆倍数 = 执行中的槽位总数
    /// TODO：支持配置 每日最大资金量 + 最大持仓名义价值以决定杠杆倍数，
    /// 比如最大持仓名义价值1w USDT，在每日最大可支配资金=1w USDT，槽位总数=10的情况下，决定了杠杆倍数为10倍
    /// 而在每日最大可支配资金=2w USDT，槽位总数=10的情况下，决定了杠杆倍数为5倍
    /// </summary>
    public int TargetLeverage = 10;

    /// <summary>
    /// 触发买入(%)，做多触发>=TriggerPerc %, 做空触发<= TriggerPerc %
    /// </summary>
    public decimal TriggerPerc = 10;

    /// <summary>
    /// 爆发式拉升触发买入所需要的一秒拉升幅度(%),只有做多
    /// </summary>
    public decimal BlastTriggerOneSecondPerc = 14.8m;

    /// <summary>
    /// 间隔时间新高/新低关注点的涨幅/跌幅 起点
    /// </summary>
    public decimal IntervalNewPerc = 10;

    /// <summary>
    /// 间隔时间新高/新低的时间间隔要求（分钟）
    /// </summary>
    public decimal IntervalNewPercRequireMinute = 60;

    /// <summary>
    /// 每一天的执行信息（最多保留最近30日）
    /// </summary>
    public List<PerDayExecutionInfo> PerDayExecutionInfoList = new List<PerDayExecutionInfo>();

    /// <summary>
    /// 当日执行信息
    /// </summary>
    public PerDayExecutionInfo CurrentDayExecutionInfo = new PerDayExecutionInfo();

    /// <summary>
    /// 当日间隔时间新高/新低信息
    /// </summary>
    public Dictionary<string, IntervalNewestPercRecord> IntervalNewestPercRecordMap = new();

    /// <summary>
    /// 每个symbol当日最高排名
    /// </summary>
    public Dictionary<string, int> SymbolBestRankMap = new();

    /// <summary>
    /// 槽位数据字典
    /// </summary>
    public Dictionary<StrategyExecutionSlotType, List<StrategyExecutionSlotData>> SlotDataMap = new Dictionary<StrategyExecutionSlotType, List<StrategyExecutionSlotData>>();

    /// <summary>
    /// 阶段性目标涨跌幅
    /// </summary>
    public List<decimal> PhasedPercList = new List<decimal>() { 12, 15, 20, 30, 50, 100 };

    /// <summary>
    /// 每个 阶段性目标涨跌幅 期望达成的 最大持仓分钟数（如：在前10分钟内达成15%的涨幅）
    /// </summary>
    public List<int> PhasedAchieveExceptedMinutes = new List<int>() { 5, 10, 30, 60, 120, 360 };

    /// <summary>
    /// 开仓时候的相当于现价的上浮百分比
    /// </summary>
    public decimal LimitOrderBuyOffsetPerc = 0.5m;

    /// <summary>
    /// 是否允许开仓
    /// </summary>
    public bool EnableOpenPosition = true;

    /// <summary>
    /// 检测到BTC穿插针时间
    /// </summary>
    public long LastBTCSuddenlyActiveTimestamp = -1L;

    /// <summary>
    /// 是否检测到BTC高涨幅/跌幅
    /// </summary>
    public bool IsBTCHighPercToday = true;

    /// <summary>
    /// 自动关闭 允许开仓的时间戳
    /// </summary>
    public long AutoDisableOpenPositionTimestamp = -1L;

    /// <summary>
    /// 被禁用执行的合约
    /// </summary>
    public HashSet<string> BannedSymbols = new HashSet<string>();

    /// <summary>
    /// 自动开仓时 数量的缩放比例(0-1之间)，如0.5表示，实际开仓的数量为可买数量的一半
    /// </summary>
    public decimal PositionScale = 1;

    #endregion

    #region 方法
    /// <summary>
    /// 获取某个 策略触发原因 对某个Symbol触发时间，若不存在则返回空
    /// </summary>
    public DateTime? GetTriggeredDateTime(StrategyTriggerReason reason, string symbol)
    {
        if(CurrentDayExecutionInfo.TriggeredSymbolDateTimeMap.ContainsKey(reason))
        {
            var dateTimeMap = CurrentDayExecutionInfo.TriggeredSymbolDateTimeMap[reason];
            if (dateTimeMap.ContainsKey(symbol)) 
            {
                return dateTimeMap[symbol];
            }
        }
        return null;
    }

    /// <summary>
    /// 设置某个 策略触发原因 对某个Symbol触发时间
    /// </summary>
    public void SetTriggeredDateTime(StrategyTriggerReason reason, string symbol, DateTime dateTime)
    {
        if (!CurrentDayExecutionInfo.TriggeredSymbolDateTimeMap.ContainsKey(reason))
        {
            CurrentDayExecutionInfo.TriggeredSymbolDateTimeMap[reason] = new Dictionary<string, DateTime>();
        }

        var dateTimeMap = CurrentDayExecutionInfo.TriggeredSymbolDateTimeMap[reason];
        dateTimeMap[symbol] = dateTime;
    }
    
    public PerSymbolExecutionInfo GetOrCreateExecutionInfo(string symbol)
    {
        if (!CurrentDayExecutionInfo.PerSymbolExecutionMap.ContainsKey(symbol))
        {
            return CurrentDayExecutionInfo.PerSymbolExecutionMap[symbol] = new PerSymbolExecutionInfo();
        }

        return CurrentDayExecutionInfo.PerSymbolExecutionMap[symbol];
    }
    public void RecordAction(string symbol, string actionName, DateTime dateTime)
    {
        GetOrCreateExecutionInfo(symbol).ExecutionActionInfos.Add(new ExecutionActionInfo()
        {
            ActionName = actionName,
            DateTime = dateTime,
        });
    }

    public void Log(string symbol, string message)
    {
        GetOrCreateExecutionInfo(symbol).LogList.Add(message);
        Logger.LogInfo($"[合约{symbol}]{message}");
    }

    /// <summary>
    /// 每天重置数据
    /// </summary>
    public void DailyReset()
    {

    }

    /// <summary>
    /// 判断当日是不是高波动性
    /// </summary>
    public bool IsHighVolatitle(string symbol)
    {
        return false;
    }

    /// <summary>
    /// 记录当日高波动性的合约s
    /// </summary>
    public void RecordHighVolatile(string symbol)
    {
    }

    /// <summary>
    /// 本日是否禁用自动买入，包含手动禁用，和高波动禁用。
    /// </summary>
    public bool IsBannedToday(string symbol)
    {
        return BannedSymbols.Contains(symbol) || IsHighVolatitle(symbol);
    }

    /// <summary>
    /// 某个类型的最大槽位数量
    /// </summary>
    /// <param name="slotType"></param>
    /// <returns></returns>
    public int GetSlotMaxCount(StrategyExecutionSlotType slotType)
    {
        if (slotType == StrategyExecutionSlotType.LONG)
            return SlotLongCount;
        else if(slotType == StrategyExecutionSlotType.SHORT)
            return SlotShortCount;

        return 1;
    }

    /// <summary>
    /// 某个类型的 执行中槽位数量
    /// </summary>
    /// <param name="slotType"></param>
    /// <returns></returns>
    public int GetExecutingSlotCount(StrategyExecutionSlotType slotType)
    {
        var slotDataList = SlotDataMap[slotType];
        for(int i = 0;  i < slotDataList.Count; i++)
        {
            var slotData = slotDataList[i];
            // 由于排序总是把 执行中的排在前面，遇到一个没执行的就可以确定执行中的数量了
            if (!slotData.IsExecuting())
            {
                return i;
            }
        }

        return 0;
    }

    public StrategyExecutionSlotData? FindSlotDataBySymbol(string symbol)
    {
        foreach(var pair in SlotDataMap)
        {
            foreach(var pair2 in pair.Value)
            {
                if(pair2.Symbol == symbol)
                {
                    return pair2;
                }
            }
        }
        return null;
    }

    #endregion
}
