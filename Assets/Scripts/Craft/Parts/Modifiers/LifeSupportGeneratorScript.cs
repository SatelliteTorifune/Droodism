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
using Assets.Scripts.Menu.ListView;
using HarmonyLib;
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

        private IFuelSource LH2FuelTank;
        private FuelTankScript _fuelTank;
        
        
        void IDesignerStart.DesignerStart(in DesignerFrameData frame)
        {
            base.OnInitialized();
        }
        
        public void FlightStart(in FlightFrameData frame)
        {
            _generatorScript = GetComponent<GeneratorScript>();
            Rechck();
            IsFunctional = _generatorScript.Data.FuelType.Id.Contains("LOX/LH2");
            if (!IsFunctional)
            {
                Debug.LogFormat("路边一条");
            }

        }

        public void FlightUpdate(in FlightFrameData frame)
        {
            if (frame.DeltaTimeWorld==0)
                return;
            if (IsFunctional&&this.PartScript.Data.Activated&&_generatorScript.Data.FuelFlow>0)
            {   
                FillFuelTankLogic(frame);
            }
            
        }

        void FillFuelTankLogic(FlightFrameData frame)
        {
            if (LH2FuelTank==null)
                return;
            if (LH2FuelTank.IsEmpty)
            {
                return;
            }
            
            double WaterlToAdd = _generatorScript.Data.FuelFlow * this.Data.WaterConvertEfficiency * frame.DeltaTimeWorld;
            double OxygenlToAdd = _generatorScript.Data.FuelFlow * this.Data.OxygenConvertEfficiency * frame.DeltaTimeWorld;
            if (waterSource!=null)
            {
                if (waterSource.TotalCapacity-waterSource.TotalFuel>=0.001)
                {
                    waterSource.AddFuel(WaterlToAdd);
                }
            }
            else
            {
                Debug.LogWarningFormat("Water Source not found");
            }
            if (oxygenSource!=null)
            {
                if (oxygenSource.TotalCapacity-oxygenSource.TotalFuel>=0.001)
                {
                    oxygenSource.AddFuel(OxygenlToAdd);
                }
            }
            else
            {
                Debug.LogWarningFormat("oxygen Source not found");
            }
            
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

        private void Rechck()
        {
            if (!Game.InFlightScene)
            {
                return;
            }
            waterSource = GetCraftFuelSource("H2O");
            oxygenSource = GetCraftFuelSource("Oxygen");
            LH2FuelTank = GetCraftFuelSource("LOX/LH2");
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
            Debug.LogFormat("OnCraftStructureChanged调用Rechck");
        }
        
        private void OnCraftFuelSourceChanged(object sender, EventArgs e)
        {
            Rechck();
            Debug.LogFormat("OnCraftFuelSourceChanged调用Rechck");
        } 
        
        public void OnGeneratePerformanceAnalysisModel(GroupModel groupModel)
        {
            this.CreateInspectorModel(groupModel, false);
        }
    
        #endregion
        
        
        private void CreateInspectorModel(GroupModel model, bool flight)
        {
            model.Add<TextModel>(new TextModel("Fuel Flow", (Func<string>) (() => Units.GetMassFlowRateString(_generatorScript.Data.FuelFlow * this.Data.WaterConvertEfficiency)), tooltip: "The kilograms of fuel being burnt per second."));
        }
         
    }
}