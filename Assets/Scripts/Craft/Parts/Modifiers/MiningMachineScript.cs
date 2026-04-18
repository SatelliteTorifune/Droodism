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

    public class MiningMachineScript : ResourceProcessorPartScript<MiningMachineData>
    {
        private Transform mainBase, groudFix1, groudFix2, nail, nail2, drillBody, drillHead, driller, drillStut,drillPiston;
        
        private Transform _offset;
        private Vector3 _offsetPositionInverse;

        private bool isDeployed, isDeploying;
        protected override void WorkingAnimation(bool active)
        {
            base.WorkingAnimation(active);
        }

        public override void FlightUpdate(in FlightFrameData frame)
        {
            bool active = isDeployed && PartScript.Data.Activated && !BatterySource.IsEmpty;
            if (Data.CurrentEnabledPercent <1)
            {
                this.isDeployed = false;
            }
            
            if (PartScript.Data.Activated)
            {
                Deploy(frame);
            }

            if (PartScript.Data.Activated&&!isDeploying&&isDeployed)
            {
                UnDeploy(frame);
            }
        }
        
        void Deploy(in FlightFrameData frame)
        {
            this.isDeploying = true;
            this.Data.CurrentEnabledPercent = Mathf.MoveTowards(this.Data.CurrentEnabledPercent, 1, frame.DeltaTime * this.Data.DeploySpeed);
            mainBase.localRotation = Quaternion.Euler(Vector3.Lerp(new Vector3(0, 0, 0),
                new Vector3(120, 0, 0), Data.CurrentEnabledPercent));
            groudFix1.localRotation = Quaternion.Euler(Vector3.Lerp(new Vector3(180, 0, 0),
                new Vector3(-30, 0,0), Data.CurrentEnabledPercent));
            if (Data.CurrentEnabledPercent >= 1)
            {
                this.Data.CurrentEnabledPercent2 = Mathf.MoveTowards(this.Data.CurrentEnabledPercent2, 1, frame.DeltaTime * this.Data.DeploySpeed);
                drillBody.localRotation = Quaternion.Euler(Vector3.Lerp(new Vector3(0, 0, 0),
                    new Vector3(0, 90, 0), Data.CurrentEnabledPercent2));
                groudFix2.localRotation = Quaternion.Euler(Vector3.Lerp(new Vector3(0, 0, 0),
                    new Vector3(0, 90, 0), Data.CurrentEnabledPercent2));
            }

            if (Data.CurrentEnabledPercent2 >= 1)
            {
                this.Data.CurrentEnabledPercent3 = Mathf.MoveTowards(this.Data.CurrentEnabledPercent3, 1, frame.DeltaTime * this.Data.DeploySpeed);
                drillStut.localRotation = Quaternion.Euler(Vector3.Lerp(new Vector3(0, 0, 0),
                    new Vector3(0, 0, 45), Data.CurrentEnabledPercent3));
               
                nail.transform.localPosition=Vector3.Lerp(new Vector3(0, 0, 0),
                    new Vector3(0, 0,0.35f), Data.CurrentEnabledPercent3);
                nail2.transform.localPosition=Vector3.Lerp(new Vector3(0, 0, 0),
                    new Vector3(0, 0,-0.35f), Data.CurrentEnabledPercent3);
                driller.transform.localPosition=Vector3.Lerp(new Vector3(2f, -0.087f, 0.166f),
                    new Vector3(0.6f, -0.087f, 0.166f), Data.CurrentEnabledPercent3);
            }
            
           
        }
        
        void UnDeploy(in FlightFrameData frame)
        {
            return;
            this.Data.CurrentEnabledPercent = Mathf.MoveTowards(this.Data.CurrentEnabledPercent, 0, frame.DeltaTime * this.Data.DeploySpeed);
            mainBase.localRotation = Quaternion.Euler(Vector3.Lerp(new Vector3(120,0,0),
                new Vector3(0, 0, 0), Data.CurrentEnabledPercent));
        }

        protected override void UpdateComponents()
        {
            string[] strArray = "MeshBase/MainBase".Split('/', StringSplitOptions.None);
            Transform subPart = this.transform;
            foreach (string n in strArray)
                subPart = subPart.Find(n) ?? subPart;
            if (subPart.name == strArray[strArray.Length - 1])
                this.SetSubPart(subPart);
            else
                this.SetSubPart(Utilities.FindFirstGameObjectMyselfOrChildren("MeshBase/MainBase", this.gameObject)?.transform);

            if (mainBase!=null)
            {
                groudFix1 = mainBase.Find("groudFix1");
                nail = groudFix1.Find("nail");
                drillBody=groudFix1.Find("drillBody");
                groudFix2=drillBody.Find("groudFix2");
                nail2=groudFix2.Find("nail2");
                drillPiston=drillBody.Find("drillPiston");
                drillHead=drillBody.Find("drillHead");
                drillStut=drillBody.Find("drillStut");
                driller=drillStut.Find("driller");
            }
        }
        private void SetSubPart(Transform subPart)
        {
            if ((UnityEngine.Object) this._offset != (UnityEngine.Object) null)
            {
                UnityEngine.Object.Destroy((UnityEngine.Object) this._offset.gameObject);
                this._offset = (Transform) null;
            }
            this.mainBase = subPart;
            if (!((UnityEngine.Object) this.mainBase != (UnityEngine.Object) null) || (double) this.Data.PositionOffset1.magnitude <= 0.0)
                return;
            this._offset = new GameObject("SubPartRotatorOffset").transform;
            this._offset.SetParent(this.mainBase.parent, false);
            this._offset.position = this.mainBase.TransformPoint(Data.PositionOffset1);
            this._offsetPositionInverse = this._offset.InverseTransformPoint(this.mainBase.position);
        }

    }
}