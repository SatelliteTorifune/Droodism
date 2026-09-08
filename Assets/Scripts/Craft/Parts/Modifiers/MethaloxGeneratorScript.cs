using ModApi.GameLoop;

namespace Assets.Scripts.Craft.Parts.Modifiers
{
    using ModApi.Craft.Parts;
    using ModApi.GameLoop.Interfaces;
    using UnityEngine;

    public class MethaloxGeneratorScript : ResourceProcessorPartScript<MethaloxGeneratorData>, IPartSubPartSetUp
    {
        private IFuelSource HPco2Source,waterSource,methaneloxSource;
        private ParticleSystem _particleSystem;
        private Transform _particleSystemTransform;

        public Transform SubPart => _particleSystemTransform;
        
        

        protected override void UpdateFuelSources()
        {
            base.UpdateFuelSources();
            HPco2Source = GetRegularCraftFuelSource("HPCO2");
            waterSource = GetCommandPodPatch()
                .WaterFuelSource;
            methaneloxSource = GetRegularCraftFuelSource("LOX/CH4");
        }

        public override void FlightUpdate(in FlightFrameData frame)
        {
            if (_particleSystem==null)
            {
                Mod.Log("return");
                return;
            }
            if (!PartScript.Data.Activated)
            {
                _particleSystem.Stop();
               return; 
            }
            WorkingLogic(frame);
        }
        protected override void WorkingLogic(in FlightFrameData frame)
        {
            if (BatterySource==null||HPco2Source==null||waterSource==null||methaneloxSource==null)
            {
                _particleSystem.Stop();
                return;
            }

            if (BatterySource.IsEmpty||HPco2Source.IsEmpty||waterSource.IsEmpty||methaneloxSource.TotalCapacity-methaneloxSource.TotalFuel<0.00001f)
            {
                _particleSystem.Stop();
                return;
            }

            BatterySource.RemoveFuel(Data.BatteryConsumption*frame.DeltaTimeWorld);
            HPco2Source.RemoveFuel(Data.Hpco2Consumption*frame.DeltaTimeWorld);
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
            SetSubPart(IPartSubPartSetUp.FindSubPart(this, "Device/ParticleSystem"));
            _particleSystem = _particleSystemTransform.GetComponent<ParticleSystem>();
            
        }

        public void SetSubPart( Transform subPart )
        {
            this._particleSystemTransform = subPart;
        }
    }
}