using System;
using System.Xml.Linq;
using Assets.Scripts.Droodism.BackGround.Interface;
using ModApi.Craft.Parts;
using ModApi.Craft.Parts.Attributes;
using UnityEngine;

namespace Assets.Scripts.Droodism.BackGround
{
    /// <summary>
    /// 参与后台结算的零件数据基类：统一提供序列化的"上次结算时间"。
    ///
    /// 用法：需要后台补算的零件让 Data 继承此类，并让对应的 Script 实现
    /// <see cref="IBackGroudCalculate"/>；脚本里不需要再自己维护时间戳。
    ///
    /// 时间戳的写入时机：
    /// - 本类的 <see cref="GenerateStateXml"/>：存档/卸载写盘时推进到当前时刻；
    /// - <see cref="BackGroundCalculator"/>：飞船卸载出游戏视图时推进。
    /// 结算（一次性补算）只发生在飞船重新加载进游戏视图时。
    /// </summary>
    /// <typeparam name="T">该数据对应的零件脚本类型。</typeparam>
    [Serializable]
    public abstract class BackgroundPartModifierData<T> : PartModifierData<T>, IBackGroundData
        where T : PartModifierScript
    {
        // 0 表示从未结算过：首次加载只记录时间戳，不补算，避免新零件被误算一个"从宇宙诞生至今"的时间。
        [SerializeField]
        [PartModifierProperty]
        private double backgroundLastUpdateTime;

        double IBackGroundData.BackgroundLastUpdateTime
        {
            get => this.backgroundLastUpdateTime;
            set => this.backgroundLastUpdateTime = value;
        }

        /// <summary>
        /// 数据被序列化（存档 / 卸载写盘）时把时间戳推进到当前时刻。
        /// 这样"在视图内存档 → 读档"不会把存档前已经实时消耗过的时间再补算一遍。
        /// </summary>
        public override XElement GenerateStateXml(bool optimizeXml = true)
        {
            BackgroundSettlement.Stamp(this);
            return base.GenerateStateXml(optimizeXml);
        }
    }
}