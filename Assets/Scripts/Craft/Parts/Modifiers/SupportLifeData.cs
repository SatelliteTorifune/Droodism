using System.Linq.Expressions;
using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using Assets.Scripts.Droodism;
using ModApi.Craft.Propulsion;
using ModApi.Design.PartProperties;
using Assets.Scripts.State;
using ModApi.Math;
using UnityEngine.Serialization;

//去你妈的我要躺在床上对着梅莉的蕾丝边小白袜撸管子,谁他妈想写这东西
//不是这都他妈啥啊
//吗的为啥我要把序列化相关写这里?
namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Linq;
    using ModApi.Craft.Parts;
    using ModApi.Craft.Parts.Attributes;
    using UnityEngine;

    [Serializable]
    [DesignerPartModifier("SupportLife",PanelOrder = 2000)]
    [PartModifierTypeId("SupportLife")]
    public class SupportLifeData : PartModifierData<SupportLifeScript>
    {
        private static bool isTourist;
        
        [FormerlySerializedAs("_oxygenComsumeRate")] [SerializeField] [PartModifierProperty(true, false)]
        private float oxygenConsumeRate=1f;
        [FormerlySerializedAs("_foodComsumeRate")] [SerializeField][PartModifierProperty(true, false)]
        private float foodConsumeRate=1f;
        [FormerlySerializedAs("_waterComsumeRate")] [SerializeField][PartModifierProperty(true, false)]
        private float waterConsumeRate=1f;
        
        [FormerlySerializedAs("_oxygenDamageScale")] [SerializeField] [PartModifierProperty(true, false)]
        private float oxygenDamageScale=1f;
        [SerializeField][PartModifierProperty(true, false)]
        private float foodDamageScale=1f;
        [SerializeField][PartModifierProperty(true, false)]
        private float waterDamageScale=1f;

        [SerializeField] [DesignerPropertyLabel(Order=-2)]
        private string crewRoleName = "Unknow";
        [SerializeField] [DesignerPropertyLabel(Order=-1)]
        private string crewRadiationDoes = "Unknow";
        [SerializeField] 
        [DesignerPropertySlider(0.1f, 3f, 30, Label = "<color=green>Oxygen</color> Carry Amount(days)",Order = 4, Tooltip = "How much <color=green>Oxygen</color> Drood himself/herself will carry when Eva.")]
        private float desireOxygenCapacity = 0.2f;
        [SerializeField] [DesignerPropertySlider(0.1f, 3f, 30, Label = "<color=yellow>Food</color> Carry Amount(days)",Order = 5, Tooltip = "How much <color=yellow>Food</color> Drood himself/herself will carry when Eva.")]
        private float desireFoodCapacity = 0.2f;
        [SerializeField] [DesignerPropertySlider(0.1f, 3f, 30, Label = "<color=red>Water</color> Carry Amount(days)",Order = 6, Tooltip = "How much<color=red> Drink Water</color> Drood himself/herself will carry when Eva.")]
        private float desireWaterCapacity = 0.2f;
        
        [SerializeField]
        [DesignerPropertySpinner(Label = "<color=yellow>Chute Type</color>", Order = 0, Tooltip = "The type of parachute this drood brings.")]
        private string _parachuteType = "Parachute";

        [SerializeField] 
        [DesignerPropertySlider(100f, 1000f, 60, Label = "Min Deploy Height",Order=1, Tooltip = "Minimum height for parachute deployment")]
        private float minDeployHeight = 250f;

        [SerializeField]
        [DesignerPropertyToggleButton(Label = "<color=#FFB600>Auto Deploy Parachute</color>", Order = 2,
            Tooltip = "Auto Deploy Parachute or not")]
        private bool autoDeployEnabled;
        [SerializeField] 
        [DesignerPropertySlider(100f, 1000f, 60, Label = "<color=#FFB600>Auto Deploy Height</color>",Order = 3, Tooltip = "Height for auto parachute deployment in Agl")]
        private float autoDeployHeight = 500f;
        
        [SerializeField][PartModifierProperty(true, false)]
        private double cumulativeRad=0f;
        [SerializeField][PartModifierProperty]
        public long MissionStartTime=0;
        [SerializeField][PartModifierProperty]
        public long LastLoadTime=0;
        
        [SerializeField] [PartModifierProperty(true, false)]
        public double _oxygenAmountBuffer=0f;
        [SerializeField] [PartModifierProperty(true, false)]
        public double _foodAmountBuffer=0f;
        [SerializeField] [PartModifierProperty(true, false)]
        public double _waterAmountBuffer = 0f;
        [SerializeField] [PartModifierProperty(true, false)]
        public double _co2AmountBuffer=0f;
        [SerializeField] [PartModifierProperty(true, false)]
        public double _wastedWaterAmountBuffer=0f;
        [SerializeField] [PartModifierProperty(true, false)]
        public double _solidWasteAmountBuffer=0f;
        [SerializeField] [PartModifierProperty(true, false)]
        public float evaConsumeEfficiency=0.3f;
        [SerializeField] [PartModifierProperty(true, false)]
        public float radiationDamageThresholdLevel1=100f;
        [SerializeField] [PartModifierProperty(true, false)]
        public float radiationDamageThresholdLevel2=400f;
        [SerializeField] [PartModifierProperty(true, false)]
        public float radiationDamageThresholdLevel3=800f;
        
        public float OxygenConsumeRate
        {
            get =>IsLegal(this.oxygenConsumeRate)*0.007f; 
            private set=>this.oxygenConsumeRate = value;
        }
        public float FoodConsumeRate
        {
            get=>this.IsLegal(foodConsumeRate)*0.0000058f;
            set=>this.foodConsumeRate = value;
        }

        public float WaterConsumeRate
        {
            get => IsLegal(waterConsumeRate)*0.0000347f;
            set => this.waterConsumeRate = value;
        }
        public float OxygenDamageScale
        {
            get=>IsLegal(this.oxygenDamageScale)*0.35f;
            set=>this.oxygenDamageScale = value;
        }
        public float FoodDamageScale
        {
            get=>IsLegal(this.foodDamageScale)*0.0002f;
            set=>this.foodDamageScale = value;
        }
        
        public float WaterDamageScale
        {
            get=>IsLegal(waterDamageScale)*0.001f;
            set=>this.waterDamageScale = value;
        }
        
        public float DesireOxygenCapacity
        {
            get=>IsLegal(desireOxygenCapacity)*600;
            private set=>this.desireOxygenCapacity = value;
        }
        
        public float DesireFoodCapacity
        {
            get=>this.IsLegal(desireFoodCapacity)*0.5f;
            set=>this.desireFoodCapacity = value;
        }
        
        public float DesireWaterCapacity
        {
            get=>this.IsLegal(this.desireWaterCapacity)*3;
            set=>this.desireWaterCapacity = value;
        }

        public double CumulativeRad
        {
            get=>this.cumulativeRad;
            set=>this.cumulativeRad=value;
        }
        
        public string ParachuteTypes
        {
            get => this._parachuteType;
        }

        public float RadiationDamageThresholdLevel1
        {
            get => radiationDamageThresholdLevel1;
        }

        public float RadiationDamageThresholdLevel2
        {
            get => radiationDamageThresholdLevel2;
        }

        public float RadiationDamageThresholdLevel3
        {
            get => radiationDamageThresholdLevel3;
        }
        private float IsLegal(float value)
        {
            return value>0?value:1;
        }

        public float MinDeployHeight
        {
            get=> Mathf.Min(this.autoDeployHeight, this.minDeployHeight);
            set
            {
                minDeployHeight=Mathf.Min(value, this.autoDeployHeight);
            }
        }

        public bool AutoDeployEnabled
        {
            get => autoDeployEnabled;
            set =>autoDeployEnabled=value;
        }

        public float AutoDeployHeight
        {
            get => autoDeployHeight;
            set => autoDeployHeight=value;
        }
        public DroodismCrewData  DroodismCrewData{get;private set;}
        protected override void OnDesignerInitialization(IDesignerPartPropertiesModifierInterface d)
        {
            base.OnDesignerInitialization(d);
            d.OnValueLabelRequested<string>(() => this._parachuteType, x => x);
            d.OnSpinnerValuesRequested<string>(() => this._parachuteType, this.GetSpinnerValues);
            d.OnValueLabelRequested(() => this.minDeployHeight, s => Units.GetDistanceString(Mathf.Min(s,autoDeployHeight)));
            d.OnValueLabelRequested(() => this.autoDeployHeight, s => Units.GetDistanceString(s));
            
            d.OnPropertyChanged<float>((Expression<Func<float>>) (() => this.minDeployHeight), (Action<float, float>) ((newVal, oldVal) =>
            {
                this.minDeployHeight = Mathf.Min(minDeployHeight,this.autoDeployHeight);
                d.Manager.RefreshUI();
            }));
            d.OnPropertyChanged<float>((Expression<Func<float>>) (() => this.autoDeployHeight), (Action<float, float>) ((newVal, oldVal) =>
            {
                this.minDeployHeight = Mathf.Min(this.autoDeployHeight, this.minDeployHeight);
                d.Manager.RefreshUI();
            }));
            d.OnVisibilityRequested<string>((Expression<Func<string>>) (() => this.crewRoleName), (Func<bool, bool>) (x => !this.Part.GetModifier<EvaData>().IsTourist));
            d.OnVisibilityRequested<string>((Expression<Func<string>>) (() => this.crewRadiationDoes), (Func<bool, bool>) (x => !this.Part.GetModifier<EvaData>().IsTourist));
        }


        private void GetSpinnerValues(List<string> chuteTypes)
        {
            chuteTypes.Clear();
            chuteTypes.Add("None");
            chuteTypes.Add("ParaGlider");
            chuteTypes.Add("Parachute");
        }

        #region functions
        
        
        protected override void OnInitialized()
        {
            base.OnInitialized();
            SetDroodismCrewData();

        }

        public void SetDroodismCrewData()
        {
            var evaData = Part.GetModifier<EvaData>();
            DroodismCrewData =
                evaData.CrewName == "Unassigned"
                    ? null
                    : DroodismCrewDataManager.Instance.GetCrewMember(evaData.CrewId);
            this.crewRoleName = DroodismCrewData == null ? "<color=yellow>Crew Role</color>: Unknow" : GetCrewRoleName();
            this.crewRadiationDoes=DroodismCrewData == null ? "<color=yellow>Radiation Dose: Unknow" : "<color=yellow>Radiation Dose:"+(DroodismCrewData.RadiationRate.ToString("f1")+" rad");
        }

        private string GetCrewRoleName()
        {
            string Description = DroodismCrewData.CrewRole == DroodType.Engineer ? "Enginner Could Fix Parts" :
                DroodismCrewData.CrewRole == DroodType.Scientist ? "Scientist Could Increase more Science Experiment outcome(LMAO i didn't even implement this)" :
                "Basic Drood which is good at taking control of the shit";
            string color = DroodismCrewData.CrewRole == DroodType.Engineer ? "#00DD9F" :
                DroodismCrewData.CrewRole == DroodType.Scientist ? "#62BF05" : "#BF2605";
            return "<color=yellow>Crew Role</color>: "+"<color="+color+">"+DroodismCrewData.CrewRole+"</color><br>"+Description;
        }
        

        public override void OnPartRecovered()
        {
            base.OnPartRecovered();
            
            this.Script.SaveDroodismCrewData();
        }
        #endregion
    }
}