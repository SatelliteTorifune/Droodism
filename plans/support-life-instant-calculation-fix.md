# SupportLifeScript - RemoveFuelAmountInstantly / AddWastedAmountInstantly 修复方案

## 背景

[`RemoveFuelAmountInstantly`](Assets/Scripts/Craft/Parts/Modifiers/SupportLifeScript.cs:708) 和 [`AddWastedAmountInstantly`](Assets/Scripts/Craft/Parts/Modifiers/SupportLifeScript.cs:768) 是当飞船从非物理加载状态（Unloaded）恢复物理时，快速结算这段时间内应消耗/产生的维生资源。

现有的计算存在**严重的单位不一致**问题，导致计算完全不准确。

---

## 关键上下文

- [`FlightState.Time`](Assets/Scripts/Craft/Modifiers/SupportLifeScript.cs:710) 的单位是**秒**
- [`amount`](Assets/Scripts/Craft/Modifiers/SupportLifeScript.cs:712) `= (FlightState.Time - LastLoadTime) / 1` → 单位也是**秒**（时间间隔）
- 消耗速率如 [`Data.OxygenConsumeRate`](Assets/Scripts/Craft/Modifiers/SupportLifeData.cs:112) 的单位是**单位/秒**（乘了 0.007f）
- [`IFuelSource.TotalFuel`](Assets/Scripts/Craft/Modifiers/SupportLifeScript.cs:719) 是燃料**存量**（单位:燃料量）
- [`Data._oxygenAmountBuffer`](Assets/Scripts/Craft/Modifiers/SupportLifeData.cs:83) 也是燃料**存量**（单位:燃料量）

**正确的消耗量 = `amount(秒) × ConsumeRate(单位/秒)`**

---

## Bug 清单

### Bug 1: `RemoveFuel(OxygenSource.TotalCapacity)` 应改为 `RemoveFuel(OxygenSource.TotalFuel)`

**位置:** [`SupportLifeScript.cs:719`](Assets/Scripts/Craft/Parts/Modifiers/SupportLifeScript.cs:719)

```csharp
OxygenSource.RemoveFuel(OxygenSource.TotalCapacity);  // ❌ 清空了整个油箱的容量
```

`TotalCapacity` 是油箱最大容量（常数），`TotalFuel` 才是当前存量。这会导致从飞船油箱中抽走超出实际存量的燃料。

同样问题存在于:
- [`SupportLifeScript.cs:733`](Assets/Scripts/Craft/Parts/Modifiers/SupportLifeScript.cs:733) — Water
- [`SupportLifeScript.cs:753`](Assets/Scripts/Craft/Parts/Modifiers/SupportLifeScript.cs:753) — Food

### Bug 2: `remain = amount - TotalFuel` 单位不一致

**位置:** [`SupportLifeScript.cs:720`](Assets/Scripts/Craft/Parts/Modifiers/SupportLifeScript.cs:720)

```csharp
double remain = amount - OxygenSource.TotalFuel;
// amount = 时间(秒), TotalFuel = 燃料量 → 单位完全不兼容!
```

应改为:

```csharp
double totalConsumed = amount * Data.OxygenConsumeRate;  // 这段时间应消耗总量
double fromSource = Math.Min(totalConsumed, OxygenSource.TotalFuel);  // 能从油箱消耗的量
double remainder = totalConsumed - fromSource;  // 剩余要从本地buffer扣除的量
```

同样问题存在于:
- [`SupportLifeScript.cs:734`](Assets/Scripts/Craft/Parts/Modifiers/SupportLifeScript.cs:734) — Water
- [`SupportLifeScript.cs:754`](Assets/Scripts/Craft/Parts/Modifiers/SupportLifeScript.cs:754) — Food

### Bug 3: `AddLifeSupportFuel` 传入错误的值

**位置:** [`SupportLifeScript.cs:721`](Assets/Scripts/Craft/Parts/Modifiers/SupportLifeScript.cs:721)

```csharp
Data.AddLifeSupportFuel("Oxygen", remain>Data.DesireOxygenCapacity?-Data._oxygenAmountBuffer:-remain);
```

由于 `remain` 单位是秒（Bug 2 导致），这个比较和计算全部错误。正确的逻辑应该是:

- 如果 `remainder > DesireOxygenCapacity` → 清空 buffer（即 `-Data._oxygenAmountBuffer`）
- 否则 → 从 buffer 扣除 `remainder`（即 `-remainder`）

同样问题存在于:
- [`SupportLifeScript.cs:726`](Assets/Scripts/Craft/Parts/Modifiers/SupportLifeScript.cs:726) — Oxygen（null/empty分支）
- [`SupportLifeScript.cs:735`](Assets/Scripts/Craft/Parts/Modifiers/SupportLifeScript.cs:735) — Water（craft source 耗尽分支）
- [`SupportLifeScript.cs:745`](Assets/Scripts/Craft/Parts/Modifiers/SupportLifeScript.cs:745) — Water（null/empty分支）
- [`SupportLifeScript.cs:755`](Assets/Scripts/Craft/Parts/Modifiers/SupportLifeScript.cs:755) — Food（craft source 耗尽分支）
- [`SupportLifeScript.cs:764`](Assets/Scripts/Craft/Parts/Modifiers/SupportLifeScript.cs:764) — Food（null/empty分支）

### Bug 4: `AddWastedAmountInstantly` 中 `remain` 使用 `amount`（时间）而不是实际废物量

**位置:** [`SupportLifeScript.cs:782`](Assets/Scripts/Craft/Parts/Modifiers/SupportLifeScript.cs:782)

```csharp
double remain = amount - (Co2Source.TotalCapacity - Co2Source.TotalFuel);
// amount = 时间(秒), (Capacity - TotalFuel) = 剩余空间(燃料量) → 单位不兼容!
```

应改为:

```csharp
double added = Math.Min(co2ToAdd, Co2Source.TotalCapacity - Co2Source.TotalFuel);
Co2Source.AddFuel(added);
double overflow = co2ToAdd - added;
```

同样问题存在于:
- [`SupportLifeScript.cs:802`](Assets/Scripts/Craft/Parts/Modifiers/SupportLifeScript.cs:802) — WastedWater
- [`SupportLifeScript.cs:820`](Assets/Scripts/Craft/Parts/Modifiers/SupportLifeScript.cs:820) — SolidWaste

### Bug 5: Craft 油箱已满时不应再次调用 `RemoveFuel`

在 [`ConsumeInputResource`](Assets/Scripts/Craft/Parts/Modifiers/SupportLifeScript.cs:625) 中，当 `craftSource != null && !craftSource.IsEmpty` 时直接消耗。但 `RemoveFuelAmountInstantly` 中先判断 `amount * rate > TotalFuel` 才走耗尽逻辑，否则走 `RemoveFuel(amount * rate)`。逻辑上应该可以，但需要注意:
- 如果 `amount * rate` 刚好等于 `TotalFuel`，会走到 else 分支执行 `RemoveFuel(amount * rate)`，这是正确的。

### Bug 6: `RemoveFuelAmountInstantly` 未处理 IsRunning / IsTourist 倍率

`ConsumptionLogic` 中实际消耗考虑了 `baseRate = frame.DeltaTimeWorld * (IsRunning ? 1.75 : 1) * (IsTourist ? 1.05 : 1)`。

但 `RemoveFuelAmountInstantly` 中没有乘以这些倍率，导致消耗量偏低（当 crew 在跑步时）。

### Bug 7: `AddWastedAmountInstantly` 中固体废物计算少了 `0.04` 系数

`ConsumptionLogic` 中:
```csharp
double solidWasteAmount = foodConsumeAmount * 1.1 * Data.evaConsumeEfficiency * 0.04;
```

但 `AddWastedAmountInstantly` 中:
```csharp
double solidWasteToAdd = Data.FoodConsumeRate * Data.evaConsumeEfficiency * 1.1 * amount;
```

少了 `* 0.04` 系数，导致固体废物产生量偏高 25 倍。

---

## 修复方案

### 方案概述

> 将 `RemoveFuelAmountInstantly` 和 `AddWastedAmountInstantly` 两个方法完全重写，使用正确的单位计算。

### 具体步骤

#### Step 1: 修复 `RemoveFuelAmountInstantly`

```csharp
private void RemoveFuelAmountInstantly()
{
    double elapsedSeconds = Game.Instance.FlightScene.FlightState.Time - Data.LastLoadTime;
    double activityMultiplier = (IsRunning ? 1.75 : 1.0) * (IsTourist ? 1.05 : 1.0);

    // --- 氧气消耗（仅在使用内部氧气时）---
    if (UsingInternalOxygen())
    {
        double oxygenToConsume = Data.OxygenConsumeRate * activityMultiplier * elapsedSeconds;
        double fromSource = 0;
        if (OxygenSource != null && !OxygenSource.IsEmpty)
        {
            fromSource = Math.Min(oxygenToConsume, OxygenSource.TotalFuel);
            OxygenSource.RemoveFuel(fromSource);
        }
        double remainingOxygen = oxygenToConsume - fromSource;
        if (remainingOxygen > 0)
        {
            double fromBuffer = Math.Min(remainingOxygen, Data._oxygenAmountBuffer);
            Data.AddLifeSupportFuel("Oxygen", -fromBuffer);
        }
    }

    // --- 水消耗 ---
    double waterToConsume = Data.WaterConsumeRate * activityMultiplier * elapsedSeconds;
    double fromWaterSource = 0;
    if (WaterSource != null && !WaterSource.IsEmpty)
    {
        fromWaterSource = Math.Min(waterToConsume, WaterSource.TotalFuel);
        WaterSource.RemoveFuel(fromWaterSource);
    }
    double remainingWater = waterToConsume - fromWaterSource;
    if (remainingWater > 0)
    {
        double fromWaterBuffer = Math.Min(remainingWater, Data._waterAmountBuffer);
        Data.AddLifeSupportFuel("H2O", -fromWaterBuffer);
    }

    // --- 食物消耗 ---
    double foodToConsume = Data.FoodConsumeRate * activityMultiplier * elapsedSeconds;
    double fromFoodSource = 0;
    if (FoodSource != null && !FoodSource.IsEmpty)
    {
        fromFoodSource = Math.Min(foodToConsume, FoodSource.TotalFuel);
        FoodSource.RemoveFuel(fromFoodSource);
    }
    double remainingFood = foodToConsume - fromFoodSource;
    if (remainingFood > 0)
    {
        double fromFoodBuffer = Math.Min(remainingFood, Data._foodAmountBuffer);
        Data.AddLifeSupportFuel("Food", -fromFoodBuffer);
    }
}
```

#### Step 2: 修复 `AddWastedAmountInstantly`

```csharp
private void AddWastedAmountInstantly()
{
    double elapsedSeconds = Game.Instance.FlightScene.FlightState.Time - Data.LastLoadTime;
    double activityMultiplier = (IsRunning ? 1.75 : 1.0) * (IsTourist ? 1.05 : 1.0);

    // --- CO2 产生（仅在使用内部氧气时）---
    if (UsingInternalOxygen())
    {
        double co2ToAdd = Data.OxygenConsumeRate * activityMultiplier * elapsedSeconds * 1.375 * Data.evaConsumeEfficiency;
        AddWasteToSource(Co2Source, ref Data._co2AmountBuffer, Data.DesireCO2Capacity, co2ToAdd);
    }

    // --- 废水产生 ---
    double wastedWaterToAdd = 1.1 * Data.WaterConsumeRate * Data.evaConsumeEfficiency * activityMultiplier * elapsedSeconds;
    AddWasteToSource(WastedWaterSource, ref Data._wastedWaterAmountBuffer, Data.DesireWastedWaterCapacity, wastedWaterToAdd);

    // --- 固体废物产生（注意有 0.04 系数）---
    double solidWasteToAdd = Data.FoodConsumeRate * Data.evaConsumeEfficiency * 1.1 * 0.04 * activityMultiplier * elapsedSeconds;
    AddWasteToSource(SolidWasteSource, ref Data._solidWasteAmountBuffer, Data.DesireSolidWasteCapacity, solidWasteToAdd);
}

/// <summary>
/// 将废物先填入 craft fuel source，满了再填入本地 buffer
/// </summary>
private void AddWasteToSource(IFuelSource craftSource, ref double buffer, float bufferCapacity, double amountToAdd)
{
    if (amountToAdd <= 0) return;

    if (craftSource != null)
    {
        double availableSpace = craftSource.TotalCapacity - craftSource.TotalFuel;
        double intoCraft = Math.Min(amountToAdd, availableSpace);
        craftSource.AddFuel(intoCraft);
        double overflow = amountToAdd - intoCraft;
        if (overflow > 0)
        {
            double intoBuffer = Math.Min(overflow, bufferCapacity - buffer);
            buffer += intoBuffer;
        }
    }
    else
    {
        double intoBuffer = Math.Min(amountToAdd, bufferCapacity - buffer);
        buffer += intoBuffer;
    }
}
```

#### Step 3: 验证边界情况

| 场景 | 预期行为 |
|------|---------|
| 油箱有足够燃料 | 从油箱扣完，buffer 不变 |
| 油箱不够燃料 | 油箱扣完 + 从 buffer 扣剩余部分 |
| 油箱为空/为 null | 全部从 buffer 扣 |
| buffer 也不够 | 扣到 0，剩余不扣（应触发 Damage，但 Instant 结算不考虑 Damage） |
| 废物油箱有空间 | 填到油箱 |
| 废物油箱满了/null | 填到 buffer |
| buffer 也满了 | 填满 buffer，多余丢失（应触发 Damage，但 Instant 结算不考虑 Damage） |

---

## 影响范围

- 仅修改 [`SupportLifeScript.cs`](Assets/Scripts/Craft/Parts/Modifiers/SupportLifeScript.cs) 中的 `RemoveFuelAmountInstantly` 和 `AddWastedAmountInstantly` 两个方法
- 对外接口不变（方法签名不变）
- 通过 [`OnPhysicsEnabled`](Assets/Scripts/Craft/Parts/Modifiers/SupportLifeScript.cs:313) 调用，逻辑不变
- 与 [`ConsumptionLogic`](Assets/Scripts/Craft/Parts/Modifiers/SupportLifeScript.cs:589) 计算方式对齐

---

## 不在此次修复范围内

- `ConsumptionLogic` 本身的逻辑（虽然可能也有问题，但用户仅报告了 Instant 结算不准）
- 其他方法中的潜在单位问题
- Damage 触发（Instant 结算不触发伤害，保持与原行为一致）
