using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using ModApi;
using ModApi.Audio;
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

    public class SacrificeScript : PartModifierScript<SacrificeData>,IFlightStart,IFlightUpdate,IDesignerUpdate,IDesignerStart
    {

        private CrewCompartmentScript _compartment;
        private ParticleSystem chunkParticleSystem,bloodParticleSystemA,bloodParticleSystemB,bloodParticleSystemC,bloodParticleSystemD,bloodParticleSystemE;
        private ISingleSound _sound;
        
        private Transform _particalSystemTransform,_bloodEffectTransform;
        private Transform LKBaseTransform;
        
        private IFuelSource highPressureGasSource;
        private IFuelSource lowPressureGasSource;
        private IFuelSource batterySource;
        public void FlightStart(in FlightFrameData frameData)
        {
            _compartment = this.PartScript.GetModifier<CrewCompartmentScript>();
            UpdateComponents();
            
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
            //_compartment.Crew[0].PartScript.TakeDamage(10f,PartDamageType.Basic);
            

            if (PartScript.Data.Activated)
            {
                PlayParticle(chunkParticleSystem);
                PlayParticle(bloodParticleSystemA);
                PlayParticle(bloodParticleSystemB);
                PlayParticle(bloodParticleSystemC);
                PlayParticle(bloodParticleSystemD);
                PlayParticle(bloodParticleSystemE);
            }
            
            else
            {
                chunkParticleSystem.Stop();
                bloodParticleSystemA.Stop();
                bloodParticleSystemB.Stop();
                bloodParticleSystemC.Stop();
                bloodParticleSystemD.Stop();
                bloodParticleSystemE.Stop();
                    
            }

            void PlayParticle(ParticleSystem particleSystem)
            {
                if (!particleSystem.isPlaying)
                {
                    particleSystem.Play();
                }
                
            }
        }

        private void ChunkParticles()
        {
            if (this.PartScript.Data.Activated)
            {
                chunkParticleSystem.Play();
            }
            else
            {
                chunkParticleSystem.Stop();
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
            }


            LKBaseTransform=_particalSystemTransform.parent.Find("LKBase");

        }
        
    }
}