using System.Linq.Expressions;
using Assets.Scripts.Design;
using ModApi;

namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Linq;
    using ModApi.Craft.Parts;
    using ModApi.Design.PartProperties;
    using ModApi.Craft.Parts.Attributes;
    using UnityEngine;

    [Serializable]
    [DesignerPartModifier("Sewage Treat Device")]
    [PartModifierTypeId("SewageTreatDevice")]
    public class SewageTreatDeivceData : PartModifierData<SewageTreatDeivceScript>
    {
        [SerializeField]
        [PartModifierProperty(true, false)]
        private int _fuelSourceAttachPoint;
        [SerializeField]
        [PartModifierProperty(true, false)]
        private float _baseMass = 100f;
        [SerializeField] [PartModifierProperty(true, false)]
        private float _wastedWaterComsumeRate=1f;
        [SerializeField] 
        [DesignerPropertySlider(0.1f, 1f, 21, Label = "Efficiency", Tooltip = "Defines the efficiency of the device.")]
        private float _convertEffiency=1f;
        [SerializeField] [PartModifierProperty(true, false)]
        private float _batteryComsumeRate=1f;
        [SerializeField]
        [DesignerPropertySlider(0.5f, 4f, 76, Label = "Size", Tooltip = "Defines the scale of the device.")]
        private float _scale = 1f;
        
        public float WastedWaterComsumeRate { get => this._wastedWaterComsumeRate*0.0002f;   }
        public float ConvertEffiency { get { return this._convertEffiency; }  }
        public float BatteryComsumeRate { get { return this._batteryComsumeRate*105f; } }
        
        public override float Scale
        {
            get => this._scale;
            set
            {
                this._scale = value;
                this.Script.UpdateScale();
            }
        }
        
        public override long Price
        {
            get => (long)(this.ConvertEffiency * _baseMass * _baseMass*1e4f);
        }
        
        public override float MassDry
        {
            get=>(float) ((double) this._baseMass * (double) this.Scale * (double) this.Scale * (double) this.Scale * 0.0099999997764825821);
        }
        public int FuelSourceAttachPoint => this._fuelSourceAttachPoint;
        protected override void OnDesignerInitialization(IDesignerPartPropertiesModifierInterface d)
        {
            d.OnValueLabelRequested<float>((Expression<Func<float>>) (() => this._scale), (Func<float, string>) (x => Utilities.FormatPercentage(x)));
            d.OnPropertyChanged<float>((Expression<Func<float>>) (() => this._scale), (Action<float, float>) ((newVal, oldVal) =>
            {
                d.Manager.RefreshUI();
                this.Script.UpdateScale();
                Symmetry.SynchronizePartModifiers(this.Script.PartScript);
                this.Script.PartScript.CraftScript.SetStructureChanged();
            }));
        }
    }
}