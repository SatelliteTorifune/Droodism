# Kerbalism 后台系统移植到 SR2 的系统性审查报告

> 参考来源：
> - Kerbalism: [`BackGround.cs`](C:/renko/unityProjects/Kerbalism/src/Kerbalism/BackGround.cs)
> - KSP 源码: [`FlightGlobals.cs`](C:/renko/unityProjects/kspre2/Assets/Scripts/Assembly-CSharp/FlightGlobals.cs)
> - SR2 反编译: [`C:/renko/shitProgram/jnoCode`](C:/renko/shitProgram/jnoCode)
> - SR2 GameLoop 接口: [`IFlightFixedUpdateWarp`](C:/renko/shitProgram/jnoCode/ModApi/GameLoop/Interfaces/IFlightFixedUpdateWarp.cs)、[`IFlightUpdate`](C:/renko/shitProgram/jnoCode/ModApi/GameLoop/Interfaces/IFlightUpdate.cs)、[`FlightFrameData`](C:/renko/shitProgram/jnoCode/ModApi/GameLoop/FlightFrameData.cs)
> - Droodism Mod: [`BackGroundCalulator`](Assets/Scripts/Droodism/BackGround/BackGround.cs)、[`SupportLifeScript`](Assets/Scripts/Craft/Parts/Modifiers/SupportLifeScript.cs)

---

## 1. SR2 与 Kerbalism/KSP 的关键设计对应表

### 1.1 飞船/Vessel 管理

| Kerbalism / KSP | SR2 | 对应状态 |
|----------------|-----|---------|
| [`FlightGlobals.Vessels`](C:/renko/unityProjects/kspre2/Assets/Scripts/Assembly-CSharp/FlightGlobals.cs:192) — 所有飞船 | [`IFlightState.CraftNodes`](C:/renko/shitProgram/jnoCode/ModApi/Flight/IFlightState.cs:14) — 所有飞船 | ✅ 等价 |
| [`FlightGlobals.VesselsLoaded`](C:/renko/unityProjects/kspre2/Assets/Scripts/Assembly-CSharp/FlightGlobals.cs:200) — 已加载 | `CraftNode.CraftScript != null`（有脚本即加载） | ✅ 可推断 |
| [`FlightGlobals.VesselsUnloaded`](C:/renko/unityProjects/kspre2/Assets/Scripts/Assembly-CSharp/FlightGlobals.cs:208) — 未加载 | `CraftNode.CraftScript == null`（无脚本即未加载） | ✅ 可推断 |
| [`FlightGlobals.AddVessel()`](C:/renko/unityProjects/kspre2/Assets/Scripts/Assembly-CSharp/FlightGlobals.cs:456) | [`FlightState.AddCraftNode()`](C:/renko/shitProgram/jnoCode/SimpleRockets2/Assets/Scripts/State/FlightState.cs:341) + `CraftNodeAdded` 事件 | ✅ 等价 |
| [`GameEvents.onVesselCreate`](C:/renko/unityProjects/kspre2/Assets/Scripts/Assembly-CSharp/GameEvents.cs) | [`FlightState.CraftNodeAdded`](C:/renko/shitProgram/jnoCode/SimpleRockets2/Assets/Scripts/State/FlightState.cs:111) | ✅ 等价 |
| [`GameEvents.onVesselDestroy`](C:/renko/unityProjects/kspre2/Assets/Scripts/Assembly-CSharp/GameEvents.cs) | [`FlightState.CraftNodeRemoved`](C:/renko/shitProgram/jnoCode/SimpleRockets2/Assets/Scripts/State/FlightState.cs:116) | ✅ 等价 |
| **ProtoPartSnapshot**（未加载飞船的零件快照） | **❌ 缺失** — `CraftScript == null` 时无零件数据 | 🔴 致命缺失 |

### 1.2 游戏循环 / 时间系统

| Kerbalism / KSP | SR2 | 对应状态 |
|----------------|-----|---------|
| `KSPGameLoop.FixedUpdate` | [`IFlightFixedUpdate`](C:/renko/shitProgram/jnoCode/ModApi/GameLoop/Interfaces/IFlightFixedUpdate.cs) | ✅ 等价 |
| `TimeWarp.FixedUpdate`（warp 时调用的物理更新） | [`IFlightFixedUpdateWarp`](C:/renko/shitProgram/jnoCode/ModApi/GameLoop/Interfaces/IFlightFixedUpdateWarp.cs) | ✅ 等价 |
| `Planetarium.GetUniversalTime()` — 游戏世界时间 | [`IFlightState.Time`](C:/renko/shitProgram/jnoCode/ModApi/Flight/IFlightState.cs:30) | ✅ 等价 |
| `Time.deltaTime * warpMultiplier` | [`FlightFrameData.DeltaTimeWorld`](C:/renko/shitProgram/jnoCode/ModApi/GameLoop/FlightFrameData.cs:17) | ✅ 等价（已含 warp 倍率） |
| `Vessel.protoVessel`（未加载飞船的持久化数据） | **❌ 缺失** — 无 `ProtoVessel` 等价物 | 🔴 致命缺失 |

### 1.3 资源系统

| Kerbalism / KSP | SR2 | 对应状态 |
|----------------|-----|---------|
| `VesselResources`（中央资源管理器） | **`IFuelSource`**（分散的燃料源接口） | 🟡 不直接对应 |
| `ResourceInfo`（单资源句柄，支持 Produce/Consume） | `IFuelSource.TotalFuel / AddFuel() / RemoveFuel()` | 🟡 功能较少 |
| `ResourceRecipe`（批量资源操作） | **❌ 缺失** | 🟡 可手动实现 |
| `ResourceBroker`（记录谁用了资源） | **❌ 缺失** | 🟢 非必需 |
| `PartResource`（零件级别的资源存储） | `FuelTank`（零件级别的燃料箱） | 🟡 概念等价但 API 不同 |

### 1.4 模块/零件系统

| Kerbalism / KSP | SR2 | 对应状态 |
|----------------|-----|---------|
| `PartModule`（零件模块基类） | `PartModifierData` / `PartModifierScript` | 🟡 概念等价 |
| `ProtoPartModuleSnapshot`（模块的持久化快照） | `PartModifierData` 的 `[SerializeField]` 字段 | 🟡 SR2 用序列化字段代替快照 |
| `PartLoader.getPartInfoByName()` | `PartType` 系统 | 🟡 不同 API |

### 1.5 物理状态管理

| Kerbalism / KSP | SR2 | 对应状态 |
|----------------|-----|---------|
| Vessel 加载/卸载事件 | [`ICraftNode.PhysicsDisabled` / `PhysicsEnabled`](C:/renko/shitProgram/jnoCode/ModApi/Craft/ICraftNode.cs:22) | ✅ 等价 |
| `Vessel.loaded` 属性 | `CraftNode.CraftScript != null` | ✅ 可推断 |
| 物理加载原因 | [`PhysicsChangeReason`](C:/renko/shitProgram/jnoCode/ModApi/Flight/GameView/PhysicsChangeReason.cs) 枚举 | ✅ 完整 |

---

## 2. 架构冲突点分析

### 2.1 🔴 致命冲突：SR2 没有未加载飞船的零件数据

**这是最关键的问题。**

在 KSP 中，所有飞船（无论加载与否）都完整保存在内存中：
```
Vessel
├── protoVessel
│   ├── protoPartSnapshots[]     ← 所有零件的快照（包括未加载的）
│   │   ├── ProtoPartSnapshot
│   │   │   ├── 零件数据
│   │   │   ├── resources[]      ← 资源存量
│   │   │   └── modules[]        ← 模块快照
│   │   │       └── ProtoPartModuleSnapshot
│   │   │           └── moduleValues（键值对持久化）
```

**在 SR2 中，未加载的飞船只有轨道数据：**
```
ICraftNode
├── CraftScript (null if unloaded)  ← 没有脚本，就没有零件数据！
├── OrbitData (轨道数据)
└── NodeId (唯一ID)
```

当 `CraftScript == null` 时，你无法获取：
- `CraftScript.Data.Assembly.Parts`（零件列表）
- `PartData.GetModifier<SupportLifeData>()`（生命维持数据）
- `IFuelSource`（燃料源）

**这意味着：** 当前 `SimulateCrewBackground()` 遍历 `FlightState.CraftNodes` 时，只有玩家当前飞船（以及附近已加载的飞船）能被模拟。远程飞船没有零件数据，直接跳过。

**解决方案方向：**
- `IFlightStateData.LoadCraftXml(nodeId)` 可以从硬盘加载飞船的 XML 存档
- 解析 XML 获取零件数据 → 提取 `SupportLifeData` → 模拟消耗
- 但这是个高开销操作（I/O + XML 解析），不适合每帧调用

### 2.2 🟡 中等冲突：资源系统架构差异

Kerbalism 的资源系统是**集中式**的：
```
Background.Update()
  → 遍历所有模块
  → 每个模块 Produce/Consume 到 VesselResources
  → VesselResources 统一结算
```

Droodism 的资源系统是**分散式**的：
```
SupportLifeScript.FlightUpdate()
  → 直接操作 IFuelSource（加/减燃料）
  → 每个 SupportLifeScript 独立操作自己的燃料源
  → 没有中央结算
```

**问题：** 当多个后台模块同时操作同一燃料源时，可能出现竞态问题。例如两个小蓝人共享同一个氧气箱。

**解决方案：** 可以保留分散模式，因为 SR2 中每个小蓝人绑定到不同的 `IFuelSource`（通过 `STCommandPodPatchScript`），天然隔离。

### 2.3 🟢 低冲突：时间加速处理

SR2 的 `IFlightFixedUpdateWarp` 恰好就是 Kerbalism 所需的后台更新入口。`FlightFrameData.DeltaTimeWorld` 已经包含了 warp 倍率。这个机制是**天然适配**的。

### 2.4 🟡 中等冲突：CraftScript 加载状态切换

当飞船进入/离开物理范围时：
1. `PhysicsDisabled` 事件触发（卸载）
2. `PhysicsEnabled` 事件触发（加载）

**问题：** 当前的 `BackGroundCalulator.OnCraftPhysicsDisabled` 只记录了时间戳，但 `OnCraftPhysicsEnabled` 触发追赶结算时，`CraftScript` 已经可用。而如果在卸载期间需要结算（比如 Mod 设置要求在卸载时结算），则无法操作 CraftScript。

**解决方案：**
- 卸载时：使用 `_lastBackgroundTimeByCraft[id]` 记录时间
- 加载时：读取时间差，用 `BackgroundUpdate(elapsed_s)` 追赶
- 这就是当前实现的方式 ✅

### 2.5 📋 场景切换

| 场景切换 | KSP | SR2 |
|---------|-----|-----|
| 飞行→设计器 | 离开飞行场景，保存游戏 | [`SceneTransitionCompleted`](C:/renko/shitProgram/jnoCode/ModApi/Scenes/Events) 事件 |
| 设计器→飞行 | 加载飞行场景 | 同上 |
| 发射新飞船 | 添加到 `Vessels` | 添加到 `CraftNodes`，触发 `CraftNodeAdded` |

**冲突点：** SR2 的场景切换会销毁 `FlightScene`，所有 GameLoop 注册的组件需要重新注册。当前 `BackGroundCalulator` 通过 `OnSceneTransitionCompleted` 处理了这一点 ✅。

---

## 3. 移植/适配方案建议

### 3.1 总体策略：分层适配

```
┌─────────────────────────────────────────────────────────────┐
│                     BackGroundCalulator                     │
│  （调度器：统一入口，时间管理，多飞船遍历）                    │
├─────────────────────────────────────────────────────────────┤
│                         ↓                                   │
│                    BackgroundUpdate(elapsed_s)              │
│                 （支持任意时间长度的核心算法）                  │
├─────────────────────────────────────────────────────────────┤
│                         ↓                                   │
│  ┌──────────────────┐  ┌──────────────────┐                 │
│  │  已加载飞船处理    │  │  未加载飞船处理    │                 │
│  │ (CraftScript存在) │  │ (CraftScript为空)│                 │
│  │                  │  │                  │                 │
│  │ 直接遍历 Parts    │  │ LoadCraftXml()   │                 │
│  │ GetModifier<>()  │  │ 解析 XML → 零件   │                 │
│  │ BackgroundUpdate │  │ 提取数据 → 模拟   │                 │
│  └──────────────────┘  └──────────────────┘                 │
└─────────────────────────────────────────────────────────────┘
```

### 3.2 三个阶段的实现方案

#### Phase 1：已加载飞船的后台模拟 ✅（当前已完成）

- [x] `BackGroundCalulator` 实现 `IFlightFixedUpdateWarp`
- [x] `SupportLifeScript.BackgroundUpdate(elapsed_s)` 方法
- [x] 已加载飞船的 warp 模拟
- [x] 物理启用/禁用的追赶结算
- [x] 多飞船动态追踪（`CraftNodeAdded/Removed`）
- [x] 避免 IFlightUpdate 和 IFlightFixedUpdateWarp 重复计算

#### Phase 2：已加载隔壁飞船的后台模拟

当玩家飞船 A 在 warp，飞船 B 也在加载范围内：
- 当前实现已覆盖 ✅（`SimulateCrewBackground` 遍历所有 `CraftNodes`）
- 需要验证：非玩家飞船在 warp 时是否也接收 `IFlightFixedUpdateWarp`？

#### Phase 3：未加载飞船的后台模拟（高风险 ⚠️）

当前缺少的部分：
1. 需要一个**缓存机制**来保存已解析的远程飞船数据（避免每帧读盘）
2. 需要一个**更新策略**来决定何时读取远程飞船的存档

**未加载飞船的缓存设计方案：**
```csharp
class UnloadedCraftCache {
    Dictionary<int, CraftCacheEntry> _cache;
    
    class CraftCacheEntry {
        int nodeId;
        double lastSimulatedTime;
        double oxygenAmount;
        double foodAmount;
        // ... 其他需要模拟的资源
    }
    
    // 从 XML 加载并解析
    CraftCacheEntry LoadFromXml(int nodeId) { ... }
    
    // 模拟并更新缓存
    void SimulateAndCache(int nodeId, double elapsed) { ... }
    
    // 当飞船加载时，将缓存数据写回 PartModifierData
    void ApplyToCraft(int nodeId, ICraftScript script) { ... }
}
```

### 3.3 最高风险模块

| 模块 | 风险等级 | 理由 |
|------|---------|------|
| **未加载飞船零件数据访问** | 🔴 极高 | `CraftScript == null` 时无 API 直接获取零件数据 |
| `IFlightStateData.LoadCraftXml()` | 🔴 高 | XML I/O 性能开销未知，频繁调用可能导致卡顿 |
| **远程飞船资源同步** | 🔴 高 | 远程模拟的数据如何写回飞船存档？时机和一致性难保证 |
| `BackGroundCalulator` GameLoop 注册 | 🟡 中 | 场景切换时 FlightScene 销毁/重建的时机问题 |
| **多飞船辐射计算** | 🟡 中 | 远程飞船的位置数据可用，但辐射带配置需要加载 |
| **休眠状态处理** | 🟢 低 | `IsHibernating` 标记在 SupportLifeData 中持久化 |
| **CraftNodeAdded/Removed 事件 cast** | 🟢 低 | `IFlightState` 向下转型到 `FlightState` 获取事件 |

### 3.4 推荐优先级

```
立即实施 ───────────────────────────────────────────────── 未来扩展
  │                                                          │
  ▼                                                          ▼
Phase 1 (已完成) → Phase 2 (已覆盖) → Phase 3 (未加载飞船)
                                        │
                                        ├─ 1. 远程飞船 XML 缓存机制
                                        ├─ 2. 远程资源模拟算法
                                        ├─ 3. 加载时数据写回
                                        └─ 4. 性能优化（缓存LRU、模拟频率控制）
```

---

## 4. 与 Kerbalism 的设计对比总结

```
Kerbalism                    SR2 (Droodism)
=========                    ==============
Background.Update()    ──→   BackGroundCalulator.SimulateAllCrewBackground()
  ├ 遍历 Vessels              ├ 遍历 CraftNodes
  ├ 用 ProtoPartSnapshot      ├ 用 CraftScript.Data.Assembly.Parts（仅已加载）
  ├ 所有飞船都能模拟           ├ 只有已加载飞船能模拟 ⚠️
  │                            └ 未加载的只能通过 LoadCraftXml（Phase 3）
  │
  ├ ModuleType() 分派         └ GetModifier<SupportLifeData>() 直接查找
  │                             无需反射/枚举，SR2 的类型系统更直观 ✅
  │
  ├ VesselResources           IFuelSource + LocalBuffer
  │  （中央管理）               （分散管理）
  │                             两者各有利弊 ✅
  │
  ├ elapsed_s 参数相同         BackgroundUpdate(elapsed_s) 复用 ✅
  │
  └ 无 GameLoop 接口           IFlightFixedUpdateWarp 天然适配 ✅
     KSP 有自己的调用时机        SR2 的接口更清晰
```

### 一句话结论

**已加载飞船的后台模拟已完全可行（Phase 1 已完成，无需修改现有资源系统）**；未加载飞船的模拟需要读取 XML 存档来获取零件数据，这是 Phase 3 的高风险工作，建议在验证 Phase 1-2 的稳定性后再推进。
