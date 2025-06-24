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

    public class PhotoBioReactorScript : PartModifierScript<PhotoBioReactorData>, IFlightStart, IFlightUpdate,
        IDesignerStart
    {
        private IFuelSource battery,_co2Source,_waterSource,_foodSource,_solidWastedSource;
        public void FlightStart(in FlightFrameData frame)
        {
            
        }

        public void FlightUpdate(in FlightFrameData frame)
        {
            
        }

        
        #region 路边一条
        
        public void DesignerStart(in DesignerFrameData frame)
        {
            ReFreshSources();
        }
        public override void OnCraftLoaded(ICraftScript craftScript, bool movedToNewCraft)
        {
            this.OnCraftStructureChanged(craftScript);
        }
        public override void OnCraftStructureChanged(ICraftScript craftScript)
        {
            ReFreshSources();
        }
        public override void OnSymmetry(SymmetryMode mode, IPartScript originalPart, bool created)
        {
            
            this.UpdateScale();
           
        }
        
        protected override void OnInitialized()
        {
            base.OnInitialized();
            
            this.UpdateScale();
            
        }
        
        private void OnCraftFuelSourceChanged(object sender, EventArgs e) => this.ReFreshSources();
        
        #endregion
        
        private void ReFreshSources()
        {
            _waterSource = GetCraftFuelSource("H2O");
            _co2Source = GetCraftFuelSource("CO2");
            _foodSource = GetCraftFuelSource("FO2");
            _solidWastedSource = GetCraftFuelSource("Solid Wasted");
            battery = PartScript.BatteryFuelSource;
        }
        
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

        private void UpdateScale()
        {
            
        }
    }
    
}