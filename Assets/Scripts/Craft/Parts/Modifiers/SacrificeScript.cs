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
        private ParticleSystem chunkParticleSystem;
        private ISingleSound _sound;
        
        private Transform _particalSystemTransform;
        
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
            //ChunkParticles();

            if (PartScript.Data.Activated)
            {
                if (!chunkParticleSystem.isPlaying)
                {
                    chunkParticleSystem.Play();
                }
            }
            
            else
            {
                chunkParticleSystem.Stop();
                    
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
                this.SetSubPart( subPart );
            else
                this.SetSubPart( Utilities.FindFirstGameObjectMyselfOrChildren( "Device/ParticleEffect", this.gameObject ) ?.transform );
            chunkParticleSystem = _particalSystemTransform.Find("ChunkParticleSystem").GetComponent<ParticleSystem>();
            if (chunkParticleSystem == null)
            {
                Debug.LogFormat("这有问题");
            }
           
        }
        public void SetSubPart( Transform subPart )
        {
            this._particalSystemTransform = subPart;
        }
    }
}