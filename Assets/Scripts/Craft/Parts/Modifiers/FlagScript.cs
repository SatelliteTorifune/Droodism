using ModApi;
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

    public class FlagScript : PartModifierScript<FlagData>,IFlightUpdate,IFlightStart
    {
        private Transform FlagBase;
        public void FlightStart(in FlightFrameData frame)
        {
            UpdateComponents();
        }
        void IFlightUpdate.FlightUpdate(in FlightFrameData frame)
        {
            Deploy(frame);
        }
        void Deploy(in FlightFrameData frame)
        {
            
        }
        private void UpdateComponents()
        {
            string[] strArray = "MeshBase/FlagBase".Split('/', StringSplitOptions.None);
            Transform subPart = this.transform;
            foreach (string n in strArray)
                subPart = subPart.Find(n) ?? subPart;
            if (subPart.name == strArray[strArray.Length - 1])
                FlagBase=(subPart);
            else
                FlagBase = (Utilities.FindFirstGameObjectMyselfOrChildren("MeshBase/FlagBase", this.gameObject)
                    ?.transform);

            if (FlagBase!=null)
            {
               Mod.Log("goodset");
            }
        }

        
    }
}