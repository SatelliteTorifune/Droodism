using ModApi;

namespace Assets.Scripts
{
    using ModApi.Settings.Core;

    /// <summary>
    /// The settings for the mod.
    /// </summary>
    /// <seealso cref="ModSettings" />
    public class ModSettings : SettingsCategory<ModSettings>
    {
        /// <summary>
        /// The mod settings instance.
        /// </summary>
        private static ModSettings _instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModSettings"/> class.
        /// </summary>
        public ModSettings() : base("Droodism")
        {
        }

        /// <summary>
        /// Gets the mod settings instance.
        /// </summary>
        /// <value>
        /// The mod settings instance.
        /// </value>
        public static ModSettings Instance => _instance ?? (_instance = Game.Instance.Settings.ModSettings.GetCategory<ModSettings>());

        ///// <summary>
        ///// Gets the TestSetting1 value
        ///// </summary>
        ///// <value>
        ///// The TestSetting1 value.
        ///// </value>
        //public NumericSetting<float> TestSetting1 { get; private set; }
        public BoolSetting ConsumeResourceWhenUnloaded { get; set; }
        public BoolSetting DebugMode { get; set; }
        public BoolSetting ActiveUpdateRadiationBeltConfig { get; set; }
        public BoolSetting EnableResourceWarning { get; set; }
        public NumericSetting<float> ResourceWarningThreshold { get; set; }
        public NumericSetting<float> ResourceCriticalThreshold { get; set; }

        public BoolSetting AltNavBallColor { get; set; }
        public BoolSetting RemoveDockingPortCamera { get; set; }

        public BoolSetting ReceiveCraftRadiation { get; set; }
        

        /// <summary>
        /// Initializes the settings in the category.
        /// </summary>
        protected override void InitializeSettings()
        {
            // 注意:设置名称/描述使用 "{...}" 本地化键引用(游戏新版模式),由设置系统在渲染时惰性解析。
            // xmlName 显式固定为语言无关的稳定 id(沿用旧代码的派生键,可兼容玩家已保存的设置);
            // 若不指定 xmlName,存储键会从显示名派生,而显示名是本地化引用,切换语言后存储键随之改变,导致设置被重置。
            ConsumeResourceWhenUnloaded = (BoolSetting)CreateBool("{Droodism.ModSettings.ConsumeResourceWhenUnloaded}", "consumeResourceWhenUnloaded")
                .SetDescription("{Droodism.ModSettings.ConsumeResourceWhenUnloadedDesc}")
                .SetDefault(false);
            DebugMode = (BoolSetting)CreateBool("{Droodism.ModSettings.ToggleDebugMode}", "toggleDebugMode")
                .SetDescription("{Droodism.ModSettings.ToggleDebugModeDesc}")
                .SetDefault(false);
            AltNavBallColor = (BoolSetting)CreateBool("{Droodism.ModSettings.AltNavBallColor}", "altNavBallColor")
                .SetDescription("{Droodism.ModSettings.AltNavBallColorDesc}")
                .SetDefault(false);
            ActiveUpdateRadiationBeltConfig = (BoolSetting)CreateBool("{Droodism.ModSettings.ActiveUpdateRadiationBeltConfig}", "activeUpdateRadiationBeltConfig")
                .SetDescription("{Droodism.ModSettings.ActiveUpdateRadiationBeltConfigDesc}")
                .SetDefault(false);
            ReceiveCraftRadiation = (BoolSetting)CreateBool("{Droodism.ModSettings.ReceiveCraftRadiation}", "receiveCraftRadiation")
                .SetDescription("{Droodism.ModSettings.ReceiveCraftRadiationDesc}")
                .SetDefault(false);
            RemoveDockingPortCamera = (BoolSetting)CreateBool("{Droodism.ModSettings.DisableDockingPortCamera}", "disableDockingPortCamera")
                .SetDescription("{Droodism.ModSettings.DisableDockingPortCameraDesc}")
                .SetDefault(false);
            EnableResourceWarning = (BoolSetting)CreateBool("{Droodism.ModSettings.EnableResourceWarning}", "enableResourceWarning")
                .SetDescription("{Droodism.ModSettings.EnableResourceWarningDesc}")
                .SetDefault(true);
            ResourceWarningThreshold = CreateNumeric<float>("{Droodism.ModSettings.WarningThreshold}", 0.2f, 0.5f, 0.05f, "warningThreshold")
                .SetDescription("{Droodism.ModSettings.WarningThresholdDesc}")
                .SetDefault(0.25f);
            ResourceCriticalThreshold = CreateNumeric<float>("{Droodism.ModSettings.CriticalThreshold}", 0.05f, 0.2f, 0.01f, "criticalThreshold")
                .SetDescription("{Droodism.ModSettings.CriticalThresholdDesc}")
                .SetDefault(0.10f);
        }
    }
}