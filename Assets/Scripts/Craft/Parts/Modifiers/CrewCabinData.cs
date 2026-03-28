
using System.Collections.Generic;
using System.Windows.Forms;
using ModApi.Design.PartProperties;


namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System;
    using ModApi.Craft.Parts;
    using Assets.Scripts;
    using ModApi.Craft.Parts.Attributes;
    using UnityEngine;

    [Serializable]
    [DesignerPartModifier("CrewCabin")]
    [PartModifierTypeId("CrewCabin")]
    public class CrewCabinData : PartModifierData<CrewCabinScript>
    {
        
        [SerializeField] [PartModifierProperty(true, false)]
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
        private void ApplyShieldMaterialDefaults()
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
        /// 不同材料的最大耐久（单位与消耗计算一致，作为可用“预算”上限）。
        /// 数值可按平衡需要再调整。
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

        public void SetDefaultRadiationShieldType()
        {
            var partType = this.Part.PartType.Name;
            switch (partType)
            {
                case "Vroz Space Capsule" :
                    this.radiationShieldType = "Aluminium";
                    break;
                case "Vroz Orbital Module" :
                    this.radiationShieldType = "Aluminium"; 
                    break;
                case "Komodo Capsule":
                    this.radiationShieldType = "PolyEthylene"; 
                    break;
                case "Space Capsule":
                    this.radiationShieldType = "Aluminium"; 
                    break;
                case "CrewCompartment":
                    this.radiationShieldType = "None"; 
                    break;
                case "Patriot Capsule":
                    this.radiationShieldType = "Borated PolyEthylene"; 
                    break;
                default:
                    this.radiationShieldType = "None";
                    break;
                    
            }

            ApplyShieldMaterialDefaults();
        }
    }
}