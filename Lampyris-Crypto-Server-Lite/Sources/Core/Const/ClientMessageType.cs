namespace Lampyris.Crypto.Server.Lite;

/*
 * 发送到客户端的消息的类型
 */
public enum ClientMessageType
{
    NONE = 0,

    // 策略-触发
    STRATEGY_TRIGGERED = 1001,

    // 策略-持仓阶段性的 涨幅/跌幅 达成提示
    STRATEGY_POSITION_PHASED_ACHIEVED = 1002,

    // 策略-持仓 止损线触发
    STRATEGY_POSITION_STOP_LOSS = 1003,

    // 策略-持仓 移动止盈 触发
    STRATEGY_POSITION_MOVE_PROFIT = 1004,

    // 策略-间隔时间新高/新低 提示
    STRATEGY_INTERVAL_PERC_TIPS = 1005,

    // 策略-突然爆拉 提示
    STRATEGY_SUDDENLY_ACTIVE_TIPS = 1006,

    // 策略-手动设置的条件触发
    STRATEGY_MANUAL_CONDITION_TRIGGERED = 1007,

    // 交易-保证金追加事件
    TRADING_MARGIN_CALL = 2001,

    // 交易-爆仓通知
    TRADING_LIQUIDATION = 2002,

    // 交易-订单更新（只在订单生成/取消/有成交变化的时候）
    TRADING_ORDER_UPDATED = 2003,

    // 行情-异动提示
    QUOTE_ANOMALY_TIPS = 3001,
}

