using Assets.Scripts.Design;
using ModApi.Design;

namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System.Collections.Generic;
    using ModApi.Craft.Parts;
    using ModApi.GameLoop.Interfaces;
    using UnityEngine;

    public class ResourcePackScript : PartModifierScript<ResourcePackData>
    {
        private Transform _scalar,_attachPointPositions;
        public void UpdateScale(bool repositionAttachedParts = false)
        {
            _scalar.localScale= Vector3.one*this.Data.ResScale;
            //UpdateConnectionPointsPosition(repositionAttachedParts);
            UpdateFuel();
        }
        public override void OnSymmetry(SymmetryMode mode, IPartScript originalPart, bool created)
        {
            this.UpdateScale(true);
        }

      
        private void UpdateFuel()
        {
            List<FuelTankScript> modifiers = this.PartScript.GetModifiers<FuelTankScript>();
           
            foreach (var partModifierScript in modifiers)
            {
                switch (partModifierScript.FuelType.Id)
                {
                    case "Oxygen":
                        partModifierScript.Data.Capacity = partModifierScript.Data.Fuel = 610*Data.ResScale;
                        break;
                    case "H2O":
                        partModifierScript.Data.Capacity = partModifierScript.Data.Fuel = 3*Data.ResScale;
                        break;
                    case "Food":
                        partModifierScript.Data.Capacity = partModifierScript.Data.Fuel = 0.5f*Data.ResScale;
                        break;
                    case "LPCO2":
                        partModifierScript.Data.Capacity = 300*Data.ResScale;
                        partModifierScript.Data.Fuel = 0;
                        break;
                    case "Solid Waste":
                        partModifierScript.Data.Capacity = 0.8f*Data.ResScale;
                        partModifierScript.Data.Fuel = 0;
                        break;
                    case "Wasted Water":
                        partModifierScript.Data.Capacity = 1.25f*Data.ResScale;
                        partModifierScript.Data.Fuel = 0;
                        break;
                        
                }   
            }
        }

        private void UpdateConnectionPointsPosition(bool repositionAttachedParts = false)
        {
            if (_attachPointPositions !=  null)
            {
                Dictionary<int, bool> movedParts = new Dictionary<int, bool>();
                foreach (Transform attachPointPosition in this._attachPointPositions)
                {
                    foreach (AttachPoint attachPoint in this.Data.Part.AttachPoints)
                    {
                        if (attachPoint.Name == attachPointPosition.name)
                        {
                            attachPoint.Scale =this.Data.AttachmentSize;
                            Vector3 position1 = attachPoint.Position;
                            attachPoint.Position = attachPointPosition.localPosition * this.Data.Scale + this._scalar.localPosition * (1f - this.Data.Scale);
                            if ( attachPoint.AttachPointScript !=  null)
                            {
                                if (repositionAttachedParts)
                                {
                                    Vector3 position2 = attachPoint.Position;
                                    Vector3 delta = attachPoint.AttachPointScript.transform.parent.TransformVector(position2 - position1);
                                    foreach (PartConnection partConnection in attachPoint.PartConnections)
                                        DesignerUtilities.RepositionParts(this.Data.Part, partConnection, delta, movedParts);
                                }
                                attachPoint.AttachPointScript.transform.localPosition = attachPoint.Position;
                                break;
                            }
                            break;
                        }
                    }
                }
            }
        }
        protected override void OnInitialized()
        {
            base.OnInitialized();
            _scalar = ((Component) this).transform.Find("Scalar");
           
            if ( this._scalar !=  null)
            {
                this._attachPointPositions = IPartSubPartSetUp.FindSubPart(this._scalar, "AttachPointPositions");
            }
            this.UpdateScale();
        }
    }
}