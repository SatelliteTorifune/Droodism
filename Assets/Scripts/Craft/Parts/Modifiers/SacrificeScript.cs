using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using ModApi;
using ModApi.Audio;
using ModApi.GameLoop;
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

    public class SacrificeScript : PartModifierScript<SacrificeData>,IFlightStart,IFlightUpdate,IDesignerUpdate,IDesignerStart
    {

        private CrewCompartmentScript _compartment;
        private ParticleSystem chunkParticleSystem,bloodParticleSystemA,bloodParticleSystemB,bloodParticleSystemC,bloodParticleSystemD,bloodParticleSystemE,bloodParticleSystemF,bloodParticleSystemG,bloodParticleSystemH,bloodParticleSystemI;
        private ISingleSound _sound;
        
        private Transform _particalSystemTransform,_bloodEffectTransform;
        private Transform LKBaseTransform,leftHandTransform,leftElbowTransform,rightHandTransform,rightElbowTransform,bodyTransform,headTransform;
        private FullBodyBipedIK _pilotIK;
        private EvaScript _pilot = (EvaScript) null;
        private AttachPoint _seatAttachPoint;
        
        private IFuelSource foodSource;
        private IFuelSource waterSource;
        private IFuelSource soildWasteSource;

        protected override void OnInitialized()
        {
            base.OnInitialized();
        }
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
          
            UpdateComponents();
            _sound.MaxVolume = 0.5f;
            var patchScript = PartScript?.CommandPod.Part.PartScript.GetModifier<STCommandPodPatchScript>();
            if (patchScript == null)
            {
                foodSource = null;
            }

            if (patchScript != null)
            {
                foodSource = patchScript.FoodFuelSource;

            }

        }

        public void DesignerStart(in DesignerFrameData frameData)
        {
            UpdateComponents();
           
        }

        public void DesignerUpdate(in DesignerFrameData frameData)
        {
            
        }
        public void FlightUpdate(in FlightFrameData frameData)
        {
            
            

            if (PartScript.Data.Activated)
            {
                foodSource.AddFuel(Data.FoodGenerationScale*frameData.DeltaTimeWorld);
                foreach (var crew in _compartment.Crew)
                {
                    crew.PartScript.TakeDamage(Game.Instance.Settings.Game.Flight.ImpactDamageScale*0.1f,PartDamageType.Basic);
                }
                this._sound.AddPosition(this.transform.position, 1);
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

        
        private void UpdateComponents()
        {
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
            
            }


            LKBaseTransform=_particalSystemTransform.parent.Find("LKBase");
            bodyTransform=LKBaseTransform.Find("Body");
            headTransform=LKBaseTransform.Find("Head");
            leftElbowTransform=LKBaseTransform.Find("LeftElbow");
            leftHandTransform=leftElbowTransform.Find("LeftHand");
            rightElbowTransform=LKBaseTransform.Find("RightElbow");
            rightHandTransform=rightElbowTransform.Find("RightHand");
            
            this._sound =Game.Instance.FlightScene.SingleSoundManager.GetSingleSound("Assets/Content/Craft/Parts/Sacrifice");
            if (_sound==null)
            {
                Debug.Log("Sacrifice sound not found");
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
        this._pilotIK.solver.leftArmChain.bendConstraint.weight = 0.8f;
        /*
        this._pilotIK.solver.rightFootEffector.target = this._rudderRightFootPosition;
        this._pilotIK.solver.rightFootEffector.positionWeight = 1f;
        this._pilotIK.solver.rightFootEffector.rotationWeight = 1f;
        this._pilotIK.solver.leftFootEffector.target = this._rudderLeftFootPosition;
        this._pilotIK.solver.leftFootEffector.positionWeight = 1f;
        this._pilotIK.solver.leftFootEffector.rotationWeight = 1f;
        */
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