namespace Assets.Scripts
{
    using ModApi.Settings.Core;

    /// <summary>
    /// The settings for the mod.
    /// </summary>
    /// <seealso cref="ModApi.Settings.Core.SettingsCategory{Assets.Scripts.ModSettings}" />
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
            ConsumeResourceWhenUnloaded=CreateBool("Drood Consume Resource When Unloaded")
                .SetDescription("Drood will still Consume Resource Even the Craft is Unloaded.<br>known bug the time calculate went a <size=125%><color=red>LITTLE BIT</color></size> wrong way")
                .SetDefault(false);
            DebugMode=CreateBool("Toggle Debug Mode")
                .SetDescription("Enable some log in dev console,and toggle some hidden option")
                .SetDefault(false);
            AltNavBallColor=CreateBool("Alt Nav Ball Color")
                .SetDescription("Use Alt Nav Ball Color")
                .SetDefault(false);
            ActiveUpdateRadiationBeltConfig=CreateBool("Active Update Radiation Belt Config")
                .SetDescription("Active Update Radiation Belt Config(Cause Performance loss,but better for debugging)")
                .SetDefault(false);
            ReceiveCraftRadiation=CreateBool("Receive Craft Radiation")
                .SetDescription("Drood will Receive radiation from the RTG and NTR engines in current craft if on,maybe not so realistic,<color=red><size=150%>Enable only you like hardcore gameplay</size></color>")
                .SetDefault(false);
            RemoveDockingPortCamera=CreateBool("Disable Docking Port Camera")
                .SetDescription("Remove the camera from the docking port<br><color=red>Warning</color>: This will completely disabled <color=red><size=200%>ANY</size></color>camera modifier in<color=red><size=200%> ANY</size></color> docking ports (even if they're in flight already!), use it only if you really hate them")
                .SetDefault(false);
            EnableResourceWarning = CreateBool("Enable Resource Warning")
                .SetDescription("During warp, automatically detect  when Drood resources fall below threshold and pause game then alert the player.")
                .SetDefault(true);
            ResourceWarningThreshold = CreateNumeric<float>("Warning Threshold",0.2f,0.5f,0.05f)
                .SetDescription("Warning will trigger when resources fall below this percentage.")
                .SetDefault(0.25f);
            ResourceCriticalThreshold = CreateNumeric<float>("Critical Threshold",0.05f,0.2f,0.01f)
                .SetDescription("Critical alert will trigger when resources fall below this percentage. Auto-slowdown is triggered at this level.")
                .SetDefault(0.10f);
        }
    }
}