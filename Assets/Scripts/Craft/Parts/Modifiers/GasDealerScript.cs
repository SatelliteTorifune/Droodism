using ModApi.Craft;
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

    public class GasDealerScript : PartModifierScript<GasDealerData>,
        IDesignerStart,
        IFlightStart,
        IFlightUpdate

    {
        private IFuelSource HPOygenSource;
        private IFuelSource LPOygenSource;
        public bool IsPressuring { get; set; }
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
            if (HPOygenSource == null&&LPOygenSource == null)
                return;
            if (IsPressuring)
            {
                
            }
        }
        private IFuelSource GetCraftFuelSource(string fuelType)
        {
            var craftSources = PartScript.CraftScript.FuelSources.FuelSources;


            foreach (var source in craftSources)
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
            HPOygenSource= GetCraftFuelSource("HPOxygen");
            try
            {
                var patchScript = PartScript?.CommandPod.Part.PartScript.GetModifier<STCommandPodPatchScript>();
                if (patchScript == null)
                {
                    LPOygenSource=null;
                }

                if (patchScript!=null)
                {
                    LPOygenSource=patchScript.OxygenFuelSource;
                   
                }
                
            }
            catch (Exception)
            {
                LPOygenSource=null;
            }
        }
        public override void OnCraftStructureChanged(ICraftScript craftScript)
        {
            RefreshFuelSources();
            base.OnCraftStructureChanged(craftScript);
        }
        
        #endregion
    }
}