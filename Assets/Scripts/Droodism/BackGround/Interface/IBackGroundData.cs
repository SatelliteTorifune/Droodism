using System;
using System.Xml.Linq;
using Assets.Scripts.Droodism.BackGround.Interface;
using ModApi.Craft.Parts;
using ModApi.Craft.Parts.Attributes;
using UnityEngine;

namespace Assets.Scripts.Droodism.BackGround.Interface
{
    /// <summary>
    /// 零件数据侧的后台结算时间戳。
    ///
    /// 为什么不直接把它并进 <see cref="BackgroundPartModifierData{T}"/>：
    /// `PartModifierData&lt;T&gt;` 的 `CreateScript/DestroyScript/GetScript` 是 sealed 实现，
    /// 无法再派生一个"非泛型的、带时间戳的"数据基类；而调度器需要在不认识具体 Data 类型的前提下
    /// 读写时间戳，接口是唯一不需要反射的跨类型入口。接口只有一个成员并不浪费——
    /// 它刚好是"脚本生命周期"与"存档生命周期"之间最小的接缝。
    ///
    /// 添加新零件时不需要直接碰这个接口：让 Data 继承 <see cref="BackgroundPartModifierData{T}"/> 即可。
    ///
    /// 为什么时间戳必须在数据里：
    /// 脚本（PartModifierScript）只在飞船位于游戏视图内时存在，飞船卸载时脚本会被销毁；
    /// 时间戳只有通过 [SerializeField] + [PartModifierProperty] 序列化进船存档，
    /// 才能跨"卸载 → 重新加载"乃至跨存档保留，否则每次加载都会丢失补算区间。
    /// </summary>
    public interface IBackGroundData
    {
        /// <summary>
        /// 上次完成后台结算的游戏世界时间（<c>IFlightState.Time</c>，单位秒）。
        /// 默认 0 表示从未结算过（首次加载只记录时间戳，不补算）。
        /// </summary>
        double BackgroundLastUpdateTime { get; set; }
    }
}


