namespace Assets.Scripts.Droodism.BackGround.Interface
{
    /// <summary>
    /// 后台结算的脚本侧契约：需要后台补算的零件脚本实现此接口。
    ///
    /// 数据侧的时间戳契约 <see cref="IBackGroundData"/> 与它的基类实现放在一起：
    /// `BackGround/BackgroundPartModifierData.cs`（新零件继承基类即可，不需要直接碰接口）。
    ///
    /// 为什么时间戳不放脚本上：脚本随飞船加载/卸载创建销毁，只有零件数据能序列化进存档；
    /// 为什么结算不放数据上：结算需要 IFuelSource / PartScript 等运行时对象，只有脚本才有。
    /// </summary>
    public interface IBackGroudCalculate
    {
        /// <summary>
        /// 该零件此刻是否允许后台结算（未激活、休眠、关键燃料源为空等应返回 false）。
        /// </summary>
        bool IsBackgroundUpdateEnabled { get; }

        /// <summary>
        /// 结算 elapsedSeconds 秒内本零件应发生的资源变化。
        /// 约定：实现不得依赖帧时间（frame.DeltaTime / Time.deltaTime）与动画，
        /// 必须与实时逻辑共用同一套"速率（每秒）"公式，避免前后台数值不一致。
        /// </summary>
        /// <param name="elapsedSeconds">距上次结算经过的游戏世界时间（秒），可能非常大（离线数天）。</param>
        void BackgroundUpdate(double elapsedSeconds);
    }
}
