namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Xml.Linq;
    using ModApi.Craft.Parts;
    using ModApi.Craft.Parts.Attributes;
    using UnityEngine;

    [Serializable]
    [DesignerPartModifier("ToolBox")]
    [PartModifierTypeId("ToolBox")]
    public class ToolBoxData : PartModifierData<ToolBoxScript>
    {
        [SerializeField] [PartModifierProperty]
        private float toolPoint=500f;
   
       
        public float ToolPoint
        {
            get => toolPoint;
            internal set => toolPoint = value;
        }

    }
}