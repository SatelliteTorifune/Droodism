# Drood 2.0 降落伞移植分析与落地方案

> 目标：把 Drood 2.0（`C:\renko\reverseEnginnering\mods\Drood20`）那套高保真降落伞系统移植进当前 Droodism，替换掉现有一堆 bug 的"生成零件式"降落伞。
> 日期：2026
> 状态：分析完成，待实施

---

## 1. 背景：两套系统的本质区别

### 1.1 d20（反编译 Drood 2.0）——寄生组件式

- 降落伞**不是零件**。它是 `MonoBehaviour`（`Drood2\DroodParachute.cs`），由 Harmony 补丁
  `AddDroodFlightComponentsPatch`（patch `EvaScript.Start`）直接 `AddComponent` 到小蓝人的
  `EvaScript` 上。
- 依赖 Harmony 运行时注入（`Mod.cs` 明确要求装 Juno Harmony）。
- 配套补丁：`Drood2ActivationGroupsPatch`（AG 组改名 "Parachute"）、`Drood2ActivationPanelPatch`
  （AG 面板加按钮）、`CalmAnimatorWhileHangingPatch`（悬挂时安抚动画器）。
- 物理：自研刚体力学（阻力 + 滑翔分量 + 伞顶速度对齐 + 扭矩稳定 + 湍流），程序化伞绳（16 根
  LineRenderer），`DroodHangingPose`（FinalIK 悬挂姿态），`DroodLandingFall`（PlayableGraph 落地摔倒）。
- 触发：AG1 激活组（按键 1 / 小键盘 1 / AG 面板按钮），Packed→Armed→Deployed→Repack 状态机，可收放复用。

### 1.2 Droodism（当前）——生成零件式

- 降落伞是**真实零件**（`DroodParachute.xml` / `Glider.xml`，`showInDesigner=false` 隐藏），
  带 `CrewCompartment` + 自研 `Droodism.Glider` modifier。
- `SupportLifeScript` 自动/手动开伞 → `SpawnParachuteAndRide()` 现场 `CreateBodyData / AddBody /
  MoveToCraft` 生成伞零件 → 小蓝人 `LoadIntoCrewCompartment` 坐进伞的乘员舱 →
  `GliderScript` 手调系数算阻力 → 落地 `ExplodePart` 炸掉伞。
- 一次性消耗，无法复用；`SupportLifeScript` 源码里的注释也承认这套东西"诡异、一坨屎"。

### 1.3 关键差异对照表

| 维度 | d20（Drood 2.0） | Droodism（当前） |
|---|---|---|
| 载体 | Eva 上的运行时组件 | 运行时生成的隐藏零件 |
| 触发 | AG1 激活组 + 按键 1 + 面板按钮 | 乘员面板按钮 / 自动开伞 |
| 状态机 | Packed→Armed→Deployed→Repack，可复用 | 一次性：开伞→落地→炸伞 |
| 物理 | 自研刚体力学：Cd·面积·ρ·v²、6G 开伞过载封顶、滑翔分量、扭矩稳定、湍流 | 手调标量：`OpenPercent·kDrag·v·|v|·4·2` 纯阻力，无稳定 |
| 视觉 | 程序化伞衣 + 16 根伞绳 + 伞顶朝向 + 塌缩动画 | 静态 prefab 伞衣 localScale 动画，无伞绳 |
| 姿态 | FinalIK 悬挂（双手抓绳、腿摆动）+ 落地摔倒 | 坐着（CrewCompartment + 手部 IK） |
| 配置 | 常量硬编码，玩家不可调 | 设计器属性（ChuteType/MinDeployHeight/自动开伞），可本地化 |
| 持久化 | 状态在运行时组件，读档后重置 | 伞零件进 assembly，随船存档持久化 |

---

## 2. 移植技术难点（按难度从高到低）

### 🔴 难点1：HardLanding 动画资产缺失 —— 已解决（见 §3）

`DroodLandingFall.cs` 里 `LoadAsset<AnimationClip>("Assets/Animations/HardLanding.fbx")` 引用的
动画不在反编译源码里。**已在 `mods/Drood20` 包中找到并确认可用**（见 §3）。

### 🔴 难点2：和现有"生成零件式"系统的冲突

两套系统不能同时生效，否则双份阻力、双份视觉、乘员被坐两次。必须做模式切换：

- `ChuteType == "Parachute"` → 走 d20 组件系统，**删掉/禁用 `SpawnParachuteAndRide` 路径**
- `ChuteType == "ParaGlider"` → 保留现有 `Glider` 零件系统（可操控滑翔伞，d20 没有）
- `ChuteType == "None"` → 都不启用

### 🔴 难点3：激活组补丁作用域必须重写

d20 的 `Drood2ActivationGroupsPatch.Ensure()` 硬编码 `PartType.Id == "Drood2"`。Droodism 里小蓝人是
原版 `Eva`/`Eva-Tourist`（每船都有），不能给每个小蓝人注入 "Parachute" AG 按钮。必须改成：

- 仅当该 EVA 有 `SupportLifeData` 且 `ChuteType != "None"` 才注入
- 时序：需在 `EvaScript.OnInitialized`/`Start` 拿到 modifier（`PartAddingManager.PatchCraft` 先于
  Start 执行，应该能拿到，需实测）
- `Drood2ActivationPanelPatch` 同理，作用域判断写死了 `== "Drood2"`

### 🟠 难点4：存档/读档的伞状态丢失

d20 的伞状态全在运行时组件里，读档后重新挂上 = 全新状态（Packed）。即**空中展开一半存档，读档后伞
会收起来再开**。而现有"生成零件"方案反而能持久化（伞零件进 assembly 会存下来）。

处理：把 `_state/_extend/_inflate/_collapse/_canopyForward` 存进 `SupportLifeData` 的
`[PartModifierProperty]` 字段（Droodism 已有此模式），`Awake` 时恢复；或接受"读档重开"行为。

### 🟠 难点5：和 Droodism 的 Ragdoll 死亡系统打架

Droodism 有 `RagdollModifierScript` + `EvaScriptPatch`（ragdoll 激活时 skip `UpdateMovement` 等一串）。
d20 的悬挂 IK、落地 PlayableGraph、`FreezeMovementDuringLandingFallPatch` 都直接操作骨架/动画。

必须加守卫：`RagdollModifierScript.Data.EnableRagdoll` 为真时 → 关伞力、关悬挂姿态、不触发落地摔倒。

### 🟡 难点6：反射补丁的脆弱性

- `Drood2ActivationPanelPatch`：`AccessTools.Method(...ActivationPanelController:CraftNodeChanged)` +
  `Traverse` 反射私有字段。字段名/签名变了就静默失效（有 warning 兜底，按钮不显示，不致命）。
- `DroodLookUp`：`AccessTools.FieldRefAccess<CameraVantageScript, Transform>("_cameraPosition")` 静态
  只读字段——当前游戏字段还在（已核实），但**一旦改名会抛静态初始化异常，连带 `harmony.PatchAll` 挂掉**，
  并触发 `Mod.cs` 那个误导性的 "你他妈加 Juno Harmony 了吗?" 弹窗。需改成容错访问，或先不搬 `DroodLookUp`
  （它不是伞的核心功能）。

### 🟡 难点7：部署条件/手感调参差异

- d20：展开要求 `空气密度 ≥ 0.6`、速度 ≤ 320 m/s，充气要求 `密度 ≥ 0.96`
- Droodism：`密度 > 0.01`、速度 < 音速
- 直接搬过来，稀薄大气星球（火星类）伞就是开不了，玩家会当成 bug。需决策：保留 d20 物理阈值但复用
  Droodism 的本地化提示（`CannotDeployParachute*`）+ 自动开伞逻辑；或放宽阈值。

### 🟢 低难度项（基本是搬代码）

- `DroodParachute.cs` ~640 行自包含，几乎是直接复制（改走 `Mod.Log` 门控 + 适配 Eva 判定）
- `AddDroodFlightComponentsPatch` 判定从 `IsDroodPartType` 改成"有 SupportLife 且 ChuteType == Parachute"
- 伞衣 `Resources.Load("Craft/Parts/Prefabs/Parachutes/ParachuteStripedVertical")` 和
  `PartMaterialScript.AddRenderer` 当前游戏都在
- `CalmAnimatorWhileHangingPatch` 目标 setter（`CrewAnimController.InAir/Speed/...`）已核实存在
- 输入：`Inputs.ActivationGroup1` 存在，d20 的守卫（打字框/弹窗判断）保留

---

## 3. Drood20 mod 包资产盘点（`C:\renko\reverseEnginnering\mods\Drood20`）

### 3.1 包内容

```
mods/Drood20/
├── Drood 20.sr2-mod       282 KB   游戏实际分发压缩包(SimpleRockets2CompressedModFileV001)
├── Drood20.unity3d        566 KB   UnityFS bundle, Unity 2022.3.62f3(游戏 1.4.105.0)
├── extracted/
│   ├── AnimationClip_mixamo.com.raw.bin     81 KB  ★ HardLanding 动画
│   ├── Avatar_HardLandingAvatar.raw.bin      37 KB
│   ├── TextAsset_Drood 2.0.dll.bin          112 KB  ★ 原版 mod DLL
│   ├── TextAsset_ModManifest.xml.bin
│   ├── Drood2.clean.dll                     112 KB  反编译后重编译的干净 DLL
│   └── _manifest.txt                        bundle 全部 138 个对象清单
└── decompiled/                               与 d20_decompiled 相同的反编译源码
```

### 3.2 ★ 可用资产：HardLanding 动画（解决难点1）

`Drood20.unity3d` 里的 `AnimationClip "mixamo.com"`（81,660 B，humanoid 肌肉空间、30fps、2.133s、
200 条肌肉曲线）就是 `DroodLandingFall` 需要的动画。Manifest 的 OtherAssets 只声明了这一个资源 +
DLL，整个 mod 资产面就这么点。

| 方案 | 做法 | 代价 |
|---|---|---|
| **A. 运行时直接加载 bundle（最快）** | 把 `Drood20.unity3d` 随 mod 分发，`AssetBundle.LoadFromFile` + `LoadAsset<AnimationClip>("mixamo.com")` | 零转换；与当前游戏 Unity 版本匹配；游戏升 Unity 后可能失效 |
| **B. 转成 .anim/.fbx 进项目（最干净）** | AssetStudio 导出 `HardLanding.fbx`（`AssetStudioFBXWrapper.dll` 已在 `tools\sr2tool\bin`），放 `Assets/Content/...`，`Mod.Instance.ResourceLoader.LoadAsset<AnimationClip>("Assets/Animations/HardLanding.fbx")` 与 d20 原代码一致 | 需给 sr2tool 加导出命令 |
| C. 手动解析 raw bin 重建曲线 | — | 工作量大，不推荐 |

肌肉空间动画绑到小蓝人自己的 Animator/Avatar 即可，**bundle 里的 `HardLandingAvatar` 用不上**。

### 3.3 ★ 可用资产：原版 DLL

`TextAsset_Drood 2.0.dll.bin`（112,260 B）为权威二进制。已抽查确认 `DroodParachute` /
`DroodHangingPose` / `Drood2ActivationGroupsPatch` 等类型都在——**`decompiled/` 反编译源码与发布版一致**，
可放心照搬。`Drood2.clean.dll` 是重编译产物，别当原始二进制。

### 3.4 其他

- `.sr2-mod` 完整包可原样装进游戏当**参照物**，边移植边对比手感（伞绳视觉、悬挂姿态、落地摔倒）。
- `C:\renko\reverseEnginnering\docs\SR2_Mod_Unpack_Guide.md` 已记录拆包流程。
- `tools\sr2tool`：自定义 AssetStudio 拆包工具（含 `--reflect` 模式），可扩展用于导出 .fbx/.anim。

---

## 4. 移植前置条件核对（已全部确认）

| 依赖 | 状态 |
|---|---|
| Harmony | ✅ Droodism 已用（`Mod.cs` `PatchAll` + `EvaScriptPatch`） |
| FinalIK | ✅ `GliderScript` 已用 `FullBodyBipedIK` |
| 骨骼名（LeftShoulder/RightShoulder/LeftHand…） | ✅ d20 的 Drood2 零件复用原版 `Eva.prefab`，当前小蓝人模型同名骨骼存在 |
| 原版 Eva 带 CommandPod + 激活组 | ✅ Parts.xml `activationGroupNames=""` |
| `CommandPodScript.GetActivationGroupState` | ✅ 存在 |
| `GameInputs.ActivationGroup1` | ✅ 存在 |
| `CraftNode.RecalculateFrameState(IReferenceFrame)` | ✅ 存在 |
| `CrewAnimController` 各 setter | ✅ 存在 |
| `ActivationPanelController._buttons/_noActivationGroupsText` | ✅ 存在 |
| `CameraVantageScript._cameraPosition` | ✅ 存在（但仍建议容错） |
| `Game.ResourceLoader` | ✅ 存在 |
| 伞衣资源 `ParachuteStripedVertical` | ✅ 当前游戏存在 |
| HardLanding 动画 | ✅ 已找到（§3.2） |

---

## 5. 落地顺序（分阶段）

1. **Phase 1（核心，替换屎山）**
   - 搬 `DroodParachute` 组件 + `AddDroodFlightComponentsPatch`（适配 Eva + ChuteType 判定）
   - 激活组 Ensure 条件化（仅 Parachute 类型小蓝人注入）
   - `ChuteType == "Parachute"` 禁用旧 spawn 路径；`ParaGlider` 保留现有系统
   - 接入 HardLanding 动画（先方案 A 跑通，后方案 B）
2. **Phase 2（表现）**
   - 搬 `DroodHangingPose` + `CalmAnimatorWhileHangingPatch`（骨骼名已核实）
3. **Phase 3（着陆）**
   - 搬 `DroodLandingFall` + `FreezeMovementDuringLandingFallPatch`（动画资产已就位）
4. **Phase 4（收尾）**
   - 空中读档持久化（写进 `SupportLifeData` `[PartModifierProperty]`）
   - 部署条件调参 + 复用本地化提示 + 自动开伞
   - `Mod.Log` 门控、加 ragdoll 守卫
   - `DroodLookUp`/`DroodTeleportFix` 视情况（先做容错反射）

### 关键文件清单

- 待移植（来自 `mods/Drood20/decompiled/Drood2/`）：`DroodParachute.cs`、`AddDroodFlightComponentsPatch.cs`、
  `Drood2ActivationGroupsPatch.cs`、`Drood2ActivationPanelPatch.cs`、`CalmAnimatorWhileHangingPatch.cs`、
  `DroodHangingPose.cs`、`DroodLandingFall.cs`、`FreezeMovementDuringLandingFallPatch.cs`、`DroodTeleportFix.cs`
- 待改动（Droodism）：`SupportLifeScript.cs`（`SpawnParachuteAndRide`/`DeployParaglider` 分支）、
  `SupportLifeData.cs`（持久化 + ChuteType 判定）、`GliderScript.cs`（保留 ParaGlider 分支）、
  `Mod.cs`（PatchAll 自动拾取新补丁，注意 catch 误导信息）
- 资产：`mods/Drood20/Drood20.unity3d`（方案 A）或导出 `HardLanding.fbx`（方案 B）

---

## 6. Drood 2.0 的 Harmony 使用全景

### 6.1 总览

- **12 个 `[HarmonyPatch]` 补丁类，共 patch 16 个游戏方法**，全部在 `OnModInitialized` 里一行打完：
  `new Harmony("Drood2").PatchAll(typeof(Mod).Assembly)`
- 前置检查：`IsHarmonyLoaded()` 用反射扫 AppDomain 里有没有 `"0Harmony"` 程序集（即 Juno Harmony mod）；
  没有则只注册零件，全部补丁功能静默禁用。
- 另外有 2 个类用 `AccessTools` 反射但**不打补丁**：`PersistentTethers`（MonoBehaviour，反射
  `MaxTetherLength`/`DestroyGrapplingHook`）、`DroodLookUp`（MonoBehaviour，`FieldRefAccess` `_cameraPosition`）。
- `DroodParachute`/`DroodHangingPose`/`DroodLandingFall`/`DroodLimbFollower` 都不是补丁，是**被补丁
  AddComponent 上去的 MonoBehaviour**。

### 6.2 补丁清单（16 个目标方法）

| 补丁类 | 目标方法 | 类型 | 作用 |
|---|---|---|---|
| `AddDroodFlightComponentsPatch` | `EvaScript.Start` | Postfix | 给 Drood2 小蓝人挂 `DroodLimbFollower`/`DroodLookUp`/`DroodLandingFall`/`DroodHangingPose`/`DroodParachute` 五个组件（降落伞系统入口） |
| `Drood2ActivationGroupsPatch` | `EvaScript.OnInitialized` | Postfix | 把小蓝人 CommandPod 激活组补齐 10 个，AG1 命名 "Parachute"（`Ensure()` 也被 DroodParachute.Awake 直接调用） |
| `Drood2ActivationPanelPatch` | `ActivationPanelController.CraftNodeChanged` | Postfix | 反射扫 `_buttons`，把 AG1 按钮显示出来并改名 "Parachute"，隐藏"无激活组"提示 |
| `CalmAnimatorWhileHangingPatch` | `CrewAnimController.InAir` setter | Prefix | 悬挂时把 InAir 置 false（×1） |
| 〃 | `CrewAnimController.Speed` setter | Prefix | 悬挂时清零（×4 同类） |
| 〃 | `CrewAnimController.ForwardSpeed` setter | Prefix | 〃 |
| 〃 | `CrewAnimController.SideSpeed` setter | Prefix | 〃 |
| 〃 | `CrewAnimController.VerticalSpeed` setter | Prefix | 〃 |
| `FreezeMovementDuringLandingFallPatch` | `EvaScript.UpdateMaxSpeeds` | Postfix | 落地摔倒期间把 `MaxForwardSpeed`/`MaxStrafeSpeed` 清零（`DroodLandingFall.Active` 时） |
| `DroodTeleportFix` | `CraftNode.RecalculateFrameState` | Postfix | 传送/大幅位移后把插值刚体位置对齐 transform（防伞下小蓝人漂移） |
| `CreateLimbCollidersPatch` | `PartScript.CreateAttachPoints` | Prefix | 给 Drood2 生成 14 段肢体 Capsule/Sphere 碰撞体（Torso/头/四肢各段），设计器里顺带挂 `DroodDesignerPoseRecorder` |
| `DroodStructureChangedPatch` | `EvaScript.OnCraftStructureChanged` | Postfix | 结构变化时让 `DroodLimbFollower` 重建手持零件绑定 |
| `KeepOldTethersPatch` | `EvaScript.UpdateGrapplingHook` | Prefix | 小蓝人再射新钩爪时，把旧的 `GrapplingHookScript` 转存进 `PersistentTethers`（旧绳保留） |
| `FollowTetherLengthPatch` | `EvaScript.AdjustTetherLength` | Postfix | 调整绳长时同步调整该小蓝人名下的所有持久绳 |
| `KeepHeldPartsUncombinedPatch` | `PartGroupScript.ProcessMeshes` | Prefix | 把"手持零件"标记 `ExcludeFromMeshCombine`，防止被网格合并后跟随骨骼失效 |
| `Drood2TechTreePatch` | `TechTree.IsDesignerPartAvailable` | Postfix | Drood 2.0 零件解锁 = 原版 Drood + 原版降落伞都解锁 |

### 6.3 自定义动画是怎么写进去的（HardLanding）

**资产侧（制作/打包）：**

1. 动画来自 **mixamo**（bundle 里剪辑名 "mixamo.com" 就是 mixamo take 名）：humanoid 肌肉空间、
   30fps、2.133s、200 条肌肉曲线。
2. 放 mod 工程 `Assets/Animations/HardLanding.fbx`，在 `ModManifest.xml` 里声明：
   `<OtherAssets><Asset path="Assets/Animations/HardLanding.fbx" /></OtherAssets>`
3. mod tools 把它打进 mod bundle → 运行时由游戏的 `ResourceLoader` 按资产路径加载。
4. bundle 里实际是 4 样东西：`AnimationClip "mixamo.com"` + `Avatar "HardLandingAvatar"` +
   `Animator` + 一堆空 GameObject/Transform（fbx 导入的场景层级，**运行时用不上**）。

**运行时侧（DroodLandingFall.cs）——不碰游戏动画控制器：**

1. **加载**：`Mod.Instance.ResourceLoader.LoadAsset<AnimationClip>("Assets/Animations/HardLanding.fbx")`
2. **用独立 PlayableGraph 覆盖输出到小蓝人自己的 Animator**：
   ```csharp
   _graph = PlayableGraph.Create("Drood2 Hard Landing");
   var playable = AnimationClipPlayable.Create(_graph, clip);
   playable.SetApplyFootIK(true);
   _output = AnimationPlayableOutput.Create(_graph, "HardLanding", _animator); // _animator = GetComponentInChildren<Animator>()
   _output.SetSourcePlayable(playable);
   _output.SetWeight(0f);
   _graph.Play();
   ```
   这是关键：**不走 `Animator.Play`/`CrossFade`**（会和游戏自己的动画状态机打架），而是用
   PlayableGraph 的输出口直接接管 Animator，权重手动控制。
3. **权重渐变**：0.1s 淡入 → 播 2.133s → 0.35s 淡出，`Update()` 里 `_output.SetWeight(_clipWeight)`。
4. **FinalIK 手撑地**：`_ik.solver.OnPreUpdate += PlantHands`——每帧 IK 前从双手向下
   `Physics.RaycastNonAlloc` 找地面，把手 effector 按到 `ground + normal * 0.04`，
   `positionWeight = plant * _clipWeight`（plant 按手离地高度 0.12~0.35 范围线性算）。
5. **防走动**：`FreezeMovementDuringLandingFallPatch` 在摔倒期间把最大移动速度清零。
6. **触发**：`DroodParachute.DetectLandingFall`——落地瞬间 `inflate > 0.5` 且垂直接地速度 > 3 m/s →
   `Begin(drift, sink)`。

**另外两处"动画"不用动画文件：**

- **悬挂姿态**：`DroodHangingPose` 纯 FinalIK 目标驱动（双手抓伞绳、腿摆动 + leg-lag），配合
  `CalmAnimatorWhileHangingPatch` 把 `CrewAnimController` 的速度类参数清零，让游戏动画器保持安静。
- **伞衣**：`DroodParachute.BuildVisuals` 程序化建网格 + 16 根 LineRenderer 伞绳，跟动画无关。

> 移植提示：肌肉空间动画绑到小蓝人自己的 Avatar 即可，**bundle 里的 `HardLandingAvatar` 不需要**；
> 加载路径换成 Droodism 自己的资产路径即可，`DroodLandingFall` 其余代码可原样搬。
