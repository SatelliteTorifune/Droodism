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


    public class LifeSupportGeneratorScript : ResourceProcessorPartScript<LifeSupportGeneratorData>
        
    {

        private GeneratorScript _generatorScript;
        private bool IsHydroloxFunctional;
        private int FossilFuelTypeIndex;
        private IFuelSource waterSource, hydroLoxSource, oxygenSource, co2Source, fossilSource;
       



        public override void FlightStart(in FlightFrameData frame)
        {
            _generatorScript = GetComponent<GeneratorScript>();
            var fuelTypeId = _generatorScript.Data.FuelType.Id;
            IsHydroloxFunctional = fuelTypeId == "LOX/LH2";
            FossilFuelTypeIndex =
                fuelTypeId == "LOX/RP1" ? 1 : fuelTypeId == "LOX/CH4" ? 2 : fuelTypeId == "Jet" ? 3 : 0;
            UpdateFuelSources();
        }

        public override void FlightUpdate(in FlightFrameData frame)
        {
            if (frame.DeltaTimeWorld == 0)
                return;
            if (this.PartScript.Data.Activated && _generatorScript.Data.FuelFlow > 0)
            {
                if (IsHydroloxFunctional)
                {
                    HydroloxFillFuelTankLogic(frame);
                }

                if (FossilFuelTypeIndex != 0)
                {
                    FossilFuelFillFuelTankLogic(frame);
                }
            }

        }

        void HydroloxFillFuelTankLogic(FlightFrameData frame)
        {
            if (hydroLoxSource == null)
                return;
            if (hydroLoxSource.IsEmpty)
            {
                return;
            }

            double WaterlToAdd = _generatorScript.Data.FuelFlow * this.Data.WaterConvertEfficiency *
                                 frame.DeltaTimeWorld;
            double OxygenlToAdd = _generatorScript.Data.FuelFlow * this.Data.OxygenConvertEfficiency *
                                  frame.DeltaTimeWorld;
            if (waterSource != null)
            {
                if (waterSource.TotalCapacity - waterSource.TotalFuel >= 0.001)
                {
                    waterSource.AddFuel(WaterlToAdd);
                }
            }
            else
            {
                Debug.LogWarningFormat("Water Source not found");
            }

            if (oxygenSource != null)
            {
                if (oxygenSource.TotalCapacity - oxygenSource.TotalFuel >= 0.001)
                {
                    oxygenSource.AddFuel(OxygenlToAdd);
                }
            }
            else
            {
                Debug.LogWarningFormat("oxygen Source not found");
            }

        }

        void FossilFuelFillFuelTankLogic(FlightFrameData frame)
        {
            if (fossilSource == null)
                return;
            if (fossilSource.IsEmpty)
            {
                return;
            }

            if (co2Source != null&&!Data.IsPollution)
            {
                if (co2Source.TotalCapacity - co2Source.TotalFuel >= 0.00001)
                {
                    co2Source.AddFuel(Data.FossilFuelConvertEfficiency * _generatorScript.Data.FuelFlow *
                                      frame.DeltaTimeWorld);
                }
            }

            if (FossilFuelTypeIndex == 2)
            {
                if (waterSource != null)
                {
                    if (waterSource.TotalCapacity - waterSource.TotalFuel >= 0.001)
                    {
                        waterSource.AddFuel(_generatorScript.Data.FuelFlow * this.Data.WaterConvertEfficiency *
                                            frame.DeltaTimeWorld * Data.FossilFuelConvertEfficiency);
                    }
                }
            }
        }

        public override void OnGenerateInspectorModel(PartInspectorModel model)
        {
            base.OnGenerateInspectorModel(model);
            if (FossilFuelTypeIndex != 0)
            {
                var changePollution = new ToggleModel("Release CO2 Externally", () => Data.IsPollution, (Action<bool>)(b => { Data.IsPollution = b; }),
                    "Determines whether release CO2 externally or internally to the craft. ");
                model.Add(changePollution);
            }
        }



        #region 路边一条,无人在意

        public override void OnModifiersCreated()
        {
            base.OnModifiersCreated();
            this.Data.InspectorEnabled = true;
            _generatorScript = GetComponent<GeneratorScript>();

        }
        
        protected override void UpdateFuelSources()
        {
            if (!Game.InFlightScene)
            {
                return;
            }
            base.UpdateFuelSources();
            var patchScript = PartScript?.CommandPod?.Part.PartScript.GetModifier<STCommandPodPatchScript>();

            waterSource = patchScript?.WaterFuelSource;
            oxygenSource = patchScript?.OxygenFuelSource;
            hydroLoxSource = GetCraftFuelSource("LOX/LH2");
            co2Source = patchScript?.CO2FuelSource;
            if (PartScript != null)
            {

                fossilSource = FossilFuelTypeIndex == 0 ? null :
                    FossilFuelTypeIndex == 1 ? GetCraftFuelSource("LOX/RP1") :
                    FossilFuelTypeIndex == 2 ? GetCraftFuelSource("LOX/CH4") :
                    FossilFuelTypeIndex == 2 ? this.PartScript?.CommandPod?.JetFuelSource : null;
            }
        }
        
        #endregion


    }
}