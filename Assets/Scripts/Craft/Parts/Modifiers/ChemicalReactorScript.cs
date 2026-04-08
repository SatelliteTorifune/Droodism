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

   
    public class ChemicalReactorScript : ResourceProcessorPartScript<ChemicalReactorData>
    {
        private IFuelSource oxygenSource,HPOxygenSource,co2Source,HPco2Source,lH2Source,hydroloxSource,monoSource;
        protected override void WorkingLogic(in FlightFrameData frame)
        {
            base.WorkingLogic(in frame);
        }
       

        protected override void UpdateFuelSources()
        {
            base.UpdateFuelSources();
            var patch = PartScript.CommandPod.Part.PartScript.GetModifier<STCommandPodPatchScript>();
            monoSource = this.PartScript.CommandPod.MonoFuelSource;
        }

        public override void OnGenerateInspectorModel(PartInspectorModel model)
        {
            base.OnGenerateInspectorModel(model);
        }
    }
}