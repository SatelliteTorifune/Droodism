using System;
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Droodism.BackGround.Interface;
using Assets.Scripts.Flight.Sim;
using Assets.Scripts.State;
using ModApi;
using ModApi.Craft;
using ModApi.Craft.Parts;
using ModApi.Flight;
using ModApi.Flight.GameView;
using ModApi.GameLoop;
using ModApi.Scenes.Events;

namespace Assets.Scripts.Droodism.BackGround
{
    /// <summary>
    /// 后台结算调度器。
    ///
    /// 设计原则：**没有持续的后台计算**。
    /// 整个后台补算只在"飞船重新加载进游戏视图"的瞬间做一次：
    ///     补算秒数 = 当前游戏时间 - 零件数据里保存的上次结算时间
    /// 其余时间：
    /// - 飞船在视图内：各零件自己的 FlightUpdate 正常按帧结算（这是实时逻辑，不是后台计算）；
    /// - 飞船不在视图内：什么都没在跑，也不做任何计算，只保留时间戳。
    ///
    /// 时间戳由两部分维护，都不是周期性任务：
    /// - 零件数据被序列化时（存档 / 卸载写盘）——`BackgroundPartModifierData.GenerateStateXml`；
    /// - 飞船卸载出游戏视图时（UnloadingFromGameView）——本类。
    ///
    /// 事件挂钩同样是一次性的：飞行场景初始化后注册到所有飞船节点，之后靠
    /// FlightState.CraftNodeAdded / CraftNodeRemoved 维护，不再逐帧轮询。
    /// </summary>
    public class BackGroundCalculator : MonoBehaviourBase
    {
        public static BackGroundCalculator Instance { get; private set; }

        /// <summary>已挂钩游戏视图事件的飞船（NodeId -> CraftNode）。</summary>
        private readonly Dictionary<int, ICraftNode> hookedCraftNodes = new Dictionary<int, ICraftNode>();

        /// <summary>下次进入游戏视图时需要补算的飞船（其余情况进入视图只推进时间戳）。</summary>
        private readonly HashSet<int> settleOnNextLoad = new HashSet<int>();

        private FlightState flightState;

        private void Awake()
        {
            Instance = this;
            Game.Instance.SceneManager.SceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            Game.Instance.SceneManager.SceneLoaded -= OnSceneLoaded;
            Teardown();
        }

        private void OnSceneLoaded(object sender, SceneEventArgs args)
        {
            
            Teardown();

            if (!Game.InFlightScene)
            {
                return;
            }

            var flightScene = Game.Instance.FlightScene;
            if (flightScene == null)
            {
                return;
            }
            flightScene.Initialized += OnFlightInitialized;
        }

        private void OnFlightInitialized(IFlightScene flightScene)
        {
            flightScene.Initialized -= OnFlightInitialized;

            flightState = flightScene.FlightState as FlightState;
            if (flightState != null)
            {
                flightState.CraftNodeAdded += OnCraftNodeAdded;
                flightState.CraftNodeRemoved += OnCraftNodeRemoved;
            }

            HookExistingNodes(flightScene);
        }

        private void Teardown()
        {
            if (flightState != null)
            {
                flightState.CraftNodeAdded -= OnCraftNodeAdded;
                flightState.CraftNodeRemoved -= OnCraftNodeRemoved;
                flightState = null;
            }

            UnhookAllCraftNodes();
            settleOnNextLoad.Clear();
        }

        /// <summary>
        /// 场景初始化时已经存在的飞船：挂钩事件。
        /// 已经加载进视图的（脚本已存在）延后一帧补算；没加载的等 LoadedIntoGameView 时补算。
        /// </summary>
        private void HookExistingNodes(IFlightScene scene)
        {
            var nodes = scene.FlightState?.CraftNodes;
            if (nodes == null)
            {
                return;
            }

            
            foreach (var node in nodes)
            {
                if (node == null || hookedCraftNodes.ContainsKey(node.NodeId))
                {
                    continue;
                }

                Hook(node);

                if (node.CraftScript != null)
                {
                    DeferSettlement(node);
                }
                else
                {
                    settleOnNextLoad.Add(node.NodeId);
                }
            }
        }

        /// <summary>
        /// 运行中新出现的飞船（发射、分离、对接）：
        /// - CraftScript == null：从存档/数据加载，等进视图时补算；
        /// - CraftScript != null：分裂/对接产生的实时飞船，零件来自当前视图，不补算。
        /// </summary>
        private void OnCraftNodeAdded(CraftNode node)
        {
            if (node == null)
            {
                return;
            }

            if (!hookedCraftNodes.ContainsKey(node.NodeId))
            {
                Hook(node);

                if (node.CraftScript == null)
                {
                    settleOnNextLoad.Add(node.NodeId);
                }
            }
        }

        private void OnCraftNodeRemoved(CraftNode node)
        {
            if (node == null)
            {
                return;
            }

            Unhook(node);
            settleOnNextLoad.Remove(node.NodeId);
        }

        private void Hook(ICraftNode node)
        {
            hookedCraftNodes[node.NodeId] = node;
            node.LoadedIntoGameView += OnCraftLoadedIntoGameView;
            node.UnloadingFromGameView += OnCraftUnloadingFromGameView;
        }

        private void Unhook(ICraftNode node)
        {
            if (node == null)
            {
                return;
            }

            node.LoadedIntoGameView -= OnCraftLoadedIntoGameView;
            node.UnloadingFromGameView -= OnCraftUnloadingFromGameView;
            hookedCraftNodes.Remove(node.NodeId);
        }

        private void UnhookAllCraftNodes()
        {
            foreach (var pair in hookedCraftNodes)
            {
                ICraftNode node = pair.Value;
                if (node == null)
                {
                    continue;
                }

                node.LoadedIntoGameView -= OnCraftLoadedIntoGameView;
                node.UnloadingFromGameView -= OnCraftUnloadingFromGameView;
            }

            hookedCraftNodes.Clear();
        }

        private void OnCraftLoadedIntoGameView(IGameViewObject source)
        {
            var node = source as ICraftNode;
            if (node == null)
            {
                return;
            }

            // 从存档/卸载状态回来的飞船做一次性补算；新造/分裂飞船只推进时间戳
            bool shouldSettle = settleOnNextLoad.Remove(node.NodeId) && BackgroundSettlement.IsEnabled;
            if (!shouldSettle)
            {
                SettleCraft(node, false);
                return;
            }

            DeferSettlement(node);
        }

        /// <summary>
        /// 延后一帧再做一次性补算。
        ///
        /// 原因：LoadedIntoGameView 触发时，零件脚本的 FlightStart / 首帧 FlightUpdate 还没执行，
        /// 而结算要用到的运行态正是在那里初始化的，例如：
        /// - `SupportLifeScript.IsHibernating`：由 HibernatingChamberScript 在 FlightUpdate 里设置；
        /// - `SupportLifeScript.IsTourist`、`evaScript`、`RadiationBeltConfig`：在 FlightStart 里设置；
        /// - 飞船大气采样 `AtmosphereSample`：由 CraftScript 首帧更新才有值。
        /// 如果立即结算，会把这些状态当成"默认值"（休眠被当成清醒、可呼吸大气被当成真空），
        /// 导致补算结果明显偏大。等一帧后这些状态就绪，补算才是对的。
        /// </summary>
        private void DeferSettlement(ICraftNode node)
        {
            if (node == null || !this.isActiveAndEnabled)
            {
                return;
            }

            StartCoroutine(SettleNextFrame(node));
        }

        private IEnumerator SettleNextFrame(ICraftNode node)
        {
            yield return null;

            if (node == null || node.CraftScript == null)
            {
                // 这一帧内飞船又被卸载/销毁了：卸载事件里已经写过时间戳，这里不用再处理
                yield break;
            }

            // 结算时再检查一次开关（这一帧内可能被关掉）；关闭时只推进时间戳
            SettleCraft(node, BackgroundSettlement.IsEnabled);
        }

        private void OnCraftUnloadingFromGameView(IGameViewObject source)
        {
            var node = source as ICraftNode;
            if (node == null)
            {
                return;
            }

            // 只推进时间戳，不结算：脚本马上就要被销毁，此时结算会与销毁前的 FlightUpdate 重叠。
            SettleCraft(node, false);
            settleOnNextLoad.Add(node.NodeId);
        }

        /// <summary>
        /// 处理一艘飞船的零件。
        /// </summary>
        /// <param name="node">飞船节点。</param>
        /// <param name="applyCalculationNow">true = 真正补算资源；false = 只把时间戳推进到当前。</param>
        private void SettleCraft(ICraftNode node, bool applyCalculationNow)
        {
            if (node == null)
            {
                return;
            }

            try
            {
                var craft = node.CraftScript;
                var parts = craft?.Data?.Assembly?.Parts;
                if (parts == null)
                {
                    return;
                }

                var now = BackgroundSettlement.CurrentGameFlightTime;
                if (now <= 0.0)
                {
                    return;
                }

                var settledCount = 0;
                var longestElapsedSeconds = 0.0;
                foreach (var t in parts)
                {
                    var modifiers = t?.PartScript?.Modifiers;
                    if (modifiers == null)
                    {
                        continue;
                    }

                    foreach (var t1 in modifiers)
                    {
                        var elapsedSeconds = SettleModifier(node, t1, now, applyCalculationNow);
                        if (elapsedSeconds > 0.0)
                        {
                            settledCount++;
                            longestElapsedSeconds = Math.Max(longestElapsedSeconds, elapsedSeconds);
                        }
                    }
                }

                if (settledCount > 0)
                {
                    Mod.Log($"[BackGroundCalculator] 飞船 {node.NodeId} 一次性补算 {settledCount} 个零件，间隔 {longestElapsedSeconds:F1}s");
                }
            }
            catch (Exception e)
            {
                Mod.LogError($"Droodism:[BackGroundCalculator] 结算飞船 {node.NodeId} 失败: {e}");
            }
        }

        /// <summary>
        /// 处理单个零件：推进时间戳，必要时一次性补算。
        /// </summary>
        /// <returns>实际补算的时间（秒）；未补算时返回 0。</returns>
        private static double SettleModifier(ICraftNode node, PartModifierScript modifier, double now, bool apply)
        {
            if (modifier == null)
            {
                return 0.0;
            }

            if (!(modifier is IBackGroudCalculate calculator))
            {
                return 0.0;
            }

            if (!(modifier.GetData() is IBackGroundData data))
            {
                return 0.0;
            }

            var lastUpdateTime = data.BackgroundLastUpdateTime;
            data.BackgroundLastUpdateTime = now;

            if (!apply || lastUpdateTime <= 0.0 || !calculator.IsBackgroundUpdateEnabled)
            {
                return 0.0;
            }

            double elapsedSeconds = now - lastUpdateTime;
            if (elapsedSeconds <= 0.0)
            {
                return 0.0;
            }

            try
            {
                calculator.BackgroundUpdate(elapsedSeconds);
                return elapsedSeconds;
            }
            catch (Exception e)
            {
                Mod.LogError(
                    $"Droodism:[BackGroundCalculator] {node.NodeId}/{modifier.GetType().Name} 后台结算失败: {e}");
                return 0.0;
            }
        }
    }
}
