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

    public class MethaloxGeneratorScript : ResourceProcessorPartScript<MethaloxGeneratorData>
    {
        private IFuelSource HPco2Source,HPoxygenSource,waterSource,methaneloxSource;
        private ParticleSystem _particleSystem;
        private Transform _particleSystemTransform;
        
        

        protected override void UpdateFuelSources()
        {
            base.UpdateFuelSources();
            HPco2Source = GetRegularCraftFuelSource("HPCO2");
            HPoxygenSource = GetRegularCraftFuelSource("HPOxygen");
            waterSource = this.PartScript.CommandPod.Part.PartScript.GetModifier<STCommandPodPatchScript>()
                .WaterFuelSource;
            methaneloxSource = GetRegularCraftFuelSource("LOX/CH4");
        }

        public override void FlightUpdate(in FlightFrameData frame)
        {
            if (!PartScript.Data.Activated)
            {
                _particleSystem.Stop();
               return; 
            }
            WorkingLogic(frame);
        }
        protected override void WorkingLogic(in FlightFrameData frame)
        {
            if (BatterySource==null||HPco2Source==null||HPoxygenSource==null||waterSource==null||methaneloxSource==null)
            {
                _particleSystem.Stop();
                return;
            }

            if (BatterySource.IsEmpty||HPco2Source.IsEmpty||HPoxygenSource.IsEmpty||waterSource.IsEmpty||methaneloxSource.TotalCapacity-methaneloxSource.TotalFuel<0.00001f)
            {
                _particleSystem.Stop();
                return;
            }

            BatterySource.RemoveFuel(Data.BatteryConsumption*frame.DeltaTimeWorld);
            HPco2Source.RemoveFuel(Data.Hpco2Consumption*frame.DeltaTimeWorld);
            HPoxygenSource.RemoveFuel(Data.HpoxygenConsumption*frame.DeltaTimeWorld);
            waterSource.RemoveFuel(Data.WaterConsumption*frame.DeltaTimeWorld);
            methaneloxSource.AddFuel(Data.MethaneloxGeneration*frame.DeltaTimeWorld);
            if (_particleSystem!=null)
            {
                PlayEffects();
            }
        }


        private void PlayEffects()
        {
            if (!_particleSystem.isPlaying)
            {
               _particleSystem.Play(); 
            }
        }
        protected override void UpdateComponents()
        {
            string[]	strArray	= "Device/ParticleSystem".Split( '/', StringSplitOptions.None );
            Transform	subPart		= this.transform;
            foreach ( string n in strArray )
                subPart = subPart.Find( n ) ?? subPart;
            if ( subPart.name == strArray[strArray.Length - 1] )
                this.SetSubPart(subPart);
            else
                this.SetSubPart( ModApi.Utilities.FindFirstGameObjectMyselfOrChildren( "Device/ParticleSystem/", this.gameObject ) ?.transform );
            _particleSystem = _particleSystemTransform.GetComponent<ParticleSystem>();
            
        }
        private void SetSubPart( Transform subPart )
        {
            this._particleSystemTransform = subPart;
        }
    }
}