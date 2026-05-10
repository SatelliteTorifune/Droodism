using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using ModApi;
using ModApi.Audio;
using ModApi.Craft;
using ModApi.GameLoop;
using ModApi.Ui;
using RootMotion.FinalIK;

namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using ModApi.Craft.Parts;
    using ModApi.GameLoop.Interfaces;
    using UnityEngine;

    public class SacrificeScript : PartModifierScript<SacrificeData>,IFlightStart,IFlightUpdate,IDesignerStart
    {

        private CrewCompartmentScript _compartment;
        private ParticleSystem chunkParticleSystem,bloodParticleSystemA,bloodParticleSystemB,bloodParticleSystemC,bloodParticleSystemD,bloodParticleSystemE,bloodParticleSystemF,bloodParticleSystemG,bloodParticleSystemH,bloodParticleSystemI;
        private ISingleSound _sound;
        
        private Transform _particalSystemTransform,_bloodEffectTransform;
        private Transform LKBaseTransform,leftHandTransform,leftElbowTransform,rightHandTransform,rightElbowTransform,bodyTransform;
        private FullBodyBipedIK _pilotIK;
        private EvaScript _pilot = (EvaScript) null;
        private AttachPoint _seatAttachPoint;

        private IFuelSource _oxygenSource, _foodSource, _waterSource, _solidWasteSource, _wastedWaterSource, _co2Source;
        
        public override void OnModifiersCreated()
        {
             _compartment = this.PartScript.GetModifier<CrewCompartmentScript>();
             this._seatAttachPoint = this.PartScript.Data.GetAttachPoint("AttachPointSeat");
              this._compartment.SetCrewOrientation(this._seatAttachPoint.Position, this._seatAttachPoint.Rotation);
            this._compartment.CrewEnter += new CrewCompartmentScript.CrewEnterExitHandler(this.OnPilotEnter);
            this._compartment.CrewExit += new CrewCompartmentScript.CrewEnterExitHandler(this.OnPilotExit);
        }
        public void FlightStart(in FlightFrameData frameData)
        {
            RefreshFuelSource();
            UpdateComponents();
            //_sound.MaxVolume = 0.5f;
            
        }
        public override void OnCraftStructureChanged(ICraftScript craftScript)
        {
            RefreshFuelSource();
            base.OnCraftStructureChanged(craftScript);
        }

        private void RefreshFuelSource()
        {
            var patchScript = PartScript?.CommandPod.Part.PartScript.GetModifier<STCommandPodPatchScript>();
            if (patchScript == null)
            {
                _co2Source=_oxygenSource=_foodSource=_waterSource=_solidWasteSource=_wastedWaterSource=null;
            }

            if (patchScript != null)
            {
                _co2Source=patchScript.CO2FuelSource;
                _oxygenSource = patchScript.OxygenFuelSource;
                _waterSource=patchScript.WaterFuelSource;
                _solidWasteSource = patchScript.SolidWasteFuelSource;
                _wastedWaterSource = patchScript.WastedWaterFuelSource;
                _foodSource = patchScript.FoodFuelSource;
            }
        }

        public void DesignerStart(in DesignerFrameData frameData)
        {
            UpdateComponents();
        }
        
        public void FlightUpdate(in FlightFrameData frameData)
        {
            if (_compartment==null)
            {
                return;
            }
            
            if (PartScript.Data.Activated)
            {
                if (_compartment.Crew.Count==1)
                {
                    PlayParticle(chunkParticleSystem);
                    PlayParticle(bloodParticleSystemA);
                    PlayParticle(bloodParticleSystemB);
                    PlayParticle(bloodParticleSystemC);
                    PlayParticle(bloodParticleSystemD);
                    PlayParticle(bloodParticleSystemE);
                    PlayParticle(bloodParticleSystemF);
                    PlayParticle(bloodParticleSystemG);
                    PlayParticle(bloodParticleSystemH);
                    PlayParticle(bloodParticleSystemI);
                    DrainingLogic(frameData);
                }
                else
                {
                    chunkParticleSystem.Stop();
                    bloodParticleSystemA.Stop();
                    bloodParticleSystemB.Stop();
                    bloodParticleSystemC.Stop();
                    bloodParticleSystemD.Stop();
                    bloodParticleSystemE.Stop();
                    bloodParticleSystemF.Stop();
                    bloodParticleSystemG.Stop();
                    bloodParticleSystemH.Stop();
                    bloodParticleSystemI.Stop();
                }
               
                
            }
            else
            {
                chunkParticleSystem.Stop();
                bloodParticleSystemA.Stop();
                bloodParticleSystemB.Stop();
                bloodParticleSystemC.Stop();
                bloodParticleSystemD.Stop();
                bloodParticleSystemE.Stop();
                bloodParticleSystemF.Stop();
                bloodParticleSystemG.Stop();
                bloodParticleSystemH.Stop();
                bloodParticleSystemI.Stop();
                    
            }

            void PlayParticle(ParticleSystem particleSystem)
            {
                if (!particleSystem.isPlaying)
                {
                    particleSystem.Play();
                }
                
            }
        }

        private void DrainingLogic(in FlightFrameData frameData)
        {
            foreach (var crew in _compartment.Crew)
            {
                crew.PartScript.TakeDamage(Game.Instance.Settings.Game.Flight.ImpactDamageScale*0.05f,PartDamageType.Basic);
                var amount = Data.DrainRate * frameData.DeltaTimeWorld;
                _waterSource?.AddFuel(amount*Data.WaterConsumptionScale*5);
                _foodSource?.AddFuel(amount*Data.FoodGenerationScale*7);
               
                var data = crew.PartScript.GetModifier<SupportLifeScript>().Data;
                if (data._oxygenAmountBuffer>0)
                {
                    data.AddLifeSupportFuel("Oxygen", -amount*5);
                    _oxygenSource?.AddFuel(amount*1.1);
                }

                if (data._waterAmountBuffer>0)
                {
                    data.AddLifeSupportFuel("H2O", -amount*Data.WaterConsumptionScale*0.04);
                    _waterSource?.AddFuel(amount*Data.WaterConsumptionScale*1.1*Data.WaterConsumptionScale);
                }

                if (data._foodAmountBuffer>0)
                {
                    data.AddLifeSupportFuel("Food", -amount*Data.FoodGenerationScale*0.01);
                    _foodSource?.AddFuel(amount*Data.FoodGenerationScale*Data.FoodGenerationScale);
                }

                if (data._co2AmountBuffer>0)
                {
                    data.AddLifeSupportFuel("CO2", -amount);
                    _co2Source?.AddFuel(amount*1.1);
                }

                if (data._solidWasteAmountBuffer>0)
                {
                    data.AddLifeSupportFuel("Solid Waste", -amount);
                    _solidWasteSource?.AddFuel(amount*1.1);
                }

                if (data._wastedWaterAmountBuffer>0)
                {
                    data.AddLifeSupportFuel("Wasted Water", -amount);
                    _wastedWaterSource?.AddFuel(amount*1.1);
                }
               
               
            } 
        }        

        
        private void UpdateComponents()
        {
            _compartment = this.PartScript.GetModifier<CrewCompartmentScript>();
            string[]	strArray	= "Device/ParticleEffect".Split( '/', StringSplitOptions.None );
            Transform	subPart		= this.transform;
            foreach ( string n in strArray )
                subPart = subPart.Find( n ) ?? subPart;
            if ( subPart.name == strArray[strArray.Length - 1] )
                _particalSystemTransform =subPart;
            else
                _particalSystemTransform=( Utilities.FindFirstGameObjectMyselfOrChildren( "Device/ParticleEffect", this.gameObject ) ?.transform );
            if(_particalSystemTransform != null)
            {
                chunkParticleSystem = _particalSystemTransform.Find("ChunkParticleSystem").GetComponent<ParticleSystem>();
                bloodParticleSystemA = _particalSystemTransform.Find("BloodEffect").Find("Particle SystemA").GetComponent<ParticleSystem>();
                bloodParticleSystemB = _particalSystemTransform.Find("BloodEffect").Find("Particle SystemB").GetComponent<ParticleSystem>();
                bloodParticleSystemC = _particalSystemTransform.Find("BloodEffect").Find("Particle SystemC").GetComponent<ParticleSystem>();
                bloodParticleSystemD = _particalSystemTransform.Find("BloodEffect").Find("Particle SystemD").GetComponent<ParticleSystem>();
                bloodParticleSystemE = _particalSystemTransform.Find("BloodEffect").Find("Particle SystemE").GetComponent<ParticleSystem>();
                bloodParticleSystemF = _particalSystemTransform.Find("BloodEffect").Find("Particle SystemF").GetComponent<ParticleSystem>();
                bloodParticleSystemG = _particalSystemTransform.Find("BloodEffect").Find("Particle SystemG").GetComponent<ParticleSystem>();
                bloodParticleSystemH = _particalSystemTransform.Find("BloodEffect").Find("Particle SystemH").GetComponent<ParticleSystem>();
                bloodParticleSystemI = _particalSystemTransform.Find("BloodEffect").Find("Particle SystemI").GetComponent<ParticleSystem>();
                
                LKBaseTransform=_particalSystemTransform.parent.Find("LKBase");
                bodyTransform=LKBaseTransform.Find("Body");
                leftElbowTransform=LKBaseTransform.Find("LeftElbow");
                leftHandTransform=leftElbowTransform.Find("LeftHand");
                rightElbowTransform=LKBaseTransform.Find("RightElbow");
                rightHandTransform=rightElbowTransform.Find("RightHand");
            
            }

        }
        private void OnPilotEnter(EvaScript crew) => this.SetPilot(crew);

        /// <summary>Called when [pilot exit].</summary>
        /// <param name="crew">The crew.</param>
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
    
        private void SetIKEnabled(bool enabled) {
      if (!((UnityEngine.Object) this._pilotIK != (UnityEngine.Object) null))
        return;
      if (enabled)
      {
          
          this._pilotIK.solver.bodyEffector.positionWeight = 1f;
          this._pilotIK.solver.bodyEffector.rotationWeight = 1f;
          this._pilotIK.solver.bodyEffector.target = bodyTransform;
          this._pilotIK.solver.bodyEffector.positionWeight = 1f;
          this._pilotIK.solver.bodyEffector.rotationWeight = 1f;
          this._pilotIK.solver.rightHandEffector.target = this.rightHandTransform;
          this._pilotIK.solver.rightHandEffector.positionWeight = 1f;
          this._pilotIK.solver.rightHandEffector.rotationWeight = 1f;
          this._pilotIK.solver.leftHandEffector.target = this.leftHandTransform;
          this._pilotIK.solver.leftHandEffector.positionWeight = 1f;
          this._pilotIK.solver.leftHandEffector.rotationWeight = 1f;
          this._pilotIK.solver.leftArmChain.bendConstraint.bendGoal = this.leftElbowTransform;
          this._pilotIK.solver.leftArmChain.bendConstraint.weight = 1f;
          this._pilotIK.solver.rightArmChain.bendConstraint.bendGoal = this.rightElbowTransform;
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