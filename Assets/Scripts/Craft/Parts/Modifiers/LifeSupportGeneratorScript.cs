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
using System.Collections.Generic;
using System.Xml.Linq;
using ModApi.Craft.Propulsion;
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
        private List<FuelTankScript> oxygenTanks = new List<FuelTankScript>();
        private List<FuelTankScript> waterTanks = new List<FuelTankScript>();
        
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
            if (!this.PartScript.Data.Activated||frame.DeltaTimeWorld==0||!IsFunctional)
                return;
            FillFuelTankLogic(frame);
        }

        void FillFuelTankLogic(FlightFrameData frame)
        {
            double fuelToAdd = _generatorScript.Data.FuelFlow * this.Data.HydroloxConvertEfficiency * frame.DeltaTimeWorld;
            
            foreach (var tank in oxygenTanks)
            {
                if (tank.TotalCapacity - tank.TotalFuel >= 0.001)
                {
                    tank.AddFuel(10);
                    Debug.LogErrorFormat("尝试执行oxygenTanksAddFuel{0}", fuelToAdd);
                }
            }
            foreach (var tank in waterTanks)
            {
                if (tank.TotalCapacity - tank.TotalFuel >= 0.001)
                {
                    tank.AddFuel(fuelToAdd);
                    Debug.LogFormat("尝试执行waterTanksAddFuel{0}",fuelToAdd);
                }
            }
        }
        
        private void Rechck()
        {
            IsFunctional = this.PartScript.Data.Activated && _generatorScript.Data.FuelType.Id.Contains("LOX/LH2");
            if (!IsFunctional)
                return;
            oxygenTanks.Clear();
            waterTanks.Clear();
            
            foreach (var part in this.PartScript.CraftScript.Data.Assembly.Parts)
            {
                
                foreach (var modifier in part.Modifiers)
                {
                    if (modifier.GetScript() is FuelTankScript fuelTank && fuelTank.FuelType != null)
                    {
                        if (fuelTank.FuelType.Name == "Oxygen")
                        {
                            oxygenTanks.Add(fuelTank);
                        }
                        if (fuelTank.FuelType.Name.Contains("Drinking"))
                            waterTanks.Add(fuelTank);
                    }
                }
            }
            
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
            model.Add<TextModel>(new TextModel("Fuel Flow", (Func<string>) (() => Units.GetMassFlowRateString(_generatorScript.Data.FuelFlow * this.Data.HydroloxConvertEfficiency)), tooltip: "The kilograms of fuel being burnt per second."));
        }

        
    }
}