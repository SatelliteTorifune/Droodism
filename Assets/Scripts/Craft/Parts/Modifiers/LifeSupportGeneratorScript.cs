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
        
        
        void IDesignerStart.DesignerStart(in DesignerFrameData frame)
        {
            base.OnInitialized();
        }

        public void FlightStart(in FlightFrameData frame)
        {
            _generatorScript = GetComponent<GeneratorScript>();
            Rechck();
            IsFunctional = _generatorScript.Data.FuelType.Id.Contains("LOX/LH2");
            if (IsFunctional)
            {
                Debug.LogFormat("路边一条");
            }

        }

        public void FlightUpdate(in FlightFrameData frame)
        {
            if (frame.DeltaTimeWorld==0)
                return;
            if (IsFunctional&&this.PartScript.Data.Activated)
            {   
                FillFuelTankLogic(frame);
            }
            
        }

        void FillFuelTankLogic(FlightFrameData frame)
        {
            double fuelToAdd = _generatorScript.Data.FuelFlow * this.Data.HydroloxConvertEfficiency * frame.DeltaTimeWorld;
            if (waterSource!=null)
            {
                if (waterSource.TotalCapacity-waterSource.TotalFuel>=0.001)
                {
                    waterSource.AddFuel(fuelToAdd);
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
                    oxygenSource.AddFuel(fuelToAdd);
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
    
            // 查找匹配的燃料源
            foreach (var source in craftSources)
            {
                if (source.FuelType.Name.Contains(fuelType))
                {
                    return source;
                }
            }
            return null;
        }

        private void Rechck()
        {
            waterSource = GetCraftFuelSource("Water");
            oxygenSource = GetCraftFuelSource("Oxygen");
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
            model.Add<TextModel>(new TextModel("Fuel Flow", (Func<string>) (() => Units.GetMassFlowRateString(_generatorScript.Data.FuelFlow * this.Data.HydroloxConvertEfficiency)), tooltip: "The kilograms of fuel being burnt per second."));
        }

        
    }
}