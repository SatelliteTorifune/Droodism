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
        private Transform mainBase, groudFix1, groudFix2, nail, nail2, drillBody, drillHead, driller, drillStut,drillPiston,drillBodyPT;


        private ParticleSystem drillBodyPS;
        private Transform _offset;

        private bool isDeployed, isDeploying;
        

        public override void FlightUpdate(in FlightFrameData frame)
        {
            bool shouldBeActive = PartScript.Data.Activated && !BatterySource.IsEmpty;

            // 部署逻辑
            if (shouldBeActive)
            {
                Deploy(frame);
            }
            // 收起逻辑
            else
            {
                UnDeploy(frame);
            }

            // 只有完全展开且激活时才工作
            if (isDeployed && shouldBeActive && Data.CurrentEnabledPercent3 >= 0.99f)
            {
                WorkingAnimation(true, frame);
                WorkingParticle(true);
            }
            else
            {
                WorkingAnimation(false, frame);
                WorkingParticle(false);
            }

            // 更新部署状态
            isDeployed = Data.CurrentEnabledPercent3 >= 0.99f;
            isDeploying = Data.CurrentEnabledPercent1 < 1f || Data.CurrentEnabledPercent2 < 1f || Data.CurrentEnabledPercent3 < 1f;
        }
        
        void Deploy(in FlightFrameData frame)
        {
            if(Data.CurrentEnabledPercent3 >= 1)
            {
                this.isDeployed = true;
                this.isDeploying = false;
                return;
            }
            
            this.Data.CurrentEnabledPercent1 = Mathf.MoveTowards(this.Data.CurrentEnabledPercent1, 1, frame.DeltaTime * this.Data.DeploySpeed);
            mainBase.localRotation = Quaternion.Euler(Vector3.Lerp(new Vector3(0, 0, 0),
                new Vector3(120, 0, 0), Data.CurrentEnabledPercent1));
            groudFix1.localRotation = Quaternion.Euler(Vector3.Lerp(new Vector3(180, 0, 0),
                new Vector3(-30, 0,0), Data.CurrentEnabledPercent1));
            
            if (Data.CurrentEnabledPercent1 >= 1)
            {
                this.Data.CurrentEnabledPercent2 = Mathf.MoveTowards(this.Data.CurrentEnabledPercent2, 1, frame.DeltaTime * this.Data.DeploySpeed);
                drillBody.localRotation = Quaternion.Euler(Vector3.Lerp(new Vector3(0, 0, 0),
                    new Vector3(0, 90, 0), Data.CurrentEnabledPercent2));
                groudFix2.localRotation = Quaternion.Euler(Vector3.Lerp(new Vector3(0, 0, 0),
                    new Vector3(0, 90, 0), Data.CurrentEnabledPercent2));
                this.isDeploying = true;
                this.isDeployed = false;
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
                this.isDeploying = true;
                this.isDeployed = false;
            }
            
        }

        void UnDeploy(in FlightFrameData frame)
        {
            if (Data.CurrentEnabledPercent3 > 0)
            {
                this.Data.CurrentEnabledPercent3 = Mathf.MoveTowards(this.Data.CurrentEnabledPercent3, 0,
                    frame.DeltaTime * this.Data.DeploySpeed);
                drillStut.localRotation = Quaternion.Euler(Vector3.Lerp(new Vector3(0, 0, 0),
                    new Vector3(0, 0, 45), Data.CurrentEnabledPercent3));

                nail.transform.localPosition = Vector3.Lerp(new Vector3(0, 0, 0),
                    new Vector3(0, 0, 0.35f), Data.CurrentEnabledPercent3);
                nail2.transform.localPosition = Vector3.Lerp(new Vector3(0, 0, 0),
                    new Vector3(0, 0, -0.35f), Data.CurrentEnabledPercent3);
                driller.transform.localPosition = Vector3.Lerp(new Vector3(2f, -0.087f, 0.166f),
                    new Vector3(0.6f, -0.087f, 0.166f), Data.CurrentEnabledPercent3);
                this.isDeployed = false;
            }

            if (Data.CurrentEnabledPercent2 > 0&&Data.CurrentEnabledPercent3==0)
            {
                this.Data.CurrentEnabledPercent2 = Mathf.MoveTowards(this.Data.CurrentEnabledPercent2, 0,
                    frame.DeltaTime * this.Data.DeploySpeed);
                drillBody.localRotation = Quaternion.Euler(Vector3.Lerp(new Vector3(0, 0, 0),
                    new Vector3(0, 90, 0), Data.CurrentEnabledPercent2));
                groudFix2.localRotation = Quaternion.Euler(Vector3.Lerp(new Vector3(0, 0, 0),
                    new Vector3(0, 90, 0), Data.CurrentEnabledPercent2));
            }

            if (Data.CurrentEnabledPercent1 > 0&Data.CurrentEnabledPercent2==0&Data.CurrentEnabledPercent3==0)
            {
                this.Data.CurrentEnabledPercent1 = Mathf.MoveTowards(this.Data.CurrentEnabledPercent1, 0,
                    frame.DeltaTime * this.Data.DeploySpeed);
                mainBase.localRotation = Quaternion.Euler(Vector3.Lerp(new Vector3(0, 0, 0),
                    new Vector3(120, 0, 0), Data.CurrentEnabledPercent1));
                groudFix1.localRotation = Quaternion.Euler(Vector3.Lerp(new Vector3(180, 0, 0),
                    new Vector3(-30, 0, 0), Data.CurrentEnabledPercent1));
            }

            if (Data.CurrentEnabledPercent1 <= 0.001f &&
                Data.CurrentEnabledPercent2 <= 0.001f &&
                Data.CurrentEnabledPercent3 <= 0.001f)
            {
                Data.CurrentEnabledPercent1 = 0f;
                Data.CurrentEnabledPercent2 = 0f;
                Data.CurrentEnabledPercent3 = 0f;

                this.isDeployed = false;
                this.isDeploying = false;

                mainBase.localRotation = Quaternion.Euler(0, 0, 0);
                groudFix1.localRotation = Quaternion.Euler(180, 0, 0);
                drillBody.localRotation = Quaternion.Euler(0, 0, 0);
                groudFix2.localRotation = Quaternion.Euler(0, 0, 0);
                drillStut.localRotation = Quaternion.Euler(0, 0, 0);
            }
        }

        private float _currentAngle;
        private float _currentRotation;
        private float _animationTime;      

        private void WorkingAnimation(bool active, in FlightFrameData frameData)
        {
            Data.WorkingSpeed = Mathf.Lerp(Data.WorkingSpeed, active ? 1f : 0f, (float)frameData.DeltaTimeWorld * 3f);
            
            //if this is too low,get this transform back to 0
            if (Data.WorkingSpeed < 0.001f)
            {
                _currentAngle = Mathf.Lerp(_currentAngle, 0f, (float)frameData.DeltaTimeWorld * 5f);
                _currentRotation= Mathf.Lerp(_currentRotation, 0f, (float)frameData.DeltaTimeWorld * 5f);
                drillHead.localRotation = Quaternion.Euler(0, 0, _currentAngle);
                drillPiston.localRotation = Quaternion.Euler(0f, 0f, _currentAngle * -1);
                driller.Rotate(_currentRotation,0,0);
                return;
            }
            
            _animationTime += (float)frameData.DeltaTimeWorld * Data.WorkingSpeed * 2.5f;
            float targetAngle = Mathf.Sin(_animationTime) * 20f * (1f + 0.15f * Mathf.Sin(_animationTime * 2f));
            float targetRotation = 5* Mathf.Lerp(Data.WorkingSpeed, active ? 1f : 0f, (float)frameData.DeltaTimeWorld * 3f);
            _currentAngle = Mathf.Lerp(_currentAngle, targetAngle, (float)frameData.DeltaTimeWorld * 12f);
            _currentRotation = Mathf.Lerp(_currentRotation, targetRotation, (float)frameData.DeltaTimeWorld * 12f);
            drillHead.localRotation = Quaternion.Euler(0f, 0f, _currentAngle);
            drillPiston.localRotation = Quaternion.Euler(0f, 0f, _currentAngle * -1);
            driller.Rotate(_currentRotation,0,0);
        }

        private void WorkingParticle(bool active)
        {
           
            if (active)
            {
                if (PartScript.CraftScript.FlightData.AtmosphereSample.AirDensity>0)
                {
                    if (!drillBodyPS.isPlaying)
                    {
                        drillBodyPS.Play();
                    }
                  
                }
               
            }

            else
            {
                drillBodyPS.Stop();
            }
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
                drillHead=drillBody.Find("drillHead");
                drillPiston=drillHead.Find("drillPiston");
                drillStut=drillBody.Find("drillStut");
                driller=drillStut.Find("driller");
                drillBodyPT = drillBody.Find("drillBodyPT");
                //PS
                drillBodyPS = drillBodyPT.GetComponent<ParticleSystem>();
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
        }

    }
}