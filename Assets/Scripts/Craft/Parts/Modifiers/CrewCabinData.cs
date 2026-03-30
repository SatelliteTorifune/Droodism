using System.Collections.Generic;
using System.Linq.Expressions;
using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using ModApi.Design.PartProperties;
using ModApi.Math;


namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System;
    using ModApi.Craft.Parts;
    using ModApi.Craft.Parts.Attributes;
    using UnityEngine;

    [Serializable]
    [DesignerPartModifier("CrewCabin")]
    [PartModifierTypeId("CrewCabin")]
    public class CrewCabinData : PartModifierData<CrewCabinScript>
    {
        
        [SerializeField] [DesignerPropertySlider(10f, 200f, 90, Label = "<color=yellow>Radiation Shield Amount",Order=1, Tooltip = "Current Radiation Shield Amount of this Crew Compartment, could greatly impact the mass of this part")]
        private double radiationShieldDuration = 100d;
        [SerializeField] [PartModifierProperty(true, false)]
        private double radiationShieldDurationUpperLimit = 100d;

        [SerializeField]  [DesignerPropertySpinner(Label = "<color=yellow>Radiation Shield Type</color>", Order = 0, Tooltip = "The type of Radiation Shield this Compartment brings.")]
        private string radiationShieldType = "None";

        public double RadiationShieldDuration
        {
            get=> radiationShieldDuration;
            set
            {
                // Clamp to [0, UpperLimit] to avoid negative or overflowed values
                var clamped = value;
                if (clamped < 0d) clamped = 0d;
                if (clamped > radiationShieldDurationUpperLimit) clamped = radiationShieldDurationUpperLimit;
                radiationShieldDuration = clamped;
            }
        }

        public double RadiationShieldDurationUpperLimit
        {
            get => radiationShieldDurationUpperLimit;
        }

        public string RadiationShieldType
        {
            get => radiationShieldType;
        }

        /// <summary>
        /// 根据护盾材料为耐久上限设定一个默认值，并对当前耐久进行钳制。
        /// </summary>
        private void ApplyShieldMaterialDefaultDuration()
        {
            
            radiationShieldDurationUpperLimit = GetShieldDurabilityUpperLimitByType(radiationShieldType);
            if (radiationShieldDuration > radiationShieldDurationUpperLimit)
            {
                radiationShieldDuration = radiationShieldDurationUpperLimit;
            }
            if (radiationShieldDuration < 0d)
            {
                radiationShieldDuration = 0d;
            }
        }

        /// <summary>
        /// 不同材料的最大耐久
        /// 数值可按平衡需要再调整,
        /// 这个b override不想写一点
        /// </summary>
        public static double GetShieldDurabilityUpperLimitByType(string type)
        {
            switch (type)
            {
                case "None": return 0d;
                case "Aluminium": return 80d;                 // 电子屏蔽较好，整体预算较低
                case "PolyEthylene": return 150d;             // 轻且对质子更优
                case "Borated PolyEthylene": return 160d;     // 复合吸收中子，略高
                case "Water": return 120d;                    // 多功能，适中
                case "Liquid Hydrogen": return 200d;          // 理论最佳，最高预算
                case "Boron Nitride Nanotubes": return 140d;  // 复合材料，较高
                default: return 0d;
            }
        }

        protected override void OnDesignerInitialization(IDesignerPartPropertiesModifierInterface d)
        {
            base.OnDesignerInitialization(d);
            d.OnValueLabelRequested<string>(() => this.radiationShieldType, x => x);
            d.OnSpinnerValuesRequested<string>(() => this.radiationShieldType, this.GetSpinnerValues);
            d.OnPropertyChanged<string>((Expression<Func<string>>) (() => this.radiationShieldType), (Action<string, string>) ((newVal, oldVal) =>
            {
                this.OnPropertyChangedInDesigner();
                d.Manager.RefreshUI();
            }));
            d.OnValueLabelRequested(() => this.radiationShieldDuration, s => RadiationShieldDurationUpperLimit==0?
                "NaN":
                Units.GetPercentageString((float)(this.Min(s, this.RadiationShieldDurationUpperLimit) /RadiationShieldDurationUpperLimit)));
            d.OnPropertyChanged<double>((Expression<Func<double>>) (() => this.radiationShieldDuration), (Action<double, double>) ((newVal, oldVal) =>
            {
                var clamped = this.Min(newVal, this.RadiationShieldDurationUpperLimit);
                if (clamped < 0d) clamped = 0d;
                this.radiationShieldDuration = clamped;
                d.Manager.RefreshUI();
            }));
        }

        private void OnPropertyChangedInDesigner()
        {
            this.ApplyShieldMaterialDefaultDuration();
        }



        private void GetSpinnerValues(List<string> shieldTypes)
        {
            shieldTypes.Clear();
            shieldTypes.Add("None");
            shieldTypes.Add("Aluminium");
            shieldTypes.Add("PolyEthylene");
            shieldTypes.Add("Borated PolyEthylene");
            shieldTypes.Add("Water");
            shieldTypes.Add("Liquid Hydrogen");
            shieldTypes.Add("Boron Nitride Nanotubes");
        }

        internal float GetMassFactor()
        {
            switch (this.RadiationShieldType)
            {
                case "None" : return 1f;
                    break;
                case "Aluminium" : return 1f;
                    break;
                case "PolyEthylene" : return 1f;
                    break;
                case "Borated PolyEthylene" : return 1f;
                    break;
                case "Liquid Hydrogen" : return 1f;
                    break;
                case "Boron Nitride Nanotubes" : return 1f;
                    break;
                default: return 0f;
            }
            
        }
        

        public void SetDefaultRadiationShieldType()
        {
            var partType = this.Part.PartType.Name;
            switch (partType)
            {
                case "Vroz Space Capsule" : this.radiationShieldType = "Aluminium";
                    break;
                case "Vroz Orbital Module" : this.radiationShieldType = "Aluminium"; 
                    break;
                case "Komodo Capsule": this.radiationShieldType = "PolyEthylene"; 
                    break;
                case "Space Capsule": this.radiationShieldType = "Aluminium"; 
                    break;
                case "CrewCompartment": this.radiationShieldType = "None"; 
                    break;
                case "Patriot Capsule": this.radiationShieldType = "Borated PolyEthylene"; 
                    break;
                default:
                    this.radiationShieldType = "None";
                    break;
                    
            }

            ApplyShieldMaterialDefaultDuration();
        }

        private double Min(double a, double b)
        {
            // NaN -> treat as "upper" so the clamp result is deterministic.
            if (double.IsNaN(a)) return b;
            return a <= b ? a : b;
        }

        //10等于1吨,这个MassDry会相加到这个part的质量里面
        public override float MassDry
        {
            get
            {
                return 500;
                if (this.RadiationShieldType=="None")
                {
                    return base.MassDry;
                }
                
                return base.MassDry*(float)(this.RadiationShieldDuration / this.RadiationShieldDurationUpperLimit * this.RadiationShieldDuration * this.RadiationShieldDuration * GetMassFactor());
            }
        }
    }
}