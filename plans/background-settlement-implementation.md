# Droodism 后台结算系统（已落地实现）

> 本文档描述 **实际代码** 的实现，不是设想。
> 相关旧文档：`kerbalism-background-porting-analysis.md`（概念分析，仍可参考）、
> `kerbalism-sr2-systematic-review.md`（其中"Phase 1 已完成"的说法与仓库不符，已在其顶部加更正说明）、
> `sr2-craft-state-machine-analysis.md`（"状态 2/4 停摆"的说法不准确，见下文）。
>
> 代码位置：`Assets/Scripts/Droodism/BackGround/`

---

## 1. 结论先说：接口放 Script，时间戳放 Data

| | PartModifierData（Data） | PartModifierScript（Script） |
|---|---|---|
| 生命周期 | 随船存档持久化，跨卸载/跨存档存在 | 只在飞船位于游戏视图内时存在，离开视图即销毁 |
| 运行时能力 | 只有 `Part`（PartData）与序列化字段 | `PartScript`、`CraftScript`、`IFuelSource`、`GetModifier<>`、MonoBehaviour 生命周期 |
| 序列化 | `[SerializeField] + [PartModifierProperty]` 可存盘 | 不能序列化 |
| 适合放什么 | **持久化状态**（上次结算时间、资源存量） | **行为**（补算逻辑） |

**所以：**

- `IBackGroudCalculate`（行为契约）→ **Script**
- `IBackGroundData`（`BackgroundLastUpdateTime` 时间戳）→ **Data**

一句话：**结算要用运行时对象，只有脚本有；时间戳必须活过脚本销毁，只有数据能。**

---

## 2. 已实现的类型

| 文件 | 作用 |
|---|---|
| [`Interface/IBackGroudCalculate.cs`](../Assets/Scripts/Droodism/BackGround/Interface/IBackGroudCalculate.cs) | 脚本侧契约：`IBackGroudCalculate`（是否参与 + `BackgroundUpdate(elapsedSeconds)`） |
| [`BackgroundPartModifierData.cs`](../Assets/Scripts/Droodism/BackGround/BackgroundPartModifierData.cs) | 数据侧契约 `IBackGroundData` + 它的基类实现（同一文件）：自带序列化时间戳，并在 `GenerateStateXml`（存档/卸载写盘）时推进时间戳 |
| [`BackgroundSettlement.cs`](../Assets/Scripts/Droodism/BackGround/BackgroundSettlement.cs) | 后台工具：`IsEnabled` / `CurrentTime` / `Stamp`（只记录不结算） / `LimitSeconds`（大时间步长下按可用量限制可结算秒数） |
| [`BackGroundCalculator.cs`](../Assets/Scripts/Droodism/BackGround/BackGroundCalculator.cs) | 调度器：只负责"进入视图时一次性补算 / 离开视图时记录时间戳"，无逐帧轮询、无定时器 |
| [`ResourceProcessorPartScript.cs`](../Assets/Scripts/Craft/Parts/Modifiers/ResourceProcessorPartScript.cs) | 资源处理器基类：默认实现接口，子类只覆盖 `BackgroundLogic(double)`，可用 `LimitBackgroundSeconds` |
| [`SupportLifeScript.cs`](../Assets/Scripts/Craft/Parts/Modifiers/SupportLifeScript.cs) | 生命维持：实时/后台共用 `UpdateLifeSupport(deltaSeconds, isBackground)` |
| [`SupportLifeData.cs`](../Assets/Scripts/Craft/Parts/Modifiers/SupportLifeData.cs) | `IBackGroundData` 映射到已有的 `LastLoadTime`（存档兼容，不新增字段），同样在 `GenerateStateXml` 时推进 |

已接入后台结算的零件：

| 零件 | 说明 |
|---|---|
| `SupportLifeScript` | 氧气/水/食物消耗、CO₂/废水/固废产生、辐射剂量累计 |
| `CarbonDeoxideFilterScript` | CO₂ + 电力 |
| `ElectrolyticDeviceScript` | 水 + 电 → 氧气（+ 氢气副产物） |
| `MethaloxGeneratorScript` | 电池 + 高压 CO₂ + 水 → 甲烷液氧 |
| `SewageTreatDeivceScript` | 废水 + 电 → 水 |

---

## 3. 触发边界（关键设计：时间戳 + 瞬时补算）

> **后台不做任何持续计算。** 没有逐帧扫描、没有定时器、没有"每 N 秒结算一次"。
> 补算只发生在飞船重新进入游戏视图的那一瞬间，且只发生一次。

```
时间戳写入（不结算）：
  1) 零件数据被序列化时（存档 / 卸载写盘）
       └─ BackgroundPartModifierData.GenerateStateXml → Stamp(now)
  2) 飞船离开游戏视图时（UnloadingFromGameView）
       └─ BackGroundCalculator → Stamp(now)

一次性补算（唯一的后台计算）：
  飞船进入游戏视图（LoadedIntoGameView）
       └─ 延后一帧（等 FlightStart / 首帧 FlightUpdate 跑完，休眠/大气等运行态就绪）
       └─ elapsed = 当前时间 - 时间戳
       └─ BackgroundUpdate(elapsed)   ← 一次算完，然后时间戳=当前
       └─ 若是分裂/对接产生的新飞船（零件来自实时飞船），只写时间戳不补算

飞船在视图内：
   └─ 各零件自己的 FlightUpdate 按帧结算（这是实时逻辑，不属于后台计算）
```

事件挂钩本身也是一次性的：飞行场景 `Initialized` 后注册到所有飞船节点，
之后靠 `FlightState.CraftNodeAdded/CraftNodeRemoved` 维护——**没有 Update 轮询**。

**为什么不用 `PhysicsEnabled` / `PhysicsDisabled`：**

反编译确认 `CraftScript.EnablePhysics` 只切换 Rigidbody 运动学并调用回调，**不会注销零件脚本**；
物理关闭但飞船仍在游戏视图内时，零件脚本的 `FlightUpdate` 仍每帧执行。
若在物理恢复时 "从上次物理关闭起算" 补算，就会把这段已实时消耗的时间**再扣一遍**。
旧版 `SupportLifeScript.OnPhysicsEnabled` 的追赶结算正是这种写法，本次已改为由调度器按 GameView 边界驱动。

---

## 4. 怎么接入一个新零件（3 步）

以任意 `ResourceProcessorPartScript<T>` 子类为例：

```csharp
// 1) Data：换成后台结算基类（自带时间戳）
public class XxxData : BackgroundPartModifierData<XxxScript> { ... }

// 2) Script：把"按帧"逻辑改成"按秒"纯函数（前后台共用同一套速率）
protected override void WorkingLogic(in FlightFrameData frame)
{
    BackgroundLogic(frame.DeltaTimeWorld);
}

protected override void BackgroundLogic(double elapsedSeconds)
{
    // 先按可用输入/输出容量把本步秒数限制住（大时间步长下防止账目不平）
    double seconds = elapsedSeconds;
    seconds = LimitBackgroundSeconds(seconds, source.TotalFuel, ratePerSecond);
    if (seconds <= 0.0) return;

    source.RemoveFuel(ratePerSecond * seconds);
    target.AddFuel(generationPerSecond * seconds);
}

// 3) Script：告诉调度器什么时候允许补算
public override bool IsBackgroundUpdateEnabled => PartScript.Data.Activated;
```

对于不继承 `ResourceProcessorPartScript` 的脚本（如 `SupportLifeScript`）：

1. 脚本声明 `: ..., IBackGroudCalculate`，实现 `IsBackgroundUpdateEnabled` 与 `BackgroundUpdate(double)`；
2. 对应的 Data 声明 `: ..., IBackGroundData`（新 Data 直接继承 `BackgroundPartModifierData<T>`；
   老 Data 若已有时间字段就做映射，避免改存档格式）；
3. 补算逻辑严禁使用 `frame.DeltaTimeWorld` / `Time.deltaTime` / 动画，只按秒计算。

---

## 4.5 生命维持（SupportLife）的老逻辑替换清单

旧实现把"未加载结算"和"实时消耗"写成了两套代码（`RemoveFuelAmountInstantly`/`AddWastedAmountInstantly` vs `ConsumptionLogic`），
系数已经出现偏差（旧后台逻辑漏了跑步 1.75 倍率、废物按"请求量"而非"实际消耗量"计算）。本次统一为：

| 旧逻辑 | 现在 |
|---|---|
| `RemoveFuelAmountInstantly()` / `AddWastedAmountInstantly()` | 删除；由 `BackgroundUpdate` → `UpdateLifeSupport(elapsedSeconds, isBackground: true)` 统一处理 |
| `ConsumptionLogic(in FlightFrameData)` | `ConsumptionLogic(double deltaSeconds, bool allowDamage, bool isRunning)`，实时/后台共用 |
| `ConsumeInputResource(..., frame, ...)` 只返回 bool | `ConsumeInputResource(..., deltaSeconds, ...)` 先扣 craft 油箱再扣本地 buffer，**返回实际消耗量** |
| `ProduceWasteResource(..., frame, ...)` 直接 `AddFuel(amount)` 不管容量 | 先填 craft 油箱（按剩余容量），再填本地 buffer（按剩余容量） |
| `AutoRefillLogic(frame)` 仅实时 | `AutoRefillLogic(deltaSeconds)` 实时/后台共用（在可呼吸大气里离视图也会补氧） |
| `OnPhysicsDisabled` / `OnPhysicsEnabled` 订阅与追赶结算 | 全部删除；边界由 `BackGroundCalculator` 按 GameView 加载/卸载统一驱动 |
| `OnCraftUnloaded` 里写 `Data.LastLoadTime` | 删除（时间戳由调度器通过 `IBackGroundData` 维护）；该方法只保留原有的"剥离维生燃料箱 XML"逻辑 |
| 辐射只在 `FlightUpdate` 累计 | 后台补算也会 `CheckRadiationState(elapsedSeconds)` 累计剂量；但**不在补算里判伤害/弹提示**，避免读档瞬间瞬杀 |

后台结算的伤害策略：
- 消耗不足/废物溢出：实时逻辑始终结算伤害；后台补算仅在 **`BackGroundDamage` 设置开启**时结算，且按"实际缺乏的时间"计算，不是按整段时间。
- 辐射：后台始终累计 `CumulativeRad`；辐射伤害仅在 `BackGroundDamage` 开启时补算。
- 跑步倍率：后台按"未跑步"计算（`isRunning = false`），因为离视图时真实运动状态不可知，且旧实现也不计。
- 休眠：后台先按休眠舱电量计算"最多能休眠多久"（`HibernatingChamberScript.GetBackgroundHibernationSeconds`），
  休眠期间不消耗氧气/食物/水、但要扣休眠电量并照常累积辐射；电量耗尽后剩余时间按清醒结算。
  同一休眠舱的多个乘员共享电量，静态缓存保证每帧只计算/扣电一次。

---

## 5. 设置与开关

- 设置项：`ModSettings.BackGroundCalculation`（沿用旧 `ConsumeResourceWhenUnloaded` 的存储键 `consumeResourceWhenUnloaded`，玩家已有设置不会丢）。
- 设置项：`ModSettings.BackGroundDamage`（`backGroundDamage`，默认关闭）：后台补算是否结算伤害。
  - 关闭：离视图期间资源耗尽/辐射累积都不会掉血（旧行为）。
  - 开启：按"实际缺乏/溢出时间"结算饥饿伤害，并按整段时间结算辐射伤害；<color=red>可能导致读档时乘员已受伤甚至死亡</color>。
- 关闭 `BackGroundCalculation` 时：不做补算，但时间戳仍会在存档/卸载时推进，因此重新打开后**不会**把关闭期间的时间一次性补算进来。
- 默认值：`BackGroundCalculation` 仍为 `false`（与旧行为一致），`BackGroundDamage` 为 `false`。

---

## 6. 已知限制 / 后续工作

| 项 | 说明 | 优先级 |
|---|---|---|
| **远程未加载飞船** | `CraftScript == null` 时没有零件数据；只能用 `IFlightStateData.LoadCraftXml` 读盘 → 模拟 → `SaveCraftXml` 写回。I/O 成本高，需要限流与写回时机设计（原 Phase 3） | 低（高风险） |
| 其余资源处理器 | 未接入：`ChemicalReactor`（多反应类型，公式可照抄）、`PhotoBioReactor`（依赖光照状态）、`Water_Desalination`（依赖 `WaterPhysics.IsInWater`，离视图时不可知）、`MiningMachine`（依赖行星资源丰度）、`LifeSupportGenerator`（依赖游戏 `GeneratorScript` 的燃料流，需一并补燃料消耗）、`Sodium_Peroxide`（`oxygenGeneratedAmount` 未持久化，需先改成存档字段）。可照第 4 节逐个接入 | 中 |
| 辐射补算 | 已按"当前轨道位置的剂量率 × 经过时间"近似累计；若离视图期间跨越辐射带边界，会与实际不符。需要精确时可按时间步长重算轨道位置（成本高） | 低 |
| 休眠中途切换 | 时间戳只能表达"一个区间"，若小蓝人在离视图期间被其他逻辑切换休眠状态，无法精确分段；当前策略是"结算时按休眠舱电量推算可休眠时长，剩余时间按清醒结算" | 低 |
| 命名拼写 | `IBackGroudCalculate` / `BackGroundCalculator` 为历史拼写；如需改成 `IBackgroundCalculate` 要同步改文件名与 `.meta` | 低 |
| 旧文档修正 | `kerbalism-sr2-systematic-review.md` 的"Phase 1 已完成"、`sr2-craft-state-machine-analysis.md` 的"状态 2/4 停摆"均与代码不符 | 文档 |

---

## 7. 测试清单（建议在游戏内逐项验证）

1. **开关关闭**：离视图再回、warp 前后，资源与旧版本行为一致（不补算）。
2. **开关打开 + 生命维持**：飞船在轨道上离开视图 5 分钟，回来时 O₂/水/食物按速率减少、CO₂/废水/固体废物按比例增加。
3. **warp 期间**：不出现重复扣资源（旧实现中 warp 前后由 `FlightUpdate` 实时消耗，补算不应再算一遍）。
4. **休眠**：休眠状态下离开/返回视图，不消耗 O₂/水/食物；休眠舱电量按"休眠时长 × 每秒耗电"扣除；电量不够睡满整段时间时，剩余时间应按清醒状态消耗资源。
5. **后台伤害设置**：`BackGroundDamage` 关闭时，离视图期间资源耗尽/辐射超标都不掉血；开启后应按"实际缺乏时间"补伤害，并可能出现读档即伤/死的预期结果。
5. **CO₂ 过滤器**：激活状态下离开视图一段时间，CO₂ 与电池电量按速率减少；关掉激活则不减少。
6. **电解装置 / 甲烷发生器 / 污水处理**：激活状态下离开视图一段时间，输入按速率减少、输出按比例增加；某个储罐先耗尽时应停在耗尽那一刻，而不是继续按整段时间产出。
7. **飞行中存档 → 读档**：读档后不应出现"突然少一大截资源"（时间戳在存档序列化时推进，理论上误差≈0）。
8. **多飞船**：两艘都在视图中、其中一艘离开视图再回来，只有离开的那艘补算。
9. **性能**：后台没有任何逐帧/定时任务，飞行中 CPU 占用应与未开该功能时一致（事件只在飞船加载/卸载时触发一次）。
10. **辐射**：在辐射带内离开视图一段时间再回来，`CumulativeRad` 应相应增加，但不应出现"读档瞬间掉血/弹伤害提示"。
11. **日志**：DebugMode 打开时，飞船重新进入视图应看到 `[BackGroundCalculator] 飞船 N 后台补算 X 个零件，最长间隔 Ys`，用于核对补算是否发生、时间是否合理。
