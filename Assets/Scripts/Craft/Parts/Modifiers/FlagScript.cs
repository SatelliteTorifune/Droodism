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
        private Transform FlagBase,FlagS1;
        public void FlightStart(in FlightFrameData frame)
        {
            UpdateComponents();
        }

        private float test = 0f;
        void IFlightUpdate.FlightUpdate(in FlightFrameData frame)
        {
            Deploy(frame);
        }
        void Deploy(in FlightFrameData frame)
        {
            test += 0.01f;
            FlagS1.transform.localPosition=new Vector3(0,test+1,0);
            Mod.Log( FlagS1.transform.localPosition);
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
                FlagS1=FlagBase.Find("FlagS1");
               Mod.Log("goodset");
            }
        }
        
    }
}