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

    public class GravityRingScript : PartModifierScript<GravityRingData>, IFlightUpdate, IFlightStart, IDesignerStart
    {
        private Transform _mainBase, _rotateBase;
        private string partState;

        public void FlightUpdate(in FlightFrameData frame)
        {

        }

        public void FlightStart(in FlightFrameData frame)
        {


        }

        public void DesignerStart(in DesignerFrameData frame)
        {

        }

        #region Animation Methods

        public void DeployAnimated()
        {

        }

        public void UndeployAnimated()
        {
            
        }

        #endregion

        

}
}