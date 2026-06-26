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
            ConsumeResourceWhenUnloaded=CreateBool(Locale.GetString("Droodism.ModSettings.ConsumeResourceWhenUnloaded"))
                .SetDescription(Locale.GetString("Droodism.ModSettings.ConsumeResourceWhenUnloadedDesc"))
                .SetDefault(false);
            DebugMode=CreateBool(Locale.GetString("Droodism.ModSettings.ToggleDebugMode"))
                .SetDescription(Locale.GetString("Droodism.ModSettings.ToggleDebugModeDesc"))
                .SetDefault(false);
            AltNavBallColor=CreateBool(Locale.GetString("Droodism.ModSettings.AltNavBallColor"))
                .SetDescription(Locale.GetString("Droodism.ModSettings.AltNavBallColorDesc"))
                .SetDefault(false);
            ActiveUpdateRadiationBeltConfig=CreateBool(Locale.GetString("Droodism.ModSettings.ActiveUpdateRadiationBeltConfig"))
                .SetDescription(Locale.GetString("Droodism.ModSettings.ActiveUpdateRadiationBeltConfigDesc"))
                .SetDefault(false);
            ReceiveCraftRadiation=CreateBool(Locale.GetString("Droodism.ModSettings.ReceiveCraftRadiation"))
                .SetDescription(Locale.GetString("Droodism.ModSettings.ReceiveCraftRadiationDesc"))
                .SetDefault(false);
            RemoveDockingPortCamera=CreateBool(Locale.GetString("Droodism.ModSettings.DisableDockingPortCamera"))
                .SetDescription(Locale.GetString("Droodism.ModSettings.DisableDockingPortCameraDesc"))
                .SetDefault(false);
            EnableResourceWarning = CreateBool(Locale.GetString("Droodism.ModSettings.EnableResourceWarning"))
                .SetDescription(Locale.GetString("Droodism.ModSettings.EnableResourceWarningDesc"))
                .SetDefault(true);
            ResourceWarningThreshold = CreateNumeric<float>(Locale.GetString("Droodism.ModSettings.WarningThreshold"),0.2f,0.5f,0.05f)
                .SetDescription(Locale.GetString("Droodism.ModSettings.WarningThresholdDesc"))
                .SetDefault(0.25f);
            ResourceCriticalThreshold = CreateNumeric<float>(Locale.GetString("Droodism.ModSettings.CriticalThreshold"),0.05f,0.2f,0.01f)
                .SetDescription(Locale.GetString("Droodism.ModSettings.CriticalThresholdDesc"))
                .SetDefault(0.10f);
        }
    }
}