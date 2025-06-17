using ModApi.Craft;
using ModApi.Design;
using ModApi.GameLoop;

namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using ModApi.Craft.Parts;
    using ModApi.GameLoop.Interfaces;
    using UnityEngine;

    public class SewageTreatDeivceScript : PartModifierScript<SewageTreatDeivceData>,IFlightStart, IDesignerStart
    {
        private IFuelSource waterSource, wastedWaterSource, _battery;
        private IFuelSource GetCraftFuelSource(string fuelType)
        {
            var craftSources = PartScript.CraftScript.FuelSources.FuelSources;
            
            foreach (var source in craftSources)
            {
                if (source.FuelType.Id.Contains(fuelType))
                {
                    return source;
                }
            }
            return null;
        }
        public override void OnCraftLoaded(ICraftScript craftScript, bool movedToNewCraft)
        {
            this.OnCraftStructureChanged(craftScript);
        }
        public override void OnCraftStructureChanged(ICraftScript craftScript)
        {
            Rechck();
            _battery = PartScript.BatteryFuelSource;
            waterSource = GetCraftFuelSource("H2O");
            wastedWaterSource = GetCraftFuelSource("Wasted Water");
        }
        public override void OnSymmetry(SymmetryMode mode, IPartScript originalPart, bool created)
        {
            this.UpdateBase();
            this.UpdateScale();
            this.UpdateStretch();
        }
        
        protected override void OnInitialized()
        {
            base.OnInitialized();
            this.UpdateBase();
            this.UpdateScale();
            this.UpdateStretch();
        }
        private void OnCraftFuelSourceChanged(object sender, EventArgs e) => this.Rechck();
        public void UpdateBase()
        {
           
        }

        /// <summary>Updates the scale of the generator.</summary>
        public void UpdateScale()
        {
            
        }

        /// <summary>Updates the stretch of the RTG.</summary>
        public void UpdateStretch()
        {
            
        }
        
        
        private void Rechck()
        {
            waterSource = GetCraftFuelSource("H2O");
            wastedWaterSource = GetCraftFuelSource("Wasted Water");
            _battery = PartScript.BatteryFuelSource;
        }

        public void FlightStart(in FlightFrameData frame)
        {
            this.UpdateBase();
            this.UpdateScale();
            this.UpdateStretch();
        }

        public void DesignerStart(in DesignerFrameData frame)
        {
            this.UpdateScale();
            this.UpdateStretch();
        }
    }
}