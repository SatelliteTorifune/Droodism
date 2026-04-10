namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using ModApi.Craft.Parts;
    using ModApi.GameLoop.Interfaces;
    using UnityEngine;

    public class MiningMachineScript : ResourceProcessorPartScript<MiningMachineData>
    {
        private Transform Drill;
        protected override void WorkingAnimation(bool active)
        {
            base.WorkingAnimation(active);
        }
        
    }
}