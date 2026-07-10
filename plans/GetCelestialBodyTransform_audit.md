# `GetCelestialBodyTransform()` API 替换可行性审计报告

## 概述

对项目 `Assets/Scripts/` 下的所有脚本进行扫描，识别所有获取天体（行星/星球）`Transform` 数据的调用模式，并评估能否替换为游戏新增的 API：

```
Game.Instance.FlightScene.ViewManager.MapViewManager.MapView.GetCelestialBodyTransform(string planetName) -> Transform
```

---

## 候选调用清单

### 候选 1 (可替换 — **核心目标**) `GetMapPlanet()`

| 属性 | 内容 |
|------|------|
| **文件** | [`RadiationBeltManager.cs`](../Assets/Scripts/Droodism/RadiationBelt/RadiationBeltManager.cs) |
| **行号** | 260–278 |
| **当前实现** | 使用 `GameObject.FindObjectsOfType<Transform>(true)` 遍历场景中**所有** Transform，按名称匹配行星，再校验父级是否为 `"Planets"`、祖父级是否包含 `"MapView"` |
| **代码片段** | ```csharp
private static GameObject GetMapPlanet(string PlanetName)
{
    foreach (Transform t in GameObject.FindObjectsOfType<Transform>(true))
    {
        if (t.name == PlanetName)
        {
            Transform parent = t.parent;
            if (parent != null && parent.name == "Planets")
            {
                Transform mapView = parent.parent;
                if (mapView != null && mapView.name.Contains("MapView"))
                {
                    return t.gameObject;
                }
            }
        }
    }
    return null;
}
``` |
| **问题** | `FindObjectsOfType<Transform>(true)` 遍历场景中**全部** Transform 对象，性能极差；且依赖不稳定的层级名称硬编码（`"Planets"`、`"MapView"`），与游戏内部实现耦合。 |
| **替换后代码** | ```csharp
private static GameObject GetMapPlanet(string planetName)
{
    var mapView = Game.Instance.FlightScene.ViewManager.MapViewManager.MapView;
    var transform = mapView.GetCelestialBodyTransform(planetName);
    return transform != null ? transform.gameObject : null;
}
``` |
| **替换价值** | 🔥 **高** — 消除全场景扫描，性能提升显著；不再依赖内部层级名称 |

---

### 候选 2 (可替换 — **被调用方**）`AddPlanetRadiationBelt()`

| 属性 | 内容 |
|------|------|
| **文件** | [`RadiationBeltManager.cs`](../Assets/Scripts/Droodism/RadiationBelt/RadiationBeltManager.cs) |
| **行号** | 188–214 |
| **当前实现** | 调用 `GetMapPlanet(PlanetName)` 获取行星父对象，然后将辐射带对象作为其子级挂载 |
| **代码片段** | ```csharp
var parentGameObject = GetMapPlanet(PlanetName);
// ...
currentRadiationBeltObject.transform.SetParent(parentGameObject.transform);
``` |
| **替换方式** | 若候选 1 被替换，本方法自动受益（无需额外修改） |
| **替换价值** | 🔥 **高**（间接） |

---

### 候选 3 (不可替换) `GetCurrentFocusPlanet()`

| 属性 | 内容 |
|------|------|
| **文件** | [`RadiationBeltManager.cs`](../Assets/Scripts/Droodism/RadiationBelt/RadiationBeltManager.cs) |
| **行号** | 170–187 |
| **当前实现** | 通过 `MapViewInspector.SelectedItem.AssociatedPlanet.Name` 或 `CraftNode.Parent.Name` 获取**行星名称** |
| **代码片段** | ```csharp
lastRemembered = Game.Instance.FlightScene.ViewManager.MapViewManager.MapView
    .MapViewInspector.SelectedItem.AssociatedPlanet.Name;
// 或
lastRemembered = Game.Instance.FlightScene.CraftNode.Parent.Name;
``` |
| **不可替换原因** | 此方法返回的是 `string`（行星名称），而非 `Transform`。`GetCelestialBodyTransform()` 返回 `Transform`，语义不匹配。 |
| **替换价值** | ❌ 无 |

---

### 候选 4 (不可替换) `ConvertPlanetPositionToLatLongAgl()`

| 属性 | 内容 |
|------|------|
| **文件** | [`ModUlti.cs`](../Assets/Scripts/ModUlti.cs) |
| **行号** | 43–59 |
| **当前实现** | 通过 `CraftNode.Parent`（`IPlanetNode`）获取行星数据进行坐标转换 |
| **代码片段** | ```csharp
IPlanetNode parent = Game.Instance.FlightScene?.CraftNode?.Parent;
Vector3d surfaceVector = parent.PlanetVectorToSurfaceVector(position);
parent.GetSurfaceCoordinates(surfaceVector, out latitude, out longitude);
double num = parent.GetTerrainHeight(position);
``` |
| **不可替换原因** | 使用 `IPlanetNode` 接口获取位置/地形数据，这些是物理/游戏逻辑层面的数据，不是 MapView 的 Transform 可视化数据。`GetCelestialBodyTransform()` 无法提供 `PlanetVectorToSurfaceVector()`、`GetTerrainHeight()` 等接口。 |
| **替换价值** | ❌ 无 |

---

### 候选 5 (不可替换) `FindPlanet()`

| 属性 | 内容 |
|------|------|
| **文件** | [`RadiationBeltUI.cs`](../Assets/Scripts/Droodism/RadiationBelt/RadiationBeltUI.cs) |
| **行号** | 111–121 |
| **当前实现** | 遍历 `FlightState.SolarSystemData.Planets` 获取 `IPlanetData` |
| **代码片段** | ```csharp
private IPlanetData FindPlanet(string name)
{
    foreach (var childPlanet in Game.Instance.FlightScene.FlightState.SolarSystemData.Planets)
    {
        if (childPlanet.Name == name)
            return childPlanet;
    }
    return null;
}
``` |
| **不可替换原因** | 返回 `IPlanetData`（半径、名称等数据），不是 `Transform`。调用方需要 `planet.Radius` 计算距离显示，而非变换组件。 |
| **替换价值** | ❌ 无 |

---

### 候选 6 (不可替换) `EnsurePlanetRadiusCached()` / `GetPlanetRadiusMeters()`

| 属性 | 内容 |
|------|------|
| **文件** | [`RadiationBeltManager.cs`](../Assets/Scripts/Droodism/RadiationBelt/RadiationBeltManager.cs) |
| **行号** | 297–313 / 289–296 |
| **当前实现** | 从 `SolarSystemData.Planets` 获取行星半径数据 |
| **代码片段** | ```csharp
foreach (IPlanetData planet in Game.Instance.FlightScene.CraftNode.Parent.PlanetData.SolarSystemData.Planets)
{
    planetRadiusMetersByName[planetName] = planet.Radius;
    planetRadiusScaledByName[planetName] = planet.RadiusScaledSpace;
}
``` |
| **不可替换原因** | 需要的是 `planet.Radius`（double 类型数值），不是 Transform。`GetCelestialBodyTransform()` 返回的 Transform 无法直接提供行星半径数据。 |
| **替换价值** | ❌ 无 |

---

### 候选 7 (不可替换) `RadiationBeltCameraRenderer.OnPostRender()`

| 属性 | 内容 |
|------|------|
| **文件** | [`RadiationBeltCameraRenderer.cs`](../Assets/Scripts/Droodism/RadiationBelt/RadiationBeltCameraRenderer.cs) |
| **行号** | 11–51 |
| **当前实现** | 使用 `beltRenderer.transform.localToWorldMatrix` 获取辐射带对象的变换矩阵用于渲染 |
| **代码片段** | ```csharp
Matrix4x4 renderMatrix = beltRenderer.transform.localToWorldMatrix * Matrix4x4.Scale(Vector3.one * radiusScale);
``` |
| **不可替换原因** | 获取的是**辐射带对象自身的 Transform**，用于渲染计算，而非天体的 Transform。 |
| **替换价值** | ❌ 无 |

---

### 候选 8 (不可替换) `ProceduralRadiationBelt.SycWithParent()`

| 属性 | 内容 |
|------|------|
| **文件** | [`ProceduralRadiationBelt.cs`](../Assets/Scripts/Droodism/RadiationBelt/ProceduralRadiationBelt.cs) |
| **行号** | 151–170 |
| **当前实现** | 设置辐射带对象自身的 `localPosition`、`localRotation`（相对于父级行星对象） |
| **代码片段** | ```csharp
transform.localPosition = Vector3.zero;
transform.localRotation = tilt * spin;
``` |
| **不可替换原因** | 设置的是辐射带对象**相对于其父级**的本地变换，而非获取天体的变换。父级在 `AddPlanetRadiationBelt` 中就已经设置好了。 |
| **替换价值** | ❌ 无 |

---

### 候选 9 (不可替换) `SpawnFlag()` 中的行星引用

| 属性 | 内容 |
|------|------|
| **文件** | [`ModUlti.cs`](../Assets/Scripts/ModUlti.cs) |
| **行号** | 22–34 |
| **当前实现** | 通过 `CraftNode.Parent.PlanetData.Name` 和 `CraftNode.Parent.Name` 获取行星名称/数据 |
| **代码片段** | ```csharp
Game.Instance.FlightScene.CraftNode.Parent.PlanetData.Name  // 行28
Game.Instance.FlightScene.CraftNode.Parent.Name              // 行34,38
``` |
| **不可替换原因** | 访问的是 `IPlanetNode.Name` / `IPlanetData.Name`（字符串标识符），而非 Transform。用于构造降落位置和 UI 显示，不需要变换组件。 |
| **替换价值** | ❌ 无 |

---

### 候选 10 (不可替换) 其他 .transform 相关调用

项目中共有 73 处 `.transform` 引用，分布在各个 Part Modifier 脚本中（如 `GliderScript`、`MiningMachineScript`、`FlagScript`、`GravityRingScript` 等）。这些调用均为操作**零部件子对象**的局部变换（如动画、机械臂、旗帜杆等），与天体 Transform **完全无关**。

| 典型示例 | 文件 | 行号 | 用途 |
|----------|------|------|------|
| `parachuteMeshTransform.transform.localScale` | `GliderScript.cs` | 56,117,157 | 降落伞动画缩放 |
| `nail.transform.localPosition` | `MiningMachineScript.cs` | 88–93 | 挖矿机动画 |
| `_sphere.transform.localScale` | `balloonScript.cs` | 47,53 | 气球膨胀动画 |
| `part.GameObject.transform.position` | `ToolBoxScript.cs` | 42,89 | 计算零件间距离 |
| `_rotateBase.transform.localPosition` | `FlagScript.cs` | 59,104 | 旗帜升降动画 |
| `rb.transform.forward/right/up` | `GliderScript.cs` | 241–243 | 飞行方向计算 |

**替换价值**：❌ 全部不可替换 — 它们操作的均是零部件子对象或飞船部件的局部/世界变换，与天体 Transform 无关。

---

## 汇总：可替换的修改建议

| # | 文件 | 方法 | 行号 | 替换优先级 |
|---|------|------|------|-----------|
| 1 | `RadiationBeltManager.cs` | `GetMapPlanet()` | 260–278 | 🔴 **高** — 直接替换 `FindObjectsOfType<Transform>` |
| 2 | `RadiationBeltManager.cs` | `AddPlanetRadiationBelt()` | 188–214 | 🟡 **中** — 间接受益，`GetMapPlanet` 替换后自动优化 |

### 推荐修改方案

#### 步骤 1：重写 `GetMapPlanet()`

```csharp
private static GameObject GetMapPlanet(string planetName)
{
    var mapView = Game.Instance.FlightScene.ViewManager.MapViewManager.MapView;
    var celestialTransform = mapView.GetCelestialBodyTransform(planetName);
    return celestialTransform != null ? celestialTransform.gameObject : null;
}
```

#### 步骤 2：添加空引用保护

`GetCelestialBodyTransform()` 可能在行星未加载或不存在时返回 `null`，需要确保调用方处理：

```csharp
var parentGameObject = GetMapPlanet(PlanetName);
if (parentGameObject == null)
{
    Mod.LogError($"Parent GameObject for planet {PlanetName} is null.");
    return;
}
```

> 当前代码已有该 null 检查（行 191-195），无需额外修改。

---

## 总结

| 类别 | 数量 |
|------|------|
| 扫描的 .cs 脚本总数 | ~50 |
| 涉及变换操作的候选总数 | 10 |
| **可替换的候选数** | **2**（候补 1 和候补 2，实质为同一替换） |
| **不可替换的候选数** | **8** |
| **需要修改的文件数** | **1**（`RadiationBeltManager.cs`） |
| **需修改的方法数** | **1**（`GetMapPlanet()`） |
| **需删除的代码行** | ~18 行（`GetMapPlanet` 中的全场景遍历） |
| **需新增的代码行** | ~5 行（新 API 调用） |

### 核心发现

项目中对天体 `Transform` 的直接获取需求**高度集中在** [`RadiationBeltManager.GetMapPlanet()`](Assets/Scripts/Droodism/RadiationBelt/RadiationBeltManager.cs:260) 方法中，该方法使用 `GameObject.FindObjectsOfType<Transform>(true)` 暴力搜索全场景来定位行星 GameObject。这是**唯一一个直接获取天体 Transform 的调用点**，且是该 API 替换的理想目标。

新 API `GetCelestialBodyTransform(planetName)` 可以完全替代此方法，消除对内部层级结构的硬编码依赖，并大幅提升性能。
