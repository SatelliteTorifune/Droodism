using RootMotion.FinalIK;
using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using Assets.Scripts.Droodism.BackGround;
using ModApi.Craft;
using ModApi.GameLoop;

namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System;
    using System.Collections.Generic;
    using ModApi.Craft.Parts;
    using ModApi.GameLoop.Interfaces;
    using UnityEngine;

    public class HibernatingChamberScript : PartModifierScript<HibernatingChamberData>,IFlightUpdate,IFlightStart
    {
        private Transform LeftHandTransform,
            RightHandTransform,
            LeftElbowTransform,
            RightElbowTransform,
            BaseLKTransform;
         private FullBodyBipedIK _pilotIK;
         private EvaScript _pilot = (EvaScript) null;
        private CrewCompartmentScript _crewCompartment;
        private AttachPoint _seatAttachPoint;
        private IFuelSource _batterySource;

        // 后台补算用：同一次结算里，同一个休眠舱只计算并扣电一次（多个乘员共享同一块电池）
        private static readonly Dictionary<HibernatingChamberScript, double> backgroundHibernationCache =
            new Dictionary<HibernatingChamberScript, double>();
        private static double backgroundHibernationCacheTime = double.NaN;

        /// <summary>舱内乘员数量。</summary>
        public int CrewCount => _crewCompartment?.Crew?.Count ?? 0;

        /// <summary>休眠每秒耗电量。</summary>
        public double HibernationPowerPerSecond => Data.HibernationPowerConsumption;

        /// <summary>休眠可用电量（当前电池剩余）。</summary>
        public double HibernationBatteryFuel => _batterySource == null ? 0.0 : _batterySource.TotalFuel;

        /// <summary>
        /// 计算"这段时间最多能休眠多久"并扣掉对应电量的静态入口。
        /// 同一帧内同一休眠舱只计算一次，其余乘员复用结果，避免多乘员重复扣电。
        /// </summary>
        public static double GetBackgroundHibernationSeconds(HibernatingChamberScript chamber, double elapsedSeconds)
        {
            if (chamber == null || elapsedSeconds <= 0.0)
            {
                return 0.0;
            }

            double now = BackgroundSettlement.CurrentGameFlightTime;
            if (double.IsNaN(backgroundHibernationCacheTime) || Math.Abs(now - backgroundHibernationCacheTime) > 1e-6)
            {
                backgroundHibernationCache.Clear();
                backgroundHibernationCacheTime = now;
            }

            double cachedSeconds;
            if (backgroundHibernationCache.TryGetValue(chamber, out cachedSeconds))
            {
                return cachedSeconds;
            }

            double powerPerSecond = chamber.HibernationPowerPerSecond;
            double availableSeconds = elapsedSeconds;
            if (powerPerSecond > 0.0)
            {
                availableSeconds = Math.Min(elapsedSeconds,
                    Math.Max(0.0, chamber.HibernationBatteryFuel / powerPerSecond));
            }

            if (availableSeconds > 0.0 && chamber._batterySource != null)
            {
                chamber._batterySource.RemoveFuel(powerPerSecond * availableSeconds);
            }

            backgroundHibernationCache[chamber] = availableSeconds;
            return availableSeconds;
        }

        private void UpdateComponents()
        {
            BaseLKTransform = IPartSubPartSetUp.FindSubPart(this, "BodyBase");
            if (BaseLKTransform == null)
            {
                Mod.LogError("HibernatingChamberScript: Could not find BodyBase transform");
            }

            RightElbowTransform = BaseLKTransform.Find("RightElbow");
            RightHandTransform = RightElbowTransform.Find("RightHand");
            LeftElbowTransform = BaseLKTransform.Find("LeftElbow");
            LeftHandTransform = LeftElbowTransform.Find("LeftHand");
        }

        public override void OnModifiersCreated()
        {
            _crewCompartment = PartScript.GetModifier<CrewCompartmentScript>();
            this._crewCompartment.SetCrewOrientation(this._seatAttachPoint.Position, this._seatAttachPoint.Rotation);
            this._crewCompartment.CrewEnter += new CrewCompartmentScript.CrewEnterExitHandler(this.OnPilotEnter);
            this._crewCompartment.CrewExit += new CrewCompartmentScript.CrewEnterExitHandler(this.OnPilotExit);
        }

        protected override void OnInitialized()
        {
            this._seatAttachPoint = this.PartScript.Data.GetAttachPoint("AttachPointSeat");
            UpdateComponents();
        }

        public void FlightStart(in FlightFrameData frameData)
        {
            _batterySource = this.PartScript.BatteryFuelSource;
            _crewCompartment = PartScript.GetModifier<CrewCompartmentScript>();
        }

        public void FlightUpdate(in FlightFrameData frameData)
        {
            if (_crewCompartment?.Crew.Count==0||_batterySource==null)
            {
                return;
            }
            if (_batterySource.IsEmpty&&_crewCompartment?.Crew.Count!=0)
            {
                foreach (var crew in _crewCompartment.Crew)
                {
                    crew.PartScript.GetModifier<SupportLifeScript>().SetHibernating(false,this.PartScript.Data.PartType);
                }
            }

            if (!_batterySource.IsEmpty)
            {
                _batterySource.RemoveFuel(frameData.DeltaTimeWorld * Data.HibernationPowerConsumption); 
                foreach (var crew in _crewCompartment.Crew)
                {
                    crew.PartScript.GetModifier<SupportLifeScript>().SetHibernating(true,this.PartScript.Data.PartType);
                }
            }
        }
        

        public override void OnCraftStructureChanged(ICraftScript craftScript)
        {
            _batterySource = this.PartScript.BatteryFuelSource;
            _crewCompartment = PartScript.GetModifier<CrewCompartmentScript>();
        } 
        private void OnPilotEnter(EvaScript crew) => this.SetPilot(crew);
        
        private void OnPilotExit(EvaScript crew)
        {
            if (!((UnityEngine.Object) crew == (UnityEngine.Object) this._pilot))
                return;
            this.SetPilot((EvaScript) null);
        }
        private void SetPilot(EvaScript pilot)
        {
            this._pilot = pilot;
            if ((UnityEngine.Object) pilot == (UnityEngine.Object) null)
            {
                this.SetIKEnabled(false);
                this._pilotIK = (FullBodyBipedIK) null;
            }
            else
            {
                this._pilotIK = this._pilot.GetComponentInChildren<FullBodyBipedIK>();
                this.SetIKEnabled(true);
                
            }
        }
    
        private void SetIKEnabled(bool enabled) 
        {
            if (!((UnityEngine.Object) this._pilotIK != (UnityEngine.Object) null))
                return;
            if (enabled)
            {
          
                this._pilotIK.solver.bodyEffector.positionWeight = 1f;
                this._pilotIK.solver.bodyEffector.rotationWeight = 1f;
                this._pilotIK.solver.bodyEffector.target = BaseLKTransform;
                this._pilotIK.solver.bodyEffector.positionWeight = 1f;
                this._pilotIK.solver.bodyEffector.rotationWeight = 1f;
                this._pilotIK.solver.rightHandEffector.target = this.RightHandTransform;
                this._pilotIK.solver.rightHandEffector.positionWeight = 1f;
                this._pilotIK.solver.rightHandEffector.rotationWeight = 1f;
                this._pilotIK.solver.leftHandEffector.target = this.LeftHandTransform;
                this._pilotIK.solver.leftHandEffector.positionWeight = 1f;
                this._pilotIK.solver.leftHandEffector.rotationWeight = 1f;
                this._pilotIK.solver.leftArmChain.bendConstraint.bendGoal = this.LeftElbowTransform;
                this._pilotIK.solver.leftArmChain.bendConstraint.weight = 1f;
                this._pilotIK.solver.rightArmChain.bendConstraint.bendGoal = this.RightElbowTransform;
                this._pilotIK.solver.rightArmChain.bendConstraint.weight = 1f;
                this._pilotIK.solver.rightFootEffector.target = (Transform) null;
                this._pilotIK.solver.rightFootEffector.positionWeight = 0.0f;
                this._pilotIK.solver.rightFootEffector.rotationWeight = 0.0f;
                this._pilotIK.solver.leftFootEffector.target = (Transform) null;
                this._pilotIK.solver.leftFootEffector.positionWeight = 0.0f;
                this._pilotIK.solver.leftFootEffector.rotationWeight = 0.0f;
        
            }
            else
            {
              this._pilotIK.solver.rightHandEffector.target = (Transform) null;
              this._pilotIK.solver.rightHandEffector.positionWeight = 0.0f;
              this._pilotIK.solver.rightHandEffector.rotationWeight = 0.0f;
              this._pilotIK.solver.leftHandEffector.target = (Transform) null;
              this._pilotIK.solver.leftHandEffector.positionWeight = 0.0f;
              this._pilotIK.solver.leftHandEffector.rotationWeight = 0.0f;
              this._pilotIK.solver.leftArmChain.bendConstraint.bendGoal = (Transform) null;
              this._pilotIK.solver.leftArmChain.bendConstraint.weight = 0.0f;
              this._pilotIK.solver.rightArmChain.bendConstraint.bendGoal = (Transform) null;
              this._pilotIK.solver.rightArmChain.bendConstraint.weight = 0.0f;
              this._pilotIK.solver.rightFootEffector.target = (Transform) null;
              this._pilotIK.solver.rightFootEffector.positionWeight = 0.0f;
              this._pilotIK.solver.rightFootEffector.rotationWeight = 0.0f;
              this._pilotIK.solver.leftFootEffector.target = (Transform) null;
              this._pilotIK.solver.leftFootEffector.positionWeight = 0.0f;
              this._pilotIK.solver.leftFootEffector.rotationWeight = 0.0f;
              this._pilotIK.solver.bodyEffector.target = (Transform) null;
              this._pilotIK.solver.bodyEffector.positionWeight = 0.0f;
              this._pilotIK.solver.bodyEffector.rotationWeight = 0.0f;
              
            }
        }
        public override void OnPartDestroyed()
        {
            base.OnPartDestroyed();
            this.SetPilot((EvaScript) null);
        }
                
    }
}