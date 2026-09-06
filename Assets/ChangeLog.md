## 2026 9 7
> Debug 日志清理:
> 
> - 所有裸 Debug.Log*/Console.WriteLine 统一改走 Mod.Log/LogWarning/LogError(新增 LogWarning,LogError 改为 LogType.Error 级别)
> - 三个日志入口全部受 ModSettings.Instance.DebugMode 门控,关闭时零日志;IsDebugMode 空安全(早期初始化取不到设置时静默)
> - 清理 QuickSaveDealer 的 Console.WriteLine、Mod 初始化异常、DroodismFilesSetUp、OnHireButtonClickedPatch、FlagScript 的裸日志
> - 顺带改掉了 RadiationBeltManager 里的调试脏话日志"fucked1111"
> 
> 性能优化:
> 
> - DroodismUIManager.Update 的每帧燃料数据刷新(GetIFuelSourceByID+UpdateFuelTemplateItem,约6类型×每秒数十次字符串分配)改为只在 Droodism 面板可见时执行,面板关闭时零开销;打开面板时立即刷新一次避免显示旧数据
> 
> 适配游戏更新(设置本地化新模式):
> 
> - 背景:游戏更新后 ModSettings 本地化失效——原代码在 InitializeSettings 时用 Locale.GetString 把名称/描述烤进 Setting,加载时序变化后设置显示为键名/空白
> - ModSettings.cs 全部10个设置改为游戏新版模式:CreateBool("{Droodism.ModSettings.Xxx}") + SetDescription("{...XxxDesc}") 键引用,由设置系统渲染时惰性解析(对齐游戏 CreateBool("{Settings.General.RunInBackground}"))
> - 关键修复:显式固定每个设置的 xmlName(CreateBool/CreateNumeric 的 xmlName 参数,沿用旧派生键如 toggleDebugMode)——存储键不再从本地化显示名派生,修复切换语言时设置被重置的问题,且兼容玩家已保存的设置
> - 本地化键(10名称+10描述)在 ZH-CN.xml / EN-US.xml 均已存在,无需新增
> 
> 修复 OnHireButtonClickedPatch 删掉原版资金确认提示的问题:恢复原版 "Crew.Assignment.HireConfirm" 的 OkayCancel 确认框(显示雇佣花费,可取消),确认后才弹出职业类型选择
> > 后续清理:
> >
> > 1. 其余5处硬编码路径解析(strArray+Split+逐级Find)统一改用 IPartSubPartSetUp.FindSubPart(MiningMachine/Sacrifice/Glider/HibernatingChamber/ResourcePack)
> > 2. 移除重构后残留的未使用 using(含 GliderScript 里一串 System.Windows.Forms 之类的垃圾引用)
> > 3. 修正类内部命名(不动类名):_particalSystemTransform→_particleSystemTransform / floatingFocrce→floatingForce / groudFix→groundFix / drillStut→drillStrut / LiquidHydrogenSouce→LiquidHydrogenSource,粗俗的 region 名改中性
> > 4. PositionOffset1 统一更名为 PositionOffset(5个Data + 5个Script + balloon.prefab 同步)
> > 5. FirstAddKit 命名问题已加 TODO 注释,待手动修正
> > 6. 冗余去重:新增 PartScriptUtilities(FindCraftFuelSource/GetCommandPodPatch),Modifiers 内12处 GetModifier<STCommandPodPatchScript> 长链改用 GetCommandPodPatch();GasDealer 的 GetCraftFuelSource 循环体改委托共享实现;Sacrifice 血粒子9字段改数组循环;ChemicalReactor 去掉重复的 monoSource 赋值
> > 7. 死代码清理:PhotoBioReactor 空 UpdateScale、MiningMachine 空 Test()、balloon 死 UpdateScale+空 FlightFixedUpdate(连带移除 IFlightFixedUpdate 接口),balloon 补 _sphere 空判



## 2026 9 5
> 抽象出IPartSubPartSetUp接口,统一了9个零件脚本的SetSubPart逻辑
> 
> > 将路径解析(FindSubPart)和偏移枢轴重建(ApplySubPart)合并进接口作为静态成员
> > 
> > CarbonDeoxideFilter / balloon / ElectrolyticDevice / ChemicalReactor / GasDealer / Flag / GravityRing / PhotoBioReactor / MethaloxGenerator 全部实现该接口
> > 
> > 顺手修了粒子系统路径尾斜杠导致直接查找失败的隐患,以及CarbonDeoxideFilterScript中UpdateComponents隐藏基类虚方法的问题
> > 
> > 接口内不再声明UpdateComponents,避免与基类生命周期钩子重复,每个脚本只保留一个UpdateComponents


## 2026 8 12
> 移除了legacyPause的补丁


## 2026 8 1
> 修复了UI中Drood的职业未本地化的bug
>

## 2026 7 25
> 增加旗帜留言功能
> 
> 修复flag 和 Gravity Ring的prefab的ID错误

## 2026 7 18
> 增加了零件分类名称,零件显示名称和描述的本地化

## 2026 7 15
> 增加了更多的本地化

## 2026 7 14
> 替换并移除了Override的fuelType
> >移除了高压二氧化碳和高压氮气
>
> 完成了UI本地化

## 2026 7 13
> 修复了PartAddingManager中不给cockpit添加STCommandPodPatch的bug

## 2026 7 11
> 为menuMapView添加了辐射带支持
> 
> 修复了一处CraftFuelSourcesPatches中的空引用导致游戏无法进入FlightScene的bug

## 2026 7 9
> 使用EMA算法对UI中的消耗速率进行了平滑处理,以减少UI中消耗速率显示的波动
> 
> 优化了一点点SupportLifeScript中RemoveFuelAmountInstantly的计算
> >虽然也没好到哪里去

## 2026 7 8
> 加入了回到旧版暂停逻辑的功能
> 

## 2026 7 7
> 加入了通过钩爪补充氧气的功能
> >顺便加入了通过钩爪移除二氧化碳的功能

## 2026 7 5
> 移植了JetEngineScript的大气组分扫描逻辑到SupportLifeScript
> 
> > SupportLifeScript.UsingInternalOxygen() 改为优先通过分析行星大气组分中的氧气质量分数来判断是否可呼吸
> > 
> > 新增 IsBreathableByComposition() 和 ComputeBreathable() 方法，带缓存机制
> > 
> > 行星大气中 LOX质量分数 >= 0.1即可呼吸
> > 
> > 保留原有 IsBreathablePlanet 名称白名单作为回退兼容
> 
>  提高了SupportLifeScript中可呼吸大气密度的阈值,密度必须大于0.4方可判定为使用外部氧气
> 
>增加了RTG功率衰减
## 2026 7 4
> 更新了SRCraftFuelSources.cs中的部分函数以适配新版本的CraftFuelSources和全局资源系统
> 
> 修复DesignerUI中部分空引用导致的数据显示异常
> 
> 增加自动本地化文件功能

## 2026 6 29
> 修改了SRCraftFuelSources中的部分函数以修复JetFuel和Mono无法被读取的错误
> 
> 增加部分本地化信息
> 
> UI中加入了新的按钮,可以直接选中Drood
## 2026 6 27
> 修复了IsRagdollActive的判定问题
> 
> 给工具箱新增了连接点
## 2026 6 27
> 修复了_crewCompartment.UnloadCrewMember的参数判定
> 
> 增加初步本地化选项

## 2026 6 26
> 更新modTool到 1.4版本
> 
> 根据新版本变动修改了SRCraftFuelSources中的部分函数以适配更新后的CraftFuelSources和全局资源系统
> 
> 对Propulsion.xml进行了密度调整,并覆写了部分1.4更新后新增的FuelType
> 
> 优化文件结构
> 
> 关于Mod.GetStopwatchTimeString的bug已经修复,故移除了此函数,改为Units.GetStopwatchTimeString()便于本地化

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




