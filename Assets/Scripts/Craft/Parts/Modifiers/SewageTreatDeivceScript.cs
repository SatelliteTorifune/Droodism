using ModApi.Craft;
using ModApi.Design;
using ModApi.GameLoop;

namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System;
    using ModApi.Craft.Parts;
    using ModApi.GameLoop.Interfaces;
    using UnityEngine;
    public class SewageTreatDeivceScript : PartModifierScript<SewageTreatDeivceData>,IFlightStart, IDesignerStart,IFlightUpdate
    {
        private IFuelSource waterSource, wastedWaterSource, _battery;
        private void ReCheck()
        {
            _battery = PartScript.BatteryFuelSource;
            try
            {
                var patchScript = PartScript.CommandPod.Part.PartScript.GetModifier<STCommandPodPatchScript>();
                if (patchScript == null)
                {
                    waterSource = wastedWaterSource = null;
                }
                if (patchScript!= null)
                {
                    waterSource = patchScript.WaterFuelSource;
                    wastedWaterSource=patchScript.WastedWaterFuelSource;
                }
            }
            catch (Exception)
            {
                waterSource = wastedWaterSource = null;
            }
        }
        public void FlightStart(in FlightFrameData frame)
        {
            ReCheck();
            this.UpdateScale();
            Mod.Instance.UpdateDroodCount();
        }
        public void FlightUpdate(in FlightFrameData frame)
        {
            if (!PartScript.Data.Activated)
                return;
            WorkingLogic(frame);
        }

        private void WorkingLogic(in FlightFrameData frame)
        {
            if (_battery == null||waterSource == null||wastedWaterSource == null)
            {
                return;
            }
            if (_battery.IsEmpty || wastedWaterSource.IsEmpty ||
                waterSource.TotalCapacity - waterSource.TotalFuel <= 0.0000001f)
            {
                return; 
            }
            wastedWaterSource.RemoveFuel( Data.WastedWaterComsumeRate * frame.DeltaTimeWorld*Data.Scale);
            waterSource.AddFuel(  Data.ConvertEffiency*0.3f*Data.WastedWaterComsumeRate * frame.DeltaTimeWorld*Data.Scale);
            _battery.RemoveFuel( Data.BatteryComsumeRate * Data.WastedWaterComsumeRate * frame.DeltaTimeWorld*Data.Scale);
        }
        #region 路边一条
        public void DesignerStart(in DesignerFrameData frame)
        {
            this.UpdateScale();
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
            
            this.UpdateScale();
           
        }
        
        protected override void OnInitialized()
        {
            base.OnInitialized();
            
            this.UpdateScale();
            
        }
        
        private void OnCraftFuelSourceChanged(object sender, EventArgs e) => this.ReCheck();
        
        #endregion

        /// <summary>Updates the scale of the generator.</summary>
        public void UpdateScale()
        {
            Transform transform = ((Component) this).transform.Find("Scalar");
            if (transform == null)
            {
                Debug.LogWarning("Unable to find Scalar transform");
                return;
            }

            foreach (AttachPointScript attachPointScript in this.PartScript.AttachPointScripts)
            {
                attachPointScript.AttachPoint.Scale = 0.8f * this.Data.Scale;
            }
            transform.localScale= Vector3.one*this.Data.Scale;
        }
    }
}