# 2026 6 2
> 飞行员过载伤害减免
> >当一个Drood是飞行员时,其过载收到的伤害降低为60%

# 2026 5 29
> 增加了重新进入FlightScene后ragDoll状态的保持


# 2026  5 28
> 修复了进入ragDoll状态时jetPack仍在动画播放的bug

# 2026  5 27
> 增加了可呼吸星球的配置文件,允许玩家自己定义星球

# V0.79发布

# 2026 5 16
> 增加了滑翔伞的和降落伞的升力,下调了滑翔伞前进的动力

> 修复了降落伞和滑翔伞UnloadCrew时位置错误的bug
> >吗的这些破鸡巴玩意真的不能放在FlightFixedUpdate里面,只能丢FlightUpdate里
# 2026 5 14
> 增加了RagDollScript,用来测试小蓝人死亡时的ragDoll效果
# 2026 5 9
>修复了SacrificeScript中数值不对应的bug

# 2026 5 8
>修复了DroodismUIManager中Craft结构更新时容量未更新的bug

>修复了PartAddingManager中给cockpit添加CrewCabin Modifer的bug

# 2026 5 7
>大量重构了SupportLifeScript.cs的维生相关底层逻辑
> >使用Data内相关字段代替并模拟了FuelTank的IFuelSource接口

>修复了 SupportLifeScript.ConsumeInputResource中的一处空引用


>修复了 SupportLifeScript中RemoveFuelAmountInstantly和AddWastedAmountInstantly两个函数

>对SacrificeScript进行了适配重构后SupportLifeScript的修改
# 2026 5 6
>修复了 游戏尝试在PlanetStudio中创建辐射带Inspector Panel的bug

>移除了Legacy UI的相关代码

>修复了启用自动开伞时的更新问题
# 2026 5 5
>增加了光电反应器零件展开状态保存功能
# 2026 5 3
>修复了UI在跳伞时显示资源空引用的bug

>增加了重力环零件展开状态保存功能




