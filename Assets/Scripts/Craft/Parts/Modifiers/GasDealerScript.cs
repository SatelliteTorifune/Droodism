using ModApi.Craft;
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

    public class GasDealerScript : PartModifierScript<GasDealerData>,
        IDesignerStart,
        IFlightStart,
        IFlightUpdate

    {
        private IFuelSource highPressureGasSource;
        private IFuelSource lowPressureGasSource;
        private IFuelSource batterySource;
        public bool isOxygen = true;

        void IDesignerStart.DesignerStart(in DesignerFrameData frame)
        {
            base.OnInitialized();
        }

        public void FlightStart(in FlightFrameData frame)
        {
            RefreshFuelSources();
        }

        public void FlightUpdate(in FlightFrameData frame)
        {
            if (!PartScript.Data.Activated)
                return;
            WorkingLogic(frame);
        }

        private void WorkingLogic(in FlightFrameData frame)
        {
            if (highPressureGasSource == null||lowPressureGasSource == null)
                return;
            //理论上来说Data.GasFlowRate * lowPressureGasSource.FuelType.Density/highPressureGasSource.FuelType.Density这么写是完全没毛病的,但是出于一种我也不知道的玄学原因,游戏会发癫,凭空给我生成fuel,所以最快的解决方法就算直接*0.973,然后对外宣称这是正常损耗,这样完全不会有人怀疑对吧
            //哈哈,我他妈真是天才.
            if (Data.IsPressuring&&!lowPressureGasSource.IsEmpty&&highPressureGasSource.TotalCapacity - highPressureGasSource.TotalFuel > 1E-06&&batterySource!=null&&!batterySource.IsEmpty)
            { 
                lowPressureGasSource.RemoveFuel(Data.GasFlowRate* frame.DeltaTimeWorld);
                
                highPressureGasSource.AddFuel(0.972*Data.GasFlowRate * lowPressureGasSource.FuelType.Density/highPressureGasSource.FuelType.Density * frame.DeltaTimeWorld);
                batterySource.RemoveFuel(Data.GasFlowRate * frame.DeltaTimeWorld*Data.BatteryConsumption);
            }
            if (!Data.IsPressuring&&!highPressureGasSource.IsEmpty&&lowPressureGasSource.TotalCapacity-lowPressureGasSource.TotalFuel>1E-06)
            {
                highPressureGasSource.RemoveFuel(Data.GasFlowRate* frame.DeltaTimeWorld);
                lowPressureGasSource.AddFuel(0.972*Data.GasFlowRate * highPressureGasSource.FuelType.Density/lowPressureGasSource.FuelType.Density * frame.DeltaTimeWorld);
            }
        }
        private IFuelSource GetCraftFuelSource(string fuelType)
        {
            foreach (var source in PartScript.CraftScript.FuelSources.FuelSources)
            {
                if (source.FuelType.Id== fuelType)
                {
                    return source;
                }
            }
            return null;
        }
        
        #region 路边一条

        public void RefreshFuelSources()
        {
            batterySource = PartScript.BatteryFuelSource;
            if (isOxygen)
            {
                highPressureGasSource = GetCraftFuelSource("HPOxygen");
                try
                {
                    var patchScript = PartScript?.CommandPod.Part.PartScript.GetModifier<STCommandPodPatchScript>();
                    if (patchScript == null)
                    {
                        lowPressureGasSource = null;
                    }

                    if (patchScript != null)
                    {
                        lowPressureGasSource = patchScript.OxygenFuelSource;

                    }

                }
                catch (Exception)
                {
                    lowPressureGasSource = null;
                }
            }
            if (!isOxygen)
            {
                highPressureGasSource = GetCraftFuelSource("HPCO2");
                try
                {
                    var patchScript = PartScript?.CommandPod.Part.PartScript.GetModifier<STCommandPodPatchScript>();
                    if (patchScript == null)
                    {
                        lowPressureGasSource = null;
                    }

                    if (patchScript != null)
                    {
                        lowPressureGasSource = patchScript.CO2FuelSource;

                    }

                }
                catch (Exception)
                {
                    lowPressureGasSource = null;
                }
            }
        }
        public override void OnCraftStructureChanged(ICraftScript craftScript)
        {
            RefreshFuelSources();
            base.OnCraftStructureChanged(craftScript);
        }
        
        #endregion

        public override void OnGenerateInspectorModel(PartInspectorModel model)
        {
            base.OnGenerateInspectorModel(model);
            var changeMode=new ToggleModel("Switch to Oxygen Fuel Type", () => isOxygen, (Action<bool>) (b=>
            {
                isOxygen = b;
                RefreshFuelSources();
            }),"Determines this part is in dealing with Carbon dioxide or Oxygen");
            model.Add(changeMode);
        }
        
        
    }
}