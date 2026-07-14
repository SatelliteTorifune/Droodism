using System.Linq.Expressions;
using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using Assets.Scripts;
using Assets.Scripts.Droodism.Crew;
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
    using ModApi.Craft.Parts;
    using ModApi.Craft.Parts.Attributes;
    using UnityEngine;

    [Serializable]
    [DesignerPartModifier("SupportLife",PanelOrder = 2000)]
    [PartModifierTypeId("SupportLife")]
    public class SupportLifeData : PartModifierData<SupportLifeScript>
    {
        private static bool isTourist;
        
        [FormerlySerializedAs("_oxygenComsumeRate")] [SerializeField] [PartModifierProperty]
        private float oxygenConsumeRate=1f;
        [FormerlySerializedAs("_foodComsumeRate")] [SerializeField][PartModifierProperty]
        private float foodConsumeRate=1f;
        [FormerlySerializedAs("_waterComsumeRate")] [SerializeField][PartModifierProperty]
        private float waterConsumeRate=1f;
        
        [FormerlySerializedAs("_oxygenDamageScale")] [SerializeField] [PartModifierProperty]
        private float oxygenDamageScale=1f;
        [SerializeField][PartModifierProperty]
        private float foodDamageScale=1f;
        [SerializeField][PartModifierProperty]
        private float waterDamageScale=1f;

        [SerializeField] [DesignerPropertyLabel(Order=-3)]
        private string crewName = "Unknow";
        [SerializeField] [DesignerPropertyLabel(Order=-2)]
        private string crewRoleName = "Unknow";
        [SerializeField] [DesignerPropertyLabel(Order=-1)]
        private string crewRadiationDoes = "Unknow"; 
        [SerializeField] [DesignerPropertyLabel(Order=0)]
        private string crewMissionTime = "Unknow";
        [SerializeField] 
        [DesignerPropertySlider(0.1f, 3f, 30, Label = "<color=green>Oxygen</color> Carry Amount(days)",Order = 4, Tooltip = "How much <color=green>Oxygen</color> Drood himself/herself will carry when Eva.")]
        private float desireOxygenCapacity = 0.2f;
        [SerializeField] [DesignerPropertySlider(0.1f, 3f, 30, Label = "<color=yellow>Food</color> Carry Amount(days)",Order = 5, Tooltip = "How much <color=yellow>Food</color> Drood himself/herself will carry when Eva.")]
        private float desireFoodCapacity = 0.2f;
        [SerializeField] [DesignerPropertySlider(0.1f, 3f, 30, Label = "<color=red>Water</color> Carry Amount(days)",Order = 6, Tooltip = "How much<color=red> Drinking Water</color> Drood himself/herself will carry when Eva.")]
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
        
        [SerializeField][PartModifierProperty]
        private double cumulativeRad=0f;
        [SerializeField][PartModifierProperty]
        public long MissionStartTime=0;
        [SerializeField][PartModifierProperty]
        public long LastLoadTime=0;
        
        [SerializeField] [PartModifierProperty]
        public double _oxygenAmountBuffer=0f;
        [SerializeField] [PartModifierProperty]
        public double _foodAmountBuffer=0f;
        [SerializeField] [PartModifierProperty]
        public double _waterAmountBuffer = 0f;
        [SerializeField] [PartModifierProperty]
        public double _co2AmountBuffer=0f;
        [SerializeField] [PartModifierProperty]
        public double _wastedWaterAmountBuffer=0f;
        [SerializeField] [PartModifierProperty]
        public double _solidWasteAmountBuffer=0f;
        [SerializeField] [PartModifierProperty]
        public float evaConsumeEfficiency=0.3f;
        [SerializeField] [PartModifierProperty]
        public float radiationDamageThresholdLevel1=100f;
        [SerializeField] [PartModifierProperty]
        public float radiationDamageThresholdLevel2=400f;
        [SerializeField] [PartModifierProperty]
        public float radiationDamageThresholdLevel3=800f;

        [SerializeField] [PartModifierProperty]
        public float utilizationFactor = 300;
        public float UtilizationFactor
        {
            get => this.utilizationFactor;
            set => this.utilizationFactor = value;
        }
        public float OxygenConsumeRate
        {
            get => this.oxygenConsumeRate * 0.007f; 
            private set => this.oxygenConsumeRate = Mathf.Max(0f, value);
        }
        public float FoodConsumeRate
        {
            get => this.foodConsumeRate * 0.0000058f;
            set => this.foodConsumeRate = Mathf.Max(0f, value);
        }

        public float WaterConsumeRate
        {
            get => this.waterConsumeRate * 0.0000347f;
            set => this.waterConsumeRate = Mathf.Max(0f, value);
        }
        public float OxygenDamageScale
        {
            get => this.oxygenDamageScale * 0.35f;
            set => this.oxygenDamageScale = Mathf.Max(0f, value);
        }
        public float FoodDamageScale
        {
            get => this.foodDamageScale * 0.0002f;
            set => this.foodDamageScale = Mathf.Max(0f, value);
        }
        
        public float WaterDamageScale
        {
            get => this.waterDamageScale * 0.001f;
            set => this.waterDamageScale = Mathf.Max(0f, value);
        }
        
        public float DesireOxygenCapacity
        {
            get => this.desireOxygenCapacity * 600;
            private set => this.desireOxygenCapacity = Mathf.Max(0f, value);
        }
        
        public float DesireFoodCapacity
        {
            get => this.desireFoodCapacity * 0.5f;
            set => this.desireFoodCapacity = Mathf.Max(0f, value);
        }
        
        public float DesireWaterCapacity
        {
            get => this.desireWaterCapacity * 3;
            set => this.desireWaterCapacity = Mathf.Max(0f, value);
        }

        public float DesireCO2Capacity
        {
            get => 252f;
        }

        public float DesireWastedWaterCapacity
        {
            get => 1.05f;
        }

        public float DesireSolidWasteCapacity
        {
            get => 0.1f;
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
            
            d.OnVisibilityRequested<float>((Expression<Func<float>>) (() => this.autoDeployHeight), (Func<bool, bool>) (x => this._parachuteType!="None"));
            d.OnVisibilityRequested<bool>((Expression<Func<bool>>) (() => this.autoDeployEnabled), (Func<bool, bool>) (x => this._parachuteType!="None"));
            d.OnVisibilityRequested<float>((Expression<Func<float>>) (() => this.minDeployHeight), (Func<bool, bool>) (x => this._parachuteType!="None"));
            d.OnVisibilityRequested<string>((Expression<Func<string>>) (() => this.crewName), (Func<bool, bool>) (x => !this.Part.GetModifier<EvaData>().IsTourist));
            d.OnVisibilityRequested<string>((Expression<Func<string>>) (() => this.crewRoleName), (Func<bool, bool>) (x => !this.Part.GetModifier<EvaData>().IsTourist));
            d.OnVisibilityRequested<string>((Expression<Func<string>>) (() => this.crewRadiationDoes), (Func<bool, bool>) (x => !this.Part.GetModifier<EvaData>().IsTourist));
            d.OnVisibilityRequested<string>((Expression<Func<string>>) (() => this.crewMissionTime), (Func<bool, bool>) (x => !this.Part.GetModifier<EvaData>().IsTourist));
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
            this.crewName = DroodismCrewData == null
                ? "<color=yellow>Crew Name</color>: Unknow"
                : "<color=yellow>Crew Name</color>: " + evaData.CrewName;
            this.crewRoleName = DroodismCrewData == null ? "<color=yellow>Crew Role</color>: Unknow" : GetCrewRoleName();
            this.crewRadiationDoes=DroodismCrewData == null ? "<color=yellow>Radiation Dose: Unknow" : "<color=yellow>Radiation Dose:"+(DroodismCrewData.RadiationRate.ToString("f1")+" rad");
            this.crewMissionTime = DroodismCrewData == null ? "<color=yellow>Total Mission Time</color>: Unknow" : "<color=yellow>Total Mission Time</color>: " + Units.GetStopwatchTimeString(DroodismCrewData.MissionTime);
        }

        /// <summary>维生罐最大容量（与旧 AddTank 逻辑一致，单位与 buffer 一致）。</summary>
        public double GetLifeSupportCapacity(string fuelTypeId)
        {
            switch (fuelTypeId)
            {
                case "Oxygen": return DesireOxygenCapacity;
                case "Food": return DesireFoodCapacity;
                case "H2O": return DesireWaterCapacity;
                case "LPCO2": return DesireCO2Capacity;
                case "Wasted Water": return DesireWastedWaterCapacity;
                case "Solid Waste": return DesireSolidWasteCapacity;
                default: return 0.0;
            }
        }

        public double GetLifeSupportFuelAmount(string fuelTypeId)
        {
            switch (fuelTypeId)
            {
                case "Oxygen": return _oxygenAmountBuffer;
                case "Food": return _foodAmountBuffer;
                case "H2O": return _waterAmountBuffer;
                case "LPCO2": return _co2AmountBuffer;
                case "Wasted Water": return _wastedWaterAmountBuffer;
                case "Solid Waste": return _solidWasteAmountBuffer;
                default: return 0.0;
            }
        }

        public void SetLifeSupportFuelAmount(string fuelTypeId, double value)
        {
            double cap = GetLifeSupportCapacity(fuelTypeId);
            if (cap <= 0.0)
                return;
            value = Math.Max(0.0, Math.Min(value, cap));
            switch (fuelTypeId)
            {
                case "Oxygen": _oxygenAmountBuffer = value; break;
                case "Food": _foodAmountBuffer = value; break;
                case "H2O": _waterAmountBuffer = value; break;
                case "LPCO2": _co2AmountBuffer = value; break;
                case "Wasted Water": _wastedWaterAmountBuffer = value; break;
                case "Solid Waste": _solidWasteAmountBuffer = value; break;
            }
        }

        public void AddLifeSupportFuel(string fuelTypeId, double delta)
        {
            if (Math.Abs(delta) < 1e-12)
                return;
            SetLifeSupportFuelAmount(fuelTypeId, GetLifeSupportFuelAmount(fuelTypeId) + delta);
        }
        

        private string GetCrewRoleName()
        {
            string Description = 
                DroodismCrewData.CrewRole == DroodType.Engineer ? "Enginner Could Fix Parts" : DroodismCrewData.CrewRole == DroodType.Scientist ? "Scientist Could Increase more Science Experiment outcome(LMAO i didn't even implement this)" : 
                    DroodismCrewData.CrewRole ==DroodType.Pilot ?
                "Basic Drood which is good at taking control of the craft":
                "Unknow Drood Type";
            string color = DroodismCrewData.CrewRole == DroodType.Engineer ? "#0072FF" :
                DroodismCrewData.CrewRole == 
                DroodType.Scientist ? "#62BF05" : DroodismCrewData.CrewRole == DroodType.Pilot?"#FF0003":"white";
            return "<color=yellow>Crew Role</color>: "+"<color="+color+">"+DroodismCrewData.CrewRole+"</color><br>"+Description;
        }
        

        public override void OnPartRecovered()
        {
            base.OnPartRecovered();
            try
            {
                this.Script.SaveDroodismCrewData();
            }
            catch(Exception e)
            {
                Scripts.Mod.Log("Droodism.SupportLifeData.OnPartRecovered"+e);
            }
           
        }
        #endregion
    }
}