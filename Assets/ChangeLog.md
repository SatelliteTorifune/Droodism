## 2026 6 26
> 更新modTool到 1.4版本
> 
> 根据新版本变动修改了SRCraftFuelSources中的部分函数以适配更新后的CraftFuelSources和全局资源系统
> 
> 对Propulsion.xml进行了密度调整,并覆写了部分1.4更新后新增的FuelType

# V0.797 发布

## 2026 6 16
> 准备重置插旗机制,新增了可展开的旗子
> > 修改展开速度为非线性的
> 
> > 增加了状态保持
> 
> > 增加了读取自定义图象的功能
> 
> >已集成,并隐藏此part
> 
>修复了UI中存在Unassigned的小蓝人时无法创建的bug
> >加个空引用判断的事情

## 2026 6 15
> 修复了UI中设置转移燃料模式drood判断中的一个误判bug

## 2026 6 14
> 增加了重置资源预警flag的函数与应用
> 
> 回滚了OnToggleDroodismInspectorPanelState()函数
> >吗的有点诡异

## 2026 6 13
> 增加资源预警系统框架

## 2026 6 12
> 疑似修复了UI要点两下才能触发的bug
> 
## 2026 6 11
>彻底移除了 旗子和梯子 两个part以及对应的脚本
> 
>增加了Hire Drood时选择职业的功能
> 
>增加了SupportLifeData.cs中根据降落伞类型调整参数可见性的功能
> 
> 修改了各个Drood乘组的职业的颜色,提高可读性
## 2026 6 10
> 修复了UI中crew inspector更新问题
> >增加了强制重构Inspector Panel的功能,当craft结构发生变化或者其他什么屁事发生时会强制重构Inspector Panel以更新显示
> 
> 增加了Crew Inspector Panel中让Drood Eva的按钮
> 
> 增加了Crew Inspector Panel中隐藏部分数据的功能

## 2026 6 8
> DroodismCrewData增加了飞行总时长属性
> 
> 增加了计算craft内辐射源的功能

## 2026 6 7
> 给Resources Pack 增加了尺寸调整功能
> 
> 缩小了Resources Pack的默认尺寸
> 
> 增加了移除对接口上摄像机的设置
> >慎用,慎用!

## 2026 6 3
> 彻底移除了高压氧气的FuelType和相关支持
> 

# V 0.795 发布

## 2026 6 2
> 飞行员过载伤害减免
> >当一个Drood是飞行员时,其过载收到的伤害降低为60%

## 2026 5 29
> 增加了重新进入FlightScene后ragDoll状态的保持


## 2026  5 28
> 修复了进入ragDoll状态时jetPack仍在动画播放的bug

## 2026  5 27
> 增加了可呼吸星球的配置文件,允许玩家自己定义星球

# V0.79发布

## 2026 5 16
> 增加了滑翔伞的和降落伞的升力,下调了滑翔伞前进的动力

> 修复了降落伞和滑翔伞UnloadCrew时位置错误的bug
> >吗的这些破鸡巴玩意真的不能放在FlightFixedUpdate里面,只能丢FlightUpdate里
## 2026 5 14
> 增加了RagDollScript,用来测试小蓝人死亡时的ragDoll效果
## 2026 5 9
>修复了SacrificeScript中数值不对应的bug

## 2026 5 8
>修复了DroodismUIManager中Craft结构更新时容量未更新的bug

>修复了PartAddingManager中给cockpit添加CrewCabin Modifer的bug

## 2026 5 7
>大量重构了SupportLifeScript.cs的维生相关底层逻辑
> >使用Data内相关字段代替并模拟了FuelTank的IFuelSource接口

>修复了 SupportLifeScript.ConsumeInputResource中的一处空引用


>修复了 SupportLifeScript中RemoveFuelAmountInstantly和AddWastedAmountInstantly两个函数

>对SacrificeScript进行了适配重构后SupportLifeScript的修改
## 2026 5 6
>修复了 游戏尝试在PlanetStudio中创建辐射带Inspector Panel的bug

>移除了Legacy UI的相关代码

>修复了启用自动开伞时的更新问题
## 2026 5 5
>增加了光电反应器零件展开状态保存功能
## 2026 5 3
>修复了UI在跳伞时显示资源空引用的bug

>增加了重力环零件展开状态保存功能




