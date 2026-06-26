using ModApi.Craft;
using ModApi.Design;
using ModApi.GameLoop;
using ModApi.Math;
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

    public class Sodium_PeroxideScript : ResourceProcessorPartScript<Sodium_PeroxideData>
    {
        private IFuelSource  wastedWaterSource, _co2Source, _oxygenSource;
        private float oxygenGeneratedAmount = 0;
        private bool isUsedUp,isActive;
        private SubPartRotatorScript _rotatorScript;

        protected override void UpdateFuelSources()
        {
            base.UpdateFuelSources();
            try
            {
                var patchScript = PartScript?.CommandPod.Part.PartScript.GetModifier<STCommandPodPatchScript>();
                if (patchScript == null)
                {
                    wastedWaterSource= _oxygenSource= _co2Source=null;
                }

                if (patchScript != null)
                {
                    _oxygenSource=patchScript.OxygenFuelSource;
                    _co2Source=patchScript.CO2FuelSource;
                    wastedWaterSource=patchScript.WastedWaterFuelSource;
                }
            }
            catch (Exception)
            {
                wastedWaterSource= _oxygenSource= _co2Source=null;
            }
        }

        public override void FlightStart(in FlightFrameData frame)
        {
            base.FlightStart(frame);
            _rotatorScript = GetComponent<SubPartRotatorScript>();
            oxygenGeneratedAmount = 0;
            isUsedUp = false;
            isActive = false;
        }
        public override void FlightUpdate(in FlightFrameData frame)
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
            if (frame.DeltaTimeWorld==0)
            {
                return;
            }
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
                if (_oxygenSource != null && (_co2Source != null || wastedWaterSource != null))
                {
                    if (_co2Source != null&&!_co2Source.IsEmpty)
                    {
                        double num1 = Data.Co2ComsumeRate * frame.DeltaTimeWorld * Data.co2LevelInfluence *Math.Max(0.02, (_co2Source.TotalFuel / _co2Source.TotalCapacity));
                        _co2Source.RemoveFuel(num1);
                        _oxygenSource.AddFuel(num1*this.Data.oxygenGeneratorScale);
                        oxygenGeneratedAmount += (float)num1;
                    }

                    if (wastedWaterSource != null&&!wastedWaterSource.IsEmpty)
                    {
                        double num2 = Data.WastedWaterComsumeRate * frame.DeltaTimeWorld;
                        wastedWaterSource.RemoveFuel(num2);
                        _oxygenSource.AddFuel(num2*this.Data.oxygenGeneratorScale*2);
                        oxygenGeneratedAmount += (float)num2;
                    }

                }
            }
            


        }
        
        
        public override void OnGenerateInspectorModel(PartInspectorModel model)
        {
            base.OnGenerateInspectorModel(model);
            
            var WastedWaterProgressBarModel = new ProgressBarModel(Locale.GetString("Droodism.Sodium_PeroxideScript.GenerationProgress"), () =>
                (float)(oxygenGeneratedAmount/Data.MaxOxygenGenerateAmount));
            var percentageModel = new TextModel(Locale.GetString("Droodism.Sodium_PeroxideScript.GenerationPercentage"), () => Units.GetPercentageString(oxygenGeneratedAmount/Data.MaxOxygenGenerateAmount));
            var statesIndicatorModel = new TextModel(Locale.GetString("Droodism.Sodium_PeroxideScript.CurrentState"), () => isUsedUp ? Locale.GetString("Droodism.Sodium_PeroxideScript.ContainerUsedUp") : isActive&&!isUsedUp ? Locale.GetString("Droodism.Sodium_PeroxideScript.Working") : Locale.GetString("Droodism.Sodium_PeroxideScript.Idle"));
            model.Add(statesIndicatorModel);
            model.Add(WastedWaterProgressBarModel);
            model.Add(percentageModel);
            
        }
        
    }
}