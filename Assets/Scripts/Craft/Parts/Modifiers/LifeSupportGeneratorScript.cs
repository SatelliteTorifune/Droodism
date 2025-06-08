using Assets.Scripts.Craft.Parts.Modifiers.Propulsion;
using ModApi.Craft;
using ModApi.Craft.Parts;
using ModApi.Craft.Parts.Input;
using ModApi.Design;
using ModApi.GameLoop;
using ModApi.GameLoop.Interfaces;
using ModApi.Math;
using ModApi.Scripts.State.Validation;
using ModApi.Ui.Inspector;
using System;
using UnityEngine;

namespace Assets.Scripts.Craft.Parts.Modifiers
{
    

    public class LifeSupportGeneratorScript : PartModifierScript<LifeSupportGeneratorData>,
        IDesignerStart,
        IFlightStart,
        IFlightUpdate
    {
        
        private GeneratorScript _generatorScript;
        private bool IsFunctional;
        private IFuelSource waterSource;
        private IFuelSource oxygenSource;
        
        
        void IDesignerStart.DesignerStart(in DesignerFrameData frame)
        {
            base.OnInitialized();
            
        }

        public void FlightStart(in FlightFrameData frame)
        {
            _generatorScript = GetComponent<GeneratorScript>();
            Rechck();
            

        }

        public void FlightUpdate(in FlightFrameData frame)
        {
            if (!this.IsFunctional)
                return;
            FillFuelTankLogic(frame);
        }

        void FillFuelTankLogic(FlightFrameData frame)
        {
            if (oxygenSource != null && oxygenSource.TotalCapacity - oxygenSource.TotalFuel >= 0.001)
            {
                oxygenSource.AddFuel(_generatorScript.Data.FuelFlow * this.Data.HydroloxConvertEfficiency * frame.DeltaTimeWorld);
            }
            
            if (waterSource != null && waterSource.TotalCapacity - waterSource.TotalFuel >= 0.001)
            {
                waterSource.AddFuel(_generatorScript.Data.FuelFlow * this.Data.HydroloxConvertEfficiency * frame.DeltaTimeWorld);
            }
        }
        
        private void Rechck()
        {
            IsFunctional = isActiveAndEnabled && _generatorScript.Data.FuelType.Id.Contains("LOX/LH2");
        }

        #region 路边一条,无人在意
        public override void OnModifiersCreated()
        {
            base.OnModifiersCreated();
            this.Data.InspectorEnabled = true;
            _generatorScript = GetComponent<GeneratorScript>();
            
        }
        
        public override void OnCraftLoaded(ICraftScript craftScript, bool movedToNewCraft)
        {
            this.OnCraftStructureChanged(craftScript);
        }

        public override void OnCraftStructureChanged(ICraftScript craftScript)
        {
            Rechck();
        }
        
        private void OnCraftFuelSourceChanged(object sender, EventArgs e)
        {
            Rechck();
        } 
        
        public void OnGeneratePerformanceAnalysisModel(GroupModel groupModel)
        {
            this.CreateInspectorModel(groupModel, false);
        }
    
        #endregion
        
        
        private void CreateInspectorModel(GroupModel model, bool flight)
        {
            
        }

        
    }
}