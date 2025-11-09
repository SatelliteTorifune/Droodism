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
        public BoolSetting UseLegacyUI { get; set; }
        public BoolSetting ShowDevLog { get; set; }
        
        public BoolSetting AltNavBallColor { get; set; }

        /// <summary>
        /// Initializes the settings in the category.
        /// </summary>
        protected override void InitializeSettings()
        {
            ConsumeResourceWhenUnloaded=CreateBool("Drood Consume Resource When Unloaded")
                .SetDescription("Drood will still Consume Resource Even the Craft is Unloaded.<br>known bug the time calculate went a <size=125%><color=red>LITTLE BIT</color></size> wrong way")
                .SetDefault(false);
            UseLegacyUI=CreateBool("Use Legacy UI")
                .SetDescription("Game will use the old UI")
                .SetDefault(false);
            ShowDevLog=CreateBool("show development log in dev console")
                .SetDescription("")
                .SetDefault(true);
            AltNavBallColor=CreateBool("Alt Nav Ball Color")
                .SetDescription("Use Alt Nav Ball Color")
                .SetDefault(false);
        }
    }
}