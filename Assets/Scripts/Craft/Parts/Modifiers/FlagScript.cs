using ModApi.Flight;
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

    public class FlagScript : PartModifierScript<FlagData>, IFlightStart, IFlightFixedUpdate
    {
        private Transform flagBase;

        public void FlightStart(in FlightFrameData frame)
        {

        }

        public void FlightFixedUpdate(in FlightFrameData frame)
        {

        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            flagBase = ((Component)this).transform.Find("FlagBase");

            if (this.flagBase != null)
            {

            }
        }
    }
}