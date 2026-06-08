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
        
        public BoolSetting AltNavBallColor { get; set; }
        public BoolSetting RemoveDockingPortCamera { get; set; }

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
            RemoveDockingPortCamera=CreateBool("Disable Docking Port Camera")
                .SetDescription("Remove the camera from the docking port<br><color=red>Warning</color>: This will completely disabled <color=red><size=200%>ANY</size></color>camera modifier in<color=red><size=200%> ANY</size></color> docking ports (even if they're in flight already!), use it only if you really hate them")
                .SetDefault(false);
        }
    }
}