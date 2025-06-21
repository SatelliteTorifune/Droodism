using ModApi.Craft;
using ModApi.Design;
using ModApi.GameLoop;
using ModApi.Ui.Inspector;

namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using ModApi.Craft.Parts;
    using ModApi.GameLoop.Interfaces;
    using UnityEngine;

    public class Sodium_PeroxideScript : PartModifierScript<Sodium_PeroxideData>,IFlightStart, IDesignerStart,IFlightUpdate
    {
        private IFuelSource waterSource, wastedWaterSource, _co2Source, _oxygenSource;
        private float oxygenGeneratedAmount = 0;
        private bool isUsedUp,isActive;
        private SubPartRotatorScript _rotatorScript;
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
        private void ReCheck()
        {
            waterSource = GetCraftFuelSource("H2O");
            wastedWaterSource = GetCraftFuelSource("Wasted Water");
            _co2Source = GetCraftFuelSource("CO2");
            _oxygenSource = GetCraftFuelSource("Oxygen");
            
        }

        public void FlightStart(in FlightFrameData frame)
        {
            ReCheck();
            _rotatorScript = GetComponent<SubPartRotatorScript>();
            Mod.Inctance.UpdateDroodCount();
            oxygenGeneratedAmount = 0;
            isUsedUp = false;
            isActive = false;
        }
        public void FlightUpdate(in FlightFrameData frame)
        {
            if (isUsedUp)
            {
                _rotatorScript.enabled=false;
                return;
            }
            WorkingLogic(frame);
            
            
        }

        private void WorkingLogic(in FlightFrameData frame)
        {
            
            if (oxygenGeneratedAmount>=Data.MaxOxygenGenerateAmount)
            {
                isUsedUp = true;
                isActive=false;
                _rotatorScript.enabled = false;
                return;
            }

            if (PartScript.Data.Activated)
            {
                if (_rotatorScript.Data.CurrentEnabledPercent>=0.9f)
                {
                    isActive = true;
                }
                
            }
            
            if (!isUsedUp&&isActive)
            {
                if (_oxygenSource != null && (_co2Source != null || wastedWaterSource != null || waterSource! != null))
                {
                    if (_co2Source != null&&!_co2Source.IsEmpty)
                    {
                        double num1 = Data.Co2ComsumeRate * frame.DeltaTimeWorld * Data.co2LevelInfluence *
                                      (_co2Source.TotalFuel / _co2Source.TotalCapacity);
                        _co2Source.RemoveFuel(num1);
                        _oxygenSource.AddFuel(num1*this.Data.oxygenGeneratorScale);
                        oxygenGeneratedAmount += (float)num1;
                    }

                    if (wastedWaterSource != null&&!wastedWaterSource.IsEmpty)
                    {
                        double num2 = Data.WastedWaterComsumeRate * frame.DeltaTimeWorld;
                        wastedWaterSource.RemoveFuel(num2);
                        _oxygenSource.AddFuel(num2*this.Data.oxygenGeneratorScale*1000);
                        oxygenGeneratedAmount += (float)num2;
                    }

                    //if (waterSource!= null&&!waterSource.IsEmpty)
                    //{
                    //    double num3 = Data.WaterComsumeRate * frame.DeltaTimeWorld;
                    //    waterSource.RemoveFuel(num3);
                    //    _oxygenSource.AddFuel(num3*this.Data.oxygenGeneratorScale*400);
                    //    oxygenGeneratedAmount += (float)num3;
                    //}
                }
            }
            


        }

        #region 路边一条
        public void DesignerStart(in DesignerFrameData frame)
        {
            ReCheck();
        }
        public override void OnCraftLoaded(ICraftScript craftScript, bool movedToNewCraft)
        {
            this.OnCraftStructureChanged(craftScript);
        }
        public override void OnCraftStructureChanged(ICraftScript craftScript)
        {
            ReCheck();
            
        }
        public override void OnSymmetry(SymmetryMode mode, IPartScript originalPart, bool created)
        {
            ReCheck();
        }
        
        
        
        private void OnCraftFuelSourceChanged(object sender, EventArgs e) => this.ReCheck();
        
        #endregion
        
        public override void OnGenerateInspectorModel(PartInspectorModel model)
        {
            base.OnGenerateInspectorModel(model);
            //GroupModel groupModel = new GroupModel("<color=green><size=115%>Sodium Peroxide</size></color>");
            //model.AddGroup(groupModel);
            var WastedWaterProgressBarModel = new ProgressBarModel("Generation Progress", () =>
                (float)(oxygenGeneratedAmount/Data.MaxOxygenGenerateAmount));
            model.Add(WastedWaterProgressBarModel);
        }
        
    }
}