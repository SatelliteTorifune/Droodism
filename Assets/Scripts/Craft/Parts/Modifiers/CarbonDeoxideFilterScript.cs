using ModApi;
using ModApi.Craft;
using ModApi.GameLoop;
using RootMotion.FinalIK;

namespace Assets.Scripts.Craft.Parts.Modifiers
{
	using System;
	using ModApi.Craft.Parts;
	using ModApi.GameLoop.Interfaces;
	using UnityEngine;

	public class CarbonDeoxideFilterScript : ResourceProcessorPartScript<CarbonDeoxideFilterData>,IPartSubPartSetUp,
		IDesignerStart
	{
		private IFuelSource co2Source;
		private Transform	_offset, FanA, FanB;
		private Vector3		_offsetPositionInverse;
		

		public override void FlightStart( in FlightFrameData frame )
		{
			UpdateFuelSources();
			UpdateComponents();
		}


		public override void FlightUpdate( in FlightFrameData frame )
		{
			if ( co2Source == null || BatterySource == null )
			{
				return;
			}

			if ( co2Source.IsEmpty || BatterySource.IsEmpty )
			{
				WorkingAnimation( false );
				return;
			}
			WorkingAnimation( this.PartScript.Data.Activated );
			WorkingLogic( frame );
		}

		


		protected override void WorkingLogic( in FlightFrameData frame )
		{
			if ( PartScript.Data.Activated )
			{
				co2Source.RemoveFuel( Data.Co2ConsumptionRate * frame.DeltaTimeWorld );
				BatterySource.RemoveFuel( Data.ElectricityPowerConsumptionRatePerCo2 * Data.Co2ConsumptionRate * frame.DeltaTimeWorld );
			}
		}


		protected override void WorkingAnimation( bool active )
		{
			if ( FanA == null )
			{
				return;
			}

			if ( FanB == null )
			{
				return;
			}
			float b = 0.0f;
			if ( active )
				b = 0.25f + 0.5f;
			Data.FanSpeed = Mathf.Lerp( this.Data.FanSpeed, b, Time.deltaTime * 0.4f );
			if ( (double) this.Data.FanSpeed > 0.0 )
			{
				float zAngle = (float) (-(double) this.Data.FanSpeed * 360.0 * 3.0) * Time.deltaTime;
				FanA.Rotate( 0.0f, 0.0f, zAngle );
				FanB.Rotate( 0.0f, 0.0f, -1.5f * zAngle );
			}
		}


		public Transform SubPart => FanA;

		protected override void UpdateComponents()
		{
			SetSubPart( IPartSubPartSetUp.FindSubPart( this, "DeviceBase/DeviceFanA" ) );
			if ( this.FanA != null )
			{
				FanB = FanA.Find( "DeviceFanB" );
			}
		}

		public void SetSubPart( Transform subPart )
		{
			this.FanA = subPart;
			this._offset = IPartSubPartSetUp.ApplySubPart( this._offset, this.FanA, Data.PositionOffset1, out this._offsetPositionInverse );
		}


		#region fuelsource related
		protected override void UpdateFuelSources()
		{
			base.UpdateFuelSources();
			try
			{
				var patchScript = PartScript?.CommandPod.Part.PartScript.GetModifier<STCommandPodPatchScript>();
				if (patchScript == null)
				{
					co2Source = null;
				}

				if (patchScript!= null)
				{
					co2Source = patchScript.CO2FuelSource;
				
				}
			}
			catch (Exception)
			{
				co2Source = null;
			}
			
			
		}
		


		#endregion
	}
}