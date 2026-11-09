using System;
using Assets.Scripts.Droodism.BackGround.Interface;
using ModApi;

namespace Assets.Scripts.Droodism.BackGround
{
    /// <summary>
    /// 后台结算工具：时间戳（<see cref="Stamp"/> / <see cref="CurrentGameFlightTime"/> / <see cref="IsEnabled"/>）
    /// 与大时间步长的速率限制（<see cref="LimitSeconds"/>）。
    ///
    /// 设计原则：**不做任何持续的后台计算**。
    /// - 零件在游戏视图内时，由各自的 FlightUpdate 正常按帧结算；
    /// - 玩家看不到的时候（飞船卸载出视图 / 存档后离线），不做任何计算，只保留"最后结算时间"；
    /// - 等飞船重新加载进视图的那一刻，用 (当前时间 - 时间戳) 做一次瞬时补算。
    ///
    /// 时间戳的写入时机只有两个：
    /// 1. 零件数据被序列化（存档 / 卸载写盘）时——见 `BackgroundPartModifierData.GenerateStateXml`；
    /// 2. 飞船卸载出游戏视图时——见 `BackGroundCalculator`。
    /// 因此不存在"每帧/每几秒扫一遍"的持续计算。
    /// </summary>
    public static class BackgroundSettlement
    {
        /// <summary>
        /// Mod 设置里是否开启了后台结算。
        /// </summary>
        public static bool IsEnabled => ModSettings.Instance.BackGroundCalculation.Value;

        /// <summary>
        /// 后台补算时是否结算伤害（缺氧/缺水/缺粮、辐射）。
        /// 关闭时后台补算只消耗现有资源，不会让乘员受伤或死亡。
        /// </summary>
        public static bool IsBackgroundDamageEnabled =>
            ModSettings.Instance != null && ModSettings.Instance.BackGroundDamage.Value;

        /// <summary>
        /// 当前游戏世界时间（秒）。不在飞行场景时返回 0。
        /// </summary>
        public static double CurrentGameFlightTime
        {
            get
            {
                try
                {
                    if (Game.InFlightScene)
                    {
                        return Game.Instance.FlightScene.FlightState.Time;
                    }
                }
                catch (Exception)
                {
                    // 场景切换过程中 FlightScene / FlightState 可能已被销毁
                }

                return 0.0;
            }
        }

        /// <summary>
        /// 把时间戳推进到当前时刻（只记录，不结算）。用于存档与飞船卸载。
        /// </summary>
        public static void Stamp(IBackGroundData data)
        {
            if (data == null)
            {
                return;
            }

            double now = CurrentGameFlightTime;
            if (now > 0.0)
            {
                data.BackgroundLastUpdateTime = now;
            }
        }

        /// <summary>
        /// 按可用量限制本步可结算的秒数。
        ///
        /// 后台补算的时间步长可能非常大（离线数天），不能像实时逻辑那样"假定一步内资源充足"，
        /// 否则会出现"水已经空了、氧还按整段时间照产"这种账目不平。线性过程可以用本方法
        /// 先算"现有输入最多够跑几秒"，再按这个秒数结算。
        /// </summary>
        /// <param name="requestedSeconds">本步希望结算的秒数。</param>
        /// <param name="available">当前可用量（剩余燃料，或剩余容量）。</param>
        /// <param name="perSecond">每秒用量/产量；&lt;= 0 表示该资源不构成限制。</param>
        /// <returns>实际可结算的秒数（0 表示连一步都跑不动）。</returns>
        public static double LimitSeconds(double requestedSeconds, double available, double perSecond)
        {
            if (requestedSeconds <= 0.0)
            {
                return 0.0;
            }

            if (perSecond <= 0.0)
            {
                return requestedSeconds;
            }

            double affordableSeconds = available / perSecond;
            if (affordableSeconds <= 0.0)
            {
                return 0.0;
            }

            return Math.Min(requestedSeconds, affordableSeconds);
        }
    }
}
